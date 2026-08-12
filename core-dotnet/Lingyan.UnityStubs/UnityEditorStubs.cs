// UnityEditor API 桩，仅供编译检查。
using System;
using UnityEngine;

namespace UnityEditor
{
    public static class EditorApplication
    {
        public static bool isPlaying { get; set; }
        public static void Exit(int returnValue) { }
    }

    public class EditorBuildSettingsScene
    {
        public string path { get; set; }
        public bool enabled { get; set; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; } =
            Array.Empty<EditorBuildSettingsScene>();
    }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static string bundleVersion { get; set; }
        public static int defaultScreenWidth { get; set; }
        public static int defaultScreenHeight { get; set; }
        public static bool runInBackground { get; set; }

        public static class SplashScreen
        {
            public static bool show { get; set; }
        }
    }

    public enum BuildTarget
    {
        StandaloneWindows64 = 19,
        StandaloneLinux64 = 24,
        StandaloneOSX = 2
    }

    [Flags]
    public enum BuildOptions
    {
        None = 0,
        Development = 1
    }

    public struct BuildPlayerOptions
    {
        public string[] scenes { get; set; }
        public string locationPathName { get; set; }
        public BuildTarget target { get; set; }
        public BuildOptions options { get; set; }
    }

    public static class BuildPipeline
    {
        public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options)
        {
            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
    }
}

namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown = 0, Succeeded = 1, Failed = 2, Cancelled = 3 }

    public struct BuildSummary
    {
        public BuildResult result { get; set; }
        public TimeSpan totalTime { get; set; }
        public int totalErrors { get; set; }
    }

    public class BuildReport : UnityEngine.Object
    {
        public BuildSummary summary { get { return default; } }
    }
}
