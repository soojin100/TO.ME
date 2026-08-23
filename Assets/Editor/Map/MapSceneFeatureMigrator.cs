using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Interactables;
using TOME.Data;
using TOME.Managers;
using TOME.Map;

namespace TOME.EditorTools
{
    /// <summary>
    /// 구 Room_* 씬의 기능 계층을 새 Map_* 씬으로 옮기고, 배경 스프라이트에 클릭 인터랙션을 붙인다.
    /// 템플릿 씬은 저장하지 않으므로 원본 Room_*.unity는 그대로 유지된다.
    /// 여러 번 실행해도 안전하다 — 이전에 옮겨둔 루트를 먼저 지우고 다시 옮긴다.
    /// </summary>
    public static class MapSceneFeatureMigrator
    {
        const string MapSceneRoot    = "Assets/Scenes/Maps";
        const string OutlineMaterial = "Assets/Shaders/SpriteOutlineMaterial.mat";
        const string PickupRootName  = "Pickups";
        const string NodeAssetRoot   = "Assets/Data/Nodes";
        const string TileRoot        = "Assets/Sprites/Game/Match";
        const string ItemCsv         = "Assets/CSV/items.csv";
        const float  PickupWorldSize = 1.2f;   // 줍기 아이템 목표 높이(월드 유닛)

        /// <summary>Map 씬 → items.csv 의 map 컬럼 값. 어느 아이템이 어느 방에 있는지는 원본 데이터가 정한다.</summary>
        static readonly Dictionary<string, string[]> MapItemGroups = new()
        {
            { "Room",    new[] { "방", "주인의 방", "공통" } },
            { "Kitchen", new[] { "주방", "공통" } },
            { "Porch",   new[] { "현관", "공통" } },
            { "Yard",    new[] { "마당", "공통" } },
        };

        /// <summary>챕터별 스테이지 버튼 타일. 맵 테마에 맞춘 색 — 침실 파랑, 주방 주황, 현관 노랑, 마당 초록.</summary>
        static readonly Dictionary<int, string> ChapterTile = new()
        {
            { 1, "tile_02_blue.png"   },
            { 2, "tile_06_orange.png" },
            { 3, "tile_04_yellow.png" },
            { 4, "tile_03_green.png"  },
        };
        const int   PickupSortingOffset = 10;   // 배경 최상단 레이어보다 이만큼 위
        const float PickupGap           = 0.4f; // 아이템 사이 최소 간격(월드 유닛)
        const float PickupEdgeMargin    = 0.5f; // 맵 좌우 끝에서 띄울 여백

        /// <summary>Map 씬 → 기능을 가져올 템플릿 씬, 그리고 이 맵이 담당하는 챕터.
        /// Map_Room은 도감·컷신·강아지가 있는 Room_Bedroom에서, 나머지는 인터랙션이 가장 촘촘한 Room_Hallway에서 가져온다.
        /// 챕터 번호는 스테이지 노드(Node_{챕터}_{순번})를 물릴 때 쓴다. 0이면 스테이지가 없는 맵.
        /// 순서는 강아지의 집 탈출 동선: 침실 → 주방 → 현관 → 마당.</summary>
        static readonly (string Map, string Template, int Chapter)[] Targets =
        {
            ("Room",    "Assets/Scenes/Room_Bedroom.unity", 1),
            ("Kitchen", "Assets/Scenes/Room_Hallway.unity", 2),
            ("Porch",   "Assets/Scenes/Room_Hallway.unity", 3),
            ("Yard",    "Assets/Scenes/Room_Hallway.unity", 4),
        };

        // 템플릿에서 가져오지 "않을" 루트. 구 배경 아트만 두고 오고 나머지는 전부 가져온다.
        // 허용목록으로 두면 Dog_halfside처럼 루트에 놓인 오브젝트를 조용히 빠뜨린다.
        static readonly HashSet<string> RootsToSkip = new() { "World" };

