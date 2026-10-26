using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstPuzzle.Editor
{
    /// <summary>Copies the sample's image presentation, never its legacy gameplay components.</summary>
    public static class SampleImageStyle
    {
        [MenuItem("Puzzle/Apply Sample Image Style")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PuzzleGameSceneBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open and save PuzzleGame before applying the sample style.");
            var game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PuzzleGame>(true)).Single();
            var sample = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
            try
            {
                // Assembly-CSharp cannot be referenced by this asmdef; inspect the serialized sample component.
                var source = sample.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                    .Single(component => component != null && component.GetType().Name == "Board");
                var sourceData = new SerializedObject(source);
                var firstSprite = sourceData.FindProperty("puzzleSprites").GetArrayElementAtIndex(0).objectReferenceValue as Sprite;
                if (firstSprite == null) throw new InvalidOperationException("Sample image is missing.");
                var sourceImage = source.GetComponent<UnityEngine.UI.Image>();
                var boardData = new SerializedObject(game.board);
                var area = (RectTransform)boardData.FindProperty("boardArea").objectReferenceValue;
                float padding = sourceData.FindProperty("borderPadding").intValue;
                var host = (RectTransform)area.parent;
                var frame = host.Find("ImageFrame") as RectTransform;
                if (frame == null)
                {
                    frame = new GameObject("ImageFrame", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
                    frame.SetParent(host, false);
                }
                frame.SetAsFirstSibling();
                var image = frame.GetComponent<UnityEngine.UI.Image>();
                image.sprite = sourceImage.sprite;
                image.type = sourceImage.type;
                image.color = sourceImage.color;
                image.raycastTarget = false;
                host.GetComponent<UnityEngine.UI.Image>().enabled = false;
                area.anchorMin = Vector2.zero;
                area.anchorMax = Vector2.one;
                area.offsetMin = Vector2.one * padding;
                area.offsetMax = -Vector2.one * padding;
                var previewArea = (RectTransform)game.preview.transform.parent;
                previewArea.anchorMin = Vector2.zero;
                previewArea.anchorMax = Vector2.one;
                previewArea.offsetMin = area.offsetMin;
                previewArea.offsetMax = area.offsetMax;
                boardData.FindProperty("imageFrame").objectReferenceValue = frame;
                boardData.FindProperty("framePadding").floatValue = padding;
                boardData.FindProperty("tileGap").floatValue = sourceData.FindProperty("spacing").vector2Value.x;
                boardData.ApplyModifiedPropertiesWithoutUndo();
                var stage = new SerializedObject(game.stages[0]);
                stage.FindProperty("illustration").objectReferenceValue = firstSprite.texture;
                stage.ApplyModifiedPropertiesWithoutUndo();
                Canvas.ForceUpdateCanvases();
                float pitch = Mathf.Min(area.rect.width / game.stages[0].Columns, area.rect.height / game.stages[0].Rows);
                frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
                frame.anchoredPosition = Vector2.zero;
                frame.sizeDelta = new Vector2(pitch * game.stages[0].Columns, pitch * game.stages[0].Rows) + Vector2.one * (2 * padding);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.CloseScene(sample, true);
                SceneManager.SetActiveScene(scene);
            }
        }
    }
}
