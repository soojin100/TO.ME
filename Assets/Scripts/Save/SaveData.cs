using System.Collections.Generic;

namespace TOME.Save
{
    /// <summary>스테이지별 누적 클리어 횟수. JsonUtility가 Dictionary를 못 다뤄 리스트로 직렬화한다.</summary>
    [System.Serializable]
    public class StageClearRecord
    {
        public string id;
        public int    count;
    }

    /// <summary>save.json 으로 직렬화되는 진행도 스냅샷. 읽고/쓰는 주체는 <see cref="SaveSystemManager"/>.</summary>
    [System.Serializable]
    public class SaveData
    {
        public List<string> clearedNodes      = new();
        public List<string> clearedStages     = new();
        public List<StageClearRecord> stageClearCounts = new();   // 스테이지별 승리 횟수 (기획서: 요구치 1~4 채워야 개방)
        public List<string> seenDialogues     = new();   // 한 번 본 대사 ID
        public List<string> unlockedChars     = new();   // 해금된 조합 캐릭터
        public List<string> collectedPickups  = new();   // 맵에서 주운 줍기 오브젝트 ID
        public string       lastNodeId;
        public int          coins;
        public long         savedAtUnix;
        public string       playerName = SaveSystemManager.DefaultPlayerName;   // 인트로 이름 입력값
        public bool         seenIntro;               // 첫 실행 튜토리얼 시청 여부
        public string       currentChapterId;        // 진행 중인 챕터 (보스 클리어 시 다음 챕터로 갱신)
    }
}
