using Newtonsoft.Json.Linq;

namespace Lingyan.Core.Saves.Migrations
{
    /// <summary>
    /// v0（早期开发格式）→ v1。
    /// v0 特征：顶层 "version"，钱以贯为浮点（money_guan），
    /// 四维用拼音键（tili/shengming/liliang/zhihui），无官身、无账本、无剧情旗标。
    /// 原则：只转换确实存在的字段；源头缺失的关键字段保持缺失，
    /// 让后续 Required 校验响亮失败，绝不静默补零。
    /// </summary>
    public sealed class V0ToV1 : ISaveMigration
    {
        public int FromVersion { get { return 0; } }

        public JObject Apply(JObject save)
        {
            var result = new JObject
            {
                ["schemaVersion"] = 1
            };

            // v0 无 createdUtc；此字段非玩法关键，以纪元占位并注明来源。
            result["createdUtc"] = save.Value<string>("created") ?? "1970-01-01T00:00:00Z";

            if (save["hero"] != null) { result["protagonist"] = save["hero"]; }
            if (save["name"] != null) { result["name"] = save["name"]; }

            result["entryPath"] = save["entry"] != null ? save["entry"] : JValue.CreateNull();

            if (save["attrs"] is JObject attrs)
            {
                var mapped = new JObject();
                if (attrs["tili"] != null) { mapped["stamina"] = attrs["tili"]; }
                if (attrs["shengming"] != null) { mapped["health"] = attrs["shengming"]; }
                if (attrs["liliang"] != null) { mapped["strength"] = attrs["liliang"]; }
                if (attrs["zhihui"] != null) { mapped["wisdom"] = attrs["zhihui"]; }
                result["attributes"] = mapped;
            }

            // v0 时代还没有四轨官身，主角一律未入仕。
            result["offices"] = new JObject
            {
                ["zhishi"] = JValue.CreateNull(),
                ["sanguan"] = JValue.CreateNull(),
                ["xunZhuan"] = 0,
                ["jue"] = JValue.CreateNull()
            };

            if (save["rep"] is JObject rep)
            {
                var mapped = new JObject();
                if (rep["guan"] != null) { mapped["guansheng"] = rep["guan"]; }
                if (rep["min"] != null) { mapped["minwang"] = rep["min"]; }
                if (rep["jianghu"] != null) { mapped["jianghu"] = rep["jianghu"]; }
                result["reputation"] = mapped;
            }

            result["reputationLedger"] = new JArray();

            // 1 贯 = 1000 文；四舍五入到整文，不丢钱。
            if (save["money_guan"] != null)
            {
                double guan = save.Value<double>("money_guan");
                result["moneyWen"] = (long)System.Math.Round(guan * 1000.0);
            }

            if (save["date"] is JObject date)
            {
                var mapped = new JObject();
                if (date["era"] != null) { mapped["era"] = date["era"]; }
                if (date["year"] != null) { mapped["eraYear"] = date["year"]; }
                if (date["month"] != null) { mapped["month"] = date["month"]; }
                if (date["day"] != null) { mapped["day"] = date["day"]; }
                if (date["hour"] != null) { mapped["hourIndex"] = date["hour"]; }
                result["date"] = mapped;
            }

            result["storyFlags"] = new JObject();
            result["counters"] = new JObject();

            return result;
        }
    }
}
