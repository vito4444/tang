using System.IO;
using Lingyan.Core.Saves;
using UnityEngine;

namespace Lingyan.Game.Services
{
    /// <summary>
    /// 存档文件读写。写档先落 .tmp 再替换正档，旧档滚为 .bak；
    /// 读档一律走迁移链与严格校验，坏档抛 SaveException 由界面呈现，绝不静默。
    /// </summary>
    public sealed class SaveService
    {
        private readonly SaveMigrator _migrator = SaveMigrator.CreateDefault();

        private static string SaveDir
        {
            get { return Path.Combine(Application.persistentDataPath, "saves"); }
        }

        private static string SlotPath
        {
            get { return Path.Combine(SaveDir, "slot_1.json"); }
        }

        public bool HasSave { get { return File.Exists(SlotPath); } }

        public void Write(SaveData data)
        {
            Directory.CreateDirectory(SaveDir);
            string json = _migrator.Serialize(data);
            string tmp = SlotPath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(SlotPath))
            {
                string bak = SlotPath + ".bak";
                if (File.Exists(bak)) { File.Delete(bak); }
                File.Move(SlotPath, bak);
            }
            File.Move(tmp, SlotPath);
            Debug.Log("[Lingyan] 存档写入: " + SlotPath);
        }

        /// <summary>读档。坏档、版本问题一律抛 SaveException（带本地化 ReasonKey）。</summary>
        public SaveData Load()
        {
            string json = File.ReadAllText(SlotPath);
            return _migrator.Load(json);
        }
    }
}
