using System;
using Lingyan.Core.Characters;
using Lingyan.Core.Economy;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CaravanTests
    {
        private const int Ym = 68805;

        private static SaveData Trader()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.HuShang);
            return SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void Gate_TradeLineOnly_MonthlyOnce_NeedsCapital()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            SaveData scholar = SaveFactory.NewGame(draft, DateTime.UtcNow);
            Assert.That(CaravanService.RefusalKey(scholar, Ym),
                Is.EqualTo("caravan.refuse.not_trader"), "外行发不了货");

            SaveData poor = Trader();
            poor.MoneyWen = 49_999;
            Assert.That(CaravanService.RefusalKey(poor, Ym),
                Is.EqualTo("caravan.refuse.no_capital"));

            SaveData trader = Trader(); // 康悉达开局 240 贯
            Assert.That(CaravanService.RefusalKey(trader, Ym), Is.Null);
            CaravanService.Dispatch(trader, Ym);
            Assert.That(CaravanService.RefusalKey(trader, Ym),
                Is.EqualTo("caravan.refuse.already"), "月一次");
            Assert.That(CaravanService.RefusalKey(trader, Ym + 1), Is.Null);
        }

        [Test]
        public void Profit_ScalesWithJiangHu_CapsAtThirtyPercent()
        {
            Assert.That(CaravanService.ProfitFor(0), Is.EqualTo(5_000), "无名望吃底利一成");
            Assert.That(CaravanService.ProfitFor(25), Is.EqualTo(10_000), "江湖 25 得两成");
            Assert.That(CaravanService.ProfitFor(50), Is.EqualTo(15_000), "五十封顶三成");
            Assert.That(CaravanService.ProfitFor(90), Is.EqualTo(15_000), "越五十不再加");
        }

        [Test]
        public void Dispatch_BooksNetProfit()
        {
            SaveData trader = Trader(); // 江湖 25
            long before = trader.MoneyWen;
            CaravanResult result = CaravanService.Dispatch(trader, Ym);
            Assert.That(result.Dispatched, Is.True);
            Assert.That(result.ProfitWen, Is.EqualTo(10_000));
            Assert.That(trader.MoneyWen, Is.EqualTo(before + 10_000),
                "本金月内收回，账上只多净利");
        }

        [Test]
        public void Refused_MovesNoMoney()
        {
            SaveData poor = Trader();
            poor.MoneyWen = 100;
            CaravanResult result = CaravanService.Dispatch(poor, Ym);
            Assert.That(result.Dispatched, Is.False);
            Assert.That(poor.MoneyWen, Is.EqualTo(100));
        }
    }
}
