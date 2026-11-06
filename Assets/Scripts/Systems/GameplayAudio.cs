using UnityEngine;

namespace Mavis
{
    /// <summary>Preloaded gameplay cues and a bounded voice pool.</summary>
    public sealed class GameplayAudio : MonoBehaviour
    {
        public enum Cue { Swing, Hurt, Dodge, Gunshot, Reload, Pickup, Warning, Click }
        const int CueCount = (int)Cue.Click + 1;
        static GameplayAudio instance;
        readonly AudioClip[] clips = new AudioClip[CueCount];
        readonly AudioSource[] voices = new AudioSource[12];
        readonly float[] lastPlayed = new float[CueCount];
        readonly System.Random pitchVariation = new System.Random();
        int cursor;

        internal static AudioClip LoadClip(string name) => Resources.Load<AudioClip>("GameplayAudio/" + name);

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
                clips[k] = LoadClip(((Cue)k).ToString());
            }
        }
        public static void Play(Cue cue, Vector3 position, bool ui = false)
        {
            if (!GameAudioPolicy.SoundEnabled || !Application.isPlaying) return;
            Prewarm();
            int index = (int)cue;
            var clip = instance.clips[index];
            if (!clip) return;
            if (Time.unscaledTime - instance.lastPlayed[index] < (cue == Cue.Warning ? .3f : .035f)) return;
            instance.lastPlayed[index] = Time.unscaledTime;
            var source = instance.voices[instance.cursor];
            instance.cursor = (instance.cursor + 1) % instance.voices.Length;
            source.Stop(); source.transform.position = position; source.clip = clip;
            source.spatialBlend = ui ? 0 : 1; source.ignoreListenerPause = ui;
            source.volume = cue == Cue.Warning || cue == Cue.Gunshot ? .7f : .55f;
            source.pitch = cue == Cue.Gunshot || cue == Cue.Hurt || cue == Cue.Swing || cue == Cue.Dodge
                ? .96f + .08f * (float)instance.pitchVariation.NextDouble()
                : 1f;
            source.Play();
        }
        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
