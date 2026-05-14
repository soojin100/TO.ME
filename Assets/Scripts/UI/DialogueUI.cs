using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Core;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>DialogueManager 구독, 말풍선 표시. 항상 활성 GameObject에 부착하고 root(자식)만 토글한다.</summary>
    public class DialogueUI : MonoBehaviour
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
            if (AudioManager.I != null)
            {
                bool isOwner = e.speaker != null && e.speaker.Contains("주인");
                AudioManager.I.PlaySfx(isOwner ? AudioManager.I.humanSfx : AudioManager.I.dogSfx);
            }
        }

        void OnEnd()
        {
            if (root) root.SetActive(false);
        }

        void OnSkip()
        {
            DialogueManager.I?.SkipAll();
        }
    }
}
