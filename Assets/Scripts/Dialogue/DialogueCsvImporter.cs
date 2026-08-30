using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
namespace TOME.Dialogue
{
    /// 런타임 대사 CSV 로더. 헤더 기반이라 컬럼 순서/추가에 유연하다.
    /// 인식 컬럼: id, speaker, text, next, chapter, speakerSprite, trigger, cutsceneId, standing, effect
    public static class DialogueCsvImporter
    {
        public static Dictionary<string, DialogueEntry> LoadDialogue(TextAsset csv)
        {
            var dict = new Dictionary<string, DialogueEntry>(64);
            if (csv == null) return dict;

            var lines = csv.text.Split('\n');
            if (lines.Length == 0) return dict;

            var header = CsvUtility.SplitLine(lines[0].TrimEnd('\r'));
            int Col(string name)
            {
                for (int j = 0; j < header.Count; j++)
                    if (string.Equals(header[j].Trim(), name, System.StringComparison.OrdinalIgnoreCase))
                        return j;
                return -1;
            }
            int iId = Col("id"), iSpeaker = Col("speaker"), iText = Col("text"), iNext = Col("next");
            int iChapter = Col("chapter"), iSprite = Col("speakerSprite"), iTrigger = Col("trigger");
            int iCutscene = Col("cutsceneId");
            int iStanding = Col("standing"), iEffect = Col("effect");


            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;
                var c = CsvUtility.SplitLine(line);

                string Get(int idx) => idx >= 0 && idx < c.Count ? c[idx] : string.Empty;

                var e = new DialogueEntry {
                    id            = Get(iId),
                    speaker       = Get(iSpeaker),
                    text          = Get(iText).Replace("\\n", "\n"),
                    next          = Get(iNext),
                    chapter       = Get(iChapter),
                    speakerSprite = Get(iSprite),
                    trigger       = ParseTrigger(Get(iTrigger)),
                    cutsceneId    = Get(iCutscene),
                    standing      = Get(iStanding),
                    effect        = Get(iEffect)
                };
                if (!string.IsNullOrEmpty(e.id)) dict[e.id] = e;
            }
            return dict;
        }

        static DialogueTrigger ParseTrigger(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return DialogueTrigger.None;
            return System.Enum.TryParse(s.Trim(), true, out DialogueTrigger t) ? t : DialogueTrigger.None;
        }
    }
}
