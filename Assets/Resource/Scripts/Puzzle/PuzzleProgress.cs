using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPuzzle
{
    /// <summary>Offline stage progression, unlock rules and local best records.</summary>
    public static class PuzzleProgress
    {
        public static bool IsCleared(StageData stage) =>
            stage != null && !string.IsNullOrEmpty(stage.StageId) &&
            PlayerPrefs.GetInt("PUZZLE_CLEAR_" + stage.StageId, 0) != 0;

        public static bool IsCleared(string stageId) =>
            !string.IsNullOrEmpty(stageId) && PlayerPrefs.GetInt("PUZZLE_CLEAR_" + stageId, 0) != 0;

        public static int GetBestMoves(StageData stage) =>
            stage == null || string.IsNullOrEmpty(stage.StageId)
                ? int.MaxValue
                : PlayerPrefs.GetInt("PUZZLE_BEST_" + stage.StageId, int.MaxValue);

        public static float GetBestTime(StageData stage) =>
            stage == null || string.IsNullOrEmpty(stage.StageId)
                ? float.MaxValue
                : PlayerPrefs.GetFloat("PUZZLE_BEST_TIME_" + stage.StageId, float.MaxValue);

        public static bool IsUnlocked(IReadOnlyList<StageData> stages, int index)
        {
            if (stages == null || index < 0 || index >= stages.Count || stages[index] == null) return false;
            if (index == 0 || IsCleared(stages[index]) || IsCleared(stages[index - 1])) return true;

            // Also unlock the first stage of each distinct grid size so every difficulty tab has an entry point.
            StageData target = stages[index];
            for (int i = 0; i < index; i++)
            {
                StageData earlier = stages[i];
                if (earlier != null && earlier.Columns == target.Columns && earlier.Rows == target.Rows)
                    return IsCleared(stages[i]) || (index > 0 && IsCleared(stages[index - 1]));
            }
            return true;
        }

        public static int NextPlayableIndex(IReadOnlyList<StageData> stages, int currentIndex)
        {
            if (stages == null || stages.Count == 0) return 0;
            for (int offset = 1; offset <= stages.Count; offset++)
            {
                int candidate = (currentIndex + offset) % stages.Count;
                if (IsUnlocked(stages, candidate)) return candidate;
            }
            return 0;
        }

        public static int ClearedCount(IReadOnlyList<StageData> stages)
        {
            if (stages == null) return 0;
            int count = 0;
            for (int i = 0; i < stages.Count; i++)
                if (IsCleared(stages[i])) count++;
            return count;
        }

        public static void RecordClear(StageData stage, int moves, float seconds)
        {
            if (stage == null || string.IsNullOrEmpty(stage.StageId)) return;
            string moveKey = "PUZZLE_BEST_" + stage.StageId;
            int bestMoves = PlayerPrefs.GetInt(moveKey, int.MaxValue);
            PlayerPrefs.SetInt(moveKey, Mathf.Min(bestMoves, Mathf.Max(1, moves)));

            string timeKey = "PUZZLE_BEST_TIME_" + stage.StageId;
            float bestTime = PlayerPrefs.GetFloat(timeKey, float.MaxValue);
            PlayerPrefs.SetFloat(timeKey, Mathf.Min(bestTime, Mathf.Max(0f, seconds)));

            PlayerPrefs.SetInt("PUZZLE_CLEAR_" + stage.StageId, 1);
            PlayerPrefs.Save();
        }
    }
}

