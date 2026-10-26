using System;
using UnityEditor;
using UnityEngine;

namespace FirstPuzzle.Editor
{
    /// <summary>Uses the smallest supported grid closest to the source aspect ratio.</summary>
    public static class PuzzleImageLayout
    {
        public static Vector2Int ChooseGrid(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            double aspect = (double)width / height;
            if (aspect >= .9 && aspect <= 1.1) return new Vector2Int(3, 3);
            var best = new Vector2Int(3, 3);
            double error = double.MaxValue;
            // Keep a short edge of three tiles; larger grids are difficulty choices, not image resolution.
            for (int longEdge = 3; longEdge <= (width >= height ? 6 : 8); longEdge++)
            {
                var grid = width >= height ? new Vector2Int(longEdge, 3) : new Vector2Int(3, longEdge);
                double candidate = Math.Abs(Math.Log(aspect / ((double)grid.x / grid.y)));
                if (candidate < error) { best = grid; error = candidate; }
            }
            return best;
        }

        public static Vector2Int ForTexture(Texture2D texture)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            int width = texture.width, height = texture.height;
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (importer != null) importer.GetSourceTextureWidthAndHeight(out width, out height);
            return ChooseGrid(width, height);
        }
    }
}
