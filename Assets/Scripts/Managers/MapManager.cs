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

        readonly HashSet<string> unlocked = new();

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            RebuildUnlockSet();
        }

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
