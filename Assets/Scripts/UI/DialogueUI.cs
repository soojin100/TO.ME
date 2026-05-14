using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>DialogueManager 구독. 말풍선 표시 + 클릭 진행 + 스킵.</summary>
    public class DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text  speakerLabel;
        [SerializeField] TMP_Text  textLabel;
        [SerializeField] Button    skipButton;

        void Awake()
        {
            if (root) root.SetActive(false);
            if (skipButton) skipButton.onClick.AddListener(OnSkip);
        }

        void OnEnable()
        {
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnLine += OnLine;
                DialogueManager.I.OnEnd  += OnEnd;
            }
        }

        void OnDisable()
        {
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnLine -= OnLine;
                DialogueManager.I.OnEnd  -= OnEnd;
            }
        }

        void OnLine(DialogueEntry e)
        {
            if (root) root.SetActive(true);
            if (speakerLabel) speakerLabel.text = e.speaker;
            if (textLabel)    textLabel.text    = e.text;
        }

        void OnEnd()
        {
            if (root) root.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            DialogueManager.I?.Advance();
        }

        void OnSkip()
        {
            DialogueManager.I?.SkipAll();
        }
    }
}
