using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Lantern.Face.Json;

namespace face.demo {
    
    public static class JsonTest {
        public static void Run() {
            
            var bm = new Benchmarker("JSON");
            if (File.Exists(compareDataFilename))
                bm.PreviousData = JsValue.FromJson(File.ReadAllText(compareDataFilename));
            
            bm.TimeLimitMs =  3000;
            bm.Participants["Utf8Json"] = data => Utf8Json.JsonSerializer.Deserialize<object>(data as string);
            bm.Participants["System.Text"] = data => System.Text.Json.JsonSerializer.Deserialize<object>(data as string);
            bm.Participants["Newtonsoft"] = data => Newtonsoft.Json.JsonConvert.DeserializeObject<object>(data as string);
            bm.Participants["Face.Json"] = data => JsValue.FromJson(data as string);
            
            bm.Tests.Add("hardcoded_example", new Benchmarker.Test {
                Remark = "basic example test",
                Action = (run, data) => run("[2, 4,6, 8, null, 3.142]") // called for each bm.Participants["..."] as run, data is from Setup:
                // Setup: () => "eg data" // called once per Test before iterating participants
            });

            // load the rest from testidx.json
            JsValue[] testIndex = JsValue.FromJson(File.ReadAllText("demo/testidx.json"), true);
            foreach (var testSpec in testIndex) {
                if (testSpec.ContainsKey("filename") && !File.Exists(testSpec["filename"])) {
                    Console.WriteLine($"Missing test source '{testSpec["filename"]}'");
                    continue;
                }

                Func<object> setup;
                string testRef;
                string remark; 

                if (testSpec.IsString) {
                    var displayJson = testSpec.StringValue.Replace("\n", "␊").Replace("\r", "");
                    if (displayJson.Length > 24) displayJson = displayJson.Substring(0, 21) + "...";
                    remark = $"{formatFileSize(testSpec.StringValue.Length)} ━ '{displayJson}'";
                    setup = () => testSpec.StringValue;
                    testRef = testSpec.StringValue;

                } else if (testSpec.ContainsKey("filename")) {
                    string filename = testSpec["filename"];
                    var length = new FileInfo(filename).Length;
                    testRef = testSpec["filename"].StringValue.Split("/").Last();
                    setup = () => File.ReadAllText(filename);
                    remark = $"{formatFileSize(length)} ━ {testSpec.PropertyValueOr("remark", testRef)}";
                    
                } else {
                    string json = testSpec["json"];
                    var md5 = System.Security.Cryptography.MD5.Create();
                    testRef = string.Join("", md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json)).Select(b => $"{b:X}"));
                    setup = () => json;
                    var displayJson = json.Replace("\n", "␊").Replace("\r", "");
                    if (displayJson.Length > 24) displayJson = displayJson.Substring(0, 21) + "...";
                    remark =
                        $"{formatFileSize(json.Length)} ━ {testSpec.PropertyValueOr("remark", testRef.Substring(0, 6))} ━ '{displayJson}'";
                }
                bm.Tests[testRef] = new Benchmarker.Test {
                    Remark = remark,
                    //Action = (run, data) => run(data), // this is now the default action
                    Setup = setup // result passed to Action as data
                };
            }

            File.WriteAllText(logFilename, "");
            Thread.Sleep(1000);
            bm.Run((s, col) => {
                Console.ForegroundColor = col;
                Console.Write(s);
                var writer = File.AppendText(logFilename);
                writer.WriteAsync(s);
                writer.Close();
            });
            File.WriteAllText(compareDataFilename, bm.PreviousData.ToJson(true));
        }

        private const string logFilename = "jsonbenchmark.log";
        private const string compareDataFilename = "benchmarkStandard.json";

        private static string formatFileSize(long size) {
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
                
                try {
                    JsValue.FromJson(content);
                } catch (Exception e) {
                    if (expectSuccess) {
                        Console.WriteLine("** FAILED ** - " + basename + " - " + RenderException(e));
                    } else if (expectFailure) {
                        if(showPass) Console.WriteLine("Pass (expected error) - " + basename + " - " + RenderException(e));
                    } else {
                        if(showPass) Console.WriteLine("Pass (optional, thrown) - " + basename + " - " + RenderException(e));
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
        
        public static string RenderException(Exception e) {
            List<string> result = new List<string>{$"{e.GetType().Name}: {e.Message}"};
            while (e.InnerException != null) {
                e = e.InnerException;
                result.Add($"{e.GetType().Name}: {e.Message}");
            }
            return string.Join(" ⟶ ", result);
        }

        private static void assert(bool cond, string remark) {
            if(!cond) Console.WriteLine($"** ASSERTION FAILED ** {remark}");
        } 

        
    }
}