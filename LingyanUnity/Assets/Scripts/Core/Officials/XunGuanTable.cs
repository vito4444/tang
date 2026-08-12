using System.Collections.Generic;

namespace Lingyan.Core.Officials
{
    /// <summary>勋官一转。</summary>
    public sealed class XunGuanDef
    {
        /// <summary>转数，1–12；上柱国十二转。</summary>
        public int Zhuan { get; }

        public string Id { get; }
        public string Zh { get; }

        /// <summary>英译；名号显赫者用 Hucker 译名，其余以拼音转写兜底。</summary>
        public string En { get; }

        /// <summary>视品（比视文武官品，不分上下阶）。</summary>
        public RankGrade ComparableGrade { get; }

        public XunGuanDef(int zhuan, string id, string zh, string en, RankGrade grade)
        {
            Zhuan = zhuan;
            Id = id;
            Zh = zh;
            En = en;
            ComparableGrade = grade;
        }
    }

    /// <summary>勋官十二转，军功叙迁专属。</summary>
    public static class XunGuanTable
    {
        public static readonly IReadOnlyList<XunGuanDef> Ranks = new[]
        {
            new XunGuanDef(12, "shang_zhuguo", "上柱国", "Supreme Pillar of State", RankGrade.Zheng(2)),
            new XunGuanDef(11, "zhuguo", "柱国", "Pillar of State", RankGrade.Cong(2)),
            new XunGuanDef(10, "shang_hujun", "上护军", "Shang Hujun", RankGrade.Zheng(3)),
            new XunGuanDef(9, "hujun", "护军", "Hujun", RankGrade.Cong(3)),
            new XunGuanDef(8, "shang_qingche_duwei", "上轻车都尉", "Shang Qingche Duwei", RankGrade.Zheng(4)),
            new XunGuanDef(7, "qingche_duwei", "轻车都尉", "Qingche Duwei", RankGrade.Cong(4)),
            new XunGuanDef(6, "shang_qi_duwei", "上骑都尉", "Shang Qi Duwei", RankGrade.Zheng(5)),
            new XunGuanDef(5, "qi_duwei", "骑都尉", "Qi Duwei", RankGrade.Cong(5)),
            new XunGuanDef(4, "xiaoqi_wei", "骁骑尉", "Xiaoqi Wei", RankGrade.Zheng(6)),
            new XunGuanDef(3, "feiqi_wei", "飞骑尉", "Feiqi Wei", RankGrade.Cong(6)),
            new XunGuanDef(2, "yunqi_wei", "云骑尉", "Yunqi Wei", RankGrade.Zheng(7)),
            new XunGuanDef(1, "wuqi_wei", "武骑尉", "Wuqi Wei", RankGrade.Cong(7))
        };

        /// <summary>按累计转数取当前勋号；0 转无勋。</summary>
        public static XunGuanDef ForZhuan(int zhuan)
        {
            if (zhuan <= 0) { return null; }
            if (zhuan > 12) { zhuan = 12; }
            for (int i = 0; i < Ranks.Count; i++)
            {
                if (Ranks[i].Zhuan == zhuan) { return Ranks[i]; }
            }
            return null;
        }
    }
}
