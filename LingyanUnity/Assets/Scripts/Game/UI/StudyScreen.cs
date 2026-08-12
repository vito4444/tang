using Lingyan.Core.Calendar;
using Lingyan.Core.Characters;
using Lingyan.Core.Economy;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 书房：开局落脚点。展示角色的四维、四轨官身、服色、三轨名声、
    /// 囊中钱与唐历时日——数据层的活体验收面。
    /// </summary>
    public static class StudyScreen
    {
        public static void Build(GameController c, RectTransform root)
        {
            UiKit.InkBackground(root);
            SaveData save = c.ActiveSave;
            if (save == null)
            {
                c.GoMainMenu();
                return;
            }

            ProtagonistDef def = ProtagonistCatalog.GetByKey(save.ProtagonistKey);
            bool en = c.L10n.Locale == Locale.En;

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.94f, 700, 64),
                "T", c.L10n.Tr("study.title"), 1.8f,
                InkPalette.PaperText, TextAlignmentOptions.Center);

            UiKit.Text(UiKit.At(root, "Who", 0.5f, 0.875f, 1200, 48),
                "T", save.CharacterName + " · " + c.L10n.Tr(def.TitleKey)
                    + " · " + c.L10n.Tr(def.StartStatusKey),
                1.15f, InkPalette.Faint, TextAlignmentOptions.Center);

            // 左列：四维 + 名声
            RectTransform left = UiKit.Rect(root, "LeftPanel",
                new Vector2(0.06f, 0.16f), new Vector2(0.47f, 0.82f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(left, "PanelBg");

            float y = 0.89f;
            SectionHead(c, left, "AttrHead", y, en ? "Attributes" : "四维");
            y -= 0.105f;
            AttrRow(left, "A1", y, c.L10n.Tr("attr.stamina"), save.Attributes.Stamina);
            y -= 0.082f;
            AttrRow(left, "A2", y, c.L10n.Tr("attr.health"), save.Attributes.Health);
            y -= 0.082f;
            AttrRow(left, "A3", y, c.L10n.Tr("attr.strength"), save.Attributes.Strength);
            y -= 0.082f;
            AttrRow(left, "A4", y, c.L10n.Tr("attr.wisdom"), save.Attributes.Wisdom);

            y -= 0.115f;
            SectionHead(c, left, "RepHead", y, c.L10n.Tr("study.reputation"));
            y -= 0.105f;
            RepRow(left, "R1", y, c.L10n.Tr("rep.guansheng"), save.Reputation.GuanSheng);
            y -= 0.082f;
            RepRow(left, "R2", y, c.L10n.Tr("rep.minwang"), save.Reputation.MinWang);
            y -= 0.082f;
            RepRow(left, "R3", y, c.L10n.Tr("rep.jianghu"), save.Reputation.JiangHu);

            // 右列：四轨 + 服色 + 钱 + 时日
            RectTransform right = UiKit.Rect(root, "RightPanel",
                new Vector2(0.53f, 0.16f), new Vector2(0.94f, 0.82f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(right, "PanelBg");

            OfficeDef zhishi = OfficialLadders.Get(save.Offices.ZhiShiId);
            SanGuanDef sanguan = SanGuanTable.Get(save.Offices.SanGuanId);
            XunGuanDef xun = XunGuanTable.ForZhuan(save.Offices.XunZhuan);
            JueDef jue = JueTable.Get(save.Offices.JueId);

            float ry = 0.90f;
            SectionHead(c, right, "TrackHead", ry, c.L10n.Tr("study.four_tracks"));
            ry -= 0.095f;
            Row(right, "T1", ry, c.L10n.Tr("track.zhishi"),
                zhishi == null
                    ? c.L10n.Tr("study.none")
                    : OfficeLabel(c, zhishi.Zh, zhishi.Grade, en));
            ry -= 0.075f;
            Row(right, "T2", ry, c.L10n.Tr("track.sanguan"),
                sanguan == null
                    ? c.L10n.Tr("study.none")
                    : OfficeLabel(c, sanguan.Zh, sanguan.Grade, en, sanguan.Pinyin));
            ry -= 0.075f;
            Row(right, "T3", ry, c.L10n.Tr("track.xunguan"),
                xun == null
                    ? c.L10n.Tr("study.no_xun")
                    : c.L10n.TrF("study.xun_zhuan", en ? xun.En : xun.Zh, xun.Zhuan));
            ry -= 0.075f;
            Row(right, "T4", ry, c.L10n.Tr("track.jue"),
                jue == null
                    ? c.L10n.Tr("study.none")
                    : OfficeLabel(c, jue.Zh, jue.Grade, en));

            // 服色（符号 + 颜色 + 文字三重编码）
            ry -= 0.11f;
            RobeColor robe = RobeColors.FromGrade(sanguan?.Grade);
            UiKit.Text(UiKit.At(right, "RobeLabel", 0.22f, ry, 300, 46),
                "T", c.L10n.Tr("study.robe"), 1.05f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            var swatch = UiKit.Swatch(UiKit.At(right, "RobeSwatch", 0.47f, ry, 42, 42),
                "Box", InkPalette.Hex(RobeColors.UiHex(robe)));
            UiKit.Stretch(swatch.rectTransform);
            UiKit.Text(UiKit.At(right, "RobeName", 0.72f, ry, 420, 46),
                "T", c.L10n.Tr(RobeKey(robe)), 1.1f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);

            ry -= 0.11f;
            var money = new Money(save.MoneyWen);
            Row(right, "Money", ry, c.L10n.Tr("study.money"), en ? money.ToEn() : money.ToZh());

            ry -= 0.075f;
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            Row(right, "Date", ry, c.L10n.Tr("study.date"), en ? date.ToEn() : date.ToZh());
            ry -= 0.075f;
            Row(right, "Term", ry, c.L10n.Tr("study.solar_term"),
                en ? date.SolarTerm.En : date.SolarTerm.Zh);

            // 底部动作
            UiKit.TextButton(UiKit.At(root, "BtnSave", 0.30f, 0.075f, 480, 56),
                "Btn", c.L10n.Tr("study.save_and_menu"),
                () =>
                {
                    c.Saves.Write(save);
                    c.GoMainMenu();
                }, 1.15f);

            UiKit.TextButton(UiKit.At(root, "BtnChapter", 0.70f, 0.075f, 560, 56),
                "Btn", c.L10n.Tr("study.chapter_locked"), null, 1.15f, false);

            UiKit.TextButton(UiKit.At(root, "BtnSettings", 0.94f, 0.94f, 180, 48),
                "Btn", c.L10n.Tr("menu.settings"), c.GoSettings, 1.0f);
        }

        private static string OfficeLabel(
            GameController c, string zh, RankGrade? grade, bool en, string enOverride = null)
        {
            string name = en ? (enOverride ?? c.L10n.OfficeEn(zh)) : zh;
            if (grade == null) { return name; }
            return name + " · " + (en ? grade.Value.ToEn() : grade.Value.ToZh());
        }

        private static void SectionHead(
            GameController c, RectTransform panel, string name, float y, string text)
        {
            UiKit.Text(UiKit.At(panel, name, 0.5f, y, 640, 50),
                "T", text, 1.25f, InkPalette.Seal, TextAlignmentOptions.Center);
            UiKit.Hairline(panel, name + "_Rule", 0.5f, y - 0.042f, 420, 0.17f);
        }

        private static void Row(
            RectTransform panel, string name, float y, string label, string value)
        {
            UiKit.Text(UiKit.At(panel, name + "_L", 0.24f, y, 330, 46),
                "T", label, 1.05f, InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, name + "_V", 0.68f, y, 560, 46),
                "T", value, 1.1f, InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
        }

        /// <summary>四维行：标签 + 数值 + 十二格双编码。</summary>
        private static void AttrRow(
            RectTransform panel, string name, float y, string label, int value)
        {
            UiKit.Text(UiKit.At(panel, name + "_L", 0.17f, y, 220, 46),
                "T", label, 1.05f, InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, name + "_V", 0.36f, y, 80, 46),
                "T", value.ToString(), 1.15f, InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Cells(panel, name + "_Cells", 0.48f, y, value);
        }

        /// <summary>名声行：标签 + 数值 + 朱砂进度条（0–100）。</summary>
        private static void RepRow(
            RectTransform panel, string name, float y, string label, int value)
        {
            UiKit.Text(UiKit.At(panel, name + "_L", 0.17f, y, 220, 46),
                "T", label, 1.05f, InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, name + "_V", 0.36f, y, 80, 46),
                "T", value.ToString(), 1.15f, InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Bar(panel, name + "_Bar", 0.66f, y, 300, value / 100f);
        }

        private static string RobeKey(RobeColor robe)
        {
            switch (robe)
            {
                case RobeColor.Purple: return "robe.purple";
                case RobeColor.Scarlet: return "robe.scarlet";
                case RobeColor.Green: return "robe.green";
                case RobeColor.Qing: return "robe.qing";
                default: return "robe.white";
            }
        }
    }
}
