using System;
using System.IO;
using System.Linq;
using Lingyan.Core.Calendar;
using Lingyan.Core.Characters;
using Lingyan.Core.Dialogue;
using Lingyan.Core.Localization;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class DialogueTests
    {
        private static string HuanJson()
        {
            return File.ReadAllText(
                Path.Combine(TestData.DataDir, "dialogues", "huan_fuzi.json"));
        }

        private static SaveData NewSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(draft, DateTime.UtcNow);
        }

        private static DialogueRunner Runner(SaveData save)
        {
            return new DialogueRunner(
                DialogueTree.Parse(HuanJson()), save,
                NpcProfiles.Get("huan_fuzi"), NpcArchetype.Commoner);
        }

        private static TangDate Date(SaveData save)
        {
            return new TangDate(save.Date.EraId, save.Date.EraYear,
                save.Date.Month, save.Date.Day, save.Date.HourIndex);
        }

        [Test]
        public void Tree_ParsesAndValidates()
        {
            DialogueTree tree = DialogueTree.Parse(HuanJson());
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            var violations = DialogueValidator.Check(tree, catalog);
            Assert.That(violations, Is.Empty,
                "对话树违规:\n" + string.Join("\n", violations));
            Assert.That(tree.NpcId, Is.EqualTo("huan_fuzi"));
        }

        [Test]
        public void AllTrees_ParseValidate_NpcMatchesFilename_HasProfile()
        {
            // 量产纪律：dialogues 目录下每棵树自动巡检，新树入库即被咬住
            string dir = Path.Combine(TestData.DataDir, "dialogues");
            string[] files = Directory.GetFiles(dir, "*.json");
            Assert.That(files.Length, Is.GreaterThanOrEqualTo(3),
                "至少桓夫子/康三/郑五三棵树");

            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            var problems = new System.Collections.Generic.List<string>();
            foreach (string file in files)
            {
                string expectNpc = Path.GetFileNameWithoutExtension(file);
                DialogueTree tree = DialogueTree.Parse(File.ReadAllText(file));
                if (tree.NpcId != expectNpc)
                {
                    problems.Add(file + ": npc=" + tree.NpcId + " 与文件名不符");
                }
                if (NpcProfiles.Get(tree.NpcId) == null)
                {
                    problems.Add(file + ": npc 无社交档案");
                }
                foreach (string violation in DialogueValidator.Check(tree, catalog))
                {
                    problems.Add(file + ": " + violation);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void KangTree_TeaMoney_GatesCaseClueTalk()
        {
            SaveData save = NewSave();
            save.StoryFlags["heard_silk_case"] = true;
            string json = File.ReadAllText(
                Path.Combine(TestData.DataDir, "dialogues", "kang_san.json"));
            var runner = new DialogueRunner(DialogueTree.Parse(json), save,
                NpcProfiles.Get("kang_san"), NpcArchetype.Commoner);

            var caseChoice = runner.AvailableChoices()
                .FirstOrDefault(ch => ch.TextKey == "dlg.kang.c_case");
            Assert.That(caseChoice, Is.Not.Null, "听过丝帛案才见此问");
            runner.Choose(caseChoice, Date(save));

            long before = save.MoneyWen;
            var pay = runner.AvailableChoices()
                .FirstOrDefault(ch => ch.TextKey == "dlg.kang.c_pay_tea");
            Assert.That(pay, Is.Not.Null, "6000 文付得起 50 文茶钱");
            runner.Choose(pay, Date(save));
            Assert.That(save.MoneyWen, Is.EqualTo(before - 50), "茶钱落账");
            Assert.That(save.StoryFlags["kang_told_case"], Is.True, "线索话头旗标入档");
            Assert.That(runner.Current.TextKey, Is.EqualTo("dlg.kang.case_talk"));

            // 再进树：已说过就不再有此问（FlagNotSet 咬住）
            var again = new DialogueRunner(DialogueTree.Parse(json), save,
                NpcProfiles.Get("kang_san"), NpcArchetype.Commoner);
            Assert.That(again.AvailableChoices()
                .Any(ch => ch.TextKey == "dlg.kang.c_case"), Is.False);
        }

        [Test]
        public void ZhengTree_TauntCostsReputation_PatrolNeedsAffinity()
        {
            SaveData save = NewSave();
            string json = File.ReadAllText(
                Path.Combine(TestData.DataDir, "dialogues", "zheng_wu.json"));
            var runner = new DialogueRunner(DialogueTree.Parse(json), save,
                NpcProfiles.Get("zheng_wu"), NpcArchetype.Official);

            // 郑五基准好感 35 < 45：巡夜话头不开
            Assert.That(runner.AvailableChoices()
                .Any(ch => ch.TextKey == "dlg.zheng.c_patrol"), Is.False,
                "交情不到，武侯不吐口");

            // 挑衅到底：好感 -8、官声 -1
            var taunt = runner.AvailableChoices()
                .First(ch => ch.TextKey == "dlg.zheng.c_taunt");
            runner.Choose(taunt, Date(save));
            int guanShengBefore = save.Reputation.GuanSheng;
            var press = runner.AvailableChoices()
                .First(ch => ch.TextKey == "dlg.zheng.c_taunt_press");
            runner.Choose(press, Date(save));
            Assert.That(save.Reputation.GuanSheng, Is.EqualTo(guanShengBefore - 1),
                "顶撞武侯掉官声");
        }

        [Test]
        public void Validator_CatchesDanglingGoto_MutationCheck()
        {
            DialogueTree tree = DialogueTree.Parse(HuanJson());
            tree.Lines[0].Choices[0].GotoId = "no_such_node";
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            var violations = DialogueValidator.Check(tree, catalog);
            Assert.That(violations.Any(v => v.Contains("goto 悬空")), Is.True,
                "悬空跳转必须被校验器咬住");
        }

        [Test]
        public void AffinityGate_HidesAndRevealsBorrowChoice()
        {
            SaveData save = NewSave();
            DialogueRunner runner = Runner(save);
            // 明镜起始：基准 45 + 动态项（民望 20 ≤25 → 民望扫地 -5）= 40
            Assert.That(runner.AvailableChoices().Any(
                c => c.TextKey == "dlg.huan.c_borrow"), Is.False,
                "好感不足 55，借书选项不可见");

            // 送书 +8、送绢 +8、初识 +2 → 40+18 = 58 ≥ 55
            NpcState state = NpcStateStore.Load(save, "huan_fuzi");
            state.Add(+8, "affinity.src.gift_liked", "s");
            state.Add(+8, "affinity.src.gift_liked", "s");
            state.Add(+2, "affinity.src.first_meet", "s");
            NpcStateStore.Store(save, "huan_fuzi", state);

            DialogueRunner runner2 = Runner(save);
            Assert.That(runner2.AvailableChoices().Any(
                c => c.TextKey == "dlg.huan.c_borrow"), Is.True,
                "好感够 55 后借书选项出现");
        }

        [Test]
        public void Choice_Effects_PersistToStoryFlags()
        {
            SaveData save = NewSave();
            DialogueRunner runner = Runner(save);

            // greet → news
            DialogueChoice news = runner.AvailableChoices()
                .First(c => c.TextKey == "dlg.huan.c_news");
            runner.Choose(news, Date(save));
            Assert.That(runner.Current.Id, Is.EqualTo("news"));

            // news 里能问丝帛案（旗标未立）
            DialogueChoice ask = runner.AvailableChoices()
                .First(c => c.TextKey == "dlg.huan.c_case_ask");
            runner.Choose(ask, Date(save));
            Assert.That(runner.Current.Id, Is.EqualTo("case"));

            // 记下 → SetFlag heard_silk_case，对话结束
            DialogueChoice noted = runner.AvailableChoices()
                .First(c => c.TextKey == "dlg.huan.c_case_noted");
            runner.Choose(noted, Date(save));
            Assert.That(runner.Finished, Is.True);
            Assert.That(save.StoryFlags["heard_silk_case"], Is.True,
                "分支后果持久化：旗标必须落进存档");

            // 再进对话：news 里"初问"选项因 FlagNotSet 消失，"再问"因 FlagSet 出现
            DialogueRunner again = Runner(save);
            DialogueChoice news2 = again.AvailableChoices()
                .First(c => c.TextKey == "dlg.huan.c_news");
            again.Choose(news2, Date(save));
            Assert.That(again.AvailableChoices().Any(
                c => c.TextKey == "dlg.huan.c_case_ask"), Is.False,
                "问过的事不再作为新话头——后续剧情读得到旗标");
            Assert.That(again.AvailableChoices().Any(
                c => c.TextKey == "dlg.huan.c_case_more"), Is.True);
        }

        [Test]
        public void AffinityEffect_LandsInLedger()
        {
            SaveData save = NewSave();
            DialogueRunner runner = Runner(save);
            DialogueChoice askBook = runner.AvailableChoices()
                .First(c => c.TextKey == "dlg.huan.c_ask_book");
            runner.Choose(askBook, Date(save));

            NpcState state = NpcStateStore.Load(save, "huan_fuzi");
            Assert.That(state.Ledger.Count, Is.EqualTo(1));
            Assert.That(state.Ledger[0].Delta, Is.EqualTo(2));
            Assert.That(state.Ledger[0].SourceKey, Is.EqualTo("affinity.src.conversation"));
        }

        [Test]
        public void HourBetween_WrapsMidnight()
        {
            SaveData save = NewSave();
            var condition = new DialogueCondition
            {
                Kind = ConditionKind.HourBetween,
                Value = 10,
                Value2 = 3
            };
            var choice = new DialogueChoice();
            choice.Conditions.Add(condition);
            var tree = DialogueTree.Parse(HuanJson());
            var runner = new DialogueRunner(tree, save,
                NpcProfiles.Get("huan_fuzi"), NpcArchetype.Commoner);

            save.Date.HourIndex = 0; // 子时 ∈ [戌, 寅)
            Assert.That(runner.Passes(choice), Is.True);
            save.Date.HourIndex = 5; // 巳时 ∉
            Assert.That(runner.Passes(choice), Is.False);
        }

        [Test]
        public void OutcomeApplier_MoneyNeverNegative_WantedFloorsAtZero()
        {
            SaveData save = NewSave();
            OutcomeApplier.ApplyMoney(save, -999_999);
            Assert.That(save.MoneyWen, Is.EqualTo(0), "钱扣穿夹到零");
            OutcomeApplier.ApplyWanted(save, -5);
            Assert.That(save.WantedLevel, Is.EqualTo(0));
        }
    }
}
