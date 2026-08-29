using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using TOME.Data;
using TOME.Systems;

namespace TOME.UI
{
    /// <summary>DialogueManager 구독, 말풍선 표시 + 타이핑 효과 + 이름 입력 팝업.
    /// 초상화는 Resources/Dialogue/{Talking,Blink,Angry,Intro}/*.png 프레임 시퀀스를 2채널 합성으로 swap.
    /// - 입(Mouth): 타이핑 중 = Talking 01(닫힘) ↔ 02/03(벌림) 교차, 대기 = 01 고정.
    /// - 눈(Blink): 독립 타이머로 평균 blinkIntervalAvg ± jitter 마다 Blink 시퀀스 1회 오버레이.
    /// - Intro: 대화 세션 첫 줄에 1회 리드인 후 일반 루프.
    /// - Angry/Happy: speakerSprite 컬럼의 감정 키로 활성(angry/화남, happy/웃음). 이때는 감정 시퀀스가
    ///   초상화 텍스처를 전담하고 입·눈 채널은 비켜선다. 감정 시퀀스는 [도입 1회][지속 반복][정지 고정] 3단계.
    /// - effect 컬럼: "shake" 면 초상화가 랜덤하게 흔들린다(기획서 p12). 빈칸이면 멈춘다(p13).</summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text  speakerLabel;
        [SerializeField] TMP_Text  textLabel;
        [SerializeField] Button    skipButton;

        [Header("Typing")]
        [SerializeField, Range(0.005f, 0.2f)] float typingSpeed = 0.04f;

        [Header("Portrait")]
        // 씬에 이미 있는 RawImage(portraitImage)에 PNG 시퀀스 텍스처를 swap 한다.
        // portraitVideo 는 호환을 위해 유지하되 사용하지 않는다 (있으면 Stop 처리).
        [SerializeField] GameObject  portraitRoot;
        [SerializeField] VideoPlayer portraitVideo;   // legacy — 사용 안함
        [SerializeField] RawImage    portraitImage;

        [Header("NPC Portrait (오른쪽, 유령 등장 시)")]
        [Tooltip("NPC(유령) 초상화 루트. 유령이 처음 화자가 되기 전까진 비활성. 화자 판단은 speaker가 '유령' 포함 여부.")]
        [SerializeField] GameObject npcPortraitRoot;
        [SerializeField] RawImage   npcPortraitImage;
        [Tooltip("화자가 아닌 쪽 초상화에 적용할 회색(grayscale) 머터리얼. UI/Grayscale 셰이더.")]
        [SerializeField] Material   grayscaleMaterial;

        [Header("Portrait Animation")]
        [Tooltip("한 번 뻐끔(벌림→닫힘)에 걸리는 시간(초). ★입 속도 단일 조절값★ — typingSpeed와 완전 무관. " +
                 "클수록 입이 천천히, 작을수록 빠르게. 입은 타이핑 동안만 움직이고 끝나면 닫힘(입 시간=글자 출력 시간).")]
        [SerializeField, Range(0.1f, 0.9f)] float mouthMinInterval = 0.5f;
        [Tooltip("말하기 프레임 cps(초당 컷). Angry 등 일부에서만 사용.")]
        [SerializeField, Range(2f, 20f)] float talkingFps = 6f;
        [Tooltip("화남 프레임 cps")]
        [SerializeField, Range(2f, 30f)] float angryFps = 10f;
        [Tooltip("도입 프레임 cps")]
        [SerializeField, Range(2f, 20f)] float introFps = 6f;
        [Tooltip("말하기 도중 자동 눈깜빡 빈도(초). 0이면 깜빡임 없음.")]
        [SerializeField, Range(0f, 10f)] float blinkIntervalAvg = 3.5f;
        [SerializeField, Range(0f, 5f)]  float blinkIntervalJitter = 1.5f;
        [Tooltip("눈깜빡 1회의 시퀀스 fps")]
        [SerializeField, Range(4f, 30f)] float blinkFps = 12f;

        [Header("Frame Index Mapping (이미지 디자인 기준)")]
        [Tooltip("Talking 폴더에서 '입 닫힘' frame 인덱스. 디자인상 Talking/02(=인덱스 1)가 닫힘.")]
        [SerializeField] int mouthClosedIndex = 1;
        [Tooltip("Blink 폴더에서 깜빡임 시퀀스 시작 인덱스. Blink/01(=인덱스 0)은 입 벌림이라 제외 → 1부터 시작.")]
        [SerializeField] int blinkStartIndex = 1;

