using UnityEngine;
using UnityEngine.UI;
using TOME.Core;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>맵에 배치된 줍기 오브젝트. 클릭 시 아이템을 인벤토리에 넣고 사라진다. 줍기 상태는 저장에 영속.</summary>
    [RequireComponent(typeof(Button))]
    public class MapPickup : MonoBehaviour
    {
        [SerializeField] string pickupId;   // 씬 내 고유 ID — 저장 키
        [SerializeField] ItemSO item;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Pick);
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(pickupId)
                && SaveSystemManager.I != null
                && SaveSystemManager.I.IsPickupCollected(pickupId))
            {
                gameObject.SetActive(false);
            }
        }

        void Pick()
        {
            if (item != null) InventoryManager.I?.Add(item);
            if (!string.IsNullOrEmpty(pickupId))
                SaveSystemManager.I?.MarkPickupCollected(pickupId);
            gameObject.SetActive(false);
        }
    }
}
