using Newtonsoft.Json.Linq;

namespace Lingyan.Core.Saves.Migrations
{
    /// <summary>
    /// v1 → v2：阶段 3 引入 NPC 社交状态与通缉值。
    /// 老档没见过任何 NPC、无案底：补空结构与零值是语义正确的默认，
    /// 不属于"静默补齐关键进度"——这两个字段在 v1 时代根本不存在。
    /// </summary>
    public sealed class V1ToV2 : ISaveMigration
    {
        public int FromVersion { get { return 1; } }

        public JObject Apply(JObject save)
        {
            save["schemaVersion"] = 2;
            if (save["wantedLevel"] == null)
            {
                save["wantedLevel"] = 0;
            }
            if (save["npcStates"] == null)
            {
                save["npcStates"] = new JObject();
            }
            return save;
        }
    }
}
