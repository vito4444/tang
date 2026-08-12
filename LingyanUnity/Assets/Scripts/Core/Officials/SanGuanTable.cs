using System.Collections.Generic;
using System.Linq;

namespace Lingyan.Core.Officials
{
    /// <summary>一阶散官（品阶身份，决定服色与俸禄档次）。</summary>
    public sealed class SanGuanDef
    {
        public string Id { get; }
        public string Zh { get; }

        /// <summary>拼音转写（Title Case），未经 Hucker 校验词条的英文名兜底。</summary>
        public string Pinyin { get; }

        public RankGrade Grade { get; }

        public bool Civil { get; }

        /// <summary>序位：0 = 最低（将仕郎/陪戎副尉）。</summary>
        public int Order { get; }

        public SanGuanDef(string id, string zh, string pinyin, RankGrade grade, bool civil, int order)
        {
            Id = id;
            Zh = zh;
            Pinyin = pinyin;
            Grade = grade;
            Civil = civil;
            Order = order;
        }

        public RobeColor Robe { get { return RobeColors.FromGrade(Grade); } }
    }

    /// <summary>
    /// 文散官二十九阶；武散官主干二十九阶（怀化大将军、归德将军两蕃号不入本表）。
    /// 名称与品阶待复核《旧唐书·职官志》，见 docs/GLOSSARY.md 校验流程。
    /// </summary>
    public static class SanGuanTable
    {
        private static SanGuanDef C(int order, string id, string zh, string py, RankGrade g)
        {
            return new SanGuanDef(id, zh, py, g, true, order);
        }

        private static SanGuanDef M(int order, string id, string zh, string py, RankGrade g)
        {
            return new SanGuanDef(id, zh, py, g, false, order);
        }

        /// <summary>文散官，自高至低。</summary>
        public static readonly IReadOnlyList<SanGuanDef> CivilRanks = new[]
        {
            C(28, "kaifu_yitong_sansi", "开府仪同三司", "Kaifu Yitong Sansi", RankGrade.Cong(1)),
            C(27, "te_jin", "特进", "Tejin", RankGrade.Zheng(2)),
            C(26, "guanglu_dafu", "光禄大夫", "Guanglu Dafu", RankGrade.Cong(2)),
            C(25, "jinzi_guanglu_dafu", "金紫光禄大夫", "Jinzi Guanglu Dafu", RankGrade.Zheng(3)),
            C(24, "yinqing_guanglu_dafu", "银青光禄大夫", "Yinqing Guanglu Dafu", RankGrade.Cong(3)),
            C(23, "zhengyi_dafu", "正议大夫", "Zhengyi Dafu", RankGrade.ZhengUpper(4)),
            C(22, "tongyi_dafu", "通议大夫", "Tongyi Dafu", RankGrade.ZhengLower(4)),
            C(21, "taizhong_dafu", "太中大夫", "Taizhong Dafu", RankGrade.CongUpper(4)),
            C(20, "zhong_dafu", "中大夫", "Zhong Dafu", RankGrade.CongLower(4)),
            C(19, "zhongsan_dafu", "中散大夫", "Zhongsan Dafu", RankGrade.ZhengUpper(5)),
            C(18, "chaoyi_dafu", "朝议大夫", "Chaoyi Dafu", RankGrade.ZhengLower(5)),
            C(17, "chaoqing_dafu", "朝请大夫", "Chaoqing Dafu", RankGrade.CongUpper(5)),
            C(16, "chaosan_dafu", "朝散大夫", "Chaosan Dafu", RankGrade.CongLower(5)),
            C(15, "chaoyi_lang", "朝议郎", "Chaoyi Lang", RankGrade.ZhengUpper(6)),
            C(14, "chengyi_lang", "承议郎", "Chengyi Lang", RankGrade.ZhengLower(6)),
            C(13, "fengyi_lang", "奉议郎", "Fengyi Lang", RankGrade.CongUpper(6)),
            C(12, "tongzhi_lang", "通直郎", "Tongzhi Lang", RankGrade.CongLower(6)),
            C(11, "chaoqing_lang", "朝请郎", "Chaoqing Lang", RankGrade.ZhengUpper(7)),
            C(10, "xuande_lang", "宣德郎", "Xuande Lang", RankGrade.ZhengLower(7)),
            C(9, "chaosan_lang", "朝散郎", "Chaosan Lang", RankGrade.CongUpper(7)),
            C(8, "xuanyi_lang", "宣义郎", "Xuanyi Lang", RankGrade.CongLower(7)),
            C(7, "jishi_lang", "给事郎", "Jishi Lang", RankGrade.ZhengUpper(8)),
            C(6, "zhengshi_lang", "征事郎", "Zhengshi Lang", RankGrade.ZhengLower(8)),
            C(5, "chengfeng_lang", "承奉郎", "Chengfeng Lang", RankGrade.CongUpper(8)),
            C(4, "chengwu_lang", "承务郎", "Chengwu Lang", RankGrade.CongLower(8)),
            C(3, "rulin_lang", "儒林郎", "Rulin Lang", RankGrade.ZhengUpper(9)),
            C(2, "dengshi_lang", "登仕郎", "Dengshi Lang", RankGrade.ZhengLower(9)),
            C(1, "wenlin_lang", "文林郎", "Wenlin Lang", RankGrade.CongUpper(9)),
            C(0, "jiangshi_lang", "将仕郎", "Jiangshi Lang", RankGrade.CongLower(9))
        };

