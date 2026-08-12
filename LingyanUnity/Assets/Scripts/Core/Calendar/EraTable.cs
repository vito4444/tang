using System.Collections.Generic;
using System.Linq;

namespace Lingyan.Core.Calendar
{
    /// <summary>一个年号。</summary>
    public sealed class EraDef
    {
        public string Id { get; }
        public string Zh { get; }
        public string Pinyin { get; }

        /// <summary>建元之年（公元）。改元多在年中，本表按起始年简化，见 docs/DECISIONS.md。</summary>
        public int StartYear { get; }

        /// <summary>是否武周年号（690 革命后）。</summary>
        public bool WuZhou { get; }

        public EraDef(string id, string zh, string pinyin, int startYear, bool wuZhou)
        {
            Id = id;
            Zh = zh;
            Pinyin = pinyin;
            StartYear = startYear;
            WuZhou = wuZhou;
        }
    }

    /// <summary>
    /// 本作时代窗口（唐高宗后期至武周，660–705）内的年号表。
    /// 年号更替由剧情脚本驱动（SetEra），不做年中自动改元。
    /// </summary>
    public static class EraTable
    {
        public static readonly IReadOnlyList<EraDef> Eras = new[]
        {
            new EraDef("xianqing", "显庆", "Xianqing", 656, false),
            new EraDef("longshuo", "龙朔", "Longshuo", 661, false),
            new EraDef("linde", "麟德", "Linde", 664, false),
            new EraDef("qianfeng", "乾封", "Qianfeng", 666, false),
            new EraDef("zongzhang", "总章", "Zongzhang", 668, false),
            new EraDef("xianheng", "咸亨", "Xianheng", 670, false),
            new EraDef("shangyuan_gaozong", "上元", "Shangyuan", 674, false),
            new EraDef("yifeng", "仪凤", "Yifeng", 676, false),
            new EraDef("tiaolu", "调露", "Tiaolu", 679, false),
            new EraDef("yonglong", "永隆", "Yonglong", 680, false),
            new EraDef("kaiyao", "开耀", "Kaiyao", 681, false),
            new EraDef("yongchun", "永淳", "Yongchun", 682, false),
            new EraDef("hongdao", "弘道", "Hongdao", 683, false),
            new EraDef("sisheng", "嗣圣", "Sisheng", 684, false),
            new EraDef("wenming", "文明", "Wenming", 684, false),
            new EraDef("guangzhai", "光宅", "Guangzhai", 684, false),
            new EraDef("chuigong", "垂拱", "Chuigong", 685, false),
            new EraDef("yongchang", "永昌", "Yongchang", 689, false),
            new EraDef("zaichu", "载初", "Zaichu", 690, false),
            new EraDef("tianshou", "天授", "Tianshou", 690, true),
            new EraDef("ruyi", "如意", "Ruyi", 692, true),
            new EraDef("changshou", "长寿", "Changshou", 692, true),
            new EraDef("yanzai", "延载", "Yanzai", 694, true),
            new EraDef("zhengsheng", "证圣", "Zhengsheng", 695, true),
            new EraDef("tiancewansui", "天册万岁", "Tiance Wansui", 695, true),
            new EraDef("wansuidengfeng", "万岁登封", "Wansui Dengfeng", 696, true),
            new EraDef("wansuitongtian", "万岁通天", "Wansui Tongtian", 696, true),
            new EraDef("shengong", "神功", "Shengong", 697, true),
            new EraDef("shengli", "圣历", "Shengli", 698, true),
            new EraDef("jiushi", "久视", "Jiushi", 700, true),
            new EraDef("dazu", "大足", "Dazu", 701, true),
            new EraDef("changan_era", "长安", "Chang'an", 701, true),
            new EraDef("shenlong", "神龙", "Shenlong", 705, false)
        };

        private static readonly Dictionary<string, EraDef> ById = Eras.ToDictionary(e => e.Id);

        public static EraDef Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var era) ? era : null;
        }

        /// <summary>年号纪年 → 公元年。</summary>
        public static int ToGregorianYear(string eraId, int eraYear)
        {
            EraDef era = Get(eraId);
            return era == null ? 0 : era.StartYear + eraYear - 1;
        }
    }
}
