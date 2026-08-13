using System;
using System.Linq;
using Lingyan.Core.Architecture;
using Lingyan.Core.World;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    /// <summary>考据红线的可执行化（规格第二节）。改坏任何一条比例，这里先红。</summary>
    [TestFixture]
    public class ArchitectureTests
    {
        [Test]
        public void Roof_IsShallow_OneToSix()
        {
            Assert.That(TangArchitectureSpec.RoofRisePerDepth, Is.EqualTo(1.0 / 6.0).Within(1e-12),
                "举高比进深 1:6，唐制平缓屋面");
            // 进深 9 米 → 举高 1.5 米，坡角约 18.4°；明清 30–45°，出界即红
            double pitchDeg = TangArchitectureSpec.RoofPitchRadians(9.0) * 180.0 / Math.PI;
            Assert.That(pitchDeg, Is.InRange(15.0, 22.0), "屋面坡角须平缓（唐），实际 " + pitchDeg);
        }

        [Test]
        public void Eaves_AreDeep()
        {
            Assert.That(TangArchitectureSpec.EaveOverhangPerColumnHeight,
                Is.GreaterThanOrEqualTo(TangArchitectureSpec.MinEaveOverhangPerColumnHeight),
                "出檐深远：出檐/柱高不得低于下限");
            Assert.That(TangArchitectureSpec.EaveOverhang(4.0), Is.EqualTo(2.2).Within(1e-9));
        }

        [Test]
        public void BracketBand_IsHalfColumnHeight()
        {
            Assert.That(TangArchitectureSpec.BracketBandPerColumnHeight, Is.EqualTo(0.5),
                "斗拱整攒高度约为柱高之半——承重构件，不是明清装饰件");
            Assert.That(TangArchitectureSpec.BracketBandHeight(4.0), Is.EqualTo(2.0).Within(1e-9));
        }

        [Test]
        public void LatticeWindow_BarsEvenAndSymmetric()
        {
            var offsets = TangArchitectureSpec.LatticeBarOffsets(1.6);
            Assert.That(offsets.Count, Is.EqualTo(9), "1.6 米净宽约九根直棂");
            // 对称
            Assert.That(offsets[0], Is.EqualTo(-offsets[offsets.Count - 1]).Within(1e-9));
            // 均匀
            double gap = offsets[1] - offsets[0];
            for (int i = 2; i < offsets.Count; i++)
            {
                Assert.That(offsets[i] - offsets[i - 1], Is.EqualTo(gap).Within(1e-9));
            }
            // 窄到放不下就一根不放，不硬塞
            Assert.That(TangArchitectureSpec.LatticeBarOffsets(0.1), Is.Empty);
        }

        [Test]
        public void Chiwei_IsCurledFin_NotOpenJawedBeast()
        {
            var pts = ChiweiProfile.Points(1.2);
            Assert.That(pts.Count, Is.GreaterThanOrEqualTo(12), "轮廓要有足够弧段");

            double maxY = pts.Max(p => p.y);
            Assert.That(maxY, Is.EqualTo(1.2).Within(0.01), "最高点即总高");

            // 卷尾：终段顶点须向内（x 减小）且低于最高点——尾鳍内卷，而非张口兽头
            var top = pts.First(p => Math.Abs(p.y - maxY) < 1e-9);
            var curlEnd = pts[pts.Count - 4];
            Assert.That(curlEnd.x, Is.LessThan(top.x), "卷收端点须向脊内收");
            Assert.That(curlEnd.y, Is.LessThan(maxY), "卷收端点须低于顶点（卷曲）");

            // 无出界负坐标（轮廓贴脊而立）
            Assert.That(pts.All(p => p.x >= -1e-9 && p.y >= -1e-9), Is.True);

            // 肋纹存在（"有肋纹"红线）
            Assert.That(ChiweiProfile.RibCount(1.2), Is.InRange(3, 5));
        }

        [Test]
        public void SkyLight_AlwaysAboveHorizon_TheRightAssertion()
        {
            // 规格第十四节原文：正确的断言是"夜间光源必须在地平线以上"。
            for (int h = 0; h < 12; h++)
            {
                SkyLightState state = SunMoon.For(h);
                Assert.That(state.ElevationDeg, Is.GreaterThan(0.0),
                    "时辰 " + h + " 光源仰角必须为正（在地平线以上），否则照不到任何朝上的表面");
                Assert.That(state.Intensity, Is.GreaterThan(0.0));
            }
        }

        [Test]
        public void SkyLight_DayNightFollowShiChen()
        {
            // 白昼卯东、午高、酉西；夜间是月，与宵禁时段一致
            SkyLightState mao = SunMoon.For(3);
            SkyLightState wu = SunMoon.For(6);
            SkyLightState you = SunMoon.For(9);
            Assert.That(mao.IsMoon, Is.False);
            Assert.That(mao.AzimuthDeg, Is.EqualTo(90.0).Within(1e-9), "卯时日出东方");
            Assert.That(wu.AzimuthDeg, Is.EqualTo(180.0).Within(1e-9), "午时日在正南");
            Assert.That(wu.ElevationDeg, Is.EqualTo(SunMoon.NoonElevationDeg).Within(1e-9), "午时最高");
            Assert.That(you.AzimuthDeg, Is.EqualTo(270.0).Within(1e-9), "酉时日落西方");

            for (int h = 0; h < 12; h++)
            {
                Assert.That(SunMoon.For(h).IsMoon, Is.EqualTo(WardDef.CurfewAt(h)),
                    "月光时段必须与宵禁时段一致，时辰 " + h);
            }

            Assert.That(SunMoon.For(0).Intensity, Is.LessThan(SunMoon.For(6).Intensity),
                "月光远弱于日光");
        }
    }
}
