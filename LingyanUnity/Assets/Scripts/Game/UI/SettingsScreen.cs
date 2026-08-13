using Lingyan.Core.Localization;
using Lingyan.Core.Settings;
using Lingyan.Game.Services;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>设置：语言、字号（基准 22px 只增不减）、无障碍说明。</summary>
    public static class SettingsScreen
    {
        public static void Build(GameController c, RectTransform root)
        {
            UiKit.InkBackground(root);
            GameSettings settings = c.Settings.Current;

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.9f, 700, 70),
                "T", c.L10n.Tr("settings.title"), 1.8f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.856f, 480, 0.24f);

            RectTransform panel = UiKit.Rect(root, "Panel",
                new Vector2(0.24f, 0.2f), new Vector2(0.76f, 0.8f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "PanelBg");

            // 语言
            UiKit.Text(UiKit.At(panel, "LangLabel", 0.20f, 0.885f, 300, 50),
                "T", c.L10n.Tr("settings.language"), 1.15f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);

            bool isZh = c.L10n.Locale == Locale.ZhHans;
            UiKit.TextButton(UiKit.At(panel, "LangZh", 0.52f, 0.885f, 300, 50),
                "Btn", (isZh ? "\u25c9 " : "\u25cb ") + c.L10n.Tr("settings.language.zh"),
                () => SetLocale(c, Locale.ZhHans), 1.05f);
            UiKit.TextButton(UiKit.At(panel, "LangEn", 0.82f, 0.885f, 300, 50),
                "Btn", (!isZh ? "\u25c9 " : "\u25cb ") + c.L10n.Tr("settings.language.en"),
                () => SetLocale(c, Locale.En), 1.05f);

            // 字号
            UiKit.Text(UiKit.At(panel, "FontLabel", 0.20f, 0.765f, 300, 50),
                "T", c.L10n.Tr("settings.font_scale"), 1.15f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);

            UiKit.TextButton(UiKit.At(panel, "FontMinus", 0.44f, 0.765f, 70, 50),
                "Btn", "\u2212", () => Bump(c, -10), 1.2f,
                settings.FontScalePercent > GameSettings.MinScalePercent);

            UiKit.Text(UiKit.At(panel, "FontValue", 0.63f, 0.765f, 360, 50),
                "T", c.L10n.TrF("settings.font_scale_value",
                    settings.FontScalePercent, settings.EffectiveBaseFontPx),
                1.05f, InkPalette.PaperText, TextAlignmentOptions.Center);

            UiKit.TextButton(UiKit.At(panel, "FontPlus", 0.84f, 0.765f, 70, 50),
                "Btn", "+", () => Bump(c, +10), 1.2f,
                settings.FontScalePercent < GameSettings.MaxScalePercent);

            // 分辨率（循环档位；(0,0) = 随桌面）
            UiKit.Text(UiKit.At(panel, "ResLabel", 0.20f, 0.645f, 300, 50),
                "T", c.L10n.Tr("settings.resolution"), 1.15f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            // 不加装饰符：⟳（U+27F3）不在 LXGWWenKai 字库，实机渲成豆腐块（第十二轮实测）；
            // 可点性由悬停括弧表意，与全局按钮一致
            UiKit.TextButton(UiKit.At(panel, "ResCycle", 0.67f, 0.645f, 460, 50),
                "Btn", ResolutionLabel(c, settings),
                () => CycleResolution(c), 1.05f);

            // 显示模式
            UiKit.Text(UiKit.At(panel, "ModeLabel", 0.20f, 0.525f, 300, 50),
                "T", c.L10n.Tr("settings.display_mode"), 1.15f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.TextButton(UiKit.At(panel, "ModeFull", 0.52f, 0.525f, 300, 50),
                "Btn", (settings.Fullscreen ? "\u25c9 " : "\u25cb ")
                    + c.L10n.Tr("settings.display_mode.fullscreen"),
                () => SetFullscreen(c, true), 1.05f);
            UiKit.TextButton(UiKit.At(panel, "ModeWin", 0.82f, 0.525f, 300, 50),
                "Btn", (!settings.Fullscreen ? "\u25c9 " : "\u25cb ")
                    + c.L10n.Tr("settings.display_mode.windowed"),
                () => SetFullscreen(c, false), 1.05f);

            // 主音量（拨弦点击音即试听）
            UiKit.Text(UiKit.At(panel, "VolLabel", 0.20f, 0.405f, 300, 50),
                "T", c.L10n.Tr("settings.volume"), 1.15f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);

            UiKit.TextButton(UiKit.At(panel, "VolMinus", 0.44f, 0.405f, 70, 50),
                "Btn", "\u2212", () => BumpVolume(c, -GameSettings.VolumeStep), 1.2f,
                settings.MasterVolumePercent > 0);

            string volumeText = settings.MasterVolumePercent == 0
                ? c.L10n.Tr("settings.volume_mute")
                : c.L10n.TrF("settings.volume_value", settings.MasterVolumePercent);
            UiKit.Text(UiKit.At(panel, "VolValue", 0.63f, 0.405f, 360, 50),
                "T", volumeText, 1.05f,
                InkPalette.PaperText, TextAlignmentOptions.Center);

            UiKit.TextButton(UiKit.At(panel, "VolPlus", 0.84f, 0.405f, 70, 50),
                "Btn", "+", () => BumpVolume(c, +GameSettings.VolumeStep), 1.2f,
                settings.MasterVolumePercent < 100);

            // 无障碍说明
            UiKit.Text(UiKit.At(panel, "Note1", 0.5f, 0.29f, 900, 46),
                "T", c.L10n.Tr("settings.font_scale_note"), 1.0f,
                InkPalette.Faint, TextAlignmentOptions.Center);
            UiKit.Text(UiKit.At(panel, "Note2", 0.5f, 0.215f, 900, 46),
                "T", "\u2713 " + c.L10n.Tr("settings.colorblind_note"), 1.0f,
                InkPalette.Good, TextAlignmentOptions.Center);

            UiKit.TextButton(UiKit.At(panel, "BtnBack", 0.5f, 0.09f, 300, 56),
                "Btn", c.L10n.Tr("settings.back"), c.BackFromSettings, 1.2f);
        }

        private static string ResolutionLabel(GameController c, GameSettings settings)
        {
            if (settings.ResolutionWidth <= 0)
            {
                return c.L10n.Tr("settings.resolution.native");
            }
            return settings.ResolutionWidth + " \u00d7 " + settings.ResolutionHeight;
        }

        private static void CycleResolution(GameController c)
        {
            c.Settings.Current.CycleResolution();
            c.Settings.Save();
            DisplayService.Apply(c.Settings.Current);
            c.GoSettings();
        }

        private static void SetFullscreen(GameController c, bool fullscreen)
        {
            if (c.Settings.Current.Fullscreen == fullscreen) { return; }
            c.Settings.Current.Fullscreen = fullscreen;
            c.Settings.Save();
            DisplayService.Apply(c.Settings.Current);
            c.GoSettings();
        }

        private static void BumpVolume(GameController c, int delta)
        {
            GameSettings settings = c.Settings.Current;
            settings.MasterVolumePercent += delta;
            settings.ClampAll();
            c.Settings.Save();
            c.Audio.ApplyVolume(settings.MasterVolume01);
            c.GoSettings();
        }

        private static void SetLocale(GameController c, Locale locale)
        {
            if (c.L10n.Locale == locale) { return; }
            c.Settings.Current.Locale = locale;
            c.Settings.Save();
            c.L10n.SetLocale(locale); // LocaleChanged → 整屏重建取词
        }

        private static void Bump(GameController c, int delta)
        {
            GameSettings settings = c.Settings.Current;
            settings.FontScalePercent += delta;
            settings.ClampScale();
            c.Settings.Save();
            UiKit.Configure(c.Fonts, settings.EffectiveBaseFontPx);
            c.GoSettings();
        }
    }
}
