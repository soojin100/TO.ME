using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TOME.Core;
using TOME.Crafting;
namespace TOME.EditorTools
{
    /// <summary>items.csv → ItemSO 에셋 일괄 생성/갱신.
    /// 파일명은 Item_{PascalId}.asset (기존 에셋이 있으면 갱신하여 guid·icon 보존).
    /// icon 은 건드리지 않는다(아트 연결 보존).</summary>
    public static class ItemGenerator
    {
        const string CsvPath = "Assets/CSV/items.csv";
        const string OutRoot = "Assets/Data/Items";

        [MenuItem("Tools/Item/Generate Items from CSV")]
        public static void Generate()
        {
            if (!File.Exists(CsvPath)) { Debug.LogError($"[ItemGenerator] CSV 없음: {CsvPath}"); return; }

            var lines = File.ReadAllText(CsvPath).Split('\n');
            if (lines.Length < 2) { Debug.LogWarning("[ItemGenerator] 데이터 행 없음"); return; }

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

                string assetPath = $"{OutRoot}/Item_{EditorAssetUtility.Pascal(id)}.asset";
                var so = AssetDatabase.LoadAssetAtPath<ItemSO>(assetPath);
                bool isNew = so == null;
                if (isNew) { so = ScriptableObject.CreateInstance<ItemSO>(); AssetDatabase.CreateAsset(so, assetPath); }

                so.id          = id;
                so.displayName = Get("displayname");
                so.tier        = ParseTier(Get("tier"));
                // icon 은 의도적으로 건드리지 않음

                EditorUtility.SetDirty(so);
                if (isNew) created++; else updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemGenerator] 완료 — 생성 {created}, 갱신 {updated}");
        }

        static ItemTier ParseTier(string s)
        {
            return Enum.TryParse<ItemTier>(s, true, out var t) ? t : ItemTier.Basic;
        }

    }
}
