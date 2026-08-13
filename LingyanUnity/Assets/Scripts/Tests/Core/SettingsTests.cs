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

        [Test]
        public void Defaults_Volume80_FullscreenOn_NativeResolution()
        {
            var settings = new GameSettings();
            Assert.That(settings.MasterVolumePercent, Is.EqualTo(80));
            Assert.That(settings.MasterVolume01, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(settings.Fullscreen, Is.True);
            Assert.That(settings.ResolutionWidth, Is.EqualTo(0), "默认随桌面");
            Assert.That(settings.ResolutionIndex, Is.EqualTo(0));
        }

        [Test]
        public void ClampAll_VolumeSnapsToTensWithinRange()
        {
            var settings = new GameSettings { MasterVolumePercent = 87 };
            settings.ClampAll();
            Assert.That(settings.MasterVolumePercent, Is.EqualTo(90));
            settings.MasterVolumePercent = -30;
            settings.ClampAll();
            Assert.That(settings.MasterVolumePercent, Is.EqualTo(0));
            settings.MasterVolumePercent = 250;
            settings.ClampAll();
            Assert.That(settings.MasterVolumePercent, Is.EqualTo(100));
        }

        [Test]
        public void ClampAll_UnknownResolutionFallsBackToNative()
        {
            var settings = new GameSettings { ResolutionWidth = 123, ResolutionHeight = 456 };
            settings.ClampAll();
            Assert.That(settings.ResolutionWidth, Is.EqualTo(0));
            Assert.That(settings.ResolutionHeight, Is.EqualTo(0));
        }

        [Test]
        public void CycleResolution_WalksPresetsAndWraps()
        {
            var settings = new GameSettings();
            var seen = new System.Collections.Generic.List<int>();
            for (int i = 0; i < GameSettings.ResolutionPresets.Count; i++)
            {
                seen.Add(settings.ResolutionIndex);
                settings.CycleResolution();
            }
            Assert.That(settings.ResolutionIndex, Is.EqualTo(0), "走完一圈须绕回随桌面");
            Assert.That(seen, Is.Unique, "循环不得跳档或重复");
        }

        [Test]
        public void RoundTrip_DisplayAndVolumeFields()
        {
            var settings = new GameSettings
            {
                MasterVolumePercent = 40,
                Fullscreen = false,
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
            };
            GameSettings restored = GameSettings.ParseOrDefault(settings.ToJson());
            Assert.That(restored.MasterVolumePercent, Is.EqualTo(40));
            Assert.That(restored.Fullscreen, Is.False);
            Assert.That(restored.ResolutionWidth, Is.EqualTo(1920));
            Assert.That(restored.ResolutionHeight, Is.EqualTo(1080));
            Assert.That(restored.ResolutionIndex, Is.EqualTo(3));
        }

        [Test]
        public void OldSettingsFile_WithoutNewFields_GetsDefaults()
        {
            // 旧版设置文件只有语言和字号——新字段回默认，不炸不丢
            GameSettings settings = GameSettings.ParseOrDefault(
                "{\"locale\":\"en\",\"fontScalePercent\":120}");
            Assert.That(settings.Locale, Is.EqualTo(Locale.En));
            Assert.That(settings.FontScalePercent, Is.EqualTo(120));
            Assert.That(settings.MasterVolumePercent, Is.EqualTo(80));
            Assert.That(settings.Fullscreen, Is.True);
            Assert.That(settings.ResolutionIndex, Is.EqualTo(0));
        }
    }
}
