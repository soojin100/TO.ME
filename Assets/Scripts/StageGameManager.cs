using TMPro;
using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트 제어용

public class StageGameManager : MonoBehaviour
{
    // 싱글톤 패턴 (어디서나 접근하기 쉽게)
    public static StageGameManager Instance;

    [Header("--- UI 연결 ---")]
    public TextMeshProUGUI timerText;           // Text_Timer 연결
    public TextMeshProUGUI enemyCountText;      // Text_EnemyCount 연결

    [Header("--- 스테이지 설정 ---")]
    public float limitTime = 60f;    // 제한 시간 (60초)
    private float currentTimer;
    private bool isGamePlaying = true;

    [Header("--- 연동 스크립트 ---")]
    public EnemySpawner enemySpawner; // 우리가 만든 스포너 연결

    private int totalEnemiesToSpawn;  // 이번 스테이지에 나와야 할 총 마리 수
    private int currentAliveEnemies = 0; // 현재 화면에 살아있는 몬스터 수
    private int spawnedCount = 0;        // 현재까지 스폰된 마리 수

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        currentTimer = limitTime;

        if (enemySpawner != null)
        {
            // 이번 스테이지에 나와야 할 총 마리 수를 가져옴
            totalEnemiesToSpawn = enemySpawner.maxSpawnCount;
        }

        // [핵심 수정] 게임 시작하자마자 '현재 살아있는 적' 숫자를 0이 아니라 maxSpawnCount와 동일하게 세팅합니다.
        currentAliveEnemies = totalEnemiesToSpawn;

        UpdateUI();
    }

    void Update()
    {
        if (!isGamePlaying) return;

        // 1. 타이머 감소
        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            GameOver(false); // 패배 조건: 시간 초과
        }

        UpdateUI();
    }

    // UI 글자 갱신 기능
    void UpdateUI()
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(currentTimer).ToString(); // 숫자만 깔끔하게 출력 [cite: 584]
        }

        if (enemyCountText != null)
        {
            // [핵심 수정] 이제 왼쪽 숫자가 0에서 시작하지 않고, max 숫자인 3에서 시작해서 죽을 때마다 깎입니다.
            // 출력 예시: "남은적: 3 / 3" -> 유령 한 마리 잡으면 -> "남은적: 2 / 3"
            enemyCountText.text = "남은 적 " + currentAliveEnemies + " / " + totalEnemiesToSpawn;
        }
    }

    // 몬스터가 스폰될 때마다 카운트를 올려주는 함수 (MonsterSpawner에서 호출해 줄 것임)
    public void OnEnemySpawned()
    {
        spawnedCount++;
        //currentAliveEnemies++;
        UpdateUI();
    }

    // 몬스터가 죽을 때마다 카운트를 내려주는 함수 (MonsterAI에서 호출해 줄 것임)
    public void OnEnemyDied()
    {
        currentAliveEnemies--;
        UpdateUI();

        // [승리 조건 체크] 지정된 마리 수를 다 소환했고, 화면에 남은 적이 0마리일 때
        if (spawnedCount >= totalEnemiesToSpawn && currentAliveEnemies <= 0)
        {
            GameOver(true); // 승리!
        }
    }

    void GameOver(bool isVictory)
    {
        isGamePlaying = false;

        // [핵심 치트키] 유니티의 흐르는 시간을 0배속(정지)으로 만듭니다!
        // 이 코드가 실행되는 순간, Update의 Time.deltaTime이 0이 되어 모든 이동과 타이머가 멈춥니다.
        Time.timeScale = 0f;

        if (isVictory)
        {
            Debug.Log("STAGE CLEAR! 모든 적을 소멸시켰습니다.");
            // TODO: 기획서대로 캐릭터가 화면 중앙으로 가며 2초 뒤 돌아가기 버튼을 띄우는 연출 위치 [cite: 563]
    }
        else
        {
            Debug.Log("GAME OVER... 제한 시간이 초과되었습니다.");
            // TODO: 리트라이(다시시도) 팝업 UI 창 띄우기
        }
    }
}