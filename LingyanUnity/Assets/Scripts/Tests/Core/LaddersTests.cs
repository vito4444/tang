using System.Linq;
using Lingyan.Core.Officials;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    /// <summary>迁转序列的契约测试：名称、顺序、规格给定的品阶逐条锁定。</summary>
    [TestFixture]
    public class LaddersTests
    {
        [Test]
        public void CivilLadder_MatchesSpecOrder()
        {
            string[] expected =
            {
                "县尉", "县丞", "监察御史", "侍御史", "大理寺丞", "刑部员外郎",
                "刑部郎中", "大理寺少卿", "御史中丞", "大理寺卿", "中书侍郎", "同中书门下平章事"
            };
            Assert.That(OfficialLadders.Civil.Select(o => o.Zh), Is.EqualTo(expected));
        }

        [Test]
        public void MilitaryLadder_MatchesSpecOrder()
        {
            string[] expected =
            {
                "队正", "旅帅", "校尉", "果毅都尉", "折冲都尉",
                "中郎将", "诸卫将军", "诸卫大将军", "行军总管", "骠骑大将军"
            };
            Assert.That(OfficialLadders.Military.Select(o => o.Zh), Is.EqualTo(expected));
        }

        [Test]
        public void SpecGivenGrades_AreLockedIn()
        {
            // 规格第四节明文给出的品阶
            Assert.That(Get("xian_cheng").Grade, Is.EqualTo(RankGrade.ZhengLower(8)), "县丞 正八品下");
            Assert.That(Get("jiancha_yushi").Grade, Is.EqualTo(RankGrade.ZhengUpper(8)), "监察御史 正八品上");
            Assert.That(Get("shi_yushi").Grade, Is.EqualTo(RankGrade.CongLower(6)), "侍御史 从六品下");
            Assert.That(Get("dalisi_cheng").Grade, Is.EqualTo(RankGrade.CongUpper(6)), "大理寺丞 从六品上");
            Assert.That(Get("xingbu_yuanwailang").Grade, Is.EqualTo(RankGrade.CongUpper(6)), "刑部员外郎 从六品上");
            Assert.That(Get("xingbu_langzhong").Grade, Is.EqualTo(RankGrade.CongUpper(5)), "刑部郎中 从五品上");
            Assert.That(Get("dalisi_shaoqing").Grade, Is.EqualTo(RankGrade.CongUpper(4)), "大理寺少卿 从四品上");
            Assert.That(Get("dalisi_qing").Grade, Is.EqualTo(RankGrade.Cong(3)), "大理寺卿 从三品");
            Assert.That(Get("xian_wei").Grade.Value.Band, Is.EqualTo(9), "县尉 从九品");
            Assert.That(Get("xian_wei").Grade.Value.IsCong, Is.True, "县尉 从九品");

            Assert.That(Get("lv_shuai").Grade, Is.EqualTo(RankGrade.CongUpper(8)), "旅帅 从八品上");
            Assert.That(Get("xiao_wei").Grade, Is.EqualTo(RankGrade.ZhengUpper(6)), "校尉 正六品上");
            Assert.That(Get("zhonglang_jiang").Grade, Is.EqualTo(RankGrade.ZhengLower(4)), "中郎将 正四品下");
            Assert.That(Get("zhuwei_jiangjun").Grade, Is.EqualTo(RankGrade.Cong(3)), "诸卫将军 从三品");
            Assert.That(Get("zhuwei_dajiangjun").Grade, Is.EqualTo(RankGrade.Zheng(3)), "诸卫大将军 正三品");
            Assert.That(Get("piaoqi_dajiangjun").Grade, Is.EqualTo(RankGrade.Cong(1)), "骠骑大将军 从一品");
        }

        [Test]
        public void Commissions_HaveNoOwnGrade()
        {
            Assert.That(Get("tong_pingzhangshi").Grade, Is.Null, "同平章事为差遣，无本品");
            Assert.That(Get("tong_pingzhangshi").IsCommission, Is.True);
            Assert.That(Get("xingjun_zongguan").Grade, Is.Null, "行军总管为差遣，无本品");
            Assert.That(Get("xingjun_zongguan").IsCommission, Is.True);
        }

        [Test]
        public void PiaoqiDajiangjun_IsPrestigeCapstone()
        {
            // 规格：骠骑大将军(从一品·武散官极品)
            Assert.That(Get("piaoqi_dajiangjun").Track, Is.EqualTo(OfficeTrack.SanGuan));
        }

        [Test]
        public void NextOf_WalksLadderAndStopsAtEnd()
        {
            Assert.That(OfficialLadders.NextOf(Get("xian_wei")).Id, Is.EqualTo("xian_cheng"));
            Assert.That(OfficialLadders.NextOf(Get("tong_pingzhangshi")), Is.Null);
            Assert.That(OfficialLadders.NextOf(Get("piaoqi_dajiangjun")), Is.Null);
        }

        private static OfficeDef Get(string id)
        {
            OfficeDef def = OfficialLadders.Get(id);
            Assert.That(def, Is.Not.Null, "缺官职: " + id);
            return def;
        }
    }

    [TestFixture]
    public class SanGuanXunJueTests
    {
        [Test]
        public void CivilAndMilitarySanGuan_Have29RanksEach()
        {
            // 文散官自开府仪同三司至将仕郎二十九阶；
            // 武散官主干自骠骑大将军至陪戎副尉亦二十九阶（怀化、归德蕃号不入本表）。
            Assert.That(SanGuanTable.CivilRanks.Count, Is.EqualTo(29), "文散官二十九阶");
            Assert.That(SanGuanTable.MilitaryRanks.Count, Is.EqualTo(29), "武散官主干二十九阶");
        }

        [Test]
        public void SanGuan_IdsUnique_OrderStrictlyDescending()
        {
            var all = SanGuanTable.All.ToList();
            Assert.That(all.Select(s => s.Id).Distinct().Count(), Is.EqualTo(all.Count));

            foreach (var ranks in new[] { SanGuanTable.CivilRanks, SanGuanTable.MilitaryRanks })
            {
                for (int i = 1; i < ranks.Count; i++)
                {
                    Assert.That(ranks[i - 1].Grade.OrderValue, Is.LessThan(ranks[i].Grade.OrderValue),
                        ranks[i - 1].Zh + " 应高于 " + ranks[i].Zh);
                }
            }
        }

        [Test]
        public void SanGuan_TopAndBottom_AreCanonical()
        {
            Assert.That(SanGuanTable.CivilRanks[0].Zh, Is.EqualTo("开府仪同三司"));
            Assert.That(SanGuanTable.CivilRanks[28].Zh, Is.EqualTo("将仕郎"));
            Assert.That(SanGuanTable.MilitaryRanks[0].Zh, Is.EqualTo("骠骑大将军"));
            Assert.That(SanGuanTable.MilitaryRanks[28].Zh, Is.EqualTo("陪戎副尉"));
        }

        [Test]
        public void InitialFor_XianWei_IsJiangshiLang()
        {
            // 从九品下的县尉，起家散官应为同阶的将仕郎
            SanGuanDef initial = SanGuanTable.InitialFor(RankGrade.CongLower(9), civil: true);
            Assert.That(initial.Zh, Is.EqualTo("将仕郎"));
        }

        [Test]
        public void InitialFor_DuiZheng_IsMilitaryNinthBand()
        {
            SanGuanDef initial = SanGuanTable.InitialFor(RankGrade.ZhengLower(9), civil: false);
            Assert.That(initial.Zh, Is.EqualTo("仁勇副尉"));
        }

        [Test]
        public void XunGuan_TwelveZhuan_ShangZhuguoAtTop()
        {
            Assert.That(XunGuanTable.Ranks.Count, Is.EqualTo(12));
            Assert.That(XunGuanTable.ForZhuan(12).Zh, Is.EqualTo("上柱国"));
            Assert.That(XunGuanTable.ForZhuan(1).Zh, Is.EqualTo("武骑尉"));
            Assert.That(XunGuanTable.ForZhuan(0), Is.Null, "无转无勋");
            Assert.That(XunGuanTable.ForZhuan(99).Zh, Is.EqualTo("上柱国"), "超上限封顶");
        }

        [Test]
        public void Jue_TiersDescendFromQinWang()
        {
            Assert.That(JueTable.Tiers[0].Zh, Is.EqualTo("亲王"));
            Assert.That(JueTable.Tiers[JueTable.Tiers.Count - 1].Zh, Is.EqualTo("开国县男"));
            Assert.That(JueTable.Get("kaiguo_xiannan").Grade, Is.EqualTo(RankGrade.CongUpper(5)));
        }
    }
}
