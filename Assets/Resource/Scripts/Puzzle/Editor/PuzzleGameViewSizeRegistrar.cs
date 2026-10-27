using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FirstPuzzle.Editor
{
    /// <summary>
    /// Automatically registers representative mobile portrait resolutions, Galaxy Z Fold (Cover & Main Inner),
    /// and Surface Duo / Dual-Screen (iPhone/Foldable Duo) presets into the Unity Game View resolution dropdown.
    /// </summary>
    [InitializeOnLoad]
    public static class PuzzleGameViewSizeRegistrar
    {
        public readonly struct Preset
        {
            public readonly string Label;
            public readonly int Width;
            public readonly int Height;

            public Preset(string label, int width, int height)
            {
                Label = label;
                Width = width;
                Height = height;
            }
        }

        // Representative Mobile Portrait, Foldable (Z Fold), and Duo resolutions
        // Note: Unity's GameViewSize.baseText is truncated at 40 characters by Unity internally,
        // and Unity automatically appends " (WxH)" in the dropdown UI. Keep labels <= 38 chars!
        public static readonly Preset[] Presets =
        {
            // Standard & Representative Mobile Portrait Resolutions
            new Preset("Phone Portrait HD+", 720, 1600),
            new Preset("Phone Portrait 16:9 FHD", 1080, 1920),
            new Preset("Phone Portrait 19.5:9 Galaxy/iPhone", 1080, 2340),
            new Preset("Phone Portrait 20:9 Android FHD+", 1080, 2400),
            new Preset("iPhone 15/16 Pro Portrait", 1179, 2556),
            new Preset("iPhone 15/16 Pro Max Portrait", 1290, 2796),
            new Preset("Galaxy S24 Ultra QHD+ Portrait", 1440, 3120),

            // Galaxy Z Fold (Cover Screen & Unfolded Main Display)
            new Preset("Galaxy Z Fold Cover Portrait", 904, 2316),
            new Preset("Galaxy Z Fold Unfolded Portrait", 1812, 2176),
            new Preset("Galaxy Z Fold Unfolded Landscape", 2176, 1812),

            // Duo / Dual-Screen (iPhone Duo / Surface Duo Single & Dual Spanned)
            new Preset("iPhone/Surface Duo Single Portrait", 1350, 1800),
            new Preset("iPhone/Surface Duo Dual Portrait", 1800, 2700),
            new Preset("iPhone/Surface Duo Dual Landscape", 2700, 1800)
        };

        static PuzzleGameViewSizeRegistrar()
        {
            EditorApplication.delayCall += EnsureRegistered;
        }

        [MenuItem("Puzzle/Register Mobile, Fold & Duo Game View Resolutions")]
        public static void RegisterFromMenu()
        {
            int added = EnsureRegisteredAndCount();
            Debug.Log($"Game View resolutions synchronized: {added} new preset(s) added ({Presets.Length} total mobile/fold/duo presets verified).");
        }

        public static void EnsureRegistered()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EnsureRegisteredAndCount();
        }

        public static int EnsureRegisteredAndCount()
        {
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp == null) return 0;

            object sizesInstance = instanceProp.GetValue(null, null);
            if (sizesInstance == null) return 0;

            var getGroupMethod = sizesType.GetMethod("GetGroup", BindingFlags.Public | BindingFlags.Instance);
            var currentGroupTypeProp = sizesType.GetProperty("currentGroupType", BindingFlags.Public | BindingFlags.Instance);
            var saveMethod = sizesType.GetMethod("SaveToHDD", BindingFlags.Public | BindingFlags.Instance);
            if (getGroupMethod == null) return 0;

            // Register in Standalone, Android, iOS, and current group so the dropdown always shows them regardless of active Build Target
            var targetGroups = new HashSet<GameViewSizeGroupType>
            {
                GameViewSizeGroupType.Standalone,
                GameViewSizeGroupType.Android,
                GameViewSizeGroupType.iOS
            };
            if (currentGroupTypeProp != null)
            {
                var current = (GameViewSizeGroupType)currentGroupTypeProp.GetValue(sizesInstance, null);
                targetGroups.Add(current);
            }

            int totalAdded = 0;
            foreach (GameViewSizeGroupType groupType in targetGroups)
            {
                object group = getGroupMethod.Invoke(sizesInstance, new object[] { groupType });
                if (group != null)
                    totalAdded += EnsurePresetsInGroup(group);
            }

            if (totalAdded > 0 && saveMethod != null)
                saveMethod.Invoke(sizesInstance, null);

            return totalAdded;
        }

        public static bool HasPresetInCurrentGroup(string label, int width, int height)
        {
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            object sizesInstance = instanceProp?.GetValue(null, null);
            if (sizesInstance == null) return false;

            var getGroupMethod = sizesType.GetMethod("GetGroup", BindingFlags.Public | BindingFlags.Instance);
            var currentGroupTypeProp = sizesType.GetProperty("currentGroupType", BindingFlags.Public | BindingFlags.Instance);
            var current = (GameViewSizeGroupType)currentGroupTypeProp.GetValue(sizesInstance, null);
            object group = getGroupMethod.Invoke(sizesInstance, new object[] { current });
            return GroupContains(group, label, width, height);
        }

        private static int EnsurePresetsInGroup(object group)
        {
            var groupType = group.GetType();
            var addCustomSize = groupType.GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            var removeCustomSize = groupType.GetMethod("RemoveCustomSize", BindingFlags.Public | BindingFlags.Instance);
            var getBuiltinCount = groupType.GetMethod("GetBuiltinCount", BindingFlags.Public | BindingFlags.Instance);
            var getTotalCount = groupType.GetMethod("GetTotalCount", BindingFlags.Public | BindingFlags.Instance);
            var getGameViewSize = groupType.GetMethod("GetGameViewSize", BindingFlags.Public | BindingFlags.Instance);
            var gameViewSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var gameViewSizeTypeEnum = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            if (addCustomSize == null || gameViewSizeType == null || gameViewSizeTypeEnum == null) return 0;

            int changed = 0;
            // Clean up any legacy/truncated duplicate custom entries with the same preset dimensions but outdated labels
            if (removeCustomSize != null && getBuiltinCount != null && getTotalCount != null && getGameViewSize != null)
            {
                int builtinCount = (int)getBuiltinCount.Invoke(group, null);
                int totalCount = (int)getTotalCount.Invoke(group, null);
                for (int i = totalCount - 1; i >= builtinCount; i--)
                {
                    object size = getGameViewSize.Invoke(group, new object[] { i });
                    if (size == null) continue;
                    var sizeType = size.GetType();
                    string baseText = sizeType.GetProperty("baseText")?.GetValue(size, null) as string ?? "";
                    int w = (int)(sizeType.GetProperty("width")?.GetValue(size, null) ?? 0);
                    int h = (int)(sizeType.GetProperty("height")?.GetValue(size, null) ?? 0);
                    if (IsLegacyDuplicateEntry(baseText, w, h))
                    {
                        removeCustomSize.Invoke(group, new object[] { i });
                        changed++;
                    }
                }
            }

            object fixedResolutionEnum = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
            var ctor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            if (ctor == null) return changed;

            foreach (Preset preset in Presets)
            {
                if (GroupContains(group, preset.Label, preset.Width, preset.Height))
                    continue;

                object newSize = ctor.Invoke(new[] { fixedResolutionEnum, preset.Width, preset.Height, preset.Label });
                addCustomSize.Invoke(group, new[] { newSize });
                changed++;
            }
            return changed;
        }

        private static bool IsLegacyDuplicateEntry(string baseText, int width, int height)
        {
            if (string.IsNullOrEmpty(baseText)) return false;
            bool matchesPresetDims = false;
            foreach (Preset preset in Presets)
            {
                if (preset.Width == width && preset.Height == height)
                {
                    if (string.Equals(preset.Label, baseText, StringComparison.Ordinal))
                        return false;
                    matchesPresetDims = true;
                }
            }
            if (!matchesPresetDims) return false;
            return baseText.StartsWith("Phone Portrait", StringComparison.Ordinal)
                || baseText.StartsWith("iPhone 15/16", StringComparison.Ordinal)
                || baseText.StartsWith("Galaxy ", StringComparison.Ordinal)
                || baseText.StartsWith("Duo ", StringComparison.Ordinal);
        }

        private static bool GroupContains(object group, string label, int width, int height)
        {
            if (group == null) return false;
            var groupType = group.GetType();
            var getTotalCount = groupType.GetMethod("GetTotalCount", BindingFlags.Public | BindingFlags.Instance);
            var getGameViewSize = groupType.GetMethod("GetGameViewSize", BindingFlags.Public | BindingFlags.Instance);
            if (getTotalCount == null || getGameViewSize == null) return false;

            int count = (int)getTotalCount.Invoke(group, null);
            for (int i = 0; i < count; i++)
            {
                object size = getGameViewSize.Invoke(group, new object[] { i });
                if (size == null) continue;
                var sizeType = size.GetType();
                string baseText = sizeType.GetProperty("baseText")?.GetValue(size, null) as string;
                int w = (int)(sizeType.GetProperty("width")?.GetValue(size, null) ?? 0);
                int h = (int)(sizeType.GetProperty("height")?.GetValue(size, null) ?? 0);
                if (w == width && h == height && string.Equals(baseText, label, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
