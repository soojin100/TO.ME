using TOME.Managers;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("--- 소환할 몬스터 프리패브 ---")]
    public GameObject enemyPrefab;

    [Header("--- 스폰 설정 (유니티 창에서 수정 가능) ---")]
    [Tooltip("스폰할 총 몬스터 마리 수")]
    public int maxSpawnCount = 3;      // [핵심] 인스펙터에서 마리 수 조절 가능! 기본값 3

    [Tooltip("스폰 간격 (초)")]
    public float spawnInterval = 2.0f;

    // 상단 몬스터 구역 좌표 (Y: 3.6 ~ 9.6 마진 반영)
    private float minX = -4.5f;
    private float maxX = 4.5f;
    private float minY = 1.0f;
    private float maxY = 9.0f;

    private float timer = 0f;
    private int currentSpawnCount = 0; // 현재까지 소환된 마리 수 체크용

    void Start()
    {
        // 씬이 시작될 때 카운트 초기화
        currentSpawnCount = 0;
        timer = 0f;
    }

    void Update()
    {
        // 시간은 계속 흘러가게 둡니다.
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            // [수정 포인트] 진짜 몬스터를 만들기 "직전"에 현재 마리 수를 최종 검사합니다!
            // 만약 이미 3마리를 다 채웠다면, 여기서 커트라인에 걸려 생성(SpawnMonster)을 안 하고 리턴됩니다.
            if (currentSpawnCount >= maxSpawnCount) return;

            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        // 몬스터 구역 내부의 무작위 좌표 계산
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

        // 몬스터 실시간 생성
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        currentSpawnCount++;

        // 소환 카운트 1 증가
        if (StageGameManager.Instance != null)
        {
            StageGameManager.Instance.OnEnemySpawned();
        }
    }

    // 외부(예: StageManager)에서 다음 스테이지로 넘어갈 때 마리 수를 리셋하고 다시 켜주는 함수
    public void ResetSpawner(int newMaxCount)
    {
        maxSpawnCount = newMaxCount;
        currentSpawnCount = 0;
        timer = 0f;
    }
}