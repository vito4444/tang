using Lingyan.Core.Calendar;
using Lingyan.Core.Localization;
using Lingyan.Core.Saves;
using Lingyan.Core.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 槐里坊：三维坊景（轨道相机漫游）+ HUD。
    /// 时辰驱动天光、坊门启闭与 NPC 落位；HUD 只做信息与推进。
    /// </summary>
    public static class WardScreen
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
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);

            // 顶部信息条（半透明墨带，压在 3D 之上）
            RectTransform topBar = UiKit.Rect(root, "TopBar",
                new Vector2(0f, 0.925f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var topBg = topBar.gameObject.AddComponent<Image>();
            topBg.color = new Color(0.05f, 0.045f, 0.035f, 0.55f);
            topBg.raycastTarget = false;

            UiKit.Text(UiKit.At(topBar, "WardName", 0.09f, 0.5f, 320, 50),
                "T", c.L10n.Tr(SampleWard.Ward.NameKey), 1.25f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);

            UiKit.Text(UiKit.At(topBar, "Date", 0.45f, 0.5f, 1150, 50),
                "T", (en ? date.ToEn() : date.ToZh())
                    + " · " + (en ? date.SolarTerm.En : date.SolarTerm.Zh),
                1.0f, InkPalette.Faint, TextAlignmentOptions.Center);

            bool curfew = WardDef.CurfewAt(save.Date.HourIndex);
            UiKit.Text(UiKit.At(topBar, "GateState", 0.85f, 0.5f, 560, 50),
                "T", c.L10n.Tr(curfew ? "ward.curfew_on" : "ward.gates_open"),
                1.0f, curfew ? InkPalette.Seal : InkPalette.Good,
                TextAlignmentOptions.MidlineRight);

            // 右上：坊中此刻（小面板）
            RectTransform nowPanel = UiKit.Rect(root, "NowPanel",
                new Vector2(0.665f, 0.60f), new Vector2(0.985f, 0.90f),
                Vector2.zero, Vector2.zero);
            var nowBg = nowPanel.gameObject.AddComponent<Image>();
            nowBg.color = new Color(0.05f, 0.045f, 0.035f, 0.64f);
            nowBg.raycastTarget = false;
            UiKit.Frame(nowPanel, "Frame", InkPalette.Faint, 0.22f, 0f);

            UiKit.Text(UiKit.At(nowPanel, "Head", 0.5f, 0.88f, 500, 42),
                "T", c.L10n.Tr("ward.now"), 1.05f, InkPalette.Seal,
                TextAlignmentOptions.Center);

            float y = 0.66f;
            foreach (NpcScheduleDef npc in SampleWard.Npcs)
            {
                ScheduleEntry entry = npc.At(save.Date.HourIndex);
                PlaceDef place = SampleWard.Ward.Place(entry.PlaceId);
                string line = c.L10n.TrF("ward.npc_line",
                    c.L10n.Tr(npc.NameKey),
                    c.L10n.Tr(place.NameKey),
                    c.L10n.Tr(entry.ActivityKey));
                TextMeshProUGUI text = UiKit.Text(
                    UiKit.At(nowPanel, "Npc_" + npc.NpcId, 0.5f, y, 560, 64),
                    "T", line, 1.0f, InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
                text.rectTransform.offsetMin = new Vector2(24, text.rectTransform.offsetMin.y);
                y -= 0.24f;
            }

            // 左下：操作提示
            UiKit.Text(UiKit.At(root, "ControlsHint", 0.16f, 0.145f, 560, 40),
                "T", c.L10n.Tr("ward.controls_hint"), 1.0f,
                new Color(InkPalette.PaperText.r, InkPalette.PaperText.g,
                    InkPalette.PaperText.b, 0.66f),
                TextAlignmentOptions.MidlineLeft);

            // 底部按钮条
            RectTransform bottomBar = UiKit.Rect(root, "BottomBar",
                new Vector2(0f, 0f), new Vector2(1f, 0.105f), Vector2.zero, Vector2.zero);
            var bottomBg = bottomBar.gameObject.AddComponent<Image>();
            bottomBg.color = new Color(0.05f, 0.045f, 0.035f, 0.55f);
            bottomBg.raycastTarget = false;

            UiKit.TextButton(UiKit.At(root, "BtnWait", 0.30f, 0.052f, 380, 54),
                "Btn", c.L10n.Tr("ward.wait_hour"),
                () => Advance(c, save, 1), 1.1f);
            UiKit.TextButton(UiKit.At(root, "BtnRest", 0.53f, 0.052f, 420, 54),
                "Btn", c.L10n.Tr("ward.rest_morning"),
                () => Advance(c, save,
                    TangDate.HoursUntilNext(save.Date.HourIndex, targetHourIndex: 3)), 1.1f);
            UiKit.TextButton(UiKit.At(root, "BtnBack", 0.76f, 0.052f, 320, 54),
                "Btn", c.L10n.Tr("ward.back_study"),
                () => c.GoStudy(save), 1.1f);
        }

        private static void Advance(GameController c, SaveData save, int shiChen)
        {
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            date.AdvanceHours(shiChen);
            save.Date.EraId = date.EraId;
            save.Date.EraYear = date.EraYear;
            save.Date.Month = date.Month;
            save.Date.Day = date.Day;
            save.Date.HourIndex = date.HourIndex;
            c.GoWard(save);
        }
    }
}
