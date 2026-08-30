using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace TOME.Dialogue
{
    /// <summary>DialogueManager 구독, 말풍선 표시 + 타이핑 효과 + 이름 입력 팝업.
    /// 이 파일: 대사 진행 흐름(구독·타이핑·탭·이름 입력).
    /// 초상화(입·눈 2채널 애니, 감정 시퀀스, 흔들림)는 DialogueUI.Portrait.cs 참고.</summary>
    public partial class DialogueUI : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text  speakerLabel;
        [SerializeField] TMP_Text  textLabel;
        [SerializeField] Button    skipButton;

        [Header("Typing")]
        [SerializeField, Range(0.005f, 0.2f)] float typingSpeed = 0.04f;

        [Header("Narration")]
        [Tooltip("이 화자 이름이면 초상화를 띄우지 않는다(나레이션 줄). 기획서 p7.")]
        [SerializeField] string narrationSpeaker = "나레이션";

        [Header("Name Input (optional)")]
        [Tooltip("nameInputPanel 이 비어 있을 때 즉시 기본 이름으로 넘길지. " +
                 "다른 컴포넌트(예: 튜토리얼 나무판자)가 이름 입력을 담당하면 반드시 해제한다 — " +
                 "켜 두면 입력을 받기도 전에 기본 이름으로 확정돼 버린다.")]
        [SerializeField] bool           autoSubmitWhenNoPanel = true;
        [SerializeField] GameObject     nameInputPanel;
        [SerializeField] TMP_InputField nameInputField;
        [SerializeField] Button         nameConfirmButton;

        Coroutine _typing;
        bool   _isTyping;
        bool   _isFirstLineOfSession = true;
        string _fullText = "";

        void Awake()
        {
            if (root) root.SetActive(false);
            if (nameInputPanel) nameInputPanel.SetActive(false);
            if (skipButton) skipButton.onClick.AddListener(OnSkip);
            if (nameConfirmButton) nameConfirmButton.onClick.AddListener(OnConfirmName);
            InitPortrait();   // DialogueUI.Portrait.cs — 프레임 로드·초상화 초기 상태
        }

        bool _subscribed;

        void OnEnable() => TrySubscribe();
        void Start()    => TrySubscribe();  // OnEnable이 DialogueManager.Awake보다 먼저 돌아도 Start에서 재시도(보장된 Awake 완료 시점)

        void TrySubscribe()
        {
            if (_subscribed || DialogueManager.I == null) return;
            DialogueManager.I.OnLine += OnLine;
            DialogueManager.I.OnEnd  += OnEnd;
            DialogueManager.I.OnNameInputRequested += OnNameInputRequested;
            DialogueManager.I.OnInteractionRequested += OnInteractionRequested;
            _subscribed = true;
        }

        void OnDisable()
        {
            if (!_subscribed) return;
            if (DialogueManager.I != null)
            {
                DialogueManager.I.OnLine -= OnLine;
                DialogueManager.I.OnEnd  -= OnEnd;
                DialogueManager.I.OnNameInputRequested -= OnNameInputRequested;
                DialogueManager.I.OnInteractionRequested -= OnInteractionRequested;
            }
            _subscribed = false;
        }

        // 인터랙티브 컷신(벽 보기/성수 등) 트리거 시 대사 UI 숨김. 다음 줄 OnLine에서 자동 재표시.
        void OnInteractionRequested(DialogueTrigger trigger)
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
            _isTyping = false;
            if (root) root.SetActive(false);
            HidePortrait();
        }

        void OnLine(DialogueEntry e)
        {
            bool wasActive = root && root.activeSelf;
            if (root) root.SetActive(true);
            if (!wasActive) _isFirstLineOfSession = true;

            bool playIntro = _isFirstLineOfSession;
            _isFirstLineOfSession = false;

            UpdateSpeakerSides(e.speaker);   // NPC(유령) 화자 판정 + 좌우 초상화 회색 처리

            // 나레이션 줄(화자 없음 또는 "나레이션")은 초상화를 띄우지 않는다 — 기획서 p7.
            if (IsNarration(e.speaker)) HidePortrait();
            else                        ShowPortrait(e, playIntro);

            ApplyEffect(e.effect);   // effect 가 비면 흔들림이 멈춘다 (기획서 p13: 움직임이 갑자기 멈춤)

            if (speakerLabel) speakerLabel.text = e.speaker;

            _fullText = e.text ?? "";
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeRoutine(_fullText));
        }

        IEnumerator TypeRoutine(string text)
        {
            // 입 = 글자 타이핑 동안만 움직임(입 시간 = 글자 출력 시간 = 대화 길이), 끝나면 닫힘.
            // ★입 속도는 typingSpeed와 완전 분리★ — 일정한 '말하기 박자'(mouthMinInterval)로만 벌림↔닫힘.
            //   → 타이핑이 아무리 빨라도 입은 차분(과거 글자 step 방식은 빠른 typingSpeed에 입이 버즈처럼 떨림).
            _isTyping = true;
            if (textLabel) textLabel.text = "";
            var wait = new WaitForSecondsRealtime(typingSpeed);

            // Angry 는 EmotionLoopRoutine 이 텍스처를 전담한다 — 여기서 입을 그리면 두 코루틴이 충돌해 깨진다.
            bool hasFrames = portraitImage != null && MouthClosedFrame != null && MouthOpenFrames.Length > 0
                             && !_npcSpeaking && !EmotionOwnsPortrait && HasSpeech(text);
            if (hasFrames) portraitImage.texture = MouthClosedFrame;    // 시작: 입 닫힘
            bool mouthOpen = false;
            int openVariant = -1;                                       // 벌림 프레임을 번갈아(벌림 크기 변화)
            float lastMouth = float.NegativeInfinity;
            float half = Mathf.Max(0.06f, mouthMinInterval * 0.5f);     // 입 한 상태(벌림 또는 닫힘) 유지 시간

            for (int i = 0; i < text.Length; i++)
            {
                if (textLabel) textLabel.text += text[i];

                // 일정 박자로만 입 상태 전환(글자 수·typingSpeed 무관) → 차분한 뻐끔.
                if (hasFrames && Time.unscaledTime - lastMouth >= half)
                {
                    mouthOpen = !mouthOpen;
                    if (mouthOpen)
                    {
                        var open = MouthOpenFrames;
                        openVariant = (openVariant + 1) % open.Length;
                        portraitImage.texture = open[openVariant];
                    }
                    else portraitImage.texture = MouthClosedFrame;
                    lastMouth = Time.unscaledTime;
                }
                yield return wait;
            }
            _isTyping = false;
            // 타이핑 끝 → 입 닫힘 보장 (이후 BlinkLoop가 눈만 깜빡). Angry 는 제 시퀀스를 유지한다.
            if (!EmotionOwnsPortrait && portraitImage != null && MouthClosedFrame != null)
                portraitImage.texture = MouthClosedFrame;
        }

        /// <summary>대사창 탭 시 호출(DialogueAdvanceArea). 타이핑 중이면 즉시 완성, 아니면 다음 줄.</summary>
        public void HandleTap()
        {
            if (nameInputPanel && nameInputPanel.activeSelf) return;
            if (_isTyping)
            {
                if (_typing != null) StopCoroutine(_typing);
                _isTyping = false;
                if (textLabel) textLabel.text = _fullText;
                // 즉시 완성(스킵) 시에도 입 닫힘 보장 — 토글 도중 멈춰 입이 열린 채 남지 않게.
                if (!_npcSpeaking && !EmotionOwnsPortrait && portraitImage != null && MouthClosedFrame != null)
                    portraitImage.texture = MouthClosedFrame;
                return;
            }
            DialogueManager.I?.Advance();
        }

        void OnEnd()
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
            _isTyping = false;
            _isFirstLineOfSession = true;
            ResetPortraitOnEnd();   // DialogueUI.Portrait.cs — NPC 표시·색·입 상태 원복
            if (root) root.SetActive(false);
            HidePortrait();
        }

        // 화자가 비었거나 나레이션이면 초상화 없음.
        bool IsNarration(string speaker)
            => string.IsNullOrWhiteSpace(speaker)
            || string.Equals(speaker.Trim(), narrationSpeaker, System.StringComparison.Ordinal);

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
            else if (autoSubmitWhenNoPanel)
            {
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
