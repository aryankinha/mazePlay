using System.IO;
using System.Text;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace ArrowMaze.Tests
{
    internal static class AuditTestRunner
    {
        private const string ResultPath = "Logs/audit-editmode-results.txt";

        [MenuItem("Audit/Run EditMode Tests")]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter { testMode = TestMode.EditMode };
            api.RegisterCallbacks(new WriterCallbacks(ResultPath));
            api.Execute(new ExecutionSettings(filter));
        }

        private sealed class WriterCallbacks : ICallbacks
        {
            private readonly string resultPath;
            private readonly StringBuilder failures = new StringBuilder();
            private int passed;
            private int failed;
            private int skipped;

            public WriterCallbacks(string resultPath)
            {
                this.resultPath = resultPath;
                File.WriteAllText(resultPath, "RUNNING\n");
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                File.WriteAllText(resultPath, "RUNNING\n");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"SUMMARY passed={passed} failed={failed} skipped={skipped} overall={result.ResultState}");
                sb.AppendLine("FAILURES:");
                sb.Append(failures.Length == 0 ? "(none)\n" : failures.ToString());
                File.WriteAllText(resultPath, sb.ToString());
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite)
                {
                    return;
                }

                if (result.FailCount > 0)
                {
                    failed++;
                    failures.AppendLine($"FAIL {result.FullName}");
                    failures.AppendLine($"  {result.Message}");
                    failures.AppendLine($"  {result.StackTrace}");
                }
                else if (result.ResultState == "Skipped")
                {
                    skipped++;
                }
                else
                {
                    passed++;
                }
            }
        }
    }
}
