using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 커맨드라인 빌드: Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.BuildWindows -buildPath &lt;폴더&gt;
/// Build Settings에 켜진 씬을 순서대로 사용 (첫 씬 = Title).
/// </summary>
public static class BuildScript
{
    [MenuItem("DebugRush/Build Windows 64bit")]
    public static void BuildWindows()
    {
        string dir = GetArg("-buildPath") ?? Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows");
        Directory.CreateDirectory(dir);

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(dir, "DebugRush.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[BuildScript] {report.summary.result} → {options.locationPathName} ({report.summary.totalErrors} errors)");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