        // 위치를 건드리지 않는 루트(화면 UI·매니저·카메라·조명). 나머지 루트는 새 맵 안으로 옮긴다.
        static readonly HashSet<string> RootsKeepingPosition =
            new() { "UI", "Managers", "Main Camera", "Directional Light", "Cutscenes" };

        [MenuItem("Tools/Map Background/Migrate Features Into Map Scenes")]
        public static void MigrateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Material outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterial);
            if (outline == null)
                Debug.LogWarning($"[MapSceneFeatureMigrator] 아웃라인 머터리얼 없음: {OutlineMaterial}. SpriteHighlight는 붙지만 외곽선이 꺼진 상태가 된다.");

            foreach ((string mapName, string templatePath, int chapter) in Targets)
                Migrate(mapName, templatePath, chapter, outline);

            AssetDatabase.SaveAssets();
            Debug.Log($"[MapSceneFeatureMigrator] Map 씬 {Targets.Length}개 기능 이관 완료");
        }

        static void Migrate(string mapName, string templatePath, int chapter, Material outline)
        {
            string mapPath = $"{MapSceneRoot}/Map_{mapName}.unity";
            Scene map = EditorSceneManager.OpenScene(mapPath, OpenSceneMode.Single);

            GameObject background = map.GetRootGameObjects()
                .FirstOrDefault(go => go.name == $"{mapName}_Background");
            if (!TryGetSpriteBounds(background, out Bounds newBounds))
            {
                Debug.LogError($"[MapSceneFeatureMigrator] Map_{mapName}: '{mapName}_Background' 없음 — 건너뜀");
                return;
            }

            // 재실행 대비: 배경만 남기고 이전 이관물과 생성기 카메라를 모두 지운다.
            foreach (GameObject go in map.GetRootGameObjects())
                if (go != background)
                    Object.DestroyImmediate(go);

            // 템플릿을 추가로 열고 필요한 루트만 Map 씬으로 "이동"시킨다.
            // 이동은 프리팹 인스턴스와 루트 간 참조를 보존한다(Instantiate는 프리팹 연결이 끊긴다).
            Scene template = EditorSceneManager.OpenScene(templatePath, OpenSceneMode.Additive);

            GameObject templateWorld = template.GetRootGameObjects().FirstOrDefault(g => g.name == "World");

            var moved = new List<GameObject>();
            foreach (GameObject src in template.GetRootGameObjects())
            {
                if (RootsToSkip.Contains(src.name)) continue;
                SceneManager.MoveGameObjectToScene(src, map);
                moved.Add(src);
            }

            // World 안에 섞여 있는 기능 오브젝트만 건져낸다 — 배경 아트는 템플릿에 두고 온다.
            MigrateNavigator(templateWorld, map, moved);

            // 템플릿은 저장하지 않고 닫는다 → 디스크의 원본 씬은 변경되지 않는다.
            EditorSceneManager.CloseScene(template, true);

            SetupCamera(moved, newBounds);
            List<StageNodeButton> stages = AssignStageNodes(moved, chapter);
            List<float> stageXs = LayOutStageButtons(stages, newBounds, mapName);
            StyleStageButtons(stages, chapter, mapName);
            SetupNavigator(moved, background, stages.Count);
            WireManagers(moved, mapName);
            PlaceStrayRoots(moved, newBounds, stageXs, mapName);
            SetupStageGate(moved, stages);
            int touched = SetupInteractables(background, outline);
            BuildPickups(mapName, map, moved);
            LayOutPickups(moved, background, newBounds, stageXs);

            EditorSceneManager.MarkSceneDirty(map);
            EditorSceneManager.SaveScene(map);
            Debug.Log($"[MapSceneFeatureMigrator] Map_{mapName} ← {System.IO.Path.GetFileNameWithoutExtension(templatePath)}: " +
                      $"챕터 {chapter}, 루트 {moved.Count}개, 스테이지 {stages.Count}개, 인터랙션 {touched}개");
        }

        /// <summary>ScreenNavigator는 World/ScrollSections 아래 있어서 루트 이동만으로는 안 따라온다.</summary>
        static void MigrateNavigator(GameObject templateWorld, Scene map, List<GameObject> moved)
        {
            if (templateWorld == null) return;
            ScreenNavigator nav = templateWorld.GetComponentInChildren<ScreenNavigator>(true);
            if (nav == null) return;

            nav.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(nav.gameObject, map);
            moved.Add(nav.gameObject);

            // 구 고정 앵커(Section_0..6)는 더 이상 안 쓴다 — 구역은 런타임에 계산된다.
            foreach (Transform child in nav.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);
        }

        /// <summary>이 맵에 있어야 할 줍기 아이템을 items.csv 기준으로 만든다.
        /// 템플릿에서 복사하면 침실 물건(당근인형·삼국지 책)이 주방·마당까지 따라온다.
        /// 아이콘이 없는 아이템은 화면에 안 보이므로 만들지 않고 목록으로 알린다.</summary>
        static void BuildPickups(string mapName, Scene map, List<GameObject> moved)
        {
            if (!MapItemGroups.TryGetValue(mapName, out string[] groups)) return;
            if (!File.Exists(ItemCsv))
            {
                Debug.LogWarning($"[MapSceneFeatureMigrator] {ItemCsv} 없음 — 줍기 아이템을 만들지 않는다");
                return;
            }

            Dictionary<string, ItemSO> byId = new();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemSO"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null && !string.IsNullOrEmpty(item.id)) byId[item.id] = item;
            }

            var root = new GameObject(PickupRootName);
            SceneManager.MoveGameObjectToScene(root, map);
            moved.Add(root);

            var placed = new List<string>();
            var noIcon = new List<string>();
            string[] lines = File.ReadAllLines(ItemCsv);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cell = lines[i].Split(',');
                if (cell.Length < 4) continue;
                string id = cell[0].Trim(), display = cell[1].Trim(), group = cell[3].Trim();
                if (System.Array.IndexOf(groups, group) < 0) continue;

                if (!byId.TryGetValue(id, out ItemSO item) || item.icon == null)
                {
                    noIcon.Add(display.Length > 0 ? display : id);
                    continue;
                }

                var go = new GameObject($"Pickup_{id}");
                go.transform.SetParent(root.transform, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = item.icon;
                float h = sr.sprite.bounds.size.y;
                if (h > 0.0001f) go.transform.localScale = Vector3.one * (PickupWorldSize / h);

                var col = go.AddComponent<PolygonCollider2D>();
                col.isTrigger = true;
                var hl = go.AddComponent<SpriteHighlight>();
                var hlSo = new SerializedObject(hl);
                hlSo.FindProperty("outlineMaterial").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterial);
                hlSo.ApplyModifiedPropertiesWithoutUndo();

                var pickup = go.AddComponent<MapPickup>();
                var pSo = new SerializedObject(pickup);
                pSo.FindProperty("pickupId").stringValue = $"{mapName}_{id}";   // 맵마다 고유 저장 키
                pSo.FindProperty("item").objectReferenceValue = item;
                pSo.ApplyModifiedPropertiesWithoutUndo();

                placed.Add(display.Length > 0 ? display : id);
            }

            Debug.Log($"[MapSceneFeatureMigrator] Map_{mapName} 줍기 아이템 {placed.Count}개: {string.Join(", ", placed)}");
            if (noIcon.Count > 0)
                Debug.LogWarning($"[MapSceneFeatureMigrator] Map_{mapName}: 아이콘 없는 아이템 {noIcon.Count}개는 건너뜀 — {string.Join(", ", noIcon)}");
        }

        /// <summary>카메라 세로를 맵 높이에 정확히 맞추고 맵 중앙에 놓는다.
        /// 가로로 몇 화면이 되는지는 ScreenNavigator가 실제 종횡비로 런타임에 계산한다.</summary>
        static void SetupCamera(List<GameObject> moved, Bounds mapBounds)
        {
            GameObject camGo = moved.FirstOrDefault(g => g.name == "Main Camera");
            if (camGo == null || !camGo.TryGetComponent<Camera>(out Camera cam)) return;

            cam.orthographic     = true;
            cam.orthographicSize = mapBounds.size.y * 0.5f;
            camGo.transform.position =
                new Vector3(mapBounds.center.x, mapBounds.center.y, camGo.transform.position.z);
        }

        /// <summary>구 씬의 고정 구역 앵커를 끊고 맵 루트를 물려, 구역 계산을 런타임으로 넘긴다.</summary>
        static void SetupNavigator(List<GameObject> moved, GameObject background, int stageCount)
        {
            foreach (ScreenNavigator nav in EachComponent<ScreenNavigator>(moved))
            {
                var so = new SerializedObject(nav);
                so.FindProperty("sections").ClearArray();     // 비면 ScreenNavigator가 런타임 계산
                if (background != null)
                    so.FindProperty("mapRoot").objectReferenceValue = background.transform;
                // 스테이지 버튼 하나당 구역 하나 — 화면 폭 기준 개수보다 많으면 이 값이 쓰인다.
                so.FindProperty("minSections").intValue = stageCount;
                // 진행 1번 스테이지가 맵 오른쪽 끝에 있으므로 시작 구역도 오른쪽 끝(마지막 인덱스).
                so.FindProperty("startIndex").intValue = Mathf.Max(0, stageCount - 1);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>MapManager에 노드·챕터 목록을 물려준다.
        /// 비어 있으면 해금 판정과 챕터 복원이 동작하지 않는다(모든 스테이지가 잠긴 것처럼 보인다).</summary>
        static void WireManagers(List<GameObject> moved, string mapName)
        {
            foreach (MapManager mm in EachComponent<MapManager>(moved))
            {
                var so = new SerializedObject(mm);
                FillList(so.FindProperty("allNodes"), CollectAssets<NodeSO>("t:NodeSO"));
                FillList(so.FindProperty("allChapters"), CollectAssets<ChapterSO>("t:ChapterSO"));
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[MapSceneFeatureMigrator] Map_{mapName}: MapManager 노드 {so.FindProperty("allNodes").arraySize}개, 챕터 {so.FindProperty("allChapters").arraySize}개 연결");
            }
        }

        static List<T> CollectAssets<T>(string filter) where T : Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) list.Add(a);
            }
            list.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
            return list;
        }

        static void FillList<T>(SerializedProperty prop, List<T> values) where T : Object
        {
            if (prop == null) return;
            prop.ClearArray();
            for (int i = 0; i < values.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        /// <summary>스테이지 버튼을 구역 앵커 위에 하나씩 올린다.
        /// 앵커는 ScreenNavigator가 런타임에 쓰는 것과 같은 공식으로 계산한다 —
        /// 카메라는 맵 끝까지 못 가고 화면 반폭만큼 안쪽까지가 한계이므로,
        /// 맵 전체 폭에 균등 배치하면 양끝 버튼이 같은 구역에 겹치고 중간 구역이 빈다.
        /// 진행 방향은 오른쪽 → 왼쪽이라 1번이 가장 오른쪽 구역에 온다.
        /// 반환값은 배치된 x 좌표(왼→오 정렬) — 줍기 아이템을 그 사이에 끼울 때 쓴다.</summary>
        static List<float> LayOutStageButtons(List<StageNodeButton> ordered, Bounds mapBounds, string mapName)
        {
            var xs = new List<float>();
            int count = ordered.Count;
            if (count == 0) return xs;

            // 빌드 타깃 해상도(세로 모바일)의 종횡비로 화면 반폭을 구한다.
            // SetupCamera가 orthographicSize를 맵 높이의 절반으로 맞춰두므로 그대로 쓴다.
            float aspect = PlayerSettings.defaultScreenHeight > 0
                ? PlayerSettings.defaultScreenWidth / (float)PlayerSettings.defaultScreenHeight
                : 0.5625f;
            float halfView = mapBounds.size.y * 0.5f * aspect;

            // ScreenNavigator.BuildAnchors 와 동일: 카메라가 갈 수 있는 x 범위를 균등 분할.
            float minX = mapBounds.min.x + halfView;
            float maxX = mapBounds.max.x - halfView;
            if (maxX <= minX) minX = maxX = mapBounds.center.x;   // 맵이 화면보다 좁으면 구역 1개

            float halfButton = 0f, halfHeight = 0f;
            foreach (StageNodeButton b in ordered)
                if (b.transform is RectTransform rt)
                {
                    halfButton = Mathf.Max(halfButton, rt.rect.width  * 0.5f * Mathf.Abs(rt.lossyScale.x));
                    halfHeight = Mathf.Max(halfHeight, rt.rect.height * 0.5f * Mathf.Abs(rt.lossyScale.y));
                }

            float bottom = mapBounds.min.y + halfHeight;
            float top    = mapBounds.max.y - halfHeight;

            for (int i = 0; i < count; i++)
            {
                // i번째 버튼(1번=오른쪽) → 오른쪽에서 i번째 구역
                float t = count == 1 ? 0f : (count - 1 - i) / (float)(count - 1);
                Transform tr = ordered[i].transform;
                Vector3 pos = tr.position;
                pos.x = Mathf.Clamp(Mathf.Lerp(minX, maxX, t),
                                    mapBounds.min.x + halfButton, mapBounds.max.x - halfButton);
                pos.y = top >= bottom ? Mathf.Clamp(pos.y, bottom, top) : mapBounds.center.y;
                SetWorld(tr, pos);
            }

            xs.AddRange(ordered.Select(b => b.transform.position.x));
            xs.Sort();

            VerifyOnePerSection(ordered, minX, maxX, count, mapName);
            Debug.Log($"[MapSceneFeatureMigrator] Map_{mapName} 스테이지 {count}개: " +
                      $"x {xs[0]:F2} ~ {xs[^1]:F2} / 구역 {minX:F2} ~ {maxX:F2} / 맵 {mapBounds.min.x:F2} ~ {mapBounds.max.x:F2}");
            return xs;
        }

        /// <summary>스테이지 버튼을 챕터별 색으로 칠하고 번호를 다시 쓴다.
        /// 템플릿을 복사해 온 탓에 Porch·Yard가 챕터 2 번호(2-1…)를 그대로 달고 있다.</summary>
        static void StyleStageButtons(List<StageNodeButton> ordered, int chapter, string mapName)
        {
            if (ordered.Count == 0) return;

            Sprite tile = ChapterTile.TryGetValue(chapter, out string file)
                ? AssetDatabase.LoadAssetAtPath<Sprite>($"{TileRoot}/{file}")
                : null;
            if (tile == null)
                Debug.LogWarning($"[MapSceneFeatureMigrator] Map_{mapName}: 챕터 {chapter} 타일 스프라이트를 못 찾음 — 색은 그대로 둔다");

            for (int i = 0; i < ordered.Count; i++)
            {
                GameObject go = ordered[i].gameObject;

                if (tile != null && go.TryGetComponent<Image>(out Image image))
                {
                    image.sprite = tile;
                    EditorUtility.SetDirty(image);
                }

                if (tile != null && go.TryGetComponent<Button>(out Button button))
                {
                    // 타일 스프라이트가 색을 갖고 있으므로 틴트는 중립으로 — 곱해져서 탁해지는 걸 막는다.
                    var colors = button.colors;
                    colors.normalColor      = Color.white;
                    colors.highlightedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
                    colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
                    colors.selectedColor    = Color.white;
                    colors.disabledColor    = new Color(0.55f, 0.55f, 0.55f, 1f);
                    button.colors = colors;

                    var sprites = button.spriteState;
                    sprites.highlightedSprite = tile;
                    sprites.pressedSprite     = tile;
                    sprites.selectedSprite    = tile;
                    button.spriteState = sprites;
                    EditorUtility.SetDirty(button);
                }

                foreach (TMP_Text label in go.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.text = $"{chapter}-{i + 1}";
                    EditorUtility.SetDirty(label);
                }
            }

            Debug.Log($"[MapSceneFeatureMigrator] Map_{mapName}: 버튼 색 {file ?? "(없음)"}, 번호 {chapter}-1 ~ {chapter}-{ordered.Count}");
        }

        /// <summary>버튼이 구역마다 정확히 하나씩 들어갔는지 확인한다. 어긋나면 조용히 넘어가지 않고 알린다.</summary>
        static void VerifyOnePerSection(List<StageNodeButton> ordered, float minX, float maxX, int count, string mapName)
        {
            var used = new HashSet<int>();
            foreach (StageNodeButton b in ordered)
            {
                int best = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    float anchor = count == 1 ? minX : Mathf.Lerp(minX, maxX, i / (float)(count - 1));
                    float d = Mathf.Abs(anchor - b.transform.position.x);
                    if (d < bestDist) { bestDist = d; best = i; }
                }
                if (!used.Add(best))
                    Debug.LogWarning($"[MapSceneFeatureMigrator] Map_{mapName}: {b.name}이(가) 이미 찬 구역 {best}에 겹친다");
            }
            if (used.Count != count)
                Debug.LogWarning($"[MapSceneFeatureMigrator] Map_{mapName}: 구역 {count}개 중 {used.Count}개만 채워짐");
        }

        /// <summary>스테이지 버튼에 이 맵의 챕터 노드를 순서대로 물린다.
        /// 진행 방향은 맵 오른쪽 → 왼쪽이므로 가장 오른쪽 버튼이 1번(Node_{챕터}_1)이다.
        /// 반환값은 진행 순서대로 정렬된 버튼 목록.</summary>
        static List<StageNodeButton> AssignStageNodes(List<GameObject> moved, int chapter)
        {
            List<StageNodeButton> ordered = EachComponent<StageNodeButton>(moved)
                .OrderByDescending(b => b.transform.position.x)   // 맵 오른쪽 끝이 1번
                .ToList();

            if (ordered.Count == 0) return ordered;

            // 챕터가 없는 맵에는 스테이지를 두지 않는다 — 노드가 없어 눌러도 아무 일이 없다.
            if (chapter <= 0)
            {
                foreach (StageNodeButton b in ordered) Object.DestroyImmediate(b.gameObject);
                ordered.Clear();
                return ordered;
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                string path = $"{NodeAssetRoot}/Node_{chapter}_{i + 1}.asset";
                var node = AssetDatabase.LoadAssetAtPath<NodeSO>(path);
                if (node == null)
                {
                    Debug.LogWarning($"[MapSceneFeatureMigrator] 노드 에셋 없음: {path} — 버튼 {i + 1}번은 비워 둔다");
                    continue;
                }
                var so = new SerializedObject(ordered[i]);
                so.FindProperty("node").objectReferenceValue = node;
                so.ApplyModifiedPropertiesWithoutUndo();
                ordered[i].gameObject.name = $"StageButton_{chapter}_{i + 1}";
            }
            return ordered;
        }

        /// <summary>조합 자물쇠를 진행 순서 2번째 스테이지에 건다.
        /// 1번은 맵에 들어오자마자 눌러야 하므로 잠그면 진행이 막힌다.
        /// 구역 인덱스도 그 버튼의 월드 위치에서 역산하게 물려준다(기기 종횡비 대응).</summary>
        static void SetupStageGate(List<GameObject> moved, List<StageNodeButton> ordered)
        {
            foreach (MapStageGate gate in EachComponent<MapStageGate>(moved))
            {
                var so = new SerializedObject(gate);
                SerializedProperty buttonProp = so.FindProperty("stageButton");

                if (buttonProp.objectReferenceValue == null && ordered.Count >= 2)
                    buttonProp.objectReferenceValue = ordered[1].GetComponent<Button>();

                if (buttonProp.objectReferenceValue is Button button)
                    so.FindProperty("sectionAnchor").objectReferenceValue = button.transform;
                else
                    Debug.LogWarning($"[MapSceneFeatureMigrator] {gate.name}: 물릴 스테이지 버튼이 없어 조합 해금을 못 건다");

                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>UI·매니저가 아닌 루트(강아지 캐릭터 등)를 새 맵 안으로 옮긴다.
        /// 구 맵(가로 약 60유닛) 좌표를 그대로 두면 새 맵(38.4유닛) 바깥에 떨어져 화면에 안 보인다.
        /// 진행 1번 스테이지가 있는 오른쪽 끝에 놓는다 — 플레이가 거기서 시작하므로.</summary>
        static void PlaceStrayRoots(List<GameObject> moved, Bounds mapBounds, List<float> stageXs, string mapName)
        {
            float startX = stageXs != null && stageXs.Count > 0 ? stageXs[^1] : mapBounds.center.x;

            foreach (GameObject root in moved)
            {
                if (RootsKeepingPosition.Contains(root.name)) continue;
                if (root.name == PickupRootName) continue;                       // LayOutPickups가 처리
                if (root.GetComponentInChildren<ScreenNavigator>(true) != null) continue;  // 위치 의미 없음

                Vector3 p = root.transform.position;
                bool outside = p.x < mapBounds.min.x || p.x > mapBounds.max.x
                            || p.y < mapBounds.min.y || p.y > mapBounds.max.y;
                p.x = startX;
                p.y = Mathf.Clamp(p.y, mapBounds.min.y, mapBounds.max.y);
                SetWorld(root.transform, p);

                Debug.Log($"[MapSceneFeatureMigrator] Map_{mapName}: '{root.name}' 배치 " +
                          $"→ x {startX:F2}{(outside ? " (원래 맵 밖이었음)" : "")}");
            }
        }

        /// <summary>줍기 아이템이 배경에 묻히거나 서로 겹치지 않게 정리한다.
        /// 구 씬 기준 sortingOrder(10)를 그대로 두면 새 맵의 11번 이상 레이어에 가려지고,
        /// 맵 폭이 줄면서 좌표가 압축돼 아이템끼리 붙어버린다.</summary>
        static void LayOutPickups(List<GameObject> moved, GameObject background, Bounds mapBounds,
                                  List<float> stageXs)
        {
            GameObject root = moved.FirstOrDefault(g => g.name == PickupRootName);
            if (root == null || background == null) return;

            // 1) 배경보다 확실히 위로 올린다 — 렌더 순서이자 클릭 우선순위.
            int topOfBackground = 0;
            foreach (SpriteRenderer r in background.GetComponentsInChildren<SpriteRenderer>(true))
                topOfBackground = Mathf.Max(topOfBackground, r.sortingOrder);

            List<SpriteRenderer> items = root.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r => r.sprite != null)
                .OrderBy(r => r.transform.position.x)
                .ToList();
            if (items.Count == 0) return;

            foreach (SpriteRenderer r in items)
                r.sortingOrder = topOfBackground + PickupSortingOffset;

            // 2) 스테이지 버튼 사이 빈칸에 하나씩 끼운다 — 버튼과 겹치지 않는 가장 확실한 자리.
            //    버튼 n개면 사이 간격은 n-1개. 아이템이 그보다 많으면 아래 밀어내기 단계가 처리한다.
            // 스테이지 버튼 사이 빈칸 + 바닥 높이로 내린다(새로 만든 아이템은 y=0이라 공중에 뜬다).
            float floorY = mapBounds.min.y + mapBounds.size.y * 0.2f;
            for (int i = 0; i < items.Count; i++)
            {
                float x = stageXs != null && stageXs.Count >= 2 && i < stageXs.Count - 1
                    ? (stageXs[i] + stageXs[i + 1]) * 0.5f
                    : items[i].transform.position.x;
                SetWorld(items[i].transform, new Vector3(x, floorY, items[i].transform.position.z));
            }

            // 3) 왼쪽부터 훑으며 앞 아이템과 겹치면 오른쪽으로 민다.
            for (int i = 1; i < items.Count; i++)
            {
                float needLeft = items[i - 1].bounds.max.x + PickupGap + items[i].bounds.extents.x;
                if (items[i].transform.position.x < needLeft)
                    SetX(items[i].transform, needLeft);
            }

            // 4) 오른쪽 끝을 넘었으면 반대로 훑으며 되민다 — 맵 밖으로 나가지 않게.
            float rightLimit = mapBounds.max.x - PickupEdgeMargin;
            if (items[^1].bounds.max.x > rightLimit)
            {
                SetX(items[^1].transform, rightLimit - items[^1].bounds.extents.x);
                for (int i = items.Count - 2; i >= 0; i--)
                {
                    float needRight = items[i + 1].bounds.min.x - PickupGap - items[i].bounds.extents.x;
                    if (items[i].transform.position.x > needRight)
                        SetX(items[i].transform, needRight);
                }
            }

            // 5) 왼쪽 끝 클램프 + 세로도 맵 안으로. Y를 안 잡아서 아이템이 바닥 아래로 빠져 있었다.
            float leftLimit = mapBounds.min.x + PickupEdgeMargin;
            foreach (SpriteRenderer r in items)
            {
                if (r.bounds.min.x < leftLimit)
                    SetX(r.transform, leftLimit + r.bounds.extents.x);

                float halfH = r.bounds.extents.y;
                float lo = mapBounds.min.y + halfH + PickupEdgeMargin;
                float hi = mapBounds.max.y - halfH - PickupEdgeMargin;
                Vector3 p = r.transform.position;
                p.y = hi >= lo ? Mathf.Clamp(p.y, lo, hi) : mapBounds.center.y;
                SetWorld(r.transform, p);
            }

            for (int i = 1; i < items.Count; i++)
                if (items[i].bounds.min.x < items[i - 1].bounds.max.x)
                {
                    Debug.LogWarning($"[MapSceneFeatureMigrator] 줍기 아이템이 맵 폭에 다 안 들어간다: " +
                                     $"{items[i - 1].name} / {items[i].name} 겹침. 아이템을 줄이거나 수동 배치 필요.");
                    break;
                }
        }

        /// <summary>월드 X로 옮긴다. RectTransform은 m_AnchoredPosition이 직렬화 필드라
        /// transform.position만 건드리면 메모리 값만 바뀌고 저장 시 옛 좌표가 그대로 기록된다.</summary>
        static void SetX(Transform t, float x) => SetWorld(t, new Vector3(x, t.position.y, t.position.z));

        static void SetWorld(Transform t, Vector3 world)
        {
            t.position = world;

            if (t is RectTransform rt)
            {
                Vector2 anchored = rt.anchoredPosition;   // localPosition 기준으로 다시 계산된 값
                rt.anchoredPosition = anchored;           // m_AnchoredPosition 에 확정 기록
            }
            EditorUtility.SetDirty(t);
        }

        /// <summary>배경 스프라이트마다 콜라이더 + 하이라이트를 붙여 클릭/호버 대상으로 만든다.</summary>
        static int SetupInteractables(GameObject background, Material outline)
        {
            if (background == null) return 0;

            int count = 0;
            foreach (SpriteRenderer sr in background.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null) continue;

                // 벽·바닥처럼 화면을 덮는 레이어는 제외 — 앞의 오브젝트 클릭을 가로챈다.
                if (IsBackdropLayer(sr.gameObject.name)) continue;

                if (!sr.TryGetComponent<Collider2D>(out _))
                {
                    var col = sr.gameObject.AddComponent<PolygonCollider2D>();
                    col.isTrigger = true;
                }

                if (!sr.TryGetComponent<SpriteHighlight>(out _))
                {
                    var hl = sr.gameObject.AddComponent<SpriteHighlight>();
                    var so = new SerializedObject(hl);
                    so.FindProperty("outlineMaterial").objectReferenceValue = outline;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                count++;
            }
            return count;
        }

        static bool IsBackdropLayer(string objectName) =>
            objectName.Contains("_Wall") || objectName.Contains("_Floor");

        static IEnumerable<T> EachComponent<T>(List<GameObject> roots) where T : Component
        {
            foreach (GameObject root in roots)
                foreach (T c in root.GetComponentsInChildren<T>(true))
                    yield return c;
        }

        static bool TryGetSpriteBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null) return false;

            bool any = false;
            foreach (SpriteRenderer r in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r.sprite == null) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }
    }
}
