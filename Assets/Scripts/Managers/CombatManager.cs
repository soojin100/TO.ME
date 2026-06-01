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
        [SerializeField] int        enemyPrewarmCount = 4;

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

        ObjectPool enemyPool;
        readonly Dictionary<GameObject, EnemySO> instToDef = new(64);
        float savedFixedDt;
        StageSO stage;
        bool started;
        EnemySpawnEntry[] _entries;     // 이번 스테이지의 실제 스폰 목록 (수동 또는 자동)
        float _statMul = 1f;            // 자동 스폰 시 적 능력치 배수

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void OnDestroy() { if (I == this) I = null; }

        public void BeginStage(StageSO s)
        {
            stage = s;
            IsFinished = false;
            IsPaused   = false;
            TimeLeft   = s.timeLimit;
            Time.timeScale = 1f;

            // 하이브리드: spawns가 있으면 수동(스케일 X), 비면 difficulty+로스터로 자동 생성(스케일 O)
            bool manual = s.spawns != null && s.spawns.Length > 0;
            _entries = manual ? s.spawns : DifficultyScaler.BuildSpawns(s.difficulty, ResolveRoster(s));
            _statMul = manual ? 1f : DifficultyScaler.StatMultiplier(s.difficulty);

            TotalEnemies = 0;
            foreach (var e in _entries) TotalEnemies += e.totalCount;
            RemainingToKill = TotalEnemies;
            AliveOnField    = 0;

            // 풀 예열 — 모든 적이 동일 프리팹을 쓰므로 단일 풀
            if (enemyPool == null && enemyPrefab)
                enemyPool = new ObjectPool(enemyPrefab, enemyPrewarmCount, enemyRoot);

            OnCountChanged?.Invoke(RemainingToKill, TotalEnemies);
            OnTimerChanged?.Invoke(TimeLeft);

            foreach (var e in _entries) StartCoroutine(SpawnLoop(e));
            started = true;
        }

        // 자동 스폰용 적 로스터 결정: 스테이지 오버라이드 > 챕터 로스터
        EnemySO[] ResolveRoster(StageSO s)
        {
            if (s == null) return null;
            if (s.enemyRoster != null && s.enemyRoster.Length > 0) return s.enemyRoster;
            if (s.chapter != null && s.chapter.enemyRoster != null && s.chapter.enemyRoster.Length > 0)
                return s.chapter.enemyRoster;
            return null;
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
            if (!def || enemyPool == null) return;
            float x = UnityEngine.Random.Range(spawnXRange.x, spawnXRange.y);
            var go = enemyPool.Get(new Vector3(x, spawnY, 0f), Quaternion.identity);

            // SpriteRenderer 정의 반영 (프리팹의 SR 재사용)
            if (go.TryGetComponent<SpriteRenderer>(out var sr) && def.sprite) sr.sprite = def.sprite;

            instToDef[go] = def;

            if (go.TryGetComponent<EnemyBase>(out var eb))
                eb.Init(def, go.transform.position, OnEnemyDied, _statMul);

            AliveOnField++;
        }

        void OnEnemyDied(EnemyBase e)
        {
            AliveOnField    = Mathf.Max(0, AliveOnField - 1);
            RemainingToKill = Mathf.Max(0, RemainingToKill - 1);
            OnCountChanged?.Invoke(RemainingToKill, TotalEnemies);
            // (제거) enemySfx는 효과음이 아니라 To.You의 긴 트랙(Enemy.mp3 3.7MB)이라 BGM과 겹쳐서 재생 안 함. 짧은 효과음 생기면 교체.

            var go = e.gameObject;
            if (instToDef.TryGetValue(go, out var def))
                OnEnemyKilled?.Invoke(def, go.transform.position);

            if (enemyPool != null) enemyPool.Release(go);
            else go.SetActive(false);

            if (RemainingToKill == 0) Finish(true);
        }

        void Update()
        {
            if (!started || IsFinished || IsPaused) return;
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

        public void Finish(bool win)
        {
            if (IsFinished) return;
            IsFinished = true;
            IsPaused = false;
            started = false;
            AudioListener.pause = false;
            Time.fixedDeltaTime = savedFixedDt > 0f ? savedFixedDt : 0.02f;
            Time.timeScale = 0f;
            OnFinished?.Invoke(win);
        }
    }
}
