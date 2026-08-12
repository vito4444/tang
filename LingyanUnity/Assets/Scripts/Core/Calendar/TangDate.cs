using System;

namespace Lingyan.Core.Calendar
{
    /// <summary>
    /// 游戏历法：年号纪年 + 十二月 × 三十日 + 十二时辰 + 二十四节气。
    /// 每月三十日为游戏化简化（不排真实朔闰），节气每十五日一换，
    /// 自正月初一之立春起。见 docs/DECISIONS.md。
    /// </summary>
    public sealed class TangDate : IEquatable<TangDate>
    {
        public const int DaysPerMonth = 30;
        public const int MonthsPerYear = 12;

        public string EraId { get; private set; }

        /// <summary>年号纪年，1 = 元年。</summary>
        public int EraYear { get; private set; }

        /// <summary>1–12。</summary>
        public int Month { get; private set; }

        /// <summary>1–30。</summary>
        public int Day { get; private set; }

        /// <summary>时辰序 0–11：0 = 子时（夜半 23–1 时）。</summary>
        public int HourIndex { get; private set; }

        public TangDate(string eraId, int eraYear, int month, int day, int hourIndex)
        {
            if (EraTable.Get(eraId) == null) { throw new ArgumentException("未知年号: " + eraId, nameof(eraId)); }
            if (eraYear < 1) { throw new ArgumentOutOfRangeException(nameof(eraYear)); }
            if (month < 1 || month > MonthsPerYear) { throw new ArgumentOutOfRangeException(nameof(month)); }
            if (day < 1 || day > DaysPerMonth) { throw new ArgumentOutOfRangeException(nameof(day)); }
            if (hourIndex < 0 || hourIndex > 11) { throw new ArgumentOutOfRangeException(nameof(hourIndex)); }
            EraId = eraId;
            EraYear = eraYear;
            Month = month;
            Day = day;
            HourIndex = hourIndex;
        }

        public EraDef Era { get { return EraTable.Get(EraId); } }

        /// <summary>年内第几日，1–360。</summary>
        public int DayOfYear { get { return (Month - 1) * DaysPerMonth + Day; } }

        /// <summary>当前节气。</summary>
        public SolarTermDef SolarTerm
        {
            get { return SolarTerms.All[(DayOfYear - 1) / 15]; }
        }

        public ShiChenDef ShiChen { get { return ShiChenTable.All[HourIndex]; } }

        /// <summary>推进若干时辰，就地滚动日/月/年。年号不自动更替，由剧情脚本改元。</summary>
        public void AdvanceHours(int shiChenCount)
        {
            if (shiChenCount < 0) { throw new ArgumentOutOfRangeException(nameof(shiChenCount)); }
            int total = HourIndex + shiChenCount;
            HourIndex = total % 12;
            AdvanceDays(total / 12);
        }

        public void AdvanceDays(int days)
        {
            if (days < 0) { throw new ArgumentOutOfRangeException(nameof(days)); }
            int dayTotal = (Day - 1) + days;
            Day = dayTotal % DaysPerMonth + 1;
            int monthTotal = (Month - 1) + dayTotal / DaysPerMonth;
            Month = monthTotal % MonthsPerYear + 1;
            EraYear += monthTotal / MonthsPerYear;
        }

        /// <summary>剧情改元：换年号并重置纪年（默认元年）。</summary>
        public void SetEra(string eraId, int eraYear = 1)
        {
            if (EraTable.Get(eraId) == null) { throw new ArgumentException("未知年号: " + eraId, nameof(eraId)); }
            EraId = eraId;
            EraYear = eraYear;
        }

        /// <summary>"垂拱四年 三月十七 · 巳时"。</summary>
        public string ToZh()
        {
            return Era.Zh + ZhNumerals.YearZh(EraYear) + "年 "
                + ZhNumerals.MonthZh(Month) + ZhNumerals.DayZh(Day)
                + " · " + ShiChen.Zh + "时";
        }

        /// <summary>"Chuigong 4 · Month 3, Day 17 · Hour of Si (9–11 a.m.)"。</summary>
        public string ToEn()
        {
            return Era.Pinyin + " " + EraYear
                + " · Month " + Month + ", Day " + Day
                + " · " + ShiChen.En;
        }

        /// <summary>紧凑戳，账本与存档用："chuigong:4:3:17:5"。</summary>
        public string ToStamp()
        {
            return EraId + ":" + EraYear + ":" + Month + ":" + Day + ":" + HourIndex;
        }

        public static TangDate FromStamp(string stamp)
        {
            if (string.IsNullOrEmpty(stamp)) { return null; }
            string[] parts = stamp.Split(':');
            if (parts.Length != 5) { return null; }
            return new TangDate(
                parts[0],
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                int.Parse(parts[3]),
                int.Parse(parts[4]));
        }

        public TangDate Clone()
        {
            return new TangDate(EraId, EraYear, Month, Day, HourIndex);
        }

        public bool Equals(TangDate other)
        {
            return other != null
                && EraId == other.EraId
                && EraYear == other.EraYear
                && Month == other.Month
                && Day == other.Day
                && HourIndex == other.HourIndex;
        }

        public override bool Equals(object obj) { return Equals(obj as TangDate); }

        public override int GetHashCode() { return ToStamp().GetHashCode(); }

        public override string ToString() { return ToZh(); }
    }
}
