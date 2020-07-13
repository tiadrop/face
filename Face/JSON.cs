using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lantern.Face.JSON {

	public static class Extensions {
		// primitives
		public static string ToJSON(this string s) => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
		public static string ToJSON(this int v) => v.ToString();
		public static string ToJSON(this double v) => v.ToString();
		public static string ToJSON(this bool v) => v ? "true" : "false";

		public static string ToJSON(this IJSONEncodable obj) => obj.ToJSValue().ToJSON();

		// base collections
		public static string ToJSON(this JSValue[] list, int maxDepth = JSValue.DefaultMaxDepth)
			=> "[" + String.Join(",", list.Select(val => val == null ? "null" : val.ToJSON(maxDepth - 1))) + "]";
		public static string ToJSON(this IDictionary<string, JSValue> dict, int maxDepth = JSValue.DefaultMaxDepth){
			var sb = new StringBuilder();
			sb.Append("{");
			string[] colonicPairings = dict.Keys.Select<string, string>(key => {
				var value = dict[key];
				var sb = new StringBuilder();
				sb.Append(key.ToJSON());
				sb.Append(":");
				sb.Append(value.ToJSON(maxDepth - 1));
				return sb.ToString();
			}).ToArray();

			sb.Append(string.Join(",", colonicPairings));

			sb.Append("}");
			return sb.ToString();
		}

		// compatible Dictionaries
		public static string ToJSON(this IDictionary<string, IJSONEncodable> dict)
			=> new Dictionary<string, JSValue>(dict.Select(kv => 
				new KeyValuePair<string, JSValue>(kv.Key, kv.Value.ToJSValue())
			)).ToJSON();
		public static string ToJSON(this IDictionary<string, string> dict)
			=> new Dictionary<string, JSValue>(dict.Select(kv =>
				new KeyValuePair<string, JSValue>(kv.Key, kv.Value)
			)).ToJSON();
		public static string ToJSON(this IDictionary<string, double> dict)
			=> new Dictionary<string, JSValue>(dict.Select(kv =>
				new KeyValuePair<string, JSValue>(kv.Key, kv.Value)
			)).ToJSON();
		public static string ToJSON(this IDictionary<string, int> dict)
			=> new Dictionary<string, JSValue>(dict.Select(kv =>
				new KeyValuePair<string, JSValue>(kv.Key, kv.Value)
			)).ToJSON();
		public static string ToJSON(this IDictionary<string, bool> dict)
			=> new Dictionary<string, JSValue>(dict.Select(kv =>
				new KeyValuePair<string, JSValue>(kv.Key, kv.Value)
			)).ToJSON();

		// compatible arrays
		public static string ToJSON(this IJSONEncodable[] list) => list.Select(v => v.ToJSValue()).ToArray().ToJSON();
		public static string ToJSON(this string[] list) => list.Select(v => new JSValue(v)).ToArray().ToJSON();
		public static string ToJSON(this int[] list) => list.Select(v => new JSValue(v)).ToArray().ToJSON();
		public static string ToJSON(this bool[] list) => list.Select(v => new JSValue(v)).ToArray().ToJSON();
		public static string ToJSON(this double[] list) => list.Select(v => new JSValue(v)).ToArray().ToJSON();

	}

	public interface IJSONEncodable {
		/// <summary>
		/// Express the object as a JSValue to enable implicit and explicit JSON encoding
		/// </summary>
		/// <returns>A JSValue representing this object</returns>
		JSValue ToJSValue();
	}

	public enum JSType {
		Boolean,
		Number,
		String,
		Object,
		Array,
		Null,
	}

	public class JSValue {
		public const int DefaultMaxDepth = 32;
		public readonly JSType DataType;
		private bool _booleanValue;
		private readonly double _numberValue;
		private readonly string _stringValue;
		public readonly ReadOnlyDictionary<string, JSValue> _ObjectValue;
		public readonly JSValue[] _arrayValue;

		public string StringValue {
			get {
				switch (DataType) {
					case JSType.String: return _stringValue;
					case JSType.Number: return _numberValue.ToString();
					case JSType.Boolean: return _booleanValue ? "True" : "False";
				}
				throw new InvalidCastException("Can't read " + DataType.ToString() + " as string");
			}
		}
		public double NumberValue {
			get {
				switch (DataType) {
					case JSType.String: return Convert.ToDouble(_stringValue);
					case JSType.Number: return _numberValue;
					case JSType.Boolean: return _booleanValue ? 1 : 0;
				}
				throw new InvalidCastException("Can't read " + DataType.ToString() + " as number");
			}
		}
		public bool BooleanValue {
			get {
				switch (DataType) {
					case JSType.String: return Convert.ToBoolean(_stringValue);
					case JSType.Number: return _numberValue != 0;
					case JSType.Boolean: return _booleanValue;
					case JSType.Null: return false;
				}
				throw new InvalidCastException("Can't read " + DataType.ToString() + " as boolean");
			}
		}
		public JSValue[] ArrayValue {
			get {
				if(DataType != JSType.Array) throw new InvalidCastException("Can't read " + DataType.ToString() + " as array");
				return _arrayValue;
			}
		}
		public ReadOnlyDictionary<string, JSValue> ObjectValue {
			get {
				if(DataType != JSType.Object) throw new InvalidCastException("Can't read " + DataType.ToString() + " as object");
				return _ObjectValue;
			}
		}

		public ReadOnlyDictionary<string, JSValue>.KeyCollection Keys => _ObjectValue.Keys;

		public JSValue(string s) {
			DataType = JSType.String;
			_stringValue = s;
		}
		public JSValue(double n) {
			DataType = JSType.Number;
			_numberValue = n;
		}
		public JSValue(int n) {
			DataType = JSType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JSValue(bool b) {
			DataType = JSType.Boolean;
			_booleanValue = b;
		}

		public JSValue(JSValue[] array) {
			DataType = JSType.Array;
			_arrayValue = array;
		}

		private JSValue(JsNull nul) {
			DataType = JSType.Null;
		}

		public JSValue(IDictionary<string, JSValue> properties) {
			DataType = JSType.Object;
			_ObjectValue = new ReadOnlyDictionary<string, JSValue>(properties);
		}


		private class JsNull { }
		public static readonly JSValue Null = new JSValue(new JsNull());

		public bool IsNull => DataType == JSType.Null;

		// native -> JS
		public static implicit operator JSValue(string s) => new JSValue(s);
		public static implicit operator JSValue(bool b) => new JSValue(b);
		public static implicit operator JSValue(int n) => new JSValue(n);
		public static implicit operator JSValue(double n) => new JSValue(n);
		public static implicit operator JSValue(Dictionary<string, JSValue> properties) => new JSValue(properties);
		public static implicit operator JSValue(IJSONEncodable[] arr) => arr == null ? null : new JSValue(arr.Select(item => {
			return item.ToJSValue();
		}).ToArray());
		public static implicit operator JSValue(JSValue[] arr) {
			if (arr == null) return JSValue.Null;
			return new JSValue(arr);
		}
		public static implicit operator JSValue(string[] v) => v.Select(m => new JSValue(m)).ToArray();
		public static implicit operator JSValue(bool[] v) => v.Select(m => new JSValue(m)).ToArray();
		public static implicit operator JSValue(int[] v) => v.Select(m => new JSValue(m)).ToArray();
		public static implicit operator JSValue(double[] v) => v.Select(m => new JSValue(m)).ToArray();

		// JS -> native
		public static implicit operator string(JSValue j){
			if(j.DataType != JSType.String) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to string is not allowed; consider jsValue.StringValue");
			return j.StringValue;
		}
		public static implicit operator double(JSValue j) {
			if(j.DataType == JSType.String) return ParseJSON(j.StringValue).NumberValue;
			if (j.DataType != JSType.Number) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to double is not allowed; consider jsValue.NumberValue");
			return j.NumberValue;
		}
		public static implicit operator int(JSValue j) { // not actually sure about this one
			double value = j.NumberValue;
			if(value != Math.Round(value)) throw new InvalidCastException("Lost data in implicit cast");
			if (j.DataType != JSType.Number) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to int is not allowed; consider jsValue.NumberValue");
			return Convert.ToInt32(j.NumberValue);
		}
		public static implicit operator bool(JSValue j){
			if (j.DataType != JSType.Boolean) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to bool is not allowed; consider jsValue.BooleanValue");
			return j.BooleanValue;
		}

		// JS array > native typed array
		public static implicit operator string[](JSValue j) => j.ArrayValue.Select(m => (string)m).ToArray();
		public static implicit operator bool[](JSValue j) => j.ArrayValue.Select(m => (bool)m).ToArray();
		public static implicit operator int[](JSValue j) => j.ArrayValue.Select(m => (int)m).ToArray();
		public static implicit operator double[](JSValue j) => j.ArrayValue.Select(m => (double)m).ToArray();

		// JS object > native typed Dictionary
		public static implicit operator ReadOnlyDictionary<string, string>(JSValue j)
			=> new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, string>(kv.Key, kv.Value)
			)));
		public static implicit operator ReadOnlyDictionary<string, bool>(JSValue j)
			=> new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, bool>(kv.Key, kv.Value)
			)));
		public static implicit operator ReadOnlyDictionary<string, int>(JSValue j)
			=> new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, int>(kv.Key, kv.Value)
			)));
		public static implicit operator ReadOnlyDictionary<string, double>(JSValue j)
			=> new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, double>(kv.Key, kv.Value)
			)));



		public static implicit operator JSValue[](JSValue j) => j.ArrayValue; // ArrayValue and ObjectValue getters already check the type
		public static implicit operator ReadOnlyDictionary<string, JSValue>(JSValue j) => j.ObjectValue;

		public override string ToString() => StringValue;

		// indexers
		public JSValue this[string property] {
			get {
				if (DataType != JSType.Object) throw new ArgumentException("Trying to access property of a " + DataType.ToString() + " value");
				return ObjectValue[property];
			}
		}
		public JSValue this[int index] {
			get {
				if (DataType != JSType.Array) throw new ArgumentException("Trying to access index of a " + DataType.ToString() + " value");
				return ArrayValue[index];
			}
		}

		public bool ContainsKey(string key) => ObjectValue.ContainsKey(key);
		public bool Contains(JSValue value) => ArrayValue.Contains(value);

		/// <summary>
		/// Converts the object to a JSON-formatted string
		/// </summary>
		/// <param name="maxDepth">Specifies a maximum nesting level for array- and object-typed values</param>
		/// <returns>JSON-formatted string</returns>
		public string ToJSON(int maxDepth = DefaultMaxDepth) {
			if (maxDepth < 0) throw new ArgumentOutOfRangeException("Maximum depth exceeded");
			switch (DataType) {
				case JSType.Boolean: return _booleanValue.ToJSON();
				case JSType.Number: return _numberValue.ToJSON();
				case JSType.String: return _stringValue.ToJSON();
				case JSType.Object: return ObjectValue.ToJSON(maxDepth);
				case JSType.Array: return ArrayValue.ToJSON(maxDepth);
				case JSType.Null: return "null";
			}
			return "";
		}

		/// <summary>
		/// Creates a JSValue object from a JSON-formatted strong
		/// </summary>
		/// <param name="json">A JSON-formatted string</param>
		/// <returns>An object representing the data structure expressed in the input JSON string</returns>
		public static JSValue ParseJSON(string json) => Parser.Parse(json);

	}

	public class ParseError : Exception {
		public ParseError(string reason) : base(reason) { }
		public ParseError(string reason, Exception InnerException) : base(reason, InnerException) { }
	}

	internal class Parser {
		delegate bool ReadingState(Parser parser);
		private string input;
		private Parser(string s) => input = s;
		public static JSValue Parse(string s){
			var parser = new Parser(s);
			parser.NextToken();
			var result = parser.readValue();
			parser.NextToken(true);
			return result;
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
		private JSValue readValue(){
			if(current == '"') return readString();
			if(current == '[') return readArray();
			if(current == '{') return readObject();
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

			var numberMatch = new Regex("\\d*\\.?\\d+").Match(input, position);
			if (numberMatch.Success) {
				string cap = numberMatch.Captures[0].Value;
				double num = Convert.ToDouble(cap);
				position += numberMatch.Captures[0].Length - 1;
				return num;
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
					else if (current == 'u') {
						// todo codepoint encoding
						var sequence = input.Substring(position + 1, 4);
						if(sequence.Length != 4 || !new Regex("^[0-9a-f]{4}$").IsMatch(sequence)) throw new ParseError("Malformed \\u sequence at " + position);
						sb.Append(char.ConvertFromUtf32(int.Parse(sequence, System.Globalization.NumberStyles.HexNumber)));
						position += 5;
						continue;
					}
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
				found.Add(readValue());

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
				var keyPosition = position;
				var keyValue = readValue();
				if(keyValue.DataType != JSType.String) throw new ParseError("Expected property name at " + position.ToString());
				NextToken();
				if(current != ':') throw new ParseError("Expected : at " + position.ToString());
				NextToken();
				var value = readValue();
				result[keyValue] = value;
				NextToken();
				if (current == '}') return result;
				if (current != ',') throw new ParseError("Expected , or } at " + position);
			}
		}

	}

}