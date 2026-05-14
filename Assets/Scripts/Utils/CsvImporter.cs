using System.Collections.Generic;
using UnityEngine;
using TOME.Data;

namespace TOME.Utils
{
    /// 런타임 대사 CSV 로더. 헤더: id,speaker,text,next
    public static class CsvImporter
    {
        public static Dictionary<string, DialogueEntry> LoadDialogue(TextAsset csv)
        {
            var dict = new Dictionary<string, DialogueEntry>(64);
            if (csv == null) return dict;

            var lines = csv.text.Split('\n');
            for (int i = 1; i < lines.Length; i++) // skip header
            {
                var line = lines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;
                var c = SplitCsv(line);
                if (c.Count < 3) continue;
                var e = new DialogueEntry {
                    id      = c[0],
                    speaker = c[1],
                    text    = c[2].Replace("\\n", "\n"),
                    next    = c.Count > 3 ? c[3] : string.Empty
                };
                if (!string.IsNullOrEmpty(e.id)) dict[e.id] = e;
            }
            return dict;
        }

        // 쉼표 안 따옴표 안전 처리(간이)
        static List<string> SplitCsv(string line)
        {
            var result = new List<string>(4);
            var buf = new System.Text.StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"') { inQ = !inQ; continue; }
                if (ch == ',' && !inQ) { result.Add(buf.ToString()); buf.Clear(); }
                else buf.Append(ch);
            }
            result.Add(buf.ToString());
            return result;
        }
    }
}
