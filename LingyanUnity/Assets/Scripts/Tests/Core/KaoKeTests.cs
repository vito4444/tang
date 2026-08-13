using Lingyan.Core.Officials;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class KaoKeTests
    {
        // 《唐六典》九等映射逐行锁定
        [TestCase(true, 4, NineGrade.ShangShang)]
        [TestCase(true, 3, NineGrade.ShangZhong)]
        [TestCase(false, 4, NineGrade.ShangZhong)]
        [TestCase(true, 2, NineGrade.ShangXia)]
        [TestCase(false, 3, NineGrade.ShangXia)]
        [TestCase(true, 1, NineGrade.ZhongShang)]
        [TestCase(false, 2, NineGrade.ZhongShang)]
        [TestCase(true, 0, NineGrade.ZhongZhong)]
        [TestCase(false, 1, NineGrade.ZhongZhong)]
        [TestCase(false, 0, NineGrade.ZhongXia)]
        public void MapGrade_MatchesTangLiuDian(bool zui, int shan, NineGrade expected)
        {
            Assert.That(KaoKeService.MapGrade(zui, shan), Is.EqualTo(expected));
        }

        [Test]
        public void Misconduct_OverridesEverything()
        {
            var input = new KaoKeInput
            {
                MeritPoints = 100,
                CompletionRatio = 1.0,
                GuanSheng = 90,
                MinWang = 90,
                Misconduct = Misconduct.TanZhuoYouZhuang
            };
            Assert.That(KaoKeService.Evaluate(input).Grade, Is.EqualTo(NineGrade.XiaXia),
                "贪浊有状，纵有功绩亦为下下");

            input.Misconduct = Misconduct.BeiGongXiangSi;
            Assert.That(KaoKeService.Evaluate(input).Grade, Is.EqualTo(NineGrade.XiaZhong));

            input.Misconduct = Misconduct.AiZengRenQing;
            Assert.That(KaoKeService.Evaluate(input).Grade, Is.EqualTo(NineGrade.XiaShang));
        }

        [Test]
        public void Evaluate_UsesReputationAndCompletion()
        {
            // 官声民望俱高、完成度高、功绩达"最"→ 上上
            var strong = new KaoKeInput
            {
                MeritPoints = KaoKeService.ZuiMeritThreshold,
                CompletionRatio = 0.9,
                GuanSheng = 70,
                MinWang = 75
            };
            KaoKeResult result = KaoKeService.Evaluate(strong);
            Assert.That(result.HasZui, Is.True);
            Assert.That(result.ShanCount, Is.EqualTo(4));
            Assert.That(result.Grade, Is.EqualTo(NineGrade.ShangShang));

            // 受贿事发丢"清慎明著"，冤案丢"公平可称"
            var tainted = new KaoKeInput
            {
                MeritPoints = KaoKeService.ZuiMeritThreshold,
                CompletionRatio = 0.9,
                GuanSheng = 70,
                MinWang = 75,
                BriberyExposed = true,
                WrongfulConviction = true
            };
            KaoKeResult taintedResult = KaoKeService.Evaluate(tainted);
            Assert.That(taintedResult.ShanCount, Is.EqualTo(2));
            Assert.That(taintedResult.Grade, Is.EqualTo(NineGrade.ShangXia));
        }

        [Test]
        public void GradeNames_Bilingual()
        {
            Assert.That(KaoKeService.GradeZh(NineGrade.ShangShang), Is.EqualTo("上上"));
            Assert.That(KaoKeService.GradeZh(NineGrade.XiaXia), Is.EqualTo("下下"));
            Assert.That(KaoKeService.GradeEn(NineGrade.ZhongShang), Is.EqualTo("Middle-upper"));
        }
    }

    [TestFixture]
    public class PromotionTests
    {
        private static OfficeDef XianWei { get { return OfficialLadders.Get("xian_wei"); } }

        [Test]
        public void FourGoodYears_Promote()
        {
            var grades = new[]
            {
                NineGrade.ZhongZhong, NineGrade.ZhongShang, NineGrade.ZhongZhong, NineGrade.ZhongZhong
            };
            PromotionDecision decision = PromotionService.Evaluate(grades, XianWei);
            Assert.That(decision.Eligible, Is.True);
            Assert.That(decision.ReasonKey, Is.EqualTo("promotion.reason.term_complete"));
            Assert.That(decision.NextOffice.Id, Is.EqualTo("xian_cheng"));
        }

        [Test]
        public void TermIncomplete_Denied()
        {
            var grades = new[] { NineGrade.ZhongShang, NineGrade.ZhongShang };
            PromotionDecision decision = PromotionService.Evaluate(grades, XianWei);
            Assert.That(decision.Eligible, Is.False);
            Assert.That(decision.ReasonKey, Is.EqualTo("promotion.reason.term_incomplete"));
        }

        [Test]
        public void OneBadYear_Blocks()
        {
            var grades = new[]
            {
                NineGrade.ZhongShang, NineGrade.ZhongZhong, NineGrade.ZhongXia, NineGrade.ZhongShang
            };
            PromotionDecision decision = PromotionService.Evaluate(grades, XianWei);
            Assert.That(decision.Eligible, Is.False);
            Assert.That(decision.ReasonKey, Is.EqualTo("promotion.reason.grades_low"));
        }

        [Test]
        public void AllMediocre_NoDistinction_Blocks()
        {
            var grades = new[]
            {
                NineGrade.ZhongZhong, NineGrade.ZhongZhong, NineGrade.ZhongZhong, NineGrade.ZhongZhong
            };
            PromotionDecision decision = PromotionService.Evaluate(grades, XianWei);
            Assert.That(decision.Eligible, Is.False);
            Assert.That(decision.ReasonKey, Is.EqualTo("promotion.reason.no_distinction"));
        }

        [Test]
        public void ShangShang_SpecialEdict_SkipsTerm()
        {
            var grades = new[] { NineGrade.ShangShang };
            PromotionDecision decision = PromotionService.Evaluate(grades, XianWei);
            Assert.That(decision.Eligible, Is.True);
            Assert.That(decision.ReasonKey, Is.EqualTo("promotion.reason.special_edict"));
        }

        [Test]
        public void LadderEnd_NoPromotion()
        {
            var grades = new[]
            {
                NineGrade.ShangShang, NineGrade.ShangShang, NineGrade.ShangShang, NineGrade.ShangShang
            };
            PromotionDecision decision = PromotionService.Evaluate(
                grades, OfficialLadders.Get("tong_pingzhangshi"));
            Assert.That(decision.Eligible, Is.False);
            Assert.That(decision.ReasonKey, Is.EqualTo("promotion.reason.ladder_end"));
        }
    }
}
