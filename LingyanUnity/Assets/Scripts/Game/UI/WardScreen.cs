using Lingyan.Core.Calendar;
using Lingyan.Core.Localization;
using Lingyan.Core.Saves;
using Lingyan.Core.World;
using TMPro;
using UnityEngine;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 槐里坊：阶段 2 的活体验收面。时辰流逝、坊门启闭、
    /// NPC 按作息表移动，全部由同一份数据驱动。
    /// 三维坊景与轨道相机在 Unity 视觉层实装时复用本屏之下的模型。
    /// </summary>
    public static class WardScreen
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

            bool en = c.L10n.Locale == Locale.En;
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);

            UiKit.Text(UiKit.At(root, "Title", 0.5f, 0.94f, 700, 64),
                "T", c.L10n.Tr(SampleWard.Ward.NameKey), 1.8f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
            UiKit.Hairline(root, "TitleRule", 0.5f, 0.896f, 480, 0.24f);

            UiKit.Text(UiKit.At(root, "Date", 0.5f, 0.855f, 1300, 48),
                "T", (en ? date.ToEn() : date.ToZh())
                    + " · " + (en ? date.SolarTerm.En : date.SolarTerm.Zh),
                1.15f, InkPalette.Faint, TextAlignmentOptions.Center);

            // 坊门状态（符号 + 颜色双编码）
            bool curfew = WardDef.CurfewAt(save.Date.HourIndex);
            UiKit.Text(UiKit.At(root, "GateState", 0.5f, 0.80f, 1300, 48),
                "T", c.L10n.Tr(curfew ? "ward.curfew_on" : "ward.gates_open"),
                1.1f, curfew ? InkPalette.Seal : InkPalette.Good,
                TextAlignmentOptions.Center);

            // 坊中此刻：NPC 实况
            RectTransform panel = UiKit.Rect(root, "NowPanel",
                new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.74f),
                Vector2.zero, Vector2.zero);
            UiKit.PanelBox(panel, "PanelBg");

            UiKit.Text(UiKit.At(panel, "NowHead", 0.5f, 0.87f, 640, 50),
                "T", c.L10n.Tr("ward.now"), 1.25f,
                InkPalette.Seal, TextAlignmentOptions.Center);
            UiKit.Hairline(panel, "NowRule", 0.5f, 0.76f, 420, 0.17f);

            float y = 0.60f;
            foreach (NpcScheduleDef npc in SampleWard.Npcs)
            {
                ScheduleEntry entry = npc.At(save.Date.HourIndex);
                PlaceDef place = SampleWard.Ward.Place(entry.PlaceId);
                string line = c.L10n.TrF("ward.npc_line",
                    c.L10n.Tr(npc.NameKey),
                    c.L10n.Tr(place.NameKey),
                    c.L10n.Tr(entry.ActivityKey));
                UiKit.Text(UiKit.At(panel, "Npc_" + npc.NpcId, 0.5f, y, 980, 46),
                    "T", line, 1.05f,
                    InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
                y -= 0.17f;
            }

            // 时辰推进
            UiKit.TextButton(UiKit.At(root, "BtnWait", 0.32f, 0.20f, 380, 56),
                "Btn", c.L10n.Tr("ward.wait_hour"),
                () => Advance(c, save, 1), 1.15f);
            UiKit.TextButton(UiKit.At(root, "BtnRest", 0.68f, 0.20f, 420, 56),
                "Btn", c.L10n.Tr("ward.rest_morning"),
                () => Advance(c, save,
                    TangDate.HoursUntilNext(save.Date.HourIndex, targetHourIndex: 3)), 1.15f);

            UiKit.TextButton(UiKit.At(root, "BtnBack", 0.5f, 0.09f, 320, 56),
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
