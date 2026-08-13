using System.Collections.Generic;
using System.Linq;

namespace Lingyan.Core.Officials
{
    /// <summary>
    /// 文、武两条职事官迁转序列（规格第四节）。
    /// 序列顺序是"权势/清要"序列，不是品阶单调序列——唐制中
    /// 监察御史（正八品上）之权远过县丞，正是四轨分立的味道所在。
    /// 品阶取规格给定值；规格未给者补史料值（见 docs/DECISIONS.md）。
    /// </summary>
    public static class OfficialLadders
    {
        public static readonly IReadOnlyList<OfficeDef> Civil = new[]
        {
            new OfficeDef("xian_wei", "县尉", "xian_wei", RankGrade.CongLower(9), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 0),
            new OfficeDef("xian_cheng", "县丞", "xian_cheng", RankGrade.ZhengLower(8), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 1),
            new OfficeDef("jiancha_yushi", "监察御史", "jiancha_yushi", RankGrade.ZhengUpper(8), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 2),
            new OfficeDef("shi_yushi", "侍御史", "shi_yushi", RankGrade.CongLower(6), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 3),
            new OfficeDef("dalisi_cheng", "大理寺丞", "dalisi_cheng", RankGrade.CongUpper(6), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 4),
            new OfficeDef("xingbu_yuanwailang", "刑部员外郎", "xingbu_yuanwailang", RankGrade.CongUpper(6), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 5),
            new OfficeDef("xingbu_langzhong", "刑部郎中", "xingbu_langzhong", RankGrade.CongUpper(5), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 6),
            new OfficeDef("dalisi_shaoqing", "大理寺少卿", "dalisi_shaoqing", RankGrade.CongUpper(4), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 7),
            new OfficeDef("yushi_zhongcheng", "御史中丞", "yushi_zhongcheng", RankGrade.ZhengUpper(5), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 8),
            new OfficeDef("dalisi_qing", "大理寺卿", "dalisi_qing", RankGrade.Cong(3), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 9),
            new OfficeDef("zhongshu_shilang", "中书侍郎", "zhongshu_shilang", RankGrade.ZhengUpper(4), OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 10),
            new OfficeDef("tong_pingzhangshi", "同中书门下平章事", "tong_pingzhangshi", null, OfficeTrack.ZhiShi, CareerLine.CivilJudicial, 11, isCommission: true)
        };

        public static readonly IReadOnlyList<OfficeDef> Military = new[]
        {
            new OfficeDef("dui_zheng", "队正", "dui_zheng", RankGrade.ZhengLower(9), OfficeTrack.ZhiShi, CareerLine.Military, 0),
            new OfficeDef("lv_shuai", "旅帅", "lv_shuai", RankGrade.CongUpper(8), OfficeTrack.ZhiShi, CareerLine.Military, 1),
            new OfficeDef("xiao_wei", "校尉", "xiao_wei", RankGrade.ZhengUpper(6), OfficeTrack.ZhiShi, CareerLine.Military, 2),
            new OfficeDef("guoyi_duwei", "果毅都尉", "guoyi_duwei", RankGrade.CongLower(5), OfficeTrack.ZhiShi, CareerLine.Military, 3),
            new OfficeDef("zhechong_duwei", "折冲都尉", "zhechong_duwei", RankGrade.ZhengUpper(4), OfficeTrack.ZhiShi, CareerLine.Military, 4),
            new OfficeDef("zhonglang_jiang", "中郎将", "zhonglang_jiang", RankGrade.ZhengLower(4), OfficeTrack.ZhiShi, CareerLine.Military, 5),
            new OfficeDef("zhuwei_jiangjun", "诸卫将军", "zhuwei_jiangjun", RankGrade.Cong(3), OfficeTrack.ZhiShi, CareerLine.Military, 6),
            new OfficeDef("zhuwei_dajiangjun", "诸卫大将军", "zhuwei_dajiangjun", RankGrade.Zheng(3), OfficeTrack.ZhiShi, CareerLine.Military, 7),
            new OfficeDef("xingjun_zongguan", "行军总管", "xingjun_zongguan", null, OfficeTrack.ZhiShi, CareerLine.Military, 8, isCommission: true),
            new OfficeDef("piaoqi_dajiangjun", "骠骑大将军", "piaoqi_dajiangjun", RankGrade.Cong(1), OfficeTrack.SanGuan, CareerLine.Military, 9)
        };

        /// <summary>
        /// 内廷·宫官线（尚宫局，《唐六典》卷十二）。宫官品阶自成体系、不带散官；
        /// 女官主角（武周段）自掖庭起家。宫官不分上下阶。
        /// </summary>
        public static readonly IReadOnlyList<OfficeDef> Palace = new[]
        {
            new OfficeDef("zhang_ji", "掌记", "zhang_ji", RankGrade.Zheng(8), OfficeTrack.ZhiShi, CareerLine.Palace, 0),
            new OfficeDef("dian_ji", "典记", "dian_ji", RankGrade.Zheng(7), OfficeTrack.ZhiShi, CareerLine.Palace, 1),
            new OfficeDef("si_ji", "司记", "si_ji", RankGrade.Zheng(6), OfficeTrack.ZhiShi, CareerLine.Palace, 2),
            new OfficeDef("shang_gong", "尚宫", "shang_gong", RankGrade.Zheng(5), OfficeTrack.ZhiShi, CareerLine.Palace, 3)
        };

        private static readonly Dictionary<string, OfficeDef> ById =
            Civil.Concat(Military).Concat(Palace).ToDictionary(o => o.Id);

        public static OfficeDef Get(string id)
        {
            // 白身（未入仕）时 ZhiShiId 为 null，与 SanGuanTable/JueTable 同约定：null 入 null 出
            return id != null && ById.TryGetValue(id, out var def) ? def : null;
        }

        public static IEnumerable<OfficeDef> All { get { return ById.Values; } }

        /// <summary>同线序列中的下一阶；已到顶或不在序列中返回 null。</summary>
        public static OfficeDef NextOf(OfficeDef current)
        {
            if (current == null) { return null; }
            IReadOnlyList<OfficeDef> ladder;
            switch (current.Line)
            {
                case CareerLine.Military: ladder = Military; break;
                case CareerLine.Palace: ladder = Palace; break;
                default: ladder = Civil; break;
            }
            int next = current.LadderIndex + 1;
            return next >= 0 && next < ladder.Count ? ladder[next] : null;
        }
    }
}
