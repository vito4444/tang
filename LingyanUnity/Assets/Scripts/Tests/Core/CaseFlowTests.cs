using System;
using Lingyan.Core.Calendar;
using Lingyan.Core.Cases;
using Lingyan.Core.Characters;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CaseFlowTests
    {
        private static SaveData NewSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        private static TangDate Now(SaveData save)
        {
            return new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
        }

        /// <summary>把丝帛案办结（严刑指认真凶）。</summary>
        private static void CloseSilk(SaveData save)
        {
            SaveCaseState silk = CaseService.Open(save, SilkCase.Def, Now(save));
            CaseService.Accuse(silk, SilkCase.Def, "bookkeeper",
                AccuseMethod.Forced, witnessTalked: false);
        }

        [Test]
        public void Order_SilkFirst_ThenWell()
        {
            SaveData save = NewSave();
            Assert.That(CaseFlow.Current(save).Id, Is.EqualTo(SilkCase.CaseId));

            CloseSilk(save);
            Assert.That(CaseFlow.Current(save).Id, Is.EqualTo(WellCase.CaseId),
                "丝帛案具结即轮到枯井案");
        }

        [Test]
        public void WellCase_NeedsClosedSilk_AndOffice()
        {
            SaveData save = NewSave();
            Assert.That(CaseFlow.CanOpen(save, WellCase.Def), Is.False, "前案未结不开");

            CloseSilk(save);
            Assert.That(CaseFlow.CanOpen(save, WellCase.Def), Is.False,
                "白身调不动旧案卷宗");

            save.Offices.ZhiShiId = "xian_wei";
            Assert.That(CaseFlow.CanOpen(save, WellCase.Def), Is.True,
                "前案已结 + 官身在手，方许接旧案");
        }

        [Test]
        public void SilkGate_StillRumorDriven()
        {
            SaveData save = NewSave();
            Assert.That(CaseFlow.CanOpen(save, SilkCase.Def), Is.False, "没听过传闻不开卷");
            save.StoryFlags["heard_silk_case"] = true;
            Assert.That(CaseFlow.CanOpen(save, SilkCase.Def), Is.True);
        }

        [Test]
        public void WellCase_FullThoroughPlaythrough()
        {
            SaveData save = NewSave();
            CloseSilk(save);
            save.Offices.ZhiShiId = "xian_wei";

            SaveCaseState state = CaseService.Open(save, WellCase.Def, Now(save));
            Assert.That(CaseService.DaysLeft(state, Now(save)), Is.EqualTo(15),
                "旧案难查，十五日之限");

            CaseService.Discover(state, WellCase.Def, "bone_belt");
            CaseService.Discover(state, WellCase.Def, "well_ledger");
            CaseService.Discover(state, WellCase.Def, "missing_roll");
            CaseService.Discover(state, WellCase.Def, "old_neighbor");

            Assert.That(CaseService.Combine(state, WellCase.Def, "bone_belt", "missing_roll").Id,
                Is.EqualTo("identity_match"), "带銙×旧牍 → 骸骨即坊正");
            Assert.That(CaseService.Combine(state, WellCase.Def, "well_ledger", "missing_roll").Id,
                Is.EqualTo("no_flight"), "淘井簿×旧牍 → 潜逃是伪报");
            Assert.That(CaseService.Combine(state, WellCase.Def, "old_neighbor", "bone_belt").Id,
                Is.EqualTo("debt_quarrel"));

            Assert.That(CaseService.EvidenceCount(state), Is.EqualTo(7));
            Assert.That(CaseService.WitnessWillTalk(WellCase.Def, wisdom: 12, affinityTotal: 0),
                Is.True, "明镜智慧 12 达标");

            AccusationOutcome outcome = CaseService.Accuse(
                state, WellCase.Def, "gambler_boss", AccuseMethod.Thorough, witnessTalked: true);
            Assert.That(outcome.CorrectCulprit, Is.True, "真凶：赌坊主");
            Assert.That(outcome.WrongfulConviction, Is.False);
            Assert.That(CaseFlow.IsClosed(save, WellCase.Def), Is.True);
        }

        [Test]
        public void WellCase_ForcedWrong_IsWrongful()
        {
            SaveData save = NewSave();
            CloseSilk(save);
            save.Offices.ZhiShiId = "xian_wei";
            SaveCaseState state = CaseService.Open(save, WellCase.Def, Now(save));

            AccusationOutcome outcome = CaseService.Accuse(
                state, WellCase.Def, "widow", AccuseMethod.Forced, witnessTalked: false);
            Assert.That(outcome.WrongfulConviction, Is.True, "屈打遗孀成冤——循吏传就此无缘");
            Assert.That(state.WrongfulConviction, Is.True);
        }

        [Test]
        public void AllClosed_CurrentStaysOnLast()
        {
            SaveData save = NewSave();
            CloseSilk(save);
            save.Offices.ZhiShiId = "xian_wei";
            SaveCaseState state = CaseService.Open(save, WellCase.Def, Now(save));
            CaseService.Accuse(state, WellCase.Def, "gambler_boss",
                AccuseMethod.Forced, witnessTalked: false);

            Assert.That(CaseFlow.Current(save).Id, Is.EqualTo(WellCase.CaseId),
                "全部具结后停在末案供回看结语");
        }
    }
}
