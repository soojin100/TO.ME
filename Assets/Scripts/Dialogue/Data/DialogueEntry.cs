namespace TOME.Dialogue
{
    /// 대사 진행 중 특정 줄에서 발생하는 게임 이벤트.
    public enum DialogueTrigger
    {
        None = 0,
        NameInput,        // 이름 입력 팝업을 띄우고 입력 완료까지 대기
        StartBattle,      // 튜토리얼 전투 시작 (대사 종료)
        InspectWall,      // 벽 장신구 클릭 컷신 대기 (바를 정자 낙서 표시)
        InspectHolyWater,  // 침대 아래 성수병 클릭 컷신 대기 (유령 움찔)
        PlayCutscene,     // 범용성 컷신 트리거
        TutorialSummon,   // 튜토리얼 도입: 강아지 집만 남기고 암전 → 집 클릭 대기 → 강아지 소환 (기획서 p4~p5)
        TutorialDogLine,  // 소환된 강아지를 라인없는 것으로 재배치 (기획서 p7)
        TutorialEnemy,    // 맵에 공격 능력 없는 에너미 배치 (기획서 p14)
        TutorialBear      // 목이 덜렁덜렁한 곰인형을 중앙에 출력 (기획서 p11)
    }

    /// CSV 한 줄 매핑. 헤더 기반 파싱이라 컬럼 순서/추가에 유연하다.
    public struct DialogueEntry
    {
        public string id;
        public string speaker;
        public string text;         // {name} 토큰은 표시 시 플레이어 이름으로 치환
        public string next;         // 빈 문자열이면 종료
        public string chapter;      // 다시보기(지난 대화) 그룹 키
        public string speakerSprite;// 화자 스프라이트 리소스 키 (Resources 경로)
        public DialogueTrigger trigger;
        public string cutsceneId; // 범용성 컷신 트리거
        public string standing;   // 이 줄에서 표시할 스탠딩 키. 빈칸=변경 없음, "none"=숨김
        public string effect;     // 이 줄 동안 적용할 연출 키("shake" 등). 빈칸=연출 정지
    }
}
