using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace TOME.EditorTools
{
    /// <summary>세이브 디버그 메뉴. (To.You의 SaveDataDebugMenu를 TO.ME 세이브 구조에 맞춰 이식)
    /// 세이브는 Application.persistentDataPath/save.json(+.bak/.tmp) 평문 JSON.</summary>
    public static class SaveDataDebugMenu
    {
        static string Dir  => Application.persistentDataPath;
        static string Path => System.IO.Path.Combine(Dir, "save.json");
        static string Bak  => Path + ".bak";
        static string Tmp  => Path + ".tmp";

        // 런타임 SaveData와 동일 필드 (런타임 어셈블리 참조 없이 JSON 라운드트립용 미러)
        [System.Serializable]
        class SaveMirror
        {
            public List<string> clearedNodes     = new();
            public List<string> clearedStages    = new();
            public List<string> seenDialogues    = new();
            public List<string> unlockedChars    = new();
            public List<string> collectedPickups = new();
            public string lastNodeId;
            public int    coins;
            public long   savedAtUnix;
            public string playerName = "제임스";
            public bool   seenIntro;
        }

        [MenuItem("Tools/Save System/Clear All Save Data + PlayerPrefs")]
        public static void ClearAllSaveData()
        {
            int n = 0;
            foreach (var p in new[] { Path, Bak, Tmp })
                if (File.Exists(p)) { File.Delete(p); n++; }
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log($"[SaveDataDebugMenu] 세이브 파일 {n}개 + PlayerPrefs 삭제 완료");
        }

        [MenuItem("Tools/Save System/Reset Intro (seenIntro + 대사 c1_*)")]
        public static void ResetIntro()
        {
            if (!File.Exists(Path))
            {
                Debug.Log("[SaveDataDebugMenu] 세이브 없음 — 이미 첫 실행 상태입니다.");
                return;
            }
            try
            {
                var data = JsonUtility.FromJson<SaveMirror>(File.ReadAllText(Path)) ?? new SaveMirror();
                data.seenIntro = false;
                data.seenDialogues?.RemoveAll(id => id != null && id.StartsWith("c1_"));
                File.WriteAllText(Path, JsonUtility.ToJson(data));
                Debug.Log("[SaveDataDebugMenu] 인트로 초기화 완료 (seenIntro=false, c1_* 대사 제거). 다른 진행도는 보존.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveDataDebugMenu] 인트로 초기화 실패: {e.Message}");
            }
        }

        [MenuItem("Tools/Save System/Open Save Data Folder")]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Dir);
        }
    }
}
