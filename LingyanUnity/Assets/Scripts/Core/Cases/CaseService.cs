using System;
using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Cases
{
    /// <summary>
    /// 案件流程：接案 → 走访集线索 → 组合推论 → 关键证人（属性/好感门槛）
    /// → 指认 → 结案（双结局）。状态入存档 v3（SaveCaseState）。
    /// </summary>
    public static class CaseService
    {
        /// <summary>接案：登记状态并起算期限。</summary>
        public static SaveCaseState Open(SaveData save, CaseDef def, TangDate now)
        {
            if (save.Cases.TryGetValue(def.Id, out SaveCaseState existing))
            {
                return existing;
            }
            var state = new SaveCaseState
            {
                Status = (int)CaseStatus.Active,
                OpenedStamp = now.ToStamp(),
                DeadlineStamp = DeadlineOf(def, now).ToStamp()
            };
            save.Cases[def.Id] = state;
            return state;
        }

        public static TangDate DeadlineOf(CaseDef def, TangDate opened)
        {
            TangDate deadline = opened.Clone();
            deadline.AdvanceDays(def.DeadlineDays);
            return deadline;
        }

        /// <summary>剩余整日数（不足返回 0，已过期返回负数）。以日为粒度。</summary>
        public static int DaysLeft(SaveCaseState state, TangDate now)
        {
            TangDate deadline = TangDate.FromStamp(state.DeadlineStamp);
            int deadlineOrdinal = Ordinal(deadline);
            return deadlineOrdinal - Ordinal(now);
        }

        private static int Ordinal(TangDate date)
        {
            // 以年号起年折算连续日序（同一年号内单调；改元由剧本控制，案件期限不跨改元）
            int year = EraTable.ToGregorianYear(date.EraId, date.EraYear);
            return year * 360 + (date.Month - 1) * 30 + date.Day;
        }

        /// <summary>过期检查：逾期未结自动作废（失败也是内容，吃考课）。</summary>
        public static bool CheckExpire(SaveCaseState state, TangDate now)
        {
            if (state.Status == (int)CaseStatus.Active && DaysLeft(state, now) < 0)
            {
                state.Status = (int)CaseStatus.Expired;
                state.OutcomeKey = "case.outcome.expired";
                return true;
            }
            return false;
        }

        /// <summary>发现线索（幂等）。</summary>
        public static bool Discover(SaveCaseState state, CaseDef def, string clueId)
        {
            if (def.Clue(clueId) == null)
            {
                throw new ArgumentException("未知线索: " + clueId);
            }
            if (state.Clues.Contains(clueId))
            {
                return false;
            }
            state.Clues.Add(clueId);
            return true;
        }

        /// <summary>组合两条已有线索；命中定义则得推论（幂等），否则 null。</summary>
        public static InferenceDef Combine(
            SaveCaseState state, CaseDef def, string clueA, string clueB)
        {
            if (!state.Clues.Contains(clueA) || !state.Clues.Contains(clueB))
            {
                return null;
            }
            InferenceDef inference = def.Inferences
                .FirstOrDefault(i => i.Matches(clueA, clueB));
            if (inference != null && !state.Inferences.Contains(inference.Id))
            {
                state.Inferences.Add(inference.Id);
            }
            return inference;
        }

        /// <summary>证据数 = 线索 + 推论。</summary>
        public static int EvidenceCount(SaveCaseState state)
        {
            return state.Clues.Count + state.Inferences.Count;
        }

        /// <summary>关键证人是否肯开口（智慧或好感二者其一达标）。</summary>
        public static bool WitnessWillTalk(CaseDef def, int wisdom, int affinityTotal)
        {
            return wisdom >= def.Witness.MinWisdom
                || affinityTotal >= def.Witness.MinAffinity;
        }

        /// <summary>
        /// 指认结案（规格第七节的双结局）：
        /// 严刑速破——立即可用；指认对：官声+6 民望-4 功绩20；
        ///   指认错：officially "破了"，官声+2 民望-8，冤案旗标（后患）+功绩10。
        /// 缓查详审——需证据 ≥ 阈值且证人开口；指认对：官声+4 民望+6 功绩30；
        ///   指认错（凑够证据仍指错人）：官声-4 民望-6 冤案，功绩0。
        /// </summary>
        public static AccusationOutcome Accuse(
            SaveCaseState state, CaseDef def, string suspectId,
            AccuseMethod method, bool witnessTalked)
        {
            if (state.Status != (int)CaseStatus.Active)
            {
                throw new InvalidOperationException("案件不在办理中");
            }
            if (method == AccuseMethod.Thorough)
            {
                if (EvidenceCount(state) < def.ThoroughEvidenceCount || !witnessTalked)
                {
                    throw new InvalidOperationException("详审证据未足或证人未开口");
                }
            }

            bool correct = suspectId == def.TrueCulpritId;
            var outcome = new AccusationOutcome
            {
                CorrectCulprit = correct,
                Method = method,
                WrongfulConviction = !correct
            };

            if (method == AccuseMethod.Forced)
            {
                outcome.GuanShengDelta = correct ? +6 : +2;
                outcome.MinWangDelta = correct ? -4 : -8;
                outcome.MeritPoints = correct ? 20 : 10;
                outcome.OutcomeKey = correct
                    ? "case.outcome.forced_right"
                    : "case.outcome.forced_wrong";
            }
            else
            {
                outcome.GuanShengDelta = correct ? +4 : -4;
                outcome.MinWangDelta = correct ? +6 : -6;
                outcome.MeritPoints = correct ? 30 : 0;
                outcome.OutcomeKey = correct
                    ? "case.outcome.thorough_right"
                    : "case.outcome.thorough_wrong";
            }

            state.Status = (int)CaseStatus.Closed;
            state.Accused = suspectId;
            state.OutcomeKey = outcome.OutcomeKey;
            if (!correct)
            {
                state.WrongfulConviction = true;
            }
            return outcome;
        }
    }
}
