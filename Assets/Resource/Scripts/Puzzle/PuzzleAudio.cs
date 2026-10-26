using UnityEngine;

namespace FirstPuzzle
{
    /// <summary>Separate music/effects channels; optional authored clips replace the bright arcade synth defaults.</summary>
    public sealed class PuzzleAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip musicClip, moveClip, clearClip;
        private AudioSource music, effects;
        private AudioClip generatedMusic, generatedMove, generatedClear;
        private SettingsManager settings;
        private BoardManager board;
        private int previousMoves, previousUndo;

        public void Initialize(SettingsManager preferences, BoardManager puzzle)
        {
            settings = preferences;
            board = puzzle;
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = effects.playOnAwake = false;
            music.loop = true;
            music.volume = .16f;
            effects.volume = .22f;
            if (musicClip == null) musicClip = generatedMusic = UpbeatPuzzleBgm("Puzzle upbeat arcade bgm");
            if (moveClip == null) moveClip = generatedMove = Tone("Tile move", new[] { 783.99f, 987.77f }, .035f);
            if (clearClip == null) clearClip = generatedClear = Tone("Puzzle clear", new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, .13f);
            music.clip = musicClip;
            settings.Changed += Apply;
            board.StateChanged += OnBoardChanged;
            board.PuzzleSolved += OnSolved;
            Apply();
            music.Play();
        }

        private void Apply()
        {
            music.mute = !settings.Bgm;
            effects.mute = !settings.Sfx;
        }

        private void OnBoardChanged()
        {
            if (board.MoveCount > previousMoves || board.UndoUsed > previousUndo)
                effects.PlayOneShot(moveClip);
            previousMoves = board.MoveCount;
            previousUndo = board.UndoUsed;
        }

        private void OnSolved() => effects.PlayOneShot(clearClip);

        /// <summary>
        /// Synthesizes a bright, bouncy Puyo-Puyo style chiptune/arcade loop (150 BPM, 64 sixteenth-note steps)
        /// with a cheerful lead melody, harmony/arpeggio backing, and upbeat walking bassline.
        /// </summary>
        private static AudioClip UpbeatPuzzleBgm(string name)
        {
            const int rate = 22050;
            const float stepSeconds = 0.10f; // 150 BPM sixteenth note (0.4s per beat, 6.4s loop)
            int samplesPerStep = Mathf.RoundToInt(rate * stepSeconds);

            // Lead melody in C major / G mixolydian bounce (0f = rest)
            float[] lead =
            {
                // Bar 1 (C major bounce)
                523.25f, 0f, 659.25f, 0f, 783.99f, 0f, 659.25f, 0f,
                880.00f, 0f, 783.99f, 0f, 659.25f, 783.99f, 1046.50f, 0f,
                // Bar 2 (F -> G playful turnaround)
                880.00f, 0f, 698.46f, 0f, 783.99f, 0f, 587.33f, 0f,
                659.25f, 587.33f, 523.25f, 587.33f, 659.25f, 0f, 783.99f, 0f,
                // Bar 3 (Upbeat call & response)
                1046.50f, 0f, 987.77f, 0f, 880.00f, 0f, 783.99f, 0f,
                698.46f, 783.99f, 880.00f, 0f, 783.99f, 659.25f, 523.25f, 0f,
                // Bar 4 (Dm7 -> G7 -> C bright cadence)
                587.33f, 659.25f, 698.46f, 0f, 783.99f, 880.00f, 987.77f, 0f,
                1046.50f, 0f, 783.99f, 659.25f, 1046.50f, 0f, 0f, 0f
            };

            // Light staccato harmony / chordal off-beats
            float[] harmony =
            {
                // Bar 1
                0f, 329.63f, 0f, 392.00f, 0f, 523.25f, 0f, 392.00f,
                0f, 440.00f, 0f, 392.00f, 0f, 493.88f, 0f, 523.25f,
                // Bar 2
                0f, 440.00f, 0f, 349.23f, 0f, 392.00f, 0f, 293.66f,
                0f, 329.63f, 0f, 392.00f, 0f, 493.88f, 0f, 392.00f,
                // Bar 3
                0f, 523.25f, 0f, 493.88f, 0f, 440.00f, 0f, 392.00f,
                0f, 349.23f, 0f, 440.00f, 0f, 392.00f, 0f, 329.63f,
                // Bar 4
                0f, 349.23f, 0f, 440.00f, 0f, 493.88f, 0f, 587.33f,
                523.25f, 0f, 392.00f, 0f, 523.25f, 659.25f, 783.99f, 0f
            };

            // Bouncy octave-jumping bassline
            float[] bass =
            {
                // Bar 1 (C -> Am)
                130.81f, 261.63f, 130.81f, 261.63f, 164.81f, 329.63f, 196.00f, 392.00f,
                220.00f, 440.00f, 196.00f, 392.00f, 164.81f, 329.63f, 130.81f, 261.63f,
                // Bar 2 (F -> G)
                174.61f, 349.23f, 174.61f, 349.23f, 196.00f, 392.00f, 196.00f, 392.00f,
                130.81f, 261.63f, 146.83f, 293.66f, 164.81f, 329.63f, 196.00f, 392.00f,
                // Bar 3 (C -> Em -> F -> C)
                130.81f, 261.63f, 164.81f, 329.63f, 220.00f, 440.00f, 196.00f, 392.00f,
                174.61f, 349.23f, 174.61f, 349.23f, 130.81f, 261.63f, 164.81f, 329.63f,
                // Bar 4 (Dm -> G7 -> C)
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

                // Snappy attack + bouncy staccato decay envelope
                float env = Mathf.Clamp01(progress / 0.08f) * Mathf.Pow(1f - progress, 0.65f);
                float sample = 0f;

                float leadFreq = lead[step];
                if (leadFreq > 0f)
                {
                    // Bright square-like retro wave (fundamental + 3rd harmonic + octave shimmer)
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

                // Light hi-hat tick on off-beats for arcade rhythm
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
                board.PuzzleSolved -= OnSolved;
            }
            if (generatedMusic != null) Destroy(generatedMusic);
            if (generatedMove != null) Destroy(generatedMove);
            if (generatedClear != null) Destroy(generatedClear);
        }
    }
}
