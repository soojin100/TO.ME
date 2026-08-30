using UnityEngine;
using UnityEngine.U2D.Animation;
using TOME.Characters;
namespace TOME.Combat
{
    /// <summary>조합 결과 캐릭터 프리팹 루트. 외형·애니메이션만 책임.</summary>
    public class CharacterCore : MonoBehaviour
    {
        public CharacterSO Def { get; private set; }

        [SerializeField] SpriteRenderer body;
        [SerializeField] Animator       animator;
        [SerializeField] string         idleStateName = "Idle";

        bool _skinsRebound;

        void Awake() => RebindSpriteSkins();

        // PSB 본 리그를 wrapper prefab에 nested하면 SpriteSkin.rootBone이 인스턴스가 아니라
        // 원본(asset) 본을 가리켜 mesh deform이 정지 본 기준이 된다 → sprite가 안 움직임.
        // 런타임에 rootBone을 현재 인스턴스 본으로 재바인딩하고 boneTransforms도 이름으로 재연결.
        void RebindSpriteSkins()
        {
            if (_skinsRebound) return;
            var skins = GetComponentsInChildren<SpriteSkin>(true);
            if (skins == null || skins.Length == 0) return;

            // 인스턴스 본 후보 = 자식 전체 Transform (이름 매칭용)
            var allTr = GetComponentsInChildren<Transform>(true);

            foreach (var skin in skins)
            {
                // 1) rootBone을 같은 이름의 인스턴스 Transform으로 교체
                var curRoot = skin.rootBone;
                if (curRoot != null)
                {
                    var inst = FindByName(allTr, curRoot.name);
                    if (inst != null && inst != curRoot) skin.SetRootBone(inst);
                }
                // 2) boneTransforms도 이름으로 인스턴스 본에 재연결
                var bones = skin.boneTransforms;
                if (bones != null && bones.Length > 0)
                {
                    bool changed = false;
                    var rebound = new Transform[bones.Length];
                    for (int i = 0; i < bones.Length; i++)
                    {
                        rebound[i] = bones[i];
                        if (bones[i] != null)
                        {
                            var inst = FindByName(allTr, bones[i].name);
                            if (inst != null && inst != bones[i]) { rebound[i] = inst; changed = true; }
                        }
                    }
                    if (changed) skin.SetBoneTransforms(rebound);
                }
            }
            _skinsRebound = true;
        }

        static Transform FindByName(Transform[] pool, string name)
        {
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].name == name) return pool[i];
            return null;
        }

        public void RebindOnly()
        {
            _skinsRebound = false;  
            RebindSpriteSkins();
        }

        public void Bind(CharacterSO def)
        {
            Def = def;
            if (body)
            {
                if (def.icon) body.sprite = def.icon;
                body.color = def.bodyTint;
            }
            RebindSpriteSkins();
            // Instantiate 직후 Animator가 아직 초기화 전이면 Play가 무효화될 수 있어 Rebind로 보장.
            if (animator)
            {
                animator.Rebind();
                if (!string.IsNullOrEmpty(idleStateName)) animator.Play(idleStateName, 0, 0f);
                animator.Update(0f);   // 첫 프레임 즉시 적용
            }
        }
    }
}
