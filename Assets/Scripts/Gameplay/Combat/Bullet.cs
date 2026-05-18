using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;
    public float damage = 1f; // 총알의 기본 대미지

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    // [핵심] 무언가와 부딪혔을 때 유니티가 자동으로 실행해 주는 함수
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 부딪힌 대상이 "Monster" 컴포넌트를 가지고 있는지 확인
        EnemyController monster = collision.GetComponent<EnemyController>();

        if (monster != null)
        {
            // 몬스터에게 대미지를 준다!
            monster.TakeDamage(damage);

            // 대미지를 줬으니 총알은 화면에서 소멸
            Destroy(gameObject);
        }
    }
}