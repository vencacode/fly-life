using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless Android build script. Run from CLI:
///   Unity.exe -batchmode -quit -executeMethod BuildAndroid.BuildAPK
///   Unity.exe -batchmode -quit -executeMethod BuildAndroid.BuildAAB
/// Or invoke from menu: Build > Android APK / Android AAB
/// </summary>
public static class BuildAndroid
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Bootstrap.unity",
        "Assets/Scenes/MainMap.unity",
        "Assets/Scenes/Battle.unity",
        "Assets/Scenes/ClanScreen.unity"
    };

    [MenuItem("Build/Android APK")]
    public static void BuildAPK()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Builds/flylife-firstplayable.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        RunBuild(options);
    }

    [MenuItem("Build/Android AAB")]
    public static void BuildAAB()
    {
        EditorUserBuildSettings.buildAppBundle = true;
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = "Builds/flylife-firstplayable.aab",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        RunBuild(options);
    }

    static void RunBuild(BuildPlayerOptions options)
    {
        // Ensure output directory exists
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(options.locationPathName));

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildAndroid] Build succeeded: {summary.outputPath} ({summary.totalSize / (1024 * 1024)}MB)");
        }
        else
        {
            Debug.LogError($"[BuildAndroid] Build failed: {summary.result} ({summary.totalErrors} errors)");
            // Exit with error code in batch mode
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
