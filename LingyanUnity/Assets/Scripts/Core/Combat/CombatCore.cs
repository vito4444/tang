using System;

namespace Lingyan.Core.Combat
{
    /// <summary>
    /// 即时轻动作战斗的判定内核（规格第十三节阶段 7：轻击/重击/格挡减伤/
    /// 闪避 i-frame/耐力条）。全部以逻辑帧（60fps 语义）推进，纯 C# 可测；
    /// Unity 表现层只负责把输入喂进来、把状态画出去。
    /// </summary>
    public enum CombatAction
    {
        None = 0,
        LightAttack = 1,
        HeavyAttack = 2,
        Dodge = 3,
        Block = 4,
        Staggered = 5
    }

    /// <summary>一个攻击动作的帧数据：前摇 / 判定 / 后摇。</summary>
    public sealed class AttackFrames
    {
        public int Startup { get; }
        public int Active { get; }
        public int Recovery { get; }
        public int Damage { get; }
        public int StaminaCost { get; }

        public AttackFrames(int startup, int active, int recovery, int damage, int staminaCost)
        {
            Startup = startup;
            Active = active;
            Recovery = recovery;
            Damage = damage;
            StaminaCost = staminaCost;
        }

        public int Total { get { return Startup + Active + Recovery; } }
    }

    public static class CombatTuning
    {
        public const int DodgeDurationFrames = 22;

        /// <summary>闪避无敌窗口：自第 1 帧起。</summary>
        public const int DodgeIFrames = 12;

        public const int DodgeStaminaCost = 20;

        /// <summary>格挡减伤系数（受 30%）。</summary>
        public const double BlockDamageFactor = 0.30;

        public const int BlockStaminaPerHit = 10;

        public const int StaggerFrames = 18;

        /// <summary>非动作帧的耐力回复（每帧 0.5，整数化为每 2 帧 1 点）。</summary>
        public const int StaminaRegenPer2Frames = 1;

        public static AttackFrames Light(int strength)
        {
            return new AttackFrames(
                startup: 8, active: 4, recovery: 12,
                damage: strength * 2, staminaCost: 15);
        }

        public static AttackFrames Heavy(int strength)
        {
            return new AttackFrames(
                startup: 20, active: 6, recovery: 24,
                damage: strength * 4, staminaCost: 30);
        }

        public static int MaxHp(int health)
        {
            return health * 10;
        }

        public static int MaxStamina(int stamina)
        {
            return stamina * 10;
        }
    }

