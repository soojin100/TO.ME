using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Dialogue;
using TOME.Save;
namespace TOME.Tutorial
{
    /// <summary>이름 입력 나무 판자 (기획서 p6). 위에서 내려와 입력을 받고 아래로 슝 내려간다.
    ///
    /// 기존 이름 입력 흐름을 그대로 탄다 — dialogue 시트의 NameInput 트리거가 발생하면
    /// DialogueManager.OnNameInputRequested 를 받아 열리고, 확정 시 SubmitName 으로 대사를 재개한다.
    /// (DialogueUI 의 기존 nameInputPanel 을 대체하는 연출이므로 둘 중 하나만 씬에 연결한다.)
    /// 이름 정제(불완전 자모 제거)는 SaveSystemManager.SetPlayerName이 이미 담당한다.</summary>
    public class NameInputBoardUI : MonoBehaviour
    {
        [SerializeField] RectTransform  board;
        [SerializeField] TMP_InputField inputField;
        [SerializeField] Button         confirmButton;
        [Tooltip("글자수가 규칙을 벗어났을 때 표시할 안내(선택).")]
        [SerializeField] GameObject     invalidHint;

        [Header("슬라이드")]
        [Tooltip("화면 밖 대기 위치(anchoredPosition). 보통 화면 위쪽.")]
        [SerializeField] Vector2 hiddenPosition = new(0f, 1400f);
        [Tooltip("입력 받을 때 머무는 위치(anchoredPosition).")]
        [SerializeField] Vector2 shownPosition  = Vector2.zero;
        [Tooltip("퇴장 시 내려갈 위치(anchoredPosition). 기획서: 아래로 슝.")]
        [SerializeField] Vector2 exitPosition   = new(0f, -1400f);
        [SerializeField] float slideInDuration  = 0.35f;
        [SerializeField] float slideOutDuration = 0.25f;
        [SerializeField] AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("글자수 (기획서 p6: 최소 1, 최대 12)")]
        [Min(1)] [SerializeField] int minLength = 1;
        [Min(1)] [SerializeField] int maxLength = 12;

        bool _confirmed;
        int  _min = 1, _max = 12;
        bool _subscribed;

        /// <summary>확정 가능한 입력인지. 빈 입력은 허용한다 — 아무것도 안 넣으면 기본 이름이 쓰인다.
        /// 최대 글자수만 넘지 않으면 된다(최소 글자수는 빈 입력을 막지 않는다).</summary>
        public static bool IsLengthValid(string raw, int min, int max)
        {
            string trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0) return true;          // 빈 입력 → 기본 이름
            return trimmed.Length >= min && trimmed.Length <= max;
        }

        void Awake()
        {
            if (board) board.anchoredPosition = hiddenPosition;
            if (invalidHint) invalidHint.SetActive(false);
            if (confirmButton) confirmButton.onClick.AddListener(OnConfirm);
            if (inputField) inputField.onValueChanged.AddListener(OnValueChanged);
        }

        void OnDestroy()
        {
            if (confirmButton) confirmButton.onClick.RemoveListener(OnConfirm);
            if (inputField) inputField.onValueChanged.RemoveListener(OnValueChanged);
        }

        void OnEnable() => TrySubscribe();
        void Start()    => TrySubscribe();   // DialogueManager.Awake보다 먼저 켜져도 여기서 재시도

        void TrySubscribe()
        {
            if (_subscribed || DialogueManager.I == null) return;
            DialogueManager.I.OnNameInputRequested += OnNameInputRequested;
            _subscribed = true;
        }

        void OnDisable()
        {
            if (!_subscribed) return;
            if (DialogueManager.I != null)
                DialogueManager.I.OnNameInputRequested -= OnNameInputRequested;
            _subscribed = false;
        }

        // 대사의 NameInput 트리거 → 판자를 내려 입력을 받고, 끝나면 대사를 재개한다.
        void OnNameInputRequested() => StartCoroutine(HandleNameInput());

        IEnumerator HandleNameInput()
        {
            yield return RunRoutine(minLength, maxLength);
            DialogueManager.I?.SubmitName(_lastEntered);
        }

        string _lastEntered;

        /// <summary>판자를 내려 입력을 받고, 확정되면 저장 후 아래로 퇴장한다.</summary>
        public IEnumerator RunRoutine(int minLength, int maxLength)
        {
            _min = Mathf.Max(1, minLength);
            _max = Mathf.Max(_min, maxLength);
            _confirmed = false;
            _lastEntered = null;

            // 루트는 계속 활성으로 둔다 — 비활성이면 OnNameInputRequested 구독이 끊긴다.
            // 판자는 화면 밖(hiddenPosition)에 있어 보이지 않는다.
            if (board) board.anchoredPosition = hiddenPosition;
            if (inputField)
            {
                inputField.characterLimit = _max;   // 최대 길이는 입력 단계에서 막는다
                inputField.text = "";
            }
            RefreshConfirmState();

            yield return SlideRoutine(hiddenPosition, shownPosition, slideInDuration);

            if (inputField) inputField.ActivateInputField();
            while (!_confirmed) yield return null;

            if (inputField) inputField.DeactivateInputField();
            yield return SlideRoutine(shownPosition, exitPosition, slideOutDuration);
            // 다음 호출을 위해 대기 위치로 되돌려 둔다.
            if (board) board.anchoredPosition = hiddenPosition;
        }

        void OnValueChanged(string _) => RefreshConfirmState();

        void RefreshConfirmState()
        {
            bool ok = IsLengthValid(inputField ? inputField.text : null, _min, _max);
            if (confirmButton) confirmButton.interactable = ok;
            if (invalidHint) invalidHint.SetActive(!ok);
        }

        void OnConfirm()
        {
            string entered = inputField ? inputField.text : null;
            if (!IsLengthValid(entered, _min, _max)) return;   // 버튼을 못 막은 경로도 차단

            // 저장은 DialogueManager.SubmitName 이 담당한다(기존 이름 입력 흐름과 동일).
            // 아무것도 안 넣었으면 null 을 넘겨 기본 이름(제임스)이 쓰이게 한다.
            string trimmed = (entered ?? string.Empty).Trim();
            _lastEntered = trimmed.Length == 0 ? null : trimmed;
            _confirmed = true;
        }

        IEnumerator SlideRoutine(Vector2 from, Vector2 to, float duration)
        {
            if (board == null) yield break;
            if (duration <= 0f) { board.anchoredPosition = to; yield break; }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                board.anchoredPosition = Vector2.LerpUnclamped(
                    from, to, slideCurve.Evaluate(Mathf.Clamp01(t / duration)));
                yield return null;
            }
            board.anchoredPosition = to;
        }
    }
}
