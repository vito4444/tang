using System;

namespace Lingyan.Core.Characters
{
    /// <summary>选人界面四维。</summary>
    public enum AttributeId
    {
        /// <summary>体力。</summary>
        Stamina = 0,

        /// <summary>生命。</summary>
        Health = 1,

        /// <summary>力量。</summary>
        Strength = 2,

        /// <summary>智慧。</summary>
        Wisdom = 3
    }

    public sealed class AttributeSet : IEquatable<AttributeSet>
    {
        public int Stamina { get; set; }
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Wisdom { get; set; }

        public AttributeSet() { }

        public AttributeSet(int stamina, int health, int strength, int wisdom)
        {
            Stamina = stamina;
            Health = health;
            Strength = strength;
            Wisdom = wisdom;
        }

        public int Get(AttributeId id)
        {
            switch (id)
            {
                case AttributeId.Stamina: return Stamina;
                case AttributeId.Health: return Health;
                case AttributeId.Strength: return Strength;
                case AttributeId.Wisdom: return Wisdom;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        public void Set(AttributeId id, int value)
        {
            switch (id)
            {
                case AttributeId.Stamina: Stamina = value; break;
                case AttributeId.Health: Health = value; break;
                case AttributeId.Strength: Strength = value; break;
                case AttributeId.Wisdom: Wisdom = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        public int Total { get { return Stamina + Health + Strength + Wisdom; } }

        public AttributeSet Clone()
        {
            return new AttributeSet(Stamina, Health, Strength, Wisdom);
        }

        public bool Equals(AttributeSet other)
        {
            return other != null
                && Stamina == other.Stamina
                && Health == other.Health
                && Strength == other.Strength
                && Wisdom == other.Wisdom;
        }

        public override bool Equals(object obj) { return Equals(obj as AttributeSet); }

        public override int GetHashCode()
        {
            return Stamina * 8000 + Health * 400 + Strength * 20 + Wisdom;
        }

        public static readonly AttributeId[] AllIds =
        {
            AttributeId.Stamina, AttributeId.Health, AttributeId.Strength, AttributeId.Wisdom
        };
    }
}
