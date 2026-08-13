using Lingyan.Core.Characters;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Officials
{
    /// <summary>一次入仕机会：门槛、去处、代价，全部摆在明面。</summary>
    public sealed class EntryOffer
    {
        /// <summary>门槛是否已达。</summary>
        public bool Available { get; set; }

        /// <summary>入仕动作的按钮文案键（铨选/应举/应募/待诏/纳资）。</summary>
        public string ActionKey { get; set; }

        /// <summary>未达门槛时的驳文键。</summary>
        public string GateKey { get; set; }

        /// <summary>授予的职事官；纳资拜官（胡商线）无职事，为 null。</summary>
        public string ZhiShiOfficeId { get; set; }

        /// <summary>授予的散官；宫官线不带散官，为 null。</summary>
        public string SanGuanId { get; set; }

        /// <summary>入仕代价（文）：胡商纳资 200 贯，其余为 0。</summary>
        public long PriceWen { get; set; }

        /// <summary>授官公文文本键。</summary>
        public string NoticeKey { get; set; }
    }

    /// <summary>
    /// 五主角线的入仕分化（规格：其余主角线开局）：
    /// - 明镜（法司线）：结案一起 → 铨选授县尉（从九品下）。
    /// - 白身柳七：明经（智 ≥ 10）/进士（智 ≥ 12 且民望 ≥ 15，行卷之风）→ 县尉；
    ///   投军（切磋两胜）→ 队正。
    /// - 戍卒铁衣（军线）：切磋两胜（武艺可称）→ 队正（正九品下）。
    /// - 女官青漪（宫线，武周段）：智 ≥ 12 且官声 ≥ 25 → 掌记（正八品，宫官不带散官）。
    /// - 胡商悉达（市籍之限，唐制商贾不预士伍）：纳资二百贯且民望 ≥ 30 →
    ///   得文散将仕郎虚衔，职事仍无门——身份即命运。
    /// </summary>
    public static class CareerEntryService
    {
        public const int SparWinsRequired = 2;
        public const long HuShangPriceWen = 200_000; // 二百贯

        public static EntryOffer Evaluate(SaveData save)
        {
            ProtagonistDef hero = ProtagonistCatalog.GetByKey(save.ProtagonistKey);
            save.Counters.TryGetValue("cases_closed", out int casesClosed);
            save.Counters.TryGetValue("spar_wins", out int sparWins);
            int wisdom = save.Attributes.Wisdom;

            switch (hero.Id)
            {
                case ProtagonistId.MingJing:
                    return new EntryOffer
                    {
                        ActionKey = "career.entry.act.xuanshou",
                        Available = casesClosed >= 1,
                        GateKey = "career.entry.gate.mingjing",
                        ZhiShiOfficeId = "xian_wei",
                        SanGuanId = SanGuanTable.InitialFor(
                            RankGrade.CongLower(9), civil: true).Id,
                        NoticeKey = "career.entry.notice.mingjing"
                    };

                case ProtagonistId.ShuZu:
                    return MilitaryOffer(sparWins, "career.entry.notice.shuzu");

                case ProtagonistId.NvGuan:
                    return new EntryOffer
                    {
                        ActionKey = "career.entry.act.daizhao",
                        Available = wisdom >= 12 && save.Reputation.GuanSheng >= 25,
                        GateKey = "career.entry.gate.nvguan",
                        ZhiShiOfficeId = "zhang_ji",
                        SanGuanId = null, // 宫官品阶自成体系，不带散官
                        NoticeKey = "career.entry.notice.nvguan"
                    };

                case ProtagonistId.HuShang:
                    return new EntryOffer
                    {
                        ActionKey = "career.entry.act.nazi",
                        Available = save.MoneyWen >= HuShangPriceWen
                            && save.Reputation.MinWang >= 30,
                        GateKey = "career.entry.gate.hushang",
                        ZhiShiOfficeId = null, // 市籍之限：有衔无职
                        SanGuanId = "jiangshi_lang",
                        PriceWen = HuShangPriceWen,
                        NoticeKey = "career.entry.notice.hushang"
                    };

                default: // 白身柳七：按创角时选定的途径
                    if (save.EntryPath == "TouJun")
                    {
                        return MilitaryOffer(sparWins, "career.entry.notice.toujun");
                    }
                    bool jinshi = save.EntryPath == "KejuJinShi";
                    return new EntryOffer
                    {
                        ActionKey = "career.entry.act.yingju",
                        Available = jinshi
                            ? wisdom >= 12 && save.Reputation.MinWang >= 15
                            : wisdom >= 10,
                        GateKey = jinshi
                            ? "career.entry.gate.jinshi"
                            : "career.entry.gate.mingjing_ke",
                        ZhiShiOfficeId = "xian_wei",
                        SanGuanId = SanGuanTable.InitialFor(
                            RankGrade.CongLower(9), civil: true).Id,
                        NoticeKey = jinshi
                            ? "career.entry.notice.jinshi"
                            : "career.entry.notice.mingjing_ke"
                    };
            }
        }

        private static EntryOffer MilitaryOffer(int sparWins, string noticeKey)
        {
            return new EntryOffer
            {
                ActionKey = "career.entry.act.yingmu",
                Available = sparWins >= SparWinsRequired,
                GateKey = "career.entry.gate.military",
                ZhiShiOfficeId = "dui_zheng",
                SanGuanId = SanGuanTable.InitialFor(
                    RankGrade.ZhengLower(9), civil: false).Id,
                NoticeKey = noticeKey
            };
        }

        /// <summary>落定：授官、扣纳资，一次成型。门槛未达时拒绝且不动任何状态。</summary>
        public static bool Apply(SaveData save, EntryOffer offer)
        {
            if (!offer.Available) { return false; }
            if (offer.PriceWen > 0)
            {
                if (save.MoneyWen < offer.PriceWen) { return false; }
                save.MoneyWen -= offer.PriceWen;
            }
            save.Offices.ZhiShiId = offer.ZhiShiOfficeId;
            save.Offices.SanGuanId = offer.SanGuanId;
            return true;
        }
    }
}
