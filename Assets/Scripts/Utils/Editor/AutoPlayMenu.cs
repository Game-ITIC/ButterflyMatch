using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Utils.DebugTools;

namespace Utils.Editor
{
    /// <summary>
    /// Menu for AutoPlayBot: plays levels in the real game scene and writes Logs/AutoPlay/results.csv.
    /// Back up PlayerPrefs first: the bot wins/loses real levels (OpenLevel, stars, lives change).
    /// </summary>
    public static class AutoPlayMenu
    {
        private const string Root = "Tools/JigsawMatch2/AutoPlay/";

        [MenuItem(Root + "Verify all levels (1-300, 2 runs)")]
        private static void VerifyAll() => Run("1-300", 2, false);

        [MenuItem(Root + "Verify generated levels (46-300, 1 run)")]
        private static void VerifyGenerated() => Run("46-300", 1, false);

        [MenuItem(Root + "Measure generated levels (for calibrate.py)")]
        private static void Measure() => Run("46-300", 2, true);

        [MenuItem(Root + "Stop")]
        private static void Stop()
        {
            EditorPrefs.SetBool(AutoPlayBot.KeyEnabled, false);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void Run(string levels, int runs, bool measure)
        {
            if (EditorApplication.isPlaying)
            {
                UnityEngine.Debug.LogWarning("[AutoPlay] Stop Play mode first.");
                return;
            }

            if (!EditorUtility.DisplayDialog("AutoPlay",
                    $"Play levels {levels} x{runs} with the bot?\nIt changes PlayerPrefs (progress, stars, lives).", "Run", "Cancel"))
            {
                return;
            }

            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "AutoPlay",
                System.DateTime.Now.ToString("yyyyMMdd_HHmm")));
            EditorPrefs.SetBool(AutoPlayBot.KeyEnabled, true);
            EditorPrefs.SetBool(AutoPlayBot.KeyMeasure, measure);
            EditorPrefs.SetString(AutoPlayBot.KeyLevels, levels);
            EditorPrefs.SetInt(AutoPlayBot.KeyRuns, runs);
            EditorPrefs.SetFloat(AutoPlayBot.KeyTimeScale, 6f);
            EditorPrefs.SetString(AutoPlayBot.KeyOut, outDir);
            UnityEngine.Debug.Log("[AutoPlay] Results: " + outDir);

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/_Scenes/Bootstrap.unity");
                EditorApplication.isPlaying = true;
            }
        }
    }
}
