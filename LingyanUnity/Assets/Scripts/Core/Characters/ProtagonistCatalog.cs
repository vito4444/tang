using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Officials;

namespace Lingyan.Core.Characters
{
    public enum ProtagonistId
    {
        /// <summary>明镜（狄仁杰式）：无名县衙小吏，文官·法司线。</summary>
        MingJing = 0,

        /// <summary>白身（自由角色）：一介布衣，可科举可投军。</summary>
        BaiShen = 1,

        /// <summary>戍卒：边镇无名士兵，武官·折冲府线。</summary>
        ShuZu = 2,

        /// <summary>女官：掖庭宫人，内廷·女官线（武周特有）。</summary>
        NvGuan = 3,

        /// <summary>胡商（粟特）：西市商贾，商路·情报线。</summary>
        HuShang = 4
    }

    /// <summary>白身的入仕途径。</summary>
    public enum EntryPath
    {
        /// <summary>科举·明经科（帖经 + 墨义，易）。</summary>
        KejuMingJing = 0,

        /// <summary>科举·进士科（诗赋 + 策问，难而清贵）。</summary>
        KejuJinShi = 1,

        /// <summary>折冲府应募，队正起步，军功叙迁。</summary>
        TouJun = 2
    }

    public sealed class ProtagonistDef
    {
        public ProtagonistId Id { get; }

        /// <summary>字符串 id，存档与本地化键用。</summary>
        public string Key { get; }

        public string DefaultNameZh { get; }
        public string DefaultNameEn { get; }

        /// <summary>预设四维；仅白身可自由分配。</summary>
        public AttributeSet Preset { get; }

        public bool FreeAllocation { get; }

        public long StartMoneyWen { get; }

        public CareerLine Line { get; }

        /// <summary>起始身份本地化键（流外吏、募人、宫人、市籍商贾、白身）。</summary>
        public string StartStatusKey { get; }

        /// <summary>起始三轨名誉（官声, 民望, 江湖）。</summary>
        public int StartGuanSheng { get; }
        public int StartMinWang { get; }
        public int StartJiangHu { get; }

        public ProtagonistDef(
            ProtagonistId id,
            string key,
            string nameZh,
            string nameEn,
            AttributeSet preset,
            bool freeAllocation,
            long startMoneyWen,
            CareerLine line,
            string startStatusKey,
            int startGuanSheng,
            int startMinWang,
            int startJiangHu)
        {
            Id = id;
            Key = key;
            DefaultNameZh = nameZh;
            DefaultNameEn = nameEn;
            Preset = preset;
            FreeAllocation = freeAllocation;
            StartMoneyWen = startMoneyWen;
            Line = line;
            StartStatusKey = startStatusKey;
            StartGuanSheng = startGuanSheng;
            StartMinWang = startMinWang;
            StartJiangHu = startJiangHu;
        }

        public string TitleKey { get { return "protagonist." + Key + ".title"; } }
        public string BlurbKey { get { return "protagonist." + Key + ".blurb"; } }
    }

    /// <summary>
    /// 五位主角。四维总和一律 28；仅白身以基础 4/4/4/4 加 12 点自由分配。
    /// 默认姓名为占位设定，进入内容阶段可改。
    /// </summary>
    public static class ProtagonistCatalog
    {
        public static readonly IReadOnlyList<ProtagonistDef> All = new[]
        {
            new ProtagonistDef(
                ProtagonistId.MingJing, "mingjing", "沈知白", "Shen Zhibai",
                new AttributeSet(stamina: 6, health: 6, strength: 4, wisdom: 12),
                freeAllocation: false, startMoneyWen: 6000,
                CareerLine.CivilJudicial, "status.liuwai_clerk",
                startGuanSheng: 15, startMinWang: 20, startJiangHu: 5),

            new ProtagonistDef(
                ProtagonistId.BaiShen, "baishen", "柳七", "Liu Qi",
                new AttributeSet(stamina: 4, health: 4, strength: 4, wisdom: 4),
                freeAllocation: true, startMoneyWen: 3000,
                CareerLine.Undecided, "status.commoner",
                startGuanSheng: 10, startMinWang: 10, startJiangHu: 10),

            new ProtagonistDef(
                ProtagonistId.ShuZu, "shuzu", "陈铁衣", "Chen Tieyi",
                new AttributeSet(stamina: 9, health: 8, strength: 8, wisdom: 3),
                freeAllocation: false, startMoneyWen: 1500,
                CareerLine.Military, "status.garrison_recruit",
                startGuanSheng: 10, startMinWang: 10, startJiangHu: 15),

            new ProtagonistDef(
                ProtagonistId.NvGuan, "nvguan", "苏青漪", "Su Qingyi",
                new AttributeSet(stamina: 5, health: 6, strength: 4, wisdom: 13),
                freeAllocation: false, startMoneyWen: 800,
                CareerLine.Palace, "status.palace_attendant",
                startGuanSheng: 20, startMinWang: 5, startJiangHu: 0),

            new ProtagonistDef(
                ProtagonistId.HuShang, "hushang", "康悉达", "Kang Xida",
                new AttributeSet(stamina: 7, health: 7, strength: 5, wisdom: 9),
                freeAllocation: false, startMoneyWen: 240000,
                CareerLine.Trade, "status.sogdian_merchant",
                startGuanSheng: 5, startMinWang: 15, startJiangHu: 25)
        };

        private static readonly Dictionary<ProtagonistId, ProtagonistDef> ById =
            All.ToDictionary(p => p.Id);

        private static readonly Dictionary<string, ProtagonistDef> ByKey =
            All.ToDictionary(p => p.Key);

        public static ProtagonistDef Get(ProtagonistId id) { return ById[id]; }

        public static ProtagonistDef GetByKey(string key)
        {
            return key != null && ByKey.TryGetValue(key, out var def) ? def : null;
        }
    }
}
