using System.Collections.Generic;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Officials
{
    /// <summary>四匦（《旧唐书·则天皇后纪》垂拱二年置铜匦于朝堂）。</summary>
    public enum GuiSlot
    {
        /// <summary>延恩匦（东，青）：怀才自荐、求仕进。</summary>
        YanEn = 0,

        /// <summary>招谏匦（南，丹）：言朝政得失。</summary>
        ZhaoJian = 1,

        /// <summary>申冤匦（西，白）：有冤滞者投之。</summary>
        ShenYuan = 2,

        /// <summary>通玄匦（北，玄）：言天象灾变。</summary>
        TongXuan = 3
    }

    /// <summary>一次投书的结果（效果全在明面，回执文本键交呈现层）。</summary>
    public sealed class TongGuiResult
    {
        public bool Accepted { get; set; }

        /// <summary>回执/驳文文本键。</summary>
        public string TextKey { get; set; }

        public int GuanShengDelta { get; set; }

        public int MinWangDelta { get; set; }

        public int JiangHuDelta { get; set; }

        /// <summary>申冤匦昭雪的案件 id；无则 null。</summary>
        public string RedressedCaseId { get; set; }
    }

    /// <summary>
    /// 铜匦投书：每月一次（有司须校理，投多不受）。
    /// - 延恩：白身得官声（自荐上达）；已仕者驳回（有官守者不由此进）。
    /// - 招谏：官声 +2、民望 +1；谏而涉险，江湖 -1（告密之风盛，坊间侧目）。
    /// - 申冤：档上有冤案且民望 ≥ 50（须有人证愿随投）→ 冤案昭雪：
    ///   该案 wrongful 洗清、官声 -3（自承其失）、民望 +6（坊间称快）。
    ///   循吏传的封锁就此解开——认错也是仕途的一部分。
    /// - 通玄：江湖 +2（坊间传为异士），官声 -1（有司不喜怪力乱神）。
    /// </summary>
    public static class TongGuiService
    {
        public const int ShenYuanMinMinWang = 50;

        /// <summary>本月是否已投过。</summary>
        public static bool AlreadyThisMonth(SaveData save, int yearMonth)
        {
            return save.Counters.TryGetValue("tonggui_ym", out int last) && last >= yearMonth;
        }

        public static TongGuiResult Submit(SaveData save, GuiSlot slot, int yearMonth)
        {
            var result = new TongGuiResult();
            if (AlreadyThisMonth(save, yearMonth))
            {
                result.TextKey = "tonggui.result.monthly_limit";
                return result;
            }

            switch (slot)
            {
                case GuiSlot.YanEn:
                    if (save.Offices.ZhiShiId != null || save.Offices.SanGuanId != null)
                    {
                        result.TextKey = "tonggui.result.yanen_officed";
                        return result;
                    }
                    result.Accepted = true;
                    result.GuanShengDelta = +3;
                    result.TextKey = "tonggui.result.yanen_ok";
                    break;

                case GuiSlot.ZhaoJian:
                    result.Accepted = true;
                    result.GuanShengDelta = +2;
                    result.MinWangDelta = +1;
                    result.JiangHuDelta = -1;
                    result.TextKey = "tonggui.result.zhaojian_ok";
                    break;

                case GuiSlot.ShenYuan:
                {
                    string wrongfulCaseId = FirstWrongfulCase(save);
                    if (wrongfulCaseId == null)
                    {
                        result.TextKey = "tonggui.result.shenyuan_no_case";
                        return result;
                    }
                    if (save.Reputation.MinWang < ShenYuanMinMinWang)
                    {
                        result.TextKey = "tonggui.result.shenyuan_no_backing";
                        return result;
                    }
                    result.Accepted = true;
                    result.RedressedCaseId = wrongfulCaseId;
                    result.GuanShengDelta = -3;
                    result.MinWangDelta = +6;
                    result.TextKey = "tonggui.result.shenyuan_ok";
                    save.Cases[wrongfulCaseId].WrongfulConviction = false;
                    break;
                }

                default:
                    result.Accepted = true;
                    result.JiangHuDelta = +2;
                    result.GuanShengDelta = -1;
                    result.TextKey = "tonggui.result.tongxuan_ok";
                    break;
            }

            save.Counters["tonggui_ym"] = yearMonth;
            return result;
        }

        private static string FirstWrongfulCase(SaveData save)
        {
            foreach (KeyValuePair<string, SaveCaseState> pair in save.Cases)
            {
                if (pair.Value.WrongfulConviction) { return pair.Key; }
            }
            return null;
        }
    }
}
