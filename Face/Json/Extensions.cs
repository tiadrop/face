using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lantern.Face.Json {
    public static class Extensions {
        // primitives
        public static string ToJson(this string s) {
            s = s.Replace("\\", "\\\\");
            // escape any non-ascii
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s) {
                var code = (int) ch;
                if (code > 126 || code < 32) {
                    sb.Append($"\\u{code:X4}");
                } else sb.Append(ch);
            }

            return "\"" + sb.ToString()
                            .Replace("\"", "\\\"")
                       .Replace("\r", "\\r")
                       .Replace("\n", "\\n")
                       .Replace("\t", "\\t")
                       .Replace("\x08", "\\b")
                       .Replace("\x0c", "\\f")
                   + "\"";
        }

        public static string ToJson(this int v) => v.ToString(CultureInfo.InvariantCulture);
        public static string ToJson(this double v) => v.ToString(CultureInfo.InvariantCulture);
        public static string ToJson(this bool v) => v ? "true" : "false";

        public static string ToJson(this IJsonEncodable obj) => obj.ToJsValue().ToJson();

        // base collections
        public static string ToJson(this IEnumerable<JsValue> list, bool formatted = false,  int maxDepth = JsValue.DefaultMaxDepth) {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth), "Maximum depth exceeded");
            var jsonValues = list.Select(val => val == null ? "null" : val.ToJson(formatted, maxDepth - 1));
            var newline = formatted ? "\n" : "";
            var result = string.Join($",{newline}", jsonValues);
            var space = formatted ? " " : "";
            if (formatted) result = result.Replace("\n", "\n  ");
            return $"[{newline}{space}{space}{result}{newline}]";
        }

        public static string ToJson(this IDictionary<string, JsValue> dict, bool formatted = false,  int maxDepth = JsValue.DefaultMaxDepth){
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth), "Maximum depth exceeded");
            var space = formatted ? " " : "";
            var colonicPairings = dict.Keys.Select(key => {
                var value = dict[key];
                return $"{key.ToJson()}{space}:{space}{value.ToJson(formatted, maxDepth - 1)}";
            });
            var newline = formatted ? "\n" : "";
            var result = string.Join($",{newline}", colonicPairings);
            if (formatted) result = result.Replace("\n", "\n  ");
            return $"{{{newline}{space}{space}{result}{newline}}}";
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
        public static string ToJson(this IEnumerable<IJsonEncodable> list) => list.Select(v => v.ToJsValue()).ToArray().ToJson();
        public static string ToJson(this IEnumerable<string> list) => list.Select(v => new JsValue(v)).ToArray().ToJson();
        public static string ToJson(this IEnumerable<int> list) => list.Select(v => new JsValue(v)).ToArray().ToJson();
        public static string ToJson(this IEnumerable<bool> list) => list.Select(v => new JsValue(v)).ToArray().ToJson();
        public static string ToJson(this IEnumerable<double> list) => list.Select(v => new JsValue(v)).ToArray().ToJson();

    }
}