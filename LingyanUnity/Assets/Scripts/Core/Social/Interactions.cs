using System;
using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Reputation;
using Lingyan.Core.World;

namespace Lingyan.Core.Social
{
    /// <summary>可注入随机源（测试用固定序列）。</summary>
    public interface IRng
    {
        /// <summary>0–99。</summary>
        int Roll();
    }

    public sealed class SystemRng : IRng
    {
        private readonly Random _random = new Random();

        public int Roll()
        {
            return _random.Next(0, 100);
        }
    }

    /// <summary>一次互动的结算：文本、好感、名誉、钱、通缉，全部显式列出。</summary>
    public sealed class InteractionResult
    {
        /// <summary>结果文本键（interact.result.*）。</summary>
        public string TextKey { get; set; }

        /// <summary>文本参数（本地化键或数字字符串，可空）。</summary>
        public string TextParam { get; set; }

        public bool Success { get; set; } = true;

        /// <summary>好感账本新条目（已写入 NpcState）。</summary>
        public List<AffinityEntry> AffinityWritten { get; } = new List<AffinityEntry>();

        /// <summary>名誉变动（轨, Δ, 来源键）——由调用方落到 ReputationState 与存档。</summary>
        public List<(ReputationTrack track, int delta, string sourceKey)> ReputationDeltas { get; }
            = new List<(ReputationTrack, int, string)>();

        public long MoneyDeltaWen { get; set; }

        public int WantedDelta { get; set; }
    }

    /// <summary>
    /// 互动规则（规格第六节）。全部纯函数化：输入状态 + 随机源，输出显式结算；
    /// 数值规则见各方法注释，测试逐条锁定。
    /// </summary>
    public static class InteractionService
    {
        // ---- 对话（阶段 4 对话树前的寒暄占位）----
        // 首次 +2（初识），此后每日一次 +1；同日重复无增益。
        public static InteractionResult Greet(NpcState state, TangDate date)
        {
            var result = new InteractionResult();
            string day = date.EraId + ":" + date.EraYear + ":" + date.Month + ":" + date.Day;
            if (!state.Met)
            {
                state.Met = true;
                state.LastGreetDay = day;
                state.Add(+2, "affinity.src.first_meet", date.ToStamp());
                result.AffinityWritten.Add(state.Ledger[state.Ledger.Count - 1]);
                result.TextKey = "interact.result.greet_first";
            }
            else if (state.LastGreetDay != day)
            {
                state.LastGreetDay = day;
                state.Add(+1, "affinity.src.greet", date.ToStamp());
                result.AffinityWritten.Add(state.Ledger[state.Ledger.Count - 1]);
                result.TextKey = "interact.result.greet";
            }
            else
            {
                result.TextKey = "interact.result.greet_again";
            }
            return result;
        }

        // ---- 送礼 ----
        // 礼从行囊出（市集购入，v4 起）；投其所好 +8；不合口味 -4（送错了反而减分）；
        // 行囊里没有直接失败。消耗行囊由调用方在成功后执行（MarketService.TakeOne）。
        public static InteractionResult Gift(
            NpcProfile profile, NpcState state, GiftDef gift, int ownedCount, TangDate date)
        {
            var result = new InteractionResult();
            if (ownedCount < 1)
            {
                result.Success = false;
                result.TextKey = "interact.result.gift_none";
                return result;
            }
            bool liked = profile.Tastes.Contains(gift.Taste);
            int delta = liked ? +8 : -4;
            state.Add(delta, liked ? "affinity.src.gift_liked" : "affinity.src.gift_wrong",
                date.ToStamp(), gift.NameKey);
            result.AffinityWritten.Add(state.Ledger[state.Ledger.Count - 1]);
            result.TextKey = liked ? "interact.result.gift_liked" : "interact.result.gift_wrong";
            result.TextParam = gift.NameKey;
            return result;
        }

        // ---- 辱骂 ----
        // 好感 -15；当众失态民望 -2；傲慢者另记恨（隐藏支线钩子，旗标入档）。
        public static InteractionResult Insult(
            NpcProfile profile, NpcState state, TangDate date)
        {
            var result = new InteractionResult();
            state.Add(-15, "affinity.src.insult", date.ToStamp());
            result.AffinityWritten.Add(state.Ledger[state.Ledger.Count - 1]);
            result.ReputationDeltas.Add(
                (ReputationTrack.MinWang, -2, "rep.src.public_insult"));
            if ((profile.Personality & Personality.Proud) != 0)
            {
                string flag = "insulted_proud_" + profile.NpcId;
                if (!state.Flags.Contains(flag))
                {
                    state.Flags.Add(flag);
                }
                result.TextKey = "interact.result.insult_proud";
            }
            else
            {
                result.TextKey = "interact.result.insult";
            }
            return result;
        }

