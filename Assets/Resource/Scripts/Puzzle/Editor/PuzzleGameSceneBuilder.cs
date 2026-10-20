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
    public static class PuzzleGameSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/PuzzleGame.unity";
        private static readonly Color Ink = new Color(0.035f, 0.065f, 0.10f);

        [MenuItem("Puzzle/Create Playable Game Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("PuzzleGame already exists.");
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("PuzzleGame", typeof(RectTransform), typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                Panel("Background", root.transform, 0, 0, 1, 1, Ink, false);
                var content = Rect("Content", root.transform, .04f, .03f, .96f, .97f);
                var game = root.AddComponent<PuzzleGame>();
                game.stages = MakeStages();
                game.heading = Label("Title", content, .05f, .87f, .95f, .99f, "PICTURE PUZZLE", 42);
                game.stats = Label("Statistics", content, 0, .81f, 1, .87f, "00:00 / 0 MOVES", 28);
                var frame = Panel("BoardFrame", content, 0, .24f, 1, .80f, new Color(.08f, .13f, .18f), false);
                var area = Rect("Board", frame, .025f, .025f, .975f, .975f);
                game.board = area.gameObject.AddComponent<BoardManager>();
                var board = new SerializedObject(game.board);
                board.FindProperty("stage").objectReferenceValue = game.stages[0];
                board.FindProperty("boardArea").objectReferenceValue = area;
                board.FindProperty("tilePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resource/PuzzlePhaseOne/PuzzleTile.prefab").GetComponent<PuzzleTile>();
                board.ApplyModifiedPropertiesWithoutUndo();
                var previewArea = Rect("PreviewArea", frame, .025f, .025f, .975f, .975f);
                var previewRect = Rect("IllustrationPreview", previewArea, 0, 0, 1, 1);
                game.preview = previewRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                game.preview.raycastTarget = true;
                var fitter = previewRect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 1;
                previewRect.gameObject.SetActive(false);
                Label("Instructions", content, 0, .19f, 1, .24f, "Slide a tile into the empty space.", 26);
                game.undoButton = Button(content, "Undo", 0, .125f, .31f, .18f, game.Undo);
                var hold = Button(content, "Hold to preview", .345f, .125f, .655f, .18f, null);
                hold.gameObject.AddComponent<HoldPreview>().game = game;
                game.pauseButton = Button(content, "Pause", .69f, .125f, 1, .18f, game.Pause);
                game.numbersLabel = Button(content, "Numbers: ON", 0, .055f, .48f, .11f, game.Numbers).GetComponentInChildren<TMP_Text>();
                Button(content, "Restart", .52f, .055f, 1, .11f, game.Restart);
                Label("Footer", content, 0, 0, 1, .04f, "ONE TILE AT A TIME. A PICTURE COMES TOGETHER.", 18);
                var curtain = Panel("PauseAndResult", root.transform, 0, 0, 1, 1, Ink, true);
                game.curtain = curtain.gameObject;
                game.message = Label("Message", curtain, .08f, .55f, .92f, .80f, "PAUSED", 48);
                var pause = Rect("PauseControls", curtain, .12f, .15f, .88f, .54f);
                game.pauseControls = pause.gameObject;
                game.resumeButton = Button(pause, "Continue", 0, .76f, 1, .96f, game.Resume);
                game.lineLabel = Button(pause, "Line slide: ON", 0, .51f, 1, .71f, game.Lines).GetComponentInChildren<TMP_Text>();
                game.animationLabel = Button(pause, "Animation: ON", 0, .26f, 1, .46f, game.Animation).GetComponentInChildren<TMP_Text>();
                Button(pause, "Restart puzzle", 0, .01f, 1, .21f, game.Restart);
                var results = Rect("ResultControls", curtain, .12f, .23f, .88f, .51f);
                game.resultControls = results.gameObject;
                game.nextButton = Button(results, "Next illustration", 0, .55f, 1, .9f, game.Next);
                Button(results, "Play this again", 0, .1f, 1, .45f, game.Restart);
                curtain.gameObject.SetActive(false);
                FitPortraitLayout(game);
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Ink;
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid()) SceneManager.SetActiveScene(original);
            }
            Debug.Log("Playable scene created: " + ScenePath);
        }

        public static void FitPortraitLayout(PuzzleGame game)
        {
            if (game.transform.Find("PortraitFrame") != null) return;
            var frame = Rect("PortraitFrame", game.transform, 0, 0, 1, 1);
            var fit = frame.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 9f / 16f;
            game.transform.Find("Content").SetParent(frame, false);
            game.curtain.transform.SetParent(frame, false);
        }

        private static StageData[] MakeStages()
        {
            const string folder = "Assets/Resource/PuzzleGame";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resource", "PuzzleGame");
            string[] images = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resource.art/Puzzle_images" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            if (images.Length == 0) throw new InvalidOperationException("Puzzle illustrations are missing.");
            Vector2Int[] sizes = { new Vector2Int(3, 3), new Vector2Int(3, 3), new Vector2Int(3, 4),
                new Vector2Int(4, 4), new Vector2Int(4, 5), new Vector2Int(5, 6), new Vector2Int(6, 8) };
            var stages = new StageData[sizes.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                string path = $"{folder}/Stage{i + 1:00}.asset";
                var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage == null)
                {
                    stage = ScriptableObject.CreateInstance<StageData>();
                    var data = new SerializedObject(stage);
                    data.FindProperty("stageId").stringValue = $"picture-{i + 1:00}";
                    data.FindProperty("illustration").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(images[i % images.Length]);
                    data.FindProperty("columns").intValue = sizes[i].x;
                    data.FindProperty("rows").intValue = sizes[i].y;
                    data.FindProperty("shuffleSeed").intValue = 7351 + i;
                    data.FindProperty("shuffleCount").intValue = i == 0 ? 12 : 30 + i * 20;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(stage, path);
                }
                stages[i] = stage;
            }
            return stages;
        }
        private static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static RectTransform Panel(string name, Transform parent, float x0, float y0, float x1, float y1, Color color, bool raycast)
        {
            var rect = Rect(name, parent, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return rect;
        }
        private static TMP_Text Label(string name, Transform parent, float x0, float y0, float x1, float y1, string text, int size)
        {
            var rect = Rect(name, parent, x0, y0, x1, y1);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * .65f;
            label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.88f, .94f, .96f);
            label.raycastTarget = false;
            return label;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string text, float x0, float y0, float x1, float y1, UnityAction action)
        {
            var rect = Panel(text, parent, x0, y0, x1, y1, new Color(.10f, .28f, .32f), true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            Label("Label", rect, .04f, .04f, .96f, .96f, text, 28);
            if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }
    }
}
