using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lantern.Face.Json {
    
    public class ParseError : Exception {
        public ParseError(string reason) : base(reason) { }
        public ParseError(string reason, Exception innerException) : base(reason, innerException) { }
    }
    
    internal class Parser {
        private readonly bool relaxed;
        private readonly string input;
        private int position = -1;
        private static readonly char[] whiteSpaceChars = new char[] { '\r', '\n', '\t', ' ' };

        private Parser(string s, bool relaxed = false) {
            input = s;
            this.relaxed = relaxed;
        }

        /// <summary>
        /// Produces a JsValue object from a JSON-formatted string
        /// </summary>
        /// <param name="s">JSON-formatted string</param>
        /// <param name="relaxed">True to allow // comments and unquoted property names</param>
        /// <returns>Object representing the structure defined in JSON</returns>
        public static JsValue Parse(string s, bool relaxed = false){
            var parser = new Parser(s, relaxed);
            parser.NextToken();
            var result = parser.readValue();
            parser.NextToken(true);
            return result;
        }
       
        /// <summary>
        /// Moves the read position to the next meaningful symbol.
        /// </summary>
        /// <param name="expectEot">True to throw an exception if a symbol is found</param>
        /// <exception cref="ParseError"></exception>
        private void NextToken(bool expectEot = false){
            position++;
            if (!expectEot && position >= input.Length) throw new ParseError("Past end of input");
            while(position < input.Length && whiteSpaceChars.Contains(current)){
                position++;
                if(!expectEot && position >= input.Length) throw new ParseError("Past end of input");
            }

            if (relaxed && position < input.Length - 1 && current == '/' && input[position + 1] == '/') { // skip comment if relaxed
                var regex = new Regex("\r\n|\r|\n");
                var nextLine = regex.Match(input, position);
                if (nextLine.Success) {
                    position = nextLine.Index;
                    NextToken(expectEot);
                }
                else position = input.Length;
            }
            if(expectEot && position < input.Length) throw new ParseError($"Unexpected '{current}' at input position {position}");
        }

        /// <summary>
        /// Determines the line number of a given character position in the JSON input and appends it if the input contains multiple lines
        /// </summary>
        /// <param name="pos">A position in the JSON input</param>
        /// <returns>E.g. "61 (line 4)"</returns>
        private string appendLineNumber(int pos) {
            string s = pos.ToString();
            Regex regex = new Regex("\r\n|\n|\r");
            int lineCount = regex.Matches(input.Substring(0, pos)).Count;
            // needn't scan for a cr/lf if we've just found some
            if (lineCount > 0 || regex.IsMatch(input)) s += $" (line {lineCount + 1})";
            return s;
        }

        /// <summary>
        /// A string representation of the current read position for output in exception messages.
        /// </summary>
        private string positionWithLine => appendLineNumber(position);

        /// <summary>
        /// Reads a value from input and places the read position at the end.
        /// </summary>
        /// <returns>The value read from input</returns>
        /// <exception cref="ParseError"></exception>
        private JsValue readValue() {
            if (current == '"') return readString();
            if (current == '[') return readArray();
            if (current == '{') return readObject();
            if (input.Length - position >= 4 && input.Substring(position, 4) == "null") {
                position += 3;
                return JsValue.Null;
            }

            if (input.Length - position >= 4 && input.Substring(position, 4) == "true") {
                position += 3;
                return true;
            }

            if (input.Length - position >= 5 && input.Substring(position, 5) == "false") {
                position += 4;
                return false;
            }

            var numberMatch = new Regex("\\G-?\\d*\\.?\\d+").Match(input, position);
            if (numberMatch.Success) {
                string cap = numberMatch.Captures[0].Value;
                double num = Convert.ToDouble(cap);
                position += numberMatch.Captures[0].Length - 1;
                return num;
            }

            throw new ParseError($"Unexpected '{current}' at input position {positionWithLine}");
        }

        /// <summary>
        /// Reads the next four characters as a hexadecimal symbol reference and places the read position at the end.
        /// </summary>
        /// <returns>String containing the referenced character</returns>
        /// <exception cref="ParseError"></exception>
        private string readEscapedCodepoint() {
            var sequenceMatch = new Regex("\\G[0-9a-f]{4}").Match(input, position + 1);
            if(!sequenceMatch.Success) throw new ParseError($"Malformed \\u sequence at input position {positionWithLine}");
            position += 4;
            return char.ConvertFromUtf32(int.Parse(sequenceMatch.Value, System.Globalization.NumberStyles.HexNumber));
        }
        
        /// <summary>
        /// Reads a string value from input and places the read position at the closing '"'. Assumes the current character is '"'.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ParseError"></exception>
        private string readString(){
            bool escaping = false;
            if (relaxed && current != '"') {
                var regex = new Regex("\\G\\w+");
                var match = regex.Match(input, position);
                if (match.Success) {
                    position += match.Value.Length - 1;
                    return match.Value;
                }
            }
            var startPosition = position;
            position++; // skip first "
            var sb = new StringBuilder();
            if(position >= input.Length) throw new ParseError($"Unclosed string at input position {appendLineNumber(startPosition)}");
            while(escaping || current != '"'){
                if(escaping){
                    escaping = false;
                    try {
                        switch (current) {
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case '"':
                                sb.Append('"');
                                break;
                            case 'u': {
                                sb.Append(readEscapedCodepoint());
                                break;
                            }
                            case '\\':
                                sb.Append('\\');
                                break;
                            default:
                                throw new ParseError($"Unknown escape character at input position {positionWithLine}");
                        }
                    }
                    catch (ParseError e) {
                        throw new ParseError($"Failed to parse string starting at input position {appendLineNumber(startPosition)}", e);
                    }

                    position++; 
                    if(position >= input.Length) throw new ParseError($"Unclosed string at input position {appendLineNumber(startPosition)}");
                    continue;
                }
                if(current == '\\'){
                    escaping = true;
                } else sb.Append(current);
                position++;
                if(position >= input.Length) throw new ParseError($"Unclosed string at input position {appendLineNumber(startPosition)}");
            }
            return sb.ToString();
        }

        private char current => input[position];
        
        /// <summary>
        /// Reads an array from input and places the read position at the closing ']'. Assumes the current character is '['
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ParseError"></exception>
        private JsValue[] readArray(){
            var startPosition = position;
            List<JsValue> found = new List<JsValue>();
            try {
                while (true) {
                    NextToken();
                    if (current == ']') {
                        return found.ToArray();
                    }

                    // add to found if and when we find , or ] to keep correct index in error message
                    var value = readValue();

                    NextToken();
                    if (current == ']') {
                        found.Add(value);
                        return found.ToArray();
                    }
                    if (current != ',')
                        throw new ParseError($"Expected ',' or ']', found '{current}' at input position {positionWithLine}");
                    found.Add(value);
                }
            }
            catch (ParseError e) {
                throw new ParseError($"Failed to parse item #{found.Count} in array starting at input position {appendLineNumber(startPosition)}", e);
            }
        }
        
        /// <summary>
        /// Shortens strings to a maximum length for inclusion in exception messages.
        /// </summary>
        /// <param name="k"></param>
        /// <returns>Shortened key</returns>
        private static string shortenKey(string k) => k.Length <= 16 ? k : (k.Substring(0, 13) + "..."); 
        
        /// <summary>
        /// Reads an object from input and places the cursor at the closing '}'. Assumes the current character is '{'.
        /// </summary>
        /// <returns>A JsValue object with a DataType of Object</returns>
        /// <exception cref="ParseError"></exception>
        private JsValue readObject() {
            var startPosition = position;
            var result = new Dictionary<string, JsValue>();
            while (true) {
                NextToken();
                if (current == '}') return result;
                if (!relaxed && current != '"')
                    throw new ParseError(
                        $"Expected property name #{result.Count} in object starting at input position {appendLineNumber(startPosition)}, found '{current}' at input position {positionWithLine}");
                string keyValue;
                try {
                    keyValue = readString();
                }
                catch (ParseError e) {
                    throw new ParseError(
                        $"Failed to parse property name #{result.Count} in object starting at input position {appendLineNumber(startPosition)}", e);
                }

                NextToken();
                if (current != ':')
                    throw new ParseError(
                        $"Expected ':' for property #{result.Count} {shortenKey(keyValue).ToJson()} in object starting at input position {appendLineNumber(startPosition)}, found '{current}' at input position {positionWithLine}");
                NextToken();
                JsValue value;
                try {
                    value = readValue();
                }
                catch (ParseError e) {
                    throw new ParseError($"Failed to parse value for property {shortenKey(keyValue).ToJson()} in object starting at input position {appendLineNumber(startPosition)}", e);
                }

                result[keyValue] = value;
                NextToken();
                if (current == '}') return result;
                if (current != ',')
                    throw new ParseError(
                        $"Expected ',' or '{'}'}' in object starting at input position {appendLineNumber(startPosition)}, found '{current}' at input position {positionWithLine}");
            }
        }

    }
}