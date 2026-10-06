using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    public sealed class BombardinoOrdnance : MonoBehaviour
    {
        public enum Kind { Bomb, Missile, Fire }
        public Kind Payload { get; private set; }
        BombardinoBoss owner;
        Transform target;
        Vector3 velocity, aim;
        float age, damage, radius, fireTick;
        bool resolved;
        Material material;
        Material flameMaterial;
        Material smokeMaterial;
        readonly RaycastHit[] hits=new RaycastHit[64];
        readonly Collider[] overlaps=new Collider[128];
        readonly Dictionary<PlayerHealth,float> playerDistances=new Dictionary<PlayerHealth,float>();
        readonly HashSet<BombardinoDestructible> structures=new HashSet<BombardinoDestructible>();

        public void Launch(BombardinoBoss source,Kind kind,Vector3 direction,Transform victim,Vector3 point,float amount,float blastRadius)
        {
            owner=source;owner.RegisterOrdnance(this);Payload=kind;target=victim;aim=point;damage=amount;radius=blastRadius;
            velocity=direction.normalized*(kind==Kind.Missile?32f:2f);
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor",kind==Kind.Missile?new Color(.46f,.48f,.45f):new Color(.15f,.17f,.18f));
            material.SetFloat("_Smoothness",.28f);
            var body=GameObject.CreatePrimitive(kind==Kind.Fire?PrimitiveType.Cylinder:PrimitiveType.Capsule);body.name=kind+" visible payload";body.transform.SetParent(transform,false);
            DestroyImmediate(body.GetComponent<Collider>());body.GetComponent<Renderer>().sharedMaterial=material;
            body.transform.localScale=kind==Kind.Fire?new Vector3(radius*2,.08f,radius*2):new Vector3(.35f,.65f,.35f);
            if(kind==Kind.Fire)
            {
                // The fire itself identifies the hazard; hide the flat coloured area disk.
                body.GetComponent<Renderer>().enabled=false;
                flameMaterial=new Material(source.effectMaterial);flameMaterial.SetFloat("_Flame",1);
                for(int i=0;i<7;i++){var flame=GameObject.CreatePrimitive(PrimitiveType.Quad);flame.name="Napalm flame";DestroyImmediate(flame.GetComponent<Collider>());flame.transform.SetParent(transform,false);float a=i*2.4f+Random.Range(-.35f,.35f);float height=Random.Range(.8f,1.7f);flame.transform.localPosition=new Vector3(Mathf.Sin(a)*radius*Random.Range(.15f,.65f),height*.5f,Mathf.Cos(a)*radius*Random.Range(.15f,.65f));flame.transform.localScale=new Vector3(Random.Range(.65f,1.1f),height,1);flame.GetComponent<Renderer>().sharedMaterial=flameMaterial;}
            }
            if(kind==Kind.Missile)
            {
                smokeMaterial=NaturalParticleEffects.Material("Missile exhaust",new Color(.52f,.51f,.48f,.28f));
                var smoke=NaturalParticleEffects.Emitter(transform,"Missile exhaust smoke",smokeMaterial,40,-.035f);
                smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                smoke.transform.localPosition=Vector3.down*.5f;smoke.transform.localRotation=Quaternion.LookRotation(Vector3.down);
                var main=smoke.main;main.loop=true;main.startSpeed=.8f;main.startSize=new ParticleSystem.MinMaxCurve(.08f,.18f);main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.65f);
                var emission=smoke.emission;emission.enabled=true;emission.rateOverTime=14;
                smoke.Play();
            }
        }
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if(resolved||PauseSettingsMenu.IsOpen||deltaTime<=0)return;
            if(owner==null||!owner.isActiveAndEnabled||owner.IsDead){Expire();return;}
            float dt=Mathf.Min(deltaTime,.1f);age+=dt;
            if(Payload==Kind.Fire)
            {
                transform.GetChild(0).localScale=new Vector3(radius*2,.08f+Mathf.Sin(age*11)*.035f,radius*2);
                if(flameMaterial!=null){flameMaterial.SetFloat("_Clock",age);var camera=Camera.main;if(camera!=null)for(int i=1;i<transform.childCount;i++)transform.GetChild(i).rotation=camera.transform.rotation;}
                if(age>=fireTick){fireTick=age+.65f;Blast(damage,false);}if(age>=5f)Expire();return;
            }
            if(Payload==Kind.Bomb)velocity+=Vector3.down*25f*dt;
            else
            {
                // Only steer during the first second, giving a committed dodge window.
                if(age<1.1f&&target!=null){var body=target.GetComponent<Collider>();aim=body!=null?body.bounds.center:target.position;}
                Vector3 wanted=aim-transform.position;
                if(age<1.35f&&wanted.sqrMagnitude>.01f)velocity=Vector3.RotateTowards(velocity,wanted.normalized*32f,95f*Mathf.Deg2Rad*dt,0);
            }
            Vector3 from=transform.position,step=velocity*dt;
            int count=Physics.SphereCastNonAlloc(from,.25f,step.normalized,hits,step.magnitude,owner.groundMask,QueryTriggerInteraction.Ignore);
            float nearest=float.PositiveInfinity;RaycastHit contact=default;
            for(int i=0;i<count;i++)
            {
                var c=hits[i].collider;if(c.transform.IsChildOf(owner.transform)||c.GetComponentInParent<BombardinoOrdnance>()!=null)continue;
                if(hits[i].distance<nearest){nearest=hits[i].distance;contact=hits[i];}
            }
            if(nearest<float.PositiveInfinity){transform.position=contact.point+contact.normal*.05f;Detonate();return;}
            transform.position+=step;if(velocity.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(velocity)*Quaternion.Euler(90,0,0);
            if(age>6f)Expire();
        }
        public void Detonate()
        {
            if(resolved)return;resolved=true;Blast(damage,true);if(owner!=null)owner.Explosion(transform.position,radius);Release();
        }
        void Blast(float amount,bool breakStructures)
        {
            playerDistances.Clear();structures.Clear();int count=Physics.OverlapSphereNonAlloc(transform.position,radius,overlaps,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var c=overlaps[i];var player=c.GetComponentInParent<PlayerHealth>();
                if(player!=null)
                {
                    float distance=Vector3.Distance(c.ClosestPoint(transform.position),transform.position);
                    if(!playerDistances.TryGetValue(player,out var old)||distance<old)playerDistances[player]=distance;
                }
                var structure=c.GetComponentInParent<BombardinoDestructible>();
                if(breakStructures&&structure!=null&&structures.Add(structure))structure.ApplyBlast(amount*6,transform.position);
            }
            foreach(var player in playerDistances){float falloff=Payload==Kind.Fire?1:Mathf.Lerp(1,.45f,Mathf.Clamp01(player.Value/radius));player.Key.ApplyDamage(amount*falloff,transform.position);}
        }
        void Expire(){if(resolved)return;resolved=true;Release();}
        void Release(){if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);}
        void OnDestroy(){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}if(flameMaterial!=null){if(Application.isPlaying)Destroy(flameMaterial);else DestroyImmediate(flameMaterial);}if(smokeMaterial!=null){if(Application.isPlaying)Destroy(smokeMaterial);else DestroyImmediate(smokeMaterial);}}
    }
}
