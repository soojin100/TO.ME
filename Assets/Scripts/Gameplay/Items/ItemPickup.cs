using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.Gameplay.Items
{
    /// <summary>필드 아이템. 스폰 후 수집 영역으로 이동, 플레이어 충돌 시 인벤토리 획득.</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public class ItemPickup : MonoBehaviour
    {
        SpriteRenderer _sr;
        Transform _tr;
        ItemSO _def;
        ObjectPool _pool;
        float _driftSpeed;
        float _targetY;
        bool _active;

        void Awake()
        {
            _tr = transform;
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Init(ItemSO def, Vector3 spawnPos, float targetY, float driftSpeed, ObjectPool pool)
        {
            _def = def;
            _pool = pool;
            _targetY = targetY;
            _driftSpeed = driftSpeed;
            _tr.position = spawnPos;
            if (_sr && def != null && def.icon) _sr.sprite = def.icon;
            _active = true;
        }

        void Update()
        {
            if (!_active) return;
            Vector3 p = _tr.position;
            p.y = Mathf.MoveTowards(p.y, _targetY, _driftSpeed * Time.deltaTime);
            _tr.position = p;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active || _def == null) return;
            if (!other.GetComponentInParent<PlayerShell>()) return;
            InventoryManager.I?.Add(_def);
            Despawn();
        }

        void Despawn()
        {
            _active = false;
            if (_pool != null) _pool.Release(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
