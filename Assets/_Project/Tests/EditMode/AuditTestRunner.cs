using System.IO;
using System.Text;
using ArrowMaze.Core;
using ArrowMaze.Data;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        [MenuItem("Audit/Capture Level Previews")]
        public static void CaptureLevelPreviews()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay.unity");
            var gridManager = Object.FindFirstObjectByType<GridManager>();
            var cam = Camera.main;
            if (gridManager == null || cam == null)
            {
                Debug.LogError("GridManager or Camera not found in Gameplay scene");
                return;
            }

            var artifactDir = "/Users/aryankinha/.gemini/antigravity-ide/brain/a5a2b4f5-0157-4fe3-9180-9d90147baba7";
            int[] levelsToCapture = { 5, 10, 23 };

            foreach (var levelId in levelsToCapture)
            {
                var def = LevelCatalog.Get(levelId);
                var level = def.BuildLevel();
                gridManager.BuildLevel(level);

                var rt = new RenderTexture(1080, 1920, 24);
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prevRt;

                RenderTexture.active = rt;
                var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                var bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(rt);

                var path = Path.Combine(artifactDir, $"level_{levelId}_topology.png");
                File.WriteAllBytes(path, bytes);
                Debug.Log($"Captured level preview: {path}");
            }
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
