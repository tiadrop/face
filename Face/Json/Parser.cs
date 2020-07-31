using System;
using System.Collections.Generic;
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
        private readonly int length;

        private Parser(string json, bool relaxed = false) {
            this.input = json;
            length = json.Length;
            this.relaxed = relaxed;
        }

        /// <summary>
        /// Produces a JsValue object from a JSON-formatted string
        /// </summary>
        /// <param name="json">JSON-formatted string</param>
        /// <param name="relaxed">True to allow // comments and unquoted property names</param>
        /// <returns>Object representing the structure defined in JSON</returns>
        public static JsValue Parse(string json, bool relaxed = false){
            var parser = new Parser(json, relaxed);
            parser.NextToken(); // bring us to #0 + any whitespace
            var result = parser.readValue();
            parser.NextToken(true); // bring us hopefully to end of input
            if(parser.position < parser.length) throw new ParseError($"Unexpected '{parser.current}' at input position {parser.positionWithLine}");
            return result;
        }
       
        /// <summary>
        /// Reads the character at the current read position
        /// </summary>
        private char current => input[position];
        private static readonly Regex crLfRegex = new Regex("\r\n|\r|\n");

        /// <summary>
        /// Moves the read position to the next meaningful symbol.
        /// </summary>
        /// <param name="expectEot">True to throw a ParseError if a symbol is found, False to throw a ParseError otherwise</param>
        /// <exception cref="ParseError"></exception>
        private void NextToken(in bool expectEot = false) {
            while (true) {
                position++;

                if (position >= length) {
                    if (!expectEot)
                        throw new ParseError("Past end of input");
                    return;
                }

                char c = current;
                // skip whitespace
                if (position < length)
                    while (c == ' ' || c == '\r' || c == '\t' || c == '\n') {
                        position++;
                        if (position == length) {
                            if (!expectEot)
                                throw new ParseError("Past end of input");
                            return; // expected eot, found eot
                        }
                        c = current;
                    }

                // skip comments (if relaxed)
                if (relaxed && position < length - 1 && c == '/' && input[position + 1] == '/') { // skip comment if relaxed
                    var nextLine = crLfRegex.Match(input, position + 2);
                    if (nextLine.Success) {
                        position = nextLine.Index + nextLine.Length - 2;
                        continue;
                    } else { // already on the last line; eot
                        if (!expectEot)
                            throw new ParseError("Past end of input");
                        position = length;
                    }
                }

                break;
            }
        }

        /// <summary>
        /// Determines the line number of a given character position in the JSON input and appends it if the input contains multiple lines
        /// </summary>
        /// <param name="pos">A position in the JSON input</param>
        /// <returns>E.g. "61 (line 4)"</returns>
        private string appendLineNumber(int pos) {
            string s = pos.ToString();
            int lineCount = crLfRegex.Matches(input.Substring(0, pos)).Count;
            // needn't scan for a cr/lf if we've just found some
            if (lineCount > 0 || crLfRegex.IsMatch(input)) s += $" (line {lineCount + 1})";
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
            char c = current;
            switch (c) {
                case '"': return new JsValue(readString());
                case '[': return new JsValue(readArray());
                case '{': return readObject();
            }

            if (numberMatch(true)) return new JsValue(readNumber());

            if (length - position >= 4 && (c == 't' || c == 'f' || c == 'n')) {
                string nextFour = input[position .. (position + 4)];
                switch (nextFour) {
                    case "null":
                        position += 3;
                        return JsValue.Null;
                    case "true":
                        position += 3;
                        return true;
                    case "fals" when length - position >= 5 && input[position + 4] == 'e':
                        position += 4;
                        return false;
                    default:
                        var key = readKeyRelaxed();
                        position -= key.Length - 1;
                        throw new ParseError($"Unexpected '{shortenKey(key)}' at input position {positionWithLine}");
                }
            }

            throw new ParseError($"Unexpected '{current}' at input position {positionWithLine}");
        }

        private double readNumber() {
            int lengthFound = 1;
            while (position < length && numberMatch(false, 1)) {
                position++;
                lengthFound++;
            }

            pointOrExpFound = false;
            string numberString = input[(position + 1 - lengthFound) .. (position + 1)];
            try {
                return double.Parse(numberString);
            } catch (Exception e) {
                throw new ParseError($"Failed to parse number at input position {appendLineNumber(position + 1 - lengthFound)}", e);
            }
        }

        private bool pointOrExpFound = false;
        /// <summary>
        /// Ascertains whether the the current character is numeric. '.' is considered numeric on first encounter after setting floatingPointFound = false.
        /// Sets pointOrExpFound to True if the character is '.' or 'e'.
        /// </summary>
        /// <param name="allowMinus">Defines whether to accept "-" as numeric, i.e. if we are expecting the first character of the numeric value</param>
        /// <returns>True if the symbol at current position is numeric</returns>
        private bool numberMatch(bool allowMinus, int offset = 0) {
            switch (input[position + offset]) {
                case '-': return allowMinus;
                case '.':
                case 'e':
                    if(pointOrExpFound) return false;
                    pointOrExpFound = true;
                    return true;
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    return true;
            }
            return false;
        }

        private static readonly Regex escapedCodePointRegex = new Regex("\\G[0-9a-fA-F]{4}");

        /// <summary>
        /// Reads the next four characters as a hexadecimal symbol reference and places the read position at the end.
        /// </summary>
        /// <returns>String containing the referenced character</returns>
        /// <exception cref="ParseError"></exception>
        private string readEscapedCodepoint() {
            var sequenceMatch = escapedCodePointRegex.Match(input, position + 1);
            if(!sequenceMatch.Success) throw new ParseError($"Malformed \\u sequence at input position {positionWithLine}");
            position += 4;
            try {
                return char.ConvertFromUtf32(
                    int.Parse(sequenceMatch.Value, System.Globalization.NumberStyles.HexNumber));
            }
            catch (ArgumentOutOfRangeException e) {
                throw new ParseError($"Failed to parse \\u sequence at input position {positionWithLine}", e);
            }
        }
        
        private static readonly Regex unquotedKeyRegex = new Regex("\\G\\w+");

        private string readKeyRelaxed() {
            var match = unquotedKeyRegex.Match(input, position);
            if (match.Success) {
                position += match.Value.Length - 1;
                return match.Value;
            }
            throw new ParseError($"Unexpected '{current}' at input position {positionWithLine}");
        }
        
        /// <summary>
        /// Reads a string value from input and places the read position at the closing '"'. Assumes the current character is '"'.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ParseError"></exception>
        private string readString() {
            var startPosition = position;
            position++; // skip first "

            var sb = new StringBuilder();
            bool escaping = false;
            int literalLength = 0;
            if(position == length) throw new ParseError($"Unclosed string at input position {appendLineNumber(startPosition)}");
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
                } else if(current == '\\'){
                    escaping = true;
                    if (literalLength > 0) {
                        sb.Append(input, position - literalLength, literalLength);
                        literalLength = 0;
                    }
                } else literalLength++;
                position++;
                
                if(position >= length) throw new ParseError($"Unclosed string at input position {appendLineNumber(startPosition)}");
            }

            if(literalLength > 0) sb.Append(input, position - literalLength, literalLength);
            return sb.ToString();
        }
        
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
                    if (current == ']') return found.ToArray();

                    // add to found if and when we find , or ] to keep correct index in error message
                    var value = readValue();
                    NextToken(); 
                    
                    switch (current) {
                        case ']':
                            found.Add(value);
                            return found.ToArray();
                        case ',':
                            found.Add(value);
                            continue;
                        default: throw new ParseError($"Expected ',' or ']', found '{current}' at input position {positionWithLine}");
                    }
                }
            } catch (ParseError e) {
                throw new ParseError($"Failed to parse array starting at input position {appendLineNumber(startPosition)}; index {found.Count}", e);
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
                string keyValue;
                try {
                    NextToken(); // move to key
                    switch (current) {
                        case '}': return result;
                        case '"':
                            keyValue = readString();
                            break;
                        default: 
                            if(!relaxed) throw new ParseError(
                            $"Expected '\"', found '{current}' at input position {positionWithLine}");
                            keyValue = readKeyRelaxed();
                            break;
                    }
                } catch (ParseError e) {
                    throw new ParseError(
                        $"Failed to parse object starting at input position {appendLineNumber(startPosition)}; invalid property name at index {result.Count}", e);
                }

                try { NextToken(); } // move to :
                catch (ParseError e) {
                    throw new ParseError($"Failed to parse object starting at input position {appendLineNumber(startPosition)}; expected ':' following property name '{shortenKey(keyValue)}'", e);
                }
                if (current != ':')
                    throw new ParseError(
                        $"Failed to parse object starting at input position {appendLineNumber(startPosition)}; expected ':' following property name '{shortenKey(keyValue).ToJson()}', found '{current}' at input position {positionWithLine}");

                try { NextToken(); } // move to value
                catch (ParseError e) {
                    throw new ParseError($"Failed to parse object starting at input position {appendLineNumber(startPosition)}; expected value for property '{shortenKey(keyValue)}'", e);
                }
                
                JsValue value;
                try {
                    value = readValue();
                } catch (ParseError e) {
                    throw new ParseError($"Failed to parse object starting at input position {appendLineNumber(startPosition)}, value for property {shortenKey(keyValue).ToJson()}", e);
                }

                result[keyValue] = value;
                try { NextToken(); } // move to , or }
                catch (ParseError e) { // surely EOT
                    throw new ParseError($"Failed to parse object starting at input position {appendLineNumber(startPosition)}; expected ',' or '{'}'}' following value for property '{shortenKey(keyValue)}'", e);
                }

                switch (current) {
                    case '}': return result;
                    case ',': continue;
                    default:
                        throw new ParseError(
                            $"Failed to parse object starting at input position {appendLineNumber(startPosition)}; expected ',' or '{'}'}', found '{current}' at input position {positionWithLine}");
                }
            }
        }

    }
}