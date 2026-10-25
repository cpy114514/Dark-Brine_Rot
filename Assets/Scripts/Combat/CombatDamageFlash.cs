using UnityEngine;
using System.Collections.Generic;

namespace Mavis
{
    // Brief material response using reused property blocks; never clones a
    // material per hit or moves the collision body to communicate damage.
    [DisallowMultipleComponent,DefaultExecutionOrder(750)]
    public sealed class CombatDamageFlash : MonoBehaviour
    {
        sealed class Surface
        {
            public Renderer renderer;public int slot,id;public Color rest,applied;
            public readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
            public readonly MaterialPropertyBlock saved=new MaterialPropertyBlock();
            public bool owns,hadColor;
        }
        readonly List<Surface> surfaces=new List<Surface>();
        float age=1,strength;
        const float Duration=.115f;
        public bool IsFlashing=>age<Duration;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),ColorId=Shader.PropertyToID("_Color");
        void Awake()
        {
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if(renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var material=materials[i];if(!material)continue;
                    int id=material.HasProperty(BaseColor)?BaseColor:material.HasProperty(ColorId)?ColorId:0;
                    if(id!=0)surfaces.Add(new Surface{renderer=renderer,slot=i,id=id,rest=material.GetColor(id)});
                }
            }
        }
        public void Hit(float amount)
        {
            if(!isActiveAndEnabled)return;
            strength=Mathf.Clamp01(amount);age=0;
            foreach(var s in surfaces)
            {
                if(!s.renderer)continue;
                s.renderer.GetPropertyBlock(s.block,s.slot);
                if(!s.owns){s.renderer.GetPropertyBlock(s.saved,s.slot);s.hadColor=s.saved.HasProperty(s.id);}
                if(s.block.HasProperty(s.id) && (!s.owns || !s.block.GetColor(s.id).Equals(s.applied)))s.rest=s.block.GetColor(s.id);
            }
        }
        void LateUpdate()
        {
            if(!IsFlashing)return;
            if(Time.timeScale<=0)return;
            age=Mathf.Min(Duration,age+Time.deltaTime);
            float t=1-age/Duration;
            foreach(var s in surfaces)
            {
                if(!s.renderer)continue;
                s.renderer.GetPropertyBlock(s.block,s.slot);
                if(s.owns && s.block.HasProperty(s.id) && !s.block.GetColor(s.id).Equals(s.applied)){s.rest=s.block.GetColor(s.id);s.hadColor=true;}
                if(t<=0){Restore(s);continue;}
                var tint=Color.Lerp(s.rest,new Color(1,.88f,.68f,s.rest.a),.48f*strength*t);
                s.applied=tint;s.block.SetColor(s.id,tint);s.renderer.SetPropertyBlock(s.block,s.slot);s.owns=t>0;
            }
        }
        static void Restore(Surface s)
        {
            if(s.block.GetColor(s.id).Equals(s.applied))
            {
                if(s.hadColor){s.block.SetColor(s.id,s.rest);s.renderer.SetPropertyBlock(s.block,s.slot);}
                else s.renderer.SetPropertyBlock(s.saved,s.slot);
            }
            s.owns=false;
        }
        void OnDisable()
        {
            foreach(var s in surfaces)
            {
                if(!s.owns||!s.renderer)continue;
                s.renderer.GetPropertyBlock(s.block,s.slot);
                Restore(s);
            }
            age=Duration;
        }
    }
}
