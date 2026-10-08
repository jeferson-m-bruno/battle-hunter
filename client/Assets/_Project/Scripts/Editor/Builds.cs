using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BattleHunter.Client.Editor
{
    /// <summary>
    /// Builds reproduzíveis pelo menu ou em batch:
    ///   Unity.exe -batchmode -quit -projectPath client -executeMethod BattleHunter.Client.Editor.Builds.Windows
    /// Saída em client/Builds/&lt;plataforma&gt;/ (ignorado pelo git).
    /// </summary>
    public static class Builds
    {
        private const string OutputRoot = "Builds";

        [MenuItem("Battle Hunter/Build Windows")]
        public static void Windows()
        {
            Build(BuildTarget.StandaloneWindows64, Path.Combine(OutputRoot, "Windows", "BattleHunter.exe"));
        }

        [MenuItem("Battle Hunter/Build Android")]
        public static void Android()
        {
            Build(BuildTarget.Android, Path.Combine(OutputRoot, "Android", "BattleHunter.apk"));
        }

        private static void Build(BuildTarget target, string output)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                ProjectSetup.CreateScenes();
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} erro(s), {summary.totalTime.TotalSeconds:F0} s -> {output}");

            if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
