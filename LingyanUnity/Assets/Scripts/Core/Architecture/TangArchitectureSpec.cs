using System;
using System.Collections.Generic;

namespace Lingyan.Core.Architecture
{
    /// <summary>
    /// 唐构比例红线（规格第二节），全部程序化建筑从这里取参数：
    /// 一句话判据——唐是「平、远、大、素」。
    ///   平：屋面坡度平缓，举高比进深约 1:6；
    ///   远：出檐深远（佛光寺东大殿出檐 3.96 米）；
    ///   大：斗拱硕大承重，整攒高度约为柱高之半；
    ///   素：土红 / 白墙 / 灰瓦，直棂窗。
    /// 参照实物：佛光寺东大殿、南禅寺大殿、唐招提寺金堂。
    /// </summary>
    public static class TangArchitectureSpec
    {
        /// <summary>举高 / 进深 = 1/6（明清陡峻在 1/3–1/2，禁用）。</summary>
        public const double RoofRisePerDepth = 1.0 / 6.0;

        /// <summary>出檐 / 柱高 下限（"远"）。佛光寺出檐近 4 米，比值约 0.8；本作取保守下限。</summary>
        public const double MinEaveOverhangPerColumnHeight = 0.40;

        /// <summary>本作出檐取值。</summary>
        public const double EaveOverhangPerColumnHeight = 0.55;

        /// <summary>斗拱带（铺作层）高度 / 柱高 = 1/2（"大"，承重构件而非装饰件）。</summary>
        public const double BracketBandPerColumnHeight = 0.50;

        /// <summary>直棂窗棂条中距（米）。</summary>
        public const double LatticeSpacing = 0.16;

        /// <summary>棂条截面宽（米）。</summary>
        public const double LatticeBarWidth = 0.045;

        /// <summary>屋面坡角（弧度）。前后坡各跨进深之半：atan(举高 / (进深/2))。</summary>
        public static double RoofPitchRadians(double depth)
        {
            if (depth <= 0) { throw new ArgumentOutOfRangeException(nameof(depth)); }
            double rise = depth * RoofRisePerDepth;
            return Math.Atan(rise / (depth / 2.0));
        }

        /// <summary>举高（米）。</summary>
        public static double RoofRise(double depth)
        {
            return depth * RoofRisePerDepth;
        }

        /// <summary>出檐（米）。</summary>
        public static double EaveOverhang(double columnHeight)
        {
            return columnHeight * EaveOverhangPerColumnHeight;
        }

        /// <summary>铺作层高（米）。</summary>
        public static double BracketBandHeight(double columnHeight)
        {
            return columnHeight * BracketBandPerColumnHeight;
        }

        /// <summary>
        /// 直棂窗棂条布局：给定净宽，返回各棂条中心的横向偏移（自窗中心，米）。
        /// 条数取整、左右对称、间距均匀。
        /// </summary>
        public static IReadOnlyList<double> LatticeBarOffsets(double clearWidth)
        {
            if (clearWidth <= LatticeSpacing)
            {
                return Array.Empty<double>();
            }
            int count = (int)Math.Floor(clearWidth / LatticeSpacing) - 1;
            if (count < 1) { return Array.Empty<double>(); }
            var offsets = new double[count];
            double span = clearWidth / (count + 1);
            for (int i = 0; i < count; i++)
            {
                offsets[i] = span * (i + 1) - clearWidth / 2.0;
            }
            return offsets;
        }
    }
}
