using System;
using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Localization;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using Lingyan.Core.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// NPC 互动屏：好感明细（可回溯账本 + 时下观感）与互动菜单
    /// （对话/送礼/打听/切磋/辱骂/偷窃；提亲、雇佣灰显待后续章节）。
    /// 互动结果显式落账：好感、名誉、钱、通缉一样不少。
    /// </summary>
    public static class NpcScreen
    {
        private static string _lastResultText;
        private static bool _giftMode;
        private static int _ledgerPage;
        private static readonly IRng Rng = new SystemRng();

        public static void Reset()
        {
            _lastResultText = null;
            _giftMode = false;
            _ledgerPage = 0;
        }

        /// <summary>供对决等外部流程回填结果行。</summary>
        public static void SetResult(string text)
        {
            _lastResultText = text;
        }

        public static void Build(GameController c, RectTransform root, string npcId)
        {
            SaveData save = c.ActiveSave;
            if (save == null || npcId == null)
            {
                c.GoMainMenu();
                return;
            }

            NpcPanelRenderer.Data data = NpcPanelRenderer.Fetch(c, npcId);
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            bool present = data.Schedule.At(save.Date.HourIndex).PlaceId != "west_market";

            RectTransform panel = UiKit.Rect(root, "NpcPanel",
                new Vector2(0.24f, 0.06f), new Vector2(0.76f, 0.94f),
                Vector2.zero, Vector2.zero);
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.055f, 0.05f, 0.04f, 0.90f);
            bg.raycastTarget = true;
            UiKit.Frame(panel, "Frame", InkPalette.Faint, 0.30f, 0f);
            UiKit.Frame(panel, "FrameInner", InkPalette.Faint, 0.12f, 6f);

            // 头部
            UiKit.Text(UiKit.At(panel, "Name", 0.30f, 0.945f, 480, 52),
                "T", c.L10n.Tr(data.Schedule.NameKey), 1.5f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(panel, "Total", 0.80f, 0.94f, 300, 60),
                "T", c.L10n.Tr("npc.affinity") + " " + data.Total, 1.6f,
                NpcPanelRenderer.TotalColor(data.Total), TextAlignmentOptions.MidlineRight);
            UiKit.Hairline(panel, "HeadRule", 0.5f, 0.895f, 780, 0.22f);

            // 互动结果行
            if (_lastResultText != null)
            {
                UiKit.Text(UiKit.At(panel, "Result", 0.5f, 0.855f, 860, 44),
                    "T", _lastResultText, 1.0f, InkPalette.Seal,
                    TextAlignmentOptions.Center);
            }

            if (_giftMode)
            {
                BuildGiftList(c, panel, data, save, date);
                return;
            }

            // 时下观感（动态项）
            float y = 0.795f;
            UiKit.Text(UiKit.At(panel, "DynHead", 0.5f, y, 500, 40),
                "T", c.L10n.Tr("npc.dyn_head"), 1.05f,
                InkPalette.Seal, TextAlignmentOptions.Center);
            y -= 0.055f;
            if (data.Dynamics.Count == 0)
            {
                UiKit.Text(UiKit.At(panel, "DynNone", 0.5f, y, 700, 36),
                    "T", "—", 1.0f, InkPalette.Faint, TextAlignmentOptions.Center);
                y -= 0.055f;
            }
            foreach (DynamicAffinity dyn in data.Dynamics)
            {
                NpcPanelRenderer.EntryRow(c, panel, "Dyn", y, dyn.Delta,
                    string.Format(c.L10n.Tr(dyn.SourceKey), dyn.Param), "", 780);
                y -= 0.052f;
            }

            // 好感始末（账本，倒序分页）
            y -= 0.02f;
            UiKit.Text(UiKit.At(panel, "LedgerHead", 0.5f, y, 500, 40),
                "T", c.L10n.Tr("npc.ledger_head"), 1.05f,
                InkPalette.Seal, TextAlignmentOptions.Center);
            y -= 0.055f;

            const int pageSize = 6;
            var reversed = data.State.Ledger.AsEnumerable().Reverse().ToList();
            int pageCount = Math.Max(1, (reversed.Count + pageSize - 1) / pageSize);
            _ledgerPage = Math.Min(_ledgerPage, pageCount - 1);
            var page = reversed.Skip(_ledgerPage * pageSize).Take(pageSize).ToList();

            if (page.Count == 0)
            {
                UiKit.Text(UiKit.At(panel, "LedgerNone", 0.5f, y, 700, 36),
                    "T", c.L10n.Tr("npc.no_ledger"), 1.0f,
                    InkPalette.Faint, TextAlignmentOptions.Center);
                y -= 0.052f;
            }
            foreach (AffinityEntry entry in page)
            {
                NpcPanelRenderer.EntryRow(c, panel, "L", y, entry.Delta,
                    NpcPanelRenderer.EntryText(c, entry),
                    NpcPanelRenderer.EntryDate(c, entry), 780);
                y -= 0.052f;
            }
            if (pageCount > 1)
            {
                UiKit.TextButton(UiKit.At(panel, "PgPrev", 0.38f, y, 70, 40),
                    "Btn", "\u2039", () => { _ledgerPage = Math.Max(0, _ledgerPage - 1); c.GoNpc(npcId); },
                    1.0f, _ledgerPage > 0);
                UiKit.Text(UiKit.At(panel, "PgNum", 0.5f, y, 120, 40),
                    "T", (_ledgerPage + 1) + " / " + pageCount, 1.0f,
                    InkPalette.Faint, TextAlignmentOptions.Center);
                UiKit.TextButton(UiKit.At(panel, "PgNext", 0.62f, y, 70, 40),
                    "Btn", "\u203a",
                    () => { _ledgerPage = Math.Min(pageCount - 1, _ledgerPage + 1); c.GoNpc(npcId); },
                    1.0f, _ledgerPage < pageCount - 1);
            }

            BuildActions(c, panel, data, save, date, present, npcId);

            UiKit.TextButton(UiKit.At(panel, "BtnClose", 0.5f, 0.045f, 300, 50),
                "Btn", c.L10n.Tr("creation.back"), () => { Reset(); c.GoWard(save); }, 1.05f);
        }

        private static void BuildActions(
            GameController c, RectTransform panel, NpcPanelRenderer.Data data,
            SaveData save, TangDate date, bool present, string npcId)
        {
            if (!present)
            {
                UiKit.Text(UiKit.At(panel, "Away", 0.5f, 0.155f, 800, 44),
                    "T", c.L10n.Tr("interact.away"), 1.05f,
                    InkPalette.Faint, TextAlignmentOptions.Center);
                return;
            }

            float rowY1 = 0.185f;
            float rowY2 = 0.115f;
            float[] xs = { 0.155f, 0.385f, 0.615f, 0.845f };

            Btn(c, panel, xs[0], rowY1, "interact.greet",
                () =>
                {
                    // 有对话树走树（阶段 4），无树退回寒暄
                    if (DialogueScreen.TryStart(c, npcId))
                    {
                        c.GoDialogue();
                        return;
                    }
                    Run(c, save, npcId, s => InteractionService.Greet(s, date));
                });
            Btn(c, panel, xs[1], rowY1, "interact.gift",
                () => { _giftMode = true; c.GoNpc(npcId); });
            Btn(c, panel, xs[2], rowY1, "interact.ask",
                () => RunAsk(c, save, npcId, data));
            Btn(c, panel, xs[3], rowY1, "interact.spar",
                () =>
                {
                    // 应战者进实时对决（阶段 7 内核）；不应战者拿婉拒文本
                    if (data.Profile.Prowess != null)
                    {
                        DuelScreen.Start(c, npcId);
                        return;
                    }
                    Run(c, save, npcId, s => InteractionService.Spar(
                        data.Profile, s, save.Attributes.Strength, save.Attributes.Stamina,
                        Rng, date));
                });

            Btn(c, panel, xs[0], rowY2, "interact.insult",
                () => Run(c, save, npcId, s => InteractionService.Insult(data.Profile, s, date)));
            Btn(c, panel, xs[1], rowY2, "interact.steal",
                () => Run(c, save, npcId, s =>
                {
                    string place = data.Schedule.At(save.Date.HourIndex).PlaceId;
                    bool witnesses = InteractionService.WitnessesAt(
                        place, npcId, save.Date.HourIndex);
                    return InteractionService.Steal(
                        data.Profile, s, save.Attributes.Wisdom, witnesses, Rng, date);
                }));
            Btn(c, panel, xs[2], rowY2, "interact.propose", null, false);
            Btn(c, panel, xs[3], rowY2, "interact.hire", null, false);
        }

        private static void Btn(
            GameController c, RectTransform panel, float x, float y,
            string key, Action onClick, bool enabled = true)
        {
            UiKit.TextButton(UiKit.At(panel, "Act_" + key, x, y, 210, 50),
                "Btn", c.L10n.Tr(key) + (enabled ? "" : " " + c.L10n.Tr("interact.locked")),
                onClick, 1.0f, enabled);
        }

        /// <summary>统一结算：好感入档、名誉/钱/通缉走 OutcomeApplier 单一口径，然后重建屏。</summary>
        private static void Run(
            GameController c, SaveData save, string npcId,
            Func<NpcState, InteractionResult> action)
        {
            NpcState state = NpcStateStore.Load(save, npcId);
            InteractionResult result = action(state);
            NpcStateStore.Store(save, npcId, state);

            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
            OutcomeApplier.ApplyInteraction(save, result, date);

            string text = c.L10n.Tr(result.TextKey);
            if (result.TextParam != null)
            {
                string param = result.TextParam;
                if (c.L10n.Catalog.Has(param))
                {
                    param = c.L10n.Tr(param);
                }
                else if (c.L10n.Catalog.Has("place." + param))
                {
                    param = c.L10n.Tr("place." + param);
                }
                text = string.Format(text, param);
            }
            _lastResultText = text;
            c.GoNpc(npcId);
        }

        /// <summary>打听：问其余坊民之一的去向，顺带一条传闻。</summary>
        private static void RunAsk(
            GameController c, SaveData save, string npcId, NpcPanelRenderer.Data data)
        {
            string aboutId = SampleWard.Npcs
                .First(n => n.NpcId != npcId).NpcId;
            InteractionResult result = InteractionService.AskAround(
                data.Total, aboutId, save.Date.HourIndex, Rng);
            string text;
            if (result.Success)
            {
                string aboutName = c.L10n.Tr(
                    SampleWard.Npcs.First(n => n.NpcId == aboutId).NameKey);
                string place = c.L10n.Tr("place." + result.TextParam);
                text = aboutName + "？" + string.Format(c.L10n.Tr(result.TextKey), place)
                    + "\n" + c.L10n.Tr("npc.rumor_head") + "：" + c.L10n.Tr(
                        InteractionService.RumorKey(Rng));
            }
            else
            {
                text = c.L10n.Tr(result.TextKey);
            }
            _lastResultText = text;
            c.GoNpc(npcId);
        }

        private static void BuildGiftList(
            GameController c, RectTransform panel, NpcPanelRenderer.Data data,
            SaveData save, TangDate date)
        {
            UiKit.Text(UiKit.At(panel, "GiftHead", 0.5f, 0.79f, 700, 44),
                "T", c.L10n.Tr("interact.gift"), 1.1f,
                InkPalette.Seal, TextAlignmentOptions.Center);

            // 礼从行囊出（v4 起）：只有囊中有货才可送，市集购置
            float y = 0.71f;
            foreach (GiftDef gift in GiftCatalog.All)
            {
                GiftDef captured = gift;
                int owned = Lingyan.Core.Economy.MarketService.CountOf(save, gift.Id);
                UiKit.TextButton(UiKit.At(panel, "Gift_" + gift.Id, 0.5f, y, 820, 48),
                    "Btn",
                    c.L10n.Tr(gift.NameKey) + "　—　" + c.L10n.TrF("market.owned", owned),
                    () =>
                    {
                        _giftMode = false;
                        Run(c, save, data.Profile.NpcId, s =>
                        {
                            InteractionResult result = InteractionService.Gift(
                                data.Profile, s, captured,
                                Lingyan.Core.Economy.MarketService.CountOf(save, captured.Id),
                                date);
                            if (result.Success)
                            {
                                Lingyan.Core.Economy.MarketService.TakeOne(save, captured.Id);
                            }
                            return result;
                        });
                    },
                    1.0f, owned > 0);
                y -= 0.075f;
            }
            UiKit.Text(UiKit.At(panel, "GiftHint", 0.5f, 0.175f, 820, 40),
                "T", c.L10n.Tr("npc.gift_hint"), 0.92f,
                InkPalette.Faint, TextAlignmentOptions.Center);
            UiKit.TextButton(UiKit.At(panel, "GiftBack", 0.5f, 0.10f, 300, 50),
                "Btn", c.L10n.Tr("creation.back"),
                () => { _giftMode = false; c.GoNpc(data.Profile.NpcId); }, 1.05f);
        }
    }
}
