using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstPuzzle.Editor
{
    /// <summary>
    /// Generates physical .wav audio assets in Assets/Resource/Audio, assigns them to PuzzleAudio in PuzzleGame.unity,
    /// and cleans up PortraitFrame/Content in Edit Mode so Undo/Hold to preview/Numbers are inside PauseControls
    /// and the duplicate Content/Restart is removed.
    /// </summary>
    [InitializeOnLoad]
    public static class PuzzleSceneAudioAndHierarchySync
    {
        public const string AudioFolder = "Assets/Resource/Audio";
        public const string TitleBgmPath = AudioFolder + "/bgm_title.wav";
        public const string LobbyBgmPath = AudioFolder + "/bgm_lobby.wav";
        public const string InGameBgmPath = AudioFolder + "/bgm_ingame_loop.wav";
        public const string GameStartSfxPath = AudioFolder + "/sfx_game_start.wav";
        public const string TileMoveSfxPath = AudioFolder + "/sfx_tile_move.wav";
        public const string TileCorrectSfxPath = AudioFolder + "/sfx_tile_correct.wav";
        public const string StageClearSfxPath = AudioFolder + "/sfx_stage_clear.wav";

        private const string SyncSessionKey = "FirstPuzzle.SceneAudioAndHierarchySync.v3";

        static PuzzleSceneAudioAndHierarchySync()
        {
            EditorApplication.delayCall += TryAutoSyncOnce;
        }

        private static void TryAutoSyncOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (SessionState.GetBool(SyncSessionKey, false))
                return;
            SessionState.SetBool(SyncSessionKey, true);

            if (NeedsSync())
                SyncSceneAndAudio();
        }

        private static bool NeedsSync()
        {
            if (!File.Exists(TitleBgmPath) || !File.Exists(LobbyBgmPath) || !File.Exists(InGameBgmPath) ||
                !File.Exists(GameStartSfxPath) || !File.Exists(TileMoveSfxPath) || !File.Exists(TileCorrectSfxPath) ||
                !File.Exists(StageClearSfxPath))
                return true;

            if (new FileInfo(InGameBgmPath).Length < 700000)
                return true;

            var scene = SceneManager.GetSceneByPath(PuzzleGameSceneBuilder.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                var game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuzzleGame>(true)).FirstOrDefault();
                if (game != null)
                {
                    var audio = game.GetComponent<PuzzleAudio>();
                    if (audio == null || audio.TitleMusicClip == null || audio.LobbyMusicClip == null ||
                        audio.InGameMusicClip == null || audio.GameStartClip == null || audio.MoveClip == null ||
                        audio.TileCorrectClip == null || audio.ClearClip == null)
                        return true;

                    var content = game.pauseButton != null ? game.pauseButton.transform.parent : null;
                    if (content != null && (content.Find("Undo") != null || content.Find("Hold to preview") != null ||
                                            content.Find("Numbers: ON") != null || content.Find("Restart") != null))
                        return true;
                }
            }
            return false;
        }

        [MenuItem("Puzzle/Sync Audio Assets And Scene Hierarchy")]
        public static void SyncSceneAndAudio()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");

            EnsureAudioWavFiles();
            AssetDatabase.Refresh();

            var original = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(PuzzleGameSceneBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(PuzzleGameSceneBuilder.ScenePath, OpenSceneMode.Additive);

            try
            {
                var game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuzzleGame>(true)).Single();
                bool dirty = false;

                // 1. Ensure PuzzleAudio component on PuzzleGame root and assign all 7 AudioClip assets
                var audio = game.GetComponent<PuzzleAudio>();
                if (audio == null)
                {
                    audio = game.gameObject.AddComponent<PuzzleAudio>();
                    dirty = true;
                }

                var so = new SerializedObject(audio);
                dirty |= AssignClip(so, "titleMusicClip", TitleBgmPath);
                dirty |= AssignClip(so, "lobbyMusicClip", LobbyBgmPath);
                dirty |= AssignClip(so, "musicClip", InGameBgmPath);
                dirty |= AssignClip(so, "gameStartClip", GameStartSfxPath);
                dirty |= AssignClip(so, "moveClip", TileMoveSfxPath);
                dirty |= AssignClip(so, "tileCorrectClip", TileCorrectSfxPath);
                dirty |= AssignClip(so, "clearClip", StageClearSfxPath);
                if (dirty) so.ApplyModifiedPropertiesWithoutUndo();

                // 2. Clean up PortraitFrame/Content in Edit Mode:
                //    - Move Undo, Hold to preview, Numbers: ON into PauseAndResult/PauseControls
                //    - Delete duplicate Content/Restart (PauseControls/Restart puzzle already exists)
                //    - Position Pause at top-left with 3-bar HamburgerIcon and expand BoardFrame
                dirty |= CleanUpContentAndPauseHierarchy(game);

                if (dirty)
                {
                    EditorUtility.SetDirty(game);
                    EditorUtility.SetDirty(audio);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log("PuzzleGame scene hierarchy and 7 AudioClip inspector references synchronized.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        private static bool AssignClip(SerializedObject so, string propName, string assetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            var prop = so.FindProperty(propName);
            if (prop == null || clip == null || prop.objectReferenceValue == clip) return false;
            prop.objectReferenceValue = clip;
            return true;
        }

        private static bool CleanUpContentAndPauseHierarchy(PuzzleGame game)
        {
            bool changed = false;
            if (game.pauseControls == null || game.pauseButton == null) return false;

            Transform pauseParent = game.pauseControls.transform;
            Transform content = game.pauseButton.transform.parent;
            if (content == null) return false;

            // Remove duplicate Restart from Content (Restart puzzle already exists in PauseControls)
            var duplicateRestart = content.Find("Restart");
            if (duplicateRestart != null)
            {
                UnityEngine.Object.DestroyImmediate(duplicateRestart.gameObject);
                changed = true;
            }

            // Reparent Undo from Content into PauseControls
            if (game.undoButton != null && game.undoButton.transform.parent == content)
            {
                game.undoButton.transform.SetParent(pauseParent, false);
                changed = true;
            }

            // Reparent Hold to preview from Content into PauseControls
            var holdPreview = game.GetComponentInChildren<HoldPreview>(true);
            if (holdPreview != null && holdPreview.transform.parent == content)
            {
                holdPreview.transform.SetParent(pauseParent, false);
                changed = true;
            }

            // Reparent Numbers: ON from Content into PauseControls
            var numbersBtn = game.numbersLabel != null ? game.numbersLabel.GetComponentInParent<Button>(true) : null;
            if (numbersBtn != null && numbersBtn.transform.parent == content)
            {
                numbersBtn.transform.SetParent(pauseParent, false);
                changed = true;
            }

            // Style and position HUD Pause button at top-left with 3-bar HamburgerIcon directly in Edit Mode
            var pauseRect = game.pauseButton.transform as RectTransform;
            if (pauseRect != null)
            {
                Vector2 targetMin = new Vector2(0.00f, 0.905f);
                Vector2 targetMax = new Vector2(0.16f, 0.985f);
                if (pauseRect.anchorMin != targetMin || pauseRect.anchorMax != targetMax)
                {
                    pauseRect.anchorMin = targetMin;
                    pauseRect.anchorMax = targetMax;
                    pauseRect.offsetMin = pauseRect.offsetMax = Vector2.zero;
                    changed = true;
                }

                var pauseLabel = game.pauseButton.GetComponentInChildren<TMP_Text>(true);
                if (pauseLabel != null && pauseLabel.text != "")
                {
                    pauseLabel.text = "";
                    changed = true;
                }

                if (pauseRect.Find("HamburgerIcon") == null)
                {
                    var iconRoot = new GameObject("HamburgerIcon", typeof(RectTransform)).GetComponent<RectTransform>();
                    iconRoot.SetParent(pauseRect, false);
                    iconRoot.anchorMin = new Vector2(0.24f, 0.24f);
                    iconRoot.anchorMax = new Vector2(0.76f, 0.76f);
                    iconRoot.offsetMin = iconRoot.offsetMax = Vector2.zero;

                    CreateBar(iconRoot, "BarTop", 0.76f, 0.94f);
                    CreateBar(iconRoot, "BarMid", 0.41f, 0.59f);
                    CreateBar(iconRoot, "BarBot", 0.06f, 0.24f);
                    changed = true;
                }
            }

            // Update Content layout and readable dark navy text colors for Edit Mode preview
            SetAnchorsIfChanged(game.heading?.rectTransform, 0.18f, 0.895f, 1.00f, 0.99f, ref changed);
            SetAnchorsIfChanged(game.stats?.rectTransform, 0.00f, 0.825f, 1.00f, 0.885f, ref changed);
            SetAnchorsIfChanged(content.Find("BoardFrame") as RectTransform, 0.00f, 0.11f, 1.00f, 0.81f, ref changed);
            SetAnchorsIfChanged(content.Find("Instructions") as RectTransform, 0.00f, 0.05f, 1.00f, 0.095f, ref changed);
            SetAnchorsIfChanged(content.Find("Footer") as RectTransform, 0.00f, 0.005f, 1.00f, 0.042f, ref changed);

            SetLabelColorIfChanged(game.heading, new Color(0.11f, 0.24f, 0.34f, 1f), true, ref changed);
            SetLabelColorIfChanged(game.stats, new Color(0.14f, 0.28f, 0.38f, 0.96f), true, ref changed);
            SetLabelColorIfChanged(content.Find("Instructions")?.GetComponent<TMP_Text>(), new Color(0.14f, 0.28f, 0.38f, 0.94f), false, ref changed);
            SetLabelColorIfChanged(content.Find("Footer")?.GetComponent<TMP_Text>(), new Color(0.12f, 0.24f, 0.32f, 0.92f), false, ref changed);

            return changed;
        }

        private static void CreateBar(Transform parent, string name, float yMin, float yMax)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.anchorMin = new Vector2(0f, yMin);
            bar.anchorMax = new Vector2(1f, yMax);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            var img = bar.GetComponent<Image>();
            img.color = new Color(0.90f, 0.96f, 0.98f, 1f);
            img.raycastTarget = false;
        }

        private static void SetAnchorsIfChanged(RectTransform rect, float x0, float y0, float x1, float y1, ref bool changed)
        {
            if (rect == null) return;
            Vector2 min = new Vector2(x0, y0);
            Vector2 max = new Vector2(x1, y1);
            if (rect.anchorMin != min || rect.anchorMax != max || rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero)
            {
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                changed = true;
            }
        }

        private static void SetLabelColorIfChanged(TMP_Text label, Color color, bool bold, ref bool changed)
        {
            if (label == null) return;
            if (label.color != color)
            {
                label.color = color;
                changed = true;
            }
            if (bold && (label.fontStyle & FontStyles.Bold) == 0)
            {
                label.fontStyle |= FontStyles.Bold;
                changed = true;
            }
        }

        public static void EnsureAudioWavFiles()
        {
            if (!Directory.Exists(AudioFolder))
                Directory.CreateDirectory(AudioFolder);

            const int rate = 22050;
            WriteWavIfMissing(TitleBgmPath, SynthesizeTitleBgm(rate), rate);
            WriteWavIfMissing(LobbyBgmPath, SynthesizeLobbyBgm(rate), rate);
            WriteWavExactLength(InGameBgmPath, SynthesizeInGameLoopBgm(rate), rate);
            WriteWavIfMissing(GameStartSfxPath, SynthesizeCrispChime(rate, new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.065f), rate);
            WriteWavIfMissing(TileMoveSfxPath, SynthesizeTone(rate, new[] { 783.99f, 987.77f }, 0.032f), rate);
            WriteWavIfMissing(TileCorrectSfxPath, SynthesizeCrispChime(rate, new[] { 1318.51f, 1975.53f }, 0.045f), rate);
            WriteWavIfMissing(StageClearSfxPath, SynthesizeTone(rate, new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.13f), rate);
        }

        private static void WriteWavExactLength(string path, float[] samples, int sampleRate)
        {
            int expectedBytes = 44 + samples.Length * 2;
            if (File.Exists(path) && new FileInfo(path).Length == expectedBytes) return;
            if (File.Exists(path)) File.Delete(path);
            WriteWavIfMissing(path, samples, sampleRate);
        }

        private static void WriteWavIfMissing(string path, float[] samples, int sampleRate)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 100) return;

            int numSamples = samples.Length;
            int byteRate = sampleRate * 2;
            int dataSize = numSamples * 2;

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // PCM chunk size
            bw.Write((short)1); // AudioFormat = 1 (PCM)
            bw.Write((short)1); // NumChannels = 1 (Mono)
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2); // BlockAlign
            bw.Write((short)16); // BitsPerSample
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            for (int i = 0; i < numSamples; i++)
            {
                float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                short pcm = (short)Mathf.RoundToInt(clamped * 32767f);
                bw.Write(pcm);
            }
        }

        private static float[] SynthesizeTitleBgm(int rate)
        {
            int samplesPerStep = Mathf.RoundToInt(rate * 0.125f);
            float[] lead =
            {
                523.25f, 659.25f, 783.99f, 1046.50f, 987.77f, 0f, 783.99f, 0f,
                880.00f, 0f, 783.99f, 0f, 659.25f, 0f, 523.25f, 0f,
                587.33f, 698.46f, 880.00f, 1174.66f, 1046.50f, 0f, 880.00f, 0f,
                783.99f, 659.25f, 587.33f, 659.25f, 783.99f, 0f, 0f, 0f
            };
            float[] bass =
            {
                261.63f, 0f, 329.63f, 0f, 392.00f, 0f, 329.63f, 0f,
                220.00f, 0f, 261.63f, 0f, 329.63f, 0f, 261.63f, 0f,
                293.66f, 0f, 349.23f, 0f, 440.00f, 0f, 349.23f, 0f,
                196.00f, 0f, 246.94f, 0f, 293.66f, 0f, 196.00f, 0f
            };
            return RenderTwoVoiceLoop(rate, samplesPerStep, lead, bass, 0.36f, 0.22f);
        }

        private static float[] SynthesizeLobbyBgm(int rate)
        {
            int samplesPerStep = Mathf.RoundToInt(rate * 0.134f);
            float[] lead =
            {
                659.25f, 0f, 523.25f, 659.25f, 783.99f, 0f, 659.25f, 0f,
                587.33f, 0f, 493.88f, 587.33f, 698.46f, 0f, 587.33f, 0f,
                523.25f, 0f, 440.00f, 523.25f, 659.25f, 0f, 523.25f, 0f,
                587.33f, 0f, 659.25f, 0f, 523.25f, 0f, 0f, 0f
            };
            float[] bass =
            {
                130.81f, 196.00f, 261.63f, 196.00f, 130.81f, 196.00f, 261.63f, 196.00f,
                146.83f, 220.00f, 293.66f, 220.00f, 146.83f, 220.00f, 293.66f, 220.00f,
                174.61f, 261.63f, 349.23f, 261.63f, 174.61f, 261.63f, 349.23f, 261.63f,
                196.00f, 246.94f, 293.66f, 246.94f, 130.81f, 196.00f, 261.63f, 0f
            };
            return RenderTwoVoiceLoop(rate, samplesPerStep, lead, bass, 0.32f, 0.20f);
        }

        private static float[] RenderTwoVoiceLoop(int rate, int samplesPerStep, float[] lead, float[] bass, float leadGain, float bassGain)
        {
            int totalSamples = samplesPerStep * lead.Length;
            var samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                int step = i / samplesPerStep;
                int stepSample = i % samplesPerStep;
                float t = stepSample / (float)rate;
                float progress = stepSample / (float)samplesPerStep;
                float env = Mathf.Clamp01(progress / 0.06f) * Mathf.Pow(1f - progress, 0.85f);
                float s = 0f;
                if (lead[step] > 0f)
                {
                    float p = 2f * Mathf.PI * lead[step] * t;
                    s += (Mathf.Sin(p) + 0.25f * Mathf.Sin(2f * p) + 0.1f * Mathf.Sin(3f * p)) * env * leadGain;
                }
                if (bass[step] > 0f)
                {
                    float p = 2f * Mathf.PI * bass[step] * t;
                    s += (Mathf.Sin(p) + 0.2f * Mathf.Sin(2f * p)) * env * bassGain;
                }
                samples[i] = Mathf.Clamp(s, -0.9f, 0.9f);
            }
            return samples;
        }

        private static float[] SynthesizeInGameLoopBgm(int rate)
        {
            // 120 BPM sixteenth note (0.125s per step; 150 BPM / 0.10s is reserved for future time-limit stages)
            int samplesPerStep = Mathf.RoundToInt(rate * 0.125f);
            float[] lead =
            {
                523.25f, 0f, 659.25f, 0f, 783.99f, 0f, 659.25f, 0f,
                880.00f, 0f, 783.99f, 0f, 659.25f, 783.99f, 1046.50f, 0f,
                880.00f, 0f, 698.46f, 0f, 783.99f, 0f, 587.33f, 0f,
                659.25f, 587.33f, 523.25f, 587.33f, 659.25f, 0f, 783.99f, 0f,
                1046.50f, 0f, 987.77f, 0f, 880.00f, 0f, 783.99f, 0f,
                698.46f, 783.99f, 880.00f, 0f, 783.99f, 659.25f, 523.25f, 0f,
                587.33f, 659.25f, 698.46f, 0f, 783.99f, 880.00f, 987.77f, 0f,
                1046.50f, 0f, 783.99f, 659.25f, 1046.50f, 0f, 0f, 0f
            };
            float[] harmony =
            {
                0f, 329.63f, 0f, 392.00f, 0f, 523.25f, 0f, 392.00f,
                0f, 440.00f, 0f, 392.00f, 0f, 493.88f, 0f, 523.25f,
                0f, 440.00f, 0f, 349.23f, 0f, 392.00f, 0f, 293.66f,
                0f, 329.63f, 0f, 392.00f, 0f, 493.88f, 0f, 392.00f,
                0f, 523.25f, 0f, 493.88f, 0f, 440.00f, 0f, 392.00f,
                0f, 349.23f, 0f, 440.00f, 0f, 392.00f, 0f, 329.63f,
                0f, 349.23f, 0f, 440.00f, 0f, 493.88f, 0f, 587.33f,
                523.25f, 0f, 392.00f, 0f, 523.25f, 659.25f, 783.99f, 0f
            };
            float[] bass =
            {
                130.81f, 261.63f, 130.81f, 261.63f, 164.81f, 329.63f, 196.00f, 392.00f,
                220.00f, 440.00f, 196.00f, 392.00f, 164.81f, 329.63f, 130.81f, 261.63f,
                174.61f, 349.23f, 174.61f, 349.23f, 196.00f, 392.00f, 196.00f, 392.00f,
                130.81f, 261.63f, 146.83f, 293.66f, 164.81f, 329.63f, 196.00f, 392.00f,
                130.81f, 261.63f, 164.81f, 329.63f, 220.00f, 440.00f, 196.00f, 392.00f,
                174.61f, 349.23f, 174.61f, 349.23f, 130.81f, 261.63f, 164.81f, 329.63f,
                146.83f, 293.66f, 174.61f, 349.23f, 196.00f, 392.00f, 246.94f, 493.88f,
                130.81f, 261.63f, 196.00f, 164.81f, 130.81f, 261.63f, 196.00f, 130.81f
            };

            int totalSamples = samplesPerStep * lead.Length;
            var samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                int step = i / samplesPerStep;
                int stepSample = i % samplesPerStep;
                float t = stepSample / (float)rate;
                float progress = stepSample / (float)samplesPerStep;
                float env = Mathf.Clamp01(progress / 0.08f) * Mathf.Pow(1f - progress, 0.65f);
                float sample = 0f;

                if (lead[step] > 0f)
                {
                    float phase = 2f * Mathf.PI * lead[step] * t;
                    float wave = Mathf.Sin(phase) + 0.32f * Mathf.Sin(3f * phase) + 0.18f * Mathf.Sin(2f * phase);
                    sample += wave * env * 0.42f;
                }
                if (harmony[step] > 0f)
                {
                    float phase = 2f * Mathf.PI * harmony[step] * t;
                    float wave = Mathf.Sin(phase) + 0.25f * Mathf.Sin(2f * phase);
                    sample += wave * env * 0.22f;
                }
                if (bass[step] > 0f)
                {
                    float bassEnv = Mathf.Clamp01(progress / 0.05f) * (1f - 0.55f * progress);
                    float phase = 2f * Mathf.PI * bass[step] * t;
                    float wave = Mathf.Sin(phase) + 0.4f * Mathf.Sin(2f * phase);
                    sample += wave * bassEnv * 0.26f;
                }
                if (step % 2 == 1 && progress < 0.22f)
                {
                    float hatEnv = 1f - (progress / 0.22f);
                    float pseudoNoise = Mathf.Sin(i * 12.9898f) * 43758.5453f;
                    pseudoNoise -= Mathf.Floor(pseudoNoise);
                    sample += (pseudoNoise * 2f - 1f) * hatEnv * hatEnv * 0.06f;
                }
                samples[i] = Mathf.Clamp(sample, -0.95f, 0.95f);
            }
            return samples;
        }

        private static float[] SynthesizeCrispChime(int rate, float[] notes, float secondsPerNote)
        {
            int samplesPerNote = Mathf.RoundToInt(rate * secondsPerNote);
            var samples = new float[samplesPerNote * notes.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                int noteIdx = i / samplesPerNote;
                int sampleInNote = i % samplesPerNote;
                float t = sampleInNote / (float)rate;
                float progress = sampleInNote / (float)samplesPerNote;
                float env = Mathf.Clamp01(progress / 0.05f) * Mathf.Exp(-3.2f * progress);
                float phase = 2f * Mathf.PI * notes[noteIdx] * t;
                float wave = Mathf.Sin(phase) + 0.35f * Mathf.Sin(2f * phase) + 0.15f * Mathf.Sin(3f * phase);
                samples[i] = Mathf.Clamp(wave * env * 0.65f, -0.95f, 0.95f);
            }
            return samples;
        }

        private static float[] SynthesizeTone(int rate, float[] notes, float secondsPerNote)
        {
            int samplesPerNote = Mathf.RoundToInt(rate * secondsPerNote);
            var samples = new float[samplesPerNote * notes.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (i % samplesPerNote) / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * t / secondsPerNote);
                samples[i] = Mathf.Sin(2 * Mathf.PI * notes[i / samplesPerNote] * t) * envelope * envelope;
            }
            return samples;
        }
    }
}

