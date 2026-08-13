using System.Collections.Generic;
using Lingyan.Core.Calendar;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Social
{
    /// <summary>
    /// 雇佣（佣保/长随）：坊民受雇随行，月钱随支俸结算。
    /// 门槛 = 好感 ≥ 40 且付得起首月佣钱；同一时间只养一名长随。
    /// 实益 = 打听免情面（长随替你跑腿，消息必到）。
    /// 佣钱结不出即自动辞退并掉好感——养人是持续开销，不是一次性买断。
    /// </summary>
    public static class RetainerService
    {
        /// <summary>NpcState.Flags 里的受雇标记。</summary>
        public const string RetainerFlag = "retainer";

        /// <summary>雇人托付身家，须比打听（30）更相熟。</summary>
        public const int MinAffinity = 40;

        public static bool IsHired(SaveData save, string npcId)
        {
            return save.NpcStates.TryGetValue(npcId, out SaveNpcState state)
                && state.Flags.Contains(RetainerFlag);
        }

        /// <summary>当前长随的 npcId；没雇人 = null。</summary>
        public static string HiredRetainerId(SaveData save)
        {
            foreach (KeyValuePair<string, SaveNpcState> pair in save.NpcStates)
            {
                if (pair.Value.Flags.Contains(RetainerFlag)) { return pair.Key; }
            }
            return null;
        }

        /// <summary>
        /// 雇为佣保。成功：首月佣钱走 MoneyDeltaWen（由 OutcomeApplier 统一落账）、
        /// 状态记 retainer 旗标、好感 +5（有月钱进项）。失败原因逐一分明。
        /// </summary>
        public static InteractionResult Hire(
            SaveData save, NpcProfile profile, NpcState state,
            int affinityTotal, TangDate date)
        {
            var result = new InteractionResult();
            if (profile.HireWageWen == null)
            {
                result.Success = false;
                result.TextKey = "interact.result.hire_not_hireable";
                return result;
            }
            if (state.Flags.Contains(RetainerFlag))
            {
                result.Success = false;
                result.TextKey = "interact.result.hire_already";
                return result;
            }
            if (HiredRetainerId(save) != null)
            {
                result.Success = false;
                result.TextKey = "interact.result.hire_other";
                return result;
            }
            if (affinityTotal < MinAffinity)
            {
                result.Success = false;
                result.TextKey = "interact.result.hire_low_affinity";
                return result;
            }
            int wage = profile.HireWageWen.Value;
            if (save.MoneyWen < wage)
            {
                result.Success = false;
                result.TextKey = "interact.result.hire_no_money";
                return result;
            }

            result.MoneyDeltaWen = -wage;
            state.Flags.Add(RetainerFlag);
            state.Add(+5, "affinity.src.hired", date.ToStamp());
            result.AffinityWritten.Add(state.Ledger[state.Ledger.Count - 1]);
            result.TextKey = "interact.result.hire_ok";
            return result;
        }

        /// <summary>
        /// 支俸时结佣钱。付得起：扣钱返 true。付不起：当场辞退
        /// （旗标摘除 + 好感 -10）返 false。没雇人返 true 且不动账。
        /// 结算结果由调用方（书房支俸流程）呈现给玩家。
        /// </summary>
        public static bool SettleMonthlyWage(SaveData save, TangDate date, out string npcId)
        {
            npcId = HiredRetainerId(save);
            if (npcId == null) { return true; }

            NpcProfile profile = NpcProfiles.Get(npcId);
            int wage = profile.HireWageWen.Value;
            if (save.MoneyWen >= wage)
            {
                save.MoneyWen -= wage;
                return true;
            }

            NpcState state = NpcStateStore.Load(save, npcId);
            state.Flags.Remove(RetainerFlag);
            state.Add(-10, "affinity.src.dismissed_broke", date.ToStamp());
            NpcStateStore.Store(save, npcId, state);
            return false;
        }
    }
}
