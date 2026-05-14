using UnityEngine;
using UnityEngine.EventSystems;

namespace TOME.Map
{
    /// <summary>화살표 버튼. 클릭 시 ScreenNavigator로 화면 이동. To.You RoomArrowButton 기반 포팅.</summary>
    [DisallowMultipleComponent]
    public class ArrowButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] ScreenNavigator.Direction direction;

        public void OnPointerClick(PointerEventData eventData)
        {
            ScreenNavigator.Instance?.TryMove(direction);
        }

        public void OnClick()
        {
            ScreenNavigator.Instance?.TryMove(direction);
        }
    }
}
