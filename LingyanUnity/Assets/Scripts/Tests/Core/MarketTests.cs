using System;
using Lingyan.Core.Characters;
using Lingyan.Core.Economy;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class MarketTests
    {
        private static SaveData Save(int hourIndex)
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            SaveData save = SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
            save.Date.HourIndex = hourIndex;
            return save;
        }

        [Test]
        public void MarketHours_NoonToSunset_PerTangLiudian()
        {
            // 《唐六典》：日中而市，日入前散。游戏粒度：午未申开，酉即闭。
            Assert.That(MarketService.IsOpenAt(5), Is.False, "巳时未击鼓，市不开");
            Assert.That(MarketService.IsOpenAt(6), Is.True, "午时（日中）开市");
            Assert.That(MarketService.IsOpenAt(7), Is.True);
            Assert.That(MarketService.IsOpenAt(8), Is.True);
            Assert.That(MarketService.IsOpenAt(9), Is.False, "酉时（日入）已散");
            Assert.That(MarketService.IsOpenAt(0), Is.False, "宵禁时段更不可能开");
        }

        [Test]
        public void Buy_DeductsMoney_AddsToInventory()
        {
            SaveData save = Save(hourIndex: 6);
            long before = save.MoneyWen;

            MarketError error = MarketService.Buy(save, "gift_jiu"); // 120 文
            Assert.That(error, Is.EqualTo(MarketError.None));
            Assert.That(save.MoneyWen, Is.EqualTo(before - 120), "钱按价扣");
            Assert.That(MarketService.CountOf(save, "gift_jiu"), Is.EqualTo(1));

            MarketService.Buy(save, "gift_jiu");
            Assert.That(MarketService.CountOf(save, "gift_jiu"), Is.EqualTo(2), "重复买入累计件数");
        }

        [Test]
        public void Buy_WhenClosed_NothingChanges()
        {
            SaveData save = Save(hourIndex: 9); // 酉时已散市
            long before = save.MoneyWen;
            MarketError error = MarketService.Buy(save, "gift_jiu");
            Assert.That(error, Is.EqualTo(MarketError.Closed));
            Assert.That(save.MoneyWen, Is.EqualTo(before), "失败不动钱");
            Assert.That(save.Inventory, Is.Empty, "失败不动货");
        }

        [Test]
        public void Buy_NotEnoughMoney_NothingChanges()
        {
            SaveData save = Save(hourIndex: 6);
            save.MoneyWen = 100; // 玉佩 2500 文买不起
            MarketError error = MarketService.Buy(save, "gift_yupei");
            Assert.That(error, Is.EqualTo(MarketError.NotEnoughMoney));
            Assert.That(save.MoneyWen, Is.EqualTo(100));
            Assert.That(save.Inventory, Is.Empty);
        }

        [Test]
        public void Buy_UnknownGoods_Refused()
        {
            SaveData save = Save(hourIndex: 6);
            Assert.That(MarketService.Buy(save, "gift_nothing"),
                Is.EqualTo(MarketError.UnknownGoods));
        }

        [Test]
        public void TakeOne_ConsumesAndRemovesZeroEntries()
        {
            SaveData save = Save(hourIndex: 6);
            MarketService.Buy(save, "gift_hubing");
            MarketService.Buy(save, "gift_hubing");

            Assert.That(MarketService.TakeOne(save, "gift_hubing"), Is.EqualTo(MarketError.None));
            Assert.That(MarketService.CountOf(save, "gift_hubing"), Is.EqualTo(1));

            Assert.That(MarketService.TakeOne(save, "gift_hubing"), Is.EqualTo(MarketError.None));
            Assert.That(save.Inventory.ContainsKey("gift_hubing"), Is.False,
                "件数归零必须移除键，存档不留 0 条目（校验器会拒 0 件）");

            Assert.That(MarketService.TakeOne(save, "gift_hubing"),
                Is.EqualTo(MarketError.NotInInventory), "囊中已无，取物失败");
        }

        [Test]
        public void Goods_AreWholeGiftCatalog()
        {
            Assert.That(MarketService.Goods.Count, Is.EqualTo(6), "现阶段市售 = 全礼单");
        }
    }
}
