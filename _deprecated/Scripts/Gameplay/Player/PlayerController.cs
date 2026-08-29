using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("--- 이동 제한 영역 (World Unit) ---")]
    // 지난번 계산한 경계선 Y = 3.6에서 캐릭터 크기(반지름 약 0.6)를 감안해 마진을 조금 둠
    private float minX = -5.0f; // 화면 가로 해상도 1080 기준 좌측 끝 우측 끝 마진
    private float maxX = 5.0f;
    private float minY = -7.0f; // 화면 하단 끝 (-9.6에서 캐릭터 마진 적용)
    private float maxY = 0.0f;  // 몬스터 구역 경계선인 +3.6 아래로 제한

    [Header("--- 자동 발사 시스템 ---")]
    public GameObject bulletPrefab;  // 발사할 총알 프리패브
    public Transform firePoint;      // 총알이 나갈 위치 (캐릭터 머리 위 등)
    public float fireRate = 0.5f;    // 발사 주기 (0.5초에 한 번씩)
    private float fireTimer;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        fireTimer = fireRate; // 시작하자마자 바로 한 발 쏘도록 세팅
    }

    void Update()
    {
        HandleMovement();
        HandleAutoFire();
    }

    // 1. 손가락/마우스로 플레이어 끌어서 움직이기 (영역 제한 포함)
    void HandleMovement()
    {
        // 마우스 왼쪽 버튼 클릭 또는 터치 시작
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // 플레이어의 콜라이더 영역을 클릭했는지 체크
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isDragging = true;
                // 클릭한 지점과 캐릭터 중심점 사이의 거리 저장 (급격하게 튀는 현상 방지)
                offset = transform.position - mousePos;
            }
        }

        // 드래그 중일 때
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            Vector3 targetPosition = mousePos + offset;

            // [핵심] 계산해둔 플레이어 Area를 벗어나지 못하도록 철저히 제한 (Clamping)
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

            transform.position = targetPosition;
        }

        // 드래그 종료
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    // 2. 몇 초에 한 번씩 알아서 총알이 나가는 루프
    void HandleAutoFire()
    {
        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            Fire();
            fireTimer = fireRate; // 타이머 리셋
        }
    }

    void Fire()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            // 총알 생성!
            Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        }
    }
}