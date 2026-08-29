using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace TOME.EditorTools
{
    /// <summary>
    /// Assets/CSV/Source/*.xlsx 를 감시해, 엑셀에서 저장되면 같은 이름의
    /// Assets/CSV/*.csv 로 자동 변환한다(외부 DLL 없이 ZipArchive+XML 파싱).
    /// 변환된 csv는 CsvAutoReimport가 이어서 재임포트한다.
    /// </summary>
    [InitializeOnLoad]
    public static class XlsxToCsvImporter
    {
        const string SourceFolder = "Assets/CSV";  // GameData.xlsx(엑셀 마스터) 위치 = csv 출력과 동일 폴더
        const string OutFolder    = "Assets/CSV";   // 시트별 csv 출력 폴더

        static FileSystemWatcher _watcher;
        static readonly HashSet<string> _pending = new();
        static readonly object _lock = new();

        static XlsxToCsvImporter()
        {
            EditorApplication.update += ProcessPending;
            EditorApplication.quitting += Dispose;
            Setup();
        }

        static void Setup()
        {
            if (!Directory.Exists(SourceFolder)) return;
            try
            {
                _watcher = new FileSystemWatcher(SourceFolder, "*.xlsx")
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };
                _watcher.Changed += OnChanged;
                _watcher.Created += OnChanged;
                _watcher.Renamed += OnChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception e) { Debug.LogWarning($"[XlsxToCsv] 감시 설정 실패: {e.Message}"); }
        }

        static void OnChanged(object s, FileSystemEventArgs e)
        {
            // 엑셀 임시 잠금파일(~$...) 제외
            string name = Path.GetFileName(e.FullPath);
            if (name.StartsWith("~$")) return;
            if (!e.FullPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) return;
            lock (_lock) _pending.Add(e.FullPath);
        }

        static void ProcessPending()
        {
            if (_pending.Count == 0) return;
            string[] paths;
            lock (_lock) { paths = new string[_pending.Count]; _pending.CopyTo(paths); _pending.Clear(); }

            bool any = false;
            foreach (var full in paths)
            {
                if (!File.Exists(full)) continue;
                try
                {
                    var made = ConvertFile(full);   // 시트별로 csv 생성
                    foreach (var csv in made) Debug.Log($"[XlsxToCsv] 변환: [{Path.GetFileName(full)}] → {csv}");
                    if (made.Count > 0) any = true;
                }
                catch (Exception ex) { Debug.LogError($"[XlsxToCsv] 변환 실패 {Path.GetFileName(full)}: {ex.Message}"); }
            }
            if (any) AssetDatabase.Refresh();
        }

        // 메뉴는 CsvAutoReimport의 "모든 CSV 재임포트"로 통합됨 (xlsx 변환 → csv 재임포트 일괄).
        public static void ConvertAll()
        {
            if (!Directory.Exists(SourceFolder)) { Debug.LogWarning($"[XlsxToCsv] 폴더 없음: {SourceFolder}"); return; }
            int n = 0;
            foreach (var f in Directory.GetFiles(SourceFolder, "*.xlsx"))
            {
                if (Path.GetFileName(f).StartsWith("~$")) continue;
                try { n += ConvertFile(f).Count; }
                catch (Exception ex) { Debug.LogError($"[XlsxToCsv] {Path.GetFileName(f)}: {ex.Message}"); }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[XlsxToCsv] 시트 {n}개 → csv 변환 완료");
        }

        // --- xlsx 파싱: 워크북의 모든 시트를 각각 "<시트이름>.csv" 로 변환 ---
        static List<string> ConvertFile(string xlsxPath)
        {
            var made = new List<string>();
            // 잠금 회피: 메모리로 복사 후 열기
            byte[] bytes = File.ReadAllBytes(xlsxPath);
            using var ms = new MemoryStream(bytes);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

            var shared = ReadSharedStrings(zip);
            foreach (var (sheetName, entry) in GetSheets(zip))
            {
                XDocument doc;
                using (var st = entry.Open()) doc = XDocument.Load(st);
                string csv = SheetToCsv(doc, shared);
                string outCsvPath = Path.Combine(OutFolder, SanitizeName(sheetName) + ".csv");
                File.WriteAllText(outCsvPath, csv, new UTF8Encoding(false));
                made.Add(outCsvPath);
            }
            return made;
        }

        // workbook.xml(시트 이름·순서) + .rels(rId→파일) 로 시트 이름→엔트리 매핑
        static IEnumerable<(string name, ZipArchiveEntry entry)> GetSheets(ZipArchive zip)
        {
            var wb = zip.GetEntry("xl/workbook.xml");
            var rels = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (wb == null || rels == null)
            {
                // 폴백: sheet1만
                var s = FindFirstSheet(zip);
                if (s != null) yield return ("Sheet1", s);
                yield break;
            }
            // rId → target
            var ridToTarget = new Dictionary<string, string>();
            XDocument relsDoc; using (var st = rels.Open()) relsDoc = XDocument.Load(st);
            foreach (var r in Descendants(relsDoc.Root, "Relationship"))
            {
                string id = (string)r.Attribute("Id");
                string tgt = (string)r.Attribute("Target");
                if (id != null && tgt != null) ridToTarget[id] = tgt;
            }
            XDocument wbDoc; using (var st = wb.Open()) wbDoc = XDocument.Load(st);
            foreach (var sh in Descendants(wbDoc.Root, "sheet"))
            {
                string name = (string)sh.Attribute("name") ?? "Sheet";
                // r:id 속성 (네임스페이스 무관하게 LocalName=id 인 속성 탐색)
                string rid = null;
                foreach (var a in sh.Attributes())
                    if (a.Name.LocalName == "id") { rid = a.Value; break; }
                if (rid == null || !ridToTarget.TryGetValue(rid, out var target)) continue;
                string full = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target.Replace("../", "");
                var e = zip.GetEntry(full) ?? zip.GetEntry("xl/" + target);
                if (e != null) yield return (name, e);
            }
        }

        static string SanitizeName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }

        static string SheetToCsv(XDocument doc, List<string> shared)
        {
            var rows = new List<string[]>();
            int maxCols = 0;
            foreach (var row in Descendants(doc.Root, "row"))
            {
                var cells = new SortedDictionary<int, string>();
                foreach (var c in Descendants(row, "c"))
                {
                    string r = (string)c.Attribute("r") ?? "";
                    int col = ColIndex(r);
                    string t = (string)c.Attribute("t");
                    string val = "";
                    var v = FirstLocal(c, "v");
                    if (t == "s")
                    {
                        if (v != null && int.TryParse(v.Value, out int idx) && idx >= 0 && idx < shared.Count)
                            val = shared[idx];
                    }
                    else if (t == "inlineStr")
                    {
                        var isEl = FirstLocal(c, "is");
                        if (isEl != null) val = ConcatText(isEl);
                    }
                    else if (v != null) val = v.Value;

                    if (col >= 0) cells[col] = val;
                }
                int rowMax = 0;
                foreach (var k in cells.Keys) rowMax = Mathf.Max(rowMax, k + 1);
                var arr = new string[rowMax];
                for (int i = 0; i < rowMax; i++) arr[i] = cells.TryGetValue(i, out var s) ? s : "";
                rows.Add(arr);
                maxCols = Mathf.Max(maxCols, rowMax);
            }

            // 끝의 완전 빈 행 제거
            while (rows.Count > 0 && IsEmpty(rows[rows.Count - 1])) rows.RemoveAt(rows.Count - 1);

            var sb = new StringBuilder();
            foreach (var row in rows)
            {
                for (int i = 0; i < maxCols; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(Escape(i < row.Length ? row[i] : ""));
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            var e = zip.GetEntry("xl/sharedStrings.xml");
            if (e == null) return list;
            XDocument doc;
            using (var st = e.Open()) doc = XDocument.Load(st);
            foreach (var si in Descendants(doc.Root, "si"))
                list.Add(ConcatText(si));
            return list;
        }

        static ZipArchiveEntry FindFirstSheet(ZipArchive zip)
        {
            foreach (var e in zip.Entries)
                if (e.FullName.StartsWith("xl/worksheets/") && e.FullName.EndsWith(".xml"))
                    return e;
            return null;
        }

        static string ConcatText(XElement el)
        {
            var sb = new StringBuilder();
            foreach (var t in Descendants(el, "t")) sb.Append(t.Value);
            return sb.ToString();
        }

        static IEnumerable<XElement> Descendants(XElement root, string local)
        {
            foreach (var e in root.Descendants())
                if (e.Name.LocalName == local) yield return e;
        }
        static XElement FirstLocal(XElement parent, string local)
        {
            foreach (var e in parent.Elements())
                if (e.Name.LocalName == local) return e;
            return null;
        }

        static int ColIndex(string cellRef)
        {
            int col = 0; bool any = false;
            foreach (char ch in cellRef)
            {
                if (ch >= 'A' && ch <= 'Z') { col = col * 26 + (ch - 'A' + 1); any = true; }
                else if (ch >= 'a' && ch <= 'z') { col = col * 26 + (ch - 'a' + 1); any = true; }
                else break;
            }
            return any ? col - 1 : -1;
        }

        static bool IsEmpty(string[] row)
        {
            foreach (var s in row) if (!string.IsNullOrEmpty(s)) return false;
            return true;
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            bool need = s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0;
            if (!need) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        static void Dispose()
        {
            if (_watcher == null) return;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }
}
