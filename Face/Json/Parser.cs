using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lantern.Face.Json {
    
    public class ParseError : Exception {
        public int JsonPosition;

        public ParseError(string reason, int position = -1) : base(reason) {
            JsonPosition = position;
        }
        public ParseError(string reason, Exception inner, int position = -1) : base(reason, inner) {
            JsonPosition = position;
        }

        public ParseError(string reason, Exception inner) : base(reason, inner) { }
        public readonly string jsonPath;
        public ParseError(JsValue jsonPath) : base() {
            this.jsonPath = jsonPath;
        }
        public ParseError(Exception innerException, JsValue jsonPath) : base(null, innerException) {
            this.jsonPath = jsonPath;
            if (innerException is ParseError innerParseError) JsonPosition = innerParseError.JsonPosition;
        }

        public string FullJsonPath {
            get {
                if (!(InnerException is ParseError inner)) return jsonPath; 
                return jsonPath + inner.FullJsonPath;
            }
        }
        
    }
    
    internal class Parser {
        private readonly bool relaxed;
        private readonly string input;
        private int position = -1;
        private readonly int length;

        private Parser(string json, bool relaxed = false) {
            input = json;
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
            try {
                parser.NextToken(); // bring us to #0 + any whitespace
                var result = parser.readValue();
                parser.NextToken(true); // bring us hopefully to end of input
                if (parser.position < parser.length)
                    throw new ParseError($"Unexpected '{parser.current}'", parser.position);
                return result;
            } catch (ParseError e) {
                // consolidate ParseError path chains
                List<string> location = new List<string>(2);
                string path = e.FullJsonPath;
                if (path != "") location.Add(path);
                if (e.JsonPosition > -1) location.Add(parser.describePosition(e.JsonPosition));
                string suffix = location.Count > 0 ? $" at {string.Join(", ", location)}" : "";
                var baseParseError = e;
                while (baseParseError.InnerException is ParseError inner) baseParseError = inner;
                throw new ParseError($"{baseParseError.Message}{suffix}", baseParseError.InnerException);
            }
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

                if (position == length) {
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
        /// Returns a human-readable reference to an input position for use in error messages.
        /// If input contains multiple lines the position's line number is included in the description.
        /// </summary>
        /// <param name="pos">A position in the JSON input</param>
        /// <returns>E.g. "input position 61 (line 4)"</returns>
        private string describePosition(int pos) {
            string s = $"input position {pos.ToString()}";
            int lineCount = crLfRegex.Matches(input.Substring(0, pos)).Count;
            // needn't scan for a cr/lf if we've already found some
            if (lineCount > 0 || crLfRegex.IsMatch(input, pos)) s += $" (line {lineCount + 1})";
            return s;
        }

        /// <summary>
        /// Reads a value from input and places the read position at the end.
        /// </summary>
        /// <returns>The value read from input</returns>
        /// <exception cref="ParseError"></exception>
        private JsValue readValue() {
            char c = current;
            switch (c) {
                case '"': return readString();
                case '[': return readArray();
                case '{': return readObject();
            }

            if (numberMatch(true)) return readNumber();

            if (length - position > 3 && (c == 't' || c == 'f' || c == 'n')) {
                string nextFour = input[position .. (position + 4)];
                switch (nextFour) {
                    case "null":
                        position += 3;
                        return JsValue.Null;
                    case "true":
                        position += 3;
                        return true;
                    case "fals" when length - position > 4 && input[position + 4] == 'e':
                        position += 4;
                        return false;
                    default:
                        var key = readKeyRelaxed();
                        position -= key.Length - 1;
                        throw new ParseError($"Unexpected '{shortenKey(key)}'", position);
                }
            }

            throw new ParseError($"Unexpected '{current}'", position);
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
                throw new ParseError( $"Failed to parse number", e, position + 1);
            }
        }

        private bool pointOrExpFound = false;

        /// <summary>
        /// Ascertains whether the the current character is numeric. '.' and 'e' are considered numeric on first encounter after setting pointOrExpFound = false.
        /// Sets pointOrExpFound to true if the character is '.' or 'e'.
        /// </summary>
        /// <param name="firstCharacter">Defines whether to accept "-" as numeric, i.e. if we are expecting the first character of the numeric value</param>
        /// <param name="offset"></param>
        /// <returns>True if the symbol at current position is numeric</returns>
        private bool numberMatch(bool firstCharacter, int offset = 0) {
            switch (input[position + offset]) {
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                    return true;
                case '-': 
                    return firstCharacter;
                case '.':
                case 'e' when !firstCharacter:
                    if(pointOrExpFound) return false;
                    pointOrExpFound = true;
                    return true;
                default: return false;
            }
        }

        private bool isHexadecimalDigit(int offset) {
            byte c = (byte) input[position + offset];
            return (c > 47 && c < 58) // 0-9
                   || (c > 64 && c < 71) // A-F
                   || (c > 96 && c < 103) // a-f
                ;
        }

        /// <summary>
        /// Reads the next four characters as a hexadecimal symbol reference and places the read position at the end.
        /// </summary>
        /// <returns>String containing the referenced character</returns>
        /// <exception cref="ParseError"></exception>
        private string readEscapedCodepoint() {
            position += 4;
            if(position >= length) throw new ParseError("Past end of input");
            if(!isHexadecimalDigit(0) || !isHexadecimalDigit(-1) || !isHexadecimalDigit(-2) || !isHexadecimalDigit(-3))
                throw new ParseError($"Malformed \\u sequence", position - 4);
            try {
                return char.ConvertFromUtf32(
                    int.Parse(input[(position - 3) .. (position + 1)], System.Globalization.NumberStyles.HexNumber));
            }
            catch (ArgumentOutOfRangeException e) {
                throw new ParseError($"Failed to parse \\u sequence at {describePosition(position - 4)}", e);
            }
        }
        
        private static readonly Regex unquotedKeyRegex = new Regex("\\G\\w+");

        private string readKeyRelaxed() {
            var match = unquotedKeyRegex.Match(input, position);
            if (match.Success) {
                position += match.Value.Length - 1;
                return match.Value;
            }
            throw new ParseError($"Unexpected '{current}'", position);
        }

        /// <summary>
        /// Reads a string value from input and places the read position at the closing '"'. Assumes the current character is '"'.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ParseError"></exception>
        private JsValue readString() {
            var startPosition = position;
            position++; // skip first "

            var sb = new StringBuilder();
            bool escaping = false;
            int literalLength = 0;
            if(position == length) throw new ParseError($"Unclosed string", startPosition);
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
                            default: throw new ParseError($"Unknown escape character '{current}'", position);
                        }
                    } catch (ParseError e) {
                        throw;
                    } catch (Exception e) {
                        throw new ParseError($"Failed to parse string", e, startPosition);
                    }
                } else if(current == '\\'){
                    escaping = true;
                    if (literalLength > 0) {
                        sb.Append(input, position - literalLength, literalLength);
                        literalLength = 0;
                    }
                } else literalLength++;
                position++;
                
                if(position == length) throw new ParseError($"Unclosed string", startPosition);
            }

            if(literalLength > 0) sb.Append(input, position - literalLength, literalLength);
            return sb.ToString();
        }
        
        /// <summary>
        /// Reads an array from input and places the read position at the closing ']'. Assumes the current character is '['
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ParseError"></exception>
        private JsValue readArray(){
            List<JsValue> found = new List<JsValue>();
            
            try {
                while (true) {
                    // current is either [ or ,
                    NextToken(); 
                    if (current == ']') return found.ToArray();

                    var value = readValue();
                    NextToken(); 
                    
                    switch (current) {
                        case ']':
                            found.Add(value);
                            return found.ToArray();
                        case ',':
                            found.Add(value);
                            continue;
                        default: throw new ParseError($"Expected ',' or ']', found '{current}'", position - 1);
                    }
                }
            } catch (ParseError e) {
                throw new ParseError(e, $"[{found.Count}]");
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
        private Dictionary<string, JsValue> readObject() {
            var startPosition = position;
            var result = new Dictionary<string, JsValue>();

            while (true) {
                string keyValue;
                try {
                    NextToken(); // move to key or }
                    switch (current) {
                        case '}': return result;
                        case '"':
                            keyValue = readString();
                            break;
                        default: 
                            if(!relaxed) throw new ParseError(
                            $"Expected '\"', found '{current}'", position);
                            keyValue = readKeyRelaxed();
                            break;
                    }
                } catch (ParseError e) {
                    throw new ParseError(e, $"{{key {result.Count}}}");
                }

                try {
                    NextToken(); // move to :
                    if (current != ':')
                        throw new ParseError(
                            $"Expected ':' following property name, found '{current}'", position);

                    NextToken(); // move to value

                    JsValue value;
                    value = readValue();

                    result[keyValue] = value;
                    NextToken(); // move to , or }

                switch (current) {
                    case '}': return result;
                    case ',': continue;
                    default:
                        throw new ParseError(
                            $"Expected ',' or '}}' following value, found '{current}'", position);
                    }
                } catch (ParseError e) { // surely EOT
                    throw new ParseError(e, $"{{{shortenKey(keyValue).ToJson()}}}");
                }
            }
        }

    }
}