using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    // Small, bounded emitters; cosmetic quality never changes damage or AI timing.
    public static class BossCombatVfx
    {
        const int ImpactCapacity=16;
        static readonly System.Collections.Generic.List<BossImpactEmitter> impacts =
            new System.Collections.Generic.List<BossImpactEmitter>();

        public static int ImpactEmitterCount => impacts.Count;
        public static void PrewarmImpacts()
        {
            if(!Application.isPlaying)return;
            impacts.RemoveAll(p=>p==null);
            if(impacts.Count>0)return;
            CreateImpact(false);CreateImpact(false);CreateImpact(true);CreateImpact(true);
        }
        static BossImpactEmitter CreateImpact(bool hot)
        {
            var go=new GameObject(hot?"Reusable hot impact":"Reusable combat dust");
            var emitter=go.AddComponent<BossImpactEmitter>();emitter.Initialize(hot);
            impacts.Add(emitter);return emitter;
        }
        public static Material FlameMaterial()
        {
            var template = Resources.Load<Material>("BossCombat/Flame");
            if(template!=null)return new Material(template);
            var shader=Shader.Find("DarkBrine/Boss Flame") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            return new Material(shader);
        }
        public static ParticleSystem Flames(Transform parent, string name, int budget, float rate, float size, float lifetime, float speed)
        {
            var mat = FlameMaterial();
            var p = NaturalParticleEffects.Emitter(parent, name, mat, budget, -.03f);
            p.gameObject.AddComponent<BossVfxMaterialOwner>().material = mat;
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main; main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * .7f, lifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(size * .5f, size); main.startSpeed = speed;
            main.startRotation = new ParticleSystem.MinMaxCurve(-.18f, .18f);
            main.startColor = Color.white;
            var emission = p.emission; emission.enabled = true; emission.rateOverTime = rate;
            var shape = p.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12; shape.radius = .15f;
            var color = p.colorOverLifetime; var g = new Gradient();
            g.SetKeys(new[]{new GradientColorKey(new Color(1,.85f,.48f),0),new GradientColorKey(new Color(1,.35f,.05f),.45f),new GradientColorKey(new Color(.4f,.09f,.015f),1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.85f,.08f),new GradientAlphaKey(.65f,.4f),new GradientAlphaKey(0,1)});
            color.color = g;
            var sizeLife = p.sizeOverLifetime; sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0,.45f,1,1.5f));
            var noise = p.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = .8f; noise.scrollSpeed = 1.2f;
            p.Play(); return p;
        }
        public static void Burst(Vector3 point, Vector3 normal, Color tint, int count, float spread, bool hot = false)
        {
            count = NaturalParticleEffects.BurstBudget(point,count); if (count == 0) return;
            BossImpactEmitter emitter=null;
            for(int i=impacts.Count-1;i>=0;i--)
            {
                var candidate=impacts[i];
                if(candidate==null){impacts.RemoveAt(i);continue;}
                if(candidate.Hot==hot && candidate.Available)emitter=candidate;
            }
            if(emitter==null && impacts.Count<ImpactCapacity)emitter=CreateImpact(hot);
            if(emitter!=null)emitter.Emit(point,normal,tint,Mathf.Min(count,64),spread);
        }
        public static void SmokeTrail(Transform parent, float rate, float size, int budget)
        {
            var mat=NaturalParticleEffects.SmokeMaterial("Exhaust smoke",new Color(.56f,.55f,.52f,.48f));
            var p=NaturalParticleEffects.Emitter(parent,"Smoke exhaust",mat,budget,-.035f);
            p.gameObject.AddComponent<BossVfxMaterialOwner>().material=mat;
            var main=p.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.1f);main.startSpeed=.2f;
            main.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            var emission=p.emission;emission.enabled=true;emission.rateOverTime=0;emission.rateOverDistance=rate;
            var sizeLife=p.sizeOverLifetime;sizeLife.enabled=true;sizeLife.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.4f,1,2));
        }
        public static void DetachTrails(Transform root)
        {
            foreach(var p in root.GetComponentsInChildren<ParticleSystem>())
            {
                if(p.main.simulationSpace!=ParticleSystemSimulationSpace.World)continue;
                p.Stop(true,ParticleSystemStopBehavior.StopEmitting);p.transform.SetParent(null,true);
                p.gameObject.AddComponent<BossVfxLifetime>().seconds=1.5f;
            }
        }
    }
    public sealed class BossImpactEmitter : MonoBehaviour
    {
        public bool Hot {get;private set;}
        public bool Available=>remaining<=0f;
        ParticleSystem particles;
        float remaining;
        public void Initialize(bool hot)
        {
            Hot=hot;
            var mat=hot?BossCombatVfx.FlameMaterial():NaturalParticleEffects.SmokeMaterial("Pooled combat dust",Color.white);
            particles=NaturalParticleEffects.Emitter(transform,"Burst",mat,64,.12f);
            particles.gameObject.AddComponent<BossVfxMaterialOwner>().material=mat;
            var lifetime=particles.sizeOverLifetime;lifetime.enabled=true;
            lifetime.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.45f,1,1.8f));
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void Emit(Vector3 point,Vector3 normal,Color tint,int count,float spread)
        {
            transform.position=point;particles.Clear();particles.Play();remaining=1.3f;
            for(int i=0;i<count;i++)
            {
                Vector3 direction=(Random.insideUnitSphere+normal*.8f).normalized;
                particles.Emit(new ParticleSystem.EmitParams {position=point+direction*.12f,
                    velocity=direction*Random.Range(1,spread),startSize=Random.Range(.15f,.4f)*spread*.3f,
                    startLifetime=Random.Range(.3f,Hot?.65f:1.1f),startColor=tint,rotation=Random.Range(0,Mathf.PI*2)},1);
            }
        }
        void Update()
        {
            if(PauseSettingsMenu.IsOpen||remaining<=0)return;
            remaining-=Time.deltaTime;
            if(remaining<=0)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    public sealed class BossVfxMaterialOwner : MonoBehaviour
    {
        public Material material;
        bool paused;
        ParticleSystem particles;
        void Awake()=>particles=GetComponent<ParticleSystem>();
        void Update()
        {
            var p=particles;if(!p)return;
            if(PauseSettingsMenu.IsOpen&&!paused){p.Pause();paused=true;}
            else if(!PauseSettingsMenu.IsOpen&&paused){p.Play();paused=false;}
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
    public sealed class BossVfxLifetime : MonoBehaviour
    {
        public float seconds=1.5f;
        void Update(){if(!PauseSettingsMenu.IsOpen && (seconds-=Time.deltaTime)<=0)Destroy(gameObject);}
    }
}
