using System;
using System.Collections.Generic;

namespace Lingyan.Core.Architecture
{
    /// <summary>
    /// 鸱尾侧视轮廓（本作 660–705 窗口，早唐至盛唐形制）：
    /// 卷曲的尾鳍状，外缘上扬、顶端向内卷收；有肋纹、无兽头、无张口。
    /// 「张着大口吞屋脊的兽头」是中唐以后的鸱吻，明清龙吻更晚——一律禁用。
    /// 输出为 XY 平面闭合多边形点列（X 朝脊外为正，Y 向上），供网格挤出。
    /// </summary>
    public static class ChiweiProfile
    {
        /// <summary>
        /// 生成轮廓点列。height 为鸱尾总高；宽约为高的 0.62。
        /// 点序逆时针，首点在基座外下角。
        /// </summary>
        public static IReadOnlyList<(double x, double y)> Points(double height, int arcSteps = 7)
        {
            if (height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
            if (arcSteps < 3) { arcSteps = 3; }

            double w = height * 0.62;
            var pts = new List<(double, double)>();

            // 基座（骑在正脊上的部分）
            pts.Add((w * 0.95, 0.0));
            pts.Add((w * 0.95, height * 0.14));

            // 外缘：自基座外沿向上、向内的大弧（尾鳍外脊）
            for (int i = 1; i <= arcSteps; i++)
            {
                double t = i / (double)arcSteps;
                // 二次贝塞尔：起点(0.95w, 0.14h) 控制(1.02w, 0.72h) 终点(0.34w, h)
                double x = Bez(w * 0.95, w * 1.02, w * 0.34, t);
                double y = Bez(height * 0.14, height * 0.72, height, t);
                pts.Add((x, y));
            }

            // 顶端向内卷收的小弧（卷尾，不开口、无兽头）
            for (int i = 1; i <= arcSteps; i++)
            {
                double t = i / (double)arcSteps;
                double x = Bez(w * 0.34, w * 0.10, w * 0.22, t);
                double y = Bez(height, height * 0.97, height * 0.80, t);
                pts.Add((x, y));
            }

            // 内缘回落到脊面
            pts.Add((w * 0.16, height * 0.55));
            pts.Add((0.0, height * 0.10));
            pts.Add((0.0, 0.0));

            return pts;
        }

        /// <summary>肋纹条数（沿外缘的浅棱），随高度取 3–5 条。</summary>
        public static int RibCount(double height)
        {
            return Math.Max(3, Math.Min(5, (int)Math.Round(height * 4)));
        }

        private static double Bez(double a, double b, double c, double t)
        {
            double u = 1 - t;
            return u * u * a + 2 * u * t * b + t * t * c;
        }
    }
}
