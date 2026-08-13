using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lingyan.Core.Localization;
using Lingyan.Core.Terminology;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class LocalizationGlossaryTests
    {
        private LocalizationCatalog _catalog;
        private Glossary _glossary;

        [OneTimeSetUp]
        public void Load()
        {
            _catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            _glossary = Glossary.Parse(TestData.GlossaryJson());
        }

        [Test]
        public void Catalog_LoadsWithContent()
        {
            Assert.That(_catalog.Count, Is.GreaterThan(60));
        }

        [Test]
        public void EveryKey_HasBothLanguages()
        {
            List<string> violations = _catalog.FindParityViolations();
            Assert.That(violations, Is.Empty,
                "双语并齐违规:\n" + string.Join("\n", violations));
        }

        [Test]
        public void MissingKey_ReturnsVisibleSentinel_NotSilence()
        {
            string value = _catalog.Get("no.such.key", Locale.ZhHans);
            Assert.That(value, Does.Contain("no.such.key"), "缺键必须在画面上可见");
            Assert.That(_catalog.MissingKeys, Does.Contain("no.such.key"));
        }

        [Test]
        public void Glossary_AllLadderOfficesResolve()
        {
            List<string> violations = GlossaryValidator.CheckOfficesResolve(_glossary);
            Assert.That(violations, Is.Empty,
                "官职术语缺口:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Glossary_CatalogUsesLockedTranslations()
        {
            List<string> violations = GlossaryValidator.CheckCatalogConsistency(_catalog, _glossary);
            Assert.That(violations, Is.Empty,
                "术语一致性违规:\n" + string.Join("\n", violations));
        }

        [Test]
        public void Glossary_NoDuplicateEnglishWithinOffices()
        {
            List<string> violations = GlossaryValidator.CheckInternalUniqueness(_glossary);
            Assert.That(violations, Is.Empty, string.Join("\n", violations));
        }

        [Test]
        public void Validator_CatchesRogueTranslation_MutationCheck()
        {
            // 变异验证：把逻辑改坏，校验必须变红——故意塞一条私译，断言被咬住。
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            catalog.Add("test.rogue", "此人官拜县尉。", "He was appointed County Sheriff.");
            List<string> violations = GlossaryValidator.CheckCatalogConsistency(catalog, _glossary);
            Assert.That(violations.Count(v => v.Contains("test.rogue")), Is.EqualTo(1),
                "校验器必须抓住未按术语表翻译的官职");
        }

        [Test]
        public void Validator_IgnoresSubstringCoveredByLongerTerm()
        {
            // 「开国郡公」中含「国公」，不得因短词条误报
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            catalog.Add("test.covered", "帝封其为开国郡公。", "The emperor made him a Commandery Duke.");
            List<string> violations = GlossaryValidator.CheckCatalogConsistency(catalog, _glossary);
            Assert.That(violations.Where(v => v.Contains("test.covered")), Is.Empty,
                "被长词条覆盖的短词条命中不应报违规");
        }

        [Test]
        public void ArchitectureRedlines_AreInGlossary()
        {
            // 考据红线词条必须在册：鸱尾（用）、鸱吻（禁）、下昂、斗拱
            Assert.That(_glossary.ByZh("鸱尾"), Is.Not.Null);
            Assert.That(_glossary.ByZh("鸱吻"), Is.Not.Null);
            Assert.That(_glossary.ByZh("下昂"), Is.Not.Null);
            Assert.That(_glossary.ByZh("斗拱"), Is.Not.Null);
            Assert.That(_glossary.ByZh("鸱尾").Note, Does.Contain("660"),
                "鸱尾词条须注明本作年代窗口用形");
        }

        [Test]
        public void MilitaryRedline_NoXiongnu()
        {
            // 考据红线：不写"抗击匈奴"。词表与文案任何地方不得出现匈奴。
            Assert.That(_glossary.ByZh("匈奴"), Is.Null);
            foreach (string key in _catalog.Keys)
            {
                _catalog.TryGet(key, Locale.ZhHans, out string zh);
                Assert.That(zh ?? "", Does.Not.Contain("匈奴"), key);
            }
            Assert.That(_glossary.ByZh("吐蕃"), Is.Not.Null, "真正的边患：吐蕃");
            Assert.That(_glossary.ByZh("后突厥"), Is.Not.Null, "真正的边患：后突厥");
        }

        [Test]
        public void FontBan_NoUnlicensedFontAnywhere()
        {
            // 规格第十一节：绝不用那款无商用授权的系统黑体。
            // 禁词以拼接构造，免得本测试文件自己撞网；扫描不设任何豁免。
            string banZh = "微软" + "雅黑";
            string banEn = "microsoft" + " yahei";
            string banFile = "msy" + "h";

            string root = TestData.UnityProjectRoot;
            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(
                Path.Combine(root, "Assets"), "*.*", SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".cs" && ext != ".json" && ext != ".txt" && ext != ".asset"
                    && ext != ".unity" && ext != ".asmdef" && ext != ".md")
                {
                    continue;
                }
                string text = File.ReadAllText(file);
                string lower = text.ToLowerInvariant();
                if (text.Contains(banZh) || lower.Contains(banEn) || lower.Contains(banFile))
                {
                    offenders.Add(file);
                }
            }
            Assert.That(offenders, Is.Empty, "禁用字体出现于:\n" + string.Join("\n", offenders));
        }

        [Test]
        public void UnverifiedTerms_AreTracked_NotHidden()
        {
            // 未复核词条必须可枚举（诚实台账），而非藏在代码里
            List<GlossaryEntry> unverified = _glossary.Entries
                .Where(e => (e.Category == "office" || e.Category == "jue") && !e.Verified)
                .ToList();
            List<GlossaryEntry> verified = _glossary.Entries
                .Where(e => (e.Category == "office" || e.Category == "jue") && e.Verified)
                .ToList();
            Assert.That(verified.Count, Is.GreaterThanOrEqualTo(8), "核心词条应已核定");
            Assert.That(unverified.Count + verified.Count,
                Is.EqualTo(_glossary.Entries.Count(e => e.Category == "office" || e.Category == "jue")));
        }
    }
}
