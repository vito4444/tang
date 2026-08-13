using Lingyan.Core.Characters;
using Lingyan.Core.Economy;
using Lingyan.Core.Officials;
using Lingyan.Game.Services;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 选人界面：五位主角，四维数值全部明示；
    /// 仅白身（#2）可自由分配点数，其余固定预设、只读展示。
    /// </summary>
    public static class CharacterCreationScreen
    {
        private static CharacterDraft _draft;

        public static void Build(GameController c, RectTransform root)
        {
            UiKit.InkBackground(root);
            if (_draft == null)
            {
                _draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            }

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.945f, 800, 60),
                "TitleText", c.L10n.Tr("creation.title"), 1.7f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.902f, 520, 0.24f);

            BuildOriginList(c, root);
            BuildDetail(c, root);

            UiKit.TextButton(UiKit.At(root, "BtnBack", 0.09f, 0.06f, 260, 56),
                "Btn", c.L10n.Tr("creation.back"),
                () => { _draft = null; c.GoMainMenu(); }, 1.15f);

            UiKit.TextButton(UiKit.At(root, "BtnConfirm", 0.91f, 0.06f, 260, 56),
                "Btn", c.L10n.Tr("creation.confirm"),
                () => Confirm(c), 1.3f);
        }

        private static void BuildOriginList(GameController c, RectTransform root)
        {
            RectTransform panel = UiKit.Rect(root, "OriginPanel",
                new Vector2(0.035f, 0.13f), new Vector2(0.30f, 0.90f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "PanelBg");

            float y = 0.92f;
            foreach (ProtagonistDef def in ProtagonistCatalog.All)
            {
                ProtagonistDef captured = def;
                bool selected = _draft.Def.Id == def.Id;
                string marker = selected ? "\u25c9 " : "\u25cb ";

                UiKit.TextButton(
                    UiKit.At(panel, "Origin_" + def.Key, 0.5f, y, 460, 54),
                    "Btn",
                    marker + c.L10n.Tr(def.TitleKey),
                    () =>
                    {
                        _draft = CharacterCreationRules.NewDraft(captured.Id);
                        RefreshVia(c);
                    },
                    1.2f);

                UiKit.Text(UiKit.At(panel, "Role_" + def.Key, 0.5f, y - 0.062f, 470, 56),
                    "RoleText", c.L10n.Tr(def.RoleKey()), 1.0f,
                    selected ? InkPalette.PaperText : InkPalette.Faint,
                    TextAlignmentOptions.Center);

                y -= 0.185f;
            }
        }

        private static void BuildDetail(GameController c, RectTransform root)
        {
            RectTransform panel = UiKit.Rect(root, "DetailPanel",
                new Vector2(0.33f, 0.13f), new Vector2(0.965f, 0.90f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "PanelBg");
            ProtagonistDef def = _draft.Def;

            // 姓名
            UiKit.Text(UiKit.At(panel, "NameLabel", 0.10f, 0.93f, 200, 44),
                "T", c.L10n.Tr("creation.name_label"), 1.05f,
                InkPalette.Faint, TextAlignmentOptions.MidlineRight);
            TMP_InputField nameInput = UiKit.Input(
                UiKit.At(panel, "NameInput", 0.32f, 0.93f, 300, 52), "Input",
                _draft.Name, 12);
            nameInput.onValueChanged.AddListener(v => { _draft.Name = v; });

            // 身世
            TextMeshProUGUI blurb = UiKit.Text(
                UiKit.At(panel, "Blurb", 0.5f, 0.815f, 1080, 120),
                "BlurbText", c.L10n.Tr(def.BlurbKey), 1.0f,
                InkPalette.PaperText, TextAlignmentOptions.TopLeft);
            blurb.lineSpacing = 8f;

            // 四维
            float y = 0.635f;
            foreach (AttributeId attr in AttributeSet.AllIds)
            {
                AttributeId captured = attr;
                UiKit.Text(UiKit.At(panel, "AttrLabel_" + attr, 0.16f, y, 260, 46),
                    "T", c.L10n.Tr(AttrKey(attr)), 1.1f,
                    InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);

                UiKit.Text(UiKit.At(panel, "AttrValue_" + attr, 0.36f, y, 90, 46),
                    "T", _draft.Attributes.Get(attr).ToString(), 1.25f,
                    InkPalette.PaperText, TextAlignmentOptions.Center);

                if (def.FreeAllocation)
                {
                    UiKit.TextButton(UiKit.At(panel, "AttrMinus_" + attr, 0.44f, y, 60, 46),
                        "Btn", "\u2212",
                        () => { CharacterCreationRules.TryDecrease(_draft, captured); RefreshVia(c); },
                        1.2f);
                    UiKit.TextButton(UiKit.At(panel, "AttrPlus_" + attr, 0.50f, y, 60, 46),
                        "Btn", "+",
                        () => { CharacterCreationRules.TryIncrease(_draft, captured); RefreshVia(c); },
                        1.2f);
                }
                // 十二格数值条：格数 + 实心/空心双编码
                UiKit.Cells(panel, "AttrCells_" + attr, 0.565f, y,
                    _draft.Attributes.Get(attr));
                y -= 0.075f;
            }

            if (def.FreeAllocation)
            {
                UiKit.Text(UiKit.At(panel, "Pool", 0.30f, 0.32f, 520, 46),
                    "T", c.L10n.TrF("creation.points_remaining",
                        CharacterCreationRules.RemainingPool(_draft)),
                    1.1f, InkPalette.Seal, TextAlignmentOptions.MidlineLeft);

                BuildEntryPathPicker(c, panel);
            }
            else
            {
                UiKit.Text(UiKit.At(panel, "Locked", 0.30f, 0.32f, 520, 46),
                    "T", c.L10n.Tr("creation.locked_preset"), 1.0f,
                    InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            }

            // 起点信息（右列）
            float infoY = 0.60f;
            InfoRow(c, panel, "InfoMoney", infoY, "creation.start_money",
                new Money(def.StartMoneyWen).ToZhOrEn(c));
            InfoRow(c, panel, "InfoStatus", infoY - 0.10f, "creation.start_status",
                c.L10n.Tr(def.StartStatusKey));
            InfoRow(c, panel, "InfoLine", infoY - 0.20f, "creation.career_line",
                c.L10n.Tr(LineKey(def.Line)));

            // 错误提示（若有；双编码：✗ + 色）
            string error = CharacterCreationRules.ValidateFinal(_draft);
            if (error != null)
            {
                UiKit.Text(UiKit.At(panel, "DraftError", 0.5f, 0.05f, 1000, 44),
                    "T", "\u2717 " + c.L10n.Tr(error), 1.0f,
                    InkPalette.Bad, TextAlignmentOptions.Center);
            }
        }

        private static void BuildEntryPathPicker(GameController c, RectTransform panel)
        {
            UiKit.Text(UiKit.At(panel, "EntryLabel", 0.30f, 0.25f, 520, 44),
                "T", c.L10n.Tr("creation.entry_path"), 1.1f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);

            var options = new[]
            {
                (EntryPath.KejuMingJing, "creation.entry.keju_mingjing"),
                (EntryPath.KejuJinShi, "creation.entry.keju_jinshi"),
                (EntryPath.TouJun, "creation.entry.toujun")
            };
            float y = 0.195f;
            foreach (var (path, key) in options)
            {
                EntryPath captured = path;
                bool selected = _draft.EntryPath == path;
                UiKit.TextButton(UiKit.At(panel, "Entry_" + path, 0.5f, y, 1080, 46),
                    "Btn",
                    (selected ? "\u25c9 " : "\u25cb ") + c.L10n.Tr(key),
                    () => { _draft.EntryPath = captured; RefreshVia(c); },
                    1.0f);
                y -= 0.055f;
            }
        }

        private static void InfoRow(
            GameController c, RectTransform panel, string name, float y,
            string labelKey, string value)
        {
            UiKit.Text(UiKit.At(panel, name + "_L", 0.80f, y, 300, 40),
                "T", c.L10n.Tr(labelKey), 1.0f,
                InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, name + "_V", 0.80f, y - 0.064f, 300, 80),
                "T", value, 1.05f,
                InkPalette.PaperText, TextAlignmentOptions.TopLeft);
        }

        private static void Confirm(GameController c)
        {
            string error = CharacterCreationRules.ValidateFinal(_draft);
            if (error != null)
            {
                RefreshVia(c);
                return;
            }
            Lingyan.Core.Saves.SaveData save =
                Lingyan.Core.Saves.SaveFactory.NewGame(_draft, System.DateTime.UtcNow);
            c.Saves.Write(save);
            _draft = null;
            c.GoStudy(save);
        }

        private static void RefreshVia(GameController c)
        {
            c.GoCreation();
        }

        private static string AttrKey(AttributeId attr)
        {
            switch (attr)
            {
                case AttributeId.Stamina: return "attr.stamina";
                case AttributeId.Health: return "attr.health";
                case AttributeId.Strength: return "attr.strength";
                default: return attr == AttributeId.Wisdom ? "attr.wisdom" : "attr.health";
            }
        }

        private static string LineKey(CareerLine line)
        {
            switch (line)
            {
                case CareerLine.CivilJudicial: return "line.civil_judicial";
                case CareerLine.Military: return "line.military";
                case CareerLine.Palace: return "line.palace";
                case CareerLine.Trade: return "line.trade";
                default: return "line.undecided";
            }
        }
    }

    internal static class CreationExtensions
    {
        public static string RoleKey(this ProtagonistDef def)
        {
            return "protagonist." + def.Key + ".role";
        }

        /// <summary>钱按当前语言排版。</summary>
        public static string ToZhOrEn(this Money money, GameController c)
        {
            return c.L10n.Locale == Lingyan.Core.Localization.Locale.En
                ? money.ToEn()
                : money.ToZh();
        }
    }
}
