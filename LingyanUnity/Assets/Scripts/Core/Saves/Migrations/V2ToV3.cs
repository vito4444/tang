using Newtonsoft.Json.Linq;

namespace Lingyan.Core.Saves.Migrations
{
    /// <summary>v2 → v3：阶段 5 引入案件进度。老档未接任何案，补空表。</summary>
    public sealed class V2ToV3 : ISaveMigration
    {
        public int FromVersion { get { return 2; } }

        public JObject Apply(JObject save)
        {
            save["schemaVersion"] = 3;
            if (save["cases"] == null)
            {
                save["cases"] = new JObject();
            }
            if (save["kaokeGrades"] == null)
            {
                save["kaokeGrades"] = new JArray();
            }
            if (save["housing"] == null)
            {
                save["housing"] = "hut";
            }
            if (save["codex"] == null)
            {
                save["codex"] = new JArray();
            }
            return save;
        }
    }
}
