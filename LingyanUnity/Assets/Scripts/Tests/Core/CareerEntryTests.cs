using System;
using Lingyan.Core.Characters;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CareerEntryTests
    {
        private static SaveData NewSave(ProtagonistId id)
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(id);
            if (id == ProtagonistId.BaiShen)
            {
                draft.EntryPath = EntryPath.KejuMingJing;
                for (int i = 0; i < 12; i++) // 白身 12 自由点须配完才能开档
                {
                    foreach (var attr in AttributeSet.AllIds)
                    {
                        if (CharacterCreationRules.TryIncrease(draft, attr)
                            == AllocationError.None)
                        {
                            break;
                        }
                    }
                }
            }
            return SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void MingJing_NeedsClosedCase_ThenXianWei()
        {
            SaveData save = NewSave(ProtagonistId.MingJing);
            EntryOffer offer = CareerEntryService.Evaluate(save);
            Assert.That(offer.Available, Is.False, "未结案不得铨选");
            Assert.That(offer.GateKey, Is.EqualTo("career.entry.gate.mingjing"));
            Assert.That(CareerEntryService.Apply(save, offer), Is.False, "门槛未达拒绝落定");
            Assert.That(save.Offices.ZhiShiId, Is.Null);

            save.Counters["cases_closed"] = 1;
            offer = CareerEntryService.Evaluate(save);
            Assert.That(offer.Available, Is.True);
            Assert.That(CareerEntryService.Apply(save, offer), Is.True);
            Assert.That(save.Offices.ZhiShiId, Is.EqualTo("xian_wei"));
            Assert.That(save.Offices.SanGuanId, Is.Not.Null, "文线带起家文散官");
        }

        [Test]
        public void ShuZu_TwoSparWins_ThenDuiZheng()
        {
            SaveData save = NewSave(ProtagonistId.ShuZu);
            Assert.That(CareerEntryService.Evaluate(save).Available, Is.False);

            save.Counters["spar_wins"] = 1;
            Assert.That(CareerEntryService.Evaluate(save).Available, Is.False, "一胜不算武艺可称");

            save.Counters["spar_wins"] = 2;
            EntryOffer offer = CareerEntryService.Evaluate(save);
            Assert.That(offer.Available, Is.True);
            Assert.That(offer.ZhiShiOfficeId, Is.EqualTo("dui_zheng"));
            Assert.That(CareerEntryService.Apply(save, offer), Is.True);
            Assert.That(save.Offices.ZhiShiId, Is.EqualTo("dui_zheng"));
            Assert.That(save.Offices.SanGuanId, Is.Not.Null, "军线带武散官");
        }

        [Test]
        public void NvGuan_WisdomAndStanding_ThenPalaceRank_NoSanGuan()
        {
            SaveData save = NewSave(ProtagonistId.NvGuan); // 智 13、官声 20
            Assert.That(CareerEntryService.Evaluate(save).Available, Is.False,
                "官声 20 < 25，内廷未有诏下");

            save.Reputation.GuanSheng = 25;
            EntryOffer offer = CareerEntryService.Evaluate(save);
            Assert.That(offer.Available, Is.True);
            Assert.That(CareerEntryService.Apply(save, offer), Is.True);
            Assert.That(save.Offices.ZhiShiId, Is.EqualTo("zhang_ji"), "掌记起家（尚宫局）");
            Assert.That(save.Offices.SanGuanId, Is.Null, "宫官品阶自成体系，不带散官");

            // 授官后的存档必须过校验器（宫官序列真的入了官表）
            var migrator = SaveMigrator.CreateDefault();
            Assert.DoesNotThrow(() => migrator.Load(migrator.Serialize(save)));
        }

        [Test]
        public void HuShang_MarketRegisterBars_MoneyBuysTitleOnly()
        {
            SaveData save = NewSave(ProtagonistId.HuShang); // 钱 240 贯、民望 15
            Assert.That(CareerEntryService.Evaluate(save).Available, Is.False,
                "民望不到三十，纳资也无门");

            save.Reputation.MinWang = 30;
            EntryOffer offer = CareerEntryService.Evaluate(save);
            Assert.That(offer.Available, Is.True);
            long before = save.MoneyWen;
            Assert.That(CareerEntryService.Apply(save, offer), Is.True);
            Assert.That(save.MoneyWen, Is.EqualTo(before - 200_000), "纳资二百贯真金白银");
            Assert.That(save.Offices.SanGuanId, Is.EqualTo("jiangshi_lang"), "得文散虚衔");
            Assert.That(save.Offices.ZhiShiId, Is.Null,
                "市籍之限：职事无门——身份即命运，钱买不动");
        }

        [Test]
        public void BaiShen_ThreePaths_ThreeGates()
        {
            // 明经：智 10 及第
            SaveData keju = NewSave(ProtagonistId.BaiShen);
            keju.Attributes.Wisdom = 9;
            Assert.That(CareerEntryService.Evaluate(keju).Available, Is.False, "智 9 落第");
            keju.Attributes.Wisdom = 10;
            Assert.That(CareerEntryService.Evaluate(keju).Available, Is.True, "明经智 10 及第");

            // 进士：智 12 且民望 15
            keju.EntryPath = "KejuJinShi";
            keju.Attributes.Wisdom = 12;
            keju.Reputation.MinWang = 10;
            Assert.That(CareerEntryService.Evaluate(keju).Available, Is.False,
                "行卷无名（民望不足）进士难第");
            keju.Reputation.MinWang = 15;
            EntryOffer jinshi = CareerEntryService.Evaluate(keju);
            Assert.That(jinshi.Available, Is.True);
            Assert.That(jinshi.ZhiShiOfficeId, Is.EqualTo("xian_wei"));

            // 投军：切磋两胜授队正
            SaveData toujun = NewSave(ProtagonistId.BaiShen);
            toujun.EntryPath = "TouJun";
            toujun.Counters["spar_wins"] = 2;
            EntryOffer mu = CareerEntryService.Evaluate(toujun);
            Assert.That(mu.Available, Is.True);
            Assert.That(mu.ZhiShiOfficeId, Is.EqualTo("dui_zheng"));
        }

        [Test]
        public void PalaceLadder_ClimbsShangGongJu_GradesAscend()
        {
            // 掌记(正八) → 典记(正七) → 司记(正六) → 尚宫(正五)，宫官不分上下阶
            OfficeDef office = OfficialLadders.Get("zhang_ji");
            var seen = new System.Collections.Generic.List<string>();
            while (office != null)
            {
                seen.Add(office.Id);
                OfficeDef next = OfficialLadders.NextOf(office);
                if (next != null)
                {
                    Assert.That(next.Grade.Value.OrderValue,
                        Is.LessThan(office.Grade.Value.OrderValue), "迁转品阶必须走高");
                }
                office = next;
            }
            Assert.That(seen, Is.EqualTo(
                new[] { "zhang_ji", "dian_ji", "si_ji", "shang_gong" }));
        }

        [Test]
        public void EntryKeys_AllInCatalog()
        {
            var catalog = Localization.LocalizationCatalog.Parse(TestData.StringsJson());
            var missing = new System.Collections.Generic.List<string>();
            foreach (ProtagonistDef hero in ProtagonistCatalog.All)
            {
                foreach (string path in new[] { "KejuMingJing", "KejuJinShi", "TouJun" })
                {
                    SaveData save = NewSave(hero.Id);
                    if (hero.Id == ProtagonistId.BaiShen) { save.EntryPath = path; }
                    EntryOffer offer = CareerEntryService.Evaluate(save);
                    foreach (string key in new[]
                        { offer.ActionKey, offer.GateKey, offer.NoticeKey })
                    {
                        if (!catalog.Has(key)) { missing.Add(hero.Key + ": " + key); }
                    }
                }
            }
            Assert.That(missing, Is.Empty, "缺词条:\n" + string.Join("\n", missing));
        }
    }
}
