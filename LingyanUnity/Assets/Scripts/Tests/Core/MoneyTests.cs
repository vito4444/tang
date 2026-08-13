using Lingyan.Core.Economy;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class MoneyTests
    {
        [Test]
        public void SpecExample_3420Wen_Displays3Guan420Wen()
        {
            // 规格第八节："显示成 3 贯 420 文 而不是 3420"
            Assert.That(new Money(3420).ToZh(), Is.EqualTo("3 贯 420 文"));
            Assert.That(new Money(3420).ToEn(), Is.EqualTo("3 guan 420 wen"));
        }

        [TestCase(0L, "0 文")]
        [TestCase(999L, "999 文")]
        [TestCase(1000L, "1 贯")]
        [TestCase(240000L, "240 贯")]
        [TestCase(1001L, "1 贯 1 文")]
        public void Formatting_Zh(long wen, string expected)
        {
            Assert.That(new Money(wen).ToZh(), Is.EqualTo(expected));
        }

        [Test]
        public void GuanIs1000Wen()
        {
            Assert.That(Money.FromGuanWen(3, 420).Wen, Is.EqualTo(3420));
            Assert.That(new Money(3420).GuanPart, Is.EqualTo(3));
            Assert.That(new Money(3420).WenPart, Is.EqualTo(420));
        }

        [Test]
        public void NegativeMoney_Rejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new Money(-1));
        }

        [Test]
        public void Arithmetic_And_Overdraft()
        {
            Money purse = new Money(1500);
            purse += new Money(700);
            Assert.That(purse.Wen, Is.EqualTo(2200));
            purse -= new Money(200);
            Assert.That(purse.Wen, Is.EqualTo(2000));
            Assert.That(purse.CanAfford(new Money(2001)), Is.False);
            Assert.Throws<System.InvalidOperationException>(() => { var _ = purse - new Money(2001); });
        }
    }
}
