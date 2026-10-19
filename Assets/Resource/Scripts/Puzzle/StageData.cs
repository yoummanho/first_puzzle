using System;
using UnityEngine;

namespace FirstPuzzle
{
    [CreateAssetMenu(fileName = "Stage", menuName = "Puzzle/Stage Data")]
    public sealed class StageData : ScriptableObject
    {
        [SerializeField] private string stageId = "stage-001";
        [SerializeField] private Texture2D illustration;
        [SerializeField, Range(3, 6)] private int columns = 3;
        [SerializeField, Range(3, 8)] private int rows = 3;
        [SerializeField] private int shuffleSeed = 20261020;
        [SerializeField, Min(1)] private int shuffleCount = 100;
        [SerializeField, Min(1)] private int shuffleVersion = 1;

        public string StageId => stageId;
        public Texture2D Illustration => illustration;
        public int Columns => columns;
        public int Rows => rows;
        public int ShuffleSeed => shuffleSeed;
        public int ShuffleCount => shuffleCount;
        public int ShuffleVersion => shuffleVersion;

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(stageId)) error = "Stage ID is required.";
            else if (illustration == null) error = "An illustration texture is required.";
            else if (columns < 3 || columns > 6 || rows < 3 || rows > 8)
                error = "Grid dimensions must be 3..6 columns and 3..8 rows.";
            else if (shuffleCount < 1) error = "Shuffle count must be positive.";
            else if (shuffleVersion != 1) error = "Unsupported shuffle version.";
            else { error = null; return true; }
            return false;
        }

        /// <summary>Center crop preserves the image proportions while all cells remain square.</summary>
        public Rect GetImageUV()
        {
            if (illustration == null) return new Rect(0, 0, 1, 1);
            float imageAspect = (float)illustration.width / illustration.height;
            float boardAspect = (float)columns / rows;
            if (imageAspect > boardAspect)
            {
                float width = boardAspect / imageAspect;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
            float height = imageAspect / boardAspect;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        public Rect GetTileUV(int tile)
        {
            if (tile < 0 || tile >= columns * rows) throw new ArgumentOutOfRangeException(nameof(tile));
            Rect source = GetImageUV();
            float width = source.width / columns;
            float height = source.height / rows;
            return new Rect(source.x + tile % columns * width,
                source.y + source.height - (tile / columns + 1) * height, width, height);
        }
    }
}
