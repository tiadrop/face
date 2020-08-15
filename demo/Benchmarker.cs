using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Threading;
using Lantern.Face.Json;

namespace face.demo {
    
    public class Benchmarker {
        /// <summary>
        /// Represents the action each participant will run for every test
        /// </summary>
        /// <param name="data">User data as produced by each Test.Setup delegate</param>
        public delegate void BenchmarkAction(object data = null);

        public class Test {
            /// <summary>
            /// Represents an action to be timed for each participant.
            /// </summary>
            /// <param name="participantAction">A delegate which will be passed user data</param>
            /// <param name="data">User data as produced by each Test.Setup</param>
            public delegate void TestAction(BenchmarkAction participantAction, object data = null);
            public Func<object> Setup;
            public string Remark;
            /// <summary>
            /// Represents the action to be timed for each participant.
            /// If Action is null, the default action will pass user data to the participant's own action.
            /// </summary>
            public TestAction Action = null;
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

        public Benchmarker(string name = "") {
            Name = name;
        }

        public JsValue PreviousData = JsValue.Null;
        
        public delegate void WriteAction(string s, ConsoleColor colour = ConsoleColor.Gray);

        public void Run(WriteAction write = null) {
            if (write == null) write = (s, colour) => Console.WriteLine(s, colour);

            Action<(ConsoleColor, string)[]> writeColour = pairs => {
                foreach (var (colour, s) in pairs) {
                    write(s, colour);
                }
            };
            
            var newData = new Dictionary<string, JsValue>();
            var longestNameWidth = Participants.Select(pair => pair.Key.Length).Aggregate(0, (len, seed) => seed > len ? seed : len);

            writeColour(new[] {
                (ConsoleColor.Gray, "\n▎Running benchmark"),
                (ConsoleColor.Magenta, Name == "" ? "" : $" '{Name}'"),
                (ConsoleColor.Gray, " ┃ T >= "), (ConsoleColor.White, $"{TimeLimitMs}ms"),
                (ConsoleColor.Gray, " ┃ "), (ConsoleColor.DarkGray, $"{DateTime.Now.ToShortDateString()}\n")
            });
            
            Dictionary<string, List<double>> tally = new Dictionary<string, List<double>>();
            foreach (var participant in Participants.Keys) tally[participant] = new List<double>();
            
            foreach (var testPair in Tests) {
                var (testRef, test) = testPair;
                var previousTestData = (PreviousData != null && PreviousData.ContainsKey(testRef)) ? PreviousData[testRef] : null;
                write("\n┏━ ");
                write($"{test.Remark}:\n", ConsoleColor.Cyan);
                
                var resultsList = runTest(test).ToList();
                
                resultsList.Sort(new PlacementComparer());
                var testData = new Dictionary<string, JsValue>();
                int placementCellWidth = resultsList.Count.ToString().Length + resultsList.Count - 1;

                for (var i = 0; i < resultsList.Count; i++) {
                    var (name, testResult) = resultsList[i];
                    if (testResult.Exception != null) {
                        var e = testResult.Exception;
                        writeColour(new[] {
                            (ConsoleColor.Gray, "┣#"), (ConsoleColor.DarkRed, "N/A".PadRight(placementCellWidth, ' ')),
                            (ConsoleColor.Gray, "| "), (ConsoleColor.Magenta, name.PadRight(longestNameWidth)),
                            (ConsoleColor.Gray, " | "), (ConsoleColor.DarkRed, e.GetType().Name),
                            (ConsoleColor.DarkGray, ": "), (ConsoleColor.Red, e.Message)
                        });
                        while (e.InnerException != null) {
                            e = e.InnerException;
                            writeColour(new[] {
                                (ConsoleColor.DarkGray, " → "), (ConsoleColor.DarkRed, e.GetType().Name),
                                (ConsoleColor.DarkGray, ": "), (ConsoleColor.Red, e.Message)
                            });
                        }

                        write("\n");
                        continue;
                    }
                    tally[name].Add(testResult.score);
                    var placement = (i + 1);
                    var participantData = new Dictionary<string, JsValue> {
                        ["score"] = testResult.score,
                        ["placement"] = placement
                    };
                    testData[name] = participantData;
                    var previousParticipantData = (previousTestData == null || !previousTestData.ContainsKey(name)) ? null : previousTestData[name];

                    var placementColour = ConsoleColor.Gray;
                    string placementDelta = "";
                    string scoreDelta = "";
                    if (previousParticipantData != null) {
                        placementDelta = getPlacementChange(previousParticipantData["placement"], placement);
                        scoreDelta = getScoreChangePercent(previousParticipantData["score"], testResult.score);
                        if (previousParticipantData["placement"] != placement) {
                            placementColour = previousParticipantData["placement"] > placement
                                ? ConsoleColor.Green
                                : ConsoleColor.Red;
                        }
                    }

                    var paddedPlacement = $"{placement}{placementDelta}".PadRight(placementCellWidth, ' ');
                    writeColour(new[] {
                        (ConsoleColor.Gray, $"┣#"), (placementColour, paddedPlacement), (ConsoleColor.Gray, "| "),
                        (ConsoleColor.Magenta, name.PadRight(longestNameWidth)), (ConsoleColor.Gray, " | "),
                        (ConsoleColor.White, $"{testResult.score * 1000:N}"), (ConsoleColor.Gray, "/s")
                    });
                    if(previousParticipantData != null) write(scoreDelta,
                        previousParticipantData["score"] < testResult.score ? ConsoleColor.Green : ConsoleColor.Red);
                    write("\n");
                }
                newData[testRef] = testData;
            }

            if (PreviousData != null) {
                writeColour(new[] {
                    (ConsoleColor.Gray, "\n┏━ Average change:\n")
                });
                foreach (var (name, scores) in tally) {
                    var prevScores = new List<double>();
                    foreach (var (_, participantData) in PreviousData.ObjectValue) {
                        if (!participantData.ContainsKey(name))
                            continue; // this test doesn't contain a score for this participant
                        prevScores.Add(participantData[name]["score"]);
                    }
                    if(prevScores.Count == 0) continue;
                    
                    
                    var average = scores.Sum() / scores.Count;
                    var previousAverage = prevScores.Sum() / prevScores.Count;

                    writeColour(new[] {
                        (ConsoleColor.Gray, "┃ "), (ConsoleColor.Magenta, name.PadRight(longestNameWidth)),
                        (ConsoleColor.Gray, " |"),
                        (previousAverage > average ? ConsoleColor.Red : ConsoleColor.Green,
                            getScoreChangePercent(previousAverage, average)), (ConsoleColor.Gray, "\n")
                    });
                }
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
            return $" {symbol}{percentChange:F2}%";
        }

        private static string getPlacementChange(int previous, int placement) {
            int placementDiff = previous - placement;
            var symbol = placementDiff > 0 ? "⮭" : "⮯";
            placementDiff = Math.Abs(placementDiff);
            return repeatChar(symbol, placementDiff);
        }

        private static string repeatChar(string c, int times) {
            var sb = new StringBuilder();
            for (var i = 0; i < times; i++) sb.Append(c);
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

        private Dictionary<string, TestResult> runTest(Test test) {
            var result = new Dictionary<string, TestResult>();
            object data = null;
            if (test.Setup != null) data = test.Setup();
            var action = test.Action ?? ((run, d) => run(d));
            foreach (var (name, participantAction) in Participants) {
                result[name] = runParticipant(participantAction, action, data);
            }
            return result;
        }

        private TestResult runParticipant(BenchmarkAction action, Test.TestAction testAction, object data) {
            GC.Collect();
            var iterations = 0;
            stopwatch.Restart();
            try {
                while (stopwatch.Elapsed.TotalMilliseconds < TimeLimitMs) {
                    testAction(action, data);
                    iterations++;
                }

                var timeTaken = stopwatch.Elapsed.TotalMilliseconds;
                //Thread.Sleep(20);
                return new TestResult(iterations / timeTaken);
            } catch (Exception e) {
                return new TestResult(e);
            }
        }
        
        
    }
}