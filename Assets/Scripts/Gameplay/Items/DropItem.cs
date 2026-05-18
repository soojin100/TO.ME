using UnityEngine;

namespace TOME
{
    public class DropItem : MonoBehaviour
    {
        public float fallSpeed = 3.5f; // 아이템이 아래로 내려오는 속도

        void Update()
        {
            // 매 프레임 아래로 이동
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            // 플레이어 구역 바깥 아래로 완전히 지나치면 자동 삭제
            if (transform.position.y < -11.0f)
            {
                Destroy(gameObject);
            }
        }

        // [핵심] 몸으로 받아냈을 때 사라지는 로직
        private void OnTriggerEnter2D(Collider2D collision)
        {
            // 부딪힌 대상에게 PlayerController 컴포넌트가 있는지 확인
            if (collision.GetComponent<PlayerController>() != null)
            {
                Debug.Log("아이템 획득! 화면에서 사라집니다.");

                // 닿았으니 아이템 오브젝트 파괴(삭제)
                Destroy(gameObject);
            }
        }
    }
}
