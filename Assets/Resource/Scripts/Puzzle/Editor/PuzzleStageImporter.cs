using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstPuzzle.Editor
{
    /// <summary>Appends new illustrations, removes duplicate texture stages, and sorts stages by ascending grid tile count.</summary>
    public static class PuzzleStageImporter
    {
        public const string ImageFolder = "Assets/Resource.art/Puzzle_images";
        private const string StageFolder = "Assets/Resource/PuzzleGame";

        [MenuItem("Puzzle/Add New Images To Stages")]
        public static void Import()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before importing stages.");

            AssetDatabase.Refresh();
            var original = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(PuzzleGameSceneBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(PuzzleGameSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PuzzleGame>(true)).Single();
                var rawStages = new List<StageData>(game.stages ?? Array.Empty<StageData>());

                // Prefer an already registered static copy over its animated source.
                rawStages.RemoveAll(stage =>
                {
                    if (stage == null || stage.Illustration == null) return true;
                    string path = AssetDatabase.GetAssetPath(stage.Illustration);
                    return HasStaticCopy(path) && rawStages.Any(other =>
                        other != null && other.Illustration != null &&
                        AssetDatabase.GetAssetPath(other.Illustration) == Path.ChangeExtension(path, ".png"));
                });

                // Deduplicate stages sharing the same illustration texture (keep the first occurrence).
                var stages = new List<StageData>();
                var known = new HashSet<Texture2D>();
                foreach (var stage in rawStages)
                {
                    if (stage != null && stage.Illustration != null && known.Add(stage.Illustration))
                        stages.Add(stage);
                }

                // Recover previously created assets too, so repeated imports never duplicate a stage.
                var assets = AssetDatabase.FindAssets("t:StageData", new[] { StageFolder })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(asset => asset != null)
                    .ToList();
                string[] images = AssetDatabase.FindAssets("t:Texture2D", new[] { ImageFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !HasStaticCopy(path))
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                int next = 1;
                int added = 0;
                foreach (string path in images)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture == null || !known.Add(texture)) continue;
                    var stage = assets.FirstOrDefault(asset => asset.Illustration == texture);
                    if (stage == null)
                    {
                        while (AssetDatabase.LoadMainAssetAtPath($"{StageFolder}/Stage{next:00}.asset") != null ||
                               assets.Any(asset => asset.StageId == $"picture-{next:00}")) next++;
                        stage = ScriptableObject.CreateInstance<StageData>();
                        var data = new SerializedObject(stage);
                        data.FindProperty("stageId").stringValue = $"picture-{next:00}";
                        data.FindProperty("illustration").objectReferenceValue = texture;
                        Vector2Int size = PuzzleImageLayout.ForTexture(texture);
                        data.FindProperty("columns").intValue = size.x;
                        data.FindProperty("rows").intValue = size.y;
                        data.FindProperty("shuffleSeed").intValue = 7350 + next;
                        data.FindProperty("shuffleCount").intValue = 30 + (Math.Max(size.x, size.y) - 3) * 20;
                        data.ApplyModifiedPropertiesWithoutUndo();
                        AssetDatabase.CreateAsset(stage, $"{StageFolder}/Stage{next:00}.asset");
                        assets.Add(stage);
                        next++;
                    }
                    stages.Add(stage);
                    added++;
                }

                // Sort stages by difficulty: 3x3 first (lowest tile count), then increasing tile count (Columns * Rows).
                stages = stages
                    .OrderBy(s => s.Columns * s.Rows)
                    .ThenBy(s => s.Columns)
                    .ThenBy(s => s.Rows)
                    .ThenBy(s => s.Illustration != null ? s.Illustration.name : s.StageId, StringComparer.Ordinal)
                    .ToList();

                foreach (var stage in stages)
                    if (!stage.Validate(out string error)) throw new InvalidOperationException(stage.name + ": " + error);

                bool changed = added > 0 || game.stages == null || !game.stages.SequenceEqual(stages);
                if (changed)
                {
                    game.stages = stages.ToArray();
                    EditorUtility.SetDirty(game);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"Puzzle stage import: added {added}; total {stages.Count}.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        private static bool HasStaticCopy(string path)
        {
            string extension = Path.GetExtension(path);
            return (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)) &&
                   File.Exists(Path.ChangeExtension(path, ".png"));
        }
    }
}
