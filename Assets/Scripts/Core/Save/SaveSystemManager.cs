using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TOME.Core
{
    [System.Serializable]
    public class SaveData
    {
        public List<string> clearedNodes      = new();
        public List<string> clearedStages     = new();
        public List<string> seenDialogues     = new();   // 한 번 본 대사 ID
        public List<string> unlockedChars     = new();   // 해금된 조합 캐릭터
        public List<string> collectedPickups  = new();   // 맵에서 주운 줍기 오브젝트 ID
        public string       lastNodeId;
        public int          coins;
        public long         savedAtUnix;
    }

    public class SaveSystemManager : MonoBehaviour
    {
        public static SaveSystemManager I { get; private set; }
        public SaveData Data { get; private set; } = new();

        string Path    => System.IO.Path.Combine(Application.persistentDataPath, "save.json");
        string TmpPath => Path + ".tmp";
        string BakPath => Path + ".bak";

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            Load();
        }

        void OnDestroy() { if (I == this) I = null; }

        public void Save()
        {
            Data.savedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // 임시 파일에 먼저 쓰고 원자적으로 교체 — 쓰기 중 크래시 시 기존 저장 보존
            File.WriteAllText(TmpPath, JsonUtility.ToJson(Data));
            if (File.Exists(Path))
                File.Replace(TmpPath, Path, BakPath);
            else
                File.Move(TmpPath, Path);
        }

        public bool IsNodeCleared(string id) => Data.clearedNodes.Contains(id);
        public void MarkNodeCleared(string id)
        {
            if (!Data.clearedNodes.Contains(id)) { Data.clearedNodes.Add(id); Save(); }
        }
        public bool IsCharUnlocked(string id) => Data.unlockedChars.Contains(id);
        public void UnlockChar(string id)
        {
            if (!Data.unlockedChars.Contains(id)) { Data.unlockedChars.Add(id); Save(); }
        }

        public void Load()
        {
            if (TryLoadFrom(Path)) return;
            // 본 파일 손상 시 백업으로 폴백 — 진행도 전체 소실 방지
            if (TryLoadFrom(BakPath)) { Debug.LogWarning("[Save] 본 저장 파일 손상, 백업에서 복구"); return; }
            Data = new SaveData();
        }

        bool TryLoadFrom(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (loaded == null) return false;
                Data = loaded;
                return true;
            }
            catch { return false; }
        }

        public bool HasSeenDialogue(string id) => Data.seenDialogues.Contains(id);
        public void MarkDialogueSeen(string id)
        {
            if (!Data.seenDialogues.Contains(id)) { Data.seenDialogues.Add(id); Save(); }
        }

        public bool IsPickupCollected(string id) => Data.collectedPickups.Contains(id);
        public void MarkPickupCollected(string id)
        {
            if (!Data.collectedPickups.Contains(id)) { Data.collectedPickups.Add(id); Save(); }
        }
    }
}
