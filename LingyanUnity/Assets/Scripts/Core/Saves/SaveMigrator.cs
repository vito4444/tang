using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lingyan.Core.Saves
{
    /// <summary>把 vN 的存档 JSON 原地升到 vN+1。</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }

        JObject Apply(JObject save);
    }

    /// <summary>
    /// 存档读写与版本迁移链。
    /// 契约：
    ///   1. 版本比当前新 → SaveVersionTooNewException；
    ///   2. 版本旧且缺迁移器 → SaveMigrationMissingException；
    ///   3. 迁移后仍缺关键字段/值非法 → SaveCorruptException；
    ///   4. 任何情况下都不得以默认值静默补齐关键字段。
    /// </summary>
    public sealed class SaveMigrator
    {
        private readonly Dictionary<int, ISaveMigration> _migrations =
            new Dictionary<int, ISaveMigration>();

        public SaveMigrator(IEnumerable<ISaveMigration> migrations)
        {
            foreach (ISaveMigration migration in migrations)
            {
                _migrations[migration.FromVersion] = migration;
            }
        }

        /// <summary>带全部已知迁移器的默认实例。</summary>
        public static SaveMigrator CreateDefault()
        {
            return new SaveMigrator(new ISaveMigration[]
            {
                new Migrations.V0ToV1(),
                new Migrations.V1ToV2()
            });
        }

        public string Serialize(SaveData data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public SaveData Load(string json)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (Exception e)
            {
                throw new SaveCorruptException("存档不是合法 JSON: " + e.Message);
            }

            int version = ReadVersion(root);

            if (version > SaveData.CurrentVersion)
            {
                throw new SaveVersionTooNewException(version);
            }

            while (version < SaveData.CurrentVersion)
            {
                if (!_migrations.TryGetValue(version, out ISaveMigration migration))
                {
                    throw new SaveMigrationMissingException(version);
                }
                root = migration.Apply(root);
                int after = ReadVersion(root);
                if (after != version + 1)
                {
                    throw new SaveCorruptException(
                        "迁移器 v" + version + " 未把版本推进到 v" + (version + 1));
                }
                version = after;
            }

            SaveData data;
            try
            {
                data = root.ToObject<SaveData>(JsonSerializer.Create(new JsonSerializerSettings
                {
                    // 未知字段容忍（向前兼容），缺失字段由 Required 特性拦截。
                    MissingMemberHandling = MissingMemberHandling.Ignore
                }));
            }
            catch (JsonSerializationException e)
            {
                throw new SaveCorruptException("存档缺字段或类型不符: " + e.Message);
            }

            SaveValidator.Validate(data);
            return data;
        }

        private static int ReadVersion(JObject root)
        {
            JToken token = root["schemaVersion"] ?? root["version"];
            if (token == null || token.Type != JTokenType.Integer)
            {
                throw new SaveCorruptException("存档缺版本号（schemaVersion/version）");
            }
            return token.Value<int>();
        }
    }
}
