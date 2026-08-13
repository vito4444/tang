using System;
using System.IO;

namespace Lingyan.Core.Tests
{
    /// <summary>
    /// 定位仓库内的数据文件。dotnet test 与 Unity Test Runner 的工作目录不同，
    /// 因而从多个起点向上探测，两种相对布局都试。
    /// </summary>
    public static class TestData
    {
        private static string _dataDir;

        public static string DataDir
        {
            get
            {
                if (_dataDir != null) { return _dataDir; }
                string[] roots = { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
                string[] layouts =
                {
                    Path.Combine("Assets", "Resources", "Data"),
                    Path.Combine("LingyanUnity", "Assets", "Resources", "Data")
                };
                foreach (string root in roots)
                {
                    DirectoryInfo dir = new DirectoryInfo(root);
                    while (dir != null)
                    {
                        foreach (string layout in layouts)
                        {
                            string candidate = Path.Combine(dir.FullName, layout);
                            if (File.Exists(Path.Combine(candidate, "strings.json")))
                            {
                                _dataDir = candidate;
                                return _dataDir;
                            }
                        }
                        dir = dir.Parent;
                    }
                }
                throw new InvalidOperationException("找不到数据目录（Assets/Resources/Data）");
            }
        }

        /// <summary>仓库内 Unity 工程根（…/LingyanUnity）。</summary>
        public static string UnityProjectRoot
        {
            get
            {
                // DataDir = <root>/Assets/Resources/Data
                return Path.GetFullPath(Path.Combine(DataDir, "..", "..", ".."));
            }
        }

        public static string StringsJson()
        {
            return File.ReadAllText(Path.Combine(DataDir, "strings.json"));
        }

        public static string GlossaryJson()
        {
            return File.ReadAllText(Path.Combine(DataDir, "glossary.json"));
        }
    }
}
