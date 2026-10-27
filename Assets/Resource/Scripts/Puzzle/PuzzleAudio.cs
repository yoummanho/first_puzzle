using System.Collections;
using UnityEngine;

namespace FirstPuzzle
{
    public enum MusicContext
    {
        Title = 0,
        Lobby = 1,
        InGame = 2
    }

    /// <summary>
    /// Separate music/effects channels with distinct Title, Lobby, and In-Game BGM tracks,
    /// plus Game Start SFX, Tile Move SFX, Tile Correct Placement (crisp chime) SFX, and Clear SFX.
    /// All AudioClips can be assigned directly in the PuzzleGame Inspector.
    /// </summary>
    public sealed class PuzzleAudio : MonoBehaviour
    {
        [Header("BGM Clips (Editable in Inspector)")]
        [SerializeField] private AudioClip titleMusicClip;
        [SerializeField] private AudioClip lobbyMusicClip;
        [SerializeField] private AudioClip musicClip; // In-Game Loop BGM

        [Header("SFX Clips (Editable in Inspector)")]
        [SerializeField] private AudioClip gameStartClip;
        [SerializeField] private AudioClip moveClip;
        [SerializeField] private AudioClip tileCorrectClip;
        [SerializeField] private AudioClip clearClip;

        private const float DefaultMusicVolume = 0.22f;
        public const float InGameStartDelaySeconds = 0.5f;
        public const float InGameFadeInDurationSeconds = 0.8f;

        private AudioSource music, effects;
        private AudioClip generatedTitleMusic, generatedLobbyMusic, generatedInGameMusic;
        private AudioClip generatedGameStart, generatedMove, generatedTileCorrect, generatedClear;
        private SettingsManager settings;
        private BoardManager board;
        private int previousMoves, previousUndo;
        private MusicContext currentContext = MusicContext.Title;
        private Coroutine musicTransitionRoutine;

        public AudioClip TitleMusicClip => titleMusicClip;
        public AudioClip LobbyMusicClip => lobbyMusicClip;
        public AudioClip InGameMusicClip => musicClip;
        public AudioClip GameStartClip => gameStartClip;
        public AudioClip MoveClip => moveClip;
        public AudioClip TileCorrectClip => tileCorrectClip;
        public AudioClip ClearClip => clearClip;

        public AudioClip CurrentMusicClip => music != null ? music.clip : null;
        public float CurrentMusicVolume => music != null ? music.volume : 0f;
        public bool IsMusicTransitioning => musicTransitionRoutine != null;
        public MusicContext ActiveMusicContext => currentContext;
        public AudioClip LastEffectPlayed { get; private set; }
        public int TileCorrectPlayCount { get; private set; }
        public int GameStartPlayCount { get; private set; }

        public void Initialize(SettingsManager preferences, BoardManager puzzle)
        {
            settings = preferences;
            board = puzzle;

            var existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 2)
            {
                music = existingSources[0];
                effects = existingSources[1];
            }
            else
            {
                music = existingSources.Length == 1 ? existingSources[0] : gameObject.AddComponent<AudioSource>();
                effects = gameObject.AddComponent<AudioSource>();
            }

            music.playOnAwake = effects.playOnAwake = false;
            music.loop = true;
            music.volume = DefaultMusicVolume;
            effects.volume = 0.26f;

            if (titleMusicClip == null) titleMusicClip = generatedTitleMusic = TitleScreenBgm("Puzzle title bgm");
            if (lobbyMusicClip == null) lobbyMusicClip = generatedLobbyMusic = LobbyBgm("Puzzle lobby bgm");
            if (musicClip == null) musicClip = generatedInGameMusic = UpbeatPuzzleBgm("Puzzle upbeat arcade bgm");

            if (gameStartClip == null) gameStartClip = generatedGameStart = CrispBell("Game start sfx", new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.065f);
            if (moveClip == null) moveClip = generatedMove = Tone("Tile move", new[] { 783.99f, 987.77f }, 0.032f);
            if (tileCorrectClip == null) tileCorrectClip = generatedTileCorrect = CrispChime("Tile placed correctly", new[] { 1318.51f, 1975.53f }, 0.045f);
            if (clearClip == null) clearClip = generatedClear = Tone("Puzzle clear", new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.13f);

