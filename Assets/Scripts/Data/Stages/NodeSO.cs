using UnityEngine;

namespace TOME.Data
{
    [System.Serializable]
    public class StatBonus
    {
        [Range(-1f, 2f)] public float atkMul       = 0f;   // +0.1 = +10%
        [Range(-1f, 2f)] public float atkSpeedMul  = 0f;
        [Range(-1f, 2f)] public float hpMul        = 0f;
        [Range(-1f, 2f)] public float moveSpeedMul = 0f;
    }

    [CreateAssetMenu(menuName = "TOME/Node", fileName = "Node_")]
    public class NodeSO : ScriptableObject
    {
        public string id;
        public string nodeName;                // "공원", "골목", "슈퍼"
        public Vector2 mapPosition;
        public Sprite  iconOnMap;

        public StageSO[] stages;
        public StatBonus bonus;                // B안: 노드별 보정
        public NodeSO[]  unlocksOnClear;
        public bool      unlockedByDefault;
    }
}
