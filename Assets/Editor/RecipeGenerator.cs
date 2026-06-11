using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using TOME.Data;

namespace TOME.EditorTools
{
    /// <summary>recipes.csv → RecipeSO 에셋 일괄 생성/갱신.
    /// 파일명은 재료 조합 기반 영문: Recipe_{정렬된 영문 아이템 id...} (예: Recipe_Heart_HolyWater_Star).
    /// 누가 봐도 무슨 조합법인지 알 수 있고, 조합이 고유하므로 이름도 고유하다. (한글은 파일명에 쓰지 않음)
    /// 재료(ingredients)는 ItemSO.displayName 으로 해석(';' 구분, 중복·순서 보존).
    /// 결과물 이름/능력은 resultName·ability 필드(한글)로 들어간다. result(CharacterSO)는 건드리지 않는다.
    /// 기존 손수 만든 레시피(Recipe_Red 등)와 재료가 완전히 같은 조합은 생성하지 않는다.
    /// 생성기 소유 표시는 resultId(비어있지 않음). 손수 만든 레시피는 resultId가 비어 있어 보존된다.</summary>
    public static class RecipeGenerator
    {
        const string CsvPath = "Assets/CSV/recipes.csv";
        const string OutRoot = "Assets/Data/Recipes";

        class Row
        {
            public string id, resultName, ability, notes, key, name;
            public List<ItemSO> ings;
        }

        [MenuItem("Tools/Recipe/Generate Recipes from CSV")]
        public static void Generate()
        {
            if (!File.Exists(CsvPath)) { Debug.LogError($"[RecipeGenerator] CSV 없음: {CsvPath}"); return; }

            var lines = File.ReadAllText(CsvPath).Split('\n');
            if (lines.Length < 2) { Debug.LogWarning("[RecipeGenerator] 데이터 행 없음"); return; }

            // displayName → ItemSO 맵 구축
            var byName = new Dictionary<string, ItemSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:ItemSO"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var it = AssetDatabase.LoadAssetAtPath<ItemSO>(p);
                if (it != null && !string.IsNullOrEmpty(it.displayName))
                    byName[it.displayName.Trim()] = it;
            }

            var header = SplitCsv(lines[0].TrimEnd('\r'));
            var col = new Dictionary<string, int>();
            for (int i = 0; i < header.Count; i++) col[header[i].Trim().ToLowerInvariant()] = i;

            EnsureFolder(OutRoot);
            int warned = 0;
            var log = new StringBuilder();

            // 1) CSV 행 파싱(재료 해석 + 이름/키 산출)
            var rows = new List<Row>();
            for (int r = 1; r < lines.Length; r++)
            {
                var raw = lines[r].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var c = SplitCsv(raw);
                string Get(string key) => col.TryGetValue(key, out int idx) && idx < c.Count ? c[idx].Trim() : "";

                string resultName = Get("resultname");
                if (string.IsNullOrEmpty(resultName)) continue;

                var ings = new List<ItemSO>();
                string ingRaw = Get("ingredients");
                if (!string.IsNullOrEmpty(ingRaw))
                    foreach (var part in ingRaw.Split(';'))
                    {
                        var nm = part.Trim();
                        if (nm.Length == 0) continue;
                        if (byName.TryGetValue(nm, out var it)) ings.Add(it);
                        else { log.AppendLine($"  [{resultName}] 재료 '{nm}' ItemSO 못 찾음"); warned++; }
                    }

                string en = Get("resulten");
                rows.Add(new Row
                {
                    id = Get("id"), resultName = resultName,
                    ability = Get("ability"), notes = Get("notes"),
                    ings = ings,
                    key = string.Join(",", ings.Where(i => i).Select(i => i.id).OrderBy(s => s)),
                    // 파일명: 결과물 영문명(resultEn) 기반. 비어 있으면 재료 조합으로 폴백.
                    name = !string.IsNullOrEmpty(en) ? "Recipe_" + Sanitize(en) : NameFromIngredients(ings),
                });
            }

            // 2) 이번 생성 대상 이름 집합
            var managedNames = new HashSet<string>(rows.Select(x => x.name));

