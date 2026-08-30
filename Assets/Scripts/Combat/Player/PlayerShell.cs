using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TOME.Characters;
using TOME.Progression;
using TOME.Tutorial;
namespace TOME.Combat
{
    /// <summary>껍데기: 이동·HP·드래그. 캐릭터 코어는 캐시 후 재사용(GC 0).</summary>
    public class PlayerShell : MonoBehaviour
    {
        [SerializeField] Transform corePivot;
        [SerializeField] AutoAttack autoAttack;
        [SerializeField] float dragYMin = -8f;
        [SerializeField] float dragYMax = 2f;
        [SerializeField] float locomotionBlend = 0.15f;   // walk↔idle 전환 블렌딩 시간(초)
        [SerializeField] float moveStopDelay   = 0.12f;   // 멈춤 판정 지연(초) — 드래그 프레임 끊김으로 walk가 깜빡이지 않게
        [Tooltip("드래그 입력을 무시할 하단 화면 비율(인벤토리 바 영역). 0.18 = 아래 18%.")]
        [SerializeField, Range(0f, 0.5f)] float dragIgnoreBottomRatio = 0.18f;

        public CharacterSO CurrentChar { get; private set; }
        public float Hp        { get; private set; }    // 하트 단위. 1.0 = 한 칸, 0.5 = 반 칸.
        public float MaxHp     { get; private set; }
        public float CurAtk    { get; private set; }
        public float CurAtkSpd { get; private set; }
        public float CurRange  { get; private set; }

        public event Action<float,float> OnHpChanged;
        public event Action OnDied;

        readonly Dictionary<CharacterSO, GameObject> _visualCache = new(16);
        GameObject _curVisual;
        Camera _cam;
        Transform _tr;

        Animator  _visualAnimator;   // corePivot의 Animator (PSB 본 리그)
        string    _curAnimState;
        Vector3   _lastPos;
        float     _moveStopTimer;   // >0 이면 '이동 중'으로 간주 (드래그 프레임 끊김 흡수)

        void Awake() { _cam = Camera.main; _tr = transform; _lastPos = transform.position; }

        public void EquipCharacter(CharacterSO def, StatBonus bonus = null)
        {
            if (!def) return;

            // 이전 비주얼 비활성화 (Destroy X)
            if (_curVisual) _curVisual.SetActive(false);

            // 캐시 조회 → 없으면 1회 Instantiate.
            // 중요: PSB 본 리그는 corePivot 자식으로 "직접" instantiate(single-level)해야 SpriteSkin이 정상 deform.
            //       prefab 안에 nested하면 SpriteSkin.rootBone이 원본 본에 고정되어 mesh가 안 움직인다.
            if (!_visualCache.TryGetValue(def, out _curVisual))
            {
                if (def.corePrefab)
                {
                    _curVisual = Instantiate(def.corePrefab, corePivot);
                    _curVisual.transform.localPosition = Vector3.zero;
                    if (def.visualScale > 0f)
                        _curVisual.transform.localScale = Vector3.one * def.visualScale;
                    if (!string.IsNullOrEmpty(def.visualRootName))
                        _curVisual.name = def.visualRootName;   // 애니 path가 root 이름 포함 시 매칭
                    HideDuplicateLayers(_curVisual);            // PSB의 _W/중복 레이어 제거 (겹침 방지)
                    _visualCache[def] = _curVisual;
                }
            }
            if (_curVisual)
            {
                _curVisual.SetActive(true);
                // 단일 sprite(CharacterCore) 방식이면 Bind 호출
                if (_curVisual.TryGetComponent<CharacterCore>(out var core)) core.Bind(def);
            }

            // PSB 본 리그: Animator를 corePivot에 두고 재생 (anim path가 visualRoot를 포함하므로 부모인 corePivot가 Animator)
            SetupVisualAnimator(def);

            CurrentChar = def;
            float hpM = 1f + (bonus?.hpMul       ?? 0f);
            float aM  = 1f + (bonus?.atkMul      ?? 0f);
            float sM  = 1f + (bonus?.atkSpeedMul ?? 0f);
            MaxHp     = def.hp * hpM;
            Hp        = MaxHp;
            CurAtk    = def.atk * aM;
            CurAtkSpd = def.atkSpeed * sM;
            CurRange  = def.atkRange;

            if (autoAttack) autoAttack.Configure(def.attackPattern, () => CurAtk, () => CurAtkSpd, () => CurRange);
            OnHpChanged?.Invoke(Hp, MaxHp);
        }

