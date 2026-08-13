using System;
using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Localization;
using Lingyan.Core.Saves;
using Lingyan.Core.Terminology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 典籍（Codex 百科）：分类 → 词条列表（未解锁以？？？占位）→ 详情。
    /// 词库即术语表：中文名 + 锁定英译 + 考据注，Hucker 台账直接面向玩家。
    /// </summary>
    public static class CodexScreen
    {
        private static readonly string[] Categories =
        {
            "office", "jue", "institution", "exam", "rite",
            "architecture", "system", "currency", "military"
        };

        private static string _category = "office";
        private static int _page;
        private static string _detailId;

        public static void Reset()
        {
            _category = "office";
            _page = 0;
            _detailId = null;
        }

        public static void Build(GameController c, RectTransform root)
        {
            SaveData save = c.ActiveSave;
            if (save == null)
            {
                c.GoMainMenu();
                return;
            }
            UiKit.InkBackground(root);
            Glossary glossary = c.L10n.Glossary;

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.945f, 700, 60),
                "T", c.L10n.Tr("codex.title"), 1.7f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.902f, 480, 0.24f);

            // 左：分类
            RectTransform cats = UiKit.Rect(root, "Cats",
                new Vector2(0.035f, 0.12f), new Vector2(0.24f, 0.87f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(cats, "Bg");
            float y = 0.93f;
            foreach (string category in Categories)
            {
                string captured = category;
                var (unlocked, total) = CodexService.Progress(save, glossary, category);
                bool selected = _category == category;
                UiKit.TextButton(UiKit.At(cats, "Cat_" + category, 0.5f, y, 330, 46),
                    "Btn",
                    (selected ? "\u25c9 " : "\u25cb ") + c.L10n.Tr("codex.cat." + category)
                        + "　" + unlocked + "/" + total,
                    () => { _category = captured; _page = 0; _detailId = null; c.GoCodex(); },
                    1.0f);
                y -= 0.095f;
            }

            // 右：词条列表 + 详情
            RectTransform panel = UiKit.Rect(root, "Entries",
                new Vector2(0.27f, 0.12f), new Vector2(0.965f, 0.87f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "Bg");

            List<GlossaryEntry> entries = glossary.Entries
                .Where(e => e.Category == _category)
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .ToList();

            const int pageSize = 9;
            int pageCount = Math.Max(1, (entries.Count + pageSize - 1) / pageSize);
            _page = Math.Min(_page, pageCount - 1);

            float ey = 0.93f;
            foreach (GlossaryEntry entry in entries.Skip(_page * pageSize).Take(pageSize))
            {
                bool unlocked = CodexService.IsUnlocked(save, entry.Id);
                string label = unlocked
                    ? entry.Zh + "　—　" + entry.En
                    : c.L10n.Tr("codex.locked");
                string captured = entry.Id;
                UiKit.TextButton(UiKit.At(panel, "E_" + entry.Id, 0.5f, ey, 1200, 44),
                    "Btn", label,
                    () => { _detailId = captured; c.GoCodex(); }, 1.0f, unlocked);
                ey -= 0.062f;
            }

            if (pageCount > 1)
            {
                UiKit.TextButton(UiKit.At(panel, "PgPrev", 0.40f, 0.34f, 70, 40),
                    "Btn", "\u2039",
                    () => { _page = Math.Max(0, _page - 1); c.GoCodex(); }, 1.0f, _page > 0);
                UiKit.Text(UiKit.At(panel, "PgNum", 0.5f, 0.34f, 120, 40),
                    "T", (_page + 1) + " / " + pageCount, 1.0f,
                    InkPalette.Faint, TextAlignmentOptions.Center);
                UiKit.TextButton(UiKit.At(panel, "PgNext", 0.60f, 0.34f, 70, 40),
                    "Btn", "\u203a",
                    () => { _page = Math.Min(pageCount - 1, _page + 1); c.GoCodex(); },
                    1.0f, _page < pageCount - 1);
            }

            // 详情区
            GlossaryEntry detail = _detailId != null ? glossary.ById(_detailId) : null;
            if (detail != null && CodexService.IsUnlocked(save, detail.Id))
            {
                UiKit.Hairline(panel, "DetailRule", 0.5f, 0.30f, 1100, 0.20f);
                UiKit.Text(UiKit.At(panel, "DetailZh", 0.5f, 0.245f, 1200, 50),
                    "T", detail.Zh + "　·　" + detail.En, 1.2f,
                    InkPalette.Seal, TextAlignmentOptions.Center);
                string note = string.IsNullOrEmpty(detail.Note)
                    ? c.L10n.Tr("codex.no_note")
                    : detail.Note;
                if (!detail.Verified && (detail.Category == "office" || detail.Category == "jue"))
                {
                    note += "　" + c.L10n.Tr("codex.unverified");
                }
                TextMeshProUGUI noteText = UiKit.Text(
                    UiKit.At(panel, "DetailNote", 0.5f, 0.13f, 1200, 130),
                    "T", note, 1.0f, InkPalette.PaperText, TextAlignmentOptions.TopLeft);
                noteText.lineSpacing = 8f;
            }

            UiKit.TextButton(UiKit.At(root, "BtnBack", 0.5f, 0.055f, 300, 54),
                "Btn", c.L10n.Tr("creation.back"),
                () => { Reset(); c.GoStudy(save); }, 1.1f);
        }
    }
}