            // 3) 생성기 소유(resultId 채워짐) 에셋 중 이번 대상에 없는 것 정리 — 과거 번호/한글 이름 제거.
            //    손수 만든 레시피(resultId 빈 값)는 건드리지 않는다.
            int purged = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:RecipeSO", new[] { OutRoot }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                string stem = Path.GetFileNameWithoutExtension(p);
                if (managedNames.Contains(stem)) continue;
                var rec = AssetDatabase.LoadAssetAtPath<RecipeSO>(p);
                if (rec == null || string.IsNullOrEmpty(rec.resultId)) continue;  // 손수 제작분 보존
                AssetDatabase.DeleteAsset(p);
                purged++;
            }

            // 4) 생성기가 관리하지 않는 기존 레시피(손수 제작)의 재료조합 키 — 동일 조합은 그쪽을 살림
            var reservedKeys = new Dictionary<string, string>();
            foreach (var guid in AssetDatabase.FindAssets("t:RecipeSO"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                string stem = Path.GetFileNameWithoutExtension(p);
                if (managedNames.Contains(stem)) continue;
                var rec = AssetDatabase.LoadAssetAtPath<RecipeSO>(p);
                if (rec == null) continue;
                var k = rec.Key();
                if (!string.IsNullOrEmpty(k) && !reservedKeys.ContainsKey(k)) reservedKeys[k] = stem;
            }

            // 5) 생성/갱신
            int created = 0, updated = 0, skipped = 0;
            var seenKeys = new HashSet<string>();
            foreach (var row in rows)
            {
                string assetPath = $"{OutRoot}/{row.name}.asset";

                if (!string.IsNullOrEmpty(row.key))
                {
                    if (reservedKeys.TryGetValue(row.key, out var owner))
                    {
                        if (AssetDatabase.LoadAssetAtPath<RecipeSO>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
                        log.AppendLine($"  [{row.resultName}] 기존 '{owner}' 와 동일 조합 — 생략");
                        skipped++; continue;
                    }
                    if (!seenKeys.Add(row.key))
                    {
                        if (AssetDatabase.LoadAssetAtPath<RecipeSO>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
                        log.AppendLine($"  [{row.resultName}] 동일 조합 중복 — 생략");
                        skipped++; continue;
                    }
                }

                var so = AssetDatabase.LoadAssetAtPath<RecipeSO>(assetPath);
                bool isNew = so == null;
                if (isNew) { so = ScriptableObject.CreateInstance<RecipeSO>(); AssetDatabase.CreateAsset(so, assetPath); }

                so.ingredients = row.ings.ToArray();
                so.resultId    = string.IsNullOrEmpty(row.id) ? row.name : row.id;  // 소유 표시(항상 채움)
                so.resultName  = row.resultName;
                so.ability     = row.ability;
                so.notes       = row.notes;
                // result(CharacterSO)는 건드리지 않음

                EditorUtility.SetDirty(so);
                if (isNew) created++; else updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RecipeGenerator] 완료 — 생성 {created}, 갱신 {updated}, 생략 {skipped}, 정리 {purged}, 경고 {warned}\n{log}");
        }

        // 재료 id를 정렬(Key 규칙과 동일)해 PascalCase로 이어붙임. 재료 없으면 Fallback.
        static string NameFromIngredients(List<ItemSO> ings)
        {
            var ids = ings.Where(i => i && !string.IsNullOrEmpty(i.id)).Select(i => i.id).OrderBy(s => s).ToList();
            if (ids.Count == 0) return "Recipe_Fallback";
            return "Recipe_" + string.Join("_", ids.Select(Pascal));
        }

        static string Pascal(string id)
            => string.IsNullOrEmpty(id) ? id : char.ToUpperInvariant(id[0]) + id.Substring(1);

        // 파일명 불가 문자·공백만 정리 (영문명은 그대로 유지)
        static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name.Trim())
                sb.Append(System.Array.IndexOf(invalid, ch) >= 0 || ch == ' ' ? '_' : ch);
            return sb.ToString();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

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
