using System.Collections.Generic;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using Lingyan.Core.World;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// NPC 好感明细的共用渲染器：悬停浮签与互动屏共用同一份取数与排版逻辑
    /// （规格第六节：每条带来源与时间，✓/✗ 符号与颜色双编码，不能只靠红绿）。
    /// </summary>
    public static class NpcPanelRenderer
    {
        public sealed class Data
        {
            public NpcScheduleDef Schedule;
            public NpcProfile Profile;
            public NpcState State;
            public int Total;
            public List<DynamicAffinity> Dynamics;
        }

        /// <summary>取一名 NPC 的完整社交视图（好感合成口径全游戏唯一）。</summary>
        public static Data Fetch(GameController c, string npcId)
        {
            SaveData save = c.ActiveSave;
            var data = new Data
            {
                Schedule = SampleWard.Npcs.First(n => n.NpcId == npcId),
                Profile = NpcProfiles.Get(npcId),
                State = NpcStateStore.Load(save, npcId)
            };
            var reputation = new ReputationState(
                save.Reputation.GuanSheng, save.Reputation.MinWang, save.Reputation.JiangHu);
            RobeColor playerRobe = RobeColors.FromGrade(
                SanGuanTable.Get(save.Offices.SanGuanId)?.Grade);
            data.Dynamics = AffinityService.DynamicItems(
                reputation, data.Schedule.Archetype, playerRobe, null);
            data.Total = AffinityService.Total(
                data.Profile, data.State, reputation, data.Schedule.Archetype,
                playerRobe, null);
            return data;
        }

        /// <summary>好感数字的观感色。</summary>
        public static Color TotalColor(int total)
        {
            if (total >= 60) { return InkPalette.Good; }
            if (total <= 25) { return InkPalette.Bad; }
            return InkPalette.PaperText;
        }

        /// <summary>一条增减行："✓ +8 赠《文选》一部，投其所好 · 三月十七"。</summary>
        public static void EntryRow(
            GameController c, Transform parent, string name, float anchorY,
            int delta, string text, string dateZh, float width)
        {
            bool gain = delta >= 0;
            string mark = gain ? "\u2713" : "\u2717";
            string deltaText = (gain ? "+" : "") + delta;
            var row = UiKit.At(parent, name, 0.5f, anchorY, width, 40);
            UiKit.Text(row, "Mark", mark + " " + deltaText, 1.0f,
                gain ? InkPalette.Good : InkPalette.Bad,
                TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI body = UiKit.Text(row, "Body", text
                + (string.IsNullOrEmpty(dateZh) ? "" : "　·　" + dateZh), 1.0f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
            body.rectTransform.offsetMin = new Vector2(96, 0);
        }

        public static string EntryText(GameController c, AffinityEntry entry)
        {
            string text = c.L10n.Tr(entry.SourceKey);
            if (entry.SourceParam != null)
            {
                text = string.Format(text, c.L10n.Tr(entry.SourceParam));
            }
            return text;
        }

        public static string EntryDate(GameController c, AffinityEntry entry)
        {
            Lingyan.Core.Calendar.TangDate date =
                Lingyan.Core.Calendar.TangDate.FromStamp(entry.DateStamp);
            if (date == null) { return ""; }
            bool en = c.L10n.Locale == Locale.En;
            return en
                ? date.Era.Pinyin + " " + date.EraYear + "·" + date.Month + "·" + date.Day
                : Lingyan.Core.Calendar.ZhNumerals.MonthZh(date.Month)
                    + Lingyan.Core.Calendar.ZhNumerals.DayZh(date.Day);
        }

        /// <summary>
        /// 悬停浮签：名 + 好感 + 时下观感 + 最近数条账目（只读摘要）。
        /// 返回浮签根节点，调用方负责销毁。
        /// </summary>
        public static RectTransform BuildHoverTip(GameController c, RectTransform root, string npcId)
        {
            Data data = Fetch(c, npcId);

            RectTransform tip = UiKit.Rect(root, "NpcHoverTip",
                new Vector2(0.015f, 0.30f), new Vector2(0.315f, 0.86f),
                Vector2.zero, Vector2.zero);
            var bg = tip.gameObject.AddComponent<Image>();
            bg.color = new Color(0.055f, 0.05f, 0.04f, 0.86f);
            bg.raycastTarget = false;
            UiKit.Frame(tip, "Frame", InkPalette.Faint, 0.3f, 0f);

            UiKit.Text(UiKit.At(tip, "Name", 0.34f, 0.935f, 340, 44),
                "T", c.L10n.Tr(data.Schedule.NameKey), 1.15f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(tip, "Total", 0.82f, 0.93f, 180, 50),
                "T", c.L10n.Tr("npc.affinity") + " " + data.Total, 1.2f,
                TotalColor(data.Total), TextAlignmentOptions.MidlineRight);
            UiKit.Hairline(tip, "Rule", 0.5f, 0.875f, 480, 0.22f);

            float y = 0.81f;
            foreach (DynamicAffinity dyn in data.Dynamics)
            {
                EntryRow(c, tip, "Dyn", y, dyn.Delta,
                    string.Format(c.L10n.Tr(dyn.SourceKey), dyn.Param), "", 520);
                y -= 0.085f;
            }

            var recent = data.State.Ledger.AsEnumerable().Reverse().Take(5).ToList();
            if (recent.Count == 0 && data.Dynamics.Count == 0)
            {
                UiKit.Text(UiKit.At(tip, "Empty", 0.5f, y, 500, 40),
                    "T", c.L10n.Tr("npc.no_ledger"), 1.0f,
                    InkPalette.Faint, TextAlignmentOptions.MidlineLeft);
            }
            foreach (AffinityEntry entry in recent)
            {
                EntryRow(c, tip, "L", y, entry.Delta,
                    EntryText(c, entry), EntryDate(c, entry), 520);
                y -= 0.085f;
            }
            return tip;
        }
    }
}
