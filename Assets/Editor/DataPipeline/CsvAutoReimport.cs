using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TOME.EditorTools
{
    /// <summary>
    /// 엑셀 등 외부 에디터에서 CSV를 저장하면 Unity 포커스 복귀를 기다리지 않고
    /// 즉시 해당 에셋을 재임포트한다. FileSystemWatcher는 백그라운드 스레드에서
    /// 콜백이 오므로 변경 경로를 큐에 모았다가 메인 스레드(EditorApplication.update)에서 처리한다.
    /// </summary>
    [InitializeOnLoad]
    public static class CsvAutoReimport
    {
        const string WatchFolder = "Assets/CSV";   // 감시 대상 폴더 (없으면 Assets 전체)
        static FileSystemWatcher _watcher;
        static readonly HashSet<string> _pending = new();
        static readonly object _lock = new();

        static CsvAutoReimport()
        {
            EditorApplication.update += ProcessPending;
            EditorApplication.quitting += Dispose;
            Setup();
        }

        static void Setup()
        {
            string dir = Directory.Exists(WatchFolder) ? WatchFolder : "Assets";
            try
            {
                _watcher = new FileSystemWatcher(dir, "*.csv")
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };
                _watcher.Changed += OnChanged;
                _watcher.Created += OnChanged;
                _watcher.Renamed += OnChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CsvAutoReimport] 감시 설정 실패: {e.Message}");
            }
        }

        static void OnChanged(object sender, FileSystemEventArgs e)
        {
            // .meta 등 부수 파일 제외
            if (!e.FullPath.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase)) return;
            lock (_lock) _pending.Add(e.FullPath);
        }

        static void ProcessPending()
        {
            if (_pending.Count == 0) return;

            string[] paths;
            lock (_lock)
            {
                paths = new string[_pending.Count];
                _pending.CopyTo(paths);
                _pending.Clear();
            }

            string root = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/');
            bool any = false;
            foreach (var full in paths)
            {
                string norm = full.Replace('\\', '/');
                int idx = norm.IndexOf("/Assets/", System.StringComparison.OrdinalIgnoreCase);
                string assetPath = idx >= 0 ? norm.Substring(idx + 1)
                    : (norm.StartsWith(root) ? norm.Substring(root.Length + 1) : null);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/")) continue;
                if (!File.Exists(full)) continue;

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[CsvAutoReimport] 재임포트: {assetPath}");
                any = true;
            }
            if (any) AssetDatabase.Refresh();
        }

        static void Dispose()
        {
            if (_watcher == null) return;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        [MenuItem("Tools/CSV/모든 CSV 재임포트")]
        static void ReimportAll()
        {
            // 엑셀(xlsx) 마스터가 있으면 먼저 csv로 변환한 뒤 재임포트
            XlsxToCsvImporter.ConvertAll();

            string dir = Directory.Exists(WatchFolder) ? WatchFolder : "Assets";
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
            Debug.Log("[CsvAutoReimport] 모든 CSV 재임포트 완료");
        }
    }
}
