using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lantern.Face.Json;

namespace face.demo {
    
    public static class JsonTest {
        public static void Run() {
            
            var bm = new Benchmarker("JSON");
            if (File.Exists("benchmarkStandard.json"))
                bm.PreviousData = JsValue.FromJson(File.ReadAllText("benchmarkStandard.json"));
            
            bm.TimeLimitMs = 3200;
            bm.Participants["Face"] = data => JsValue.FromJson(data as string);
            bm.Participants["Utf8Json"] = data => Utf8Json.JsonSerializer.Deserialize<object>(data as string);
            bm.Participants["System.Text"] = data => System.Text.Json.JsonSerializer.Deserialize<object>(data as string);
            bm.Participants["Newtonsoft"] = data => Newtonsoft.Json.JsonConvert.DeserializeObject<object>(data as string);
            
            bm.Tests.Add("hardcoded_example", new Benchmarker.Test {
                Remark = "basic example test",
                Action = run => run("[4,6,8]")
            });

            // load the rest from testidx.json
            JsValue[] testIndex = JsValue.FromJson(File.ReadAllText("demo/testidx.json"), true);
            foreach (var testSpec in testIndex) {
                if (testSpec.ContainsKey("filename") && !File.Exists(testSpec["filename"])) {
                    Console.WriteLine($"Missing test source '{testSpec["filename"]}'");
                    continue;
                }
                string json = testSpec.ContainsKey("filename")
                    ? File.ReadAllText(testSpec["filename"])
                    : testSpec["json"].StringValue;
                string testRef = testSpec.ContainsKey("filename")
                    ? testSpec["filename"].StringValue.Split("/").Last()
                    : testSpec["ref"].StringValue;
                bm.Tests[testRef] = new Benchmarker.Test {
                    Remark = $"{formatFileSize(json.Length)} ━ {testSpec.PropertyValueOr("remark", testRef)}",
                    Action = run => run(json)
                };
            }

            bm.Run();
            File.WriteAllText("benchmarkStandard.json", bm.PreviousData.ToJson(true));
        }

        private static string formatFileSize(int size) {
            string unit = "b";
            double newSize = Convert.ToDouble(size);
            if (newSize > 1024) {
                unit = "kb";
                newSize /= 1024;
            }
            if (newSize > 1024) {
                unit = "mb";
                newSize /= 1024;
            }
            if (newSize > 1024) {
                unit = "gb"; // lofl
                newSize /= 1024;
            }
            return $"{newSize:F0}{unit}";
        }

        /// <summary>
        /// Attempts to parse all .json files in the given directory. Files beginning with 'y' are expected to parse without error, 'n' to fail and anything else to either parse or fail.
        /// </summary>
        /// <param name="pathToJsonFiles"></param>
        /// <param name="showPass">True to output all test results, false to only show failures</param>
        /// <param name="filter">Limits tests to filenames containing this string</param>
        public static void RunSuite(string pathToJsonFiles, bool showPass, string filter = "") {
            var filenames = Directory.GetFiles(pathToJsonFiles);

            foreach (var filename in filenames) {
                if(!filename.Contains(filter)) continue;
                string content = File.ReadAllText(filename);
                var basename = filename.Split("/").Last();
                basename += " " + (content.Length > 32 ? content.Substring(0, 32) + "..." : content);
                var expectSuccess = basename[0] == 'y';
                var expectFailure = basename[0] == 'n';
                //Console.WriteLine($"Parsing {basename}");
                try {
                    JsValue.FromJson(content);
                } catch (Exception e) {
                    if (expectSuccess) {
                        Console.WriteLine("** FAILED ** - " + basename + " - " + Benchmarker.RenderException(e));
                        //Console.WriteLine(content);
                    } else if (expectFailure) {
                        if(showPass) Console.WriteLine("Pass (expected error) - " + basename + " - " + Benchmarker.RenderException(e));
                    } else {
                        if(showPass) Console.WriteLine("Pass (optional, thrown) - " + basename + " - " + Benchmarker.RenderException(e));
                    }
                    continue;
                }

                if (expectSuccess) {
                    if(showPass) Console.WriteLine("Pass (expected success) - " + basename);
                } else if (expectFailure) {
                    Console.WriteLine("** FAILED ** (expected error) - " + basename);
                    Console.WriteLine(content);
                } else {
                    if(showPass) Console.WriteLine("Pass (optional) - " + basename);
                }
            }

        }
         
         private static void fail(string desc, string reason) {
             Console.WriteLine("FAILED: " + desc);
             Console.WriteLine("- " + reason);
         }

         private static void pass(string desc, string info = "") {
             if (info == "") {
                 Console.WriteLine("pass: " + desc);
             } else {
                 Console.WriteLine("pass: " + desc + " -- " + info);
             }
         }
         public static void ExpectException(string desc, Action fn, string expectMessageContains) {
             try {
                 fn();
             } catch (Exception e) {
                 List<string> messages = new List<string>();
                 while (true) {
                     messages.Add(e.GetType().Name + ": " + e.Message);
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

    }
}