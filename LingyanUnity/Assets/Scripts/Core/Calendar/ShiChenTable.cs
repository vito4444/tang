using System.Collections.Generic;

namespace Lingyan.Core.Calendar
{
    /// <summary>一个时辰。</summary>
    public sealed class ShiChenDef
    {
        /// <summary>0–11，0 = 子。</summary>
        public int Index { get; }

        public string Zh { get; }

        /// <summary>英文表述，含现代钟点换算。</summary>
        public string En { get; }

        /// <summary>起始钟点（24 小时制）。子时起于 23 时。</summary>
        public int StartHour { get; }

        /// <summary>是否宵禁时段（暮鼓后、晨钟前）。后续坊市系统使用。</summary>
        public bool CurfewHour { get; }

        public ShiChenDef(int index, string zh, string en, int startHour, bool curfew)
        {
            Index = index;
            Zh = zh;
            En = en;
            StartHour = startHour;
            CurfewHour = curfew;
        }
    }

    public static class ShiChenTable
    {
        /// <summary>
        /// 宵禁近似取戌—寅（暮鼓后至晓鼓前），精确坊门启闭在坊市系统实装。
        /// </summary>
        public static readonly IReadOnlyList<ShiChenDef> All = new[]
        {
            new ShiChenDef(0, "子", "Hour of Zi (11 p.m.–1 a.m.)", 23, true),
            new ShiChenDef(1, "丑", "Hour of Chou (1–3 a.m.)", 1, true),
            new ShiChenDef(2, "寅", "Hour of Yin (3–5 a.m.)", 3, true),
            new ShiChenDef(3, "卯", "Hour of Mao (5–7 a.m.)", 5, false),
            new ShiChenDef(4, "辰", "Hour of Chen (7–9 a.m.)", 7, false),
            new ShiChenDef(5, "巳", "Hour of Si (9–11 a.m.)", 9, false),
            new ShiChenDef(6, "午", "Hour of Wu (11 a.m.–1 p.m.)", 11, false),
            new ShiChenDef(7, "未", "Hour of Wei (1–3 p.m.)", 13, false),
            new ShiChenDef(8, "申", "Hour of Shen (3–5 p.m.)", 15, false),
            new ShiChenDef(9, "酉", "Hour of You (5–7 p.m.)", 17, false),
            new ShiChenDef(10, "戌", "Hour of Xu (7–9 p.m.)", 19, true),
            new ShiChenDef(11, "亥", "Hour of Hai (9–11 p.m.)", 21, true)
        };
    }

    /// <summary>一个节气。</summary>
    public sealed class SolarTermDef
    {
        public int Index { get; }
        public string Zh { get; }
        public string En { get; }

        public SolarTermDef(int index, string zh, string en)
        {
            Index = index;
            Zh = zh;
            En = en;
        }
    }

    public static class SolarTerms
    {
        /// <summary>二十四节气，自立春始（游戏历正月初一）。</summary>
        public static readonly IReadOnlyList<SolarTermDef> All = new[]
        {
            new SolarTermDef(0, "立春", "Beginning of Spring"),
            new SolarTermDef(1, "雨水", "Rain Water"),
            new SolarTermDef(2, "惊蛰", "Awakening of Insects"),
            new SolarTermDef(3, "春分", "Spring Equinox"),
            new SolarTermDef(4, "清明", "Clear and Bright"),
            new SolarTermDef(5, "谷雨", "Grain Rain"),
            new SolarTermDef(6, "立夏", "Beginning of Summer"),
            new SolarTermDef(7, "小满", "Grain Buds"),
            new SolarTermDef(8, "芒种", "Grain in Ear"),
            new SolarTermDef(9, "夏至", "Summer Solstice"),
            new SolarTermDef(10, "小暑", "Minor Heat"),
            new SolarTermDef(11, "大暑", "Major Heat"),
            new SolarTermDef(12, "立秋", "Beginning of Autumn"),
            new SolarTermDef(13, "处暑", "End of Heat"),
            new SolarTermDef(14, "白露", "White Dew"),
            new SolarTermDef(15, "秋分", "Autumn Equinox"),
            new SolarTermDef(16, "寒露", "Cold Dew"),
            new SolarTermDef(17, "霜降", "Frost's Descent"),
            new SolarTermDef(18, "立冬", "Beginning of Winter"),
            new SolarTermDef(19, "小雪", "Minor Snow"),
            new SolarTermDef(20, "大雪", "Major Snow"),
            new SolarTermDef(21, "冬至", "Winter Solstice"),
            new SolarTermDef(22, "小寒", "Minor Cold"),
            new SolarTermDef(23, "大寒", "Major Cold")
        };
    }
}
