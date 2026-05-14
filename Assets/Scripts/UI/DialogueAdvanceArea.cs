using UnityEngine;
using UnityEngine.EventSystems;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>대사창 클릭 시 다음 줄로 진행. 레이캐스트 가능한 대사 패널에 부착.</summary>
    public class DialogueAdvanceArea : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            DialogueManager.I?.Advance();
        }
    }
}
