using System;
using System.Linq;
using Lingyan.Core.Characters;
using Lingyan.Core.Localization;
using Lingyan.Core.Marriage;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class MarriageTests
    {
        private static SaveData NewSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(draft, DateTime.UtcNow);
        }

        [Test]
        public void Proposal_RequiresAllFour_Conditions()
        {
            MatchDef match = MatchCatalog.Get("silk_daughter");

            // 规格第九节：好感 AND 官品 AND 聘财 AND 无婚约
            ProposalCheck all = MarriageService.CheckProposal(
                match, affinity: 70, RankGrade.CongLower(9), 100_000, betrothed: false);
            Assert.That(all.Allowed, Is.True);

            ProposalCheck lowAffinity = MarriageService.CheckProposal(
                match, 30, RankGrade.CongLower(9), 100_000, false);
            Assert.That(lowAffinity.Reasons, Does.Contain("marriage.deny.affinity"));

            ProposalCheck noRank = MarriageService.CheckProposal(
                match, 70, null, 100_000, false);
            Assert.That(noRank.Reasons, Does.Contain("marriage.deny.rank"),
                "白身不得议官户之婚");

            ProposalCheck broke = MarriageService.CheckProposal(
                match, 70, RankGrade.CongLower(9), 1_000, false);
            Assert.That(broke.Reasons, Does.Contain("marriage.deny.betrothal"));

            ProposalCheck taken = MarriageService.CheckProposal(
                match, 70, RankGrade.CongLower(9), 100_000, betrothed: true);
            Assert.That(taken.Reasons, Does.Contain("marriage.deny.betrothed"));
        }

        [Test]
        public void FiveSurnames_GateIsSteep()
        {
            // 五姓七望：九品小官纵有钱有情也攀不上——门第即天堑
            MatchDef cui = MatchCatalog.Get("cui_lady");
            ProposalCheck check = MarriageService.CheckProposal(
                cui, affinity: 90, RankGrade.CongLower(9), 1_000_000, false);
            Assert.That(check.Allowed, Is.False);
            Assert.That(check.Reasons, Does.Contain("marriage.deny.rank"));

            ProposalCheck qualified = MarriageService.CheckProposal(
                cui, 90, RankGrade.CongUpper(5), 1_000_000, false);
            Assert.That(qualified.Allowed, Is.True, "五品以上方敢遣媒");
        }

        [Test]
        public void SixRites_AdvanceInOrder_BetrothalPaidAtNaZheng()
        {
            SaveData save = NewSave();
            save.MoneyWen = 100_000;
            MatchDef match = MatchCatalog.Get("silk_daughter");

            string[] expectedKeys =
            {
                "rite.step.nacai", "rite.step.wenming", "rite.step.naji",
                "rite.step.nazheng", "rite.step.qingqi", "rite.step.qinying"
            };
            for (int i = 0; i < 6; i++)
            {
                Assert.That(save.Marriage.Married, Is.False);
                string key = MarriageService.AdvanceRite(save, match.Id, match);
                Assert.That(key, Is.EqualTo(expectedKeys[i]), "六礼次序不可乱");
                if (expectedKeys[i] == "rite.step.nazheng")
                {
                    Assert.That(save.MoneyWen, Is.EqualTo(100_000 - match.BetrothalWen),
                        "纳征即交聘财");
                }
            }
            Assert.That(save.Marriage.Married, Is.True, "亲迎毕，礼成");
            Assert.That(save.Marriage.MatchId, Is.EqualTo("silk_daughter"));
        }

        [Test]
        public void MarriageState_SurvivesSaveRoundTrip()
        {
            SaveData save = NewSave();
            save.MoneyWen = 100_000;
            MatchDef match = MatchCatalog.Get("silk_daughter");
            MarriageService.AdvanceRite(save, match.Id, match);
            MarriageService.AdvanceRite(save, match.Id, match);

            var migrator = SaveMigrator.CreateDefault();
            SaveData restored = migrator.Load(migrator.Serialize(save));
            Assert.That(restored.Marriage.MatchId, Is.EqualTo("silk_daughter"));
            Assert.That(restored.Marriage.RiteStep, Is.EqualTo(2), "行到问名，进度不丢");
            Assert.That(restored.Marriage.Married, Is.False);
        }

        [Test]
        public void MatchKeys_AllInCatalog()
        {
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            var missing = new System.Collections.Generic.List<string>();
            foreach (MatchDef match in MatchCatalog.All)
            {
                if (!catalog.Has(match.NameKey)) { missing.Add(match.NameKey); }
                if (!catalog.Has(match.ClanKey)) { missing.Add(match.ClanKey); }
            }
            foreach (var (_, key) in RiteSteps.All)
            {
                if (!catalog.Has(key)) { missing.Add(key); }
            }
            foreach (string deny in new[]
            {
                "marriage.deny.betrothed", "marriage.deny.affinity",
                "marriage.deny.rank", "marriage.deny.betrothal"
            })
            {
                if (!catalog.Has(deny)) { missing.Add(deny); }
            }
            Assert.That(missing, Is.Empty, "缺词条:\n" + string.Join("\n", missing));
        }
    }
}
