using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildProject
{
    private const string BuildOutputArgument = "-buildOutput";
    private const string BuildNameArgument = "-buildName";
    private const string DevelopmentBuildArgument = "-developmentBuild";

    private enum BuildArtifactKind
    {
        File,
        Folder
    }

    [MenuItem("Tools/Build/Build Windows x64")]
    public static void BuildWindows64()
    {
        ExecuteBuild(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Windows64", ".exe", BuildArtifactKind.File);
    }

    [MenuItem("Tools/Build/Build Android APK")]
    public static void BuildAndroidApk()
    {
        ExecuteBuild(BuildTarget.Android, BuildTargetGroup.Android, "AndroidApk", ".apk", BuildArtifactKind.File, false);
    }

    [MenuItem("Tools/Build/Build Android AAB")]
    public static void BuildAndroidAab()
    {
        ExecuteBuild(BuildTarget.Android, BuildTargetGroup.Android, "AndroidAab", ".aab", BuildArtifactKind.File, true);
    }

    [MenuItem("Tools/Build/Build WebGL")]
    public static void BuildWebGL()
    {
        ExecuteBuild(BuildTarget.WebGL, BuildTargetGroup.WebGL, "WebGL", string.Empty, BuildArtifactKind.Folder);
    }

    [MenuItem("Tools/Build/Build Active Target")]
    public static void BuildActiveTarget()
    {
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows64:
                BuildWindows64();
                break;
            case BuildTarget.Android:
                BuildAndroidApk();
                break;
            case BuildTarget.WebGL:
                BuildWebGL();
                break;
            default:
                throw new BuildFailedException($"暂未为当前平台提供标准构建入口: {EditorUserBuildSettings.activeBuildTarget}");
        }
    }

    private static void ExecuteBuild(
        BuildTarget target,
        BuildTargetGroup targetGroup,
        string outputFolderName,
        string fileExtension,
        BuildArtifactKind artifactKind,
        bool buildAppBundle = false)
    {
        string[] scenes = GetEnabledScenes();
        string productName = GetBuildName();
        string outputPath = ResolveOutputPath(outputFolderName, productName, fileExtension, artifactKind);
        BuildOptions options = GetBuildOptions();

        EnsureOutputDirectory(outputPath, artifactKind);
        SwitchBuildTargetIfNeeded(targetGroup, target);

        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        DateTime buildStartedAt = DateTime.Now;

        try
        {
            if (target == BuildTarget.Android)
                EditorUserBuildSettings.buildAppBundle = buildAppBundle;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                targetGroup = targetGroup,
                options = options
            };

            Debug.Log(
                $"[BuildProject] Starting build\n" +
                $"Target: {target}\n" +
                $"Output: {outputPath}\n" +
                $"Scenes: {string.Join(", ", scenes)}\n" +
                $"Options: {options}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            WriteBuildSummary(report, outputPath, artifactKind, buildStartedAt, DateTime.Now, scenes, buildAppBundle);

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Build failed: {report.summary.result}. See build summary for details.");

            Debug.Log($"[BuildProject] Build succeeded: {outputPath}");
        }
        finally
        {
            if (target == BuildTarget.Android)
                EditorUserBuildSettings.buildAppBundle = previousAppBundle;
        }
    }

    private static string[] GetEnabledScenes()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .Where(File.Exists)
            .ToArray();

        if (scenes.Length == 0)
            throw new BuildFailedException("Build Settings 中没有可用的启用场景，无法构建。");

        return scenes;
    }

    private static string GetBuildName()
    {
        string explicitName = GetCommandLineValue(BuildNameArgument);
        string baseName = string.IsNullOrWhiteSpace(explicitName) ? PlayerSettings.productName : explicitName;
        return SanitizeFileName(baseName);
    }

    private static string ResolveOutputPath(string outputFolderName, string productName, string fileExtension, BuildArtifactKind artifactKind)
    {
        string explicitOutput = GetCommandLineValue(BuildOutputArgument);
        if (!string.IsNullOrWhiteSpace(explicitOutput))
        {
            explicitOutput = Path.GetFullPath(explicitOutput);

            if (artifactKind == BuildArtifactKind.Folder)
                return explicitOutput;

            if (string.Equals(Path.GetExtension(explicitOutput), fileExtension, StringComparison.OrdinalIgnoreCase))
                return explicitOutput;

            return Path.Combine(explicitOutput, productName + fileExtension);
        }

        string baseOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", outputFolderName);
        return artifactKind == BuildArtifactKind.Folder
            ? Path.Combine(baseOutputDirectory, productName)
            : Path.Combine(baseOutputDirectory, productName + fileExtension);
    }

    private static void EnsureOutputDirectory(string outputPath, BuildArtifactKind artifactKind)
    {
        string directoryPath = artifactKind == BuildArtifactKind.Folder
            ? outputPath
            : Path.GetDirectoryName(outputPath);

        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new BuildFailedException($"无法解析构建输出目录: {outputPath}");

        Directory.CreateDirectory(directoryPath);
    }

    private static void SwitchBuildTargetIfNeeded(BuildTargetGroup targetGroup, BuildTarget target)
    {
        if (EditorUserBuildSettings.activeBuildTarget == target)
            return;

        bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
        if (!switched)
            throw new BuildFailedException($"切换构建平台失败: {target}");
    }

    private static BuildOptions GetBuildOptions()
    {
        bool developmentBuild = HasCommandLineFlag(DevelopmentBuildArgument);
        return developmentBuild ? BuildOptions.Development : BuildOptions.None;
    }

    private static void WriteBuildSummary(
        BuildReport report,
        string outputPath,
        BuildArtifactKind artifactKind,
        DateTime startedAt,
        DateTime finishedAt,
        string[] scenes,
        bool buildAppBundle)
    {
        string summaryDirectory = artifactKind == BuildArtifactKind.Folder
            ? outputPath
            : Path.GetDirectoryName(outputPath);

        if (string.IsNullOrWhiteSpace(summaryDirectory))
            return;

        Directory.CreateDirectory(summaryDirectory);

        BuildSummary summary = report.summary;
        StringBuilder content = new StringBuilder(512);
        content.AppendLine("Build Summary");
        content.AppendLine($"ProductName: {PlayerSettings.productName}");
        content.AppendLine($"BundleVersion: {PlayerSettings.bundleVersion}");
        content.AppendLine($"Target: {summary.platform}");
        content.AppendLine($"Result: {summary.result}");
        content.AppendLine($"OutputPath: {outputPath}");
        content.AppendLine($"TotalSizeBytes: {summary.totalSize}");
        content.AppendLine($"TotalTimeSeconds: {summary.totalTime.TotalSeconds:F2}");
        content.AppendLine($"TotalWarnings: {summary.totalWarnings}");
        content.AppendLine($"TotalErrors: {summary.totalErrors}");
        content.AppendLine($"StartedAt: {startedAt:yyyy-MM-dd HH:mm:ss}");
        content.AppendLine($"FinishedAt: {finishedAt:yyyy-MM-dd HH:mm:ss}");
        content.AppendLine($"DevelopmentBuild: {summary.options.HasFlag(BuildOptions.Development)}");
        if (summary.platform == BuildTarget.Android)
            content.AppendLine($"AndroidAppBundle: {buildAppBundle}");
        content.AppendLine("Scenes:");
        foreach (string scene in scenes)
            content.AppendLine($"- {scene}");

        string summaryPath = Path.Combine(summaryDirectory, "build-summary.txt");
        File.WriteAllText(summaryPath, content.ToString(), Encoding.UTF8);
        Debug.Log($"[BuildProject] Build summary written to: {summaryPath}");
    }

    private static string GetCommandLineValue(string argumentName)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], argumentName, StringComparison.OrdinalIgnoreCase))
                continue;

            return args[i + 1];
        }

        return null;
    }

    private static bool HasCommandLineFlag(string argumentName)
    {
        string[] args = Environment.GetCommandLineArgs();
        return args.Any(arg => string.Equals(arg, argumentName, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Build";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        StringBuilder sanitized = new StringBuilder(value.Length);
        foreach (char character in value)
            sanitized.Append(invalidChars.Contains(character) ? '_' : character);

        return sanitized.ToString().Trim();
    }
}
