using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Officials;
using Lingyan.Core.Reputation;
using Lingyan.Core.Social;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    /// <summary>测试用固定随机序列。</summary>
    internal sealed class FixedRng : IRng
    {
        private readonly int[] _rolls;
        private int _i;

        public FixedRng(params int[] rolls)
        {
            _rolls = rolls;
        }

        public int Roll()
        {
            return _rolls[_i++ % _rolls.Length];
        }
    }

    [TestFixture]
    public class SocialTests
    {
        private static TangDate Date()
        {
            return new TangDate("chuigong", 4, 3, 17, 5);
        }

        // ---- 好感合成 ----

        [Test]
        public void Affinity_SameReputation_ReadsOppositeByArchetype()
        {
            // 官声高民望低：官员眼里加分，百姓眼里减分——方向相反（规格第六节）
            var rep = new ReputationState(80, 20, 10);
            var officialItems = AffinityService.DynamicItems(
                rep, NpcArchetype.Official, RobeColor.Commoner, null);
            var commonerItems = AffinityService.DynamicItems(
                rep, NpcArchetype.Commoner, RobeColor.Commoner, null);

            Assert.That(officialItems.Sum(i => i.Delta), Is.GreaterThan(0),
                "官员看官声（80）应加分");
            Assert.That(commonerItems.Sum(i => i.Delta), Is.LessThan(0),
                "百姓看民望（20）应减分");
        }

        [Test]
        public void Affinity_DressSlight_QingRobeBeforeScarletOfficial()
        {
            // 青袍见绯官：服色差两档，衣着失礼 -3（规格第六节例）
            var rep = new ReputationState(50, 50, 50);
            var items = AffinityService.DynamicItems(
                rep, NpcArchetype.Official, RobeColor.Qing, RobeColor.Scarlet);
            Assert.That(items.Any(i => i.SourceKey == "affinity.dyn.dress_slight"
                && i.Delta == -3), Is.True);

            // 绿袍见绯官：只差一档，不失礼
            var ok = AffinityService.DynamicItems(
                rep, NpcArchetype.Official, RobeColor.Green, RobeColor.Scarlet);
            Assert.That(ok.Any(i => i.SourceKey == "affinity.dyn.dress_slight"), Is.False);
        }

        [Test]
        public void Affinity_Total_SumsBaseLedgerAndDynamics()
        {
            NpcProfile huan = NpcProfiles.Get("huan_fuzi");
            var state = new NpcState();
            state.Add(+8, "affinity.src.gift_liked", "s");
            state.Add(-15, "affinity.src.insult", "s");
            var rep = new ReputationState(50, 50, 50); // 无动态项区间
            int total = AffinityService.Total(
                huan, state, rep, NpcArchetype.Commoner, RobeColor.Commoner, null);
            Assert.That(total, Is.EqualTo(45 + 8 - 15));
        }

        // ---- 互动 ----

        [Test]
        public void Greet_FirstMeetThenDailyLimit()
        {
            var state = new NpcState();
            TangDate date = Date();

            var first = InteractionService.Greet(state, date);
            Assert.That(first.TextKey, Is.EqualTo("interact.result.greet_first"));
            Assert.That(state.Met, Is.True);
            Assert.That(state.Ledger[0].Delta, Is.EqualTo(2));

            var again = InteractionService.Greet(state, date);
            Assert.That(again.TextKey, Is.EqualTo("interact.result.greet_again"),
                "同日重复寒暄无增益");
            Assert.That(state.Ledger.Count, Is.EqualTo(1));

            date.AdvanceDays(1);
            var nextDay = InteractionService.Greet(state, date);
            Assert.That(nextDay.TextKey, Is.EqualTo("interact.result.greet"));
            Assert.That(state.Ledger[1].Delta, Is.EqualTo(1));
        }

        [Test]
        public void Gift_TasteMatters_MoneyGated()
        {
            NpcProfile huan = NpcProfiles.Get("huan_fuzi"); // 好书籍绢帛
            var state = new NpcState();
            GiftDef book = GiftCatalog.Get("gift_wenxuan");
            GiftDef pepper = GiftCatalog.Get("gift_hujiao");

            var liked = InteractionService.Gift(huan, state, book, 10_000, Date());
            Assert.That(liked.Success, Is.True);
            Assert.That(liked.MoneyDeltaWen, Is.EqualTo(-800));
            Assert.That(state.Ledger[0].Delta, Is.EqualTo(+8), "投其所好 +8");

            var wrong = InteractionService.Gift(huan, state, pepper, 10_000, Date());
            Assert.That(state.Ledger[1].Delta, Is.EqualTo(-4), "送错了反而减分");
            Assert.That(wrong.TextKey, Is.EqualTo("interact.result.gift_wrong"));

            var broke = InteractionService.Gift(huan, state, book, 100, Date());
            Assert.That(broke.Success, Is.False, "钱不够");
            Assert.That(state.Ledger.Count, Is.EqualTo(2), "失败不记账");
        }

        [Test]
        public void Insult_ProudNpc_SetsHiddenFlag()
        {
            NpcProfile zheng = NpcProfiles.Get("zheng_wu"); // Proud
            var state = new NpcState();
            var result = InteractionService.Insult(zheng, state, Date());
            Assert.That(state.Ledger[0].Delta, Is.EqualTo(-15));
            Assert.That(result.ReputationDeltas.Any(
                d => d.track == ReputationTrack.MinWang && d.delta == -2), Is.True);
            Assert.That(state.Flags, Does.Contain("insulted_proud_zheng_wu"),
                "傲慢者记恨——隐藏支线钩子（规格第六节）");
            Assert.That(result.TextKey, Is.EqualTo("interact.result.insult_proud"));
        }

        [Test]
        public void Spar_WinRaisesJianghu_LoseEarnsRespect()
        {
            NpcProfile zheng = NpcProfiles.Get("zheng_wu");
            var stateWin = new NpcState();
            var win = InteractionService.Spar(zheng, stateWin, 8, 9, new FixedRng(0), Date());
            Assert.That(win.ReputationDeltas.Single().track, Is.EqualTo(ReputationTrack.JiangHu));
            Assert.That(win.ReputationDeltas.Single().delta, Is.EqualTo(+4), "赢了涨江湖名望");

            var stateLose = new NpcState();
            var lose = InteractionService.Spar(zheng, stateLose, 8, 9, new FixedRng(99), Date());
            Assert.That(lose.ReputationDeltas.Single().delta, Is.EqualTo(-2), "输了掉一点");
            Assert.That(stateLose.Ledger[0].Delta, Is.EqualTo(+4), "但对方好感反升");

            NpcProfile huan = NpcProfiles.Get("huan_fuzi"); // 不应战
            var refuse = InteractionService.Spar(huan, new NpcState(), 8, 9, new FixedRng(0), Date());
            Assert.That(refuse.Success, Is.False);
            Assert.That(refuse.TextKey, Is.EqualTo("interact.result.spar_refused"));
        }

        [Test]
        public void Steal_SuccessRateFollowsWisdomAndAlertness()
        {
            NpcProfile huan = NpcProfiles.Get("huan_fuzi");   // 警觉 25
            NpcProfile zheng = NpcProfiles.Get("zheng_wu");   // 警觉 80
            // 智慧 12：对桓 40+(12-2)*5=90；对郑 40+(12-8)*5=60 —— 单调性
            var okRoll = new FixedRng(59);
            var s1 = new NpcState();
            var easy = InteractionService.Steal(huan, s1, 12, false, okRoll, Date());
            Assert.That(easy.Success, Is.True);
            Assert.That(easy.MoneyDeltaWen, Is.EqualTo(20), "得手取其半囊");
            Assert.That(easy.WantedDelta, Is.EqualTo(0), "无人看见不惹官非");

            var s2 = new NpcState();
            var seen = InteractionService.Steal(huan, s2, 12, true, new FixedRng(59), Date());
            Assert.That(seen.Success, Is.True);
            Assert.That(seen.WantedDelta, Is.EqualTo(5), "有目击：通缉值上升（规格第六节）");
            Assert.That(seen.ReputationDeltas.Count, Is.EqualTo(2), "官声民望双降");

            var s3 = new NpcState();
            var caught = InteractionService.Steal(zheng, s3, 4, false, new FixedRng(59), Date());
            Assert.That(caught.Success, Is.False, "智慧 4 对警觉 80：40+(4-8)*5=20 < 59 失手");
            Assert.That(caught.WantedDelta, Is.EqualTo(10));
            Assert.That(s3.Ledger[0].Delta, Is.EqualTo(-20));
        }

        [Test]
        public void Steal_Witnesses_ComeFromScheduleTable()
        {
            // 卯时（3）：康三与桓夫子都不在坊井——对郑五下手无目击？
            // 卯时郑五在武侯铺当值、康三候坊门、桓夫子在坊井汲水——互不同地
            Assert.That(InteractionService.WitnessesAt("wuhou_post", "zheng_wu", 3), Is.False);
            // 戌时（10）：康三归家安睡、郑五巡巷——康家小院无第三者
            Assert.That(InteractionService.WitnessesAt("home_kang", "kang_san", 10), Is.False);
            // 卯时坊南门：康三在候门——对郑五（不在场）无关；对康三下手，问坊门处有无他人
            Assert.That(InteractionService.WitnessesAt("south_gate", "kang_san", 3), Is.False);
        }

        [Test]
        public void AskAround_NeedsAffinity_AnswersFromSchedule()
        {
            var cold = InteractionService.AskAround(20, "kang_san", 5, new FixedRng(0));
            Assert.That(cold.Success, Is.False, "好感不足只得敷衍");

            var warm = InteractionService.AskAround(60, "kang_san", 5, new FixedRng(0));
            Assert.That(warm.Success, Is.True);
            Assert.That(warm.TextParam, Is.EqualTo("west_market"),
                "巳时康三在西市——答案直接来自作息表");
        }

        [Test]
        public void Rumors_AllKeysExistInCatalog()
        {
            var catalog = Core.Localization.LocalizationCatalog.Parse(TestData.StringsJson());
            for (int roll = 0; roll < 5; roll++)
            {
                string key = InteractionService.RumorKey(new FixedRng(roll));
                Assert.That(catalog.Has(key), Is.True, "缺传闻词条: " + key);
            }
        }

        [Test]
        public void Profiles_CoverAllWardNpcs()
        {
            foreach (var npc in Core.World.SampleWard.Npcs)
            {
                Assert.That(NpcProfiles.Get(npc.NpcId), Is.Not.Null,
                    "坊民缺社交档案: " + npc.NpcId);
            }
        }
    }
}
