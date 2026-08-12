using Lingyan.Core.Localization;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>主菜单：深墨底、纸色题字、朱砂印。占位美术，方向为水墨。</summary>
    public static class MainMenuScreen
    {
        public static void Build(GameController c, RectTransform root)
        {
            UiKit.InkBackground(root);

            // 题字
            TMPro.TextMeshProUGUI title = UiKit.Text(
                UiKit.At(root, "Title", 0.5f, 0.72f, 900, 220),
                "TitleText", c.L10n.Tr("game.title"), 5.2f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            title.characterSpacing = 18f;

            UiKit.Text(
                UiKit.At(root, "Subtitle", 0.5f, 0.585f, 900, 44),
                "SubtitleText", c.L10n.Tr("game.subtitle"), 1.0f,
                InkPalette.Faint, TextAlignmentOptions.Center);

            // 朱砂印（题字右下角）
            RectTransform seal = UiKit.At(root, "Seal", 0.5f, 0.66f, 84, 84);
            seal.anchoredPosition = new Vector2(320, 30);
            UnityEngine.UI.Image sealBox = UiKit.Swatch(seal, "SealBox", InkPalette.Seal);
            UiKit.Stretch(sealBox.rectTransform);
            TextMeshProUGUI sealText = UiKit.Text(seal, "SealText", "凌\n烟", 1.0f,
                InkPalette.Hex("EDE4D2"), TextAlignmentOptions.Center);
            UiKit.Stretch(sealText.rectTransform);
            sealText.lineSpacing = -18f;

            // 读档失败等待展示的错误（响亮，双编码：✘ + 色）
            if (!string.IsNullOrEmpty(c.PendingErrorKey))
            {
                UiKit.Text(
                    UiKit.At(root, "Error", 0.5f, 0.50f, 1400, 60),
                    "ErrorText", "✘ " + c.L10n.Tr(c.PendingErrorKey), 1.0f,
                    InkPalette.Bad, TextAlignmentOptions.Center);
                c.ClearPendingError();
            }

            // 按钮列
            float y = 0.42f;
            const float step = 0.068f;

            UiKit.TextButton(UiKit.At(root, "BtnNew", 0.5f, y, 420, 56),
                "Btn", c.L10n.Tr("menu.new"), c.GoCreation, 1.25f);
            y -= step;

            bool hasSave = c.Saves.HasSave;
            UiKit.TextButton(UiKit.At(root, "BtnContinue", 0.5f, y, 420, 56),
                "Btn", c.L10n.Tr("menu.continue"), c.ContinueCareer, 1.25f, hasSave);
            if (!hasSave)
            {
                UiKit.Text(UiKit.At(root, "NoSaveHint", 0.5f, y - 0.031f, 420, 30),
                    "Hint", c.L10n.Tr("menu.no_save_hint"), 1.0f,
                    InkPalette.Disabled, TextAlignmentOptions.Center);
                y -= 0.030f;
            }
            y -= step;

            UiKit.TextButton(UiKit.At(root, "BtnSettings", 0.5f, y, 420, 56),
                "Btn", c.L10n.Tr("menu.settings"), c.GoSettings, 1.25f);
            y -= step;

            UiKit.TextButton(UiKit.At(root, "BtnQuit", 0.5f, y, 420, 56),
                "Btn", c.L10n.Tr("menu.quit"), c.QuitGame, 1.25f);

            // 页脚：语言直切 / 版本与工作标题 / 字体授权
            UiKit.TextButton(UiKit.At(root, "BtnLanguage", 0.12f, 0.055f, 380, 44),
                "Btn", c.L10n.Tr("menu.language"),
                () =>
                {
                    Locale next = c.L10n.Locale == Locale.ZhHans ? Locale.En : Locale.ZhHans;
                    c.Settings.Current.Locale = next;
                    c.Settings.Save();
                    c.L10n.SetLocale(next);
                }, 1.0f);

            UiKit.Text(UiKit.At(root, "Version", 0.5f, 0.055f, 700, 40),
                "VersionText",
                c.L10n.Tr("game.working_title_note") + " · v" + Application.version,
                1.0f, InkPalette.Disabled, TextAlignmentOptions.Center);

            UiKit.Text(UiKit.At(root, "FontCredit", 0.87f, 0.055f, 460, 40),
                "FontCreditText", c.L10n.Tr("footer.font_credit"), 1.0f,
                InkPalette.Disabled, TextAlignmentOptions.Center);
        }
    }
}
