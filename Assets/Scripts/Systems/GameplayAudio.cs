using UnityEngine;

namespace Mavis
{
    /// <summary>Small shared cue bank and bounded voice pool; no clip synthesis on combat frames.</summary>
    public sealed class GameplayAudio : MonoBehaviour
    {
        public enum Cue { Swing, Hurt, Dodge, Gunshot, Reload, Pickup, Warning, Click }
        static GameplayAudio instance;
        readonly AudioClip[] clips = new AudioClip[8];
        readonly AudioSource[] voices = new AudioSource[12];
        readonly float[] lastPlayed = new float[8];
        int cursor;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;
        public static void Prewarm()
        {
            if (instance || !Application.isPlaying) return;
            var go = new GameObject("Gameplay audio cues");
            DontDestroyOnLoad(go); instance = go.AddComponent<GameplayAudio>();
        }
        void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this;
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("Cue voice " + i); go.transform.SetParent(transform, false);
                voices[i] = go.AddComponent<AudioSource>(); voices[i].playOnAwake = false;
                voices[i].minDistance = 10; voices[i].maxDistance = 100; voices[i].dopplerLevel = 0;
                voices[i].rolloffMode = AudioRolloffMode.Linear;
            }
            for (int k = 0; k < clips.Length; k++)
            {
                lastPlayed[k] = -100;
                int count = Mathf.RoundToInt((k == (int)Cue.Warning ? .36f : .12f) * 22050);
                var samples = new float[count]; var random = new System.Random(912 + k);
                float frequency = k == 6 ? 660 : k == 5 ? 880 : k == 7 ? 540 : k == 3 ? 90 : k == 1 ? 125 : 260;
                for (int i = 0; i < count; i++)
                {
                    float t = i / 22050f, phase = (float)i / count;
                    float noise = (float)random.NextDouble() * 2 - 1;
                    float tonal = Mathf.Sin(t * frequency * (k == 5 ? 1 + phase : 1) * Mathf.PI * 2);
                    float value = k == 0 || k == 2 ? noise : k == 3 || k == 1 ? noise * .65f + tonal * .35f : tonal;
                    samples[i] = value * Mathf.Min(1, t * 200) * Mathf.Exp(-phase * 5) * .35f;
                }
                clips[k] = AudioClip.Create("Sahur " + (Cue)k, count, 1, 22050, false);
                clips[k].SetData(samples, 0);
            }
        }
        public static void Play(Cue cue, Vector3 position, bool ui = false)
        {
            if (!GameAudioPolicy.SoundEnabled || !Application.isPlaying) return;
            Prewarm();
            int index = (int)cue;
            if (Time.unscaledTime - instance.lastPlayed[index] < (cue == Cue.Warning ? .3f : .035f)) return;
            instance.lastPlayed[index] = Time.unscaledTime;
            var source = instance.voices[instance.cursor++ % instance.voices.Length];
            source.Stop(); source.transform.position = position; source.clip = instance.clips[index];
            source.spatialBlend = ui ? 0 : 1; source.ignoreListenerPause = ui;
            source.volume = cue == Cue.Warning ? .85f : cue == Cue.Gunshot ? .7f : .55f;
            source.Play();
        }
        void OnDestroy()
        {
            if (instance == this) instance = null;
            foreach (var clip in clips) if (clip) Destroy(clip);
        }
    }
}
