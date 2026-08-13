using System.Linq;
using Lingyan.Core.Localization;
using Lingyan.Core.Reputation;
using Lingyan.Core.World;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class WorldTests
    {
        // ---- 坊门与宵禁（单一事实来源） ----

        [Test]
        public void Gates_FollowCurfewTable_MaoOpensXuCloses()
        {
            // 卯(3)…酉(9) 开；戌(10)…寅(2) 闭
            Assert.That(WardDef.GatesOpenAt(3), Is.True, "卯时启门");
            Assert.That(WardDef.GatesOpenAt(9), Is.True, "酉时未闭");
            Assert.That(WardDef.GatesOpenAt(10), Is.False, "戌时暮鼓闭门");
            Assert.That(WardDef.GatesOpenAt(0), Is.False, "子夜闭门");
            Assert.That(WardDef.GatesOpenAt(2), Is.False, "寅时未启");
            for (int h = 0; h < 12; h++)
            {
                Assert.That(WardDef.GatesOpenAt(h), Is.EqualTo(!WardDef.CurfewAt(h)),
                    "坊门开闭必须与宵禁表反相，时辰 " + h);
            }
        }

        // ---- 环形时段 ----

        [Test]
        public void ScheduleEntry_CoversAcrossMidnight()
        {
            var night = new ScheduleEntry(10, 2, "home", "activity.sleeping");
            Assert.That(night.Covers(10), Is.True);
            Assert.That(night.Covers(11), Is.True);
            Assert.That(night.Covers(0), Is.True);
            Assert.That(night.Covers(1), Is.True);
            Assert.That(night.Covers(2), Is.False, "ToHour 不含");
            Assert.That(night.Covers(5), Is.False);
            Assert.That(night.Length, Is.EqualTo(4));
        }

        // ---- 作息完整性（构造期强制） ----

        [Test]
        public void Schedule_WithGap_IsRejected()
        {
            // 只排了 6 个时辰，其余空档：NPC 不许凭空消失
            Assert.Throws<System.ArgumentException>(() => new NpcScheduleDef(
                "gap", "npc.test", NpcArchetype.Commoner, new[]
                {
                    new ScheduleEntry(0, 6, "a", "x")
                }));
        }

        [Test]
        public void Schedule_WithOverlap_IsRejected()
        {
            // 巳时被排在两处：NPC 不许分身
            Assert.Throws<System.ArgumentException>(() => new NpcScheduleDef(
                "overlap", "npc.test", NpcArchetype.Commoner, new[]
                {
                    new ScheduleEntry(0, 6, "a", "x"),
                    new ScheduleEntry(5, 0, "b", "y")
                }));
        }

        // ---- 示例坊数据 ----

        [Test]
        public void SampleWard_AllNpcs_HaveAnswerForEveryHour()
        {
            foreach (NpcScheduleDef npc in SampleWard.Npcs)
            {
                for (int h = 0; h < 12; h++)
                {
                    ScheduleEntry entry = npc.At(h);
                    Assert.That(entry, Is.Not.Null);
                    Assert.That(SampleWard.Ward.Place(entry.PlaceId), Is.Not.Null,
                        "地点必须可解析: " + entry.PlaceId);
                }
            }
        }

        [Test]
        public void KangSan_SellsAtWestMarketByDay_SleepsByNight()
        {
            NpcScheduleDef kang = SampleWard.Npcs.First(n => n.NpcId == "kang_san");
            Assert.That(kang.At(5).PlaceId, Is.EqualTo("west_market"), "巳时在西市");
            Assert.That(kang.At(5).ActivityKey, Is.EqualTo("activity.selling"));
            Assert.That(kang.At(0).PlaceId, Is.EqualTo("home_kang"), "子夜在家");
            Assert.That(kang.At(0).ActivityKey, Is.EqualTo("activity.sleeping"));
            Assert.That(kang.At(9).ActivityKey, Is.EqualTo("activity.return_ward"),
                "酉时趁暮鼓归坊——坊市制的作息体现");
        }

        [Test]
        public void ZhengWu_PatrolsDuringCurfew()
        {
            NpcScheduleDef zheng = SampleWard.Npcs.First(n => n.NpcId == "zheng_wu");
            for (int h = 0; h < 12; h++)
            {
                if (zheng.At(h).ActivityKey == "activity.night_patrol")
                {
                    Assert.That(WardDef.CurfewAt(h), Is.True,
                        "武侯巡夜必须落在宵禁时段，时辰 " + h);
                }
            }
            Assert.That(zheng.At(0).PlaceId, Is.EqualTo("ward_lanes"), "子夜在巷中巡逻");
            Assert.That(zheng.Archetype, Is.EqualTo(NpcArchetype.Official));
        }

        // ---- 词条齐备：数据引用的每个键都必须在双语目录里 ----

        [Test]
        public void SampleWard_AllKeys_ExistInCatalog()
        {
            var catalog = LocalizationCatalog.Parse(TestData.StringsJson());
            var missing = new System.Collections.Generic.List<string>();

            void Check(string key)
            {
                if (!catalog.Has(key)) { missing.Add(key); }
            }

            Check(SampleWard.Ward.NameKey);
            foreach (PlaceDef place in SampleWard.Ward.Places) { Check(place.NameKey); }
            foreach (NpcScheduleDef npc in SampleWard.Npcs)
            {
                Check(npc.NameKey);
                foreach (ScheduleEntry entry in npc.Entries) { Check(entry.ActivityKey); }
            }
            Assert.That(missing, Is.Empty, "缺词条:\n" + string.Join("\n", missing));
        }
    }
}
