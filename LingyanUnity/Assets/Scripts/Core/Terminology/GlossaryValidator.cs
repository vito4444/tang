using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;

namespace Lingyan.Core.Terminology
{
    /// <summary>
    /// 术语一致性强制校验（规格：写脚本强制校验，全局统一）。
    /// 在冒烟测试与 CI 中运行，违规即红。
    /// </summary>
    public static class GlossaryValidator
    {
        /// <summary>
        /// 规则一：所有职事官、封爵定义必须能在术语表解析出英译。
        /// </summary>
        public static List<string> CheckOfficesResolve(Glossary glossary)
        {
            var violations = new List<string>();
            foreach (OfficeDef office in OfficialLadders.All)
            {
                GlossaryEntry entry = glossary.ById(office.GlossaryId);
                if (entry == null)
                {
                    violations.Add("官职缺术语词条: " + office.Zh + " (" + office.GlossaryId + ")");
                }
                else if (entry.Zh != office.Zh)
                {
                    violations.Add("官职中文名与术语表不一致: " + office.Zh + " vs " + entry.Zh);
                }
            }
            foreach (JueDef jue in JueTable.Tiers)
            {
                if (glossary.ById(jue.GlossaryId) == null)
                {
                    violations.Add("封爵缺术语词条: " + jue.Zh);
                }
            }
            return violations;
        }

        /// <summary>
        /// 规则二：本地化目录里任何 zh 文案若包含术语表中的官职中文名，
        /// 对应 en 文案必须包含锁定英译（大小写不敏感）。
        /// 防止散落在剧情文本里的私译。
        /// 命中按"最长词条优先"：被更长词条完全覆盖的短词条命中不计
        /// （如「开国郡公」中的「国公」）。
        /// </summary>
        public static List<string> CheckCatalogConsistency(LocalizationCatalog catalog, Glossary glossary)
        {
            var violations = new List<string>();
            List<GlossaryEntry> officeTerms = glossary.Entries
                .Where(e => e.Category == "office" || e.Category == "jue")
                .OrderByDescending(e => e.Zh.Length)
                .ToList();

            foreach (string key in catalog.Keys)
            {
                if (!catalog.TryGet(key, Locale.ZhHans, out string zh)) { continue; }
                if (!catalog.TryGet(key, Locale.En, out string en)) { continue; }

                bool[] covered = new bool[zh.Length];
                foreach (GlossaryEntry term in officeTerms)
                {
                    bool hasUncoveredHit = false;
                    int search = 0;
                    while (true)
                    {
                        int at = zh.IndexOf(term.Zh, search, System.StringComparison.Ordinal);
                        if (at < 0) { break; }
                        bool alreadyCovered = true;
                        for (int i = at; i < at + term.Zh.Length; i++)
                        {
                            if (!covered[i]) { alreadyCovered = false; }
                        }
                        if (!alreadyCovered)
                        {
                            hasUncoveredHit = true;
                            for (int i = at; i < at + term.Zh.Length; i++)
                            {
                                covered[i] = true;
                            }
                        }
                        search = at + 1;
                    }

                    if (hasUncoveredHit
                        && en.IndexOf(term.En, System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        violations.Add(
                            key + ": zh 提及「" + term.Zh + "」，en 未用锁定译名 \"" + term.En + "\"");
                    }
                }
            }
            return violations;
        }

        /// <summary>
        /// 规则三：术语表内部查重——英译在 office 类目内必须唯一，
        /// 一名两译或两名一译都算违规。
        /// </summary>
        public static List<string> CheckInternalUniqueness(Glossary glossary)
        {
            var violations = new List<string>();
            var seenEn = new Dictionary<string, string>();
            foreach (GlossaryEntry entry in glossary.Entries.Where(e => e.Category == "office"))
            {
                string enKey = entry.En.ToLowerInvariant();
                if (seenEn.TryGetValue(enKey, out string firstZh))
                {
                    violations.Add("英译重复: \"" + entry.En + "\" 同时用于 " + firstZh + " 与 " + entry.Zh);
                }
                else
                {
                    seenEn[enKey] = entry.Zh;
                }
            }
            return violations;
        }
    }
}
