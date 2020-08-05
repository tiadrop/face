using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using Lantern.Face.Json;

namespace face.demo {
    
    public class Benchmarker {
        public delegate void BenchmarkAction(object data);

        public class Test {
            public delegate void TestAction(BenchmarkAction participantAction);
            public string Remark;
            public TestAction Action;
        }

        private class PlacementComparer : IComparer<KeyValuePair<string, TestResult>> {
            public int Compare(KeyValuePair<string, TestResult> x, KeyValuePair<string, TestResult> y) {
                if (x.Value.Exception != null) {
                    if (y.Value.Exception != null) return 0;
                    return 1;
                }

                if (y.Value.Exception != null) return -1;
                return x.Value.score > y.Value.score ? -1 : 1;
            }
        }
        
        public readonly Dictionary<string, BenchmarkAction> Participants = new Dictionary<string, BenchmarkAction>();
        public readonly Dictionary<string, Test> Tests = new Dictionary<string, Test>();

        public int TimeLimitMs = 1000;
        public string Name;

        public Benchmarker(string name = "unnamed") {
            Name = name;
        }

        public JsValue PreviousData = JsValue.Null;
        
        public void Run() {
            //var result = new StringBuilder();
            var newData = new Dictionary<string, JsValue>();
            var longestNameWidth = Participants.Select(pair => pair.Key.Length).Aggregate(0, (len, seed) => seed > len ? seed : len);
            Console.WriteLine($"▎Running benchmark '{Name}' ┃ T >= {TimeLimitMs}ms ┃ {DateTime.Now.ToShortDateString()}");
            
            foreach (var testPair in Tests) {
                var (testRef, test) = testPair;
                var previousTestData = (PreviousData != null && PreviousData.ContainsKey(testRef)) ? PreviousData[testRef] : null;
                Console.WriteLine();
                Console.WriteLine($"┍━ {test.Remark}:");
                var resultsList = runTest(test.Action).ToList();
                resultsList.Sort(new PlacementComparer());
                var testData = new Dictionary<string, JsValue>();
                int placementCellWidth = resultsList.Count.ToString().Length + resultsList.Count - 1;

                for (var i = 0; i < resultsList.Count; i++) {
                    var (name, testResult) = resultsList[i];
                    if (testResult.Exception != null) {
                        Console.WriteLine($"┝#{"N/A".PadRight(placementCellWidth, ' ')}| {name.PadRight(longestNameWidth)} | {testResult.Exception.GetType()} {testResult.Exception.Message}");
                        continue;
                    }
                    var placement = (i + 1);
                    var participantData = new Dictionary<string, JsValue> {
                        ["placement"] = placement,
                        ["score"] = testResult.score
                    };
                    testData[name] = participantData;
                    var previousParticipantData = previousTestData == null ? null : previousTestData[name];

                    string placementDelta = "";
                    string scoreDelta = "";
                    if (previousParticipantData != null) {
                        placementDelta = getPlacementChange(previousParticipantData["placement"], placement);
                        scoreDelta = getScoreChangePercent(previousParticipantData["score"], testResult.score);
                    }

                    var paddedPlacement = $"{placement}{placementDelta}".PadRight(placementCellWidth, ' ');
                    Console.WriteLine($"┝#{paddedPlacement}│ {name.PadRight(longestNameWidth)} │ {testResult.score * 1000:N}/s{scoreDelta}");
                }

                newData[testRef] = testData;
            }

            PreviousData = newData;
        }

        private static string getScoreChangePercent(double previous, double score) {
            var newMultiplier = score / previous;
            double percentChange;
            char symbol;
            if (newMultiplier > 1) {
                symbol = '+';
                percentChange = (newMultiplier - 1) * 100;
            } else {
                symbol = '-';
                percentChange = (1 - newMultiplier) * 100;
            }
            return $" ({symbol}{percentChange:F2}%)";
        }

        private static string getPlacementChange(int previous, int placement) {
            int placementDiff = previous - placement;
            var symbol = placementDiff > 0 ? '+' : '-';
            placementDiff = Math.Abs(placementDiff);
            return repeatChar(symbol, placementDiff);
        }

        private static string repeatChar(char c, int times) {
            var sb = new StringBuilder();
            sb.Append(c, times);
            return sb.ToString();
        } 

        private readonly Stopwatch stopwatch = new Stopwatch();

        private class TestResult {
            public double score;
            public Exception Exception;

            public TestResult(Exception e) {
                Exception = e;
            }

            public TestResult(double score) {
                this.score = score;
            }
        }

        private Dictionary<string, TestResult> runTest(Test.TestAction testAction) {
            var result = new Dictionary<string, TestResult>();
            foreach (var (name, action) in Participants) {
                result[name] = runParticipant(action, testAction);
            }
            return result;
        }

        private TestResult runParticipant(BenchmarkAction action, Test.TestAction testAction) {
            GC.Collect();
            var iterations = 0;
            stopwatch.Restart();
            try {
                while (stopwatch.Elapsed.TotalMilliseconds < TimeLimitMs) {
                    testAction(action);
                    iterations++;
                }

                var timeTaken = stopwatch.Elapsed.TotalMilliseconds;
                return new TestResult(iterations / timeTaken);
            } catch (Exception e) {
                return new TestResult(e);
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

        
        
    }
}