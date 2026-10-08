using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    public sealed class NailongCombatEffects : MonoBehaviour
    {
        readonly List<NailongSpitProjectile> projectiles = new List<NailongSpitProjectile>();
        readonly List<NailongGroundSlam> sonars = new List<NailongGroundSlam>();
        NailongAttackMotion motion;
        Material salivaMaterial;
        AudioSource voice;
        AudioClip roarClip;
        AudioClip scoldClip;
        AudioClip spitClip;
        readonly RaycastHit[] groundHits = new RaycastHit[32];
        public int ActiveProjectileCount
        {
            get { int count=0;foreach(var projectile in projectiles)if(projectile!=null)count++;return count; }
        }

        void Awake()
        {
            motion = GetComponent<NailongAttackMotion>();
            if(GetComponent<BossWeaponTrails>()==null)gameObject.AddComponent<BossWeaponTrails>();
            salivaMaterial = MakeMaterial("Nailong Saliva", new Color(0.46f, 0.52f, 0.39f));
            salivaMaterial.SetFloat("_Smoothness",.85f);
            if (!GameAudioPolicy.SoundEnabled) return;
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
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
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

        public void ShowTaunt(float seconds) { if(voice != null) voice.PlayOneShot(scoldClip); }
        public void HideTaunt() { }
        public void Roar(float radius) { if(voice != null) voice.PlayOneShot(roarClip); }
        public void GroundSlam(NailongAttack source,float radius,int pulseCount=3,float interval=1f,float travelSeconds=2.4f,float width=.4f)
        {
            var point = NailongSize.Feet(transform) + transform.forward * (.6f * NailongSize.RangeFactor(transform));
            int count = Physics.RaycastNonAlloc(point + Vector3.up * 4f, Vector3.down, groundHits, 8f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<IDamageable>() != null || hit.distance >= nearest) continue;
                nearest = hit.distance; point.y = hit.point.y;
            }
            var go=new GameObject("Nailong jumpable sonar");go.transform.position=point;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);
            var sonar=go.AddComponent<NailongGroundSlam>();sonar.Initialize(source,radius,pulseCount,interval,travelSeconds,width);
            sonars.RemoveAll(s=>s==null);sonars.Add(sonar);
        }

        public NailongSpitProjectile Spit(Transform target, float speed, float damage)
        {
            if (target == null || salivaMaterial == null) return null;
            Vector3 mouth = motion != null ? motion.MouthPosition : transform.TransformPoint(Vector3.up * 1.8f);
            var collider = target.GetComponentInParent<CharacterController>();
            Vector3 aim = collider != null ? collider.bounds.center : target.position + Vector3.up;
            float flight=Vector3.Distance(aim,mouth)/speed;
            Vector3 velocity = (aim-mouth)/Mathf.Max(.05f,flight)+Vector3.up*(3.5f*flight*.5f);
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Nailong Spit Projectile";
            obj.transform.position = mouth;
            float size = NailongSize.RangeFactor(transform);
            obj.transform.localScale = new Vector3(.18f,.18f,.32f) * size;
            obj.transform.rotation=Quaternion.LookRotation(velocity);
            var ownCollider = obj.GetComponent<Collider>();
            ownCollider.enabled = false;
            Destroy(ownCollider);
            obj.GetComponent<MeshRenderer>().sharedMaterial = salivaMaterial;
            var projectile = obj.AddComponent<NailongSpitProjectile>();
            projectile.Launch(transform, target, velocity, damage, .11f * size);
            projectiles.RemoveAll(p => p == null);
            projectiles.Add(projectile);
            if(voice != null) voice.PlayOneShot(spitClip, 0.5f);
            return projectile;
        }

        public void Cancel()
        {
            StopPresentation();
            foreach (var projectile in projectiles) if (projectile != null) Destroy(projectile.gameObject);
            projectiles.Clear();
            foreach(var sonar in sonars)if(sonar!=null)Destroy(sonar.gameObject);
            sonars.Clear();
        }

        public void StopPresentation()
        {
            HideTaunt();
            if (voice != null) voice.Stop();
        }

        void OnDisable() => Cancel();
        void OnDestroy()
        {
            Cancel();
            if (salivaMaterial != null) Destroy(salivaMaterial);
            if (roarClip != null) Destroy(roarClip);
            if (scoldClip != null) Destroy(scoldClip);
            if (spitClip != null) Destroy(spitClip);
        }
    }
}