    /// <summary>战斗中的一方。</summary>
    public sealed class CombatActor
    {
        public string Id { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int MaxStamina { get; }
        public int Stamina { get; private set; }
        public int Strength { get; }

        public CombatAction Action { get; private set; } = CombatAction.None;

        /// <summary>当前动作已经历的帧数。</summary>
        public int ActionFrame { get; private set; }

        private AttackFrames _attack;
        private int _regenTick;

        public bool Defeated { get { return Hp <= 0; } }

        public CombatActor(string id, int health, int stamina, int strength)
        {
            Id = id;
            MaxHp = CombatTuning.MaxHp(health);
            Hp = MaxHp;
            MaxStamina = CombatTuning.MaxStamina(stamina);
            Stamina = MaxStamina;
            Strength = strength;
        }

        /// <summary>当前攻击帧数据（非攻击态为 null）。</summary>
        public AttackFrames CurrentAttack { get { return _attack; } }

        /// <summary>攻击判定是否活跃（active 窗口内）。</summary>
        public bool AttackActive
        {
            get
            {
                if (_attack == null) { return false; }
                return ActionFrame >= _attack.Startup
                    && ActionFrame < _attack.Startup + _attack.Active;
            }
        }

        /// <summary>闪避无敌是否生效。</summary>
        public bool Invulnerable
        {
            get
            {
                return Action == CombatAction.Dodge
                    && ActionFrame < CombatTuning.DodgeIFrames;
            }
        }

        public bool Busy
        {
            get
            {
                return Action == CombatAction.LightAttack
                    || Action == CombatAction.HeavyAttack
                    || Action == CombatAction.Dodge
                    || Action == CombatAction.Staggered;
            }
        }

        public bool TryLight()
        {
            return TryAttack(CombatAction.LightAttack, CombatTuning.Light(Strength));
        }

        public bool TryHeavy()
        {
            return TryAttack(CombatAction.HeavyAttack, CombatTuning.Heavy(Strength));
        }

        private bool TryAttack(CombatAction kind, AttackFrames frames)
        {
            if (Busy || Stamina < frames.StaminaCost) { return false; }
            Action = kind;
            _attack = frames;
            ActionFrame = 0;
            Stamina -= frames.StaminaCost;
            return true;
        }

        public bool TryDodge()
        {
            if (Busy || Stamina < CombatTuning.DodgeStaminaCost) { return false; }
            Action = CombatAction.Dodge;
            _attack = null;
            ActionFrame = 0;
            Stamina -= CombatTuning.DodgeStaminaCost;
            return true;
        }

        /// <summary>格挡为持续态：抬盾/收盾即时，只要不在别的动作里。</summary>
        public bool SetBlocking(bool on)
        {
            if (on)
            {
                if (Busy) { return false; }
                Action = CombatAction.Block;
                _attack = null;
                ActionFrame = 0;
                return true;
            }
            if (Action == CombatAction.Block)
            {
                Action = CombatAction.None;
            }
            return true;
        }

        /// <summary>逻辑帧推进：动作计帧、结束回 Idle、闲时回耐力。</summary>
        public void Tick()
        {
            switch (Action)
            {
                case CombatAction.LightAttack:
                case CombatAction.HeavyAttack:
                    ActionFrame++;
                    if (ActionFrame >= _attack.Total)
                    {
                        Action = CombatAction.None;
                        _attack = null;
                    }
                    break;
                case CombatAction.Dodge:
                    ActionFrame++;
                    if (ActionFrame >= CombatTuning.DodgeDurationFrames)
                    {
                        Action = CombatAction.None;
                    }
                    break;
                case CombatAction.Staggered:
                    ActionFrame++;
                    if (ActionFrame >= CombatTuning.StaggerFrames)
                    {
                        Action = CombatAction.None;
                    }
                    break;
                default:
                    _regenTick++;
                    if (_regenTick >= 2)
                    {
                        _regenTick = 0;
                        if (Stamina < MaxStamina)
                        {
                            Stamina += CombatTuning.StaminaRegenPer2Frames;
                        }
                    }
                    break;
            }
        }

        /// <summary>受击结算。返回实际伤害（0 = 闪避成功）。</summary>
        public int TakeHit(int damage)
        {
            if (Invulnerable) { return 0; }
            if (Action == CombatAction.Block)
            {
                int reduced = (int)Math.Ceiling(damage * CombatTuning.BlockDamageFactor);
                Hp -= reduced;
                Stamina = Math.Max(0, Stamina - CombatTuning.BlockStaminaPerHit);
                if (Stamina == 0)
                {
                    // 破防
                    Action = CombatAction.Staggered;
                    ActionFrame = 0;
                }
                return reduced;
            }
            Hp -= damage;
            Action = CombatAction.Staggered;
            ActionFrame = 0;
            _attack = null;
            return damage;
        }
    }

    /// <summary>双人对决的最小裁判：谁的判定帧碰上谁，一次判定只结算一回。</summary>
    public sealed class CombatBout
    {
        public CombatActor A { get; }
        public CombatActor B { get; }

        private bool _aHitConsumed;
        private bool _bHitConsumed;

        public CombatBout(CombatActor a, CombatActor b)
        {
            A = a;
            B = b;
        }

        /// <summary>推进一帧：双方 Tick，判定窗口内各结算一次命中。</summary>
        public void Tick(bool inRange = true)
        {
            A.Tick();
            B.Tick();

            if (A.AttackActive && !_aHitConsumed && inRange)
            {
                _aHitConsumed = true;
                B.TakeHit(A.CurrentAttack.Damage);
            }
            if (!A.AttackActive) { _aHitConsumed = false; }

            if (B != null && B.AttackActive && !_bHitConsumed && inRange)
            {
                _bHitConsumed = true;
                A.TakeHit(B.CurrentAttack.Damage);
            }
            if (B == null || !B.AttackActive) { _bHitConsumed = false; }
        }
    }
}
