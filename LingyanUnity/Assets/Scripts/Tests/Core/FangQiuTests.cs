using System;
using Lingyan.Core.Characters;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class FangQiuTests
    {
        private static SaveData MilitarySave(ProtagonistId id = ProtagonistId.ShuZu)
        {
            CharacterDraft draft = CharacterCreationRules.NewDraft(id);
            SaveData save = SaveFactory.NewGame(
                draft, new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
            save.Offices.ZhiShiId = "dui_zheng";
            save.Date.Month = 7; // 秋
            return save;
        }

        [Test]
        public void Gate_MilitaryLine_AutumnOnly_OncePerYear()
        {
            SaveData civil = MilitarySave(ProtagonistId.MingJing);
            civil.Offices.ZhiShiId = "xian_wei";
            Assert.That(FangQiuService.RefusalKey(civil, 688),
                Is.EqualTo("fangqiu.refuse.not_military"), "文吏不与");

            SaveData spring = MilitarySave();
            spring.Date.Month = 3;
            Assert.That(FangQiuService.RefusalKey(spring, 688),
                Is.EqualTo("fangqiu.refuse.not_autumn"));

            SaveData ok = MilitarySave();
            Assert.That(FangQiuService.RefusalKey(ok, 688), Is.Null);
            FangQiuService.Go(ok, 688);
            ok.Date.Month = 9; // 役期一月推到八月，再拨回秋内
            Assert.That(FangQiuService.RefusalKey(ok, 688),
                Is.EqualTo("fangqiu.refuse.already"), "岁一次");
            Assert.That(FangQiuService.RefusalKey(ok, 689), Is.Null, "来年再点");
        }

        [Test]
        public void Vanguard_HighBody_TwoZhuan_MeritAndFame()
        {
            SaveData save = MilitarySave(); // 铁衣 体9力8 = 17 ≥ 14
            FangQiuResult result = FangQiuService.Go(save, 688);

            Assert.That(result.Went, Is.True);
            Assert.That(result.TextKey, Is.EqualTo("fangqiu.result.vanguard"));
            Assert.That(result.ZhuanGained, Is.EqualTo(2), "先登陷阵勋加二转");
            Assert.That(save.Offices.XunZhuan, Is.EqualTo(2));
            Assert.That(XunGuanTable.ForZhuan(save.Offices.XunZhuan), Is.Not.Null,
                "二转起有勋号（云骑尉），书房勋轨不再恒零");
            Assert.That(save.Counters["merit_points"], Is.EqualTo(2), "战功计入考课");
            Assert.That(result.JiangHuDelta, Is.EqualTo(+2));
        }

        [Test]
        public void RankAndFile_WeakBody_OneZhuan()
        {
            SaveData save = MilitarySave(ProtagonistId.MingJing); // 明镜 体6力4 = 10 < 14
            save.Offices.ZhiShiId = "dui_zheng"; // 假令权充军职
            FangQiuResult result = FangQiuService.Go(save, 688);
            Assert.That(result.TextKey, Is.EqualTo("fangqiu.result.served"));
            Assert.That(result.ZhuanGained, Is.EqualTo(1));
            Assert.That(result.MeritGained, Is.EqualTo(0), "随军无阵功");
        }

        [Test]
        public void Term_AdvancesOneMonth()
        {
            SaveData save = MilitarySave();
            save.Date.Day = 5;
            FangQiuService.Go(save, 688);
            Assert.That(save.Date.Month, Is.EqualTo(8), "七月应点，八月归坊");
            Assert.That(save.Date.Day, Is.EqualTo(5), "整推三十日（本历每月三十日）");
        }

        [Test]
        public void Zhuan_CapsAtTwelve_ShangZhuGuo()
        {
            SaveData save = MilitarySave();
            save.Offices.XunZhuan = 11;
            FangQiuService.Go(save, 688);
            Assert.That(save.Offices.XunZhuan, Is.EqualTo(12), "封顶十二转");
            Assert.That(XunGuanTable.ForZhuan(12).Zh, Is.EqualTo("上柱国"));
        }

        [Test]
        public void RefusedGo_ChangesNothing()
        {
            SaveData save = MilitarySave();
            save.Date.Month = 4;
            int day = save.Date.Day;
            FangQiuResult result = FangQiuService.Go(save, 688);
            Assert.That(result.Went, Is.False);
            Assert.That(save.Offices.XunZhuan, Is.EqualTo(0));
            Assert.That(save.Date.Day, Is.EqualTo(day), "驳回不动历期");
        }
    }
}
