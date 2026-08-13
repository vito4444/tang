using Newtonsoft.Json.Linq;

namespace Lingyan.Core.Saves.Migrations
{
    /// <summary>v3 → v4：阶段 10 引入市集与行囊。老档没买过东西，补空行囊。</summary>
    public sealed class V3ToV4 : ISaveMigration
    {
        public int FromVersion { get { return 3; } }

        public JObject Apply(JObject save)
        {
            save["schemaVersion"] = 4;
            if (save["inventory"] == null)
            {
                save["inventory"] = new JObject();
            }
            return save;
        }
    }
}
