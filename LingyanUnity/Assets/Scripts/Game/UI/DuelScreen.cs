using Lingyan.Core.Calendar;
using Lingyan.Core.Combat;
using Lingyan.Core.Localization;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using Lingyan.Core.World;
using Lingyan.Game.World3D;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lingyan.Game.UI
{
    /// <summary>
    /// 切磋对决屏：坊景中央开打，HUD 双方血/耐力条每帧刷新。
    /// 结算沿用切磋的社交语义：胜涨江湖名望，负掉一点但对方好感反升。
    /// </summary>
    public static class DuelScreen
    {
        private static GameObject _playerBody;
        private static GameObject _rivalBody;
        private static DuelController _controller;
        private static string _npcId;
        private static bool _running;

        public static void Start(GameController c, string npcId)
        {
            SaveData save = c.ActiveSave;
            NpcProfile profile = NpcProfiles.Get(npcId);
            if (profile?.Prowess == null) { return; }
            _npcId = npcId;

            Vector3 arena = new Vector3(0f, 0f, -6f);

            // 应战者以对决化身上场，坊景里他的标记先隐去（免"双人同框"）
            c.Ward3D.SetNpcMarkerVisible(npcId, false);

            // 像素唐人上场：玩家青绿袍、对手赭袍
            _playerBody = PixelPerson.Build(
                c.Ward3D.Root.transform, "DuelPlayer", new Color(0.30f, 0.42f, 0.30f));
            _rivalBody = PixelPerson.Build(
                c.Ward3D.Root.transform, "DuelRival", new Color(0.55f, 0.38f, 0.22f));

            _controller = c.Ward3D.Root.AddComponent<DuelController>();
            _controller.Begin(
                _playerBody.transform, _rivalBody.transform, arena,
                save.Attributes.Health, save.Attributes.Stamina, save.Attributes.Strength,
                profile.Prowess.Value,
                win => Finish(c, win));

            // 相机拉近对决场
            c.Orbit.Target = arena + new Vector3(0f, 1.2f, 0f);
            c.Orbit.Distance = 13f;
            c.Orbit.Pitch = 32f;
            c.Orbit.ApplyTransform();

            _running = true;
            c.GoDuel();
        }

        public static void Build(GameController c, RectTransform root)
        {
            if (!_running || _controller == null)
            {
                c.GoWard(c.ActiveSave);
                return;
            }

            NpcScheduleDef schedule = SampleWard.Npcs.First(n => n.NpcId == _npcId);

            // 顶部对阵条
            RectTransform top = UiKit.Rect(root, "DuelTop",
                new Vector2(0f, 0.90f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var bg = top.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.045f, 0.035f, 0.60f);
            bg.raycastTarget = false;

            UiKit.Text(UiKit.At(top, "PName", 0.12f, 0.65f, 360, 40),
                "T", c.ActiveSave.CharacterName, 1.05f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineLeft);
            UiKit.Text(UiKit.At(top, "RName", 0.88f, 0.65f, 360, 40),
                "T", c.L10n.Tr(schedule.NameKey), 1.05f,
                InkPalette.PaperText, TextAlignmentOptions.MidlineRight);

            var hud = root.gameObject.AddComponent<DuelHud>();
            hud.Bind(_controller,
                Bar(top, "PHp", 0.12f, 0.38f, InkPalette.Bad),
                Bar(top, "PSt", 0.12f, 0.16f, InkPalette.Good),
                Bar(top, "RHp", 0.88f, 0.38f, InkPalette.Bad),
                Bar(top, "RSt", 0.88f, 0.16f, InkPalette.Good));

            // 暗底衬条：淡字压浅色地面对比不足（第八轮目检观察）
            RectTransform controlsBar = UiKit.At(root, "ControlsBar", 0.5f, 0.06f, 1240, 52);
            var controlsBg = controlsBar.gameObject.AddComponent<Image>();
            controlsBg.color = new Color(0.05f, 0.045f, 0.035f, 0.60f);
            controlsBg.raycastTarget = false;
            UiKit.Text(controlsBar, "Controls", c.L10n.Tr("duel.controls"), 1.0f,
                InkPalette.PaperText, TextAlignmentOptions.Center);
        }

        private static RectTransform Bar(
            RectTransform parent, string name, float x, float y, Color color)
        {
            RectTransform box = UiKit.At(parent, name, x, y, 360, 14);
            UiKit.Frame(box, "Rim", InkPalette.Faint, 0.35f, 0f);
            var fill = UiKit.Rect(box, "Fill",
                new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            fill.sizeDelta = new Vector2(356f, -4f);
            fill.anchoredPosition = new Vector2(180f, 0f);
            var image = fill.gameObject.AddComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 0.8f);
            image.raycastTarget = false;
            return fill;
        }

        private static void Finish(GameController c, bool win)
        {
            SaveData save = c.ActiveSave;
            var date = new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);

            NpcState state = NpcStateStore.Load(save, _npcId);
            if (win)
            {
                OutcomeApplier.ApplyReputation(
                    save, ReputationTrack.JiangHu, +4, "rep.src.spar_win", date);
                state.Add(+1, "affinity.src.spar", date.ToStamp());
                // 军线入仕看武名：切磋胜场入档（CareerEntryService 叙迁门槛）
                save.Counters.TryGetValue("spar_wins", out int wins);
                save.Counters["spar_wins"] = wins + 1;
            }
            else
            {
                OutcomeApplier.ApplyReputation(
                    save, ReputationTrack.JiangHu, -2, "rep.src.spar_lose", date);
                state.Add(+4, "affinity.src.spar_respect", date.ToStamp());
            }
            NpcStateStore.Store(save, _npcId, state);
            Lingyan.Core.Terminology.CodexService.OnEvent(
                save, Lingyan.Core.Terminology.CodexEvent.Sparred);

            Cleanup(c);
            NpcScreen.SetResult(c.L10n.Tr(win
                ? "interact.result.spar_win" : "interact.result.spar_lose"));
            c.GoNpc(_npcId);
        }

        public static void Cleanup(GameController c)
        {
            _running = false;
            if (_controller != null) { Object.Destroy(_controller); _controller = null; }
            if (_playerBody != null) { Object.Destroy(_playerBody); _playerBody = null; }
            if (_rivalBody != null) { Object.Destroy(_rivalBody); _rivalBody = null; }
            if (_npcId != null && c.Ward3D != null)
            {
                c.Ward3D.SetNpcMarkerVisible(_npcId, true); // 对决散场，坊景标记归位
            }
        }
    }

    /// <summary>每帧把内核数值刷到条宽。</summary>
    public sealed class DuelHud : MonoBehaviour
    {
        private DuelController _duel;
        private RectTransform _pHp;
        private RectTransform _pSt;
        private RectTransform _rHp;
        private RectTransform _rSt;

        public void Bind(DuelController duel,
            RectTransform pHp, RectTransform pSt, RectTransform rHp, RectTransform rSt)
        {
            _duel = duel;
            _pHp = pHp;
            _pSt = pSt;
            _rHp = rHp;
            _rSt = rSt;
        }

        private void Update()
        {
            if (_duel == null || _duel.Player == null) { return; }
            Set(_pHp, (float)_duel.Player.Hp / _duel.Player.MaxHp);
            Set(_pSt, (float)_duel.Player.Stamina / _duel.Player.MaxStamina);
            Set(_rHp, (float)_duel.Rival.Hp / _duel.Rival.MaxHp);
            Set(_rSt, (float)_duel.Rival.Stamina / _duel.Rival.MaxStamina);
        }

        private static void Set(RectTransform fill, float ratio)
        {
            if (fill == null) { return; }
            float clamped = Mathf.Clamp01(ratio);
            fill.sizeDelta = new Vector2(356f * clamped, -4f);
            fill.anchoredPosition = new Vector2(2f + 356f * clamped / 2f, 0f);
        }
    }
}
