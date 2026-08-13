using System.Linq;
using Lingyan.Core.Economy;
using Lingyan.Core.Localization;
using Lingyan.Core.Marriage;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 议亲（规格第九节）：可议对象、议亲四问（好感/门第/聘财/婚约，✓✗ 双编码）、
    /// 六礼逐步推进。门第不当时钱与情皆到也不许——检查表把每条不许摆在明面。
    /// </summary>
    public static class MarriageScreen
    {
        private static string _matchId = "silk_daughter";
        private static string _lastRiteText;

        public static void Reset()
        {
            _matchId = "silk_daughter";
            _lastRiteText = null;
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
            bool en = c.L10n.Locale == Locale.En;

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.945f, 700, 60),
                "T", c.L10n.Tr("marriage.title"), 1.7f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.902f, 480, 0.24f);

            // 左：可议对象
            RectTransform list = UiKit.Rect(root, "Matches",
                new Vector2(0.035f, 0.14f), new Vector2(0.34f, 0.87f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(list, "Bg");
            float y = 0.90f;
            foreach (MatchDef match in MatchCatalog.All)
            {
                string captured = match.Id;
                bool selected = _matchId == match.Id;
                UiKit.TextButton(UiKit.At(list, "M_" + match.Id, 0.5f, y, 540, 50),
                    "Btn", (selected ? "\u25c9 " : "\u25cb ") + c.L10n.Tr(match.NameKey),
                    () => { _matchId = captured; c.GoMarriage(); }, 1.05f);
                UiKit.Text(UiKit.At(list, "MC_" + match.Id, 0.5f, y - 0.065f, 540, 40),
                    "T", c.L10n.Tr(match.ClanKey), 1.0f,
                    selected ? InkPalette.PaperText : InkPalette.Faint,
                    TextAlignmentOptions.Center);
                y -= 0.17f;
            }

            // 右：四问与六礼
            RectTransform panel = UiKit.Rect(root, "Detail",
                new Vector2(0.37f, 0.14f), new Vector2(0.965f, 0.87f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "Bg");

            MatchDef current = MatchCatalog.Get(_matchId);
            NpcPanelRenderer.Data npcLike = null;
            int affinity = 55; // 议亲对象暂非坊内 NPC：好感以对象档案基线折算（后续接 NPC 化）
            RankGrade? grade = SanGuanTable.Get(save.Offices.SanGuanId)?.Grade;
            bool betrothedToOther = save.Marriage.MatchId != null
                && save.Marriage.MatchId != current.Id;

            ProposalCheck check = MarriageService.CheckProposal(
                current, affinity, grade, save.MoneyWen,
                betrothed: betrothedToOther);

            UiKit.Text(UiKit.At(panel, "CheckHead", 0.5f, 0.92f, 500, 46),
                "T", c.L10n.Tr("marriage.check_head"), 1.2f,
                InkPalette.Seal, TextAlignmentOptions.Center);

            float cy = 0.83f;
            Row(c, panel, cy, !check.Reasons.Contains("marriage.deny.affinity"),
                c.L10n.TrF("marriage.check.affinity", affinity, current.MinAffinity));
            cy -= 0.07f;
            string rankLine = current.MinGrade == null
                ? c.L10n.Tr("marriage.check.rank_none")
                : c.L10n.TrF("marriage.check.rank",
                    en ? current.MinGrade.Value.ToEn() : current.MinGrade.Value.ToZh());
            Row(c, panel, cy, !check.Reasons.Contains("marriage.deny.rank"), rankLine);
            cy -= 0.07f;
            var betrothal = new Money(current.BetrothalWen);
            Row(c, panel, cy, !check.Reasons.Contains("marriage.deny.betrothal"),
                c.L10n.TrF("marriage.check.betrothal",
                    en ? betrothal.ToEn() : betrothal.ToZh()));
            cy -= 0.07f;
            Row(c, panel, cy, !check.Reasons.Contains("marriage.deny.betrothed"),
                c.L10n.Tr("marriage.check.free"));

            // 六礼进度
            cy -= 0.10f;
            UiKit.Text(UiKit.At(panel, "RitesHead", 0.5f, cy, 500, 46),
                "T", c.L10n.Tr("marriage.rites_head"), 1.2f,
                InkPalette.Seal, TextAlignmentOptions.Center);
            cy -= 0.075f;
            bool thisMatch = save.Marriage.MatchId == current.Id;
            int step = thisMatch ? save.Marriage.RiteStep : 0;
            for (int i = 0; i < RiteSteps.All.Count; i++)
            {
                bool done = i < step;
                UiKit.Text(UiKit.At(panel, "Rite" + i, 0.30f + (i % 3) * 0.22f,
                        cy - (i / 3) * 0.062f, 240, 40),
                    "T", (done ? "\u2713 " : "\u25cb ")
                        + c.L10n.Tr(RiteSteps.All[i].key).Split('—')[0],
                    1.0f, done ? InkPalette.Good : InkPalette.Faint,
                    TextAlignmentOptions.MidlineLeft);
            }

            if (_lastRiteText != null)
            {
                UiKit.Text(UiKit.At(panel, "RiteText", 0.5f, cy - 0.20f, 1000, 44),
                    "T", _lastRiteText, 1.0f, InkPalette.Seal, TextAlignmentOptions.Center);
            }

            // 动作
            if (save.Marriage.Married && thisMatch)
            {
                UiKit.Text(UiKit.At(panel, "Married", 0.5f, 0.10f, 800, 48),
                    "T", "\u2713 " + c.L10n.Tr("marriage.married"), 1.1f,
                    InkPalette.Good, TextAlignmentOptions.Center);
            }
            else
            {
                bool canAdvance = check.Allowed || (thisMatch && step > 0);
                UiKit.TextButton(UiKit.At(panel, "BtnRite", 0.5f, 0.10f, 360, 54),
                    "Btn", c.L10n.Tr("marriage.next_rite"),
                    () =>
                    {
                        _lastRiteText = c.L10n.Tr(
                            MarriageService.AdvanceRite(save, current.Id, current));
                        c.AutoSave();
                        c.GoMarriage();
                    }, 1.1f, canAdvance);
            }

            UiKit.TextButton(UiKit.At(root, "BtnBack", 0.5f, 0.055f, 300, 54),
                "Btn", c.L10n.Tr("creation.back"),
                () => { Reset(); c.GoStudy(save); }, 1.1f);
        }

        private static void Row(
            GameController c, RectTransform panel, float y, bool ok, string text)
        {
            UiKit.Text(UiKit.At(panel, "Chk" + y, 0.5f, y, 900, 44),
                "T", (ok ? "\u2713 " : "\u2717 ") + text, 1.05f,
                ok ? InkPalette.Good : InkPalette.Bad,
                TextAlignmentOptions.MidlineLeft);
        }
    }
}
