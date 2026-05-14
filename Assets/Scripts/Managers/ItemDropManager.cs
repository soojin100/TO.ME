using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Items;

namespace TOME.Managers
{
    /// <summary>적 사망 콜백 + 주기 타이머로 필드에 ItemPickup 스폰 (풀링).</summary>
    public class ItemDropManager : MonoBehaviour
    {
        [SerializeField] GameObject itemPickupPrefab;
        [SerializeField] Transform  itemRoot;
        [SerializeField] float collectionY      = -7f;
        [SerializeField] float driftSpeed       = 2.5f;
        [SerializeField] float timedDropInterval = 6f;
        [SerializeField] Vector2 timedSpawnXRange = new(-2.5f, 2.5f);
        [SerializeField] float timedSpawnY       = 3f;
        [SerializeField] int   prewarm           = 6;

        ObjectPool _pool;
        EnemySO    _timedSource;
        Coroutine  _timedLoop;
        bool       _active;

        void Awake()
        {
            if (itemPickupPrefab)
                _pool = new ObjectPool(itemPickupPrefab, prewarm, itemRoot);
        }

        public void Begin(EnemySO timedDropSource)
        {
            _timedSource = timedDropSource;
            _active = true;
            if (CombatManager.I != null) CombatManager.I.OnEnemyKilled += OnEnemyKilled;
            _timedLoop = StartCoroutine(TimedDropLoop());
        }

        public void Stop()
        {
            _active = false;
            if (CombatManager.I != null) CombatManager.I.OnEnemyKilled -= OnEnemyKilled;
            if (_timedLoop != null) { StopCoroutine(_timedLoop); _timedLoop = null; }
        }

        void OnDestroy() => Stop();

        void OnEnemyKilled(EnemySO def, Vector3 pos)
        {
            if (!_active || def == null) return;
            if (Random.value > def.dropChance) return;
            var item = PickItem(def);
            if (item) SpawnPickup(item, pos);
        }

        IEnumerator TimedDropLoop()
        {
            var wait = new WaitForSeconds(timedDropInterval);
            while (_active)
            {
                yield return wait;
                if (!_active || _timedSource == null) continue;
                if (CombatManager.I != null && (CombatManager.I.IsFinished || CombatManager.I.IsPaused)) continue;
                var item = PickItem(_timedSource);
                if (item)
                {
                    float x = Random.Range(timedSpawnXRange.x, timedSpawnXRange.y);
                    SpawnPickup(item, new Vector3(x, timedSpawnY, 0f));
                }
            }
        }

        ItemSO PickItem(EnemySO def)
        {
            if (def.dropTable == null || def.dropTable.Length == 0) return null;
            if (def.dropWeights == null || def.dropWeights.Length != def.dropTable.Length)
                return def.dropTable[Random.Range(0, def.dropTable.Length)];

            float total = 0f;
            for (int i = 0; i < def.dropWeights.Length; i++) total += def.dropWeights[i];
            if (total <= 0f) return def.dropTable[Random.Range(0, def.dropTable.Length)];

            float r = Random.value * total;
            for (int i = 0; i < def.dropTable.Length; i++)
            {
                r -= def.dropWeights[i];
                if (r <= 0f) return def.dropTable[i];
            }
            return def.dropTable[def.dropTable.Length - 1];
        }

        void SpawnPickup(ItemSO item, Vector3 pos)
        {
            if (_pool == null) return;
            var go = _pool.Get(pos, Quaternion.identity);
            if (go.TryGetComponent<ItemPickup>(out var pick))
                pick.Init(item, pos, collectionY, driftSpeed, _pool);
        }
    }
}
