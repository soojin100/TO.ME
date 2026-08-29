using UnityEngine;
using UnityEngine.EventSystems;
using TOME.Systems;

namespace TOME.UI
{
    /// <summary>대사창 클릭 시 진행. 타이핑 중이면 DialogueUI가 즉시 완성 처리.</summary>
    public class DialogueAdvanceArea : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] DialogueUI dialogueUI;

        void Awake()
        {
            if (dialogueUI == null) dialogueUI = GetComponentInParent<DialogueUI>();
            if (dialogueUI == null) dialogueUI = FindObjectOfType<DialogueUI>(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dialogueUI != null) dialogueUI.HandleTap();
            else DialogueManager.I?.Advance();
        }
    }
}
