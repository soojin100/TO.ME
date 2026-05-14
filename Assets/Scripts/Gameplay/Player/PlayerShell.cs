using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TOME.Data;
using TOME.Gameplay.Combat;
using TOME.Managers;

namespace TOME.Gameplay.Player
{
    /// <summary>껍데기: 이동·HP·드래그. 캐릭터 코어는 캐시 후 재사용(GC 0).</summary>
    public class PlayerShell : MonoBehaviour
    {
        [SerializeField] Transform corePivot;
        [SerializeField] AutoAttack autoAttack;
        [SerializeField] float dragYMin = -8f;
        [SerializeField] float dragYMax = -2f;
        [SerializeField] float dragLerpSpeed = 25f;

        public CharacterSO CurrentChar { get; private set; }
        public int   Hp        { get; private set; }
        public int   MaxHp     { get; private set; }
        public float CurAtk    { get; private set; }
        public float CurAtkSpd { get; private set; }
        public float CurRange  { get; private set; }

        public event Action<int,int> OnHpChanged;
        public event Action OnDied;

        readonly Dictionary<CharacterSO, CharacterCore> _coreCache = new(16);
        CharacterCore _curCore;
        Camera _cam;
        Transform _tr;

        void Awake() { _cam = Camera.main; _tr = transform; }

        public void EquipCharacter(CharacterSO def, StatBonus bonus = null)
        {
            if (!def) return;

            // 이전 코어 비활성화 (Destroy X)
            if (_curCore) _curCore.gameObject.SetActive(false);

            // 캐시 조회 → 없으면 1회 Instantiate
            if (!_coreCache.TryGetValue(def, out _curCore))
            {
                if (def.corePrefab)
                {
                    var go = Instantiate(def.corePrefab, corePivot);
                    go.transform.localPosition = Vector3.zero;
                    _curCore = go.GetComponent<CharacterCore>();
                    if (_curCore) _coreCache[def] = _curCore;
                }
            }
            if (_curCore)
            {
                _curCore.gameObject.SetActive(true);
                _curCore.Bind(def);
            }

            CurrentChar = def;
            float hpM = 1f + (bonus?.hpMul       ?? 0f);
            float aM  = 1f + (bonus?.atkMul      ?? 0f);
            float sM  = 1f + (bonus?.atkSpeedMul ?? 0f);
            MaxHp     = Mathf.RoundToInt(def.hp * hpM);
            Hp        = MaxHp;
            CurAtk    = def.atk * aM;
            CurAtkSpd = def.atkSpeed * sM;
            CurRange  = def.atkRange;

            if (autoAttack) autoAttack.Configure(def.attackPattern, () => CurAtk, () => CurAtkSpd, () => CurRange);
            OnHpChanged?.Invoke(Hp, MaxHp);
        }

        public void TakeDamage(int dmg)
        {
            if (Hp <= 0) return;
            Hp = Mathf.Max(0, Hp - dmg);
            OnHpChanged?.Invoke(Hp, MaxHp);
            if (Hp == 0) OnDied?.Invoke();
        }

        void Update()
        {
            // 일시정지 중엔 드래그 X
            if (CombatManager.I != null && (CombatManager.I.IsPaused || CombatManager.I.IsFinished)) return;
            if (!Input.GetMouseButton(0)) return;
            // UI 위 클릭은 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 sp = Input.mousePosition;
            // 인벤토리 바 영역(하단 약 28% : 960/1920 → 50%지만 UX 권장 280px만 잡으면 ≈ 0.146)
            float bottomCut = Screen.height * 0.18f;
            if (sp.y < bottomCut) return;

            Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -_cam.transform.position.z));
            wp.y = Mathf.Clamp(wp.y, dragYMin, dragYMax);
            wp.z = 0f;
            _tr.position = Vector3.Lerp(_tr.position, wp, dragLerpSpeed * Time.deltaTime);
        }
    }
}
