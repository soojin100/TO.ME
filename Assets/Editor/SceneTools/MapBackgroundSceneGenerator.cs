using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TOME.EditorTools
{
    /// <summary>
    /// Assets/Sprites/Map/Background의 네 분리 이미지 세트를 0번 완성본과 같은 위치로 배치한다.
    /// 0번 이미지는 참고용이며 생성 씬에는 포함하지 않는다.
    /// </summary>
    public static class MapBackgroundSceneGenerator
    {
        const string SpriteRoot = "Assets/Sprites/Map/Background";
        const string SceneRoot = "Assets/Scenes/Maps";
        const float PixelsPerUnit = 100f;
        const int CanvasWidth = 3840;
        const int CanvasHeight = 1700;

        readonly struct Layer
        {
            public readonly int Number;
            public readonly string File;
            public readonly int X;
            public readonly int Y;

            public Layer(int number, string file, int x, int y)
            {
                Number = number;
                File = file;
                X = x;
                Y = y;
            }
        }

        sealed class MapDefinition
        {
            public readonly string Name;
            public readonly string Folder;
            public readonly Layer[] Layers;

            public MapDefinition(string name, string folder, Layer[] layers)
            {
                Name = name;
                Folder = folder;
                Layers = layers;
            }
        }

        static readonly MapDefinition[] Maps =
        {
            new("Kitchen", "BG_Kitchen_Object", new[]
            {
                L(1,"1.Kitchen_Floor.png",0,801), L(2,"2.Kitchen_Wall.png",0,0),
                L(3,"3.Kitchen_Wall_Side.png",0,0), L(4,"4.Kitchen_Porch.png",0,38),
                L(5,"5.Kitchen_Door.png",3642,243), L(6,"6.Kitchen_Refrigerator.png",194,175),
                L(7,"7.Kitchen_Elements.png",544,0), L(8,"8.Kitchen_Condiment.png",1520,249),
                L(9,"9.Kitchen_Frame.png",2281,21), L(10,"10.Kitchen_Frame.png",2481,100),
                L(11,"11.Kitchen_Window.png",2752,0), L(12,"12.Kitchen_Table.png",2464,387),
                L(13,"13.Kitchen_Mat.png",0,931), L(14,"14.Kitchen_Scaffolding.png",639,942),
                L(15,"15.Kitchen_Bin.png",2223,637), L(16,"16.Kitchen_Chair.png",2968,536),
                L(17,"17.Kitchen_DogFood.png",3336,517), L(18,"18.Kitchen_FoodBowl.png",3346,1311),
                L(19,"19.Kitchen_Cushion.png",2903,1453), L(20,"20.Kitchen_PigFun.png",669,1469),
                L(21,"21.Kitchen_FightingFun.png",2064,1177), L(22,"22.Kitchen_Wall_Up.png",0,0),
            }),
            new("Porch", "BG_Porch_Object", new[]
            {
                L(1,"1.Porch_Floor.png",0,804), L(2,"2.Porch_Wall.png",0,0),
                L(3,"3.Porch_Wall_Side.png",0,0), L(4,"4.Porch_Door.png",542,122),
                L(5,"5.Porch_UmbrellaStand.png",766,422), L(6,"6.Porch_Slippers.png",2517,1007),
                L(7,"7.Porch_Sneakers.png",3258,1186), L(8,"8.Porch_Bottom.png",3399,847),
                L(9,"9.Porch_DoorInRoom.png",3649,0), L(10,"10.Porch_DeliveryBox.png",3429,704),
                L(11,"11.Porch_Mirror.png",3021,76), L(12,"12.Porch_Pot.png",3082,300),
                L(13,"13.Porch_TeddyBear.png",796,1022), L(14,"14.Porch_PuppyPad.png",1484,1075),
                L(15,"15.Porch_DogShoes_1.png",3300,978), L(16,"16.Porch_DogShoes_2.png",2281,1503),
                L(17,"17.Porch_DogShoes_3.png",2824,1462), L(18,"18.Porch_DogShoes_4.png",3683,985),
                L(19,"19.Porch_Doll_1.png",3340,445), L(20,"20.Porch_Doll_2.png",3200,432),
                L(21,"21.Porch_Window.png",1,1), L(22,"22.Porch_Trash.png",383,556),
                L(23,"23.Porch_Recyclable_1.png",0,219), L(24,"24.Porch_Recyclable_2.png",0,585),
                L(25,"25.Porch_Recyclable_3.png",0,680), L(26,"26.Porch_Recyclable.png",0,810),
                L(27,"27.Porch_DeliveryBox.png",0,1147), L(28,"28.Porch_Cabinet.png",2333,0),
                L(29,"29.Porch_Cabinet.png",1973,0), L(30,"30.Porch_Cabinet.png",1183,0),
                L(31,"31.Porch_DogLeash.png",2389,261), L(32,"32.Porch_Heart.png",1287,423),
                L(33,"33.Porch_Photo_1.png",1341,272), L(34,"34.Porch_Photo_2.png",1700,184),
                L(35,"35.Porch_Photo_3.png",2092,560), L(36,"36.Porch_Photo_4.png",2921,537),
                L(37,"37.Porch_Wall_Up.png",0,0),
            }),
            new("Room", "BG_Room_Object", new[]
            {
                L(1,"1.Room_Floor.png",0,800), L(2,"2.Room_Wall.png",0,0),
                L(3,"3.Room_Wall_Side.png",0,0), L(4,"4.Room_Wardrobe.png",234,87),
                L(5,"5.Room_Door.png",0,243), L(6,"6.Room_Switch.png",195,470),
                L(7,"7.Room_Window_Left.png",3567,0), L(8,"8.Room_Window_Center.png",761,0),
                L(9,"9.Room_Mirror.png",748,317), L(10,"10.Room_Carpet.png",1173,1073),
                L(11,"11.Room_Towel.png",3329,1401), L(12,"12.Room_DogHouse.png",2208,568),
                L(13,"13.Room_Carrot.png",2317,1030), L(14,"14.Room_Calendar.png",2735,148),
                L(15,"15.Room_MainBoard.png",2331,198), L(16,"16.Room_Clock.png",2119,108),
                L(17,"17.Room_Socks_R.png",860,1453), L(18,"18.Room_Socks_L.png",585,955),
                L(19,"19.Room_Photo.png",2992,75), L(20,"20.Room_Bed.png",2773,433),
                L(21,"21.Room_AirConditioning.png",2558,0), L(22,"22.Room_Outlet.png",2698,615),
                L(23,"23.Room_Bin.png",2526,633), L(24,"24.Room_Table.png",1015,351),
                L(25,"25.Room_Chair.png",1072,345), L(26,"26.Room_Drawer.png",1729,396),
                L(27,"27.Room_Shelf.png",1491,12), L(28,"28.Room_Wall_Side.png",0,0),
            }),
            new("Yard", "BG_Yard_Object", new[]
            {
                L(1,"1.Yard_Floor.png",0,581), L(2,"2.Yard_Wall.png",0,0),
                L(3,"3.Yard_Home.png",2884,0), L(4,"4.Yard_Bench.png",1250,713),
                L(5,"5.Yard_WateringCan.png",2430,1238), L(6,"6.Yard_Hose.png",2008,859),
            }),
        };

        [MenuItem("Tools/Map Background/Generate All Map Scenes")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(SceneRoot);
            ImportAllSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (MapDefinition map in Maps)
                GenerateScene(map);

            AddGeneratedScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[MapBackgroundSceneGenerator] Kitchen, Porch, Room, Yard 씬 생성 완료");
        }

        static void ImportAllSprites()
        {
            foreach (MapDefinition map in Maps)
            {
                string folder = $"{SpriteRoot}/{map.Folder}";
                foreach (string path in Directory.GetFiles(folder, "*.png").Select(NormalizePath))
                {
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                        continue;

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = PixelsPerUnit;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 4096;
                    importer.SaveAndReimport();
                }
            }
        }

        static void GenerateScene(MapDefinition map)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = $"Map_{map.Name}";

            var root = new GameObject($"{map.Name}_Background");
            foreach (Layer layer in map.Layers.OrderBy(layer => layer.Number))
            {
                string path = $"{SpriteRoot}/{map.Folder}/{layer.File}";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    throw new InvalidOperationException($"Sprite를 불러올 수 없습니다: {path}");

                var go = new GameObject(Path.GetFileNameWithoutExtension(layer.File));
                go.transform.SetParent(root.transform, false);
                go.transform.position = PixelRectCenterToWorld(layer.X, layer.Y, sprite.rect.width, sprite.rect.height);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = layer.Number;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CanvasHeight / (PixelsPerUnit * 2f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            string scenePath = $"{SceneRoot}/Map_{map.Name}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        static Vector3 PixelRectCenterToWorld(int x, int y, float width, float height)
        {
            float worldX = (x + width * 0.5f - CanvasWidth * 0.5f) / PixelsPerUnit;
            float worldY = (CanvasHeight * 0.5f - y - height * 0.5f) / PixelsPerUnit;
            return new Vector3(worldX, worldY, 0f);
        }

        static void AddGeneratedScenesToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (MapDefinition map in Maps)
            {
                string path = $"{SceneRoot}/Map_{map.Name}.unity";
                if (scenes.All(scene => scene.path != path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static Layer L(int number, string file, int x, int y) => new(number, file, x, y);
        static string NormalizePath(string path) => path.Replace('\\', '/');
    }
}
