using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Dialogue;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using Lingyan.Core.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 对话屏：底部台词框 + 编号选项。选项由 DialogueRunner 条件过滤，
    /// 效果统一落档；对话结束回 NPC 屏。坊景留作背景。
    /// </summary>
    public static class DialogueScreen
    {
        private static DialogueRunner _runner;
        private static string _npcId;

        /// <summary>尝试为 NPC 开启对话树；无树返回 false（调用方退回寒暄）。</summary>
        public static bool TryStart(GameController c, string npcId)
        {
            TextAsset asset = Resources.Load<TextAsset>("Data/dialogues/" + npcId);
            if (asset == null)
            {
                return false;
            }
            NpcScheduleDef schedule = SampleWard.Npcs.First(n => n.NpcId == npcId);
            _runner = new DialogueRunner(
                DialogueTree.Parse(asset.text), c.ActiveSave,
                NpcProfiles.Get(npcId), schedule.Archetype);
            _npcId = npcId;
            return true;
        }

        public static void Build(GameController c, RectTransform root)
        {
            SaveData save = c.ActiveSave;
            if (save == null || _runner == null)
            {
                c.GoMainMenu();
                return;
            }
            if (_runner.Finished)
            {
                End(c);
                return;
            }

            NpcScheduleDef schedule = SampleWard.Npcs.First(n => n.NpcId == _npcId);
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);

            // 台词框（下三分之一，纸底墨字）
            RectTransform box = UiKit.Rect(root, "SpeechBox",
                new Vector2(0.10f, 0.05f), new Vector2(0.90f, 0.40f),
                Vector2.zero, Vector2.zero);
            var bg = box.gameObject.AddComponent<Image>();
            bg.color = new Color(0.055f, 0.05f, 0.04f, 0.92f);
            bg.raycastTarget = true;
            UiKit.Frame(box, "Frame", InkPalette.Faint, 0.30f, 0f);
            UiKit.Frame(box, "FrameInner", InkPalette.Faint, 0.12f, 6f);

            UiKit.Text(UiKit.At(box, "Speaker", 0.14f, 0.895f, 320, 46),
                "T", c.L10n.Tr(schedule.NameKey), 1.15f,
                InkPalette.Seal, TextAlignmentOptions.MidlineLeft);
            UiKit.Hairline(box, "Rule", 0.5f, 0.82f, 1200, 0.18f);

            TextMeshProUGUI speech = UiKit.Text(
                UiKit.At(box, "Speech", 0.5f, 0.63f, 1380, 110),
                "T", c.L10n.Tr(_runner.Current.TextKey), 1.1f,
                InkPalette.PaperText, TextAlignmentOptions.TopLeft);
            speech.lineSpacing = 8f;

            // 选项
            List<DialogueChoice> choices = _runner.AvailableChoices();
            float y = 0.40f;
            int index = 1;
            foreach (DialogueChoice choice in choices)
            {
                DialogueChoice captured = choice;
                UiKit.TextButton(UiKit.At(box, "Choice" + index, 0.5f, y, 1340, 46),
                    "Btn", index + "．" + c.L10n.Tr(choice.TextKey),
                    () =>
                    {
                        _runner.Choose(captured, date);
                        if (_runner.Finished) { End(c); }
                        else { c.GoDialogue(); }
                    }, 1.0f);
                y -= 0.115f;
                index++;
            }
            if (choices.Count == 0)
            {
                UiKit.TextButton(UiKit.At(box, "ChoiceEnd", 0.5f, y, 1340, 46),
                    "Btn", c.L10n.Tr("dlg.finished"), () => End(c), 1.0f);
            }
        }

        private static void End(GameController c)
        {
            string npcId = _npcId;
            _runner = null;
            _npcId = null;
            NpcScreen.Reset();
            c.GoNpc(npcId);
        }
    }
}