        // ---- 切磋（非致命；正式战斗阶段 7 接手判定内核）----
        // 胜率 = 50 + (力量+体力 − 身手/10)×5，夹 15–85。
        // 胜：江湖 +4；负：江湖 −2、对方好感 +4（不打不相识）。不应战者直接婉拒。
        public static InteractionResult Spar(
            NpcProfile profile, NpcState state,
            int strength, int stamina, IRng rng, TangDate date)
        {
            var result = new InteractionResult();
            if (profile.Prowess == null)
            {
                result.Success = false;
                result.TextKey = "interact.result.spar_refused";
                return result;
            }
            int chance = 50 + (strength + stamina - profile.Prowess.Value / 10) * 5;
            chance = Math.Max(15, Math.Min(85, chance));
            bool win = rng.Roll() < chance;
            if (win)
            {
                result.ReputationDeltas.Add(
                    (ReputationTrack.JiangHu, +4, "rep.src.spar_win"));
                state.Add(+1, "affinity.src.spar", date.ToStamp());
                result.TextKey = "interact.result.spar_win";
            }
            else
            {
                result.ReputationDeltas.Add(
                    (ReputationTrack.JiangHu, -2, "rep.src.spar_lose"));
                state.Add(+4, "affinity.src.spar_respect", date.ToStamp());
                result.TextKey = "interact.result.spar_lose";
            }
            result.AffinityWritten.Add(state.Ledger[state.Ledger.Count - 1]);
            return result;
        }

        // ---- 偷窃 ----
        // 成功率 = 40 + (智慧 − 警觉/10)×5，夹 5–90。
        // 得手且无人在场：得钱无名誉损失；得手但同地点有他人（目击半径=同一地点）：
        // 民望 −4 官声 −3 通缉 +5；失手被抓：官声 −6 民望 −8 好感 −20 通缉 +10。
        public static InteractionResult Steal(
            NpcProfile targetProfile, NpcState targetState,
            int wisdom, bool witnessesPresent, IRng rng, TangDate date)
        {
            var result = new InteractionResult();
            int chance = 40 + (wisdom - targetProfile.Alertness / 10) * 5;
            chance = Math.Max(5, Math.Min(90, chance));
            bool success = rng.Roll() < chance;
            if (success)
            {
                int takeWen = Math.Max(10, targetProfile.PurseWen / 2);
                result.MoneyDeltaWen = takeWen;
                result.TextKey = "interact.result.steal_ok";
                result.TextParam = takeWen.ToString();
                if (witnessesPresent)
                {
                    result.ReputationDeltas.Add(
                        (ReputationTrack.MinWang, -4, "rep.src.steal_seen"));
                    result.ReputationDeltas.Add(
                        (ReputationTrack.GuanSheng, -3, "rep.src.steal_seen"));
                    result.WantedDelta = 5;
                    result.TextKey = "interact.result.steal_seen";
                }
            }
            else
            {
                result.Success = false;
                targetState.Add(-20, "affinity.src.steal_caught", date.ToStamp());
                result.AffinityWritten.Add(targetState.Ledger[targetState.Ledger.Count - 1]);
                result.ReputationDeltas.Add(
                    (ReputationTrack.GuanSheng, -6, "rep.src.steal_caught"));
                result.ReputationDeltas.Add(
                    (ReputationTrack.MinWang, -8, "rep.src.steal_caught"));
                result.WantedDelta = 10;
                result.TextKey = "interact.result.steal_caught";
            }
            return result;
        }

        /// <summary>目击判定：同一时辰同一地点是否有第三者（作息表联动）。</summary>
        public static bool WitnessesAt(string placeId, string exceptNpcId, int hourIndex)
        {
            return SampleWard.Npcs.Any(npc =>
                npc.NpcId != exceptNpcId && npc.At(hourIndex).PlaceId == placeId);
        }

        // ---- 打听 ----
        // 好感 ≥30 才肯细说：给出指定目标此刻去向 + 一条传闻；不足则敷衍。
        public static InteractionResult AskAround(
            int affinityTotal, string aboutNpcId, int hourIndex, IRng rng)
        {
            var result = new InteractionResult();
            if (affinityTotal < 30)
            {
                result.Success = false;
                result.TextKey = "interact.result.ask_brushoff";
                return result;
            }
            NpcScheduleDef target = SampleWard.Npcs.First(n => n.NpcId == aboutNpcId);
            ScheduleEntry entry = target.At(hourIndex);
            result.TextKey = "interact.result.ask_whereabouts";
            result.TextParam = entry.PlaceId;
            return result;
        }

        /// <summary>传闻池：一条随机市井传闻（阶段 5 案件在此埋线）。</summary>
        public static string RumorKey(IRng rng)
        {
            string[] pool =
            {
                "rumor.gate_repair",
                "rumor.pepper_arrived",
                "rumor.wuhou_rotation",
                "rumor.silk_case",
                "rumor.well_tale"
            };
            return pool[rng.Roll() % pool.Length];
        }
    }
}
