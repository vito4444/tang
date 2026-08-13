using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Terminology
{
    /// <summary>
    /// Codex 百科（规格第十一节）：术语表即词库，玩法节点解锁词条。
    /// 历史题材面向海外玩家的必需品，也是英文本地化的支撑。
    /// </summary>
    public static class CodexService
    {
        /// <summary>解锁（幂等）。返回本次新解锁的 id。</summary>
        public static List<string> Unlock(SaveData save, params string[] ids)
        {
            var fresh = new List<string>();
            foreach (string id in ids)
            {
                if (!save.CodexUnlocked.Contains(id))
                {
                    save.CodexUnlocked.Add(id);
                    fresh.Add(id);
                }
            }
            return fresh;
        }

        public static bool IsUnlocked(SaveData save, string id)
        {
            return save.CodexUnlocked.Contains(id);
        }

        /// <summary>玩法节点 → 词条解锁表（触点集中一处，便于随内容扩充）。</summary>
        public static List<string> OnEvent(SaveData save, CodexEvent codexEvent)
        {
            switch (codexEvent)
            {
                case CodexEvent.EnterWard:
                    return Unlock(save, "wuhou", "fanye", "shi_market");
                case CodexEvent.HeardSilkCase:
                    return Unlock(save, "dalisi", "xingbu");
                case CodexEvent.CaseOpened:
                    return Unlock(save, "kaoke", "sishan", "ershiqizui");
                case CodexEvent.Appointed:
                    return Unlock(save, "xian_wei", "xian_ling", "zhishiguan",
                        "sanguan_term", "liuwai_ruliu");
                case CodexEvent.SalaryDrawn:
                    return Unlock(save, "zhechongfu", "fubing");
                case CodexEvent.Sparred:
                    return Unlock(save, "xunguan_term");
                case CodexEvent.SawArchitecture:
                    return Unlock(save, "chiwei", "dougong", "xiaang",
                        "zhilingchuang", "wutoumen");
                case CodexEvent.VisitMarket:
                    return Unlock(save, "shi_market", "guan_currency", "wen_currency");
                case CodexEvent.Hired:
                    return Unlock(save, "yongbao");
                default:
                    return new List<string>();
            }
        }

        /// <summary>某分类的（已解锁数, 总数）。</summary>
        public static (int unlocked, int total) Progress(
            SaveData save, Glossary glossary, string category)
        {
            var entries = glossary.Entries.Where(e => e.Category == category).ToList();
            int unlocked = entries.Count(e => save.CodexUnlocked.Contains(e.Id));
            return (unlocked, entries.Count);
        }
    }

    public enum CodexEvent
    {
        EnterWard = 0,
        HeardSilkCase = 1,
        CaseOpened = 2,
        Appointed = 3,
        SalaryDrawn = 4,
        Sparred = 5,
        SawArchitecture = 6,
        VisitMarket = 7,
        Hired = 8
    }
}
