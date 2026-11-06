using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using AndroidBuildSystem = UnityEditor.AndroidBuildSystem;

namespace Editor
{
    public class BuildScript
    {
        #region Android

        public static void BuildAndroid()
        {
            string[] args = Environment.GetCommandLineArgs();
            string buildType = GetArgument(args, "-buildType");

            bool isApk = string.Equals(buildType, "APK", StringComparison.OrdinalIgnoreCase);
            bool isAab = string.Equals(buildType, "AAB", StringComparison.OrdinalIgnoreCase);

            if (!isApk && !isAab)
            {
                Debug.LogError("BuildAndroid: Invalid -buildType. Use APK or AAB.");
                EditorApplication.Exit(1);
                return;
            }

            string productName = PlayerSettings.productName;
            string extension = isAab ? "aab" : "apk";
            string outputPath = isAab
                ? $"Builds/AndroidAAB/{productName}.{extension}"
                : $"Builds/AndroidAPK/{productName}.{extension}";


            EnsureDirectoryForFile(outputPath);

            EditorUserBuildSettings.buildAppBundle = isAab;

            SetAndroidSdkLevels();

            ApplyAndroidSigningFromEnvironment();


            ApplyVersionCodeFromEnvironment();

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                // APK is the tester build: Development turns the cheat panel on. AAB stays a clean release build.
                options = isApk ? BuildOptions.Development : BuildOptions.None
            };

            //UnityEditor.Android.UserBuildSettings.DebugSymbols.level = DebugSymbolLevel.None;
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"BuildAndroid failed: {report.summary.result}");
                EditorApplication.Exit(10);
                return;
            }

            Debug.Log($"BuildAndroid succeeded: {outputPath}");
            EditorApplication.Exit(0);
        }

        private static void ApplyAndroidSigningFromEnvironment()
        {
            string keystoreFile = Environment.GetEnvironmentVariable("ITIC_GAMES_KEYSTORE_FILE");
            string keystorePass = Environment.GetEnvironmentVariable("ITIC_GAMES_KEYSTORE_PASS");
            string aliasName = Environment.GetEnvironmentVariable("ITIC_GAMES_ALIAS");
            string aliasPass = Environment.GetEnvironmentVariable("ITIC_GAMES_ALIAS_PAS");

            Debug.Log($"[DEBUG] Keystore Path: {keystoreFile}");
            Debug.Log($"[DEBUG] Alias Name Length: {aliasName.Length}");
            Debug.Log($"[DEBUG] Alias Pass Length: {aliasPass.Length}");
            if (string.IsNullOrWhiteSpace(keystoreFile) || !File.Exists(keystoreFile))
            {
                FailFast("ITIC_GAMES_KEYSTORE_FILE is missing or file does not exist.");
                return;
            }

            if (string.IsNullOrWhiteSpace(keystorePass))
            {
                FailFast("ITIC_GAMES_KEYSTORE_PASS is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(aliasName))
            {
                FailFast("ITIC_GAMES_ALIAS is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(aliasPass))
            {
                FailFast("ITIC_GAMES_ALIAS_PAS is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(keystoreFile) ||
                string.IsNullOrWhiteSpace(keystorePass) ||
                string.IsNullOrWhiteSpace(aliasName) ||
                string.IsNullOrWhiteSpace(aliasPass))
            {
                Debug.LogError(
                    "BuildAndroid: Missing signing variables. " +
                    "Expected ITIC_GAMES_KEYSTORE_FILE, ITIC_GAMES_KEYSTORE_PASS, ITIC_GAMES_ALIAS, ITIC_GAMES_ALIAS_PAS."
                );
                EditorApplication.Exit(2);
                return;
            }

            if (!File.Exists(keystoreFile))
            {
                Debug.LogError($"BuildAndroid: Keystore file not found: {keystoreFile}");
                EditorApplication.Exit(3);
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystoreFile.Trim();
            PlayerSettings.Android.keystorePass = keystorePass.Trim();
            PlayerSettings.Android.keyaliasName = aliasName.Trim();
            PlayerSettings.Android.keyaliasPass = aliasPass.Trim();
        }

        private static void FailFast(string message)
        {
            Debug.LogError($"BuildAndroid failed: {message}");
            EditorApplication.Exit(3);
        }

        private static void ApplyVersionCodeFromEnvironment()
        {
            string versionCodeRaw =
                Environment.GetEnvironmentVariable("Butterfly_Match_BUILD_NUMBER") ??
                Environment.GetEnvironmentVariable("BUILD_NUMBER");

            if (string.IsNullOrWhiteSpace(versionCodeRaw))
            {
                PlayerSettings.Android.bundleVersionCode = 1;
                return;
            }

            if (!int.TryParse(versionCodeRaw, out int versionCode) || versionCode < 1)
            {
                Debug.LogError($"BuildAndroid: Invalid version code: {versionCodeRaw}");
                EditorApplication.Exit(4);
                return;
            }

            PlayerSettings.Android.bundleVersionCode = versionCode;
        }

        private static void SetAndroidSdkLevels()
        {
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;

            try
            {
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
            }
            catch
            {
                Debug.LogWarning("BuildAndroid: AndroidApiLevel35 enum not available in this Unity version. Leaving targetSdkVersion unchanged.");
            }

            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        }

        private static void EnsureDirectoryForFile(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        #endregion

        #region Windows

        // public static void BuildWindows()
        // {

        //     string path = "Builds/Windows";

        //     CreateDirectory(path);

        //

        //     BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()

        //     {

        //         scenes = GetEnabledScenes(), target = BuildTarget.StandaloneWindows64, locationPathName = $"path/{PlayerSettings.productName}.exe", options = BuildOptions.None

        //     };

        //     BuildPipeline.BuildPlayer(buildPlayerOptions);

        //     ZipBuild(path);

        // }

        #endregion

        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        private static void ZipBuild(string buildPath)
        {
            string zipPath = $"{buildPath}.zip";

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(buildPath, zipPath);
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] == name && index + 1 < args.Length)
                {
                    return args[index + 1];
                }
            }

            return null;
        }
    }
}