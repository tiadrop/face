using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace Lantern.Face.Json {

	public static class Extensions {
		// primitives
		public static string ToJson(this string s) => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
		public static string ToJson(this int v) => v.ToString();
		public static string ToJson(this double v) => v.ToString(CultureInfo.InvariantCulture);
		public static string ToJson(this bool v) => v ? "true" : "false";

		public static string ToJson(this IJsonEncodable obj) => obj.ToJsValue().ToJson();

		// base collections
		public static string ToJson(this JsValue[] list, int maxDepth = JsValue.DefaultMaxDepth)
			=> "[" + String.Join(",", list.Select(val => val == null ? "null" : val.ToJson(maxDepth - 1))) + "]";
		public static string ToJson(this IDictionary<string, JsValue> dict, int maxDepth = JsValue.DefaultMaxDepth){
			var sb = new StringBuilder();
			sb.Append("{");
			var colonicPairings = dict.Keys.Select(key => {
				var value = dict[key];
				var sb = new StringBuilder();
				sb.Append(key.ToJson());
				sb.Append(":");
				sb.Append(value.ToJson(maxDepth - 1));
				return sb.ToString();
			});

			sb.Append(string.Join(",", colonicPairings));

			sb.Append("}");
			return sb.ToString();
		}

		// compatible Dictionaries
		public static string ToJson(this IDictionary<string, IJsonEncodable> dict)
			=> new Dictionary<string, JsValue>(dict.Select(kv => 
				new KeyValuePair<string, JsValue>(kv.Key, kv.Value.ToJsValue())
			)).ToJson();
		public static string ToJson(this IDictionary<string, string> dict)
			=> new Dictionary<string, JsValue>(dict.Select(kv =>
				new KeyValuePair<string, JsValue>(kv.Key, kv.Value) 
			)).ToJson();
		public static string ToJson(this IDictionary<string, double> dict)
			=> new Dictionary<string, JsValue>(dict.Select(kv =>
				new KeyValuePair<string, JsValue>(kv.Key, kv.Value)
			)).ToJson();
		public static string ToJson(this IDictionary<string, int> dict)
			=> new Dictionary<string, JsValue>(dict.Select(kv =>
				new KeyValuePair<string, JsValue>(kv.Key, kv.Value)
			)).ToJson();
		public static string ToJson(this IDictionary<string, bool> dict)
			=> new Dictionary<string, JsValue>(dict.Select(kv =>
				new KeyValuePair<string, JsValue>(kv.Key, kv.Value)
			)).ToJson();

		// compatible arrays
		public static string ToJson(this IJsonEncodable[] list) => list.Select(v => v.ToJsValue()).ToArray().ToJson();
		public static string ToJson(this string[] list) => list.Select(v => new JsValue(v)).ToArray().ToJson();
		public static string ToJson(this int[] list) => list.Select(v => new JsValue(v)).ToArray().ToJson();
		public static string ToJson(this bool[] list) => list.Select(v => new JsValue(v)).ToArray().ToJson();
		public static string ToJson(this double[] list) => list.Select(v => new JsValue(v)).ToArray().ToJson();

	}

	public interface IJsonEncodable {
		/// <summary>
		/// Express the object as a JSValue to enable implicit and explicit JSON encoding
		/// </summary>
		/// <returns>A JSValue representing this object</returns>
		JsValue ToJsValue();
	}

	public enum JsType {
		Boolean,
		Number,
		String,
		Object,
		Array,
		Null,
	}

	public class JsValue {
		public const int DefaultMaxDepth = 32;
		public readonly JsType DataType;
		private readonly bool _booleanValue;
		private readonly double _numberValue;
		private readonly string _stringValue;
		private readonly ReadOnlyDictionary<string, JsValue> _objectValue;
		private readonly JsValue[] _arrayValue;

		public JsValue(string s) {
			DataType = JsType.String;
			_stringValue = s;
		}
		public JsValue(double n) {
			DataType = JsType.Number;
			_numberValue = n;
		}
		public JsValue(int n) {
			DataType = JsType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(byte n) {
			DataType = JsType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(short n) {
			DataType = JsType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(uint n) {
			DataType = JsType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(ushort n) {
			DataType = JsType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(Int64 n) {
			DataType = JsType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(UInt64 n) {
			DataType = JsType.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(bool b) {
			DataType = JsType.Boolean;
			_booleanValue = b;
		}

		public JsValue(JsValue[] array) {
			DataType = JsType.Array;
			_arrayValue = array;
		}

		private JsValue(JsNull nul) {
			DataType = JsType.Null;
		}

		public JsValue(IDictionary<string, JsValue> properties) {
			DataType = JsType.Object;
			_objectValue = new ReadOnlyDictionary<string, JsValue>(properties);
		}


		public string StringValue {
			get {
				switch (DataType) {
					case JsType.String: return _stringValue;
					case JsType.Number: return _numberValue.ToString(CultureInfo.InvariantCulture);
					case JsType.Boolean: return _booleanValue ? "True" : "False";
				}
				throw new InvalidCastException("Can't read JS " + DataType.ToString() + " as string");
			}
		}
		public double NumberValue {
			get {
				switch (DataType) {
					case JsType.String: return Convert.ToDouble(_stringValue);
					case JsType.Number: return _numberValue;
					case JsType.Boolean: return _booleanValue ? 1 : 0;
				}
				throw new InvalidCastException("Can't read JS " + DataType.ToString() + " as number");
			}
		}
		public bool BooleanValue {
			get {
				switch (DataType) {
					case JsType.String: return Convert.ToBoolean(_stringValue);
					case JsType.Number: return _numberValue != 0;
					case JsType.Boolean: return _booleanValue;
					case JsType.Null: return false;
				}
				throw new InvalidCastException($"Can't read JS {DataType} as boolean");
			}
		}
		public JsValue[] ArrayValue {
			get {
				if(IsNull) return null;
				if(!IsArray) throw new InvalidCastException($"Can't read JS {DataType} as array");
				return _arrayValue;
			}
		}
		public ReadOnlyDictionary<string, JsValue> ObjectValue {
			get {
				if(IsNull) return null;
				if(!IsObject) throw new InvalidCastException($"Can't read JS {DataType} as object");
				return _objectValue;
			}
		}

		private class JsNull { }
		public static readonly JsValue Null = new JsValue(new JsNull());

		public bool IsNumber => DataType == JsType.Number;
		public bool IsString => DataType == JsType.String;
		public bool IsBoolean => DataType == JsType.Boolean;
		public bool IsArray => DataType == JsType.Array;
		public bool IsObject => DataType == JsType.Object;
		public bool IsNull => DataType == JsType.Null;

		// native -> JS
		public static implicit operator JsValue(string s) => new JsValue(s);
		public static implicit operator JsValue(bool b) => new JsValue(b);
		public static implicit operator JsValue(int n) => new JsValue(n);
		public static implicit operator JsValue(double n) => new JsValue(n);
		public static implicit operator JsValue(Dictionary<string, JsValue> properties) => new JsValue(properties);
		public static implicit operator JsValue(IJsonEncodable[] arr) => arr == null ? null : new JsValue(arr.Select(item 
			=> item.ToJsValue()).ToArray());
		public static implicit operator JsValue(JsValue[] arr) => arr == null ? JsValue.Null : new JsValue(arr);
		public static implicit operator JsValue(string[] v) => v.Select(m => new JsValue(m)).ToArray();
		public static implicit operator JsValue(bool[] v) => v.Select(m => new JsValue(m)).ToArray();
		public static implicit operator JsValue(int[] v) => v.Select(m => new JsValue(m)).ToArray();
		public static implicit operator JsValue(double[] v) => v.Select(m => new JsValue(m)).ToArray();

		// JS -> native
		public static implicit operator string(JsValue j){
			if(j.DataType != JsType.String) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to string is not allowed; consider jsValue.StringValue");
			return j.StringValue;
		}
		public static implicit operator double(JsValue j) {
			if(j.IsString) return ParseJson(j.StringValue).NumberValue;
			if (!j.IsNumber) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to double is not allowed; consider jsValue.NumberValue");
			return j.NumberValue;
		}
		public static implicit operator int(JsValue j) {
			double value = j.NumberValue;
			if(value != Math.Round(value)) throw new InvalidCastException($"Lossy cast: {value} to int");
			if (j.DataType != JsType.Number) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to int is not allowed; consider jsValue.NumberValue");
			return Convert.ToInt32(j.NumberValue);
		}
		public static implicit operator bool(JsValue j){
			if (j.DataType != JsType.Boolean) throw new InvalidCastException("Implicitly casting JS " + j.DataType.ToString() + " to bool is not allowed; consider jsValue.BooleanValue");
			return j.BooleanValue;
		}

		// JS array > native typed array
		public static implicit operator string[](JsValue j) => j.ArrayValue.Select(m => (string)m).ToArray();
		public static implicit operator bool[](JsValue j) => j.ArrayValue.Select(m => (bool)m).ToArray();
		public static implicit operator int[](JsValue j) => j.ArrayValue.Select(m => (int)m).ToArray();
		public static implicit operator double[](JsValue j) => j.ArrayValue.Select(m => (double)m).ToArray();

		// JS object > native typed Dictionary
		public static implicit operator ReadOnlyDictionary<string, string>(JsValue j)
			=> new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, string>(kv.Key, kv.Value)
			)));
		public static implicit operator ReadOnlyDictionary<string, bool>(JsValue j)
			=> new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, bool>(kv.Key, kv.Value)
			)));
		public static implicit operator ReadOnlyDictionary<string, int>(JsValue j)
			=> new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, int>(kv.Key, kv.Value)
			)));
		public static implicit operator ReadOnlyDictionary<string, double>(JsValue j)
			=> new ReadOnlyDictionary<string, double>(new Dictionary<string, double>(j.ObjectValue.Select(kv =>
				new KeyValuePair<string, double>(kv.Key, kv.Value)
			)));


		public static implicit operator JsValue[](JsValue j) => j.ArrayValue;
		public static implicit operator ReadOnlyDictionary<string, JsValue>(JsValue j) => j.ObjectValue;

		public override string ToString() => StringValue;

		// indexers
		public JsValue this[string property] => ObjectValue[property];
		public JsValue this[int index] => ArrayValue[index];

		public ReadOnlyDictionary<string, JsValue>.KeyCollection Keys => ObjectValue.Keys;
		public bool ContainsKey(string key) => ObjectValue.ContainsKey(key);
		public bool Contains(JsValue value) => ArrayValue.Contains(value);
		public int Count => ArrayValue.Length;

		/// <summary>
		/// Converts the object to a JSON-formatted string
		/// </summary>
		/// <param name="maxDepth">Specifies a maximum nesting level for array- and object-typed values</param>
		/// <returns>JSON-formatted string</returns>
		public string ToJson(int maxDepth = DefaultMaxDepth) {
			if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth), "Maximum depth exceeded");
			return DataType switch {
				JsType.Boolean => _booleanValue.ToJson(),
				JsType.Number => _numberValue.ToJson(),
				JsType.String => _stringValue.ToJson(),
				JsType.Object => ObjectValue.ToJson(maxDepth),
				JsType.Array => ArrayValue.ToJson(maxDepth),
				JsType.Null => "null",
				_ => ""
			};
		}

		/// <summary>
		/// Produces a JsValue object from a JSON-formatted string
		/// </summary>
		/// <param name="json">JSON-formatted string</param>
		/// <param name="relaxed">True to allow // comments and unquoted property names</param>
		/// <returns>Object representing the structure defined in JSON</returns>
		public static JsValue ParseJson(string json, bool relaxed = false) => Parser.Parse(json, relaxed);

		public override bool Equals(object obj) {
			if(obj is JsValue jv){
				if(jv.DataType != DataType) return false;
				switch(jv.DataType){
					case JsType.String:
						if(DataType != JsType.String) return false;
						obj = jv._stringValue; break;
					case JsType.Number:
						if (DataType != JsType.Number) return false;
						obj = jv._numberValue; break;
					case JsType.Boolean:
						if (DataType != JsType.Boolean) return false;
						obj = jv._booleanValue; break;
					case JsType.Null:
						obj = null;
						break;
					case JsType.Array: return ReferenceEquals(obj, this) || ReferenceEquals(obj, this._arrayValue);
					case JsType.Object: return ReferenceEquals(obj, this) || ReferenceEquals(obj, _objectValue);					
				}
			}
			if (obj == null) return IsNull;
			if (obj is string s) return IsString && s == _stringValue;
			if (obj is bool b) return IsBoolean && b == _booleanValue;
			if (obj is double d) return IsNumber && d == _numberValue;
			if (obj is int i) return IsNumber && Convert.ToDouble(i) == _numberValue;
			if (obj is long l) return IsNumber && Convert.ToDouble(l) == _numberValue;
			if (obj is uint ui) return IsNumber && Convert.ToDouble(ui) == _numberValue;
			if (obj is ulong ul) return IsNumber && Convert.ToDouble(ul) == _numberValue;
			if (obj is byte by) return IsNumber && Convert.ToDouble(by) == _numberValue;
			return false;
		}

		public static bool operator ==(JsValue a, JsValue b) {
			if ((object)a == null) return (object)b == null;
			return a.Equals(b);
		}

		public static bool operator !=(JsValue a, JsValue b) {
			if ((object)a == null) return (object) b != null;
			return !a.Equals(b);
		}

		public override int GetHashCode() {
			return DataType switch {
				JsType.String => _stringValue.GetHashCode(),
				JsType.Number => _numberValue.GetHashCode(),
				JsType.Boolean => _booleanValue.GetHashCode(),
				JsType.Array => _arrayValue.GetHashCode(),
				JsType.Object => _objectValue.GetHashCode(),
				JsType.Null => JsValue.Null.GetHashCode(),
				_ => 0
			};
		}
	}
	
}