using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Lantern.Face.JSON {
	public enum JSType {
		Boolean,
		Number,
		String,
		Object,
		Array,
		Null,
	}

	public class JSValue {
		public readonly JSType DataType;
		public bool BooleanValue;
		public readonly double NumberValue;
		public readonly string StringValue;
		public readonly ReadOnlyDictionary<string, JSValue> ObjectProperties;
		public readonly JSValue[] ArrayValue;

		public JSValue(string s) {
			DataType = JSType.String;
			StringValue = s;
		}
		public JSValue(double n) {
			DataType = JSType.Number;
			NumberValue = n;
		}
		public JSValue(int n) {
			DataType = JSType.Number;
			NumberValue = Convert.ToDouble(n);
		}
		public JSValue(bool b) {
			DataType = JSType.Boolean;
			BooleanValue = b;
		}

		public JSValue(JSValue[] array) {
			DataType = JSType.Array;
			ArrayValue = array;
		}

		private JSValue(JsNull nul) {
			DataType = JSType.Null;
		}

		public JSValue(Dictionary<string, JSValue> properties) {
			DataType = JSType.Object;
			ObjectProperties = new ReadOnlyDictionary<string, JSValue>(properties);
		}


		private class JsNull { }
		public static readonly JSValue Null = new JSValue(new JsNull());


		// type >> JS
		public static implicit operator JSValue(string s) => new JSValue(s);
		public static implicit operator JSValue(bool b) => new JSValue(b);
		public static implicit operator JSValue(int n) => new JSValue(n);
		public static implicit operator JSValue(double n) => new JSValue(n);
		public static implicit operator JSValue(Dictionary<string, JSValue> properties) => new JSValue(properties);
		public static implicit operator JSValue(JSValue[] arr) {
			if (arr == null) return JSValue.Null;
			return new JSValue(arr);
		}

		// JS >> type
		public static implicit operator string(JSValue j) {
			if (j.DataType != JSType.String) throw new InvalidCastException("Incorrect data type: trying to read a " + j.DataType.ToString() + " as string");
			return j.StringValue;
		}
		public static implicit operator double(JSValue j) {
			if (j.DataType != JSType.Number) throw new InvalidCastException("Incorrect data type: trying to read a " + j.DataType.ToString() + " as double");
			return j.NumberValue;
		}
		public static implicit operator bool(JSValue j) {
			if (j.DataType != JSType.Boolean) throw new InvalidCastException("Incorrect data type: trying to read a " + j.DataType.ToString() + " as bool");
			return j.BooleanValue;
		}
		public static implicit operator JSValue[](JSValue j) {
			if (j.DataType != JSType.Array) throw new InvalidCastException("Incorrect data type: trying to read a " + j.DataType.ToString() + " as array");
			return j.ArrayValue;
		}
		public static implicit operator ReadOnlyDictionary<string, JSValue>(JSValue j) {
			if (j.DataType != JSType.Object) throw new InvalidCastException("Incorrect data type: trying to read a " + j.DataType.ToString() + " as object");
			return j.ObjectProperties;
		}


		public static JSValue ParseJSON(string s) => Parser.Parse(s);

		private static string EscapeString(string s) {
			return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}

		public JSValue this[string property] {
			get {
				if (DataType != JSType.Object) throw new ArgumentException("Trying to access property of a " + DataType.ToString() + " value");
				return ObjectProperties[property];
			}
		}
		public JSValue this[int index] {
			get {
				if (DataType != JSType.Array) throw new ArgumentException("Trying to access index of a " + DataType.ToString() + " value");
				return ArrayValue[index];
			}
		}

		public JSValue[] ToArray() {
			if (DataType != JSType.Array) throw new ArgumentException("Trying create array from a " + DataType.ToString() + " value");
			return ArrayValue;
		}

		public string ToJSON(int maxDepth = 128) {
			if (maxDepth < 0) throw new ArgumentOutOfRangeException("Maximum depth exceeded");
			switch (DataType) {
				case JSType.Boolean: return BooleanValue ? "true" : "false";
				case JSType.Number: return NumberValue.ToString();
				case JSType.String: return $"\"{EscapeString(StringValue)}\"";
				case JSType.Null: return "null";
				case JSType.Object:
					var sb = new StringBuilder();
					sb.Append("{");

					string[] colonicPairings = ObjectProperties.Keys.Select<string, string>(key => {
						var value = ObjectProperties[key];
						var sb = new StringBuilder();
						sb.Append($"\"{EscapeString(key)}\": ");
						sb.Append(value.ToJSON(maxDepth - 1));
						return sb.ToString();
					}).ToArray();

					sb.Append(string.Join(", ", colonicPairings));

					sb.Append("}");
					return sb.ToString();
				case JSType.Array: return "[" + String.Join(", ", ArrayValue.Select(val => val == null ? "null" : val.ToJSON(maxDepth - 1))) + "]";
			}
			return "";
		}
	}

	public class ParseError : Exception {
		public ParseError(string reason) : base(reason) { }
	}
	internal class Parser {
		delegate bool ReadingState(Parser parser);
		private string input;
		private Parser(string s) => input = s;
		public static JSValue Parse(string s){
			var parser = new Parser(s);
			parser.NextToken();
			var result = parser.ReadValue();
			parser.NextToken(true);
			return result;
		}

		private string unescape(string s){
			return s.Replace("\\", "/");
		}

		private static char[] whiteSpaceChars = new char[] { '\r', '\n', '\t', ' ' };

		private int position = -1;
		private void NextToken(bool expectEOT = false){
			position++;
			if (!expectEOT && position >= input.Length) throw new ParseError("Past end of input");
			while(position < input.Length && whiteSpaceChars.Contains(current)){
				position++;
				if(!expectEOT && position >= input.Length) throw new ParseError("Past end of input");
			}
			if(expectEOT && position < input.Length) throw new ParseError("Unexpected " + current + " at " + position.ToString());
		}
		private JSValue ReadValue(){
			if(input[position] == '"') return readString();
			if(input[position] == '[') return readArray();
			if(input[position] == '{') return readObject();

			var numberMatch = new Regex("^\\d*(\\.?\\d+)+").Match(input.Substring(position));
			if(numberMatch.Success){
				string cap = numberMatch.Captures[0].Value;
				double num = Convert.ToDouble(cap);
				position += numberMatch.Captures[0].Length - 1;
				return num;
			}

			if (input.Substring(position, 4) == "null") {
				position += 3;
				return JSValue.Null;
			}
			if (input.Substring(position, 4) == "true") {
				position += 3;
				return true;
			}
			if (input.Substring(position, 5) == "false") {
				position += 4;
				return false;
			}
			throw new ParseError("Unexpected " + current + " at " + position.ToString());
		}

		private string readString(){
			var startPosition = position;
			bool escaping = false;
			position++; // skip first "
			var sb = new StringBuilder();
			while(escaping || current != '"'){
				if(position >= input.Length) throw new ParseError("Unclosed string at " + startPosition.ToString());
				if(escaping){
					escaping = false;
					if(current == 'n'){ sb.Append("\n"); }
					else if (current == 'r') { sb.Append("\r"); }
					else if (current == 't') { sb.Append("\t"); }
					else sb.Append(current);
					position++;
					continue;
				}
				if(current == '\\'){
					escaping = true;
				} else sb.Append(current);
				position++;
			}
			return sb.ToString();
		}

		private char current => input[position];

		private JSValue[] readArray(){
			var startPosition = position;
			List<JSValue> found = new List<JSValue>();
			while(true){
				NextToken();
				if (current == ']') {
					return found.ToArray();
				}
				found.Add(ReadValue());

				NextToken();
				if (current == ']') {
					return found.ToArray();
				}
				if(current != ',') throw new ParseError("Expected , at " + position.ToString());
			}
		}

		private JSValue readObject(){
			var startPosition = position;
			var result = new Dictionary<string, JSValue>();
			while(true){
				NextToken();
				if(current == '}') return result;
				var keyValue = ReadValue();
				if(keyValue.DataType != JSType.String) throw new ParseError("Expected property index at " + position.ToString());
				NextToken();
				if(current != ':') throw new ParseError("Expected : at " + position.ToString());
				NextToken();
				var value = ReadValue();
				result[keyValue.StringValue] = value;
				NextToken();
				if (current == '}') return result;
				if (current != ',') throw new ParseError("Expected , or } at " + position);
			}
		}

	}

}