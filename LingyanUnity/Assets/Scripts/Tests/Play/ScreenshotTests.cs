using System;
using System.Collections;
using System.IO;
using Lingyan.Core.Characters;
using Lingyan.Core.Saves;
using Lingyan.Game;
using Lingyan.Game.Screenshot;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lingyan.PlayTests
{
    /// <summary>
    /// 全屏巡游截图（规格第十四节：自动化验收截图，一条命令、可比较）。
    /// 输出 artifacts/unity-shots/*.png；断言文件真实生成且非空壳
    /// （渲染真的发生了），画面质量由人与指标脚本复核。
    /// 需要图形设备：CI 里用 xvfb 跑，不加 -nographics。
    /// </summary>
    public class ScreenshotTests
    {
        private const int MinBytes = 20_000;

        [UnityTest]
        public IEnumerator CaptureAllScreens()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            yield return null;
            yield return null;

            GameController c = UnityEngine.Object.FindFirstObjectByType<GameController>();
            Assert.That(c, Is.Not.Null, "GameController 未自举");

            // 主菜单
            yield return Snap(c, "01_menu");

            // 建角
            c.GoCreation();
            yield return null;
            yield return Snap(c, "02_creation");

            // 书房（明镜新档）
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            SaveData save = SaveFactory.NewGame(draft, DateTime.UtcNow);
            c.GoStudy(save);
            yield return null;
            yield return Snap(c, "03_study");

            // 坊景 · 巳时全景（自南望北：宅院、井亭、主巷同框）
            c.GoWard(save);
            yield return null;
            yield return null;
            SetRig(c, new Vector3(0f, 1.2f, 3f), yaw: -14f, pitch: 42f, distance: 34f);
            yield return Snap(c, "04_ward_noon_wide");

            // 坊景 · 南门楼近景（看鸱尾与门扇）
            SetRig(c, new Vector3(0f, 2.5f, -19f), yaw: 197f, pitch: 22f, distance: 15f);
            yield return Snap(c, "05_ward_gate");

            // 坊景 · 桓宅近景（看铺作层、下昂、直棂窗）
            SetRig(c, new Vector3(-12f, 2.6f, 7f), yaw: 160f, pitch: 24f, distance: 15f);
            yield return Snap(c, "06_ward_house");

            // 坊景 · 子夜（月光、闭门、武侯巡街）
            save.Date.HourIndex = 0;
            c.GoWard(save);
            yield return null;
            SetRig(c, new Vector3(0f, 1.2f, 0f), yaw: -170f, pitch: 36f, distance: 27f);
            yield return Snap(c, "07_ward_midnight");

            // 步态帧证据（第十四轮附加，不在 17 张正编内）：
            // 先把郑五压回巷心 (0,0,0)，回巳时触发 MoveTo 武侯铺 (8,-12.6) 全程约 15m；
            // 相机压近路径首段。步态 0.18s 交替、软渲染帧时抖动大，
            // 连拍三张各隔约一帧期，保证必有相邻两张腿型异相，位移亦是实走证据。
            GameObject zhengWu = GameObject.Find("Npc_zheng_wu");
            Assert.That(zhengWu, Is.Not.Null, "郑五标记未找到");
            zhengWu.GetComponent<Lingyan.Game.World3D.PixelWalker>()
                .Snap(new Vector3(0f, 0f, 0f));
            save.Date.HourIndex = 5;
            c.GoWard(save);
            yield return null;
            SetRig(c, new Vector3(0.5f, 0.9f, -0.8f), yaw: -160f, pitch: 16f, distance: 5f);
            yield return new WaitForSeconds(0.45f);
            yield return Snap(c, "walk_a");
            yield return new WaitForSeconds(0.19f);
            yield return Snap(c, "walk_b");
            yield return new WaitForSeconds(0.19f);
            yield return Snap(c, "walk_c");

            // 设置
            c.GoSettings();
            yield return null;
            yield return Snap(c, "08_settings");

            // NPC 面板（先制造几笔往来，账本非空：寒暄+送酒）
            // 第十三轮改拍康三：雇佣活钮只在可雇者（HireWageWen=500）面板可见，
            // 桓夫子不可雇灰注属设计，拍他验证不了雇佣实装。
            // 卯时=3：康三在南门候门（4–9 在西市＝不在场只渲「不在」行）。
            save.Date.HourIndex = 3;
            var date = new Lingyan.Core.Calendar.TangDate(
                save.Date.EraId, save.Date.EraYear, save.Date.Month,
                save.Date.Day, save.Date.HourIndex);
            Lingyan.Core.Social.NpcState state =
                Lingyan.Core.Social.NpcStateStore.Load(save, "kang_san");
            Lingyan.Core.Social.InteractionService.Greet(state, date);
            save.Inventory["gift_jiu"] = 1; // 礼从行囊出（v4）：先备一坛酒（康三嗜酒食）
            Lingyan.Core.Social.InteractionService.Gift(
                Lingyan.Core.Social.NpcProfiles.Get("kang_san"), state,
                Lingyan.Core.Social.GiftCatalog.Get("gift_jiu"),
                Lingyan.Core.Economy.MarketService.CountOf(save, "gift_jiu"), date);
            Lingyan.Core.Economy.MarketService.TakeOne(save, "gift_jiu");
            Lingyan.Core.Social.NpcStateStore.Store(save, "kang_san", state);
            c.GoWard(save);
            yield return null;
            c.GoNpc("kang_san");
            yield return null;
            yield return Snap(c, "09_npc_panel");
            save.Date.HourIndex = 5;

            // 对话屏（桓夫子对话树入口）
            bool started = Lingyan.Game.UI.DialogueScreen.TryStart(c, "huan_fuzi");
            Assert.That(started, Is.True, "桓夫子应有对话树");
            c.GoDialogue();
            yield return null;
            yield return Snap(c, "10_dialogue");

            // 线索板（接案 + 两条线索 + 一条推论）
            save.StoryFlags["heard_silk_case"] = true;
            var caseNow = new Lingyan.Core.Calendar.TangDate(
                save.Date.EraId, save.Date.EraYear, save.Date.Month,
                save.Date.Day, save.Date.HourIndex);
            SaveCaseState caseState = Lingyan.Core.Cases.CaseService.Open(
                save, Lingyan.Core.Cases.SilkCase.Def, caseNow);
            Lingyan.Core.Cases.CaseService.Discover(
                caseState, Lingyan.Core.Cases.SilkCase.Def, "permit");
            Lingyan.Core.Cases.CaseService.Discover(
                caseState, Lingyan.Core.Cases.SilkCase.Def, "patrol_log");
            Lingyan.Core.Cases.CaseService.Combine(
                caseState, Lingyan.Core.Cases.SilkCase.Def, "permit", "patrol_log");
            Lingyan.Game.UI.CaseScreen.Reset();
            c.GoCase();
            yield return null;
            yield return Snap(c, "11_case_board");

            // 对决屏（郑五应战；开场站位+满条 HUD）
            c.GoWard(save);
            yield return null;
            Lingyan.Game.UI.DuelScreen.Start(c, "zheng_wu");
            yield return null;
            yield return null;
            yield return Snap(c, "12_duel");
            Lingyan.Game.UI.DuelScreen.Cleanup(c);

            // 典籍（营造类：进坊后鸱尾等已解锁，详情展示复核台账）
            // 注释的取景意图靠真点按钮兑现：Reset 默认落在职官类首页，
            // 该页 9 条全锁，拍不到「解锁词条 + 详情」两种样式（第十一轮发现的编排脱节）。
            Lingyan.Core.Terminology.CodexService.OnEvent(
                save, Lingyan.Core.Terminology.CodexEvent.Appointed);
            Lingyan.Game.UI.CodexScreen.Reset();
            c.GoCodex();
            yield return null;
            ClickButton("Cat_architecture");
            yield return null;
            ClickButton("E_chiwei");
            yield return null;
            yield return Snap(c, "13_codex");

            // 议亲（授个官、给足聘财，看四问全绿与六礼进度）
            save.Offices.ZhiShiId = "xian_wei";
            save.Offices.SanGuanId = "jiangshi_lang";
            save.MoneyWen = 100_000;
            Lingyan.Game.UI.MarriageScreen.Reset();
            c.GoMarriage();
            yield return null;
            yield return Snap(c, "14_marriage");

            // 市集（午时开市；真按一次「买」，看结果行与囊中件数同框）
            save.Date.HourIndex = 6;
            Lingyan.Game.UI.MarketScreen.Reset();
            c.GoMarket();
            yield return null;
            ClickButton("Buy_gift_jiu");
            yield return null;
            yield return Snap(c, "15_market");

            // 结局卷轴（挂冠致仕：从九品下县尉+已成家 → 薄宦萧然档，回顾行齐）
            save.Marriage.Married = true;
            c.GoEnding();
            yield return null;
            yield return Snap(c, "16_ending");

            // 枯井案（第二案：具结丝帛案后接旧案，验流转与新线索板）
            SaveCaseState silk = save.Cases[Lingyan.Core.Cases.SilkCase.CaseId];
            if (silk.Accused == null)
            {
                Lingyan.Core.Cases.CaseService.Accuse(
                    silk, Lingyan.Core.Cases.SilkCase.Def, "bookkeeper",
                    Lingyan.Core.Cases.AccuseMethod.Forced, witnessTalked: false);
            }
            var wellNow = new Lingyan.Core.Calendar.TangDate(
                save.Date.EraId, save.Date.EraYear, save.Date.Month,
                save.Date.Day, save.Date.HourIndex);
            SaveCaseState well = Lingyan.Core.Cases.CaseService.Open(
                save, Lingyan.Core.Cases.WellCase.Def, wellNow);
            Lingyan.Core.Cases.CaseService.Discover(
                well, Lingyan.Core.Cases.WellCase.Def, "bone_belt");
            Lingyan.Core.Cases.CaseService.Discover(
                well, Lingyan.Core.Cases.WellCase.Def, "missing_roll");
            Lingyan.Core.Cases.CaseService.Combine(
                well, Lingyan.Core.Cases.WellCase.Def, "bone_belt", "missing_roll");
            Lingyan.Game.UI.CaseScreen.Reset();
            c.GoCase();
            yield return null;
            yield return Snap(c, "17_case2");

            // 铜匦四匦面板（书房动作列切换，真点按钮）
            c.GoStudy(save);
            yield return null;
            ClickButton("BtnTongGui");
            yield return null;
            yield return Snap(c, "18_tonggui");

            // 军线书房（戍卒任队正·秋七月：防秋点兵在列，勋轨待积转）
            CharacterDraft milDraft = CharacterCreationRules.NewDraft(ProtagonistId.ShuZu);
            SaveData milSave = SaveFactory.NewGame(milDraft, DateTime.UtcNow);
            milSave.Offices.ZhiShiId = "dui_zheng";
            milSave.Offices.SanGuanId = "renyong_fuwei"; // 正九品下武散，与队正同品
            milSave.Date.Month = 7;
            c.GoStudy(milSave);
            yield return null;
            yield return Snap(c, "19_mil_study");

            // 商线书房（胡商·遣商队在列，市籍虚衔态）
            CharacterDraft tradeDraft = CharacterCreationRules.NewDraft(ProtagonistId.HuShang);
            SaveData tradeSave = SaveFactory.NewGame(tradeDraft, DateTime.UtcNow);
            c.GoStudy(tradeSave);
            yield return null;
            yield return Snap(c, "20_trade_study");
        }

        /// <summary>按节点名点 UI 按钮（走 onClick，顺带验证按钮真挂了监听）。</summary>
        private static void ClickButton(string nodeName)
        {
            GameObject node = GameObject.Find(nodeName);
            Assert.That(node, Is.Not.Null, "按钮节点未找到: " + nodeName);
            var button = node.GetComponentInChildren<UnityEngine.UI.Button>();
            Assert.That(button, Is.Not.Null, "节点无 Button: " + nodeName);
            button.onClick.Invoke();
        }

        private static void SetRig(GameController c, Vector3 target, float yaw, float pitch, float distance)
        {
            c.Orbit.Target = target;
            c.Orbit.Yaw = yaw;
            c.Orbit.Pitch = pitch;
            c.Orbit.Distance = distance;
            c.Orbit.ApplyTransform();
        }

        private static IEnumerator Snap(GameController c, string name)
        {
            yield return null;
            string path = ScreenshotRig.Capture(c, name);
            var info = new FileInfo(path);
            Assert.That(info.Exists, Is.True, name + " 未落盘");
            Assert.That(info.Length, Is.GreaterThan(MinBytes),
                name + " 文件过小（" + info.Length + " B），疑似黑屏/未渲染");
        }
    }
}
