using Lingyan.Core.Endings;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 结局卷轴：挂冠致仕后按身份与名声定档（多结局，规格第十一节），
    /// 附生涯回顾。存档不销毁——主菜单仍可续档再入。
    /// </summary>
    public static class EndingScreen
    {
        public static void Build(GameController c, RectTransform root)
        {
            SaveData save = c.ActiveSave;
            if (save == null)
            {
                c.GoMainMenu();
                return;
            }

            bool en = c.L10n.Locale == Locale.En;
            EndingResult ending = EndingService.Evaluate(save);

            UiKit.InkBackground(root);

            UiKit.Text(UiKit.At(root, "Head", 0.5f, 0.945f, 400, 44),
                "T", c.L10n.Tr("ending.head"), 1.0f,
                InkPalette.Faint, TextAlignmentOptions.Center);
            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.875f, 900, 84),
                "T", c.L10n.Tr(ending.TitleKey), 2.2f,
                InkPalette.Seal, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.822f, 560, 0.26f);

            UiKit.Text(UiKit.At(root, "Epilogue", 0.5f, 0.72f, 1240, 150),
                "T", c.L10n.Tr(ending.EpilogueKey), 1.1f,
                InkPalette.PaperText, TextAlignmentOptions.Center);

            UiKit.Text(UiKit.At(root, "HeroLine", 0.5f, 0.60f, 1200, 60),
                "T", "—— " + c.L10n.Tr(ending.ProtagonistLineKey), 1.0f,
                InkPalette.Faint, TextAlignmentOptions.Center);

            // 生涯回顾
            RectTransform panel = UiKit.Rect(root, "Recap",
                new Vector2(0.28f, 0.16f), new Vector2(0.72f, 0.54f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "Bg");
            UiKit.Text(UiKit.At(panel, "RecapHead", 0.5f, 0.90f, 500, 44),
                "T", c.L10n.Tr("ending.recap.head"), 1.15f,
                InkPalette.Seal, TextAlignmentOptions.Center);

            float y = 0.76f;
            if (ending.FinalOfficeId != null)
            {
                OfficeDef office = OfficialLadders.Get(ending.FinalOfficeId);
                string officeName = en ? c.L10n.OfficeEn(office.Zh) : office.Zh;
                string gradeText = en
                    ? office.Grade.Value.ToEnShort()
                    : office.Grade.Value.ToZh();
                UiKit.Text(UiKit.At(panel, "R_office", 0.5f, y, 760, 44),
                    "T", c.L10n.TrF("ending.recap.office", officeName, gradeText),
                    1.0f, InkPalette.PaperText, TextAlignmentOptions.Center);
                y -= 0.115f;
            }
            foreach (var line in ending.RecapLines)
            {
                UiKit.Text(UiKit.At(panel, "R_" + line.Key, 0.5f, y, 760, 44),
                    "T", c.L10n.TrF(line.Key, line.Args), 1.0f,
                    InkPalette.PaperText, TextAlignmentOptions.Center);
                y -= 0.115f;
            }

            UiKit.Text(UiKit.At(root, "SaveNote", 0.5f, 0.115f, 1000, 40),
                "T", c.L10n.Tr("ending.save_note"), 0.9f,
                InkPalette.Faint, TextAlignmentOptions.Center);
            UiKit.TextButton(UiKit.At(root, "BtnMenu", 0.5f, 0.06f, 360, 56),
                "Btn", c.L10n.Tr("ending.back_menu"), c.GoMainMenu, 1.2f);
        }
    }
}
