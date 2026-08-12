using Lingyan.Core.Localization;
using Lingyan.Core.Settings;
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
            UiKit.Text(UiKit.At(panel, "LangLabel", 0.20f, 0.85f, 300, 50),
                "T", c.L10n.Tr("settings.language"), 1.15f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);

            bool isZh = c.L10n.Locale == Locale.ZhHans;
            UiKit.TextButton(UiKit.At(panel, "LangZh", 0.52f, 0.85f, 300, 50),
                "Btn", (isZh ? "\u25c9 " : "\u25cb ") + c.L10n.Tr("settings.language.zh"),
                () => SetLocale(c, Locale.ZhHans), 1.05f);
            UiKit.TextButton(UiKit.At(panel, "LangEn", 0.82f, 0.85f, 300, 50),
                "Btn", (!isZh ? "\u25c9 " : "\u25cb ") + c.L10n.Tr("settings.language.en"),
                () => SetLocale(c, Locale.En), 1.05f);

            // 字号
            UiKit.Text(UiKit.At(panel, "FontLabel", 0.20f, 0.65f, 300, 50),
                "T", c.L10n.Tr("settings.font_scale"), 1.15f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);

            UiKit.TextButton(UiKit.At(panel, "FontMinus", 0.44f, 0.65f, 70, 50),
                "Btn", "\u2212", () => Bump(c, -10), 1.2f,
                settings.FontScalePercent > GameSettings.MinScalePercent);

            UiKit.Text(UiKit.At(panel, "FontValue", 0.63f, 0.65f, 360, 50),
                "T", c.L10n.TrF("settings.font_scale_value",
                    settings.FontScalePercent, settings.EffectiveBaseFontPx),
                1.05f, InkPalette.PaperText, TextAlignmentOptions.Center);

            UiKit.TextButton(UiKit.At(panel, "FontPlus", 0.84f, 0.65f, 70, 50),
                "Btn", "+", () => Bump(c, +10), 1.2f,
                settings.FontScalePercent < GameSettings.MaxScalePercent);

            // 无障碍说明
            UiKit.Text(UiKit.At(panel, "Note1", 0.5f, 0.45f, 900, 46),
                "T", c.L10n.Tr("settings.font_scale_note"), 1.0f,
                InkPalette.Faint, TextAlignmentOptions.Center);
            UiKit.Text(UiKit.At(panel, "Note2", 0.5f, 0.36f, 900, 46),
                "T", "\u2713 " + c.L10n.Tr("settings.colorblind_note"), 1.0f,
                InkPalette.Good, TextAlignmentOptions.Center);

            UiKit.TextButton(UiKit.At(panel, "BtnBack", 0.5f, 0.12f, 300, 56),
                "Btn", c.L10n.Tr("settings.back"), c.BackFromSettings, 1.2f);
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
