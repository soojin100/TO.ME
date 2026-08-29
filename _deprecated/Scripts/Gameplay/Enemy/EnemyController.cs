using TOME.Managers;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    [Header("--- 능력치 설정 ---")]
    public float maxHp = 3f;
    private float currentHp;

    [Header("--- UI 설정 ---")]
    public Slider hpSlider;

    [Header("--- 이동 설정 ---")]
    public float speed = 3.0f;
    public float minWaitTime = 0.5f;
    public float maxWaitTime = 2.0f;

    [Header("--- 보상 아이템 설정 (확률 조절 가능) ---")]
    public GameObject itemPrefab;       
    [Range(0f, 100f)]
    public float dropChance = 50f;      

    private float minX = -4.5f;
    private float maxX = 4.5f;
    private float minY = 1.0f;
    private float maxY = 9.0f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        currentHp = maxHp;
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }
        SetNewTargetPosition();
    }

    void Update()
    {
        if (isMoving)
        {
            MoveTowardsTarget();
        }
    }

    void SetNewTargetPosition()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        targetPosition = new Vector3(randomX, randomY, 0f);
        isMoving = true;
    }

    void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isMoving = false;
            float randomDelay = Random.Range(minWaitTime, maxWaitTime);
            Invoke("SetNewTargetPosition", randomDelay);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
        if (hpSlider != null)
        {
            hpSlider.value = currentHp;
        }

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        float randomRoll = Random.Range(0f, 100f);

        if (randomRoll <= dropChance)
        {
            if (itemPrefab != null)
            {
                Instantiate(itemPrefab, transform.position, Quaternion.identity);
                Debug.Log("드롭 성공! (확률: " + dropChance + "% / 주사위: " + randomRoll + ")");
            }
        }
        else
        {
            Debug.Log("꽝! 아이템이 나오지 않았습니다. (확률: " + dropChance + "% / 주사위: " + randomRoll + ")");
        }

        if (StageGameManager.Instance != null)
        {
            StageGameManager.Instance.OnEnemyDied();
        }

        Destroy(gameObject);
    }
}