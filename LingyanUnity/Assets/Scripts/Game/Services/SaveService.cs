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

        private static string AutoPath
        {
            get { return Path.Combine(SaveDir, "autosave.json"); }
        }

        public bool HasSave
        {
            get { return File.Exists(SlotPath) || File.Exists(AutoPath); }
        }

        public void Write(SaveData data)
        {
            WriteTo(SlotPath, data);
        }

        /// <summary>自动存档（时辰推进、结案、考课、支俸等节点触发），独立槽。</summary>
        public void WriteAuto(SaveData data)
        {
            WriteTo(AutoPath, data);
        }

        private void WriteTo(string path, SaveData data)
        {
            Directory.CreateDirectory(SaveDir);
            string json = _migrator.Serialize(data);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path))
            {
                string bak = path + ".bak";
                if (File.Exists(bak)) { File.Delete(bak); }
                File.Move(path, bak);
            }
            File.Move(tmp, path);
            Debug.Log("[Lingyan] 存档写入: " + path);
        }

        /// <summary>
        /// 读档：手动槽与自动槽取较新者。
        /// 坏档、版本问题一律抛 SaveException（带本地化 ReasonKey），绝不静默。
        /// </summary>
        public SaveData Load()
        {
            string path = NewestPath();
            string json = File.ReadAllText(path);
            return _migrator.Load(json);
        }

        private static string NewestPath()
        {
            bool hasSlot = File.Exists(SlotPath);
            bool hasAuto = File.Exists(AutoPath);
            if (hasSlot && hasAuto)
            {
                return File.GetLastWriteTimeUtc(AutoPath) > File.GetLastWriteTimeUtc(SlotPath)
                    ? AutoPath
                    : SlotPath;
            }
            return hasAuto ? AutoPath : SlotPath;
        }
    }
}
