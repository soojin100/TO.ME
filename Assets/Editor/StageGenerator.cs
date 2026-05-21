using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using TOME.Data;

namespace TOME.EditorTools
{
    /// <summary>stages.csv → StageSO 에셋 일괄 생성/갱신.
    /// 참조(적/캐릭터/보상/썸네일)는 에셋 "이름"으로 해석한다.
    /// 챕터 내 수치 상승은 행의 숫자로, 챕터 전환의 적/아트 교체는 행의 참조 이름으로 표현.</summary>
    public static class StageGenerator
    {
        const string CsvPath    = "Assets/CSV/stages.csv";
        const string OutRoot    = "Assets/Data/Stages";
        const int    EnemySlots = 3;   // 행당 최대 적 종류

        [MenuItem("Tools/Stage/Generate Stages from CSV")]
        public static void Generate()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[StageGenerator] CSV 없음: {CsvPath}");
                return;
            }

            var lines = File.ReadAllText(CsvPath).Split('\n');
            if (lines.Length < 2) { Debug.LogWarning("[StageGenerator] 데이터 행 없음"); return; }

            var header = SplitCsv(lines[0].TrimEnd('\r'));
            var col = new Dictionary<string, int>();
            for (int i = 0; i < header.Count; i++) col[header[i].Trim().ToLowerInvariant()] = i;

            int created = 0, updated = 0, warned = 0;
            var sb = new StringBuilder();

            for (int r = 1; r < lines.Length; r++)
            {
                var raw = lines[r].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var c = SplitCsv(raw);

                string Get(string key)
                {
                    return col.TryGetValue(key, out int idx) && idx < c.Count ? c[idx].Trim() : "";
                }

                string id = Get("id");
                if (string.IsNullOrEmpty(id)) continue;

                string chapter = Get("chapter");
                string folder  = string.IsNullOrEmpty(chapter)
                    ? OutRoot
                    : $"{OutRoot}/Chapter{chapter.PadLeft(2, '0')}";
                EnsureFolder(folder);

                string assetPath = $"{folder}/Stage_{id.Replace('-', '_')}.asset";
                var so = AssetDatabase.LoadAssetAtPath<StageSO>(assetPath);
                bool isNew = so == null;
                if (isNew) { so = ScriptableObject.CreateInstance<StageSO>(); AssetDatabase.CreateAsset(so, assetPath); }

                so.id    = id;
                so.title = Get("title");
                so.introText        = Get("introtext");
                so.clearedIntroText = Get("clearedintrotext");
                so.preDialogueId    = Get("predialogueid");
                so.postDialogueId   = Get("postdialogueid");
                so.timeLimit        = ParseF(Get("timelimit"), 60f);

                so.startCharacter = FindByName<CharacterSO>(Get("startcharacter"), id, "startCharacter", sb, ref warned);
                so.thumbnail      = FindByName<Sprite>(Get("thumbnail"), id, "thumbnail", sb, ref warned, optional: true);

                // spawns
                var spawns = new List<EnemySpawnEntry>();
                for (int s = 1; s <= EnemySlots; s++)
                {
                    string en = Get($"enemy{s}");
                    if (string.IsNullOrEmpty(en)) continue;
                    var eso = FindByName<EnemySO>(en, id, $"enemy{s}", sb, ref warned);
                    spawns.Add(new EnemySpawnEntry
                    {
                        enemy         = eso,
                        totalCount    = (int)ParseF(Get($"count{s}"), 3),
                        simultaneous  = (int)ParseF(Get($"simul{s}"), 1),
                        spawnInterval = ParseF(Get($"interval{s}"), 1.5f),
                        startDelay    = ParseF(Get($"delay{s}"), 0f),
                    });
                }
                so.spawns = spawns.ToArray();

                // rewards (';' 또는 '|' 구분)
                string rw = Get("rewards");
                var rewards = new List<RewardSO>();
                if (!string.IsNullOrEmpty(rw))
                    foreach (var name in rw.Split(';', '|'))
                    {
                        var t = name.Trim();
                        if (t.Length == 0) continue;
                        var rso = FindByName<RewardSO>(t, id, "rewards", sb, ref warned);
                        if (rso) rewards.Add(rso);
                    }
                so.rewards = rewards.ToArray();

                EditorUtility.SetDirty(so);
                if (isNew) created++; else updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StageGenerator] 완료 — 생성 {created}, 갱신 {updated}, 경고 {warned}\n{sb}");
        }

        static T FindByName<T>(string name, string stageId, string field, StringBuilder log, ref int warned, bool optional = false) where T : Object
        {
            if (string.IsNullOrEmpty(name)) return null;
            // 이름으로 후보를 찾고 파일명이 정확히 일치하는 첫 에셋 반환 (Sprite는 서브에셋 대응)
            string filter = typeof(T) == typeof(Sprite) ? name : $"{name} t:{typeof(T).Name}";
            var guids = AssetDatabase.FindAssets(filter);
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (typeof(T) == typeof(Sprite))
                {
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (sp != null && (sp.name == name || Path.GetFileNameWithoutExtension(p) == name)) return sp as T;
                }
                else
                {
                    var a = AssetDatabase.LoadAssetAtPath<T>(p);
                    if (a != null && Path.GetFileNameWithoutExtension(p) == name) return a;
                }
            }
            if (!optional)
            {
                log.AppendLine($"  [{stageId}] {field}: '{name}' {typeof(T).Name} 못 찾음");
                warned++;
            }
            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static float ParseF(string s, float def)
            => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;

        static List<string> SplitCsv(string line)
        {
            var result = new List<string>();
            var buf = new StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQ && i + 1 < line.Length && line[i + 1] == '"') { buf.Append('"'); i++; }
                    else inQ = !inQ;
                }
                else if (ch == ',' && !inQ) { result.Add(buf.ToString()); buf.Clear(); }
                else buf.Append(ch);
            }
            result.Add(buf.ToString());
            return result;
        }
    }
}
