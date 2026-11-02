using UnityEngine;

namespace Mavis
{
    /// <summary>Short synthesized honks. The project has no licensed goose recording.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ChickenBroAI))]
    public sealed class BullyGooseVoice : MonoBehaviour
    {
        ChickenBroAI body;
        AudioSource source;
        AudioClip honk;
        AudioClip defeat;
        ChickenBroAI.Mood last;
        float nextIdle;

        void Awake()
        {
            body = GetComponent<ChickenBroAI>();
            source = GetComponent<AudioSource>();
            if (!source) source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.loop = false;
            source.maxDistance = 28f;
            source.minDistance = 2f;
            source.rolloffMode = AudioRolloffMode.Linear;
            honk = Tone(0.42f, 680f, 320f, 0.55f);
            defeat = Tone(0.7f, 420f, 140f, 0.4f);
            nextIdle = Time.time + Random.Range(6f, 12f);
        }

        void Update()
        {
            if (!body || !source || !honk) return;
            var mode = body.Mode;
            if (mode != last)
            {
                if (mode == ChickenBroAI.Mood.Alert) source.PlayOneShot(honk, 0.7f);
                else if (mode == ChickenBroAI.Mood.Lunge) source.PlayOneShot(honk, 0.85f);
                else if (mode == ChickenBroAI.Mood.Wing || mode == ChickenBroAI.Mood.Strike) source.PlayOneShot(honk, 0.35f);
                else if (mode == ChickenBroAI.Mood.Flinch) source.PlayOneShot(honk, 0.28f);
                else if (mode == ChickenBroAI.Mood.Dead) source.PlayOneShot(defeat, 0.6f);
                last = mode;
            }
            if ((mode == ChickenBroAI.Mood.Idle || mode == ChickenBroAI.Mood.Patrol) && Time.time >= nextIdle)
            {
                source.PlayOneShot(honk, 0.22f);
                nextIdle = Time.time + Random.Range(8f, 14f);
            }
        }

        static AudioClip Tone(float seconds, float start, float end, float gain)
        {
            const int rate = 22050;
            int count = Mathf.Max(1, Mathf.CeilToInt(rate * seconds));
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float u = t / Mathf.Max(0.01f, seconds);
                float envelope = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI) * Mathf.Clamp01(u / 0.04f);
                float freq = Mathf.Lerp(start, end, u);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.25f;
                samples[i] = wave * envelope * gain;
            }
            var clip = AudioClip.Create("GooseHonk", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
