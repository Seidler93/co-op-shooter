using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWindows
{
    // Builds to: co-op-shooter/builds/windows/CoopShooter.exe
    public static void Build()
    {
        // Unity project: co-op-shooter/game/CoopShooter
        // We want repo root: ../../
        var projectRoot = Directory.GetParent(Application.dataPath)!.FullName; // .../game/CoopShooter
        var repoRoot = Directory.GetParent(Directory.GetParent(projectRoot)!.FullName)!.FullName; // .../co-op-shooter

        var outDir = Path.Combine(repoRoot, "builds", "windows");
        Directory.CreateDirectory(outDir);

        var exePath = Path.Combine(outDir, "CoopShooter.exe");

        // Use enabled scenes from Build Settings
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("No enabled scenes in Build Settings. Add at least one scene and enable it.");
            EditorApplication.Exit(1);
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log($"Building Windows x64 to: {exePath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.result}. Errors: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Build succeeded!");
        EditorApplication.Exit(0);
    }
}