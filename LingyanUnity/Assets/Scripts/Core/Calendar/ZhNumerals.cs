using System;
using System.Text;

namespace Lingyan.Core.Calendar
{
    /// <summary>历法展示用汉字数词。</summary>
    public static class ZhNumerals
    {
        private static readonly string[] Digits =
        {
            "零", "一", "二", "三", "四", "五", "六", "七", "八", "九"
        };

        /// <summary>1–99 的汉字数词：四、十七、二十、三十一。</summary>
        public static string Number(int n)
        {
            if (n < 1 || n > 99) { throw new ArgumentOutOfRangeException(nameof(n)); }
            if (n < 10) { return Digits[n]; }
            var sb = new StringBuilder();
            int tens = n / 10;
            int ones = n % 10;
            if (tens > 1) { sb.Append(Digits[tens]); }
            sb.Append("十");
            if (ones > 0) { sb.Append(Digits[ones]); }
            return sb.ToString();
        }

        /// <summary>纪年：1 → "元"，其余为数词（"垂拱元年"、"垂拱四年"）。</summary>
        public static string YearZh(int eraYear)
        {
            return eraYear == 1 ? "元" : Number(eraYear);
        }

        /// <summary>月名：正月、二月……十二月。</summary>
        public static string MonthZh(int month)
        {
            if (month < 1 || month > 12) { throw new ArgumentOutOfRangeException(nameof(month)); }
            return month == 1 ? "正月" : Number(month) + "月";
        }

        /// <summary>日名：初一…初十、十一…十九、二十、廿一…廿九、三十。</summary>
        public static string DayZh(int day)
        {
            if (day < 1 || day > 30) { throw new ArgumentOutOfRangeException(nameof(day)); }
            if (day == 10) { return "初十"; }
            if (day < 10) { return "初" + Digits[day]; }
            if (day < 20) { return "十" + Digits[day - 10]; }
            if (day == 20) { return "二十"; }
            if (day < 30) { return "廿" + Digits[day - 20]; }
            return "三十";
        }
    }
}
