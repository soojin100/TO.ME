using System;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Combat;
using TOME.Gameplay.Player;

namespace TOME.Gameplay.Enemy
{
    /// <summary>풀 재사용 가능. Init/Despawn 호출만으로 재활용. 매 프레임 Transform 캐시 사용.</summary>
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] Transform hpFillPivot;   // 왼쪽 정렬 채움 피벗. localScale.x = HP 비율

        public EnemySO Def { get; private set; }
        public int Hp { get; private set; }
        public bool IsAlive => _alive;

        Action<EnemyBase> onDeath;
        Transform _tr;
        PlayerShell _player;
        Transform _playerTr;
        float _attackCooldown;
        float _rangedCooldown;
        bool _alive;
        float _statMul = 1f;     // 난이도 능력치 배수 (HP/공격)

        const float ContactDist   = 0.6f;
        const float AttackPeriod  = 1.0f;   // 접촉 후 1초 간격으로 데미지

        void Awake() { _tr = transform; }

        public void Init(EnemySO def, Vector3 spawnPos, Action<EnemyBase> deathCb, float statMul = 1f)
        {
            Def      = def;
            _statMul = Mathf.Max(0.01f, statMul);
            Hp       = Mathf.Max(1, Mathf.RoundToInt(def.hp * _statMul));
            onDeath  = deathCb;
            _tr.position = spawnPos;
            _attackCooldown = 0f;
            _rangedCooldown = def != null ? def.rangedCooldown * 0.5f : 0f;  // 스폰 직후 즉발 방지
            _alive   = true;

            _player   = FindPlayer();
            _playerTr = _player ? _player.transform : null;

            UpdateHpBar();
            gameObject.SetActive(true);
            EnemyRegistry.Register(this);
        }

        void UpdateHpBar()
        {
            if (!hpFillPivot) return;
            float max = (Def != null && Def.hp > 0) ? Def.hp * _statMul : 1f;
            var s = hpFillPivot.localScale;
            s.x = Mathf.Clamp01(Hp / max);
            hpFillPivot.localScale = s;
        }

        static PlayerShell _cachedPlayer;
        static PlayerShell FindPlayer()
        {
            if (_cachedPlayer) return _cachedPlayer;
            _cachedPlayer = UnityEngine.Object.FindAnyObjectByType<PlayerShell>();
            return _cachedPlayer;
        }

        public void TakeDamage(int dmg)
        {
            if (!_alive) return;
            Hp -= dmg;
            UpdateHpBar();
            if (Hp <= 0) Die();
        }

        void Update()
        {
            if (!_alive || !_playerTr) return;

            Vector3 to = _playerTr.position - _tr.position;
            float distSq = to.x * to.x + to.y * to.y;

            // 원거리: 사거리 안이면 쿨다운마다 발사. 추적/접촉과 동시에 가능.
            if (Def != null && Def.hasRangedAttack && Def.bulletPrefab != null)
            {
                _rangedCooldown -= Time.deltaTime;
                if (_rangedCooldown <= 0f && distSq <= Def.rangedRange * Def.rangedRange)
                {
                    FireBullet(to);
                    _rangedCooldown = Mathf.Max(0.1f, Def.rangedCooldown);
                }
            }

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
                    if (_player) _player.TakeDamage(Mathf.Max(0.01f, Def.atk * _statMul));
                    _attackCooldown = AttackPeriod;
                }
            }
        }

        void FireBullet(Vector3 toPlayer)
        {
            var go = UnityEngine.Object.Instantiate(Def.bulletPrefab, _tr.position, Quaternion.identity);
            if (go.TryGetComponent<EnemyBullet>(out var b))
                b.Launch(new Vector2(toPlayer.x, toPlayer.y), Def.bulletSpeed, Mathf.Max(0.01f, Def.rangedDamage));
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
