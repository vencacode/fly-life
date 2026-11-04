using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BridgeScratch
{
    public static string Run()
    {
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Scenes/Bootstrap.unity",
                    "Assets/Scenes/MainMap.unity",
                    "Assets/Scenes/Battle.unity",
                    "Assets/Scenes/ClanScreen.unity"
                },
                locationPathName = "Builds/flylife-firstplayable.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            System.IO.Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
                return $"BUILD OK: {summary.outputPath} ({summary.totalSize / (1024*1024)}MB)";
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"BUILD FAILED: {summary.result} ({summary.totalErrors} errors)");
                foreach (var step in report.steps)
                    foreach (var msg in step.messages)
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            sb.AppendLine($"  [{step.name}] {msg.content}");
                return sb.ToString();
            }
        }
        catch (System.Exception e)
        {
            return $"EXCEPTION: {e.Message}\n{e.StackTrace}";
        }
    }
}
