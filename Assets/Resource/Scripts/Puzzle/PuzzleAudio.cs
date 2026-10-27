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

        private AudioSource music, effects;
        private AudioClip generatedTitleMusic, generatedLobbyMusic, generatedInGameMusic;
        private AudioClip generatedGameStart, generatedMove, generatedTileCorrect, generatedClear;
        private SettingsManager settings;
        private BoardManager board;
        private int previousMoves, previousUndo;
        private MusicContext currentContext = MusicContext.Title;

        public AudioClip TitleMusicClip => titleMusicClip;
        public AudioClip LobbyMusicClip => lobbyMusicClip;
        public AudioClip InGameMusicClip => musicClip;
        public AudioClip GameStartClip => gameStartClip;
        public AudioClip MoveClip => moveClip;
        public AudioClip TileCorrectClip => tileCorrectClip;
        public AudioClip ClearClip => clearClip;

        public AudioClip CurrentMusicClip => music != null ? music.clip : null;
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
            music.volume = 0.16f;
            effects.volume = 0.24f;

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

            AudioClip target = context switch
            {
                MusicContext.Title => titleMusicClip,
                MusicContext.Lobby => lobbyMusicClip,
                _ => musicClip
            };

            if (target == null) return;
            if (!forceRestart && music.clip == target && music.isPlaying) return;

            music.clip = target;
            music.loop = true;
            music.Play();
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

        /// <summary>Bright, welcoming casual fanfare loop for the Title Screen (120 BPM).</summary>
        private static AudioClip TitleScreenBgm(string name)
        {
            const int rate = 22050;
            const float stepSeconds = 0.125f; // 120 BPM sixteenth note
            int samplesPerStep = Mathf.RoundToInt(rate * stepSeconds);

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

            return RenderTwoVoiceLoop(name, rate, samplesPerStep, lead, bass, 0.36f, 0.22f);
        }

        /// <summary>Breezy, relaxed casual acoustic-style marimba/bell loop for the Stage Lobby (112 BPM).</summary>
        private static AudioClip LobbyBgm(string name)
        {
            const int rate = 22050;
            const float stepSeconds = 0.134f;
            int samplesPerStep = Mathf.RoundToInt(rate * stepSeconds);

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

            return RenderTwoVoiceLoop(name, rate, samplesPerStep, lead, bass, 0.32f, 0.20f);
        }

        private static AudioClip RenderTwoVoiceLoop(string name, int rate, int samplesPerStep, float[] lead, float[] bass, float leadGain, float bassGain)
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
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Synthesizes a bright, bouncy Puyo-Puyo style chiptune/arcade loop (150 BPM, 64 sixteenth-note steps)
        /// with a cheerful lead melody, harmony/arpeggio backing, and upbeat walking bassline.
        /// </summary>
        private static AudioClip UpbeatPuzzleBgm(string name)
        {
            const int rate = 22050;
            const float stepSeconds = 0.10f; // 150 BPM sixteenth note (0.4s per beat, 6.4s loop)
            int samplesPerStep = Mathf.RoundToInt(rate * stepSeconds);

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

                float leadFreq = lead[step];
                if (leadFreq > 0f)
                {
                    float phase = 2f * Mathf.PI * leadFreq * t;
                    float wave = Mathf.Sin(phase) + 0.32f * Mathf.Sin(3f * phase) + 0.18f * Mathf.Sin(2f * phase);
                    sample += wave * env * 0.42f;
                }

                float harmFreq = harmony[step];
                if (harmFreq > 0f)
                {
                    float phase = 2f * Mathf.PI * harmFreq * t;
                    float wave = Mathf.Sin(phase) + 0.25f * Mathf.Sin(2f * phase);
                    sample += wave * env * 0.22f;
                }

                float bassFreq = bass[step];
                if (bassFreq > 0f)
                {
                    float bassEnv = Mathf.Clamp01(progress / 0.05f) * (1f - 0.55f * progress);
                    float phase = 2f * Mathf.PI * bassFreq * t;
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

            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Short, crystal-clear bell chime played when a tile snaps into its exact home position.</summary>
        private static AudioClip CrispChime(string name, float[] notes, float secondsPerNote)
        {
            const int rate = 22050;
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
            const int rate = 22050;
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
