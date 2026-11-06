using UnityEngine;

namespace Mavis
{
    /// <summary>Original goose calls triggered by encounter states.</summary>
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
            if (!GameAudioPolicy.SoundEnabled) return;
            source = GetComponent<AudioSource>();
            if (!source) source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.loop = false;
            source.maxDistance = 28f;
            source.minDistance = 2f;
            source.rolloffMode = AudioRolloffMode.Linear;
            honk = GameplayAudio.LoadClip("GooseHonk");
            defeat = GameplayAudio.LoadClip("GooseDefeat");
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
                else if (mode == ChickenBroAI.Mood.Dead && defeat) source.PlayOneShot(defeat, 0.6f);
                last = mode;
            }
            if ((mode == ChickenBroAI.Mood.Idle || mode == ChickenBroAI.Mood.Patrol) && Time.time >= nextIdle)
            {
                source.PlayOneShot(honk, 0.22f);
                nextIdle = Time.time + Random.Range(8f, 14f);
            }
        }

    }
}
