using UnityEngine;
using UnityEngine.UI;
using TOME.Data;
using TOME.Managers;

namespace TOME.Map
{
    /// <summary>맵에 배치된 줍기 오브젝트. 클릭 시 아이템을 인벤토리에 넣고 사라진다.</summary>
    [RequireComponent(typeof(Button))]
    public class MapPickup : MonoBehaviour
    {
        [SerializeField] ItemSO item;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Pick);
        }

        void Pick()
        {
            if (item != null) InventoryManager.I?.Add(item);
            gameObject.SetActive(false);
        }
    }
}
