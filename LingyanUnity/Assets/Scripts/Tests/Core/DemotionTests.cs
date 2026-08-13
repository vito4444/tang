using System;
using Lingyan.Core.Characters;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class DemotionTests
    {
        private static SaveData OfficialSave(string officeId = "jiancha_yushi")
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            SaveData save = SaveFactory.NewGame(draft, DateTime.UtcNow);
            save.Offices.ZhiShiId = officeId;
            save.Offices.SanGuanId = "jishi_lang";
            return save;
        }

        [Test]
        public void Wanted30_TriggersDemotion()
        {
            SaveData save = OfficialSave();
            save.WantedLevel = 30;
            Assert.That(DemotionService.ShouldDemote(save),
                Is.EqualTo("demotion.reason.wanted"));
        }

        [Test]
        public void TwoBottomGrades_TriggerDemotion()
        {
            SaveData save = OfficialSave();
            save.KaoKeGrades.Add((int)NineGrade.ZhongXia);
            save.KaoKeGrades.Add((int)NineGrade.XiaShang);
            Assert.That(DemotionService.ShouldDemote(save),
                Is.EqualTo("demotion.reason.grades"));

            // 只有一年差评不贬
            SaveData oneBad = OfficialSave();
            oneBad.KaoKeGrades.Add((int)NineGrade.ZhongShang);
            oneBad.KaoKeGrades.Add((int)NineGrade.ZhongXia);
            Assert.That(DemotionService.ShouldDemote(oneBad), Is.Null);
        }

        [Test]
        public void Commoner_HasNoOfficeToLose()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            SaveData save = SaveFactory.NewGame(draft, DateTime.UtcNow);
            save.WantedLevel = 99;
            Assert.That(DemotionService.ShouldDemote(save), Is.Null, "白身无官可贬");
        }

        [Test]
        public void Apply_DropsTwoSteps_ClearsWanted_SetsFlag()
        {
            SaveData save = OfficialSave("jiancha_yushi"); // 文线 index 2
            save.WantedLevel = 40;
            DemotionResult result = DemotionService.Apply(save, "demotion.reason.wanted");

            Assert.That(result.Demoted, Is.True);
            Assert.That(result.NewOffice.Id, Is.EqualTo("xian_wei"), "降两阶回县尉");
            Assert.That(save.Offices.ZhiShiId, Is.EqualTo("xian_wei"));
            Assert.That(save.WantedLevel, Is.EqualTo(0), "既已问罪，通缉清零");
            Assert.That(save.StoryFlags["demoted_lingnan"], Is.True,
                "贬谪旗标入档——翻身线剧情由此读取（失败也是内容）");
        }

        [Test]
        public void Apply_ClampsAtLadderBottom()
        {
            SaveData save = OfficialSave("xian_cheng"); // index 1，降两阶保底 0
            DemotionResult result = DemotionService.Apply(save, "demotion.reason.grades");
            Assert.That(result.NewOffice.Id, Is.EqualTo("xian_wei"), "保底最低阶不出界");
        }
    }
}
