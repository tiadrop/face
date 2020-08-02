using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lantern.Face.Json {
    public static class Extensions {
        // primitives
        public static string ToJson(this string s) => "\"" + s
           .Replace("\\", "\\\\")
           .Replace("\"", "\\\"")
           .Replace("\r", "\\r")
           .Replace("\n", "\\n")
           .Replace("\t", "\\t")
           .Replace("\x08", "\\b")
           .Replace("\x0c", "\\f")
            + "\"";
        public static string ToJson(this int v) => v.ToString(CultureInfo.InvariantCulture);
        public static string ToJson(this double v) => v.ToString(CultureInfo.InvariantCulture);
        public static string ToJson(this bool v) => v ? "true" : "false";

        public static string ToJson(this IJsonEncodable obj) => obj.ToJsValue().ToJson();

        // base collections
        public static string ToJson(this JsValue[] list, int maxDepth = JsValue.DefaultMaxDepth) {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth), "Maximum depth exceeded");
            var jsonValues = list.Select(val => val == null ? "null" : val.ToJson(maxDepth - 1));
            return $"[{String.Join(",", jsonValues)}]";
        }

        public static string ToJson(this IDictionary<string, JsValue> dict, int maxDepth = JsValue.DefaultMaxDepth){
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth), "Maximum depth exceeded");
            var colonicPairings = dict.Keys.Select(key => {
                var value = dict[key];
                return $"{key.ToJson()}:{value.ToJson(maxDepth - 1)}";
            });
            return $"{{{string.Join(",", colonicPairings)}}}";
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
}