        /// <summary>武散官，自高至低。</summary>
        public static readonly IReadOnlyList<SanGuanDef> MilitaryRanks = new[]
        {
            M(29, "piaoqi_dajiangjun_san", "骠骑大将军", "Piaoqi Dajiangjun", RankGrade.Cong(1)),
            M(28, "fuguo_dajiangjun", "辅国大将军", "Fuguo Dajiangjun", RankGrade.Zheng(2)),
            M(27, "zhenjun_dajiangjun", "镇军大将军", "Zhenjun Dajiangjun", RankGrade.Cong(2)),
            M(26, "guanjun_dajiangjun", "冠军大将军", "Guanjun Dajiangjun", RankGrade.Zheng(3)),
            M(25, "yunhui_jiangjun", "云麾将军", "Yunhui Jiangjun", RankGrade.Cong(3)),
            M(24, "zhongwu_jiangjun", "忠武将军", "Zhongwu Jiangjun", RankGrade.ZhengUpper(4)),
            M(23, "zhuangwu_jiangjun", "壮武将军", "Zhuangwu Jiangjun", RankGrade.ZhengLower(4)),
            M(22, "xuanwei_jiangjun", "宣威将军", "Xuanwei Jiangjun", RankGrade.CongUpper(4)),
            M(21, "mingwei_jiangjun", "明威将军", "Mingwei Jiangjun", RankGrade.CongLower(4)),
            M(20, "dingyuan_jiangjun", "定远将军", "Dingyuan Jiangjun", RankGrade.ZhengUpper(5)),
            M(19, "ningyuan_jiangjun", "宁远将军", "Ningyuan Jiangjun", RankGrade.ZhengLower(5)),
            M(18, "youqi_jiangjun", "游骑将军", "Youqi Jiangjun", RankGrade.CongUpper(5)),
            M(17, "youji_jiangjun", "游击将军", "Youji Jiangjun", RankGrade.CongLower(5)),
            M(16, "zhaowu_xiaowei", "昭武校尉", "Zhaowu Xiaowei", RankGrade.ZhengUpper(6)),
            M(15, "zhaowu_fuwei", "昭武副尉", "Zhaowu Fuwei", RankGrade.ZhengLower(6)),
            M(14, "zhenwei_xiaowei", "振威校尉", "Zhenwei Xiaowei", RankGrade.CongUpper(6)),
            M(13, "zhenwei_fuwei", "振威副尉", "Zhenwei Fuwei", RankGrade.CongLower(6)),
            M(12, "zhiguo_xiaowei", "致果校尉", "Zhiguo Xiaowei", RankGrade.ZhengUpper(7)),
            M(11, "zhiguo_fuwei", "致果副尉", "Zhiguo Fuwei", RankGrade.ZhengLower(7)),
            M(10, "yihui_xiaowei", "翊麾校尉", "Yihui Xiaowei", RankGrade.CongUpper(7)),
            M(9, "yihui_fuwei", "翊麾副尉", "Yihui Fuwei", RankGrade.CongLower(7)),
            M(8, "xuanjie_xiaowei", "宣节校尉", "Xuanjie Xiaowei", RankGrade.ZhengUpper(8)),
            M(7, "xuanjie_fuwei", "宣节副尉", "Xuanjie Fuwei", RankGrade.ZhengLower(8)),
            M(6, "yuwu_xiaowei", "御侮校尉", "Yuwu Xiaowei", RankGrade.CongUpper(8)),
            M(5, "yuwu_fuwei", "御侮副尉", "Yuwu Fuwei", RankGrade.CongLower(8)),
            M(4, "renyong_xiaowei", "仁勇校尉", "Renyong Xiaowei", RankGrade.ZhengUpper(9)),
            M(3, "renyong_fuwei", "仁勇副尉", "Renyong Fuwei", RankGrade.ZhengLower(9)),
            M(2, "peirong_xiaowei", "陪戎校尉", "Peirong Xiaowei", RankGrade.CongUpper(9)),
            M(1, "peirong_fuwei", "陪戎副尉", "Peirong Fuwei", RankGrade.CongLower(9))
        };

        private static readonly Dictionary<string, SanGuanDef> ById =
            CivilRanks.Concat(MilitaryRanks).ToDictionary(s => s.Id);

        public static SanGuanDef Get(string id)
        {
            return id != null && ById.TryGetValue(id, out var def) ? def : null;
        }

        public static IEnumerable<SanGuanDef> All { get { return ById.Values; } }

        /// <summary>
        /// 入仕释褐时按职事品对应授予的起家散官：
        /// 取"不高于职事品"的最高一阶；若职事品低于全表则取最低阶。
        /// </summary>
        public static SanGuanDef InitialFor(RankGrade zhiShiGrade, bool civil)
        {
            IReadOnlyList<SanGuanDef> ranks = civil ? CivilRanks : MilitaryRanks;
            for (int i = 0; i < ranks.Count; i++)
            {
                if (ranks[i].Grade.OrderValue >= zhiShiGrade.OrderValue)
                {
                    return ranks[i];
                }
            }
            return ranks[ranks.Count - 1];
        }
    }
}
