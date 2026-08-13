using Lingyan.Core.Combat;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class CombatTests
    {
        private static CombatActor Fighter(int health = 8, int stamina = 8, int strength = 6)
        {
            return new CombatActor("test", health, stamina, strength);
        }

        [Test]
        public void Attack_HasStartupActiveRecovery_Windows()
        {
            CombatActor actor = Fighter();
            Assert.That(actor.TryLight(), Is.True);
            AttackFrames frames = actor.CurrentAttack;

            // 前摇期间无判定
            for (int i = 0; i < frames.Startup; i++)
            {
                Assert.That(actor.AttackActive, Is.False, "前摇第 " + i + " 帧不应有判定");
                actor.Tick();
            }
            // 判定窗口
            for (int i = 0; i < frames.Active; i++)
            {
                Assert.That(actor.AttackActive, Is.True, "判定第 " + i + " 帧应活跃");
                actor.Tick();
            }
            // 后摇
            Assert.That(actor.AttackActive, Is.False, "后摇无判定");
            for (int i = 0; i < frames.Recovery; i++) { actor.Tick(); }
            Assert.That(actor.Action, Is.EqualTo(CombatAction.None), "动作结束回待机");
        }

        [Test]
        public void Stamina_GatesActions_AndRegens()
        {
            CombatActor actor = Fighter(stamina: 2); // 耐力 20
            Assert.That(actor.TryLight(), Is.True, "20 ≥ 15 可轻击");
            Assert.That(actor.Stamina, Is.EqualTo(5));

            // 动作中不可再出手
            Assert.That(actor.TryLight(), Is.False, "动作中");
            // 结束后耐力不足
            for (int i = 0; i < 30; i++) { actor.Tick(); }
            Assert.That(actor.Action, Is.EqualTo(CombatAction.None));
            Assert.That(actor.TryHeavy(), Is.False, "耐力不足拒绝重击");

            // 闲置回复：每 2 帧 1 点
            int before = actor.Stamina;
            for (int i = 0; i < 20; i++) { actor.Tick(); }
            Assert.That(actor.Stamina, Is.EqualTo(before + 10), "20 帧回 10 点");
        }

        [Test]
        public void Dodge_IFrames_AvoidDamage_ThenVulnerable()
        {
            CombatActor actor = Fighter();
            actor.TryDodge();
            // i-frame 窗口内无伤
            Assert.That(actor.Invulnerable, Is.True);
            Assert.That(actor.TakeHit(40), Is.EqualTo(0), "无敌帧内不吃伤害");
            Assert.That(actor.Hp, Is.EqualTo(actor.MaxHp));

            // 推过 i-frame（闪避仍在持续但无敌已过）
            for (int i = 0; i < CombatTuning.DodgeIFrames; i++) { actor.Tick(); }
            Assert.That(actor.Action, Is.EqualTo(CombatAction.Dodge), "还在闪避动作里");
            Assert.That(actor.Invulnerable, Is.False, "无敌窗口已过");
            Assert.That(actor.TakeHit(40), Is.EqualTo(40), "后半段吃满伤");
        }

        [Test]
        public void Block_Reduces70Percent_BreaksOnEmptyStamina()
        {
            CombatActor actor = Fighter(stamina: 2); // 耐力 20
            Assert.That(actor.SetBlocking(true), Is.True);
            int taken = actor.TakeHit(30);
            Assert.That(taken, Is.EqualTo(9), "格挡受 30%（向上取整）");
            Assert.That(actor.Stamina, Is.EqualTo(10), "格挡耗 10 耐力");

            actor.TakeHit(30);
            Assert.That(actor.Stamina, Is.EqualTo(0));
            Assert.That(actor.Action, Is.EqualTo(CombatAction.Staggered), "耐力见底破防");
        }

        [Test]
        public void Bout_ActiveWindowHitsOnce_NotPerFrame()
        {
            CombatActor a = Fighter(strength: 5); // 轻击 10 伤
            CombatActor b = Fighter();
            var bout = new CombatBout(a, b);
            a.TryLight();
            int hpBefore = b.Hp;
            for (int i = 0; i < 30; i++) { bout.Tick(); }
            Assert.That(hpBefore - b.Hp, Is.EqualTo(10),
                "4 帧判定窗口只结算一次命中，不是每帧一刀");
        }

        [Test]
        public void Stagger_InterruptsAction()
        {
            CombatActor actor = Fighter();
            actor.TryHeavy();
            actor.TakeHit(10);
            Assert.That(actor.Action, Is.EqualTo(CombatAction.Staggered), "受击打断出招");
            Assert.That(actor.AttackActive, Is.False);
            for (int i = 0; i < CombatTuning.StaggerFrames; i++) { actor.Tick(); }
            Assert.That(actor.Action, Is.EqualTo(CombatAction.None), "硬直结束");
        }

        [Test]
        public void Defeat_AtZeroHp()
        {
            CombatActor actor = Fighter(health: 1); // 10 HP
            actor.TakeHit(10);
            Assert.That(actor.Defeated, Is.True);
        }
    }
}
