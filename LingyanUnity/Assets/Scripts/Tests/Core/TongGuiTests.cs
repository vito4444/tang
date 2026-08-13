using System;
using Lingyan.Core.Cases;
using Lingyan.Core.Characters;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class TongGuiTests
    {
        private const int Ym = 68805; // 垂拱四年五月的年月键样式（年*100+月）

        private static SaveData NewSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void MonthlyLimit_SecondSubmitRefused()
        {
            SaveData save = NewSave();
            TongGuiResult first = TongGuiService.Submit(save, GuiSlot.ZhaoJian, Ym);
            Assert.That(first.Accepted, Is.True);

            TongGuiResult second = TongGuiService.Submit(save, GuiSlot.TongXuan, Ym);
            Assert.That(second.Accepted, Is.False, "有司须校理，月投一书");
            Assert.That(second.TextKey, Is.EqualTo("tonggui.result.monthly_limit"));

            TongGuiResult nextMonth = TongGuiService.Submit(save, GuiSlot.TongXuan, Ym + 1);
            Assert.That(nextMonth.Accepted, Is.True, "次月再投不限");
        }

        [Test]
        public void YanEn_CommonerOnly()
        {
            SaveData commoner = NewSave();
            TongGuiResult ok = TongGuiService.Submit(commoner, GuiSlot.YanEn, Ym);
            Assert.That(ok.Accepted, Is.True);
            Assert.That(ok.GuanShengDelta, Is.EqualTo(+3), "自荐上达，官声起步");

            SaveData officed = NewSave();
            officed.Offices.ZhiShiId = "xian_wei";
            TongGuiResult refused = TongGuiService.Submit(officed, GuiSlot.YanEn, Ym);
            Assert.That(refused.Accepted, Is.False, "有官守者不由延恩进");
            Assert.That(refused.TextKey, Is.EqualTo("tonggui.result.yanen_officed"));
        }

        [Test]
        public void ZhaoJian_GainsStanding_CostsStreetName()
        {
            SaveData save = NewSave();
            TongGuiResult result = TongGuiService.Submit(save, GuiSlot.ZhaoJian, Ym);
            Assert.That(result.GuanShengDelta, Is.EqualTo(+2));
            Assert.That(result.MinWangDelta, Is.EqualTo(+1));
            Assert.That(result.JiangHuDelta, Is.EqualTo(-1), "告密之风盛，坊间侧目");
        }

        [Test]
        public void ShenYuan_NeedsWrongfulCase_AndBacking()
        {
            SaveData clean = NewSave();
            clean.Reputation.MinWang = 80;
            TongGuiResult noCase = TongGuiService.Submit(clean, GuiSlot.ShenYuan, Ym);
            Assert.That(noCase.Accepted, Is.False);
            Assert.That(noCase.TextKey, Is.EqualTo("tonggui.result.shenyuan_no_case"),
                "档上无冤滞，申冤匦不受");

            SaveData lowBacking = WrongfulSave();
            lowBacking.Reputation.MinWang = 49;
            TongGuiResult refused = TongGuiService.Submit(lowBacking, GuiSlot.ShenYuan, Ym);
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.TextKey, Is.EqualTo("tonggui.result.shenyuan_no_backing"),
                "无人证愿随投（民望须至 50）");
            Assert.That(lowBacking.Cases[SilkCase.CaseId].WrongfulConviction, Is.True,
                "被驳不动案卷");
        }

        [Test]
        public void ShenYuan_RedressesCase_UnblocksXunLi()
        {
            SaveData save = WrongfulSave();
            save.Reputation.MinWang = 72;

            // 冤案在身：循吏传被封锁
            Assert.That(Endings.EndingService.Evaluate(save).Id,
                Is.Not.EqualTo(Endings.EndingId.XunLi));

            TongGuiResult result = TongGuiService.Submit(save, GuiSlot.ShenYuan, Ym);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RedressedCaseId, Is.EqualTo(SilkCase.CaseId));
            Assert.That(result.GuanShengDelta, Is.EqualTo(-3), "自承其失，官声折损");
            Assert.That(result.MinWangDelta, Is.EqualTo(+6), "坊间称快");
            Assert.That(save.Cases[SilkCase.CaseId].WrongfulConviction, Is.False, "冤案昭雪");

            // 昭雪之后：民望够高即可入循吏传——认错也是仕途的一部分
            Assert.That(Endings.EndingService.Evaluate(save).Id,
                Is.EqualTo(Endings.EndingId.XunLi));
        }

        [Test]
        public void TongXuan_StreetFame_OfficialFrown()
        {
            SaveData save = NewSave();
            TongGuiResult result = TongGuiService.Submit(save, GuiSlot.TongXuan, Ym);
            Assert.That(result.JiangHuDelta, Is.EqualTo(+2));
            Assert.That(result.GuanShengDelta, Is.EqualTo(-1));
        }

        /// <summary>丝帛案屈打成冤的档。</summary>
        private static SaveData WrongfulSave()
        {
            SaveData save = NewSave();
            var now = new Calendar.TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            SaveCaseState state = CaseService.Open(save, SilkCase.Def, now);
            CaseService.Accuse(state, SilkCase.Def, "idler",
                AccuseMethod.Forced, witnessTalked: false);
            Assert.That(state.WrongfulConviction, Is.True, "前置：已成冤案");
            return save;
        }
    }
}
