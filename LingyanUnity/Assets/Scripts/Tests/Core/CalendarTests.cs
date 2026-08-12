using Lingyan.Core.Calendar;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CalendarTests
    {
        [Test]
        public void SpecExample_ChuigongYear4()
        {
            // 规格第十节："显示成 垂拱四年 三月十七 · 巳时"
            var date = new TangDate("chuigong", 4, 3, 17, 5);
            Assert.That(date.ToZh(), Is.EqualTo("垂拱四年 三月十七 · 巳时"));
        }

        [Test]
        public void EraConversion_Chuigong4_Is688()
        {
            Assert.That(EraTable.ToGregorianYear("chuigong", 4), Is.EqualTo(688));
            Assert.That(EraTable.ToGregorianYear("tianshou", 1), Is.EqualTo(690));
            Assert.That(EraTable.ToGregorianYear("shenlong", 1), Is.EqualTo(705));
        }

        [Test]
        public void FirstYear_ReadsYuanNian()
        {
            var date = new TangDate("chuigong", 1, 1, 1, 0);
            Assert.That(date.ToZh(), Does.StartWith("垂拱元年"));
        }

        [Test]
        public void DayNames_TraditionalForms()
        {
            Assert.That(ZhNumerals.DayZh(1), Is.EqualTo("初一"));
            Assert.That(ZhNumerals.DayZh(10), Is.EqualTo("初十"));
            Assert.That(ZhNumerals.DayZh(17), Is.EqualTo("十七"));
            Assert.That(ZhNumerals.DayZh(20), Is.EqualTo("二十"));
            Assert.That(ZhNumerals.DayZh(21), Is.EqualTo("廿一"));
            Assert.That(ZhNumerals.DayZh(30), Is.EqualTo("三十"));
            Assert.That(ZhNumerals.MonthZh(1), Is.EqualTo("正月"));
            Assert.That(ZhNumerals.MonthZh(12), Is.EqualTo("十二月"));
        }

        [Test]
        public void AdvanceHours_RollsDayMonthYear()
        {
            var date = new TangDate("chuigong", 4, 12, 30, 11);
            date.AdvanceHours(1);
            Assert.That(date.EraYear, Is.EqualTo(5), "十二月三十亥时再过一时辰应跨年");
            Assert.That(date.Month, Is.EqualTo(1));
            Assert.That(date.Day, Is.EqualTo(1));
            Assert.That(date.HourIndex, Is.EqualTo(0));
        }

        [Test]
        public void AdvanceDays_RollsMonth()
        {
            var date = new TangDate("chuigong", 4, 3, 29, 5);
            date.AdvanceDays(2);
            Assert.That(date.Month, Is.EqualTo(4));
            Assert.That(date.Day, Is.EqualTo(1));
        }

        [Test]
        public void SolarTerm_StartsAtLichun_15DaysEach()
        {
            Assert.That(new TangDate("chuigong", 4, 1, 1, 0).SolarTerm.Zh, Is.EqualTo("立春"));
            Assert.That(new TangDate("chuigong", 4, 1, 15, 0).SolarTerm.Zh, Is.EqualTo("立春"));
            Assert.That(new TangDate("chuigong", 4, 1, 16, 0).SolarTerm.Zh, Is.EqualTo("雨水"));
            Assert.That(new TangDate("chuigong", 4, 12, 30, 0).SolarTerm.Zh, Is.EqualTo("大寒"));
            Assert.That(SolarTerms.All.Count, Is.EqualTo(24));
        }

        [Test]
        public void ShiChen_TwelveHours_SiIsDaytime()
        {
            Assert.That(ShiChenTable.All.Count, Is.EqualTo(12));
            Assert.That(ShiChenTable.All[5].Zh, Is.EqualTo("巳"));
            Assert.That(ShiChenTable.All[5].CurfewHour, Is.False);
            Assert.That(ShiChenTable.All[0].CurfewHour, Is.True, "子时属宵禁");
        }

        [Test]
        public void Stamp_RoundTrips()
        {
            var date = new TangDate("tianshou", 2, 7, 21, 9);
            TangDate restored = TangDate.FromStamp(date.ToStamp());
            Assert.That(restored, Is.EqualTo(date));
        }

        [Test]
        public void SetEra_StoryDriven()
        {
            var date = new TangDate("zaichu", 1, 9, 9, 4);
            date.SetEra("tianshou");
            Assert.That(date.ToZh(), Does.StartWith("天授元年"));
        }

        [Test]
        public void UnknownEra_Rejected()
        {
            Assert.Throws<System.ArgumentException>(() => new TangDate("kaiyuan", 1, 1, 1, 0));
        }

        [Test]
        public void EraWindow_CoversSpecAnchor()
        {
            // 时代锚点 660–705：窗口内关键年号必须在表
            Assert.That(EraTable.Get("xianheng"), Is.Not.Null);
            Assert.That(EraTable.Get("chuigong"), Is.Not.Null);
            Assert.That(EraTable.Get("tianshou").WuZhou, Is.True);
            Assert.That(EraTable.Get("shengli").WuZhou, Is.True);
            Assert.That(EraTable.Get("shenlong"), Is.Not.Null);
        }
    }
}
