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

        [Test]
        public void Apply_PalaceLine_StaysOnPalaceLadder()
        {
            SaveData save = OfficialSave("si_ji"); // 宫官线 index 2
            save.Offices.SanGuanId = null;        // 宫官不带散官
            DemotionResult result = DemotionService.Apply(save, "demotion.reason.wanted");
            Assert.That(result.NewOffice.Id, Is.EqualTo("zhang_ji"),
                "宫官贬宫官序（司记降两阶回掌记），不得跌进文官序");
        }

        [Test]
        public void Redeem_MidUpperGrade_WashesFlag()
        {
            SaveData save = OfficialSave();
            DemotionService.Apply(save, "demotion.reason.wanted");
            Assert.That(save.StoryFlags["demoted_lingnan"], Is.True, "前置：已贬");

            Assert.That(DemotionService.TryRedeem(save, NineGrade.ZhongZhong), Is.False,
                "中中不够，量移须中上及以上");
            Assert.That(save.StoryFlags["demoted_lingnan"], Is.True);

            Assert.That(DemotionService.TryRedeem(save, NineGrade.ZhongShang), Is.True);
            Assert.That(save.StoryFlags["demoted_lingnan"], Is.False, "贬籍洗雪");

            Assert.That(DemotionService.TryRedeem(save, NineGrade.ShangShang), Is.False,
                "未贬之身无籍可洗");
        }

        [Test]
        public void Redeem_UnlocksNonLingnanEnding()
        {
            SaveData save = OfficialSave("xian_wei");
            DemotionService.Apply(save, "demotion.reason.wanted");
            Assert.That(Lingyan.Core.Endings.EndingService.Evaluate(save).Id,
                Is.EqualTo(Lingyan.Core.Endings.EndingId.LingnanRain), "贬籍未雪锁岭南档");

            DemotionService.TryRedeem(save, NineGrade.ShangXia);
            Assert.That(Lingyan.Core.Endings.EndingService.Evaluate(save).Id,
                Is.Not.EqualTo(Lingyan.Core.Endings.EndingId.LingnanRain),
                "量移之后，结局照常论身份与名声");
        }
    }
}
