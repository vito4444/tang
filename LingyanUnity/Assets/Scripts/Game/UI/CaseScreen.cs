using System;
using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Cases;
using Lingyan.Core.Localization;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 线索板（规格第七节）：期限倒计时、已知线索、两两组合出推论、
    /// 关键证人门槛、双结局指认。取证条件不满足时给去向提示，不做空按钮。
    /// </summary>
    public static class CaseScreen
    {
        private static string _combineFirst;
        private static string _resultText;

        public static void Reset()
        {
            _combineFirst = null;
            _resultText = null;
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

            CaseDef def = SilkCase.Def;
            var now = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);

            if (!save.Cases.TryGetValue(def.Id, out SaveCaseState state))
            {
                c.GoStudy(save);
                return;
            }
            CaseService.CheckExpire(state, now);

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.945f, 900, 60),
                "T", c.L10n.Tr(def.TitleKey), 1.7f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.902f, 560, 0.24f);

            bool active = state.Status == (int)CaseStatus.Active;
            string deadlineText;
            Color deadlineColor;
            if (active)
            {
                int daysLeft = CaseService.DaysLeft(state, now);
                deadlineText = c.L10n.TrF("case.days_left", daysLeft);
                deadlineColor = daysLeft <= 3 ? InkPalette.Bad : InkPalette.Faint;
            }
            else
            {
                deadlineText = c.L10n.Tr(state.OutcomeKey ?? "case.closed");
                deadlineColor = InkPalette.Seal;
            }
            UiKit.Text(UiKit.At(root, "Deadline", 0.5f, 0.862f, 1400, 44),
                "T", deadlineText, 1.05f, deadlineColor, TextAlignmentOptions.Center);

            if (_resultText != null)
            {
                UiKit.Text(UiKit.At(root, "Result", 0.5f, 0.815f, 1500, 44),
                    "T", _resultText, 1.0f, InkPalette.Seal, TextAlignmentOptions.Center);
            }

            // 左：线索板
            RectTransform clues = UiKit.Rect(root, "CluePanel",
                new Vector2(0.035f, 0.12f), new Vector2(0.55f, 0.78f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(clues, "Bg");
            UiKit.Text(UiKit.At(clues, "Head", 0.5f, 0.93f, 400, 44),
                "T", c.L10n.Tr("case.clues_head"), 1.15f,
                InkPalette.Seal, TextAlignmentOptions.Center);

            float y = 0.82f;
            foreach (ClueDef clue in def.Clues)
            {
                bool has = state.Clues.Contains(clue.Id);
                if (has)
                {
                    string clueId = clue.Id;
                    bool selected = _combineFirst == clueId;
                    UiKit.TextButton(UiKit.At(clues, "Clue_" + clue.Id, 0.5f, y, 900, 52),
                        "Btn", (selected ? "\u25c9 " : "\u25cb ") + c.L10n.Tr(clue.Key),
                        () => ToggleCombine(c, save, def, state, clueId), 1.0f,
                        active);
                }
                else
                {
                    UiKit.Text(UiKit.At(clues, "Hint_" + clue.Id, 0.5f, y, 900, 52),
                        "T", "？ " + c.L10n.Tr(clue.HintKey), 1.0f,
                        InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
                    if (active)
                    {
                        UiKit.TextButton(UiKit.At(clues, "Get_" + clue.Id, 0.92f, y, 120, 46),
                            "Btn", c.L10n.Tr("case.fetch"),
                            () => TryFetch(c, save, def, state, clue.Id, now), 1.0f);
                    }
                }
                y -= 0.115f;
            }

            // 推论区
            y -= 0.02f;
            foreach (string inferenceId in state.Inferences)
            {
                InferenceDef inference = def.Inferences.First(i => i.Id == inferenceId);
                UiKit.Text(UiKit.At(clues, "Inf_" + inferenceId, 0.5f, y, 900, 52),
                    "T", "\u2713 " + c.L10n.Tr(inference.Key), 1.0f,
                    InkPalette.Good, TextAlignmentOptions.MidlineLeft);
                y -= 0.115f;
            }
            if (active && state.Clues.Count >= 2)
            {
                UiKit.Text(UiKit.At(clues, "CombineHint", 0.5f, 0.06f, 900, 40),
                    "T", c.L10n.Tr("case.combine_hint"), 1.0f,
                    InkPalette.Faint, TextAlignmentOptions.Center);
            }

            // 右：证人与指认
            RectTransform accuse = UiKit.Rect(root, "AccusePanel",
                new Vector2(0.58f, 0.12f), new Vector2(0.965f, 0.78f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(accuse, "Bg");
            UiKit.Text(UiKit.At(accuse, "Head", 0.5f, 0.93f, 400, 44),
                "T", c.L10n.Tr("case.accuse_head"), 1.15f,
                InkPalette.Seal, TextAlignmentOptions.Center);

            NpcPanelRenderer.Data witness = NpcPanelRenderer.Fetch(c, def.Witness.NpcId);
            bool witnessTalks = CaseService.WitnessWillTalk(
                def, save.Attributes.Wisdom, witness.Total);
            UiKit.Text(UiKit.At(accuse, "Witness", 0.5f, 0.83f, 640, 72),
                "T", (witnessTalks ? "\u2713 " : "\u2717 ")
                    + c.L10n.TrF(witnessTalks ? "case.witness_ok" : "case.witness_no",
                        c.L10n.Tr(witness.Schedule.NameKey)),
                1.0f, witnessTalks ? InkPalette.Good : InkPalette.Bad,
                TextAlignmentOptions.MidlineLeft);

            bool thoroughReady = CaseService.EvidenceCount(state) >= def.ThoroughEvidenceCount
                && witnessTalks;
            UiKit.Text(UiKit.At(accuse, "Evidence", 0.5f, 0.73f, 640, 44),
                "T", c.L10n.TrF("case.evidence_count",
                    CaseService.EvidenceCount(state), def.ThoroughEvidenceCount),
                1.0f, thoroughReady ? InkPalette.Good : InkPalette.Faint,
                TextAlignmentOptions.MidlineLeft);

            float sy = 0.60f;
            foreach (SuspectDef suspect in def.Suspects)
            {
                string suspectId = suspect.Id;
                UiKit.Text(UiKit.At(accuse, "S_" + suspect.Id, 0.30f, sy, 330, 48),
                    "T", c.L10n.Tr(suspect.NameKey), 1.05f,
                    InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
                if (active)
                {
                    UiKit.TextButton(UiKit.At(accuse, "F_" + suspect.Id, 0.62f, sy, 170, 46),
                        "Btn", c.L10n.Tr("case.accuse_forced"),
                        () => DoAccuse(c, save, def, state, suspectId,
                            AccuseMethod.Forced, witnessTalks, now), 1.0f);
                    UiKit.TextButton(UiKit.At(accuse, "T_" + suspect.Id, 0.85f, sy, 170, 46),
                        "Btn", c.L10n.Tr("case.accuse_thorough"),
                        () => DoAccuse(c, save, def, state, suspectId,
                            AccuseMethod.Thorough, witnessTalks, now), 1.0f, thoroughReady);
                }
                sy -= 0.13f;
            }
            UiKit.Text(UiKit.At(accuse, "MethodNote", 0.5f, 0.13f, 640, 80),
                "T", c.L10n.Tr("case.method_note"), 1.0f,
                InkPalette.Faint, TextAlignmentOptions.TopLeft);

            UiKit.TextButton(UiKit.At(root, "BtnBack", 0.5f, 0.055f, 300, 54),
                "Btn", c.L10n.Tr("creation.back"),
                () => { Reset(); c.GoStudy(save); }, 1.1f);
        }

        /// <summary>取证：各线索的获取条件（不满足给原因，不做空按钮）。</summary>
        private static void TryFetch(
            GameController c, SaveData save, CaseDef def, SaveCaseState state,
            string clueId, TangDate now)
        {
            string failKey = null;
            switch (clueId)
            {
                case "dossier":
                    if (!save.StoryFlags.TryGetValue("heard_silk_case", out bool heard) || !heard)
                    {
                        failKey = "case.fetch.need_hint";
                    }
                    break;
                case "permit":
                    if (NpcPanelRenderer.Fetch(c, "kang_san").Total < 40)
                    {
                        failKey = "case.fetch.need_kang";
                    }
                    break;
                case "patrol_log":
                    if (NpcPanelRenderer.Fetch(c, "zheng_wu").Total < 45)
                    {
                        failKey = "case.fetch.need_zheng";
                    }
                    break;
                case "torn_silk":
                    if (!state.Clues.Contains("dossier"))
                    {
                        failKey = "case.fetch.need_dossier";
                    }
                    break;
            }

            if (failKey != null)
            {
                _resultText = "\u2717 " + c.L10n.Tr(failKey);
            }
            else
            {
                CaseService.Discover(state, def, clueId);
                _resultText = "\u2713 " + c.L10n.Tr(def.Clue(clueId).Key);
            }
            c.GoCase();
        }

        private static void ToggleCombine(
            GameController c, SaveData save, CaseDef def, SaveCaseState state, string clueId)
        {
            if (_combineFirst == null || _combineFirst == clueId)
            {
                _combineFirst = _combineFirst == clueId ? null : clueId;
            }
            else
            {
                InferenceDef inference = CaseService.Combine(state, def, _combineFirst, clueId);
                _resultText = inference != null
                    ? "\u2713 " + c.L10n.Tr(inference.Key)
                    : c.L10n.Tr("case.combine_nothing");
                _combineFirst = null;
            }
            c.GoCase();
        }

        private static void DoAccuse(
            GameController c, SaveData save, CaseDef def, SaveCaseState state,
            string suspectId, AccuseMethod method, bool witnessTalks, TangDate now)
        {
            AccusationOutcome outcome;
            try
            {
                outcome = CaseService.Accuse(state, def, suspectId, method, witnessTalks);
            }
            catch (InvalidOperationException)
            {
                _resultText = "\u2717 " + c.L10n.Tr("case.accuse_not_ready");
                c.GoCase();
                return;
            }

            OutcomeApplier.ApplyReputation(save, ReputationTrack.GuanSheng,
                outcome.GuanShengDelta, "rep.src.case_" + (outcome.CorrectCulprit ? "right" : "wrong"), now);
            OutcomeApplier.ApplyReputation(save, ReputationTrack.MinWang,
                outcome.MinWangDelta, "rep.src.case_" + (outcome.CorrectCulprit ? "right" : "wrong"), now);
            save.Counters.TryGetValue("merit_points", out int merit);
            save.Counters["merit_points"] = merit + outcome.MeritPoints;
            save.Counters.TryGetValue("cases_closed", out int closed);
            save.Counters["cases_closed"] = closed + 1;

            _resultText = c.L10n.Tr(outcome.OutcomeKey);
            c.GoCase();
        }
    }
}
