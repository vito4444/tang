using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lingyan.EditorTools
{
    /// <summary>
    /// 一键构建入口。命令行：
    ///   Unity -batchmode -nographics -quit -projectPath LingyanUnity
    ///         -buildTarget Win64 -executeMethod Lingyan.EditorTools.BuildScript.BuildWindows
    /// 输出：仓库根 Builds/Win64/Lingyan/Lingyan.exe；失败以非零码退出。
    /// </summary>
    public static class BuildScript
    {
        private const string OutputRelative = "../Builds/Win64/Lingyan/Lingyan.exe";

        [MenuItem("凌烟/Build Windows x64")]
        public static void BuildWindowsMenu()
        {
            BuildResult result = RunWindowsBuild();
            Debug.Log("[Build] 结果: " + result);
        }

        /// <summary>批处理入口：构建失败进程退出码非零。</summary>
        public static void BuildWindows()
        {
            BuildResult result = RunWindowsBuild();
            EditorApplication.Exit(result == BuildResult.Succeeded ? 0 : 1);
        }

        private static BuildResult RunWindowsBuild()
        {
            ApplyPlayerSettings();

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                // 构建场景表意外为空时兜底到启动场景，不让构建静默产出空包
                scenes = new[] { "Assets/Scenes/Boot.unity" };
            }

            string output = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", OutputRelative));
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[Build] " + summary.result
                + " 输出=" + output
                + " 用时=" + summary.totalTime.TotalSeconds.ToString("F0") + "s"
                + " 错误=" + summary.totalErrors);
            return summary.result;
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Lingyan Project";
            PlayerSettings.productName = "Lingyan";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = false;
        }
    }
}
