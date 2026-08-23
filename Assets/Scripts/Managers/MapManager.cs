using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Data;

namespace TOME.Managers
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager I { get; private set; }

        [SerializeField] List<NodeSO> allNodes;
        [Tooltip("진행 순서대로의 챕터 목록. 저장된 진행 챕터를 복원하는 데 쓴다.")]
        [SerializeField] List<ChapterSO> allChapters;

        readonly HashSet<string> unlocked = new();

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            RebuildUnlockSet();
            // 세이브에 기록된 진행 챕터를 복원 — 없으면 GameManager가 원래 값을 유지한다.
            GameManager.I?.RestoreChapter(allChapters != null ? allChapters.ToArray() : null);
        }

        void OnDestroy() { if (I == this) I = null; }

        void RebuildUnlockSet()
        {
            unlocked.Clear();
            foreach (var n in allNodes)
                if (n && n.unlockedByDefault) unlocked.Add(n.id);

            if (SaveSystemManager.I != null)
                foreach (var id in SaveSystemManager.I.Data.clearedNodes)
                    PropagateUnlockFrom(id);
        }

        void PropagateUnlockFrom(string clearedId)
        {
            var node = allNodes.Find(n => n && n.id == clearedId);
            if (!node) return;
            foreach (var u in node.unlocksOnClear)
                if (u) unlocked.Add(u.id);
        }

        public bool IsUnlocked(NodeSO n) => n && unlocked.Contains(n.id);
        public IReadOnlyList<NodeSO> All => allNodes;

        public void MarkNodeCleared(NodeSO n)
        {
            if (!n) return;
            var save = SaveSystemManager.I?.Data;
            if (save != null && !save.clearedNodes.Contains(n.id))
            {
                save.clearedNodes.Add(n.id);
                SaveSystemManager.I.Save();
            }
            PropagateUnlockFrom(n.id);
        }
    }
}
