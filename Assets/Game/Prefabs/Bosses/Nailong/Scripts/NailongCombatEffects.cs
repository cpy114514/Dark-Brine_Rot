using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    [DefaultExecutionOrder(500)]
    public sealed class NailongCombatEffects : MonoBehaviour
    {
        readonly List<NailongSpitProjectile> projectiles = new List<NailongSpitProjectile>();
        readonly List<NailongGroundSlam> sonars = new List<NailongGroundSlam>();
        NailongAttackMotion motion;
        NailongAI ai;
        NailongAttack attack;
        readonly BossTargetPrediction standalonePrediction = new BossTargetPrediction();
        Material salivaMaterial;
        [Header("Spit trajectory")]
        [SerializeField, Min(1f)] float spitGravity = 18f;
        [SerializeField, Min(.1f)] float minimumArcRise = .65f;
        [Tooltip("Lengthens flight while preserving the arc. Gravity and launch speed are scaled together.")]
        [SerializeField, Range(.5f,3f)] float spitFlightTimeScale=1.4f;
        [Tooltip("Glob radius before boss size scaling. Visible water and swept contact share this radius.")]
        [SerializeField, Min(.02f)] float spitRadius = .24f;
        bool spitQueued;
        Transform queuedTarget;
        float queuedSpeed, queuedDamage;
        AudioSource voice;
        AudioClip roarClip;
        AudioClip scoldClip;
        AudioClip spitClip;
        public int ActiveProjectileCount
        {
            get { int count=0;foreach(var projectile in projectiles)if(projectile!=null && projectile.gameObject.activeSelf)count++;return count; }
        }

        void Awake()
        {
            motion = GetComponent<NailongAttackMotion>();
            ai = GetComponent<NailongAI>();
            attack = GetComponent<NailongAttack>();
            if(GetComponent<BossWeaponTrails>()==null)gameObject.AddComponent<BossWeaponTrails>();
            var water = Resources.Load<Material>("BossCombat/NailongSaliva");
            var shader = Shader.Find("DarkBrine/Nailong Water Droplet");
            salivaMaterial = water != null ? new Material(water) : shader != null ? new Material(shader) : null;
            if (salivaMaterial != null)
            {
                salivaMaterial.name = "Nailong water droplets";
                salivaMaterial.SetFloat("_UseRefraction", SceneColourAvailable ? 1 : 0);
                CreateProjectile(); CreateProjectile();
            }
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

        public static bool SceneColourAvailable
        {
            get
            {
                var camera = Camera.main;
                if (camera != null && camera.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var data))
                    return data.requiresColorTexture;
                var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                return asset != null && asset.supportsCameraOpaqueTexture;
            }
        }

        public static bool SceneDepthAvailable
        {
            get
            {
                var camera = Camera.main;
                if (camera != null && camera.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var data))
                    return data.requiresDepthTexture;
                var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                return asset != null && asset.supportsCameraDepthTexture;
            }
        }

        NailongSpitProjectile CreateProjectile()
        {
            var obj = new GameObject("Nailong pooled water glob");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
            var projectile = obj.AddComponent<NailongSpitProjectile>();
            projectile.PrepareVisuals(salivaMaterial);
            obj.SetActive(false); projectiles.Add(projectile);
            return projectile;
        }

        public void QueueSpit(Transform target, float speed, float damage)
        {
            if (spitQueued || target == null) return;
            spitQueued = true; queuedTarget = target; queuedSpeed = speed; queuedDamage = damage;
        }

        void LateUpdate()
        {
            if (!spitQueued || Time.deltaTime <= 0 || PauseSettingsMenu.IsOpen) return;
            spitQueued = false;
            // The Animator and attack pose have now placed the mouth for this frame.
            Spit(queuedTarget, queuedSpeed, queuedDamage); queuedTarget = null;
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
        public void Roar(float radius)
        {
            if(voice != null) voice.PlayOneShot(roarClip);
            if(attack != null)CreateVoice(attack,radius,1,1,.7f,.65f,false,attack.damage*(ai?ai.roarDamageMultiplier:.7f),ai?ai.roarPushDistance:0,false);
        }
        public void GroundSlam(NailongAttack source,float radius,int pulseCount=3,float interval=1f,float travelSeconds=2.4f,float width=.4f,bool visualOnly=false)
        {
            // Keep the legacy entry point used by existing AI/scene data; sonic pressure now leaves the mouth.
            CreateVoice(source,radius,pulseCount,interval,travelSeconds,width,visualOnly,ai?ai.sonarDamage:4f,0,true);
        }
        void CreateVoice(NailongAttack source,float radius,int pulseCount,float interval,float travelSeconds,float width,bool visualOnly,float damage,float push,bool jumpable)
        {
            var go=new GameObject("Nailong mouth-emitted sound rings");go.transform.position=motion?motion.MouthPosition:transform.position;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);
            var sonar=go.AddComponent<NailongGroundSlam>();
            sonar.InitializeVoice(source,motion,ai?ai.VoiceTarget:null,radius,pulseCount,interval,travelSeconds,width,visualOnly,damage,push,jumpable);
            sonars.RemoveAll(s=>s==null);sonars.Add(sonar);
        }

        public NailongSpitProjectile Spit(Transform target, float speed, float damage,float yawOffset=0)
        {
            if (target == null || salivaMaterial == null) return null;
            Vector3 mouth = motion != null ? motion.MouthPosition : transform.TransformPoint(Vector3.up * 1.8f);
            var prediction = ai ? ai.TargetPrediction : standalonePrediction;
            prediction.Observe(target);
            float size = NailongSize.RangeFactor(transform);
            float durationScale=Mathf.Max(.5f,spitFlightTimeScale);
            float flightGravity=Mathf.Max(1,spitGravity/(durationScale*durationScale));
            float flightSpeed=speed/durationScale;
            Vector3 aim = prediction.BodyAfter(.6f, transform);
            float flight = FlightSeconds(mouth, aim, flightSpeed, flightGravity, minimumArcRise * size);
            aim = prediction.BodyAfter(flight, transform);
            flight = FlightSeconds(mouth, aim, flightSpeed, flightGravity, minimumArcRise * size);
            Vector3 velocity = (aim-mouth)/flight+Vector3.up*(flightGravity*flight*.5f);
            velocity=Quaternion.AngleAxis(yawOffset,Vector3.up)*velocity;
            NailongSpitProjectile projectile = null;
            foreach (var item in projectiles)
                if (item != null && !item.gameObject.activeSelf) { projectile = item; break; }
            if (projectile == null && projectiles.Count < 4) projectile = CreateProjectile();
            if (projectile == null) return null;
            salivaMaterial.SetFloat("_UseRefraction", SceneColourAvailable ? 1 : 0);
            projectile.transform.SetPositionAndRotation(mouth, Quaternion.LookRotation(velocity));
            projectile.gameObject.SetActive(true);
            projectile.Launch(transform, target, velocity, damage, spitRadius * size, flightGravity);
            if(voice != null) voice.PlayOneShot(spitClip, 0.5f);
            return projectile;
        }

        public static float FlightSeconds(Vector3 from, Vector3 to, float speed, float gravity, float minimumRise)
        {
            gravity = Mathf.Max(1, gravity); minimumRise = Mathf.Max(.1f, minimumRise);
            float apex = Mathf.Max(from.y, to.y) + minimumRise;
            float arcTime = Mathf.Sqrt(2 * (apex-from.y) / gravity) + Mathf.Sqrt(2 * (apex-to.y) / gravity);
            return Mathf.Max(.25f, arcTime, Vector3.ProjectOnPlane(to-from, Vector3.up).magnitude / Mathf.Max(1,speed));
        }

        public void Cancel()
        {
            StopPresentation();
            foreach (var projectile in projectiles) if (projectile != null) projectile.Recycle();
            foreach(var sonar in sonars)if(sonar!=null)Destroy(sonar.gameObject);
            sonars.Clear();
        }

        public void StopPresentation()
        {
            spitQueued = false; queuedTarget = null;
            foreach(var sonar in sonars)if(sonar&&sonar.FromMouth)sonar.StopEmission();
            HideTaunt();
            if (voice != null) voice.Stop();
        }

        void OnDisable() => Cancel();
        void OnDestroy()
        {
            Cancel();
            foreach (var projectile in projectiles) if (projectile != null) Destroy(projectile.gameObject);
            projectiles.Clear();
            if (salivaMaterial != null) Destroy(salivaMaterial);
            if (roarClip != null) Destroy(roarClip);
            if (scoldClip != null) Destroy(scoldClip);
            if (spitClip != null) Destroy(spitClip);
        }
    }
}
