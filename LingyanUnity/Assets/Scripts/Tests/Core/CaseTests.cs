using System;
using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Cases;
using Lingyan.Core.Characters;
using Lingyan.Core.Localization;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CaseTests
    {
        private static SaveData NewSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(draft, DateTime.UtcNow);
        }

        private static TangDate Now(SaveData save)
        {
            return new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
        }

        [Test]
        public void Open_SetsDeadline_TenDaysOut()
        {
            SaveData save = NewSave();
            SaveCaseState state = CaseService.Open(save, SilkCase.Def, Now(save));
            Assert.That(state.Status, Is.EqualTo((int)CaseStatus.Active));
            Assert.That(CaseService.DaysLeft(state, Now(save)), Is.EqualTo(10),
                "接案即起算十日之限");
            // 幂等
            SaveCaseState again = CaseService.Open(save, SilkCase.Def, Now(save));
            Assert.That(again, Is.SameAs(state));
        }

        [Test]
        public void Deadline_TimePassing_HasCost()
        {
            SaveData save = NewSave();
            SaveCaseState state = CaseService.Open(save, SilkCase.Def, Now(save));
            TangDate now = Now(save);
            now.AdvanceDays(9);
            Assert.That(CaseService.DaysLeft(state, now), Is.EqualTo(1));
            Assert.That(CaseService.CheckExpire(state, now), Is.False);

            now.AdvanceDays(2);
            Assert.That(CaseService.CheckExpire(state, now), Is.True, "逾期自动作废");
            Assert.That(state.Status, Is.EqualTo((int)CaseStatus.Expired));
            Assert.That(state.OutcomeKey, Is.EqualTo("case.outcome.expired"));
        }

        [Test]
        public void Clues_Combine_IntoInference()
        {
            SaveData save = NewSave();
            SaveCaseState state = CaseService.Open(save, SilkCase.Def, Now(save));

            Assert.That(CaseService.Discover(state, SilkCase.Def, "permit"), Is.True);
            Assert.That(CaseService.Discover(state, SilkCase.Def, "permit"), Is.False, "幂等");
            CaseService.Discover(state, SilkCase.Def, "patrol_log");

            // 未同时持有的组合不产出
            Assert.That(CaseService.Combine(state, SilkCase.Def, "permit", "dossier"), Is.Null);

            InferenceDef inference = CaseService.Combine(
                state, SilkCase.Def, "permit", "patrol_log");
            Assert.That(inference, Is.Not.Null, "路引×巡夜记录 → 时辰对不上");
            Assert.That(inference.Id, Is.EqualTo("time_gap"));
            Assert.That(state.Inferences, Does.Contain("time_gap"));
            Assert.That(CaseService.EvidenceCount(state), Is.EqualTo(3), "2 线索 + 1 推论");

            Assert.Throws<ArgumentException>(
                () => CaseService.Discover(state, SilkCase.Def, "no_such_clue"));
        }

        [Test]
        public void Witness_WisdomOrAffinity_EitherOpens()
        {
            Assert.That(CaseService.WitnessWillTalk(SilkCase.Def, wisdom: 12, affinityTotal: 0),
                Is.True, "明镜智慧 12 ≥ 10，直接问出");
            Assert.That(CaseService.WitnessWillTalk(SilkCase.Def, wisdom: 4, affinityTotal: 60),
                Is.True, "笨拙但交情深也行");
            Assert.That(CaseService.WitnessWillTalk(SilkCase.Def, wisdom: 4, affinityTotal: 20),
                Is.False);
        }

        [Test]
        public void Accuse_Forced_FastButCostly()
        {
            SaveData save = NewSave();
            SaveCaseState state = CaseService.Open(save, SilkCase.Def, Now(save));

            // 严刑不需要证据，立即可用——但指错人是冤案
            AccusationOutcome wrong = CaseService.Accuse(
                state, SilkCase.Def, "idler", AccuseMethod.Forced, witnessTalked: false);
            Assert.That(wrong.CorrectCulprit, Is.False);
            Assert.That(wrong.WrongfulConviction, Is.True, "冤案入档，后期反转找上门");
            Assert.That(wrong.GuanShengDelta, Is.GreaterThan(0), "官面上'破了案'");
            Assert.That(wrong.MinWangDelta, Is.LessThan(0), "百姓不认");
            Assert.That(state.Status, Is.EqualTo((int)CaseStatus.Closed));
            Assert.That(state.WrongfulConviction, Is.True);
        }

        [Test]
        public void Accuse_Thorough_GatedByEvidenceAndWitness()
        {
            SaveData save = NewSave();
            SaveCaseState state = CaseService.Open(save, SilkCase.Def, Now(save));

            Assert.Throws<InvalidOperationException>(
                () => CaseService.Accuse(state, SilkCase.Def, "bookkeeper",
                    AccuseMethod.Thorough, witnessTalked: true),
                "证据不足不得详审指认");

            CaseService.Discover(state, SilkCase.Def, "dossier");
            CaseService.Discover(state, SilkCase.Def, "permit");
            CaseService.Discover(state, SilkCase.Def, "patrol_log");
            CaseService.Discover(state, SilkCase.Def, "torn_silk");
            CaseService.Combine(state, SilkCase.Def, "permit", "patrol_log");
            CaseService.Combine(state, SilkCase.Def, "dossier", "torn_silk");
            Assert.That(CaseService.EvidenceCount(state), Is.EqualTo(6));

            Assert.Throws<InvalidOperationException>(
                () => CaseService.Accuse(state, SilkCase.Def, "bookkeeper",
                    AccuseMethod.Thorough, witnessTalked: false),
                "证人未开口不得详审指认");

            AccusationOutcome outcome = CaseService.Accuse(
                state, SilkCase.Def, "bookkeeper", AccuseMethod.Thorough, witnessTalked: true);
            Assert.That(outcome.CorrectCulprit, Is.True);
            Assert.That(outcome.MinWangDelta, Is.GreaterThan(0), "详审得民心");
            Assert.That(outcome.MeritPoints, Is.EqualTo(30), "功绩计入考课");
            Assert.That(outcome.WrongfulConviction, Is.False);
        }

        [Test]
        public void CaseKeys_AllInCatalog()
        {
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            var missing = new System.Collections.Generic.List<string>();

            void Check(string key)
            {
                if (!catalog.Has(key)) { missing.Add(key); }
            }

            CaseDef def = SilkCase.Def;
            Check(def.TitleKey);
            Check(def.BriefKey);
            foreach (ClueDef clue in def.Clues)
            {
                Check(clue.Key);
                Check(clue.HintKey);
            }
            foreach (InferenceDef inference in def.Inferences) { Check(inference.Key); }
            foreach (SuspectDef suspect in def.Suspects) { Check(suspect.NameKey); }
            foreach (string outcome in new[]
            {
                "case.outcome.expired", "case.outcome.forced_right", "case.outcome.forced_wrong",
                "case.outcome.thorough_right", "case.outcome.thorough_wrong"
            })
            {
                Check(outcome);
            }
            Assert.That(missing, Is.Empty, "缺词条:\n" + string.Join("\n", missing));
        }

        [Test]
        public void SaveV3_CaseProgress_RoundTrips_AndV2Migrates()
        {
            SaveData save = NewSave();
            SaveCaseState state = CaseService.Open(save, SilkCase.Def, Now(save));
            CaseService.Discover(state, SilkCase.Def, "dossier");
            CaseService.Discover(state, SilkCase.Def, "permit");

            var migrator = SaveMigrator.CreateDefault();
            SaveData restored = migrator.Load(migrator.Serialize(save));
            Assert.That(restored.Cases[SilkCase.CaseId].Clues,
                Is.EquivalentTo(new[] { "dossier", "permit" }),
                "查了一半的案子，线索一条不能丢（规格第十一节的事故模型）");

            // v2 老档（无 cases 字段）迁移后案表为空、其余不动
            string v2 = migrator.Serialize(save)
                .Replace("\"schemaVersion\": 3", "\"schemaVersion\": 2");
            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(v2);
            json.Remove("cases");
            SaveData fromV2 = migrator.Load(json.ToString());
            Assert.That(fromV2.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(fromV2.Cases, Is.Empty);
            Assert.That(fromV2.MoneyWen, Is.EqualTo(6000));
        }
    }
}