        [Header("Angry Sequence (기획서 p9/p12)")]
        [Tooltip("Angry 시퀀스 앞쪽 '화가 치밀어 오르는' 도입 프레임 수(01~). 이 구간은 화가 시작될 때 한 번만 재생하고 " +
                 "그 뒤 프레임만 반복한다. 전체를 반복하면 화난 도중에 자꾸 팔 내린 차분한 포즈로 되돌아간다.")]
        [Min(0)] [SerializeField] int angryWindupCount = 5;
        [Tooltip("대사를 다 친 뒤(플레이어가 읽는 동안) 유지할 Angry 프레임 인덱스 — 입 닫힌 분노 표정. " +
                 "0번은 팔 내린 무표정이라 여기 두면 줄이 끝날 때마다 화가 풀려 버린다.")]
        [Min(0)] [SerializeField] int angryHoldIndex = 15;

        [Header("Happy Sequence (기획서 p10: 웃는 강아지 스탠딩)")]
        [Tooltip("웃는 표정 프레임 cps.")]
        [SerializeField, Range(2f, 20f)] float happyFps = 6f;
        [Tooltip("대사를 다 친 뒤 유지할 웃는 표정 프레임 인덱스.")]
        [Min(0)] [SerializeField] int happyHoldIndex = 0;

        [Header("Effect (기획서 p12: 분노 스탠딩이 랜덤하게 움직임)")]
        [Tooltip("dialogue 시트의 effect 컬럼이 이 값이면 초상화를 흔든다.")]
        [SerializeField] string shakeEffectKey = "shake";
        [Tooltip("흔들림 이동량(px) 최소~최대. 기획서: 위아래 좌우로 1~5픽셀 정도.")]
        [SerializeField] Vector2 shakeRange = new(1f, 5f);
        [Tooltip("흔들림 위치 갱신 간격(초).")]
        [SerializeField, Range(0.01f, 0.5f)] float shakeInterval = 0.05f;

        [Header("Portrait Frames (선택: 직접 와이어링. 비우면 Resources/Dialogue/* 로 폴백)")]
        [SerializeField] Texture2D[] talkingFramesOverride;
        [SerializeField] Texture2D[] blinkFramesOverride;
        [SerializeField] Texture2D[] angryFramesOverride;
        [SerializeField] Texture2D[] introFramesOverride;
        [Tooltip("웃는 표정 프레임. 비우면 Intro 프레임(전부 웃는 얼굴)을 그대로 쓴다.")]
        [SerializeField] Texture2D[] happyFramesOverride;

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

        // mood key → frame textures (loaded from Resources at Awake)
        Texture2D[] _framesTalking;
        Texture2D[] _framesBlink;
        Texture2D[] _framesAngry;
        Texture2D[] _framesIntro;
        Texture2D[] _framesHappy;

        // idle(가만히) 기준 프레임 = "입 닫힘 + 눈 뜸". 디자인상 Talking 시퀀스는 전부 입 벌림(말하기)이라
        // 진짜 입 닫힘은 Blink 시퀀스(blinkStartIndex)에 있음. 말 안 할 땐 이 프레임 고정 + 눈만 깜빡.
        Texture2D _idleFrame;

        // 현재 줄의 표정. Angry 는 EmotionLoopRoutine 이 portraitImage.texture 를 전담하므로
        // 입 애니메이션(TypeRoutine)이 같은 텍스처를 건드리지 않도록 이 값으로 구분한다.
        Mood _currentMood = Mood.Talking;

        bool _npcAppeared;   // 유령이 한 번이라도 화자가 됐으면 true → 이후 NPC 초상화 계속 표시
        bool _npcSpeaking;   // 현재 줄의 화자가 유령이면 true → 플레이어 입 정지 + 플레이어 회색

        Coroutine _typing;
        Coroutine _portraitCo;
        Coroutine _blinkCo;
        bool   _isTyping;
        bool   _isFirstLineOfSession = true;
        string _fullText = "";

