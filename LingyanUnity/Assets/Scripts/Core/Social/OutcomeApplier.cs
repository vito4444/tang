using Lingyan.Core.Calendar;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;

namespace Lingyan.Core.Social
{
    /// <summary>
    /// 互动/对话结算的唯一落档口径：名誉（带账）、钱（不为负）、通缉。
    /// UI 一律经此写档，保证互动菜单与对话树的后果同一套规则。
    /// </summary>
    public static class OutcomeApplier
    {
        public static void ApplyReputation(
            SaveData save, ReputationTrack track, int delta, string sourceKey, TangDate date)
        {
            var reputation = new ReputationState(
                save.Reputation.GuanSheng, save.Reputation.MinWang, save.Reputation.JiangHu);
            reputation.Apply(track, delta, sourceKey, date.ToStamp());
            save.Reputation.GuanSheng = reputation.GuanSheng;
            save.Reputation.MinWang = reputation.MinWang;
            save.Reputation.JiangHu = reputation.JiangHu;
            save.ReputationLedger.Add(new SaveLedgerEntry
            {
                Track = track.ToString(),
                Delta = delta,
                SourceKey = sourceKey,
                DateStamp = date.ToStamp()
            });
        }

        public static void ApplyMoney(SaveData save, long deltaWen)
        {
            long money = save.MoneyWen + deltaWen;
            save.MoneyWen = money < 0 ? 0 : money;
        }

        public static void ApplyWanted(SaveData save, int delta)
        {
            int wanted = save.WantedLevel + delta;
            save.WantedLevel = wanted < 0 ? 0 : wanted;
        }

        /// <summary>互动结果整单落档（好感账本已在 InteractionService 内写入 state）。</summary>
        public static void ApplyInteraction(
            SaveData save, InteractionResult result, TangDate date)
        {
            foreach (var (track, delta, sourceKey) in result.ReputationDeltas)
            {
                ApplyReputation(save, track, delta, sourceKey, date);
            }
            ApplyMoney(save, result.MoneyDeltaWen);
            ApplyWanted(save, result.WantedDelta);
        }
    }
}
