using Lingyan.Core.Officials;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class RankGradeTests
    {
        [Test]
        public void Ordering_FollowsTangSequence()
        {
            // 正三品 < 从三品 < 正四品上 < 正四品下 < 从四品上 < 从四品下 < 正五品上
            var chain = new[]
            {
                RankGrade.Zheng(3),
                RankGrade.Cong(3),
                RankGrade.ZhengUpper(4),
                RankGrade.ZhengLower(4),
                RankGrade.CongUpper(4),
                RankGrade.CongLower(4),
                RankGrade.ZhengUpper(5)
            };
            for (int i = 1; i < chain.Length; i++)
            {
                Assert.That(chain[i - 1].OrderValue, Is.LessThan(chain[i].OrderValue),
                    chain[i - 1].ToZh() + " 应高于 " + chain[i].ToZh());
            }
        }

        [Test]
        public void AtLeast_MeansEqualOrHigherStatus()
        {
            Assert.That(RankGrade.Zheng(3).AtLeast(RankGrade.Cong(3)), Is.True);
            Assert.That(RankGrade.Cong(3).AtLeast(RankGrade.Cong(3)), Is.True);
            Assert.That(RankGrade.ZhengUpper(4).AtLeast(RankGrade.Cong(3)), Is.False);
        }

        [Test]
        public void ToZh_Formats()
        {
            Assert.That(RankGrade.ZhengUpper(4).ToZh(), Is.EqualTo("正四品上"));
            Assert.That(RankGrade.CongLower(9).ToZh(), Is.EqualTo("从九品下"));
            Assert.That(RankGrade.Cong(3).ToZh(), Is.EqualTo("从三品"));
            Assert.That(RankGrade.Zheng(1).ToZh(), Is.EqualTo("正一品"));
        }

        [Test]
        public void ToEnShort_UsesScholarlyNotation()
        {
            Assert.That(RankGrade.ZhengUpper(4).ToEnShort(), Is.EqualTo("4a1"));
            Assert.That(RankGrade.CongLower(9).ToEnShort(), Is.EqualTo("9b2"));
            Assert.That(RankGrade.Zheng(3).ToEnShort(), Is.EqualTo("3a"));
        }

        [Test]
        public void TopThreeBands_RejectSteps()
        {
            Assert.Throws<System.ArgumentException>(() => RankGrade.ZhengUpper(3));
        }

        [Test]
        public void BandOutOfRange_Rejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RankGrade.Zheng(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RankGrade.Zheng(10));
        }
    }

    [TestFixture]
    public class RobeColorTests
    {
        [Test]
        public void SpecBoundaries_PurpleScarletGreenQing()
        {
            // 三品以上紫、五品以上绯、六七品绿、八九品青（规格第四节）
            Assert.That(RobeColors.FromGrade(RankGrade.Zheng(1)), Is.EqualTo(RobeColor.Purple));
            Assert.That(RobeColors.FromGrade(RankGrade.Cong(3)), Is.EqualTo(RobeColor.Purple));
            Assert.That(RobeColors.FromGrade(RankGrade.ZhengUpper(4)), Is.EqualTo(RobeColor.Scarlet));
            Assert.That(RobeColors.FromGrade(RankGrade.CongLower(5)), Is.EqualTo(RobeColor.Scarlet));
            Assert.That(RobeColors.FromGrade(RankGrade.ZhengUpper(6)), Is.EqualTo(RobeColor.Green));
            Assert.That(RobeColors.FromGrade(RankGrade.CongLower(7)), Is.EqualTo(RobeColor.Green));
            Assert.That(RobeColors.FromGrade(RankGrade.ZhengUpper(8)), Is.EqualTo(RobeColor.Qing));
            Assert.That(RobeColors.FromGrade(RankGrade.CongLower(9)), Is.EqualTo(RobeColor.Qing));
        }

        [Test]
        public void NoRank_IsCommonerWhite()
        {
            Assert.That(RobeColors.FromGrade(null), Is.EqualTo(RobeColor.Commoner));
        }
    }
}
