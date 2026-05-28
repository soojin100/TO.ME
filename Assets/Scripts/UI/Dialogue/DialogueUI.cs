using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Core;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>DialogueManager 구독, 말풍선 표시 + 타이핑 효과 + 이름 입력 팝업.
    /// 항상 활성 GameObject에 부착하고 root(자식)만 토글한다.</summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text  speakerLabel;
        [SerializeField] TMP_Text  textLabel;
        [SerializeField] Button    skipButton;

        [Header("Typing")]
        [SerializeField, Range(0.005f, 0.2f)] float typingSpeed = 0.04f;

        [Header("Name Input (optional)")]
        [SerializeField] GameObject     nameInputPanel;   // 없으면 기본 이름으로 자동 진행
        [SerializeField] TMP_InputField nameInputField;
        [SerializeField] Button         nameConfirmButton;

        Coroutine _typing;
        bool   _isTyping;
        string _fullText = "";

        void Awake()
        {
            if (root) root.SetActive(false);
            if (nameInputPanel) nameInputPanel.SetActive(false);
            if (skipButton) skipButton.onClick.AddListener(OnSkip);
            if (nameConfirmButton) nameConfirmButton.onClick.AddListener(OnConfirmName);
        }

        void OnEnable()
        {
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnLine += OnLine;
                DialogueManager.I.OnEnd  += OnEnd;
                DialogueManager.I.OnNameInputRequested += OnNameInputRequested;
            }
        }

        void OnDisable()
        {
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnLine -= OnLine;
                DialogueManager.I.OnEnd  -= OnEnd;
                DialogueManager.I.OnNameInputRequested -= OnNameInputRequested;
            }
        }

        void OnLine(DialogueEntry e)
        {
            if (root) root.SetActive(true);
            if (speakerLabel) speakerLabel.text = e.speaker;

            _fullText = e.text ?? "";
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeRoutine(_fullText));
            // (제거) 과거 To.You의 강아지/인간 모드 BGM(dogSfx/humanSfx)을 대사 줄마다 SFX로 재생 →
            //        Title BGM 위에 또 다른 BGM이 겹쳐 들리던 원인. 이 프로젝트엔 모드 전환이 없어 재생하지 않는다.
        }

        IEnumerator TypeRoutine(string text)
        {
            _isTyping = true;
            if (textLabel) textLabel.text = "";
            var wait = new WaitForSecondsRealtime(typingSpeed);
            for (int i = 0; i < text.Length; i++)
            {
                if (textLabel) textLabel.text += text[i];
                yield return wait;
            }
            _isTyping = false;
        }

        /// <summary>대사창 탭 시 호출(DialogueAdvanceArea). 타이핑 중이면 즉시 완성, 아니면 다음 줄.</summary>
        public void HandleTap()
        {
            if (nameInputPanel && nameInputPanel.activeSelf) return; // 이름 입력 중엔 진행 차단
            if (_isTyping)
            {
                if (_typing != null) StopCoroutine(_typing);
                _isTyping = false;
                if (textLabel) textLabel.text = _fullText;
                return;
            }
            DialogueManager.I?.Advance();
        }

        void OnEnd()
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
            _isTyping = false;
            if (root) root.SetActive(false);
        }

        void OnSkip() => DialogueManager.I?.SkipAll();

        // --- 이름 입력 ---
        void OnNameInputRequested()
        {
            if (nameInputPanel && nameInputField)
            {
                nameInputPanel.SetActive(true);
                nameInputField.text = "";
                nameInputField.ActivateInputField();
            }
            else
            {
                // 팝업 UI 미구성 시 기본 이름으로 자동 진행 (흐름 차단 방지)
                DialogueManager.I?.SubmitName(null);
            }
        }

        void OnConfirmName()
        {
            string entered = nameInputField ? nameInputField.text : null;
            if (nameInputPanel) nameInputPanel.SetActive(false);
            DialogueManager.I?.SubmitName(entered);
        }
    }
}
