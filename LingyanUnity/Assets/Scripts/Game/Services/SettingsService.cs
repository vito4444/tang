using System.IO;
using Lingyan.Core.Settings;
using UnityEngine;

namespace Lingyan.Game.Services
{
    /// <summary>设置读写。设置损坏回默认（不承载进度，允许宽容）。</summary>
    public sealed class SettingsService
    {
        public GameSettings Current { get; private set; } = new GameSettings();

        private static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "settings.json"); }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    Current = GameSettings.ParseOrDefault(File.ReadAllText(FilePath));
                }
            }
            catch (IOException e)
            {
                Debug.LogWarning("[Lingyan] 设置读取失败，用默认值: " + e.Message);
                Current = new GameSettings();
            }
        }

        public void Save()
        {
            Current.ClampScale();
            File.WriteAllText(FilePath, Current.ToJson());
        }
    }
}
