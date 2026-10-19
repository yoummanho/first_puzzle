using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace FirstPuzzle.Editor
{
    public static class PhaseOneSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/PhaseOneTest.unity";
        private const string DataFolder = "Assets/Resource/PuzzlePhaseOne";
        private const string PrefabPath = DataFolder + "/PuzzleTile.prefab";

        [MenuItem("Puzzle/Create Phase 1 Test Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("PhaseOneTest already exists; open the existing scene.");
            if (TMP_Settings.defaultFontAsset == null)
                throw new InvalidOperationException("Import TMP Essentials before generating the scene.");

            string[] images = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resource.art/Puzzle_images" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (images.Length == 0) throw new InvalidOperationException("No puzzle illustrations found.");
            EnsureFolder(DataFolder);
            EnsureFolder("Assets/Scenes");
            StageData[] stages = CreateStages(images);
            Scene original = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                GameObject canvasObject = new GameObject("PhaseOneCanvas", typeof(RectTransform), typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
                var background = Rect("Background", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var backgroundImage = background.gameObject.AddComponent<UnityEngine.UI.Image>();
                backgroundImage.color = new Color(0.035f, 0.055f, 0.09f);
                backgroundImage.raycastTarget = false;

                var header = Rect("Status", canvas.transform, new Vector2(0, 1), Vector2.one,
                    new Vector2(36, -190), new Vector2(-36, -28));
                TMP_Text status = Text(header, "PHASE 1", 32);
                var boardArea = Rect("BoardArea", canvas.transform, Vector2.zero, Vector2.one,
                    new Vector2(36, 480), new Vector2(-36, -210));
                BoardManager board = boardArea.gameObject.AddComponent<BoardManager>();
                SetObject(board, "stage", stages[0]);
                SetObject(board, "boardArea", boardArea);
                SetObject(board, "tilePrefab", CreateTilePrefab());

                var controls = Rect("Controls", canvas.transform, Vector2.zero, new Vector2(1, 0),
                    new Vector2(36, 100), new Vector2(-36, 450));
                var grid = controls.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                grid.cellSize = new Vector2(486, 76);
                grid.spacing = new Vector2(20, 12);
                grid.childAlignment = TextAnchor.UpperCenter;
                PhaseOneDemo demo = canvasObject.AddComponent<PhaseOneDemo>();
                SetObject(demo, "board", board);
                SetObject(demo, "status", status);
                var serialized = new SerializedObject(demo);
                var stageArray = serialized.FindProperty("stages");
                stageArray.arraySize = stages.Length;
                for (int i = 0; i < stages.Length; i++) stageArray.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SetObject(demo, "undoButton", Button(controls, "Undo", demo.Undo));
                SetObject(demo, "rewardButton", Button(controls, "TEST reward +3", demo.Reward));
                SetObject(demo, "multiLabel", Button(controls, "Line slide: ON", demo.ToggleMultiPush).GetComponentInChildren<TMP_Text>());
                SetObject(demo, "animationLabel", Button(controls, "Animation: ON", demo.ToggleAnimation).GetComponentInChildren<TMP_Text>());
                SetObject(demo, "numbersLabel", Button(controls, "Numbers: ON", demo.ToggleNumbers).GetComponentInChildren<TMP_Text>());
                Button(controls, "Restart", demo.Restart);
                Button(controls, "Next test stage", demo.NextStage);
                var footer = Rect("TestNotice", canvas.transform, Vector2.zero, new Vector2(1, 0),
                    new Vector2(36, 20), new Vector2(-36, 90));
                Text(footer, "PHASE 1 TEST  |  Reward button simulates an earned ad reward.\nTap a tile in the blank's row or column.", 23);

                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = backgroundImage.color;
                cameraObject.transform.position = new Vector3(0, 0, -10);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid()) SceneManager.SetActiveScene(original);
            }
            Debug.Log("Created Phase 1 test scene: " + ScenePath);
        }

        private static StageData[] CreateStages(string[] images)
        {
            Vector2Int[] sizes = { new Vector2Int(3, 3), new Vector2Int(3, 4), new Vector2Int(4, 4),
                new Vector2Int(4, 5), new Vector2Int(5, 6), new Vector2Int(6, 8), new Vector2Int(3, 3) };
            var stages = new StageData[sizes.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                string id = i == sizes.Length - 1 ? "test-one-move" : $"test-{sizes[i].x}x{sizes[i].y}";
                string path = DataFolder + "/" + id + ".asset";
                StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage == null)
                {
                    stage = ScriptableObject.CreateInstance<StageData>();
                    var data = new SerializedObject(stage);
                    data.FindProperty("stageId").stringValue = id;
                    data.FindProperty("illustration").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(images[i % images.Length]);
                    data.FindProperty("columns").intValue = sizes[i].x;
                    data.FindProperty("rows").intValue = sizes[i].y;
                    data.FindProperty("shuffleSeed").intValue = 20261020 + i;
                    data.FindProperty("shuffleCount").intValue = i == sizes.Length - 1 ? 1 : sizes[i].x * sizes[i].y * 20;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(stage, path);
                }
                stages[i] = stage;
            }
            return stages;
        }

        private static PuzzleTile CreateTilePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing.GetComponent<PuzzleTile>();
            GameObject tile = new GameObject("PuzzleTile", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(PuzzleTile));
            try
            {
                ((RectTransform)tile.transform).sizeDelta = new Vector2(100, 100);
                var labelRect = Rect("Number", tile.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                TMP_Text label = Text(labelRect, "1", 26);
                label.color = new Color(1, 1, 1, 0.75f);
                // A dark outline keeps the translucent number readable on bright illustrations.
                label.outlineWidth = 0.18f;
                label.outlineColor = new Color32(0, 0, 0, 180);
                SetObject(tile.GetComponent<PuzzleTile>(), "numberLabel", label);
                return PrefabUtility.SaveAsPrefabAsset(tile, PrefabPath).GetComponent<PuzzleTile>();
            }
            finally { UnityEngine.Object.DestroyImmediate(tile); }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static TMP_Text Text(RectTransform rect, string text, float size)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.92f, 0.95f, 1f);
            label.raycastTarget = false;
            return label;
        }

        private static UnityEngine.UI.Button Button(Transform parent, string text, UnityAction action)
        {
            var rect = Rect(text, parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(486, 76));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.12f, 0.22f, 0.34f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var labelRect = Rect("Label", rect, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4));
            Text(labelRect, text, 28);
            UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
    }
}