            settings.Changed -= Apply;
            settings.Changed += Apply;
            board.StateChanged -= OnBoardChanged;
            board.StateChanged += OnBoardChanged;
            board.TilePlacedCorrectly -= OnTilePlacedCorrectly;
            board.TilePlacedCorrectly += OnTilePlacedCorrectly;
            board.PuzzleSolved -= OnSolved;
            board.PuzzleSolved += OnSolved;

            Apply();
            SetMusicContext(MusicContext.Title, true);
        }

        public void SetMusicContext(MusicContext context, bool forceRestart = false)
        {
            currentContext = context;
            if (music == null) return;
            if (musicTransitionRoutine != null)
            {
                StopCoroutine(musicTransitionRoutine);
                musicTransitionRoutine = null;
            }

            AudioClip target = context switch
            {
                MusicContext.Title => titleMusicClip,
                MusicContext.Lobby => lobbyMusicClip,
                _ => musicClip
            };

            if (target == null) return;
            music.volume = DefaultMusicVolume;
            if (!forceRestart && music.clip == target && music.isPlaying) return;

            music.clip = target;
            music.loop = true;
            music.Play();
        }

        /// <summary>
        /// Plays the Game Start SFX immediately, waits 0.5 seconds after the start SFX, and then slowly fades in the In-Game BGM loop.
        /// </summary>
        public void PlayGameStartThenInGameMusic(float delayAfterSfx = InGameStartDelaySeconds, float fadeInDuration = InGameFadeInDurationSeconds)
        {
            currentContext = MusicContext.InGame;
            if (musicTransitionRoutine != null)
            {
                StopCoroutine(musicTransitionRoutine);
                musicTransitionRoutine = null;
            }

            if (music != null)
            {
                music.Stop();
                music.clip = musicClip;
                music.loop = true;
                music.volume = 0f;
            }

            PlayGameStartSfx();

            if (isActiveAndEnabled && music != null && musicClip != null)
                musicTransitionRoutine = StartCoroutine(DelayedInGameMusicRoutine(delayAfterSfx, fadeInDuration));
        }

        private IEnumerator DelayedInGameMusicRoutine(float delayAfterSfx, float fadeInDuration)
        {
            float sfxDuration = gameStartClip != null ? gameStartClip.length : 0f;
            float totalWait = Mathf.Max(0f, sfxDuration + delayAfterSfx);
            if (totalWait > 0f)
                yield return new WaitForSecondsRealtime(totalWait);

            if (currentContext != MusicContext.InGame || music == null || musicClip == null)
            {
                musicTransitionRoutine = null;
                yield break;
            }

            music.clip = musicClip;
            music.loop = true;
            music.volume = 0f;
            music.Play();

            if (fadeInDuration <= 0f)
            {
                music.volume = DefaultMusicVolume;
                musicTransitionRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeInDuration && currentContext == MusicContext.InGame && music != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                music.volume = Mathf.Lerp(0f, DefaultMusicVolume, t);
                yield return null;
            }

            if (music != null && currentContext == MusicContext.InGame)
                music.volume = DefaultMusicVolume;
            musicTransitionRoutine = null;
        }

        public void PlayGameStartSfx()
        {
            if (effects == null || gameStartClip == null) return;
            GameStartPlayCount++;
            LastEffectPlayed = gameStartClip;
            effects.PlayOneShot(gameStartClip);
        }

        private void Apply()
        {
            if (music != null) music.mute = !settings.Bgm;
            if (effects != null) effects.mute = !settings.Sfx;
        }

        private void OnBoardChanged()
        {
            if (board.MoveCount > previousMoves || board.UndoUsed > previousUndo)
            {
                LastEffectPlayed = moveClip;
                effects.PlayOneShot(moveClip);
            }
            previousMoves = board.MoveCount;
            previousUndo = board.UndoUsed;
        }

        private void OnTilePlacedCorrectly(int count)
        {
            if (effects == null || tileCorrectClip == null || count <= 0) return;
            TileCorrectPlayCount += count;
            LastEffectPlayed = tileCorrectClip;
            effects.PlayOneShot(tileCorrectClip, 0.95f);
        }

        private void OnSolved()
        {
            if (effects == null || clearClip == null) return;
            LastEffectPlayed = clearClip;
            effects.PlayOneShot(clearClip);
        }

