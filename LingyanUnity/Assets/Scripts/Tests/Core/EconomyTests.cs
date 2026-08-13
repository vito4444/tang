using Lingyan.Core.Economy;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class EconomyTests
    {
        [Test]
        public void Salary_HasThreeParts_AndScalesWithRank()
        {
            // 规格第八节：月俸钱 + 禄米 + 职田
            MonthlyPay junior = SalaryTable.For(RankGrade.CongLower(9)); // 县尉
            Assert.That(junior.SalaryWen, Is.GreaterThan(0));
            Assert.That(junior.RiceShi, Is.GreaterThan(0));
            Assert.That(junior.FieldRentWen, Is.GreaterThan(0));
            Assert.That(junior.TotalWen, Is.EqualTo(
                junior.SalaryWen
                + (long)System.Math.Round(junior.RiceShi * SalaryTable.RicePricePerShi)
                + junior.FieldRentWen), "总额 = 三部分之和");

            MonthlyPay senior = SalaryTable.For(RankGrade.Cong(3)); // 大理寺卿
            Assert.That(senior.TotalWen, Is.GreaterThan(junior.TotalWen * 5),
                "从三品的进项应远高于从九品");

            MonthlyPay zheng = SalaryTable.For(RankGrade.ZhengUpper(6));
            MonthlyPay cong = SalaryTable.For(RankGrade.CongUpper(6));
            Assert.That(cong.SalaryWen, Is.LessThan(zheng.SalaryWen), "从品九折");
        }

        [Test]
        public void Housing_WutouGate_IsRankNotMoney()
        {
            HousingDef wutou = HousingTable.Get("wutou");

            // 白身巨富也不许起乌头门——礼制门槛（规格第八节原文）
            Assert.That(HousingTable.CanBuy(wutou, "hut", null, 9_999_999),
                Is.EqualTo(HousingDenial.RankTooLow));

            // 九品官不行
            Assert.That(HousingTable.CanBuy(wutou, "hut", RankGrade.CongLower(9), 9_999_999),
                Is.EqualTo(HousingDenial.RankTooLow));

            // 从五品下（五品以上之末）可以——但钱要够
            Assert.That(HousingTable.CanBuy(wutou, "hut", RankGrade.CongLower(5), 1000),
                Is.EqualTo(HousingDenial.CannotAfford));
            Assert.That(HousingTable.CanBuy(wutou, "hut", RankGrade.CongLower(5), 500_000),
                Is.EqualTo(HousingDenial.None));
        }

        [Test]
        public void Housing_OnlyUpgrades()
        {
            HousingDef courtyard = HousingTable.Get("courtyard");
            Assert.That(HousingTable.CanBuy(courtyard, "hut", null, 50_000),
                Is.EqualTo(HousingDenial.None), "一进院无品可购");
            Assert.That(HousingTable.CanBuy(courtyard, "two_court", null, 50_000),
                Is.EqualTo(HousingDenial.NotAnUpgrade), "不降格置换");
        }

        [Test]
        public void HousingKeys_InCatalog()
        {
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            foreach (HousingDef housing in HousingTable.All)
            {
                Assert.That(catalog.Has(housing.NameKey), Is.True, "缺词条: " + housing.NameKey);
            }
        }
    }
}