        // PSB 본 리그의 파츠 이름 규약. Dog_halfside 리그는 본체 파츠 뒤에 "_W"(외곽선/뒷면) 파츠를 깔고,
        // 중복 파츠에는 " 2"가 붙는다 — TutorialCutsceneController.lineRendererSuffix와 같은 규약.
        const string OutlineLayerSuffix   = "_W";
        const string DuplicateLayerMarker = " 2";

        // PSB 본 리그에 일반 파츠와 겹치는 "_W"(흰색/뒷면) 및 " 2"(중복) 레이어가 있어 두 겹으로 보임 → 비활성화.
        static void HideDuplicateLayers(GameObject root)
        {
            if (root == null) return;
            var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < srs.Length; i++)
            {
                var n = srs[i].gameObject.name;
                if (n.EndsWith(OutlineLayerSuffix) || n.Contains(DuplicateLayerMarker))
                    srs[i].gameObject.SetActive(false);
            }
        }

        // PSB 본 리그 비주얼의 Animator를 corePivot(비주얼의 부모)에 두고 재생.
        // anim path가 "Dog_halfside/Body_1/..."처럼 root 이름을 포함하므로, Animator는 그 root의 부모에 있어야 매칭됨.
        void SetupVisualAnimator(CharacterSO def)
        {
            var anim = corePivot.GetComponent<Animator>();
            if (def.animatorController == null)
            {
                if (anim != null) anim.enabled = false;   // 단일 sprite 캐릭터면 corePivot Animator 비활성
                _visualAnimator = null;
                return;
            }
            if (anim == null) anim = corePivot.gameObject.AddComponent<Animator>();
            anim.enabled = true;
            anim.runtimeAnimatorController = def.animatorController;
            anim.Rebind();
            if (!string.IsNullOrEmpty(def.idleStateName)) anim.Play(def.idleStateName, 0, 0f);
            anim.Update(0f);
            _visualAnimator = anim;
            _curAnimState   = def.idleStateName;
            _lastPos        = _tr.position;
            _moveStopTimer  = 0f;
        }

        public void TakeDamage(float dmg)
        {
            if (Hp <= 0f) return;
            Hp = Mathf.Max(0f, Hp - dmg);
            OnHpChanged?.Invoke(Hp, MaxHp);
            if (Hp <= 0f) OnDied?.Invoke();
        }

        void Update()
        {
            // 일시정지/종료 중엔 드래그·이동 애니 갱신 X
            if (CombatManager.I != null && (CombatManager.I.IsPaused || CombatManager.I.IsFinished)) return;

            HandleDrag();
            UpdateLocomotionAnim();   // 이동하면 walk, 멈추면 idle
        }

        void HandleDrag()
        {
            if (!Input.GetMouseButton(0)) return;
            // UI 위 클릭은 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 sp = Input.mousePosition;
            // 인벤토리 바 영역(하단)은 드래그 무시
            float bottomCut = Screen.height * dragIgnoreBottomRatio;
            if (sp.y < bottomCut) return;

            Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -_cam.transform.position.z));
            wp.y = Mathf.Clamp(wp.y, dragYMin, dragYMax);
            wp.z = 0f;
            _tr.position = wp;
        }

        // 위치 변화로 이동 여부 판정 → walk/idle 전환.
        void UpdateLocomotionAnim()
        {
            if (_visualAnimator == null || CurrentChar == null) return;

            // 드래그 입력은 매 프레임 좌표가 갱신되지 않아(마우스 이벤트 < 프레임레이트)
            // 위치 델타가 한 프레임 걸러 0이 된다. 이를 그대로 쓰면 walk↔idle가 매 프레임 뒤집혀
            // CrossFade가 계속 재시작되고 walk가 화면에 안 나온다.
            // → 마지막 이동 후 moveStopDelay 동안은 '이동 중'으로 유지(히스테리시스).
            bool movedThisFrame = (_tr.position - _lastPos).sqrMagnitude > 0.0000004f;
            _lastPos = _tr.position;

            if (movedThisFrame) _moveStopTimer = moveStopDelay;
            else if (_moveStopTimer > 0f) _moveStopTimer -= Time.deltaTime;
            bool moving = _moveStopTimer > 0f;

            string want = moving ? CurrentChar.walkStateName : CurrentChar.idleStateName;
            if (string.IsNullOrEmpty(want) || want == _curAnimState) return;

            // 즉시 점프(Play) 대신 짧게 블렌딩 → walk↔idle 자연스러운 전환.
            _visualAnimator.CrossFadeInFixedTime(want, locomotionBlend, 0);
            _curAnimState = want;
        }
    }
}