        void Awake()
        {
            if (root) root.SetActive(false);
            if (portraitRoot) portraitRoot.SetActive(false);
            if (npcPortraitRoot) npcPortraitRoot.SetActive(false);   // 유령 등장 전까진 숨김
            // VideoPlayer 가 남아있어도 사용하지 않는다.
            if (portraitVideo)
            {
                portraitVideo.Stop();
                portraitVideo.playOnAwake = false;
                portraitVideo.enabled = false;
            }
            if (nameInputPanel) nameInputPanel.SetActive(false);
            if (skipButton) skipButton.onClick.AddListener(OnSkip);
            if (nameConfirmButton) nameConfirmButton.onClick.AddListener(OnConfirmName);

            _framesTalking = (talkingFramesOverride != null && talkingFramesOverride.Length > 0) ? talkingFramesOverride : LoadFrames("Dialogue/Talking");
            _framesBlink   = (blinkFramesOverride   != null && blinkFramesOverride.Length   > 0) ? blinkFramesOverride   : LoadFrames("Dialogue/Blink");
            _framesAngry   = (angryFramesOverride   != null && angryFramesOverride.Length   > 0) ? angryFramesOverride   : LoadFrames("Dialogue/Angry");
            _framesIntro   = (introFramesOverride   != null && introFramesOverride.Length   > 0) ? introFramesOverride   : LoadFrames("Dialogue/Intro");
            // Talking 세트는 01=닫힘, 02·03=벌림. 벌림 프레임만 따로 모아 둔다.
            if (_framesTalking != null && _framesTalking.Length > 1)
            {
                _openTalking = new Texture2D[_framesTalking.Length - 1];
                System.Array.Copy(_framesTalking, 1, _openTalking, 0, _openTalking.Length);
            }
            // 전용 웃음 프레임이 없으면 Intro 프레임을 쓴다 — Intro/01~04가 전부 웃는 얼굴이라 그대로 성립한다.
            _framesHappy   = (happyFramesOverride   != null && happyFramesOverride.Length   > 0) ? happyFramesOverride
                           : (LoadFrames("Dialogue/Happy") ?? _framesIntro);
        }

