using UnityEngine;
namespace TOME.Combat
{
    /// <summary>적이 발사하는 총알. 발사 시 결정된 방향으로 직진하며 PlayerShell에 데미지.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyBullet : MonoBehaviour
    {
        [SerializeField] float lifeTime = 4f;

        Vector2 _dir;
        float   _speed;
        float   _damage;
        float   _life;
        Transform _tr;

        void Awake()
        {
            _tr = transform;
            // Collider는 무조건 트리거로 강제
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true;
            // Rigidbody2D가 없으면 자동 추가(Kinematic, 무중력) — 2D 트리거 발동에 필요
            if (!TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            rb.bodyType    = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        public void Launch(Vector2 dir, float speed, float damage)
        {
            _dir    = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.down;
            _speed  = speed;
            _damage = damage;
            _life   = 0f;
            float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            _tr.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        void Update()
        {
            _life += Time.deltaTime;
            if (_life > lifeTime) { Destroy(gameObject); return; }
            _tr.position += (Vector3)(_dir * (_speed * Time.deltaTime));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Player 콜라이더가 자식/부모에 있어도 PlayerShell을 찾도록
            var p = other.GetComponentInParent<PlayerShell>();
            if (p == null) return;
            p.TakeDamage(_damage);
            Destroy(gameObject);
        }
    }
}