        /// <summary>Warm acoustic piano & celesta fanfare loop for the Title Screen (120 BPM, 44.1 kHz).</summary>
        private static AudioClip TitleScreenBgm(string name)
        {
            const int rate = 44100;
            var samples = SynthesizeTitleBgmSamples(rate);
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Relaxed acoustic electric-piano & marimba loop for the Stage Lobby (112 BPM, 44.1 kHz).</summary>
        private static AudioClip LobbyBgm(string name)
        {
            const int rate = 44100;
            var samples = SynthesizeLobbyBgmSamples(rate);
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// High-quality acoustic-electric piano, warm string pad, vibraphone counter-melody, upright bass,
        /// and studio percussion arrangement for the Main Stage In-Game BGM (120 BPM, 8.0s seamless loop, 44.1 kHz).
        /// Uses polyphonic cross-step note decay, stereo-like chorus detuning, and wrap-around reverb so it sounds rich and natural rather than MIDI.
        /// </summary>
        private static AudioClip UpbeatPuzzleBgm(string name)
        {
            const int rate = 44100;
            var samples = SynthesizeInGameLoopSamples(rate);
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static float[] SynthesizeTitleBgmSamples(int rate)
        {
            const float stepSeconds = 0.125f;
            const int steps = 32;
            int totalSamples = Mathf.RoundToInt(rate * stepSeconds * steps);
            var mix = new float[totalSamples];

            float[] lead =
            {
                523.25f, 659.25f, 783.99f, 1046.50f, 987.77f, 0f, 783.99f, 0f,
                880.00f, 0f, 783.99f, 0f, 659.25f, 0f, 523.25f, 0f,
                587.33f, 698.46f, 880.00f, 1174.66f, 1046.50f, 0f, 880.00f, 0f,
                783.99f, 659.25f, 587.33f, 659.25f, 783.99f, 0f, 0f, 0f
            };
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392.00f, 493.88f }, // Cmaj7
                new[] { 220.00f, 261.63f, 329.63f, 392.00f }, // Am7
                new[] { 293.66f, 349.23f, 440.00f, 523.25f }, // Dm7
                new[] { 196.00f, 246.94f, 293.66f, 349.23f }  // G7
            };

            for (int step = 0; step < steps; step++)
            {
                float start = step * stepSeconds;
                if (lead[step] > 0f)
                    AddAcousticPianoNote(mix, rate, start, lead[step], 0.48f, 0.24f);
                if (step % 8 == 0)
                {
                    float[] ch = chords[(step / 8) % chords.Length];
                    for (int n = 0; n < ch.Length; n++)
                        AddWarmPadNote(mix, rate, start + n * 0.015f, ch[n], stepSeconds * 8f, 0.08f);
                    AddUprightBassNote(mix, rate, start, ch[0] * 0.5f, stepSeconds * 4f, 0.22f);
                }
                else if (step % 8 == 4)
                {
                    float[] ch = chords[(step / 8) % chords.Length];
                    AddUprightBassNote(mix, rate, start, ch[2] * 0.5f, stepSeconds * 4f, 0.18f);
                }
            }

            ApplySeamlessStudioPolish(mix, rate);
            return mix;
        }

        public static float[] SynthesizeLobbyBgmSamples(int rate)
        {
            const float stepSeconds = 0.134f;
            const int steps = 32;
            int totalSamples = Mathf.RoundToInt(rate * stepSeconds * steps);
            var mix = new float[totalSamples];

            float[] lead =
            {
                659.25f, 0f, 523.25f, 659.25f, 783.99f, 0f, 659.25f, 0f,
                587.33f, 0f, 493.88f, 587.33f, 698.46f, 0f, 587.33f, 0f,
                523.25f, 0f, 440.00f, 523.25f, 659.25f, 0f, 523.25f, 0f,
                587.33f, 0f, 659.25f, 0f, 523.25f, 0f, 0f, 0f
            };
            float[] bass =
            {
                130.81f, 0f, 196.00f, 0f, 261.63f, 0f, 196.00f, 0f,
                146.83f, 0f, 220.00f, 0f, 293.66f, 0f, 220.00f, 0f,
                174.61f, 0f, 261.63f, 0f, 349.23f, 0f, 261.63f, 0f,
                196.00f, 0f, 246.94f, 0f, 130.81f, 0f, 196.00f, 0f
            };

            for (int step = 0; step < steps; step++)
            {
                float start = step * stepSeconds;
                if (lead[step] > 0f)
                    AddVibraphoneNote(mix, rate, start, lead[step], 0.55f, 0.22f);
                if (bass[step] > 0f)
                    AddUprightBassNote(mix, rate, start, bass[step], 0.34f, 0.20f);
            }

            ApplySeamlessStudioPolish(mix, rate);
            return mix;
        }

        public static float[] SynthesizeInGameLoopSamples(int rate)
        {
            // 120 BPM: 64 sixteenth-note steps * 0.125s = 8.000s exact seamless loop
            const float stepSeconds = 0.125f;
            const int steps = 64;
            int totalSamples = Mathf.RoundToInt(rate * stepSeconds * steps);
            var mix = new float[totalSamples];

            // Expressive, singable acoustic-piano lead melody with natural phrasing
            float[] lead =
            {
                // Bar 1 (Cmaj9 -> G/B)
                659.25f, 0f, 783.99f, 880.00f, 783.99f, 0f, 659.25f, 523.25f,
                587.33f, 0f, 659.25f, 783.99f, 659.25f, 0f, 587.33f, 0f,
                // Bar 2 (Am9 -> Em7)
                523.25f, 0f, 659.25f, 880.00f, 783.99f, 0f, 659.25f, 587.33f,
                523.25f, 587.33f, 659.25f, 0f, 783.99f, 0f, 0f, 0f,
                // Bar 3 (Fmaj9 -> C/E)
                698.46f, 0f, 880.00f, 1046.50f, 987.77f, 0f, 880.00f, 783.99f,
                659.25f, 0f, 783.99f, 880.00f, 783.99f, 0f, 659.25f, 523.25f,
                // Bar 4 (Dm9 -> G13 turnaround)
                587.33f, 659.25f, 698.46f, 783.99f, 880.00f, 0f, 783.99f, 659.25f,
                783.99f, 0f, 587.33f, 659.25f, 523.25f, 0f, 0f, 0f
            };

            // Sparkling vibraphone / rhodes counter-arpeggio
            float[] counter =
            {
                0f, 523.25f, 0f, 0f, 0f, 523.25f, 0f, 0f,
                0f, 493.88f, 0f, 0f, 0f, 493.88f, 0f, 392.00f,
                0f, 440.00f, 0f, 0f, 0f, 440.00f, 0f, 0f,
                0f, 392.00f, 0f, 493.88f, 0f, 587.33f, 523.25f, 0f,
                0f, 523.25f, 0f, 0f, 0f, 587.33f, 0f, 0f,
                0f, 523.25f, 0f, 0f, 0f, 493.88f, 0f, 0f,
                0f, 440.00f, 0f, 523.25f, 0f, 587.33f, 0f, 0f,
                0f, 493.88f, 0f, 440.00f, 0f, 392.00f, 493.88f, 0f
            };

            // Lush 8-chord jazz-pop progression (1 chord every 8 steps = 1.0s)
            float[][] chordProgression =
            {
                new[] { 261.63f, 329.63f, 392.00f, 493.88f }, // Cmaj7
                new[] { 246.94f, 293.66f, 392.00f, 493.88f }, // G/B
                new[] { 220.00f, 261.63f, 329.63f, 392.00f }, // Am7
                new[] { 196.00f, 246.94f, 329.63f, 392.00f }, // Em7
                new[] { 261.63f, 349.23f, 440.00f, 523.25f }, // Fmaj7
                new[] { 261.63f, 329.63f, 392.00f, 493.88f }, // C/E
                new[] { 293.66f, 349.23f, 440.00f, 523.25f }, // Dm7
                new[] { 246.94f, 329.63f, 392.00f, 440.00f }  // G13sus -> G7
            };

            // Warm walking acoustic bassline
            float[] bass =
            {
                130.81f, 0f, 0f, 196.00f, 261.63f, 0f, 196.00f, 0f,
                123.47f, 0f, 0f, 196.00f, 246.94f, 0f, 196.00f, 0f,
                110.00f, 0f, 0f, 164.81f, 220.00f, 0f, 164.81f, 0f,
                82.41f,  0f, 0f, 123.47f, 164.81f, 0f, 196.00f, 0f,
                87.31f,  0f, 0f, 130.81f, 174.61f, 0f, 220.00f, 0f,
                82.41f,  0f, 0f, 130.81f, 164.81f, 0f, 196.00f, 0f,
                73.42f,  0f, 0f, 110.00f, 146.83f, 0f, 174.61f, 0f,
                98.00f,  0f, 146.83f, 0f, 196.00f, 0f, 146.83f, 98.00f
            };

            for (int step = 0; step < steps; step++)
            {
                float start = step * stepSeconds;

                // 1. Warm String & Electric Piano Pad on each chord change (every 8 steps = 1.0s)
                if (step % 8 == 0)
                {
                    float[] chord = chordProgression[(step / 8) % chordProgression.Length];
                    for (int n = 0; n < chord.Length; n++)
                    {
                        // Gentle harp-like upward strum (14ms between chord tones)
                        AddWarmPadNote(mix, rate, start + n * 0.014f, chord[n], stepSeconds * 8.8f, 0.075f);
                        AddElectricPianoComp(mix, rate, start + n * 0.012f, chord[n], stepSeconds * 4.5f, 0.065f);
                    }
                }
                else if (step % 8 == 3 || step % 8 == 6)
                {
                    // Syncopated bossa/pop electric piano chord comping
                    float[] chord = chordProgression[(step / 8) % chordProgression.Length];
                    for (int n = 0; n < chord.Length; n++)
                        AddElectricPianoComp(mix, rate, start + n * 0.008f, chord[n], stepSeconds * 2.4f, 0.048f);
                }

                // 2. Expressive Grand Piano + Bell Layer for Lead Melody (with natural sustain across steps!)
                if (lead[step] > 0f)
                {
                    bool nextIsRest = (step + 1 < steps) && lead[step + 1] <= 0f;
                    float noteDur = nextIsRest ? 0.52f : 0.34f;
                    AddAcousticPianoNote(mix, rate, start, lead[step], noteDur, 0.25f);
                }

                // 3. Vibraphone counter-arpeggio
                if (counter[step] > 0f)
                {
                    AddVibraphoneNote(mix, rate, start, counter[step], 0.42f, 0.11f);
                }

                // 4. Warm Upright / Acoustic Bass
                if (bass[step] > 0f)
                {
                    float bassDur = (step + 1 < steps && bass[step + 1] <= 0f) ? 0.38f : 0.20f;
                    AddUprightBassNote(mix, rate, start, bass[step], bassDur, 0.24f);
                }

                // 5. Soft Acoustic Studio Percussion (warm kick, wooden rim/snare, brushed shaker)
                if (step % 8 == 0 || step % 8 == 3)
                    AddWarmKick(mix, rate, start, 0.16f);
                if (step % 8 == 4)
                    AddSoftBrushSnare(mix, rate, start, 0.10f);
                if (step % 2 == 1)
                    AddSoftShaker(mix, rate, start, 0.032f);
            }

            ApplySeamlessStudioPolish(mix, rate);
            return mix;
        }

        private static void AddAcousticPianoNote(float[] mix, int rate, float startSec, float freq, float durationSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(durationSec * rate);
            int total = mix.Length;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                // Fast felt-hammer attack + natural exponential string decay
                float attack = Mathf.Clamp01(t / 0.006f);
                float decay = Mathf.Exp(-4.2f * t) * 0.72f + Mathf.Exp(-1.35f * t) * 0.28f;
                float release = Mathf.Clamp01((durationSec - t) / 0.045f);
                float env = attack * decay * release;

                // Higher partials decay faster like a real acoustic piano string (natural low-pass filter over time)
                float brightEnv = Mathf.Exp(-11.0f * t);
                // Slight chorus detune (+0.18% / -0.18%) eliminates sterile digital MIDI tone
                float w1 = 2f * Mathf.PI * freq * t;
                float w2 = 2f * Mathf.PI * (freq * 1.0018f) * t;
                float w3 = 2f * Mathf.PI * (freq * 0.9982f) * t;

                float body = (Mathf.Sin(w1) + 0.45f * Mathf.Sin(w2) + 0.45f * Mathf.Sin(w3)) * 0.53f;
                float overtones =
                    0.34f * Mathf.Sin(2f * w1 + 0.3f) * Mathf.Exp(-5.5f * t) +
                    0.16f * Mathf.Sin(3f * w2) * brightEnv +
                    0.07f * Mathf.Sin(4f * w1) * brightEnv * brightEnv;

                // Subtle hammer transient on the first 12ms
                float hammer = t < 0.012f ? Mathf.Sin(7f * w1) * (1f - t / 0.012f) * 0.08f : 0f;

                int idx = (startIdx + i) % total;
                mix[idx] += (body + overtones + hammer) * env * amp;
            }
        }

