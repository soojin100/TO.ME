using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Progression;
namespace TOME.Combat
{
    /// <summary>적 스폰·타이머·일시정지·승패. EnemySO 별 ObjectPool 캐시.
    ///
    /// 스테이지는 라운드로 나뉜다. 한 라운드의 적을 전부 쓰러뜨리면 다음 라운드가 시작되고,
    /// 마지막 라운드를 끝내야 클리어다. StageSO.rounds가 비어 있으면 기존 spawns/difficulty로
    /// 한 라운드짜리 스테이지가 되어 동작이 이전과 같다.</summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager I { get; private set; }

        [SerializeField] Transform  enemyRoot;
        [SerializeField] GameObject enemyPrefab;       // EnemyBase + SpriteRenderer + Collider2D
        [SerializeField] Vector2    spawnXRange = new(-3f, 3f);
        [SerializeField] float      spawnY = 4.5f;
        [SerializeField] int        enemyPrewarmCount = 4;

        /// <summary>현재 라운드의 총 적 수. (스테이지 전체가 아니라 라운드 기준)</summary>
        public int   TotalEnemies   { get; private set; }
        /// <summary>현재 라운드에서 남은 적 수.</summary>
        public int   RemainingToKill{ get; private set; }
        /// <summary>현재 라운드 번호(1부터).</summary>
        public int   RoundNumber    { get; private set; }
        /// <summary>이 스테이지의 총 라운드 수.</summary>
        public int   RoundCount     { get; private set; }
        /// <summary>현재 라운드 표시 이름.</summary>
        public string RoundLabel    { get; private set; }
        public int   AliveOnField   { get; private set; }
        public float TimeLeft       { get; private set; }
        public bool  IsPaused       { get; private set; }
        public bool  IsFinished     { get; private set; }

        public event Action<int,int> OnCountChanged;
        public event Action<float>   OnTimerChanged;
        public event Action<bool>    OnFinished;
        public event Action<EnemySO,Vector3> OnEnemyKilled;
        /// <summary>라운드가 시작될 때. (라운드 번호, 총 라운드 수, 표시 이름)</summary>
        public event Action<int,int,string> OnRoundChanged;

        ObjectPool enemyPool;
        readonly Dictionary<GameObject, EnemySO> instToDef = new(64);
        float savedFixedDt;
        StageSO stage;
        bool started;
        EnemySpawnEntry[] _entries;     // 현재 라운드의 실제 스폰 목록 (수동 또는 자동)
        float _statMul = 1f;            // 자동 스폰 시 적 능력치 배수
        StageRound[] _rounds;           // 이번 스테이지의 라운드 구성
        int _roundIndex;                // 0-based
        int _roundToken;                // 라운드가 바뀌면 증가 — 이전 라운드의 스폰 루프를 무효화한다
        bool _advancing;                // 라운드 전환 중 중복 진행 방지

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

            _rounds     = BuildRounds(s);
            RoundCount  = _rounds.Length;
            _roundIndex = -1;
            _advancing  = false;

            // 풀 예열 — 모든 적이 동일 프리팹을 쓰므로 단일 풀
            if (enemyPool == null && enemyPrefab)
                enemyPool = new ObjectPool(enemyPrefab, enemyPrewarmCount, enemyRoot);

            OnTimerChanged?.Invoke(TimeLeft);
            started = true;
            StartCoroutine(StartRound(0));
        }

        /// <summary>스테이지의 라운드 구성을 만든다. rounds가 비어 있으면 기존 spawns/difficulty로
        /// 한 라운드짜리를 합성해 예전과 같은 동작을 유지한다.</summary>
        static StageRound[] BuildRounds(StageSO s)
        {
            if (s.rounds != null && s.rounds.Length > 0) return s.rounds;
            return new[] { new StageRound {
                label = null, difficulty = s.difficulty,
                enemyRoster = s.enemyRoster, spawns = s.spawns,
                startDelay = 0f, timeBonus = 0f
            } };
        }

        IEnumerator StartRound(int index)
        {
            if (index < 0 || index >= _rounds.Length) yield break;

            var round = _rounds[index];
            if (round.startDelay > 0f) yield return new WaitForSeconds(round.startDelay);
            if (IsFinished) yield break;

            _roundIndex = index;
            _roundToken++;
            int token = _roundToken;
            _advancing = false;

            RoundNumber = index + 1;
            RoundLabel  = string.IsNullOrWhiteSpace(round.label) ? $"라운드 {RoundNumber}" : round.label;

            // 라운드가 늘어난 만큼 제한시간도 얹어 준다.
            if (round.timeBonus > 0f)
            {
                TimeLeft += round.timeBonus;
                OnTimerChanged?.Invoke(TimeLeft);
            }

            // 하이브리드: spawns가 있으면 수동(스케일 X), 비면 difficulty+로스터로 자동 생성(스케일 O)
            bool manual = round.spawns != null && round.spawns.Length > 0;
            _entries = manual ? round.spawns
                              : DifficultyScaler.BuildSpawns(round.difficulty, ResolveRoster(stage, round));
            _statMul = manual ? 1f : DifficultyScaler.StatMultiplier(round.difficulty);

            TotalEnemies = 0;
            foreach (var e in _entries) TotalEnemies += e.totalCount;
            RemainingToKill = TotalEnemies;
            AliveOnField    = 0;

            OnRoundChanged?.Invoke(RoundNumber, RoundCount, RoundLabel);
            OnCountChanged?.Invoke(RemainingToKill, TotalEnemies);

            // 적이 하나도 없는 라운드는 곧바로 다음으로 넘긴다(데이터 실수로 진행이 멈추지 않게).
            if (TotalEnemies <= 0)
            {
                Debug.LogWarning($"[Combat] {RoundLabel} 에 적이 없습니다. 다음 라운드로 넘어갑니다.", this);
                AdvanceRound();
                yield break;
            }

            foreach (var e in _entries) StartCoroutine(SpawnLoop(e, token));
        }

        /// <summary>현재 라운드를 끝내고 다음 라운드로. 마지막 라운드였으면 스테이지 클리어.</summary>
        void AdvanceRound()
        {
            if (_advancing || IsFinished) return;
            _advancing = true;
            _roundToken++;                       // 남아 있는 스폰 루프 무효화
            if (_roundIndex + 1 < _rounds.Length) StartCoroutine(StartRound(_roundIndex + 1));
            else Finish(true);
        }

        // 자동 스폰용 적 로스터 결정: 라운드 > 스테이지 > 챕터
        static EnemySO[] ResolveRoster(StageSO s, StageRound round)
        {
            if (round != null && round.enemyRoster != null && round.enemyRoster.Length > 0) return round.enemyRoster;
            if (s == null) return null;
            if (s.enemyRoster != null && s.enemyRoster.Length > 0) return s.enemyRoster;
            if (s.chapter != null && s.chapter.enemyRoster != null && s.chapter.enemyRoster.Length > 0)
                return s.chapter.enemyRoster;
            return null;
        }

        // token 이 바뀌면(=라운드가 넘어가면) 이 루프는 즉시 멈춘다. 이전 라운드 적이 새 라운드에 섞이지 않게.
        IEnumerator SpawnLoop(EnemySpawnEntry e, int token)
        {
            if (e.startDelay > 0f) yield return new WaitForSeconds(e.startDelay);
            int spawned = 0;
            while (spawned < e.totalCount && !IsFinished && token == _roundToken)
            {
                while (AliveOnField >= e.simultaneous && !IsFinished && token == _roundToken) yield return null;
                if (IsFinished || token != _roundToken) yield break;
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

            if (RemainingToKill == 0) AdvanceRound();
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
