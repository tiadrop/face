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

        private readonly string jsonPathComponent = "";
        
        public ParseError(Exception innerException, string jsonPathComponent) : base($"Error parsing `{getJsonPath(innerException, jsonPathComponent).TrimStart('.')}`", innerException) {
            this.jsonPathComponent = jsonPathComponent;
            if (innerException is ParseError innerParseError) JsonPosition = innerParseError.JsonPosition;
        }

        private static string getJsonPath(Exception innerException, string path) {
            if (!(innerException is ParseError inner)) return path; 
            return path + inner.FullJsonPath;
        }

        private string FullJsonPath => getJsonPath(InnerException, jsonPathComponent);

        /// <summary>
        /// Reduces a chain of ParseErrors into a single ParseError, assembling the
        /// JSON path from the chain.
        /// </summary>
        /// <param name="input">The full input string</param>
        /// <returns></returns>
        internal ParseError Consolidate(string input) {
            var location = new List<string>(2);
            // prepare json path from this and inner ParseErrors
            string path = FullJsonPath;
            if (path.Length > 0 && path[0] == '.') path = path.Substring(1);
            if (path != "") location.Add($"`{path}`");
            // prepare location suffix from this ParseError's position
            if (JsonPosition > -1) location.Add(describePosition(JsonPosition, input));
            // join up all the location info we have
            string suffix = location.Count > 0 ? $" at {string.Join(", ", location)}" : "";
            var baseParseError = this;
            // find the deepest ParseError in the chain 
            while (baseParseError.InnerException is ParseError inner) baseParseError = inner;
            // use its message and inner with this ParseError's json path and location
            return new ParseError($"{baseParseError.Message}{suffix}", baseParseError.InnerException);
        }
        
        /// <summary>
        /// Returns a human-readable reference to an input position for use in error messages.
        /// If input contains multiple lines the position's line number is included in the description.
        /// </summary>
        /// <param name="pos">A position in the JSON input</param>
        /// <param name="input"></param>
        /// <returns>E.g. "input position 61 (line 4)"</returns>
        private static string describePosition(int pos, string input) {
            string s = $"input position {pos.ToString()}";
            int lineCount = Parser.CrLfRegex.Matches(input.Substring(0, pos)).Count;
            // needn't scan for a cr/lf if we've already found some
            if (lineCount > 0 || Parser.CrLfRegex.IsMatch(input, pos)) s += $" (line {lineCount + 1})";
            return s;
        }
       
    }
    
    
    internal class Parser {
        private readonly bool relaxed;
        private readonly string input;
        private int position = -1;
        private readonly int length;

        private const int defaultMaxDepth = 128;

        private Parser(string json, bool relaxed = false) {
            input = json;
            length = json.Length;
            this.relaxed = relaxed;
        }
        
        internal static readonly Regex CrLfRegex = new Regex("\r\n|\r|\n");

        /// <summary>
        /// Produces a JsValue object from a JSON-formatted string
        /// </summary>
        /// <param name="json">JSON-formatted string</param>
        /// <param name="relaxed">True to allow // comments and unquoted property names</param>
        /// <param name="maxDepth"></param>
        /// <returns>Object representing the structure defined in JSON</returns>
        internal static JsValue Parse(string json, bool relaxed = false, int maxDepth = defaultMaxDepth){
            var parser = new Parser(json, relaxed);
            try {
                parser.NextToken(); // bring us to #0 + any whitespace
                var result = parser.readValue(maxDepth);
                parser.NextToken(true); // bring us hopefully to end of input
                if (parser.position < parser.length)
                    throw new ParseError($"Unexpected '{parser.getSymbol()}'", parser.position);
                return result;
            } catch (ParseError e) {
                throw e.Consolidate(json);
            }
        }

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

                char c = input[position];
                
                // skip whitespace
                while (c == ' ' || c == '\r' || c == '\t' || c == '\n') {
                    position++;
                    if (position == length) {
                        if (!expectEot)
                            throw new ParseError("Past end of input");
                        return; // expected eot, found eot
                    }
                    c = input[position];
                }

                if (relaxed && position < length - 1 && c == '/' && input[position + 1] == '/') { // skip comment if relaxed
                    var nextLine = CrLfRegex.Match(input, position + 2);
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

        static readonly Regex symbolRegex = new Regex("\\G\\w{1,17}"); 
        private string getSymbol() {
            var match = symbolRegex.Match(input, position);
            if (!match.Success) return input[position].ToString();
            var symbol = symbolRegex.Match(input, position).Value;
            if (symbol.Length > 16) symbol = symbol.Substring(0, 16) + "...";
            return symbol;
        }

        /// <summary>
        /// Reads a value from input and places the read position at the end.
        /// </summary>
        /// <returns>The value read from input</returns>
        /// <exception cref="ParseError"></exception>
        private JsValue readValue(int maxDepth = defaultMaxDepth) {
            if(maxDepth < 0) throw new ParseError("Maximum object depth exceeded", position);
            char c = input[position];
            switch (c) {
                case '"': return readString();
                case '[': return readArray(maxDepth - 1);
                case '{': return readObject(maxDepth - 1);
            }

            if (isNumeric(c)) return readNumber();

            string nextThree = input[(position + 1) .. (Math.Min(position + 4, length))];
            switch (nextThree) {
                case "ull" when c == 'n':
                    if (length > position + 4 && char.IsLetterOrDigit(input[position + 4])) break;
                    position += 3;
                    return JsValue.Null;
                case "rue" when c == 't':
                    if (length > position + 4 && char.IsLetterOrDigit(input[position + 4])) break;
                    position += 3;
                    return true;
                case "als" when c == 'f' && position < length - 4 && input[position + 4] == 'e':
                    if (length > position + 5 && char.IsLetterOrDigit(input[position + 5])) break;
                    position += 4;
                    return false;
            }
            throw new ParseError($"Expected value, found '{getSymbol()}'", position);
        }

        private double readNumber() {
            int start = position;
            while (position < length - 1 && isNumeric(input[position + 1])) {
                position++;
            }

            string numberString = input[start .. (position + 1)];
            try {
                return double.Parse(numberString);
            } catch (Exception e) {
                throw new ParseError( "Failed to parse number", e, start);
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
                if (position > length - 3) {
                    throw new ParseError("Expected low surrogate \\u sequence, past end of input");
                }
                if (input[position + 1] != '\\' || input[position + 2] != 'u') {
                    throw new ParseError($"Expected low surrogate, found '{getSymbol()}'", position);
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
                Regex fourAlphaNum = new Regex(@"\w{1,4}|.");
                throw new ParseError($"Invalid \\u sequence '{fourAlphaNum.Match(input, position).Value}'", position);
            }
        }
        
        private static readonly Regex unquotedKeyRegex = new Regex("\\G\\w+");

        private string readKeyRelaxed() {
            var match = unquotedKeyRegex.Match(input, position);
            if (!match.Success)
                throw new ParseError($"Unexpected '{getSymbol()}'", position);
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
            //if(position == length) throw new ParseError("Unclosed string", startPosition);

            
            var sb = new StringBuilder();
            bool escaping = false;
            int literalLength = 0;
            try {
                var c = input[position];
                while (escaping || c != '"') {
                    if (escaping) {
                        escaping = false;
                        sb.Append(c switch {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '"' => '"',
                            'u' => readEscapedCodepointChar(),
                            '\\' => '\\',
                            '/' => '/',
                            'b' => '\x08',
                            'f' => '\x0c',
                            _ => throw new ParseError($"Unknown escape character '{c}'", position)
                        });
                    
                    } else if (c == '\\') {
                        escaping = true;
                        if (literalLength > 0) {
                            sb.Append(input, position - literalLength, literalLength);
                            literalLength = 0;
                        }
                    } else literalLength++;

                    position++;

                    //if (position == length) throw new ParseError("Unclosed string", startPosition);
                    c = input[position];
                }
            } catch (IndexOutOfRangeException) {
                throw new ParseError("Unclosed string", startPosition);
            }

            if(literalLength > 0) sb.Append(input, position - literalLength, literalLength);
            return sb.ToString();
        }
        
        /// <summary>
        /// Reads an array from input and places the read position at the closing ']'. Assumes the current character is '['
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ParseError"></exception>
        private JsValue readArray(int maxDepth){
            var found = new List<JsValue>();
            
            try {
                while (true) {
                    // current is either [ (first loop) or , (subsequent)
                    NextToken(); // move to ] or value 
                    if (input[position] == ']') return found.ToArray();

                    var value = readValue(maxDepth);
                    NextToken(); 
                    
                    switch (input[position]) {
                        case ']':
                            found.Add(value);
                            return found.ToArray();
                        case ',':
                            found.Add(value);
                            continue;
                        default: throw new ParseError($"Expected ',' or ']', found '{getSymbol()}'", position);
                    }
                }
            } catch (ParseError e) {
                throw new ParseError(e, $"[{found.Count}]");
            }
        }
        
        /// <summary>
        /// Shortens a string to a maximum length for inclusion in exception messag`es.
        /// </summary>
        /// <param name="k"></param>
        /// <returns>Shortened key</returns>
        private static string shortenKey(string k) => k.Length <= 16 ? k : (k.Substring(0, 13) + "..."); 
        
        /// <summary>
        /// Reads an object from input and places the cursor at the closing '}'. Assumes the current character is '{'.
        /// </summary>
        /// <returns>A JsValue object with a DataType of Object</returns>
        /// <exception cref="ParseError"></exception>
        private Dictionary<string, JsValue> readObject(int maxDepth) {
            var result = new Dictionary<string, JsValue>();

            while (true) {
                string keyValue;
                try {
                    NextToken(); // move to key or }
                    switch (input[position]) {
                        case '}': return result;
                        case '"':
                            keyValue = readString();
                            break;
                        default: 
                            if(!relaxed) throw new ParseError(
                            $"Expected '\"', found '{getSymbol()}'", position);
                            keyValue = readKeyRelaxed();
                            break;
                    }
                } catch (ParseError e) {
                    throw new ParseError(e, $"{{key {result.Count}}}");
                }
                
                // key is now known; future ParseErrors can include it in path

                try {
                    NextToken(); // move to :
                    if (input[position] != ':')
                        throw new ParseError(
                            $"Expected ':' following property name, found '{getSymbol()}'", position);

                    NextToken(); // move to value

                    var value = readValue(maxDepth);
                    result[keyValue] = value;

                    NextToken(); // move to , or }

                switch (input[position]) {
                    case '}': return result;
                    case ',': continue;
                    default:
                        throw new ParseError(
                            $"Expected ',' or '}}' following value, found '{getSymbol()}'", position);
                    }
                } catch (Exception e) { // surely EOT
                    throw new ParseError(e, FormatObjectPathComponent(keyValue));
                }
            }
        }

        private static readonly Regex nonWord = new Regex("\\W");

        private static string FormatObjectPathComponent(string s) {
            if (s == "") return "{\"\"}";
            s = shortenKey(s);
            return nonWord.IsMatch(s) ? $"{{{shortenKey(s).ToJson()}}}" : $".{s}";
        }
    }
}