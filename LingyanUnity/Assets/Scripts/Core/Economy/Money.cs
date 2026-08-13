using System;

namespace Lingyan.Core.Economy
{
    /// <summary>
    /// 货币：贯 / 文，1 贯 = 1000 文。内部以文计。
    /// 展示规格："3 贯 420 文"，不是 "3420"。
    /// </summary>
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public const long WenPerGuan = 1000;

        public long Wen { get; }

        public Money(long wen)
        {
            if (wen < 0) { throw new ArgumentOutOfRangeException(nameof(wen), "钱不为负"); }
            Wen = wen;
        }

        public static Money FromGuanWen(long guan, long wen)
        {
            return new Money(guan * WenPerGuan + wen);
        }

        public long GuanPart { get { return Wen / WenPerGuan; } }
        public long WenPart { get { return Wen % WenPerGuan; } }

        /// <summary>"3 贯 420 文"；整贯 "3 贯"；不足贯 "420 文"；零 "0 文"。</summary>
        public string ToZh()
        {
            long g = GuanPart;
            long w = WenPart;
            if (g > 0 && w > 0) { return g + " 贯 " + w + " 文"; }
            if (g > 0) { return g + " 贯"; }
            return w + " 文";
        }

        /// <summary>"3 guan 420 wen" 同构英文。</summary>
        public string ToEn()
        {
            long g = GuanPart;
            long w = WenPart;
            if (g > 0 && w > 0) { return g + " guan " + w + " wen"; }
            if (g > 0) { return g + " guan"; }
            return w + " wen";
        }

        public bool CanAfford(Money cost) { return Wen >= cost.Wen; }

        public static Money operator +(Money a, Money b) { return new Money(a.Wen + b.Wen); }

        public static Money operator -(Money a, Money b)
        {
            if (a.Wen < b.Wen) { throw new InvalidOperationException("钱不够扣减"); }
            return new Money(a.Wen - b.Wen);
        }

        public bool Equals(Money other) { return Wen == other.Wen; }
        public override bool Equals(object obj) { return obj is Money m && Equals(m); }
        public override int GetHashCode() { return Wen.GetHashCode(); }
        public int CompareTo(Money other) { return Wen.CompareTo(other.Wen); }
        public static bool operator ==(Money a, Money b) { return a.Wen == b.Wen; }
        public static bool operator !=(Money a, Money b) { return a.Wen != b.Wen; }
        public static bool operator <(Money a, Money b) { return a.Wen < b.Wen; }
        public static bool operator >(Money a, Money b) { return a.Wen > b.Wen; }

        public override string ToString() { return ToZh(); }
    }
}
