using System;
using System.Linq;
using Lingyan.Core.Characters;
using Lingyan.Core.Saves;
using Lingyan.Core.Terminology;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CodexTests
    {
        private static SaveData NewSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(draft, DateTime.UtcNow);
        }

        [Test]
        public void NewSave_KnowsCommonTerms()
        {
            SaveData save = NewSave();
            Assert.That(CodexService.IsUnlocked(save, "fang"), Is.True, "坊，开卷常识");
            Assert.That(CodexService.IsUnlocked(save, "guan_currency"), Is.True);
            Assert.That(CodexService.IsUnlocked(save, "chiwei"), Is.False, "鸱尾要进坊亲见");
        }

        [Test]
        public void Unlock_Idempotent_ReportsFreshOnly()
        {
            SaveData save = NewSave();
            var first = CodexService.Unlock(save, "chiwei", "dougong");
            Assert.That(first, Is.EquivalentTo(new[] { "chiwei", "dougong" }));
            var second = CodexService.Unlock(save, "chiwei", "xiaang");
            Assert.That(second, Is.EquivalentTo(new[] { "xiaang" }), "已解锁不重报");
            Assert.That(save.CodexUnlocked.Count(id => id == "chiwei"), Is.EqualTo(1));
        }

        [Test]
        public void EventTable_AllIdsExistInGlossary()
        {
            // 防拼错：触点表引用的每个词条 id 必须真实存在
            Glossary glossary = Glossary.Parse(TestData.GlossaryJson());
            foreach (CodexEvent codexEvent in Enum.GetValues(typeof(CodexEvent)))
            {
                SaveData save = NewSave();
                foreach (string id in CodexService.OnEvent(save, codexEvent))
                {
                    Assert.That(glossary.ById(id), Is.Not.Null,
                        codexEvent + " 触点引用了不存在的词条: " + id);
                }
            }
        }

        [Test]
        public void SpecNamedTerms_AreCoveredByEvents()
        {
            // 规格第十一节点名的四个词条：折冲都尉、帖经、六礼、鸱尾——
            // 鸱尾进坊解锁；其余三个属于对应内容线（投军/科举/结亲），
            // 词条已在库，触点随内容线接入（见 ROADMAP 阶段 10）。
            Glossary glossary = Glossary.Parse(TestData.GlossaryJson());
            Assert.That(glossary.ById("zhechong_duwei"), Is.Not.Null);
            Assert.That(glossary.ByZh("帖经"), Is.Not.Null);
            Assert.That(glossary.ByZh("六礼"), Is.Not.Null);

            SaveData save = NewSave();
            CodexService.OnEvent(save, CodexEvent.SawArchitecture);
            Assert.That(CodexService.IsUnlocked(save, "chiwei"), Is.True,
                "进坊亲见屋脊，鸱尾词条解锁");
        }

        [Test]
        public void Progress_CountsPerCategory()
        {
            SaveData save = NewSave();
            Glossary glossary = Glossary.Parse(TestData.GlossaryJson());
            var (unlockedBefore, total) = CodexService.Progress(save, glossary, "architecture");
            Assert.That(unlockedBefore, Is.EqualTo(0));
            Assert.That(total, Is.GreaterThanOrEqualTo(6));
            CodexService.OnEvent(save, CodexEvent.SawArchitecture);
            var (unlockedAfter, _) = CodexService.Progress(save, glossary, "architecture");
            Assert.That(unlockedAfter, Is.EqualTo(5));
        }

        [Test]
        public void CodexUnlocked_SurvivesSaveRoundTrip()
        {
            SaveData save = NewSave();
            CodexService.OnEvent(save, CodexEvent.Appointed);
            var migrator = SaveMigrator.CreateDefault();
            SaveData restored = migrator.Load(migrator.Serialize(save));
            Assert.That(CodexService.IsUnlocked(restored, "xian_wei"), Is.True,
                "解锁进度必须存活");
        }
    }
}
