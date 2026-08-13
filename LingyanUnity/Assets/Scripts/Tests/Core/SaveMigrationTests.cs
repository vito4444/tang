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
            string json = _migrator.Serialize(save).Replace(
                "\"schemaVersion\": " + SaveData.CurrentVersion, "\"schemaVersion\": 99");
            var ex = Assert.Throws<SaveVersionTooNewException>(() => _migrator.Load(json));
            Assert.That(ex.FoundVersion, Is.EqualTo(99));
            Assert.That(ex.ReasonKey, Is.EqualTo("save.error.too_new"));
        }

        [Test]
        public void V1_MigratesToV2_NpcStatesEmpty_ProgressIntact()
        {
            // 真实的 v1 存档样式（阶段 1–2 时代出的档）
            const string v1 = @"{
                ""schemaVersion"": 1,
                ""createdUtc"": ""2026-08-13T00:00:00Z"",
                ""protagonist"": ""mingjing"",
                ""name"": ""沈知白"",
                ""entryPath"": null,
                ""attributes"": { ""stamina"": 6, ""health"": 6, ""strength"": 4, ""wisdom"": 12 },
                ""offices"": { ""zhishi"": ""xian_wei"", ""sanguan"": ""jiangshi_lang"", ""xunZhuan"": 0, ""jue"": null },
                ""reputation"": { ""guansheng"": 33, ""minwang"": 41, ""jianghu"": 9 },
                ""reputationLedger"": [],
                ""moneyWen"": 5230,
                ""date"": { ""era"": ""chuigong"", ""eraYear"": 4, ""month"": 5, ""day"": 2, ""hourIndex"": 7 },
                ""storyFlags"": { ""met_magistrate"": true },
                ""counters"": { ""days_played"": 40 }
            }";
            SaveData restored = _migrator.Load(v1);
            Assert.That(restored.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(restored.WantedLevel, Is.EqualTo(0), "老档无案底");
            Assert.That(restored.NpcStates, Is.Empty, "老档未识一人");
            Assert.That(restored.MoneyWen, Is.EqualTo(5230), "钱一文不丢");
            Assert.That(restored.Offices.ZhiShiId, Is.EqualTo("xian_wei"), "官身不丢");
            Assert.That(restored.StoryFlags["met_magistrate"], Is.True, "剧情旗标不丢");
        }

        [Test]
        public void V0_ChainsThroughToCurrent()
        {
            const string v0 = @"{
                ""version"": 0, ""hero"": ""mingjing"", ""name"": ""沈知白"",
                ""attrs"": { ""tili"": 6, ""shengming"": 6, ""liliang"": 4, ""zhihui"": 12 },
                ""money_guan"": 1.0,
                ""rep"": { ""guan"": 30, ""min"": 40, ""jianghu"": 10 },
                ""date"": { ""era"": ""chuigong"", ""year"": 4, ""month"": 3, ""day"": 17, ""hour"": 5 }
            }";
            SaveData restored = _migrator.Load(v0);
            Assert.That(restored.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion),
                "v0 档要能沿迁移链一路升到当前版");
            Assert.That(restored.NpcStates, Is.Empty);
        }

        [Test]
        public void V2_NpcState_RoundTrips()
        {
            SaveData save = FreshSave();
            save.NpcStates["huan_fuzi"] = new SaveNpcState
            {
                Met = true,
                LastGreetDay = "chuigong:4:3:17",
                Ledger = new System.Collections.Generic.List<SaveAffinityEntry>
                {
                    new SaveAffinityEntry
                    {
                        Delta = 8,
                        SourceKey = "affinity.src.gift_liked",
                        SourceParam = "gift.wenxuan",
                        DateStamp = "chuigong:4:3:17:5"
                    }
                },
                Flags = new System.Collections.Generic.List<string> { "some_flag" }
            };
            save.WantedLevel = 5;

            SaveData restored = _migrator.Load(_migrator.Serialize(save));
            Assert.That(restored.WantedLevel, Is.EqualTo(5));
            Assert.That(restored.NpcStates["huan_fuzi"].Ledger[0].Delta, Is.EqualTo(8));
            Assert.That(restored.NpcStates["huan_fuzi"].Ledger[0].SourceParam,
                Is.EqualTo("gift.wenxuan"));
            Assert.That(restored.NpcStates["huan_fuzi"].Flags, Does.Contain("some_flag"));
        }

        [Test]
        public void V3_MigratesToV4_EmptyInventory_ProgressIntact()
        {
            // 真实的 v3 存档样式（阶段 5–10 时代出的档：有案、有考课、有婚约）
            const string v3 = @"{
                ""schemaVersion"": 3,
                ""createdUtc"": ""2026-08-13T00:00:00Z"",
                ""protagonist"": ""mingjing"",
                ""name"": ""沈知白"",
                ""entryPath"": null,
                ""attributes"": { ""stamina"": 6, ""health"": 6, ""strength"": 4, ""wisdom"": 12 },
                ""offices"": { ""zhishi"": ""xian_wei"", ""sanguan"": ""jiangshi_lang"", ""xunZhuan"": 0, ""jue"": null },
                ""reputation"": { ""guansheng"": 33, ""minwang"": 41, ""jianghu"": 9 },
                ""reputationLedger"": [],
                ""moneyWen"": 5230,
                ""date"": { ""era"": ""chuigong"", ""eraYear"": 4, ""month"": 5, ""day"": 2, ""hourIndex"": 7 },
                ""storyFlags"": {},
                ""counters"": {},
                ""wantedLevel"": 0,
                ""npcStates"": {},
                ""cases"": { ""silk_case"": { ""status"": 1, ""opened"": ""chuigong:4:5:1:6"",
                    ""deadline"": ""chuigong:4:5:11:6"", ""clues"": [""clue_ledger""],
                    ""inferences"": [], ""accused"": null, ""outcome"": null, ""wrongful"": false } },
                ""kaokeGrades"": [4],
                ""housing"": ""hut"",
                ""codex"": [""shi_chen""],
                ""marriage"": { ""match"": null, ""rite"": 0, ""married"": false }
            }";
            SaveData restored = _migrator.Load(v3);
            Assert.That(restored.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(restored.Inventory, Is.Empty, "老档行囊为空，不得凭空生货");
            Assert.That(restored.Cases["silk_case"].Clues, Does.Contain("clue_ledger"),
                "查案进度一条不丢");
            Assert.That(restored.KaoKeGrades[0], Is.EqualTo(4), "考课等第不丢");
            Assert.That(restored.MoneyWen, Is.EqualTo(5230));
        }

        [Test]
        public void V4_Inventory_RoundTrips()
        {
            SaveData save = FreshSave();
            save.Inventory["gift_jiu"] = 2;
            save.Inventory["gift_wenxuan"] = 1;
            SaveData restored = _migrator.Load(_migrator.Serialize(save));
            Assert.That(restored.Inventory["gift_jiu"], Is.EqualTo(2), "行囊件数一件不丢");
            Assert.That(restored.Inventory["gift_wenxuan"], Is.EqualTo(1));
        }

        [Test]
        public void V4_UnknownInventoryItem_IsCorrupt()
        {
            SaveData save = FreshSave();
            save.Inventory["sword_of_plus_ten"] = 1;
            Assert.Throws<SaveCorruptException>(() => _migrator.Load(_migrator.Serialize(save)),
                "行囊里出现目录外物品必须响亮失败");
        }

        [Test]
        public void V4_ZeroCountInventoryEntry_IsCorrupt()
        {
            SaveData save = FreshSave();
            save.Inventory["gift_jiu"] = 0;
            Assert.Throws<SaveCorruptException>(() => _migrator.Load(_migrator.Serialize(save)),
                "0 件条目该在消耗时移除，落档即为损坏");
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
