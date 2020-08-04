using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace Lantern.Face.Json {

	/// <summary>
	/// Represents a value as read from, or encodable as, JSON.
	/// </summary>
	public class JsValue {
		public enum Type {
			Boolean,
			Number,
			String,
			Object,
			Array,
			Null,
		}
		public const int DefaultMaxDepth = 32;
		public readonly Type DataType;
		private readonly bool _booleanValue;
		private readonly double _numberValue;
		private readonly string _stringValue;
		private readonly ReadOnlyDictionary<string, JsValue> _objectValue;
		private readonly JsValue[] _arrayValue;

		public JsValue(string s) {
			DataType = Type.String;
			_stringValue = s;
		}
		public JsValue(double n) {
			DataType = Type.Number;
			_numberValue = n;
		}
		public JsValue(int n) {
			DataType = Type.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(byte n) {
			DataType = Type.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(short n) {
			DataType = Type.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(uint n) {
			DataType = Type.Number;
			_numberValue = Convert.ToDouble(n);
		}
		public JsValue(ushort n) {
			DataType = Type.Number;
			_numberValue = Convert.ToDouble(n);
		}
		
		public JsValue(bool b) {
			DataType = Type.Boolean;
			_booleanValue = b;
		}

		public JsValue(JsValue[] array) {
			DataType = Type.Array;
			_arrayValue = array;
		}

		private JsValue(JsNull nul) {
			DataType = Type.Null;
		}

		public JsValue(IDictionary<string, JsValue> properties) {
			DataType = Type.Object;
			if (properties is ReadOnlyDictionary<string, JsValue> asReadOnly) {
				_objectValue = asReadOnly;
			} else {
				_objectValue = new ReadOnlyDictionary<string, JsValue>(properties);
			}
		}

		public JsValue(IEnumerable<(string, JsValue)> keyValuePairs) {
			DataType = Type.Object;
			var kvpArray = keyValuePairs.Select(pair => new KeyValuePair<string, JsValue>(pair.Item1, pair.Item2));
			var dict = new Dictionary<string, JsValue>(kvpArray);
			_objectValue = new ReadOnlyDictionary<string, JsValue>(dict);
		}

		public JsValue(IEnumerable<KeyValuePair<string, JsValue>> source) {
			DataType = Type.Object;
			_objectValue = new ReadOnlyDictionary<string, JsValue>(new Dictionary<string, JsValue>(source));
		}

		/// <summary>
		/// Returns the wrapped value as string, casting if necessary
		/// </summary>
		public string StringValue => DataType switch {
			Type.String => _stringValue,
			Type.Number => _numberValue.ToString(CultureInfo.InvariantCulture),
			Type.Boolean => _booleanValue ? "True" : "False",
			_ => throw new InvalidCastException($"Can't read JS {DataType} as string")
		};

		/// <summary>
		/// Returns the wrapped value as double, casting if necessary
		/// </summary>
		public double NumberValue => DataType switch {
			Type.String => Convert.ToDouble(_stringValue, CultureInfo.InvariantCulture),
			Type.Number => _numberValue,
			Type.Boolean => _booleanValue ? 1 : 0,
			_ => throw new InvalidCastException($"Can't read JS {DataType} as number")
		};
		
		/// <summary>
		/// Returns the wrapped value as boolean, casting if necessary
		/// </summary>
		public bool BooleanValue => DataType switch {
			Type.String => Convert.ToBoolean(_stringValue),
			Type.Number => _numberValue != 0,
			Type.Boolean => _booleanValue,
			Type.Null => false,
			_ => throw new InvalidCastException($"Can't read JS {DataType} as boolean")
		};

		/// <summary>
		/// Returns a wrapped Array-typed value
		/// </summary>
		public JsValue[] ArrayValue => DataType switch {
			Type.Array => _arrayValue,
			Type.Null => null,
			_ => throw new InvalidCastException($"Can't read JS {DataType} as array")
		};

		/// <summary>
		/// Returns a wrapped Object-typed value
		/// </summary>
		public ReadOnlyDictionary<string, JsValue> ObjectValue => DataType switch {
			Type.Object => _objectValue,
			Type.Null => null,
			_ => throw new InvalidCastException($"Can't read JS {DataType} as object")
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

		public bool IsNumber => DataType == Type.Number;
		public bool IsString => DataType == Type.String;
		public bool IsBoolean => DataType == Type.Boolean;
		public bool IsArray => DataType == Type.Array;
		public bool IsObject => DataType == Type.Object;
		public bool IsNull => DataType == Type.Null;

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
			if(j.DataType != Type.String) throw new InvalidCastException(
				$"Implicitly casting JS {j.DataType} to string is not allowed; consider jsValue.StringValue");
			return j.StringValue;
		}
		public static implicit operator double(JsValue j) {
			if(j.IsString) return FromJson(j.StringValue).NumberValue;
			if (!j.IsNumber) throw new InvalidCastException(
				$"Implicitly casting JS {j.DataType} to double is not allowed; consider jsValue.NumberValue");
			return j.NumberValue;
		}
		public static implicit operator int(JsValue j) {
			double value = j.NumberValue;
			if(value != Math.Round(value)) throw new InvalidCastException($"Lossy cast: {value} to int");
			if (j.DataType != Type.Number) throw new InvalidCastException(
				$"Implicitly casting JS {j.DataType} to int is not allowed; consider jsValue.NumberValue");
			return Convert.ToInt32(j.NumberValue);
		}
		public static implicit operator bool(JsValue j){
			if (j.DataType != Type.Boolean) throw new InvalidCastException(
				$"Implicitly casting JS {j.DataType} to bool is not allowed; consider jsValue.BooleanValue");
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
			DataType switch {
				Type.Boolean => _booleanValue.ToJson(),
				Type.Number => _numberValue.ToJson(),
				Type.String => _stringValue.ToJson(),
				Type.Object => ObjectValue.ToJson(formatted, maxDepth),
				Type.Array => ArrayValue.ToJson(formatted, maxDepth),
				Type.Null => "null",
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
				if(jv.DataType != DataType) return false;
				switch(jv.DataType){
					case Type.String:
						if(DataType != Type.String) return false;
						obj = jv._stringValue; break;
					case Type.Number:
						if (DataType != Type.Number) return false;
						obj = jv._numberValue; break;
					case Type.Boolean:
						if (DataType != Type.Boolean) return false;
						obj = jv._booleanValue; break;
					case Type.Null:
						obj = null;
						break;
					case Type.Array: return ReferenceEquals(obj, this) || ReferenceEquals(obj, this._arrayValue);
					case Type.Object: return ReferenceEquals(obj, this) || ReferenceEquals(obj, _objectValue);					
				}
			}

			return obj switch {
				null => IsNull,
				string s => IsString && s == _stringValue,
				bool b => IsBoolean && b == _booleanValue,
				double d => IsNumber && d == _numberValue,
				int i => IsNumber && Convert.ToDouble(i) == _numberValue,
				long l => IsNumber && Convert.ToDouble(l) == _numberValue,
				uint ui => IsNumber && Convert.ToDouble(ui) == _numberValue,
				ulong ul => IsNumber && Convert.ToDouble(ul) == _numberValue,
				byte by => IsNumber && Convert.ToDouble(@by) == _numberValue,
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
			return DataType switch {
				Type.String => _stringValue.GetHashCode(),
				Type.Number => _numberValue.GetHashCode(),
				Type.Boolean => _booleanValue.GetHashCode(),
				Type.Array => _arrayValue.GetHashCode(),
				Type.Object => _objectValue.GetHashCode(),
				Type.Null => JsValue.Null.GetHashCode(),
				_ => 0
			};
		}
	}
	
}