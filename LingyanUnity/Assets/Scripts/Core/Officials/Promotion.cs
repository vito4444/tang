using System.Collections.Generic;

namespace Lingyan.Core.Officials
{
    /// <summary>迁转判定结果。ReasonKey 为本地化键，UI 直接取词。</summary>
    public sealed class PromotionDecision
    {
        public bool Eligible { get; set; }
        public string ReasonKey { get; set; }
        public OfficeDef NextOffice { get; set; }
    }

    /// <summary>
    /// 迁转规则（每年一考、四年一任）：
    /// 任满四考、四考皆及中中、且至少一考中上以上，方许迁转；
    /// 考得上上者以殊考特旨，立时可迁。
    /// </summary>
    public static class PromotionService
    {
        public const int TermYears = 4;

        public static PromotionDecision Evaluate(IReadOnlyList<NineGrade> annualGrades, OfficeDef current)
        {
            var decision = new PromotionDecision();

            OfficeDef next = OfficialLadders.NextOf(current);
            if (next == null)
            {
                decision.ReasonKey = "promotion.reason.ladder_end";
                return decision;
            }
            decision.NextOffice = next;

            if (annualGrades == null || annualGrades.Count == 0)
            {
                decision.ReasonKey = "promotion.reason.no_evaluation";
                return decision;
            }

            NineGrade latest = annualGrades[annualGrades.Count - 1];
            if (latest == NineGrade.ShangShang)
            {
                decision.Eligible = true;
                decision.ReasonKey = "promotion.reason.special_edict";
                return decision;
            }

            if (annualGrades.Count < TermYears)
            {
                decision.ReasonKey = "promotion.reason.term_incomplete";
                return decision;
            }

            bool allPass = true;
            bool anyGood = false;
            for (int i = annualGrades.Count - TermYears; i < annualGrades.Count; i++)
            {
                if (annualGrades[i] < NineGrade.ZhongZhong) { allPass = false; }
                if (annualGrades[i] >= NineGrade.ZhongShang) { anyGood = true; }
            }

            if (!allPass)
            {
                decision.ReasonKey = "promotion.reason.grades_low";
                return decision;
            }
            if (!anyGood)
            {
                decision.ReasonKey = "promotion.reason.no_distinction";
                return decision;
            }

            decision.Eligible = true;
            decision.ReasonKey = "promotion.reason.term_complete";
            return decision;
        }
    }
}
