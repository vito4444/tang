using System;

namespace Lingyan.Core.Officials
{
    /// <summary>
    /// 品阶细分：一至三品不分上下；四品以下分上、下阶。
    /// 勋官"视品"不分上下，用 <see cref="RankStep.None"/> 表示。
    /// </summary>
    public enum RankStep
    {
        None = 0,
        Upper = 1,
        Lower = 2
    }

    /// <summary>
    /// 唐代流内品阶（九品三十阶）。值类型，可比较；数值越小地位越高。
    /// 无品（流外、白身）用 null 表示，不进入本类型。
    /// </summary>
    public readonly struct RankGrade : IComparable<RankGrade>, IEquatable<RankGrade>
    {
        /// <summary>一至九品。</summary>
        public int Band { get; }

        /// <summary>false = 正，true = 从。</summary>
        public bool IsCong { get; }

        public RankStep Step { get; }

        private RankGrade(int band, bool isCong, RankStep step)
        {
            Band = band;
            IsCong = isCong;
            Step = step;
        }

        public static RankGrade Of(int band, bool isCong, RankStep step = RankStep.None)
        {
            if (band < 1 || band > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(band), band, "品阶须在一至九品之间");
            }
            if (band <= 3 && step != RankStep.None)
            {
                throw new ArgumentException("一至三品不分上下阶", nameof(step));
            }
            return new RankGrade(band, isCong, step);
        }

        /// <summary>正X品上。</summary>
        public static RankGrade ZhengUpper(int band) { return Of(band, false, RankStep.Upper); }

        /// <summary>正X品下。</summary>
        public static RankGrade ZhengLower(int band) { return Of(band, false, RankStep.Lower); }

        /// <summary>从X品上。</summary>
        public static RankGrade CongUpper(int band) { return Of(band, true, RankStep.Upper); }

        /// <summary>从X品下。</summary>
        public static RankGrade CongLower(int band) { return Of(band, true, RankStep.Lower); }

        /// <summary>正X品（一至三品，或视品）。</summary>
        public static RankGrade Zheng(int band) { return Of(band, false, RankStep.None); }

        /// <summary>从X品（一至三品，或视品）。</summary>
        public static RankGrade Cong(int band) { return Of(band, true, RankStep.None); }

        /// <summary>
        /// 排序键：越小越尊。正三品(300) &lt; 从三品(350) &lt; 正四品上(400) &lt; 正四品下(425)
        /// &lt; 从四品上(450) &lt; 从四品下(475) &lt; 正五品上(500)…
        /// </summary>
        public int OrderValue
        {
            get
            {
                int step = Step == RankStep.Lower ? 25 : 0;
                return Band * 100 + (IsCong ? 50 : 0) + step;
            }
        }

        public int CompareTo(RankGrade other) { return OrderValue.CompareTo(other.OrderValue); }

        /// <summary>本品是否不低于（地位≥）另一品。</summary>
        public bool AtLeast(RankGrade other) { return OrderValue <= other.OrderValue; }

        public bool Equals(RankGrade other)
        {
            return Band == other.Band && IsCong == other.IsCong && Step == other.Step;
        }

        public override bool Equals(object obj) { return obj is RankGrade g && Equals(g); }

        public override int GetHashCode() { return OrderValue; }

        public static bool operator ==(RankGrade a, RankGrade b) { return a.Equals(b); }
        public static bool operator !=(RankGrade a, RankGrade b) { return !a.Equals(b); }
        public static bool operator <(RankGrade a, RankGrade b) { return a.OrderValue < b.OrderValue; }
        public static bool operator >(RankGrade a, RankGrade b) { return a.OrderValue > b.OrderValue; }

        private static readonly string[] BandZh = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

        /// <summary>"正四品上"、"从九品下"、"正三品"。</summary>
        public string ToZh()
        {
            string head = IsCong ? "从" : "正";
            string step = Step == RankStep.Upper ? "上" : Step == RankStep.Lower ? "下" : "";
            return head + BandZh[Band] + "品" + step;
        }

        /// <summary>学界惯用缩写：正=a，从=b，上=1，下=2。如 4a1 = 正四品上。</summary>
        public string ToEnShort()
        {
            string step = Step == RankStep.Upper ? "1" : Step == RankStep.Lower ? "2" : "";
            return Band.ToString() + (IsCong ? "b" : "a") + step;
        }

        /// <summary>"rank 4a1" 形式的英文表述。</summary>
        public string ToEn() { return "rank " + ToEnShort(); }

        public override string ToString() { return ToZh(); }
    }
}
