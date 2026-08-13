using System;
using Lingyan.Core.Calendar;
using Lingyan.Core.Characters;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class RetainerTests
    {
        private static SaveData NewSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        private static TangDate Date(SaveData save)
        {
            return new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
        }

        [Test]
        public void Hire_Succeeds_WageViaOutcome_FlagAndAffinitySet()
        {
            SaveData save = NewSave(); // 明镜 6000 文
            NpcProfile kang = NpcProfiles.Get("kang_san");
            var state = new NpcState();

            InteractionResult result = RetainerService.Hire(
                save, kang, state, affinityTotal: 50, Date(save));

            Assert.That(result.Success, Is.True);
            Assert.That(result.MoneyDeltaWen, Is.EqualTo(-500),
                "首月佣钱走统一落账口径（OutcomeApplier）");
            Assert.That(state.Flags, Does.Contain(RetainerService.RetainerFlag));
            Assert.That(state.Ledger[state.Ledger.Count - 1].Delta, Is.EqualTo(+5),
                "受雇有月钱进项，好感 +5");
            Assert.That(result.TextKey, Is.EqualTo("interact.result.hire_ok"));
        }

        [Test]
        public void Hire_LowAffinity_Refused_NothingChanges()
        {
            SaveData save = NewSave();
            var state = new NpcState();
            InteractionResult result = RetainerService.Hire(
                save, NpcProfiles.Get("kang_san"), state, affinityTotal: 39, Date(save));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TextKey, Is.EqualTo("interact.result.hire_low_affinity"));
            Assert.That(state.Flags, Is.Empty);
            Assert.That(state.Ledger, Is.Empty, "失败不记账");
        }

        [Test]
        public void Hire_NoMoney_Refused()
        {
            SaveData save = NewSave();
            save.MoneyWen = 499;
            InteractionResult result = RetainerService.Hire(
                save, NpcProfiles.Get("kang_san"), new NpcState(), 60, Date(save));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TextKey, Is.EqualTo("interact.result.hire_no_money"));
        }

        [Test]
        public void Hire_PublicServant_NotHireable()
        {
            SaveData save = NewSave();
            InteractionResult result = RetainerService.Hire(
                save, NpcProfiles.Get("zheng_wu"), new NpcState(), 90, Date(save));
            Assert.That(result.Success, Is.False, "武侯是公职，不受佣雇");
            Assert.That(result.TextKey, Is.EqualTo("interact.result.hire_not_hireable"));
        }

        [Test]
        public void Hire_SecondTime_Refused()
        {
            SaveData save = NewSave();
            NpcState state = HireKang(save);
            InteractionResult again = RetainerService.Hire(
                save, NpcProfiles.Get("kang_san"), state, 60, Date(save));
            Assert.That(again.Success, Is.False);
            Assert.That(again.TextKey, Is.EqualTo("interact.result.hire_already"));
        }

        [Test]
        public void HiredRetainerId_FindsFlagInSave()
        {
            SaveData save = NewSave();
            Assert.That(RetainerService.HiredRetainerId(save), Is.Null);
            HireKang(save);
            Assert.That(RetainerService.HiredRetainerId(save), Is.EqualTo("kang_san"));
            Assert.That(RetainerService.IsHired(save, "kang_san"), Is.True);
        }

        [Test]
        public void MonthlyWage_Paid_WhenAffordable()
        {
            SaveData save = NewSave();
            HireKang(save);
            long before = save.MoneyWen;

            bool paid = RetainerService.SettleMonthlyWage(save, Date(save), out string npcId);

            Assert.That(paid, Is.True);
            Assert.That(npcId, Is.EqualTo("kang_san"));
            Assert.That(save.MoneyWen, Is.EqualTo(before - 500), "月钱 500 文照扣");
            Assert.That(RetainerService.IsHired(save, "kang_san"), Is.True, "结得出钱就留任");
        }

        [Test]
        public void MonthlyWage_Broke_DismissesAndDropsAffinity()
        {
            SaveData save = NewSave();
            HireKang(save);
            save.MoneyWen = 3;

            bool paid = RetainerService.SettleMonthlyWage(save, Date(save), out string npcId);

            Assert.That(paid, Is.False);
            Assert.That(npcId, Is.EqualTo("kang_san"));
            Assert.That(save.MoneyWen, Is.EqualTo(3), "付不起就不扣，不许扣成负数");
            Assert.That(RetainerService.IsHired(save, "kang_san"), Is.False, "当场辞退");
            SaveNpcState state = save.NpcStates["kang_san"];
            SaveAffinityEntry last = state.Ledger[state.Ledger.Count - 1];
            Assert.That(last.Delta, Is.EqualTo(-10), "辞退掉好感");
            Assert.That(last.SourceKey, Is.EqualTo("affinity.src.dismissed_broke"));
        }

        [Test]
        public void MonthlyWage_NoRetainer_NoOp()
        {
            SaveData save = NewSave();
            long before = save.MoneyWen;
            bool paid = RetainerService.SettleMonthlyWage(save, Date(save), out string npcId);
            Assert.That(paid, Is.True);
            Assert.That(npcId, Is.Null);
            Assert.That(save.MoneyWen, Is.EqualTo(before));
        }

        [Test]
        public void AskAround_WithRetainer_BypassesAffinityGate()
        {
            var cold = InteractionService.AskAround(
                20, "kang_san", 5, new FixedRng(0), hasRetainer: false);
            Assert.That(cold.Success, Is.False, "无长随、好感不足：敷衍");

            var withRetainer = InteractionService.AskAround(
                20, "kang_san", 5, new FixedRng(0), hasRetainer: true);
            Assert.That(withRetainer.Success, Is.True, "长随跑腿，消息必到");
            Assert.That(withRetainer.TextParam, Is.EqualTo("west_market"),
                "答案仍来自作息表，不是编的");
        }

        /// <summary>把康三雇成长随（走真实路径并入档）。</summary>
        private static NpcState HireKang(SaveData save)
        {
            NpcState state = NpcStateStore.Load(save, "kang_san");
            InteractionResult result = RetainerService.Hire(
                save, NpcProfiles.Get("kang_san"), state, 60, Date(save));
            Assert.That(result.Success, Is.True, "前置：雇佣成功");
            save.MoneyWen += result.MoneyDeltaWen;
            NpcStateStore.Store(save, "kang_san", state);
            return state;
        }
    }
}
