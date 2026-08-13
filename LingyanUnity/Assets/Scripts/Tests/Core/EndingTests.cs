using System;
using Lingyan.Core.Characters;
using Lingyan.Core.Endings;
using Lingyan.Core.Localization;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class EndingTests
    {
        private static SaveData NewSave(ProtagonistId id = ProtagonistId.MingJing)
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(id);
            return SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void HighRank_BeatsReputation_ZiPao()
        {
            SaveData save = NewSave();
            save.Offices.ZhiShiId = "dalisi_qing"; // 从三品 大理寺卿
            save.Reputation.MinWang = 90;
            EndingResult result = EndingService.Evaluate(save);
            Assert.That(result.Id, Is.EqualTo(EndingId.ZiPao), "三品以上先论紫袍");
            Assert.That(result.TitleKey, Is.EqualTo("ending.title.zipao"));
            Assert.That(result.FinalOfficeId, Is.EqualTo("dalisi_qing"));
        }

        [Test]
        public void DemotedUnredeemed_TrumpsEverything()
        {
            SaveData save = NewSave();
            save.Offices.ZhiShiId = "dalisi_qing";
            save.Reputation.MinWang = 90;
            save.StoryFlags["demoted_lingnan"] = true;
            EndingResult result = EndingService.Evaluate(save);
            Assert.That(result.Id, Is.EqualTo(EndingId.LingnanRain),
                "贬谪未雪，紫袍也压不住");
        }

        [Test]
        public void WrongfulConviction_BarsXunLi()
        {
            SaveData save = NewSave();
            save.Reputation.MinWang = 80;
            save.Cases["silk_case"] = new SaveCaseState
            {
                Status = 2, OpenedStamp = "chuigong:4:5:1:6",
                DeadlineStamp = "chuigong:4:5:11:6",
                Accused = "hu_merchant", WrongfulConviction = true
            };
            EndingResult result = EndingService.Evaluate(save);
            Assert.That(result.Id, Is.Not.EqualTo(EndingId.XunLi),
                "冤案在身者不得入循吏传（同考课「公平可称」口径）");
            Assert.That(result.Id, Is.EqualTo(EndingId.BuYi), "白身冤案收场只剩布衣");
        }

        [Test]
        public void HighMinWang_Clean_XunLi()
        {
            SaveData save = NewSave();
            save.Reputation.MinWang = 72;
            EndingResult result = EndingService.Evaluate(save);
            Assert.That(result.Id, Is.EqualTo(EndingId.XunLi));
        }

        [Test]
        public void HighJiangHu_JiangHuMing()
        {
            SaveData save = NewSave();
            save.Reputation.JiangHu = 75;
            EndingResult result = EndingService.Evaluate(save);
            Assert.That(result.Id, Is.EqualTo(EndingId.JiangHuMing));
        }

        [Test]
        public void LowOffice_BoHuan_NoOffice_BuYi()
        {
            SaveData officed = NewSave();
            officed.Offices.ZhiShiId = "xian_wei"; // 从九品下
            Assert.That(EndingService.Evaluate(officed).Id, Is.EqualTo(EndingId.BoHuan));

            SaveData commoner = NewSave();
            Assert.That(EndingService.Evaluate(commoner).Id, Is.EqualTo(EndingId.BuYi));
            Assert.That(EndingService.Evaluate(commoner).FinalOfficeId, Is.Null);
        }

        [Test]
        public void Recap_CarriesCareerFacts()
        {
            SaveData save = NewSave();
            save.KaoKeGrades.Add(4);
            save.KaoKeGrades.Add(5);
            save.Marriage.Married = true;
            save.CodexUnlocked.Add("chiwei");
            EndingResult result = EndingService.Evaluate(save);

            Assert.That(result.RecapLines.Exists(l => l.Key == "ending.recap.reputation"));
            Assert.That(result.RecapLines.Exists(
                l => l.Key == "ending.recap.kaoke" && (int)l.Args[0] == 2), "考课次数如实");
            Assert.That(result.RecapLines.Exists(l => l.Key == "ending.recap.married"));
            Assert.That(result.RecapLines.Exists(
                l => l.Key == "ending.recap.codex" && (int)l.Args[0] >= 1));
        }

        [Test]
        public void AllEndingAndHeroKeys_ExistInCatalog_BothLocales()
        {
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            var missing = new System.Collections.Generic.List<string>();
            foreach (EndingId id in Enum.GetValues(typeof(EndingId)))
            {
                SaveData save = NewSave();
                Rig(save, id);
                EndingResult result = EndingService.Evaluate(save);
                Assert.That(result.Id, Is.EqualTo(id), "预置局面须命中目标结局 " + id);
                if (!catalog.Has(result.TitleKey)) { missing.Add(result.TitleKey); }
                if (!catalog.Has(result.EpilogueKey)) { missing.Add(result.EpilogueKey); }
                foreach (var line in result.RecapLines)
                {
                    if (!catalog.Has(line.Key)) { missing.Add(line.Key); }
                }
            }
            foreach (ProtagonistDef hero in ProtagonistCatalog.All)
            {
                string key = "ending.hero." + hero.Key;
                if (!catalog.Has(key)) { missing.Add(key); }
            }
            Assert.That(missing, Is.Empty, "缺词条:\n" + string.Join("\n", missing));
        }

        /// <summary>把存档摆成能命中指定结局的局面。</summary>
        private static void Rig(SaveData save, EndingId id)
        {
            switch (id)
            {
                case EndingId.ZiPao: save.Offices.ZhiShiId = "dalisi_qing"; break;
                case EndingId.QingYun: save.Offices.ZhiShiId = "yushi_zhongcheng"; break;
                case EndingId.XunLi: save.Reputation.MinWang = 80; break;
                case EndingId.JiangHuMing: save.Reputation.JiangHu = 80; break;
                case EndingId.LingnanRain: save.StoryFlags["demoted_lingnan"] = true; break;
                case EndingId.BoHuan: save.Offices.ZhiShiId = "xian_wei"; break;
                case EndingId.BuYi: break;
            }
        }
    }
}
