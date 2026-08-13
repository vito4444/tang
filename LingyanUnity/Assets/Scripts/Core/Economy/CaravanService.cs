using Lingyan.Core.Characters;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Economy
{
    /// <summary>一次商队派遣的账目。</summary>
    public sealed class CaravanResult
    {
        public bool Dispatched { get; set; }

        public string TextKey { get; set; }

        /// <summary>本金（文）。</summary>
        public long CostWen { get; set; }

        /// <summary>净利（文）。</summary>
        public long ProfitWen { get; set; }
    }

    /// <summary>
    /// 遣商队（商线中期）：粟特商路月贸——本金五十贯购绢帛香药发往西域，
    /// 伙计操办，月内结账。利钱吃江湖名望：路上认人比认路更要紧——
    /// 净利 = 本金 ×（10% + 0.4% × 江湖名望），江湖 50 以上封顶 30%。
    /// 仅商路线主角可遣（市籍是限也是本钱）；月一次；本金不足不发。
    /// </summary>
    public static class CaravanService
    {
        public const long CostWen = 50_000; // 五十贯

        public const int JiangHuCap = 50;

        /// <summary>驳文键；可发返回 null。</summary>
        public static string RefusalKey(SaveData save, int yearMonth)
        {
            ProtagonistDef hero = ProtagonistCatalog.GetByKey(save.ProtagonistKey);
            if (hero.Line != CareerLine.Trade)
            {
                return "caravan.refuse.not_trader";
            }
            if (save.Counters.TryGetValue("caravan_ym", out int last) && last >= yearMonth)
            {
                return "caravan.refuse.already";
            }
            if (save.MoneyWen < CostWen)
            {
                return "caravan.refuse.no_capital";
            }
            return null;
        }

        /// <summary>净利（文）按当前江湖名望计。</summary>
        public static long ProfitFor(int jiangHu)
        {
            int clamped = jiangHu < 0 ? 0 : (jiangHu > JiangHuCap ? JiangHuCap : jiangHu);
            long permille = 100 + 4L * clamped; // 100‰–300‰
            return CostWen * permille / 1000;
        }

        public static CaravanResult Dispatch(SaveData save, int yearMonth)
        {
            var result = new CaravanResult { CostWen = CostWen };
            string refusal = RefusalKey(save, yearMonth);
            if (refusal != null)
            {
                result.TextKey = refusal;
                return result;
            }

            result.Dispatched = true;
            result.ProfitWen = ProfitFor(save.Reputation.JiangHu);
            result.TextKey = "caravan.result.ok";
            save.MoneyWen += result.ProfitWen; // 本金当月收回，只记净利
            save.Counters["caravan_ym"] = yearMonth;
            return result;
        }
    }
}