        private static void AddElectricPianoComp(float[] mix, int rate, float startSec, float freq, float durationSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(durationSec * rate);
            int total = mix.Length;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Clamp01(t / 0.008f) * Mathf.Exp(-3.0f * t) * Mathf.Clamp01((durationSec - t) / 0.05f);
                float modIndex = 0.65f * Mathf.Exp(-9.5f * t) + 0.12f;
                float phase = 2f * Mathf.PI * freq * t;
                // Rhodes-style warm FM tine + fundamental sine
                float wave = Mathf.Sin(phase + modIndex * Mathf.Sin(2f * phase)) + 0.35f * Mathf.Sin(phase * 1.001f);
                int idx = (startIdx + i) % total;
                mix[idx] += wave * env * amp;
            }
        }

        private static void AddVibraphoneNote(float[] mix, int rate, float startSec, float freq, float durationSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(durationSec * rate);
            int total = mix.Length;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float tremolo = 1f + 0.14f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                float env = Mathf.Clamp01(t / 0.005f) * Mathf.Exp(-4.5f * t) * Mathf.Clamp01((durationSec - t) / 0.04f);
                float phase = 2f * Mathf.PI * freq * t;
                float wave = Mathf.Sin(phase) + 0.18f * Mathf.Sin(4f * phase) * Mathf.Exp(-14f * t);
                int idx = (startIdx + i) % total;
                mix[idx] += wave * env * tremolo * amp;
            }
        }

        private static void AddWarmPadNote(float[] mix, int rate, float startSec, float freq, float durationSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(durationSec * rate);
            int total = mix.Length;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                // Slow, silky string-pad swell and gentle fade
                float attack = Mathf.Clamp01(t / 0.16f);
                float release = Mathf.Clamp01((durationSec - t) / 0.22f);
                float env = attack * release * Mathf.Exp(-0.45f * t);

                float p1 = 2f * Mathf.PI * freq * t;
                float p2 = 2f * Mathf.PI * (freq * 1.0025f) * t + 0.7f;
                float p3 = 2f * Mathf.PI * (freq * 0.9975f) * t + 1.4f;
                float wave = (Mathf.Sin(p1) + Mathf.Sin(p2) + Mathf.Sin(p3)) * 0.33f
                           + 0.15f * Mathf.Sin(2f * p1);

                int idx = (startIdx + i) % total;
                mix[idx] += wave * env * amp;
            }
        }

        private static void AddUprightBassNote(float[] mix, int rate, float startSec, float freq, float durationSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(durationSec * rate);
            int total = mix.Length;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float attack = Mathf.Clamp01(t / 0.008f);
                float decay = Mathf.Exp(-3.4f * t);
                float release = Mathf.Clamp01((durationSec - t) / 0.035f);
                float env = attack * decay * release;

                float phase = 2f * Mathf.PI * freq * t;
                // Plucked acoustic bass fundamental + warm 2nd harmonic + finger pluck envelope
                float pluck = Mathf.Exp(-18f * t);
                float wave = Mathf.Sin(phase) + 0.42f * Mathf.Sin(2f * phase) + 0.16f * Mathf.Sin(3f * phase) * pluck;

                int idx = (startIdx + i) % total;
                mix[idx] += wave * env * amp;
            }
        }

        private static void AddWarmKick(float[] mix, int rate, float startSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(0.11f * rate);
            int total = mix.Length;
            float phase = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float freq = 52f + 95f * Mathf.Exp(-38f * t);
                phase += 2f * Mathf.PI * freq / rate;
                float env = Mathf.Exp(-24f * t);
                int idx = (startIdx + i) % total;
                mix[idx] += Mathf.Sin(phase) * env * amp;
            }
        }

        private static void AddSoftBrushSnare(float[] mix, int rate, float startSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(0.085f * rate);
            int total = mix.Length;
            float prev = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-32f * t);
                float body = Mathf.Sin(2f * Mathf.PI * 185f * t) * Mathf.Exp(-45f * t) * 0.45f;
                float n = Mathf.Sin((i + 17) * 12.9898f) * 43758.5453f;
                n = (n - Mathf.Floor(n)) * 2f - 1f;
                // Band-smoothed brush noise
                prev = prev * 0.62f + n * 0.38f;
                int idx = (startIdx + i) % total;
                mix[idx] += (body + prev * 0.55f) * env * amp;
            }
        }

        private static void AddSoftShaker(float[] mix, int rate, float startSec, float amp)
        {
            int startIdx = Mathf.RoundToInt(startSec * rate);
            int len = Mathf.RoundToInt(0.045f * rate);
            int total = mix.Length;
            float prev = 0f;

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.045f)) * Mathf.Exp(-28f * t);
                float n = Mathf.Sin((i + 73) * 78.233f) * 43758.5453f;
                n = (n - Mathf.Floor(n)) * 2f - 1f;
                float highPass = n - prev;
                prev = n;
                int idx = (startIdx + i) % total;
                mix[idx] += highPass * 0.4f * env * amp;
            }
        }

        /// <summary>
        /// Applies a multi-tap studio room reverb (wrapped seamlessly around the loop boundary)
        /// plus warm analog-style soft saturation and a gentle low-pass filter to remove any harsh digital edge.
        /// </summary>
        private static void ApplySeamlessStudioPolish(float[] mix, int rate)
        {
            int total = mix.Length;
            var wet = new float[total];
            int d1 = Mathf.RoundToInt(0.095f * rate);
            int d2 = Mathf.RoundToInt(0.185f * rate);
            int d3 = Mathf.RoundToInt(0.290f * rate);

            for (int i = 0; i < total; i++)
            {
                float s = mix[i]
                        + 0.20f * mix[(i - d1 + total) % total]
                        + 0.12f * mix[(i - d2 + total) % total]
                        + 0.07f * mix[(i - d3 + total) % total];
                wet[i] = s;
            }

            float lp = wet[total - 1];
            for (int i = 0; i < total; i++)
            {
                // Gentle warm low-pass smoothing
                lp = lp * 0.18f + wet[i] * 0.82f;
                // Soft tape-style saturation so peaks never clip harshly
                float x = lp * 0.92f;
                float saturated = x / (1f + 0.35f * Mathf.Abs(x));
                mix[i] = Mathf.Clamp(saturated, -0.92f, 0.92f);
            }
        }

        /// <summary>Short, crystal-clear bell chime played when a tile snaps into its exact home position.</summary>
        private static AudioClip CrispChime(string name, float[] notes, float secondsPerNote)
        {
            const int rate = 44100;
            int samplesPerNote = Mathf.RoundToInt(rate * secondsPerNote);
            var samples = new float[samplesPerNote * notes.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                int noteIdx = i / samplesPerNote;
                int sampleInNote = i % samplesPerNote;
                float t = sampleInNote / (float)rate;
                float progress = sampleInNote / (float)samplesPerNote;
                float env = Mathf.Clamp01(progress / 0.05f) * Mathf.Exp(-3.2f * progress);
                float freq = notes[noteIdx];
                float phase = 2f * Mathf.PI * freq * t;
                // Fundamental + crystal 2x and 3x bell overtones
                float wave = Mathf.Sin(phase) + 0.35f * Mathf.Sin(2f * phase) + 0.15f * Mathf.Sin(3f * phase);
                samples[i] = Mathf.Clamp(wave * env * 0.65f, -0.95f, 0.95f);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CrispBell(string name, float[] notes, float secondsPerNote) =>
            CrispChime(name, notes, secondsPerNote);

        private static AudioClip Tone(string name, float[] notes, float secondsPerNote)
        {
            const int rate = 44100;
            int samplesPerNote = Mathf.RoundToInt(rate * secondsPerNote);
            var samples = new float[samplesPerNote * notes.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (i % samplesPerNote) / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * t / secondsPerNote);
                samples[i] = Mathf.Sin(2 * Mathf.PI * notes[i / samplesPerNote] * t) * envelope * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (settings != null) settings.Changed -= Apply;
            if (board != null)
            {
                board.StateChanged -= OnBoardChanged;
                board.TilePlacedCorrectly -= OnTilePlacedCorrectly;
                board.PuzzleSolved -= OnSolved;
            }
            if (generatedTitleMusic != null) Destroy(generatedTitleMusic);
            if (generatedLobbyMusic != null) Destroy(generatedLobbyMusic);
            if (generatedInGameMusic != null) Destroy(generatedInGameMusic);
            if (generatedGameStart != null) Destroy(generatedGameStart);
            if (generatedMove != null) Destroy(generatedMove);
            if (generatedTileCorrect != null) Destroy(generatedTileCorrect);
            if (generatedClear != null) Destroy(generatedClear);
        }
    }
}
