using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    public sealed class NailongCombatEffects : MonoBehaviour
    {
        readonly List<NailongSpitProjectile> projectiles = new List<NailongSpitProjectile>();
        NailongAttackMotion motion;
        Material salivaMaterial;
        Material waveMaterial;
        LineRenderer wave;
        float waveStarted;
        float waveRadius;
        float tauntEnds;
        TextMesh taunt;
        Font tauntFont;
        bool ownsTauntFont;
        AudioSource voice;
        AudioClip roarClip;
        AudioClip scoldClip;
        AudioClip spitClip;
        public int ActiveProjectileCount => projectiles.FindAll(p => p != null).Count;

        void Awake()
        {
            motion = GetComponent<NailongAttackMotion>();
            salivaMaterial = MakeMaterial("Nailong Saliva", new Color(0.65f, 0.95f, 0.3f));
            waveMaterial = MakeMaterial("Nailong Roar Wave", new Color(1f, 0.8f, 0.25f));
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 1f;
            voice.minDistance = 4f;
            voice.maxDistance = 35f;
            voice.volume = 0.55f;
            voice.playOnAwake = false;
            roarClip = MakeSound("Nailong Roar", 0.75f, 85f, 0.3f);
            scoldClip = MakeSound("Nailong Cartoon Grumbling", 0.8f, 220f, 0.08f);
            spitClip = MakeSound("Nailong Spit", 0.12f, 420f, 0.7f);
        }

        static Material MakeMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        static AudioClip MakeSound(string name, float seconds, float frequency, float noiseMix)
        {
            const int sampleRate = 22050;
            float[] samples = new float[Mathf.CeilToInt(seconds * sampleRate)];
            var random = new System.Random(41);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / samples.Length);
                float syllable = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(t * 24f));
                float tone = Mathf.Sin(t * frequency * Mathf.PI * 2f + Mathf.Sin(t * 32f) * 2f);
                samples[i] = (tone * (1f - noiseMix) + ((float)random.NextDouble() * 2f - 1f) * noiseMix) * envelope * syllable * 0.55f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void ShowTaunt(float seconds)
        {
            if (taunt == null)
            {
                var obj = new GameObject("Nailong Taunt");
                obj.transform.SetParent(transform, false);
                taunt = obj.AddComponent<TextMesh>();
                tauntFont = GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ownsTauntFont = false;
                taunt.font = tauntFont;
                taunt.GetComponent<MeshRenderer>().sharedMaterial = tauntFont.material;
                taunt.text = GameLocalization.Text("nailong.taunt");
                taunt.fontSize = 40;
                taunt.characterSize = 0.06f;
                taunt.anchor = TextAnchor.MiddleCenter;
                taunt.color = GameUITheme.Foreground;
            }
            taunt.gameObject.SetActive(true);
            taunt.text = GameLocalization.Text("nailong.taunt");
            tauntEnds = Time.time + seconds;
            voice.PlayOneShot(scoldClip);
        }

        public void HideTaunt() { if (taunt != null) taunt.gameObject.SetActive(false); }

        public void Roar(float radius)
        {
            if (wave == null)
            {
                var obj = new GameObject("Nailong Roar Shockwave");
                obj.transform.SetParent(transform, false);
                wave = obj.AddComponent<LineRenderer>();
                wave.positionCount = 65;
                wave.loop = true;
                wave.useWorldSpace = true;
                wave.widthMultiplier = 0.14f;
                wave.sharedMaterial = waveMaterial;
                wave.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            waveStarted = Time.time;
            waveRadius = radius;
            wave.enabled = true;
            voice.PlayOneShot(roarClip);
        }

        public NailongSpitProjectile Spit(Transform target, float speed, float damage)
        {
            if (target == null || salivaMaterial == null) return null;
            Vector3 mouth = motion != null ? motion.MouthPosition : transform.position + Vector3.up * 1.8f;
            var collider = target.GetComponentInParent<CharacterController>();
            Vector3 aim = collider != null ? collider.bounds.center : target.position + Vector3.up;
            Vector3 velocity = (aim - mouth).normalized * speed;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Nailong Spit Projectile";
            obj.transform.position = mouth;
            obj.transform.localScale = Vector3.one * 0.22f;
            var ownCollider = obj.GetComponent<Collider>();
            ownCollider.enabled = false;
            Destroy(ownCollider);
            obj.GetComponent<MeshRenderer>().sharedMaterial = salivaMaterial;
            var trail = obj.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.startWidth = 0.14f;
            trail.endWidth = 0.02f;
            trail.sharedMaterial = salivaMaterial;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var projectile = obj.AddComponent<NailongSpitProjectile>();
            projectile.Launch(transform, target, velocity, damage);
            projectiles.RemoveAll(p => p == null);
            projectiles.Add(projectile);
            voice.PlayOneShot(spitClip, 0.5f);
            return projectile;
        }

        void LateUpdate()
        {
            if (taunt != null && taunt.gameObject.activeSelf)
            {
                taunt.text = GameLocalization.Text("nailong.taunt");
                if (Time.time > tauntEnds) HideTaunt();
                else
                {
                    taunt.transform.position = (motion != null ? motion.MouthPosition : transform.position + Vector3.up * 2f) + Vector3.up * 1.1f;
                    var camera = Camera.main;
                    if (camera != null) taunt.transform.rotation = camera.transform.rotation;
                }
            }
            if (wave == null || !wave.enabled) return;
            float progress = Mathf.Clamp01((Time.time - waveStarted) / 0.55f);
            if (progress >= 1f) { wave.enabled = false; return; }
            float radius = Mathf.Lerp(0.5f, waveRadius, progress);
            var capsule = GetComponent<CapsuleCollider>();
            Vector3 center = transform.position;
            center.y = capsule != null ? capsule.bounds.min.y + 0.1f : center.y + 0.1f;
            for (int i = 0; i < wave.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / (wave.positionCount - 1);
                wave.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
            wave.widthMultiplier = 0.16f * (1f - progress) + 0.015f;
        }

        public void Cancel()
        {
            StopPresentation();
            foreach (var projectile in projectiles) if (projectile != null) Destroy(projectile.gameObject);
            projectiles.Clear();
        }

        public void StopPresentation()
        {
            HideTaunt();
            if (wave != null) wave.enabled = false;
            if (voice != null) voice.Stop();
        }

        void OnDisable() => Cancel();
        void OnDestroy()
        {
            Cancel();
            if (salivaMaterial != null) Destroy(salivaMaterial);
            if (waveMaterial != null) Destroy(waveMaterial);
            if (roarClip != null) Destroy(roarClip);
            if (scoldClip != null) Destroy(scoldClip);
            if (spitClip != null) Destroy(spitClip);
            // Built-in fonts are shared assets, unlike dynamically created OS fonts.
            if (tauntFont != null && ownsTauntFont) Destroy(tauntFont);
        }
    }
}