        static Texture2D[] LoadFrames(string resourcePath)
        {
            // Resources.LoadAll 은 알파벳 순으로 반환 → 파일명을 01, 02, 03 … 으로 맞춰 두면 안정적 순서.
            var arr = Resources.LoadAll<Texture2D>(resourcePath);
            if (arr == null || arr.Length == 0) return null;
            System.Array.Sort(arr, (a, b) =>
                string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return arr;
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

            // 화자 판단: speaker에 '유령'이 들어가면 NPC가 말하는 중.
            _npcSpeaking = !string.IsNullOrEmpty(e.speaker) && e.speaker.Contains("유령");
            if (_npcSpeaking) _npcAppeared = true;                 // 유령 첫 등장 → 이후 계속 표시
            if (npcPortraitRoot) npcPortraitRoot.SetActive(_npcAppeared);
            ApplySpeakerTint();                                     // 화자 아닌 쪽 회색

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

        // 화자 아닌 쪽 초상화에 grayscale 머터리얼 적용(회색), 화자는 원래 색(material=null).
        void ApplySpeakerTint()
        {
            if (portraitImage) portraitImage.material = _npcSpeaking ? grayscaleMaterial : null;
            if (npcPortraitImage) npcPortraitImage.material = _npcSpeaking ? null : grayscaleMaterial;
        }

        void OnEnd()
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
            _isTyping = false;
            _isFirstLineOfSession = true;
            _npcAppeared = false;
            _npcSpeaking = false;
            _currentMood = Mood.Talking;
            if (npcPortraitRoot) npcPortraitRoot.SetActive(false);
            if (portraitImage)
            {
                portraitImage.material = null;                      // 색 원복
                if (_framesTalking != null && _framesTalking.Length > 0)
                    portraitImage.texture = _framesTalking[0];      // 대화 끝 → 입 닫힘 보장
            }
            if (root) root.SetActive(false);
            HidePortrait();
        }

        // --- portrait (2채널: 입 + 눈) ---

        void ShowPortrait(DialogueEntry e, bool playIntro)
        {
            if (!portraitImage) return;
            if (portraitRoot && !portraitRoot.activeSelf) portraitRoot.SetActive(true);

            StopPortraitCoroutines();

            var mood = ResolveMood(e);
            // 같은 감정이 이어지는 줄에서는 도입(치밀어 오르는 구간)을 다시 재생하지 않는다.
            // 매 줄 다시 감으면 연속된 분노 대사가 "무표정 → 폭발"을 반복해 감정이 끊긴다.
            bool moodChanged = mood != _currentMood;
            _currentMood = mood;

            // Angry: 도입(01~)은 화가 시작될 때 한 번만, 이후에는 폭발 구간만 반복.
            //        대사를 다 치면 angryHoldIndex(입 닫힌 분노)로 고정 — 화가 풀리지 않게.
            if (mood == Mood.Angry)
            {
                if (_framesAngry != null && _framesAngry.Length > 0)
                    _portraitCo = StartCoroutine(EmotionLoopRoutine(
                        _framesAngry, angryFps, angryWindupCount, angryHoldIndex, moodChanged));
                else if (_framesTalking != null && _framesTalking.Length > 0)
                    _portraitCo = StartCoroutine(MouthRoutine(false));
                return;
            }

            // Happy(기획서 p10 웃는 스탠딩)는 아래 기본 2채널 경로를 그대로 탄다.
            // 입은 TypeRoutine 이 웃는 벌림 프레임 ↔ 다문 프레임으로 뻐끔거리고, 눈은 BlinkLoop 가 깜빡인다.

            // 기본 2채널: 입 = TypeRoutine(글자 타이핑에 동기, 끝나면 닫힘), 눈 = BlinkLoop(대기 중 깜빡).
            if (_framesTalking == null || _framesTalking.Length == 0) return;
            // idle 기준 = Talking/01(입닫힘+눈뜸). 말하기(talk)도 Talking 시퀀스라 idle↔talk가 같은 베이스 →
            // 전환 시 다른 이미지로 톡 튀지 않고 매끄러움.
            _idleFrame = _framesTalking[0];
            // 입은 닫힘(Talking/01)으로 시작 → TypeRoutine이 첫 음절부터 벌림. (NPC 화자면 TypeRoutine이 입 안 건드림)
            if (portraitImage != null) portraitImage.texture = _framesTalking[0];
            // 눈 깜빡임: 말하는 중엔 BlinkLoop가 스스로 스킵(입 우선), 대기 중에만 깜빡.
            if (_framesBlink != null && _framesBlink.Length > 0 && blinkIntervalAvg > 0f)
                _blinkCo = StartCoroutine(BlinkLoop());
        }

        void StopPortraitCoroutines()
        {
            if (_portraitCo != null) { StopCoroutine(_portraitCo); _portraitCo = null; }
            if (_blinkCo != null)    { StopCoroutine(_blinkCo);    _blinkCo = null; }
        }

        void HidePortrait()
        {
            StopPortraitCoroutines();
            StopShake();
            // 초상화가 사라졌다 다시 나오면 감정도 처음부터 — 다음 분노 줄에서 도입이 다시 재생된다.
            _currentMood = Mood.Talking;
            if (portraitRoot) portraitRoot.SetActive(false);
        }

        enum Mood { Talking, Angry, Intro, Happy }

        // Angry 만 EmotionLoopRoutine 이 portraitImage.texture 를 전담한다(분노는 팔 동작까지 있는 전신 시퀀스).
        // Happy 는 얼굴만 바뀌므로 기존 입 채널(TypeRoutine)을 그대로 쓴다 — 그래야 입이 뻐끔거린다.
        bool EmotionOwnsPortrait => _currentMood == Mood.Angry;

        /// <summary>입을 다문 프레임. 웃는 프레임(Intro)은 4장 전부 입이 벌어져 있어 닫힘이 없다 →
        /// 닫힘은 항상 Talking/01(다문 미소)을 쓴다. 이게 없으면 입이 벌어진 채로 굳는다.</summary>
        Texture2D MouthClosedFrame
            => (_framesTalking != null && _framesTalking.Length > 0) ? _framesTalking[0] : null;

        /// <summary>입을 벌린 프레임들. 번갈아 써서 벌림 크기가 변한다.
        /// Happy 면 웃는 프레임(전부 입 벌림), 아니면 Talking/02·03.</summary>
        Texture2D[] MouthOpenFrames
        {
            get
            {
                if (_currentMood == Mood.Happy && _framesHappy != null && _framesHappy.Length > 0)
                    return _framesHappy;
                return _openTalking;
            }
        }
        Texture2D[] _openTalking = System.Array.Empty<Texture2D>();

        // 화자가 비었거나 나레이션이면 초상화 없음.
        bool IsNarration(string speaker)
            => string.IsNullOrWhiteSpace(speaker)
            || string.Equals(speaker.Trim(), narrationSpeaker, System.StringComparison.Ordinal);

        Mood ResolveMood(DialogueEntry e)
        {
            var key = (e.speakerSprite ?? string.Empty).ToLowerInvariant();
            if (key.Length > 0)
            {
                if (key.Contains("angry") || key.Contains("화남") || key.Contains("mad")) return Mood.Angry;
                if (key.Contains("happy") || key.Contains("웃음") || key.Contains("웃는") || key.Contains("smile")) return Mood.Happy;
                if (key.Contains("intro") || key.Contains("도입")) return Mood.Intro;
                if (key.Contains("talk")  || key.Contains("말하")) return Mood.Talking;
            }
            return Mood.Talking;
        }

        // 감정 시퀀스(분노/웃음). 시퀀스는 [도입 windup][지속 loop] 두 구간으로 나눠 재생한다.
        //  - 도입: 감정이 바뀌는 순간 한 번만. 분노 시퀀스의 01~05가 "팔 내림 → 주먹 들어올림"이라
        //          여기를 반복하면 화난 도중에 자꾸 차분한 포즈로 되돌아간다.
        //  - 지속: 타이핑 중에만 반복 → 표정 변화 길이가 대사 길이에 비례.
        //  - 정지: 타이핑이 끝나면 holdIndex 고정. 플레이어가 대사를 읽는 동안(가장 오래 보는 시점)
        //          감정이 유지돼야 한다. 0번으로 돌리면 분노가 매 줄 풀린다.
        IEnumerator EmotionLoopRoutine(Texture2D[] frames, float fps, int windupCount, int holdIndex, bool playWindup)
        {
            var wait = new WaitForSecondsRealtime(1f / Mathf.Max(1f, fps));
            int windup = Mathf.Clamp(windupCount, 0, frames.Length - 1);
            int hold   = Mathf.Clamp(holdIndex,   0, frames.Length - 1);

            if (playWindup)
            {
                for (int w = 0; w < windup; w++)
                {
                    portraitImage.texture = frames[w];
                    yield return wait;
                }
            }

            int i = windup;
            portraitImage.texture = frames[hold];
            bool wasTyping = false;
            while (true)
            {
                if (_isTyping)
                {
                    portraitImage.texture = frames[i];
                    if (++i >= frames.Length) i = windup;    // 도입 구간으로는 되돌아가지 않는다
                    wasTyping = true;
                }
                else if (wasTyping)
                {
                    portraitImage.texture = frames[hold];
                    i = windup;
                    wasTyping = false;
                }
                yield return wait;
            }
        }

        // 글자가 하나도 없는 줄("...", "!!!" 등)은 말하는 게 아니라 '사이'다 → 입을 움직이지 않는다.
        static bool HasSpeech(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text) if (char.IsLetterOrDigit(c)) return true;
            return false;
        }

