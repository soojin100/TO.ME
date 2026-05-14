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
        public string       lastNodeId;
        public int          coins;
        public long         savedAtUnix;
    }

    public class SaveSystemManager : MonoBehaviour
    {
        public static SaveSystemManager I { get; private set; }
        public SaveData Data { get; private set; } = new();

        string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Save()
        {
            Data.savedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            File.WriteAllText(Path, JsonUtility.ToJson(Data));
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
            if (!File.Exists(Path)) { Data = new SaveData(); return; }
            try { Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path)) ?? new SaveData(); }
            catch { Data = new SaveData(); }
        }

        public bool HasSeenDialogue(string id) => Data.seenDialogues.Contains(id);
        public void MarkDialogueSeen(string id)
        {
            if (!Data.seenDialogues.Contains(id)) { Data.seenDialogues.Add(id); Save(); }
        }
    }
}
