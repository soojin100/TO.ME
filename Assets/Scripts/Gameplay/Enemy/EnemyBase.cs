using System;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Player;

namespace TOME.Gameplay.Enemy
{
    /// <summary>풀 재사용 가능. Init/Despawn 호출만으로 재활용. 매 프레임 Transform 캐시 사용.</summary>
    public class EnemyBase : MonoBehaviour
    {
        public EnemySO Def { get; private set; }
        public int Hp { get; private set; }
        public bool IsAlive => _alive;

        Action<EnemyBase> onDeath;
        Transform _tr;
        PlayerShell _player;
        Transform _playerTr;
        float _attackCooldown;
        bool _alive;

        const float ContactDist   = 0.6f;
        const float AttackPeriod  = 1.0f;   // 접촉 후 1초 간격으로 데미지

        void Awake() { _tr = transform; }

        public void Init(EnemySO def, Vector3 spawnPos, Action<EnemyBase> deathCb)
        {
            Def      = def;
            Hp       = def.hp;
            onDeath  = deathCb;
            _tr.position = spawnPos;
            _attackCooldown = 0f;
            _alive   = true;

            _player   = FindPlayer();
            _playerTr = _player ? _player.transform : null;

            gameObject.SetActive(true);
            EnemyRegistry.Register(this);
        }

        static PlayerShell _cachedPlayer;
        static PlayerShell FindPlayer()
        {
            if (_cachedPlayer) return _cachedPlayer;
            _cachedPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerShell>();
            return _cachedPlayer;
        }

        public void TakeDamage(int dmg)
        {
            if (!_alive) return;
            Hp -= dmg;
            if (Hp <= 0) Die();
        }

        void Update()
        {
            if (!_alive || !_playerTr) return;

            Vector3 to = _playerTr.position - _tr.position;
            float distSq = to.x * to.x + to.y * to.y;

            if (distSq > ContactDist * ContactDist)
            {
                // 추적: moveSpeed 단위 이동
                float inv = 1f / Mathf.Sqrt(distSq);
                _tr.position += new Vector3(to.x * inv, to.y * inv, 0f) * Def.moveSpeed * Time.deltaTime;
            }
            else
            {
                // 접촉 데미지
                _attackCooldown -= Time.deltaTime;
                if (_attackCooldown <= 0f)
                {
                    if (_player) _player.TakeDamage(Def.atk);
                    _attackCooldown = AttackPeriod;
                }
            }
        }

        void Die()
        {
            if (!_alive) return;
            _alive = false;
            EnemyRegistry.Unregister(this);
            onDeath?.Invoke(this);
            // 풀로 반환은 onDeath 콜백(CombatManager) 측에서 수행
        }

        public void Despawn()
        {
            _alive = false;
            EnemyRegistry.Unregister(this);
            gameObject.SetActive(false);
        }

        void OnDisable() { EnemyRegistry.Unregister(this); }
    }
}