        // --- effect (기획서 p12: 분노 스탠딩이 위아래 좌우로 1~5픽셀 랜덤하게 움직임) ---

        Coroutine _shakeCo;
        Vector2   _shakeBase;
        bool      _shakeActive;

        void ApplyEffect(string effect)
        {
            bool wantShake = !string.IsNullOrWhiteSpace(effect) && !string.IsNullOrEmpty(shakeEffectKey)
                && string.Equals(effect.Trim(), shakeEffectKey, System.StringComparison.OrdinalIgnoreCase);
            if (wantShake) StartShake();
            else           StopShake();
        }

        void StartShake()
        {
            if (_shakeActive || portraitImage == null) return;
            _shakeBase = portraitImage.rectTransform.anchoredPosition;   // 흔들기 전 위치를 기억해 뒀다 복구
            _shakeActive = true;
            _shakeCo = StartCoroutine(ShakeRoutine());
        }

        void StopShake()
        {
            if (_shakeCo != null) { StopCoroutine(_shakeCo); _shakeCo = null; }
            if (_shakeActive && portraitImage != null)
                portraitImage.rectTransform.anchoredPosition = _shakeBase;
            _shakeActive = false;
        }

        IEnumerator ShakeRoutine()
        {
            var rt = portraitImage.rectTransform;
            var wait = new WaitForSecondsRealtime(shakeInterval);
            float min = Mathf.Min(shakeRange.x, shakeRange.y);
            float max = Mathf.Max(shakeRange.x, shakeRange.y);
            while (true)
            {
                float dx = UnityEngine.Random.Range(min, max) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
                float dy = UnityEngine.Random.Range(min, max) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
                rt.anchoredPosition = _shakeBase + new Vector2(dx, dy);
                yield return wait;
            }
        }

