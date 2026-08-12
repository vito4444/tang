using System;
using Lingyan.Core.Characters;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class SaveMigrationTests
    {
        private SaveMigrator _migrator;

        [SetUp]
        public void SetUp()
        {
            _migrator = SaveMigrator.CreateDefault();
        }

        private static SaveData FreshSave()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            return SaveFactory.NewGame(draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void V1_RoundTrip_PreservesEverything()
        {
            SaveData original = FreshSave();
            original.MoneyWen = 123456;
            original.StoryFlags["case1.confronted_magistrate"] = true;
            original.Counters["cases_solved"] = 3;

            SaveData restored = _migrator.Load(_migrator.Serialize(original));

            Assert.That(restored.ProtagonistKey, Is.EqualTo("mingjing"));
            Assert.That(restored.CharacterName, Is.EqualTo("沈知白"));
            Assert.That(restored.MoneyWen, Is.EqualTo(123456), "钱一文不能少");
            Assert.That(restored.Attributes.Wisdom, Is.EqualTo(12));
            Assert.That(restored.StoryFlags["case1.confronted_magistrate"], Is.True,
                "剧情旗标必须存活");
            Assert.That(restored.Counters["cases_solved"], Is.EqualTo(3));
            Assert.That(restored.Date.EraId, Is.EqualTo("chuigong"));
            Assert.That(restored.Date.Day, Is.EqualTo(17));
        }

        [Test]
        public void V0_Migrates_MoneyAndAttributesIntact()
        {
            // 规格第十一节的事故模型：迁移后钱不能变 0、官阶不能错、进度不能丢
            const string v0 = @"{
                ""version"": 0,
                ""hero"": ""mingjing"",
                ""name"": ""沈知白"",
                ""attrs"": { ""tili"": 6, ""shengming"": 6, ""liliang"": 4, ""zhihui"": 12 },
                ""money_guan"": 3.42,
                ""rep"": { ""guan"": 30, ""min"": 40, ""jianghu"": 10 },
                ""date"": { ""era"": ""chuigong"", ""year"": 4, ""month"": 3, ""day"": 17, ""hour"": 5 }
            }";

            SaveData restored = _migrator.Load(v0);

            Assert.That(restored.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(restored.MoneyWen, Is.EqualTo(3420), "3.42 贯 = 3420 文，一文不丢");
            Assert.That(restored.Attributes.Stamina, Is.EqualTo(6));
            Assert.That(restored.Attributes.Wisdom, Is.EqualTo(12));
            Assert.That(restored.Reputation.GuanSheng, Is.EqualTo(30));
            Assert.That(restored.Reputation.MinWang, Is.EqualTo(40));
            Assert.That(restored.Date.EraId, Is.EqualTo("chuigong"));
            Assert.That(restored.Date.HourIndex, Is.EqualTo(5));
            Assert.That(restored.Offices.ZhiShiId, Is.Null, "v0 时代未入仕");
        }

        [Test]
        public void V0_MissingMoney_FailsLoud_NeverZero()
        {
            // 命门：缺钱字段绝不能静默补 0
            const string v0 = @"{
                ""version"": 0,
                ""hero"": ""mingjing"",
                ""name"": ""沈知白"",
                ""attrs"": { ""tili"": 6, ""shengming"": 6, ""liliang"": 4, ""zhihui"": 12 },
                ""rep"": { ""guan"": 30, ""min"": 40, ""jianghu"": 10 },
                ""date"": { ""era"": ""chuigong"", ""year"": 4, ""month"": 3, ""day"": 17, ""hour"": 5 }
            }";
            Assert.Throws<SaveCorruptException>(() => _migrator.Load(v0));
        }

        [Test]
        public void NewerVersion_RefusedExplicitly()
        {
            SaveData save = FreshSave();
            string json = _migrator.Serialize(save).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99");
            var ex = Assert.Throws<SaveVersionTooNewException>(() => _migrator.Load(json));
            Assert.That(ex.FoundVersion, Is.EqualTo(99));
            Assert.That(ex.ReasonKey, Is.EqualTo("save.error.too_new"));
        }

        [Test]
        public void MissingMigration_FailsLoud()
        {
            var gappedMigrator = new SaveMigrator(Array.Empty<ISaveMigration>());
            const string v0 = @"{ ""version"": 0, ""hero"": ""mingjing"" }";
            var ex = Assert.Throws<SaveMigrationMissingException>(() => gappedMigrator.Load(v0));
            Assert.That(ex.FromVersion, Is.EqualTo(0));
        }

        [Test]
        public void GarbageJson_IsCorrupt()
        {
            Assert.Throws<SaveCorruptException>(() => _migrator.Load("这不是存档"));
        }

        [Test]
        public void NoVersionField_IsCorrupt()
        {
            Assert.Throws<SaveCorruptException>(() => _migrator.Load(@"{ ""hero"": ""mingjing"" }"));
        }

        [Test]
        public void V1_MissingOffices_FailsLoud()
        {
            SaveData save = FreshSave();
            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(_migrator.Serialize(save));
            json.Remove("offices");
            Assert.Throws<SaveCorruptException>(() => _migrator.Load(json.ToString()),
                "四轨官身整块丢失必须响亮失败，不得默认补空");
        }

        [Test]
        public void V1_NegativeMoney_IsCorrupt()
        {
            SaveData save = FreshSave();
            string json = _migrator.Serialize(save)
                .Replace("\"moneyWen\": 6000", "\"moneyWen\": -5");
            Assert.Throws<SaveCorruptException>(() => _migrator.Load(json));
        }

        [Test]
        public void V1_UnknownProtagonist_IsCorrupt()
        {
            SaveData save = FreshSave();
            string json = _migrator.Serialize(save)
                .Replace("\"protagonist\": \"mingjing\"", "\"protagonist\": \"li_bai\"");
            Assert.Throws<SaveCorruptException>(() => _migrator.Load(json));
        }

        [Test]
        public void UnknownExtraFields_AreTolerated()
        {
            // 向前兼容：未来版本多出来的字段读旧程序时忽略，不算损坏
            SaveData save = FreshSave();
            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(_migrator.Serialize(save));
            json["someFutureField"] = "whatever";
            SaveData restored = _migrator.Load(json.ToString());
            Assert.That(restored.MoneyWen, Is.EqualTo(6000));
        }

        [Test]
        public void NewGame_StartsUnrobed_AtChuigong4()
        {
            SaveData save = FreshSave();
            Assert.That(save.Offices.ZhiShiId, Is.Null, "开局白身，未任职事");
            Assert.That(save.Offices.SanGuanId, Is.Null);
            Assert.That(save.Offices.XunZhuan, Is.EqualTo(0));
            Assert.That(save.Date.EraId, Is.EqualTo("chuigong"));
            Assert.That(save.Date.EraYear, Is.EqualTo(4));
            Assert.That(save.MoneyWen, Is.EqualTo(6000), "明镜起始 6 贯");
        }

        [Test]
        public void BaiShen_EntryPathPersists()
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.BaiShen);
            for (int i = 0; i < 12; i++)
            {
                foreach (var attr in AttributeSet.AllIds)
                {
                    if (CharacterCreationRules.TryIncrease(draft, attr) == AllocationError.None) { break; }
                }
            }
            draft.EntryPath = EntryPath.TouJun;
            SaveData save = SaveFactory.NewGame(draft, DateTime.UtcNow);
            SaveData restored = _migrator.Load(_migrator.Serialize(save));
            Assert.That(restored.EntryPath, Is.EqualTo("TouJun"), "入仕途径必须存活");
        }
    }
}
