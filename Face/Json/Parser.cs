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
        private string input;
        private Parser(string s) => input = s;
        public static JsValue Parse(string s){
            var parser = new Parser(s);
            parser.NextToken();
            var result = parser.readValue();
            parser.NextToken(true);
            return result;
        }

        private static char[] whiteSpaceChars = new char[] { '\r', '\n', '\t', ' ' };

        private int position = -1;
        private void NextToken(bool expectEot = false){
            position++;
            if (!expectEot && position >= input.Length) throw new ParseError("Past end of input");
            while(position < input.Length && whiteSpaceChars.Contains(current)){
                position++;
                if(!expectEot && position >= input.Length) throw new ParseError("Past end of input");
            }
            if(expectEot && position < input.Length) throw new ParseError($"Unexpected '{current}' at input position {position}");
        }

        private string appendLineNumber(int pos) {
            string s = pos.ToString();
            Regex regex = new Regex("\r\n|\n");
            int lineCount = regex.Matches(input.Substring(0, pos)).Count;
            // needn't scan for a cr/lf if we've just found some
            if (lineCount > 0 || regex.IsMatch(input)) s += $" (line {lineCount + 1})";
            return s;
        }


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

            var numberMatch = new Regex("-?\\d*\\.?\\d+").Match(input, position);
            if (numberMatch.Success) {
                string cap = numberMatch.Captures[0].Value;
                double num = Convert.ToDouble(cap);
                position += numberMatch.Captures[0].Length - 1;
                return num;
            }

            throw new ParseError($"Unexpected '{current}' at input position {appendLineNumber(position)}");
        }

        private string readEscapedCodepoint() {
            if(input.Length - position <= 4) throw new ParseError($"Malformed \\u sequence at input position {appendLineNumber(position)}");
            var sequence = input.Substring(position + 1, 4);
            var exp = new Regex("^[0-9a-f]{4}$");
            if(sequence.Length != 4 || !exp.IsMatch(sequence)) throw new ParseError($"Malformed \\u sequence at input position {appendLineNumber(position)}");
            position += 5;
            return char.ConvertFromUtf32(int.Parse(sequence, System.Globalization.NumberStyles.HexNumber));
        }

        private string readString(){
            var startPosition = position;
            bool escaping = false;
            position++; // skip first "
            var sb = new StringBuilder();
            if(position >= input.Length) throw new ParseError($"Unclosed string at input position {appendLineNumber(startPosition)}");
            while(escaping || current != '"'){
                if(escaping){
                    escaping = false;
                    switch (current) {
                        case 'n':
                            sb.Append("\n");
                            break;
                        case 'r':
                            sb.Append("\r");
                            break;
                        case 't':
                            sb.Append("\t");
                            break;
                        case 'u': {
                            sb.Append(readEscapedCodepoint());
                            continue;
                        }
                        default:
                            sb.Append(current);
                            break;
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
                        throw new ParseError($"Expected ',' or ']', found '{current}' at input position {appendLineNumber(position)}");
                    found.Add(value);
                }
            }
            catch (Exception e) {
                throw new ParseError($"Failed to parse item #{found.Count} in array starting at input position {appendLineNumber(startPosition)}", e);
            }
        }

        private JsValue readObject(){
            var startPosition = position;
            var result = new Dictionary<string, JsValue>();
            while (true) {
                NextToken();
                if (current == '}') return result;
                if (current != '"')
                    throw new ParseError(
                        $"Expected property name #{result.Count} in object starting at input position {appendLineNumber(startPosition)}, found '{current}' at input position {appendLineNumber(position)}");
                string keyValue;
                try {
                    keyValue = readValue();
                }
                catch (Exception e) {
                    throw new ParseError(
                        $"Failed to parse property name #{result.Count} in object starting at input position {appendLineNumber(startPosition)}",
                        e);
                }

                NextToken();
                if (current != ':')
                    throw new ParseError(
                        $"Expected ':' for property #{result.Count} {keyValue.ToJson()} in object starting at input position {appendLineNumber(startPosition)}, found '{current}' at input position {appendLineNumber(position)}");
                NextToken();
                JsValue value;
                try {
                    value = readValue();
                }
                catch (Exception e) {
                    throw new ParseError($"Failed to parse value for property {keyValue.ToJson()} in object starting at input position {appendLineNumber(startPosition)}", e);
                }

                result[keyValue] = value;
                NextToken();
                if (current == '}') return result;
                if (current != ',')
                    throw new ParseError(
                        $"Expected ',' or '{'}'}' in object starting at input position {appendLineNumber(startPosition)}, found '{current}' at input position {appendLineNumber(position)}");
            }
        }

    }
}