using UnityEngine;

namespace Mavis
{
    public enum CombatHitKind { ComboOne, ComboTwo, ComboFinisher, ChargedHeavy, JumpSlash, Boomerang }

    public sealed class CombatHitFeedback : MonoBehaviour
    {
        public CombatHitKind LastHitKind { get; private set; }
        public int HitCount { get; private set; }
        readonly AudioClip[] clips = new AudioClip[6];
        AudioSource source;
        Camera viewer;
        CombatCameraShake cameraShake;
        int soundFrame = -1;

        public static bool Apply(GameObject attacker, IDamageable target, float damage, Vector3 point, CombatHitKind kind)
        {
            if (target == null || damage <= 0f) return false;
            if (target is Health genericHealth && genericHealth.IsDead) return false;
            if (target is NailongHealth boss)
            {
                if (boss.IsDead) return false;
                boss.ApplyCombatDamage(damage, point, kind);
            }
            else target.ApplyDamage(damage, point);
            var feedback = attacker.GetComponent<CombatHitFeedback>();
            if (feedback == null) feedback = attacker.AddComponent<CombatHitFeedback>();
            feedback.Emit(point, kind);
            return true;
        }

        void Awake()
        {
            // Silent gameplay must not synthesize six clips on the first hit.
            if (!GameAudioPolicy.SoundEnabled) return;
            var audioObject = new GameObject("Wood impact audio");
            audioObject.transform.SetParent(transform, false);
            source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.65f;
            source.minDistance = 4f;
            source.maxDistance = 35f;
            for (int k = 0; k < clips.Length; k++)
            {
                float length = k == 3 ? 0.24f : k == 4 ? 0.21f : k == 5 ? 0.12f : 0.11f + k * 0.02f;
                var pcm = new float[Mathf.CeilToInt(length * 44100)];
                var random = new System.Random(720 + k);
                float frequency = k == 3 ? 85f : k == 4 ? 105f : k == 5 ? 180f : 210f - k * 35f;
                for (int i = 0; i < pcm.Length; i++)
                {
                    float t = i / 44100f;
                    float noise = (float)random.NextDouble() * 2f - 1f;
                    pcm[i] = Mathf.Clamp((Mathf.Sin(t * frequency * 6.28318f) * Mathf.Exp(-t * 28f)
                        + noise * 0.7f * Mathf.Exp(-t * 75f)) * 0.48f * Mathf.Min(1f, t * 1800f), -1f, 1f);
                }
                clips[k] = AudioClip.Create("Wood impact " + (CombatHitKind)k, pcm.Length, 1, 44100, false);
                clips[k].SetData(pcm, 0);
            }
        }

        void Start() => BindCamera();
        void BindCamera()
        {
            viewer=Camera.main;
            if(!viewer){cameraShake=null;return;}
            cameraShake=viewer.GetComponent<CombatCameraShake>();
            if(!cameraShake)cameraShake=viewer.gameObject.AddComponent<CombatCameraShake>();
        }

        void Emit(Vector3 point, CombatHitKind kind)
        {
            LastHitKind = kind;
            HitCount++;
            // An area hit can damage several enemies, but should not multiply loudness or camera motion.
            if (source != null && soundFrame != Time.frameCount)
            {
                soundFrame = Time.frameCount;
                source.transform.position = point;
                source.PlayOneShot(clips[(int)kind], kind == CombatHitKind.ChargedHeavy ? 0.85f : 0.65f);
            }
            if(viewer!=Camera.main||!cameraShake)BindCamera();
            if(!cameraShake)return;
            float strength = kind == CombatHitKind.ChargedHeavy ? 0.065f : kind == CombatHitKind.JumpSlash ? 0.055f
                : kind == CombatHitKind.ComboFinisher ? 0.038f : kind == CombatHitKind.ComboTwo ? 0.024f : 0.016f;
            cameraShake.Pulse(strength, kind == CombatHitKind.ChargedHeavy || kind == CombatHitKind.JumpSlash ? 0.18f : 0.11f);
        }

        void OnDestroy()
        {
            foreach (var clip in clips) if (clip != null) Destroy(clip);
            if (source != null) Destroy(source.gameObject);
        }
    }
}
