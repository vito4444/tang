using System;

namespace Lingyan.Core.Saves
{
    /// <summary>存档异常基类。ReasonKey 为本地化键，UI 直接显示给玩家。</summary>
    public abstract class SaveException : Exception
    {
        public string ReasonKey { get; }

        protected SaveException(string reasonKey, string detail)
            : base(detail)
        {
            ReasonKey = reasonKey;
        }
    }

    /// <summary>文件不是合法 JSON、缺关键字段、或字段值非法。</summary>
    public sealed class SaveCorruptException : SaveException
    {
        public SaveCorruptException(string detail)
            : base("save.error.corrupt", detail) { }
    }

    /// <summary>存档版本比当前程序新（玩家回退了游戏版本）。</summary>
    public sealed class SaveVersionTooNewException : SaveException
    {
        public int FoundVersion { get; }

        public SaveVersionTooNewException(int found)
            : base("save.error.too_new", "存档 schemaVersion=" + found + "，当前程序仅支持到 " + SaveData.CurrentVersion)
        {
            FoundVersion = found;
        }
    }

    /// <summary>迁移链断档：有旧版本存档却没有对应迁移器。这是程序错误，必须响亮失败。</summary>
    public sealed class SaveMigrationMissingException : SaveException
    {
        public int FromVersion { get; }

        public SaveMigrationMissingException(int fromVersion)
            : base("save.error.migration_missing", "缺少自 v" + fromVersion + " 起的迁移器")
        {
            FromVersion = fromVersion;
        }
    }
}