        // 입 채널 = 상태 기반 애니메이션:
        //  - 대사 출력 중(_isTyping): Talking 시퀀스를 talkingFps 일정 속도로 루프(글자 수와 무관) → 일정한 말하기 모션.
        //  - 대사 끝/대기(다음으로 넘어가기 대기): idle(입 닫힘) 고정 → 눈만 BlinkLoop로 깜빡.
        IEnumerator MouthRoutine(bool playIntro)
        {
            if (playIntro && _framesIntro != null && _framesIntro.Length > 0)
            {
                var introWait = new WaitForSecondsRealtime(1f / Mathf.Max(1f, introFps));
                for (int i = 0; i < _framesIntro.Length; i++)
                {
                    portraitImage.texture = _framesIntro[i];
                    yield return introWait;
                }
            }

            // 입 애니 속도 = talkingFps 고정(typingSpeed 무관 → 일정한 말하기 속도).
            float mouthInterval = 1f / Mathf.Max(1f, talkingFps);
            var mouthWait = new WaitForSecondsRealtime(mouthInterval);
            int mouthIdx = 0;
            bool wasTyping = false;
            // 시작은 입 닫힘 = Talking/01 (디자인: Talking 01=닫힘, 02=벌림作, 03=벌림大).
            portraitImage.texture = _framesTalking[0];

            while (true)
            {
                if (_isTyping)
                {
                    // 말하는 중 = Talking 시퀀스 루프(01 닫힘 → 02 벌림 → 03 벌림 → 01 …) = 입 뻐끔.
                    // 타이핑 시작 즉시 다음 프레임(벌림)으로 → 짧은 대사도 입이 곧바로 열림.
                    mouthIdx = (mouthIdx + 1) % _framesTalking.Length;
                    portraitImage.texture = _framesTalking[mouthIdx];
                    wasTyping = true;
                }
                else if (wasTyping)
                {
                    // 대사 끝/대기 = 입 닫힘(Talking/01) 고정. 이후 입 채널은 텍스처를 안 건드림
                    // (BlinkLoop가 Blink 02~07로 눈만 깜빡이도록 양보).
                    portraitImage.texture = _framesTalking[0];
                    mouthIdx = 0;
                    wasTyping = false;
                }
                yield return mouthWait;
            }
        }

        // 눈 채널: 입 채널과 독립. 평균 blinkIntervalAvg ± jitter 마다 Blink 시퀀스 1회 오버레이.
        IEnumerator BlinkLoop()
        {
            // Blink/01(=index 0)은 디자인상 입 벌림 frame이라 깜빡임 시퀀스에서 제외.
            // blinkStartIndex(기본 1)부터 _framesBlink 끝까지가 "입 닫힘 + 눈 변화" 시퀀스.
            int start = Mathf.Clamp(blinkStartIndex, 0, _framesBlink.Length - 1);
            while (true)
            {
                yield return new WaitForSecondsRealtime(BlinkDelay());
                // 타이핑(말하기) 중엔 입 애니가 portraitImage를 제어하므로 깜빡임 스킵 → 입 뻐끔이 안 묻힘.
                if (_isTyping) continue;
                var blinkWait = new WaitForSecondsRealtime(1f / Mathf.Max(1f, blinkFps));
                for (int i = start; i < _framesBlink.Length; i++)
                {
                    if (_isTyping) break;   // 도중에 말 시작하면 깜빡임 중단(입 우선)
                    portraitImage.texture = _framesBlink[i];
                    yield return blinkWait;
                }
                // 깜빡임 종료 후 idle(입 닫힘 + 눈 뜬)로 복귀.
                if (!_isTyping && _idleFrame != null) portraitImage.texture = _idleFrame;
            }
        }

        float BlinkDelay()
            => Mathf.Max(0.2f, blinkIntervalAvg + Random.Range(-blinkIntervalJitter, blinkIntervalJitter));

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
