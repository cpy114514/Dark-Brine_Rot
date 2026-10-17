using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    /// <summary>Small reusable emitters using the licensed Kenney phase textures.</summary>
    public sealed class BossPhaseVfx : MonoBehaviour
    {
        ParticleSystem dust, fire;
        Material dustMaterial, fireMaterial;
        public static BossPhaseVfx For(GameObject owner) => owner.GetComponent<BossPhaseVfx>() ?? owner.AddComponent<BossPhaseVfx>();
        public void Burst(Vector3 point, bool hot=false)
        {
            var emitter=hot ? fire : dust;
            if(emitter==null)
            {
                var source=Resources.Load<Material>("BossPhases/"+(hot?"PhaseFire":"PhaseDust"));
                if(source==null)return;
                var material=new Material(source){name="Boss phase CC0 particles"};
                emitter=NaturalParticleEffects.Emitter(transform,hot?"Phase fire burst":"Phase dust burst",material,48,hot?-.05f:.1f);
                var main=emitter.main;main.startLifetime=new ParticleSystem.MinMaxCurve(.3f,.7f);
                main.startSize=new ParticleSystem.MinMaxCurve(.3f,1f);
                if(hot){fire=emitter;fireMaterial=material;}else{dust=emitter;dustMaterial=material;}
            }
            emitter.Play();
            int count=NaturalParticleEffects.BurstBudget(point,18);
            for(int i=0;i<count;i++)
            {
                var direction=Random.insideUnitSphere;direction.y=Mathf.Abs(direction.y);
                emitter.Emit(new ParticleSystem.EmitParams{position=point,velocity=direction*Random.Range(2f,5f),
                    startColor=hot?new Color(1,.6f,.15f,.8f):new Color(.75f,.7f,.45f,.6f)},1);
            }
        }
        void OnDisable(){if(dust)dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(fire)fire.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void OnDestroy(){if(dustMaterial)Destroy(dustMaterial);if(fireMaterial)Destroy(fireMaterial);}
    }
}
