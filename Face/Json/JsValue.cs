using System.Collections.ObjectModel;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace Lantern.Face.Json {

	/// <summary>
	/// Represents a value as read from, or encodable as, JSON.
	/// </summary>
	public class JsValue {
		public enum DataType {
			Boolean,
			Number,
			String,
			Object,
			Array,
			Null,
		}
		public const int DefaultMaxDepth = 32;
		public readonly DataType Type;
		//private readonly bool _booleanValue;
		//private readonly double _numberValue;
		//private readonly string _stringValue;
		//private readonly ReadOnlyDictionary<string, JsValue> _objectValue;
		//private readonly JsValue[] _arrayValue;

		private readonly object _value;

		public JsValue(string s) {
			Type = DataType.String;
			_value = s;
		}
		public JsValue(double n) {
			Type = DataType.Number;
			_value = n;
		}
		public JsValue(int n) {
			Type = DataType.Number;
			_value = Convert.ToDouble(n);
		}
		public JsValue(byte n) {
			Type = DataType.Number;
			_value = Convert.ToDouble(n);
		}
		public JsValue(short n) {
			Type = DataType.Number;
			_value = Convert.ToDouble(n);
		}
		public JsValue(uint n) {
			Type = DataType.Number;
			_value = Convert.ToDouble(n);
		}
		public JsValue(ushort n) {
			Type = DataType.Number;
			_value = Convert.ToDouble(n);
		}
		
		public JsValue(bool b) {
			Type = DataType.Boolean;
			_value = b;
		}

		public JsValue(IEnumerable array) {
			Type = DataType.Array;
			_value = array;
		}

		private JsValue(JsNull nul) {
			Type = DataType.Null;
		}

		public JsValue(IDictionary<string, JsValue> properties) {
			Type = DataType.Object;
			if (properties is ReadOnlyDictionary<string, JsValue> asReadOnly) {
				_value = asReadOnly;
			} else {
				_value = new ReadOnlyDictionary<string, JsValue>(properties);
			}
		}

		public JsValue(IEnumerable<(string, JsValue)> keyValuePairs) {
			Type = DataType.Object;
			var kvpArray = keyValuePairs.Select(pair => new KeyValuePair<string, JsValue>(pair.Item1, pair.Item2));
			var dict = new Dictionary<string, JsValue>(kvpArray);
			_value = new ReadOnlyDictionary<string, JsValue>(dict);
		}

		public JsValue(IEnumerable<KeyValuePair<string, JsValue>> source) {
			Type = DataType.Object;
			_value = new ReadOnlyDictionary<string, JsValue>(new Dictionary<string, JsValue>(source));
		}

		/// <summary>
		/// Returns the wrapped value as string, casting if necessary
		/// </summary>
		public string StringValue => Type switch {
			DataType.String => _value as string,
			DataType.Number => ((double)_value).ToString(CultureInfo.InvariantCulture),
			DataType.Boolean => (bool)_value ? "True" : "False",
			_ => throw new InvalidCastException($"Can't read JS {Type} as string")
		};

		/// <summary>
		/// Returns the wrapped value as double, casting if necessary
		/// </summary>
		public double NumberValue => Type switch {
			DataType.String => Convert.ToDouble((string)_value, CultureInfo.InvariantCulture),
			DataType.Number => (double)_value,
			DataType.Boolean => (bool)_value ? 1 : 0,
			_ => throw new InvalidCastException($"Can't read JS {Type} as number")
		};
		
		/// <summary>
		/// Returns the wrapped value as boolean, casting if necessary
		/// </summary>
		public bool BooleanValue => Type switch {
			DataType.String => Convert.ToBoolean((string)_value),
			DataType.Number => (double)_value != 0,
			DataType.Boolean => (bool)_value,
			DataType.Null => false,
			_ => throw new InvalidCastException($"Can't read JS {Type} as boolean")
		};

		/// <summary>
		/// Returns a wrapped Array-typed value
		/// </summary>
		public JsValue[] ArrayValue {
			get {
				return Type switch {
					DataType.Array => (JsValue[]) _value,
					DataType.Null => null,
					_ => throw new InvalidCastException($"Can't read JS {Type} as array")
				};
			}
		}

		/// <summary>
		/// Returns a wrapped Object-typed value
		/// </summary>
		public ReadOnlyDictionary<string, JsValue> ObjectValue => Type switch {
			DataType.Object => (ReadOnlyDictionary<string, JsValue>)_value,
			DataType.Null => null,
			_ => throw new InvalidCastException($"Can't read JS {Type} as object")
		};

		/// <summary>
		/// Returns the specified property of an Object-typed value if it exists, otherwise returns an alternative value
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="alternative"></param>
		/// <returns></returns>
		public JsValue PropertyValueOr(string propertyName, JsValue alternative = null) 
			=> ContainsKey(propertyName) ? this[propertyName] : alternative;
		
		/// <summary>
		/// Returns the specified property of an Object-typed value if it exists, otherwise evaluates and returns the result of a delegate
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="alternativeFn"></param>
		/// <returns></returns>
		public JsValue PropertyValueOr(string propertyName, Func<JsValue> alternativeFn) 
			=> ContainsKey(propertyName) ? this[propertyName] : alternativeFn();

		/// <summary>
		/// Allows for JavaScript-style or-chaining where the first "truthy" value is returned by the chain.
		/// </summary>
		/// <param name="alt"></param>
		/// <returns>this, if its value equates to true, otherwise the value passed</returns>
		public JsValue Or(JsValue alt) => BooleanValue ? this : alt;
		
		/// <summary>
		/// Allows for lazily-evaluated JavaScript-style or-chaining where the first "truthy" value is returned by the chain.
		/// </summary>
		/// <param name="altFn"></param>
		/// <returns>this, if its value equates to true, otherwise the value returned by altFn</returns>
		public JsValue Or(Func<JsValue> altFn) => BooleanValue ? this : altFn();

		private class JsNull { }
		public static readonly JsValue Null = new JsValue(new JsNull());

		public bool IsNumber => Type == DataType.Number;
		public bool IsString => Type == DataType.String;
		public bool IsBoolean => Type == DataType.Boolean;
		public bool IsArray => Type == DataType.Array;
		public bool IsObject => Type == DataType.Object;
		public bool IsNull => Type == DataType.Null;

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
		public static implicit operator JsValue((string, JsValue)[] pairs) => new JsValue(pairs);

		// JS -> native
		public static implicit operator string(JsValue j){
			if(j.Type != DataType.String) throw new InvalidCastException(
				$"Implicitly casting JS {j.Type} to string is not allowed; consider jsValue.StringValue");
			return j.StringValue;
		}
		public static implicit operator double(JsValue j) {
			if(j.IsString) return FromJson(j.StringValue).NumberValue;
			if (!j.IsNumber) throw new InvalidCastException(
				$"Implicitly casting JS {j.Type} to double is not allowed; consider jsValue.NumberValue");
			return j.NumberValue;
		}
		public static implicit operator int(JsValue j) {
			double value = j.NumberValue;
			if(value != Math.Round(value)) throw new InvalidCastException($"Lossy cast: {value} to int");
			if (j.Type != DataType.Number) throw new InvalidCastException(
				$"Implicitly casting JS {j.Type} to int is not allowed; consider jsValue.NumberValue");
			return Convert.ToInt32(j.NumberValue);
		}
		public static implicit operator bool(JsValue j){
			if (j.Type != DataType.Boolean) throw new InvalidCastException(
				$"Implicitly casting JS {j.Type} to bool is not allowed; consider jsValue.BooleanValue");
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
		/// <param name="formatted"></param>
		/// <param name="maxDepth">Specifies a maximum nesting level for array- and object-typed values</param>
		/// <returns>JSON-formatted string</returns>
		public string ToJson(bool formatted = false, int maxDepth = DefaultMaxDepth) =>
			Type switch {
				DataType.Boolean => ((bool)_value).ToJson(),
				DataType.Number => ((double)_value).ToJson(),
				DataType.String => ((string)_value).ToJson(),
				DataType.Object => ObjectValue.ToJson(formatted, maxDepth),
				DataType.Array => ArrayValue.ToJson(formatted, maxDepth),
				DataType.Null => "null",
				_ => ""
			};

		/// <summary>
		/// Produces a JsValue object from a JSON-formatted string
		/// </summary>
		/// <param name="json">JSON-formatted string</param>
		/// <param name="relaxed">True to allow // comments and unquoted property names</param>
		/// <returns>Object representing the structure defined in JSON</returns>
		public static JsValue FromJson(string json, bool relaxed = false) => Parser.Parse(json, relaxed);

		public override bool Equals(object obj) {
			if(obj is JsValue jv){
				if(jv.Type != Type) return false;
				switch(jv.Type){
					case DataType.String:
						if(Type != DataType.String) return false;
						obj = (string)jv._value; break;
					case DataType.Number:
						if (Type != DataType.Number) return false;
						obj = (double)jv._value; break;
					case DataType.Boolean:
						if (Type != DataType.Boolean) return false;
						obj = (bool)jv._value; break;
					case DataType.Null:
						obj = null;
						break;
					case DataType.Array: return ReferenceEquals(obj, this) || ReferenceEquals(obj, _value);
					case DataType.Object: return ReferenceEquals(obj, this) || ReferenceEquals(obj, _value);					
				}
			}

			return obj switch {
				null => IsNull,
				string s => IsString && s == (string)_value,
				bool b => IsBoolean && b == (bool)_value,
				double d => IsNumber && d == (double)_value,
				int i => IsNumber && Convert.ToDouble(i) == (double)_value,
				long l => IsNumber && Convert.ToDouble(l) == (double)_value,
				uint ui => IsNumber && Convert.ToDouble(ui) == (double)_value,
				ulong ul => IsNumber && Convert.ToDouble(ul) == (double)_value,
				byte by => IsNumber && Convert.ToDouble(@by) == (double)_value,
				_ => false
			};
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
			return Type switch {
				DataType.String => ((string)_value).GetHashCode(),
				DataType.Number => ((double)_value).GetHashCode(),
				DataType.Boolean => ((bool)_value).GetHashCode(),
				DataType.Array => ((JsValue[])_value).GetHashCode(),
				DataType.Object => ((ReadOnlyDictionary<string, JsValue>)_value).GetHashCode(),
				DataType.Null => 0,
				_ => 0
			};
		}
	}
	
}