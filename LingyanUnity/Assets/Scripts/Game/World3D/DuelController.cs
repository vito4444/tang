using System;
using Lingyan.Core.Combat;
using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 切磋对决的表现层：把输入喂给 Core 判定内核（CombatBout），
    /// 把状态画出去（位置、朝向、受击闪缩、耐力/血量由 HUD 读取）。
    /// 操作：J 轻击 / K 重击 / 空格闪避 / 左 Shift 格挡 / A·D 绕行。
    /// 非致命：任一方血量归零即分胜负，回调结算（好感与江湖名望）。
    /// </summary>
    public sealed class DuelController : MonoBehaviour
    {
        public CombatActor Player { get; private set; }
        public CombatActor Rival { get; private set; }

        private Transform _playerBody;
        private Transform _rivalBody;
        private Vector3 _arenaCenter;
        private float _playerAngleDeg;
        private float _range = 2.2f;
        private Action<bool> _onFinished;
        private bool _finished;

        // 简易陪练脑：数帧出招
        private int _aiCooldown;
        private System.Random _aiRandom = new System.Random(12);

        public void Begin(
            Transform playerBody, Transform rivalBody, Vector3 arenaCenter,
            int playerHealth, int playerStamina, int playerStrength,
            int rivalProwess, Action<bool> onFinished)
        {
            Player = new CombatActor("player", playerHealth, playerStamina, playerStrength);
            // 陪练三维由身手折算
            int rivalStat = Mathf.Max(3, rivalProwess / 10);
            Rival = new CombatActor("rival", rivalStat, rivalStat, rivalStat);
            _playerBody = playerBody;
            _rivalBody = rivalBody;
            _arenaCenter = arenaCenter;
            _playerAngleDeg = 180f;
            _onFinished = onFinished;
            _finished = false;
            enabled = true;
        }

        private void Update()
        {
            if (_finished || Player == null) { return; }

            // 输入 → 内核
            if (Input.GetKey(KeyCode.J)) { Player.TryLight(); }
            if (Input.GetKey(KeyCode.K)) { Player.TryHeavy(); }
            if (Input.GetKey(KeyCode.Space)) { Player.TryDodge(); }
            Player.SetBlocking(Input.GetKey(KeyCode.LeftShift));

            float orbit = (Input.GetKey(KeyCode.D) ? 1f : 0f)
                        - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            if (orbit != 0f && !Player.Busy)
            {
                _playerAngleDeg += orbit * Time.deltaTime * 70f;
            }

            // 陪练脑
            _aiCooldown--;
            if (_aiCooldown <= 0 && !Rival.Busy)
            {
                int roll = _aiRandom.Next(100);
                if (roll < 40) { Rival.TryLight(); }
                else if (roll < 55) { Rival.TryHeavy(); }
                else if (roll < 70) { Rival.TryDodge(); }
                else { Rival.SetBlocking(roll < 85); }
                _aiCooldown = 30 + _aiRandom.Next(50);
            }

            // 内核推帧（每渲染帧一逻辑帧；60fps 语义）
            var bout = new CombatBout(Player, Rival);
            bout.Tick(inRange: true);

            // 摆位：玩家绕场心站位，陪练对面
            float rad = _playerAngleDeg * Mathf.Deg2Rad;
            Vector3 playerPos = _arenaCenter
                + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * (_range / 2f);
            Vector3 rivalPos = _arenaCenter
                + new Vector3(Mathf.Sin(rad + Mathf.PI), 0f, Mathf.Cos(rad + Mathf.PI))
                    * (_range / 2f);
            _playerBody.position = playerPos + ActionOffset(Player, rivalPos - playerPos);
            _rivalBody.position = rivalPos + ActionOffset(Rival, playerPos - rivalPos);
            _playerBody.LookAt(rivalPos);
            _rivalBody.LookAt(playerPos);

            // 受击/动作的形变反馈（无动画资产阶段的表现手段）
            ApplyPose(_playerBody, Player);
            ApplyPose(_rivalBody, Rival);

            if (Player.Defeated || Rival.Defeated)
            {
                _finished = true;
                enabled = false;
                _onFinished?.Invoke(Rival.Defeated);
            }
        }

        /// <summary>出招前倾、闪避侧撤的位移表达。</summary>
        private static Vector3 ActionOffset(CombatActor actor, Vector3 toward)
        {
            Vector3 dir = toward.normalized;
            switch (actor.Action)
            {
                case CombatAction.LightAttack:
                case CombatAction.HeavyAttack:
                    return dir * (actor.AttackActive ? 0.55f : 0.2f);
                case CombatAction.Dodge:
                    return dir * -0.7f;
                case CombatAction.Staggered:
                    return dir * -0.3f;
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>姿态：格挡下蹲、硬直矮缩、无敌帧半透明由材质层后续接。</summary>
        private static void ApplyPose(Transform body, CombatActor actor)
        {
            float squash = 1f;
            if (actor.Action == CombatAction.Block) { squash = 0.85f; }
            else if (actor.Action == CombatAction.Staggered) { squash = 0.72f; }
            body.localScale = new Vector3(1f, squash, 1f);
        }
    }
}
