using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Lantern.Face.JSON;

namespace face.demo {
    public class Test {

        private static void fail(string desc, string reason) {
            Console.WriteLine("FAILED: " + desc);
            Console.WriteLine("- " + reason);
        }

        private static void pass(string desc, string info = "") {
            if (info == "") {
                Console.WriteLine("pass: " + desc);
            }
            else {
                Console.WriteLine("pass: " + desc + " -- " + info);
            }
        }
        public static void ExpectException(string desc, Action fn, string expectMessageContains) {
            try {
                fn();
            }
            catch (Exception e) {
                List<string> messages = new List<string>();
                while (true) {
                    messages.Add(e.GetType().Name + "(" + e.Message.ToJson() + ")");
                    if (e.Message.Contains(expectMessageContains)) {
                        pass(desc, string.Join(" >> ", messages));
                        return;
                    }
                    e = e.InnerException;
                    if (e == null) break;
                }
                
                fail(desc, "Unexpected exception: " + string.Join(" >> ", messages));
                return;
            }
            fail(desc, "No exception thrown");
        }

        public static void Assert(string desc, bool a) {
            if (a) pass(desc);
            else fail(desc, "assertion failed");
        }
       

        public static void Run() {
            var testJson = File.ReadAllText("demo/test.json");
            var jv = JsValue.ParseJson(testJson);
            Console.WriteLine(jv.ToJson());
            /* {
              "ints" : [ 0, 4, 5, 1],
              "doubles" : [1, 3, 3.7,],
              "strings" : ["Poland", "Gibraltar","Ethiopia" ],
              "mixed" :   ["1", 5, null, false],
              "structs"   : [
                {
                  "name": "Henry",
                  "hobbies":["darts", "rambling"],
                  "age" :31 ,
                },
                {
                  "name": "Tina",
                  "hobbies": ["gaming", "biking"],
                }
              ]
            } */
            ExpectException("Can't cast object to string", () => Console.WriteLine(jv.StringValue), "Object as string");
            Assert("Tina likes biking", jv["structs"].ArrayValue.First(v => v["name"] == "Tina")["hobbies"].Contains("biking") == true);
            var henry = jv["structs"].ArrayValue.First((v => v["name"] == "Henry"));
            var tina = jv["structs"].ArrayValue.First(v => v["name"] == "Tina");
            Assert("Henry doesn't like biking", henry["hobbies"].Contains("biking") == false);
            Assert("Henry's first hobby is darts", henry["hobbies"][0] == "darts");
            Assert("Henry's age is known", henry.ContainsKey("age"));
            Assert("Tina's age is not known", !tina.ContainsKey("age"));
            ExpectException("Non-object ContainsKey exception", () => henry["age"].ContainsKey("turtles"), "as object");
            ExpectException("Can't read numeric key from JS Object", () => Console.WriteLine(jv[5].StringValue), "Can't read JS Object as array");
            ExpectException("Can't read string key from JS Array", () => Console.WriteLine(jv["doubles"]["two"].NumberValue), "Can't read JS Array as object");
            int[] ints = jv["ints"];
            Assert("Last int is 1", ints.Last() == 1);
            ExpectException("Can't cast number array with float to int[]", () => { ints = jv["doubles"]; }, "Lossy cast");
            string[] strings = jv["strings"];
            Assert("Strings are Poland, Gibraltar, Ethiopia", string.Join(", ", strings) == "Poland, Gibraltar, Ethiopia");
            Assert("There are 2 structs", jv["structs"].Count == 2);
            Assert("Last ints [1] == first doubles [1]", jv["ints"][3] == jv["doubles"][0]);
            Assert("First mixed [\"1\"] != first doubles [1]", jv["mixed"][0] != jv["doubles"][0]);
            ExpectException("Can't implicit cast JS num to string", () => Console.Write("cast" + jv["mixed"][1]), "to string is not allowed");
            Assert("Can implicit cast JS string to string", "cast" + jv["mixed"][0] == "cast1");
            Assert("Can explicit cast JS num to string", jv["ints"][0].StringValue == "0");
            JsValue[] structs = jv["structs"];
            var structsJs = structs.ToJson();
            ExpectException("Parse error with junk appended", () => JsValue.ParseJson(structsJs + "   ."), "Unexpected");
            ExpectException("Parse error with junk prepended", () => JsValue.ParseJson("-  " + testJson), "Unexpected");
            ExpectException("Parse error with { prepended", () => JsValue.ParseJson("{" + testJson), "Expected property");
            ExpectException("Parse error with [ prepended", () => JsValue.ParseJson("[" + testJson), "Past end");
            var jv2 = JsValue.ParseJson(structsJs);
            string[] namesFromJv = jv["structs"].ArrayValue.Select(v => (string)v["name"]).ToArray();
            string[] namesFromExport = jv2.ArrayValue.Select(v => (string)v["name"]).ToArray();
            Assert("Names match after export/import", string.Join(",", namesFromJv) == string.Join(",", namesFromExport));
            Assert("JsValue(5) == JsValue(5)", new JsValue(5) == new JsValue(5));
            Assert("JsValue(5) == JsValue(5f)", new JsValue(5) == new JsValue(5f));
            Assert("JsValue(5) != JsValue(2)", new JsValue(5) != new JsValue(2));
            Assert("JsValue(5) != JsValue(\"5\")", new JsValue(5) != new JsValue("5"));
            Assert("JsValue(5) == 5f", new JsValue(5) == 5f);
            ExpectException("Can't parse \"-\"", () => JsValue.ParseJson("-"), "Unexpected");
            ExpectException("Can't parse \"[1,4,\"", () => JsValue.ParseJson("[1,4,"), "Past end");
            ExpectException("Can't parse \"{\\\"hello wo", () => JsValue.ParseJson("{\"hello wo"), "Unclosed string");
            ExpectException("Can't parse \"{\\\"hello\\\": world}", () => JsValue.ParseJson("{\"hello\": world}"), "Unexpected");
            Assert("ParseJson(\"-5.2\") == 5.2", JsValue.ParseJson("-5.2") == -5.2);
            Assert("ParseJson(\"null\") == null", JsValue.ParseJson("null") == null);
            ExpectException("Identify position of invalid @ \"[[-5.4x]]\"", () => JsValue.ParseJson("[[-5.4x]]"), "at input position 6");
            ExpectException("Fail on malformed codepoint in [\"abc\\u9\"] ", () => JsValue.ParseJson("[\"abc\\u9\"]"), "Malformed \\u sequence");
            ExpectException("Fail on malformed codepoint in [\"abc\\u123x\"] ", () => JsValue.ParseJson("[\"abc\\u123x\"]"), "Malformed \\u sequence");
            ExpectException("Exception parsing empty string", () => JsValue.ParseJson(""), "Past end");
            ExpectException("Exception parsing \"[\"", () => JsValue.ParseJson("["), "Past end");
        }
        
    }
}