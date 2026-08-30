using System.Collections.Generic;
using System.Text;

using TOME.Dialogue;
namespace TOME.Core
{
    /// <summary>CSV 한 줄 분해 공용 유틸. 쉼표/따옴표 처리, 따옴표 안의 "" 는 리터럴 " 로 이스케이프.
    /// 런타임 대사 로더(DialogueCsvImporter)와 에디터 CSV 생성기들이 같은 규칙을 공유한다 —
    /// 파서가 여러 벌이면 따옴표 처리 규칙이 미묘하게 갈라져 시트에 따라 한쪽만 깨진다.</summary>
    public static class CsvUtility
    {
        public static List<string> SplitLine(string line)
        {
            var result = new List<string>(8);
            var buf = new StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQ && i + 1 < line.Length && line[i + 1] == '"') { buf.Append('"'); i++; }
                    else inQ = !inQ;
                    continue;
                }
                if (ch == ',' && !inQ) { result.Add(buf.ToString()); buf.Clear(); }
                else buf.Append(ch);
            }
            result.Add(buf.ToString());
            return result;
        }
    }
}
