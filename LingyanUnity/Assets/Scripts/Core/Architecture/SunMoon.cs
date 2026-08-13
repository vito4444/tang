using System;

namespace Lingyan.Core.Architecture
{
    /// <summary>天光参数：一盏平行光按时辰摆角度与色温。</summary>
    public sealed class SkyLightState
    {
        /// <summary>方位角（度）：0=北，90=东，180=南，270=西。光从该方位射来。</summary>
        public double AzimuthDeg { get; set; }

        /// <summary>仰角（度）。契约：任何时辰都必须 > 0——
        /// 光源必须在地平线以上（规格第十四节的反面教材：
        /// "夜间光照强度 > 0" 在光源沉到地平线下时照样通过，错的断言）。</summary>
        public double ElevationDeg { get; set; }

        /// <summary>是否月光（宵禁时段）。</summary>
        public bool IsMoon { get; set; }

        /// <summary>光强（Unity Light.intensity 直用）。</summary>
        public double Intensity { get; set; }

        /// <summary>光色 RGB，0–1。</summary>
        public double R { get; set; }
        public double G { get; set; }
        public double B { get; set; }

        /// <summary>环境光乘数（0–1），夜里压暗。</summary>
        public double Ambient { get; set; }
    }

    /// <summary>
    /// 时辰 → 天光。简化模型（不做真实历表）：
    /// 白昼卯—酉，太阳自东（90°）经南（180°）至西（270°），午时最高 65°；
    /// 夜间戌—寅，月光恒仰 35°，随时辰自东南向西南缓移，低强度冷色。
    /// </summary>
    public static class SunMoon
    {
        public const double NoonElevationDeg = 65.0;
        public const double HorizonMarginDeg = 6.0;
        public const double MoonElevationDeg = 35.0;

        public static SkyLightState For(int hourIndex)
        {
            if (hourIndex < 0 || hourIndex > 11)
            {
                throw new ArgumentOutOfRangeException(nameof(hourIndex));
            }

            // 卯(3)…酉(9) 为昼；戌(10)、亥(11)、子(0)、丑(1)、寅(2) 为夜
            bool day = hourIndex >= 3 && hourIndex <= 9;
            var state = new SkyLightState { IsMoon = !day };

            if (day)
            {
                // t: 0=卯（日出侧）… 1=酉（日落侧）
                double t = (hourIndex - 3) / 6.0;
                state.AzimuthDeg = 90.0 + t * 180.0;
                state.ElevationDeg = HorizonMarginDeg
                    + Math.Sin(t * Math.PI) * (NoonElevationDeg - HorizonMarginDeg);
                // 正午白亮，晨昏偏暖
                double warm = 1.0 - Math.Sin(t * Math.PI);
                state.Intensity = 0.55 + 0.55 * Math.Sin(t * Math.PI);
                state.R = 1.00;
                state.G = 0.97 - warm * 0.10;
                state.B = 0.90 - warm * 0.22;
                state.Ambient = 0.55 + 0.30 * Math.Sin(t * Math.PI);
            }
            else
            {
                // 夜序：戌0 亥1 子2 丑3 寅4
                int nightStep = hourIndex >= 10 ? hourIndex - 10 : hourIndex + 2;
                double t = nightStep / 4.0;
                state.AzimuthDeg = 120.0 + t * 120.0;
                state.ElevationDeg = MoonElevationDeg;
                state.Intensity = 0.22;
                state.R = 0.62;
                state.G = 0.68;
                state.B = 0.85;
                state.Ambient = 0.16;
            }
            return state;
        }
    }
}
