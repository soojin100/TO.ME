namespace TOME.Data
{
    /// CSV 한 줄 매핑. id, speaker, text, next(optional)
    public struct DialogueEntry
    {
        public string id;
        public string speaker;
        public string text;
        public string next;     // 빈 문자열이면 종료
    }
}
