using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lantern.Face.Json {
    
    public class ParseError : Exception {
        public readonly int JsonPosition;

        public ParseError(string reason, int position = -1) : base(reason) {
            JsonPosition = position;
        }
        public ParseError(string reason, Exception inner, int position = -1) : base(reason, inner) {
            JsonPosition = position;
        }

        //public ParseError(string reason, Exception inner) : base(reason, inner) { }
        private readonly string jsonPath = "";
        
        public ParseError(Exception innerException, JsValue jsonPath) : base($"Error parsing `{getJsonPath(innerException, jsonPath).TrimStart('.')}`", innerException) {
            this.jsonPath = jsonPath;
            if (innerException is ParseError innerParseError) JsonPosition = innerParseError.JsonPosition;
        }

        private static string getJsonPath(Exception innerException, JsValue path) {
            if (!(innerException is ParseError inner)) return path ?? ""; 
            return (path ?? "") + inner.fullJsonPath;
        }

        private string fullJsonPath => getJsonPath(InnerException, jsonPath);

        /// <summary>
        /// Reduces a chain of ParseErrors into a single ParseError, assembling the
        /// JSON path from the chain.
        /// </summary>
        /// <param name="parser"></param>
        /// <returns></returns>
        internal ParseError Consolidate(Parser parser) {
            var location = new List<string>(2);
            string path = fullJsonPath;
            if (path.Length > 0 && path[0] == '.') path = path.Substring(1);
            if (path != "") location.Add($"`{path}`");
            if (JsonPosition > -1) location.Add(parser.describePosition(JsonPosition));
            string suffix = location.Count > 0 ? $" at {string.Join(", ", location)}" : "";
            var baseParseError = this;
            while (baseParseError.InnerException is ParseError inner) baseParseError = inner;
            return new ParseError($"{baseParseError.Message}{suffix}", baseParseError.InnerException);
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
                throw e.Consolidate(parser);
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
                while (c == ' ' || c == '\r' || c == '\t' || c == '\n') {
                    position++;
                    if (position == length) {
                        if (!expectEot)
                            throw new ParseError("Past end of input");
                        return; // expected eot, found eot
                    }
                    c = current;
                }

                if (relaxed && position < length - 1 && c == '/' && input[position + 1] == '/') { // skip comment if relaxed
                    var nextLine = crLfRegex.Match(input, position + 2);
                    if (nextLine.Success) {
                        position = nextLine.Index + nextLine.Length - 2;
                        continue;
                    } // already on the last line; eot
                    if (!expectEot)
                        throw new ParseError("Past end of input");
                    position = length;
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
        internal string describePosition(int pos) {
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

            if (isNumeric(c)) return readNumber();

            try {
                string nextFour = input[position .. (Math.Min(position + 4, length))];
                switch (nextFour) {
                    case "null":
                        position += 3;
                        return JsValue.Null;
                    case "true":
                        position += 3;
                        return true;
                    case "fals" when input[position + 4] == 'e':
                        position += 4;
                        return false;
                    default:
                        var key = readKeyRelaxed();
                        throw new ParseError($"Expected value, found '{shortenKey(key)}'", position - key.Length - 1);
                }
            } catch (IndexOutOfRangeException) {
                throw new ParseError("Expected value, past end of input");
            } catch (ArgumentOutOfRangeException) {
                throw new ParseError("Expected value, past end of input");
            }
        }

        private double readNumber() {
            int lengthFound = 1; // we already know (from readValue()) that current is numeric
            while (position < length - 1 && isNumeric(input[position + 1])) {
                position++;
                lengthFound++;
            }

            string numberString = input[(position + 1 - lengthFound) .. (position + 1)];
            try {
                return double.Parse(numberString);
            } catch (Exception e) {
                throw new ParseError( "Failed to parse number", e, position + 1);
            }
        }
        
        /// <summary>
        /// Ascertains whether a character is numeric. '.', 'e', '+' and '-' are considered numeric.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns>True if the symbol at current position is numeric</returns>
        private static bool isNumeric(char ch) =>
            ch switch {
                '0' => true, '1' => true, '2' => true, '3' => true, '4' => true,
                '5' => true, '6' => true, '7' => true, '8' => true, '9' => true,
                '-' => true, '+' => true, '.' => true, 'e' => true, 'E' => true,
                _ => false
            };

        /// <summary>
        /// Reads the next four characters as a hexadecimal symbol reference and places the read position at the end.
        /// Attempts to read a following "\uxxxx" sequence for surrogate pairs
        /// </summary>
        /// <returns>String containing the referenced character</returns>
        /// <exception cref="ParseError"></exception>
        private string readEscapedCodepointChar() {
            int codepoint = readEscapedCodepoint();
            if (codepoint >= highSurrogateStart && codepoint <= 57343) {
                try {
                    if (input[position + 1] != '\\' || input[position + 2] != 'u') {
                        throw new ParseError($"Expected low surrogate, found '{current}'", position);
                    }
                } catch (ArgumentOutOfRangeException) {
                    throw new ParseError("Expected low surrogate \\u sequence, past end of input", position);
                }

                position += 2; // skip \u
                var high = codepoint - highSurrogateStart;
                var low = readEscapedCodepoint() - lowSurrogateStart;
                codepoint = high * 1024 + low + 65536;
                try {
                    return char.ConvertFromUtf32(codepoint);
                } catch (ArgumentOutOfRangeException) {
                    // having successfully called readEscapedCodepoint() (twice) we can be sure these 12 chars exist and are \uxxxx\uxxxx
                    var pair = input.Substring(position - 11, 12);
                    throw new ParseError($"Invalid surrogate pair '{pair}'", position - 3);
                }
            }
            return ((char)codepoint).ToString();
        }

        private const ushort highSurrogateStart = 55296;
        private const ushort lowSurrogateStart = 56320;

        /// <summary>
        /// Reads the next four digits as a hexadecimal value and moves the read position by four places
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ParseError"></exception>
        private ushort readEscapedCodepoint() {
            position += 4;
            try {
                var sequence = input[(position - 3) .. (position + 1)];
                return ushort.Parse(sequence, System.Globalization.NumberStyles.HexNumber);
            } catch (ArgumentOutOfRangeException){
                throw new ParseError("Past end of input while reading \\u sequence", position - 3);
            } catch (FormatException) {
                position -= 3;
                var capture = new Regex(".\\w*").Match(input, position, Math.Min(4, length - position));
                throw new ParseError($"Invalid \\u sequence '{capture}'", position);
            }
        }
        
        private static readonly Regex unquotedKeyRegex = new Regex("\\G\\w+");

        private string readKeyRelaxed() {
            var match = unquotedKeyRegex.Match(input, position);
            if (!match.Success)
                throw new ParseError($"Unexpected '{current}'", position);
            position += match.Value.Length - 1;
            return match.Value;
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
            if(position == length) throw new ParseError("Unclosed string", startPosition);
            while(escaping || current != '"'){
                if(escaping){
                    escaping = false;
                    try {
                        sb.Append(current switch {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '"' => '"',
                            'u' => readEscapedCodepointChar(),
                            '\\' => '\\',
                            '/' => '/',
                            'b' => '\x08',
                            'f' => '\x0c',
                            _ => throw new ParseError($"Unknown escape character '{current}'", position)
                        });
                    } catch (ParseError) {
                        throw;
                    } catch (Exception e) {
                        // may be able to remove this catch (therefore the try) without effect;
                        throw new ParseError("Failed to parse string", e, startPosition);
                    }
                } else if(current == '\\'){
                    escaping = true;
                    if (literalLength > 0) {
                        sb.Append(input, position - literalLength, literalLength);
                        literalLength = 0;
                    }
                } else literalLength++;
                position++;
                
                if(position == length) throw new ParseError("Unclosed string", startPosition);
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
            var found = new List<JsValue>();
            
            try {
                while (true) {
                    // current is either [ (first loop) or , (subsequent)
                    NextToken(); // move to ] or value 
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
                
                // key is now known; future ParseErrors can include it in path

                try {
                    NextToken(); // move to :
                    if (current != ':')
                        throw new ParseError(
                            $"Expected ':' following property name, found '{current}'", position);

                    NextToken(); // move to value

                    var value = readValue();
                    result[keyValue] = value;

                    NextToken(); // move to , or }

                switch (current) {
                    case '}': return result;
                    case ',': continue;
                    default:
                        throw new ParseError(
                            $"Expected ',' or '}}' following value, found '{current}'", position);
                    }
                } catch (Exception e) { // surely EOT
                    throw new ParseError(e, FormatObjectPathComponent(keyValue));
                }
            }
        }

        private static readonly Regex nonWord = new Regex("\\W");

        private static string FormatObjectPathComponent(string s) {
            s = shortenKey(s);
            return nonWord.IsMatch(s) ? $"{{{shortenKey(s).ToJson()}}}" : $".{s}";
        }
    }
}