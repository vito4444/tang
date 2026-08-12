using Lingyan.Core.Localization;
using Lingyan.Core.Settings;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class SettingsTests
    {
        [Test]
        public void Defaults_ChineseAt22px()
        {
            var settings = new GameSettings();
            Assert.That(settings.Locale, Is.EqualTo(Locale.ZhHans));
            Assert.That(settings.EffectiveBaseFontPx, Is.EqualTo(22));
        }

        [Test]
        public void BaseFont_NeverBelow22px()
        {
            // 规格第十一节：字号基准别小于 22px
            Assert.That(GameSettings.BaseFontPx, Is.GreaterThanOrEqualTo(22));
            var settings = new GameSettings { FontScalePercent = 100 };
            Assert.That(settings.EffectiveBaseFontPx, Is.GreaterThanOrEqualTo(22));
            settings.FontScalePercent = GameSettings.MinScalePercent;
            settings.ClampScale();
            Assert.That(settings.EffectiveBaseFontPx, Is.GreaterThanOrEqualTo(22));
        }

        [Test]
        public void Scale_150Percent_Is33px()
        {
            var settings = new GameSettings { FontScalePercent = 150 };
            Assert.That(settings.EffectiveBaseFontPx, Is.EqualTo(33));
        }

        [Test]
        public void ClampScale_EnforcesBounds()
        {
            var settings = new GameSettings { FontScalePercent = 20 };
            settings.ClampScale();
            Assert.That(settings.FontScalePercent, Is.EqualTo(100));
            settings.FontScalePercent = 999;
            settings.ClampScale();
            Assert.That(settings.FontScalePercent, Is.EqualTo(160));
        }

        [Test]
        public void GarbageSettingsFile_FallsBackToDefaults()
        {
            GameSettings settings = GameSettings.ParseOrDefault("{{{ 不是 json");
            Assert.That(settings.Locale, Is.EqualTo(Locale.ZhHans));
            Assert.That(settings.FontScalePercent, Is.EqualTo(100));
        }

        [Test]
        public void RoundTrip()
        {
            var settings = new GameSettings { FontScalePercent = 130 };
            settings.Locale = Locale.En;
            GameSettings restored = GameSettings.ParseOrDefault(settings.ToJson());
            Assert.That(restored.Locale, Is.EqualTo(Locale.En));
            Assert.That(restored.FontScalePercent, Is.EqualTo(130));
        }
    }
}
