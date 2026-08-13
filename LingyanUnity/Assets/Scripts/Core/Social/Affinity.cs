using System.Collections.Generic;
using Lingyan.Core.Officials;
using Lingyan.Core.Reputation;

namespace Lingyan.Core.Social
{
    /// <summary>好感账本一条（来源与时间可回溯，悬停面板逐条展示）。</summary>
    public sealed class AffinityEntry
    {
        public int Delta { get; set; }

        /// <summary>来源本地化键（affinity.src.*）。</summary>
        public string SourceKey { get; set; }

        /// <summary>来源附注参数（礼物名键等，可空）。</summary>
        public string SourceParam { get; set; }

        /// <summary>游戏日期戳（chuigong:4:3:17:5）。</summary>
        public string DateStamp { get; set; }
    }

    /// <summary>一名 NPC 的持久社交状态（入存档 v2）。</summary>
    public sealed class NpcState
    {
        public List<AffinityEntry> Ledger { get; } = new List<AffinityEntry>();

        /// <summary>是否已相识（首次对话）。</summary>
        public bool Met { get; set; }

        /// <summary>最近一次寒暄的日戳（每日一次有效）。</summary>
        public string LastGreetDay { get; set; }

        /// <summary>互动旗标（如 insulted_proud，隐藏支线钩子）。</summary>
        public List<string> Flags { get; } = new List<string>();

        public void Add(int delta, string sourceKey, string dateStamp, string param = null)
        {
            Ledger.Add(new AffinityEntry
            {
                Delta = delta,
                SourceKey = sourceKey,
                SourceParam = param,
                DateStamp = dateStamp
            });
        }
    }

    /// <summary>动态好感项：非账本、由当前状态实时算出（名誉观感、衣着礼数）。</summary>
    public sealed class DynamicAffinity
    {
        public int Delta { get; set; }
        public string SourceKey { get; set; }

        /// <summary>展示参数（如官声数值）。</summary>
        public int Param { get; set; }
    }

    /// <summary>
    /// 好感合成（规格第六节）：
    /// 总好感 = 基准 + 账本累计 + 动态项（三轨名誉按 NPC 身份加权、衣着礼数）。
    /// 同一件事在不同人眼里加分减分方向可以相反——方向由身份主导轨决定。
    /// </summary>
    public static class AffinityService
    {
        public const int Min = 0;
        public const int Max = 100;

        /// <summary>身份主导的名誉轨。</summary>
        public static ReputationTrack LeadTrack(NpcArchetype archetype)
        {
            switch (archetype)
            {
                case NpcArchetype.Official:
                case NpcArchetype.Palace:
                    return ReputationTrack.GuanSheng;
                case NpcArchetype.Jianghu:
                    return ReputationTrack.JiangHu;
                case NpcArchetype.Merchant:
                case NpcArchetype.Commoner:
                default:
                    return ReputationTrack.MinWang;
            }
        }

        /// <summary>
        /// 动态项列表。名誉观感：主导轨 ≥55 加分（每 2.5 点加 1），≤25 减分；
        /// 衣着礼数：面对有品官员，服色差两档以上失礼（青袍见绯官）。
        /// </summary>
        public static List<DynamicAffinity> DynamicItems(
            ReputationState reputation, NpcArchetype archetype,
            RobeColor playerRobe, RobeColor? npcOfficialRobe)
        {
            var items = new List<DynamicAffinity>();

            ReputationTrack lead = LeadTrack(archetype);
            int value = reputation.Get(lead);
            if (value >= 55)
            {
                items.Add(new DynamicAffinity
                {
                    Delta = (value - 55) * 2 / 5 + 3,
                    SourceKey = "affinity.dyn.rep_high." + TrackKey(lead),
                    Param = value
                });
            }
            else if (value <= 25)
            {
                items.Add(new DynamicAffinity
                {
                    Delta = -((26 - value) / 2 + 2),
                    SourceKey = "affinity.dyn.rep_low." + TrackKey(lead),
                    Param = value
                });
            }

            if (npcOfficialRobe != null
                && (int)npcOfficialRobe.Value - (int)playerRobe >= 2)
            {
                items.Add(new DynamicAffinity
                {
                    Delta = -3,
                    SourceKey = "affinity.dyn.dress_slight",
                    Param = 0
                });
            }

            return items;
        }

        private static string TrackKey(ReputationTrack track)
        {
            switch (track)
            {
                case ReputationTrack.GuanSheng: return "guansheng";
                case ReputationTrack.JiangHu: return "jianghu";
                default: return "minwang";
            }
        }

        /// <summary>总好感（0–100 夹取）。</summary>
        public static int Total(
            NpcProfile profile, NpcState state,
            ReputationState reputation, NpcArchetype archetype,
            RobeColor playerRobe, RobeColor? npcOfficialRobe)
        {
            int sum = profile.BaseAffinity;
            foreach (AffinityEntry entry in state.Ledger)
            {
                sum += entry.Delta;
            }
            foreach (DynamicAffinity item in DynamicItems(
                reputation, archetype, playerRobe, npcOfficialRobe))
            {
                sum += item.Delta;
            }
            return sum < Min ? Min : sum > Max ? Max : sum;
        }
    }
}
