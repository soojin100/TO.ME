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
        public string       playerName = "제임스";   // 인트로 이름 입력값 (기본값)
        public bool         seenIntro;               // 첫 실행 튜토리얼 시청 여부
        public string       currentChapterId;        // 진행 중인 챕터 (보스 클리어 시 다음 챕터로 갱신)
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
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
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
        public void SetCurrentChapter(string id)
        {
            if (Data.currentChapterId == id) return;
            Data.currentChapterId = id;
            Save();
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

        public string PlayerName
        {
            get
            {
                var n = SanitizeName(Data.playerName);
                return string.IsNullOrWhiteSpace(n) ? "제임스" : n;
            }
        }
        public void SetPlayerName(string name)
        {
            name = SanitizeName(name);
            Data.playerName = string.IsNullOrWhiteSpace(name) ? "제임스" : name.Trim();
            Save();
        }

        // 완성형 한글/영문/숫자/공백만 남기고 불완전 한글 자모(ㄱ, ㅇ 등)·제어문자 제거.
        // Cafe24Ssurround 폰트에 자모 단독 글리프(U+3147 'ㅇ' 등)가 없어 □/공백으로 깨지는 것 방지.
        static string SanitizeName(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
            {
                // 한글 초/중/종성 자모(U+1100~11FF), 한글 호환 자모(U+3130~318F) = 불완전 글자 → 제거
                if ((c >= 'ᄀ' && c <= 'ᇿ') || (c >= '㄰' && c <= '㆏'))
                    continue;
                if (char.IsControl(c)) continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        public bool SeenIntro => Data.seenIntro;
        public void MarkIntroSeen()
        {
            if (!Data.seenIntro) { Data.seenIntro = true; Save(); }
        }

        public bool IsPickupCollected(string id) => Data.collectedPickups.Contains(id);
        public void MarkPickupCollected(string id)
        {
            if (!Data.collectedPickups.Contains(id)) { Data.collectedPickups.Add(id); Save(); }
        }
    }
}
