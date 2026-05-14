using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Gameplay.Enemy;

namespace TOME.Managers
{
    /// <summary>적 스폰·타이머·일시정지·승패. EnemySO 별 ObjectPool 캐시.</summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager I { get; private set; }

        [SerializeField] Transform  enemyRoot;
        [SerializeField] GameObject enemyPrefab;       // EnemyBase + SpriteRenderer + Collider2D
        [SerializeField] Vector2    spawnXRange = new(-3f, 3f);
        [SerializeField] float      spawnY = 4.5f;
        [SerializeField] int        prewarmPerType = 4;

        public int   TotalEnemies   { get; private set; }
        public int   RemainingToKill{ get; private set; }
        public int   AliveOnField   { get; private set; }
        public float TimeLeft       { get; private set; }
        public bool  IsPaused       { get; private set; }
        public bool  IsFinished     { get; private set; }

        public event Action<int,int> OnCountChanged;
        public event Action<float>   OnTimerChanged;
        public event Action<bool>    OnFinished;
        public event Action<EnemySO,Vector3> OnEnemyKilled;

        readonly Dictionary<EnemySO, ObjectPool> pools = new(8);
        readonly Dictionary<GameObject, EnemySO> instToDef = new(64);
        float savedFixedDt;
        StageSO stage;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void BeginStage(StageSO s)
        {
            stage = s;
            IsFinished = false;
            TimeLeft   = s.timeLimit;

            TotalEnemies = 0;
            foreach (var e in s.spawns) TotalEnemies += e.totalCount;
            RemainingToKill = TotalEnemies;
            AliveOnField    = 0;

            // 풀 예열
            foreach (var e in s.spawns)
                if (e.enemy && !pools.ContainsKey(e.enemy))
                    pools[e.enemy] = new ObjectPool(enemyPrefab, prewarmPerType, enemyRoot);

            OnCountChanged?.Invoke(RemainingToKill, TotalEnemies);
            OnTimerChanged?.Invoke(TimeLeft);

            foreach (var e in s.spawns) StartCoroutine(SpawnLoop(e));
        }

        IEnumerator SpawnLoop(EnemySpawnEntry e)
        {
            if (e.startDelay > 0f) yield return new WaitForSeconds(e.startDelay);
            int spawned = 0;
            while (spawned < e.totalCount && !IsFinished)
            {
                while (AliveOnField >= e.simultaneous && !IsFinished) yield return null;
                if (IsFinished) yield break;
                SpawnOne(e.enemy);
                spawned++;
                yield return new WaitForSeconds(e.spawnInterval);
            }
        }

        void SpawnOne(EnemySO def)
        {
            if (!def || !pools.TryGetValue(def, out var pool)) return;
            float x = UnityEngine.Random.Range(spawnXRange.x, spawnXRange.y);
            var go = pool.Get(new Vector3(x, spawnY, 0f), Quaternion.identity);

            // SpriteRenderer 정의 반영 (프리팹의 SR 재사용)
            if (go.TryGetComponent<SpriteRenderer>(out var sr) && def.sprite) sr.sprite = def.sprite;

            instToDef[go] = def;

            if (go.TryGetComponent<EnemyBase>(out var eb))
                eb.Init(def, go.transform.position, OnEnemyDied);

            AliveOnField++;
        }

        void OnEnemyDied(EnemyBase e)
        {
            AliveOnField    = Mathf.Max(0, AliveOnField - 1);
            RemainingToKill = Mathf.Max(0, RemainingToKill - 1);
            OnCountChanged?.Invoke(RemainingToKill, TotalEnemies);

            var go = e.gameObject;
            if (instToDef.TryGetValue(go, out var def))
            {
                OnEnemyKilled?.Invoke(def, go.transform.position);
                if (pools.TryGetValue(def, out var pool)) pool.Release(go);
                else go.SetActive(false);
            }
            else go.SetActive(false);

            if (RemainingToKill == 0) Finish(true);
        }

        void Update()
        {
            if (IsFinished || IsPaused) return;
            TimeLeft -= Time.deltaTime;
            OnTimerChanged?.Invoke(TimeLeft);
            if (TimeLeft <= 0f) Finish(false);
        }

        public void Pause()
        {
            if (IsPaused || IsFinished) return;
            IsPaused = true;
            savedFixedDt = Time.fixedDeltaTime;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = savedFixedDt > 0 ? savedFixedDt : 0.02f;
            AudioListener.pause = false;
        }

        void Finish(bool win)
        {
            if (IsFinished) return;
            IsFinished = true;
            IsPaused = false;
            AudioListener.pause = false;
            Time.fixedDeltaTime = savedFixedDt > 0f ? savedFixedDt : 0.02f;
            Time.timeScale = 0f;
            OnFinished?.Invoke(win);
        }
    }
}
