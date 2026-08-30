using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TOME.Characters;
using TOME.Core;
namespace TOME.EditorTools
{
    /// <summary>characters.csv → CharacterSO 에셋 일괄 생성/갱신.
    /// 파일명 Char_{id} (id는 결과물 영문명). id/displayName/ability 만 세팅하고
    /// 프리팹·아이콘·스탯·애니 등 나머지는 건드리지 않는다(기존 캐릭터 연결 보존).
    /// 조합 결과 캐릭터(아트 미정)는 스탯 기본값으로 생성된다.</summary>
    public static class CharacterGenerator
    {
        const string CsvPath = "Assets/CSV/characters.csv";
        const string OutRoot = "Assets/Data/Characters";

        [MenuItem("Tools/Character/Generate Characters from CSV")]
        public static void Generate()
        {
            if (!File.Exists(CsvPath)) { Debug.LogError($"[CharacterGenerator] CSV 없음: {CsvPath}"); return; }

            var lines = File.ReadAllText(CsvPath).Split('\n');
            if (lines.Length < 2) { Debug.LogWarning("[CharacterGenerator] 데이터 행 없음"); return; }

            var header = CsvUtility.SplitLine(lines[0].TrimEnd('\r'));
            var col = new Dictionary<string, int>();
            for (int i = 0; i < header.Count; i++) col[header[i].Trim().ToLowerInvariant()] = i;

            EditorAssetUtility.EnsureFolder(OutRoot);
            int created = 0, updated = 0;

            for (int r = 1; r < lines.Length; r++)
            {
                var raw = lines[r].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var c = CsvUtility.SplitLine(raw);

                string Get(string key) => col.TryGetValue(key, out int idx) && idx < c.Count ? c[idx].Trim() : "";

                string id = Get("id");
                if (string.IsNullOrEmpty(id)) continue;

                string assetPath = $"{OutRoot}/Char_{id}.asset";
                var so = AssetDatabase.LoadAssetAtPath<CharacterSO>(assetPath);
                bool isNew = so == null;
                if (isNew) { so = ScriptableObject.CreateInstance<CharacterSO>(); AssetDatabase.CreateAsset(so, assetPath); }

                so.id          = id;
                so.displayName = Get("displayname");
                so.abilityDescription = Get("ability");
                // 프리팹·아이콘·스탯·애니 등은 보존(건드리지 않음)

                EditorUtility.SetDirty(so);
                if (isNew) created++; else updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CharacterGenerator] 완료 — 생성 {created}, 갱신 {updated}");
        }


    }
}
