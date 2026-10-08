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
        static Material bombMaterial, missileMaterial;
        Material flameMaterial;
        Material smokeMaterial;
        ParticleSystem fire;
        readonly RaycastHit[] hits=new RaycastHit[64];
        readonly Collider[] overlaps=new Collider[128];
        readonly Dictionary<PlayerHealth,float> playerDistances=new Dictionary<PlayerHealth,float>();
        readonly HashSet<BombardinoDestructible> structures=new HashSet<BombardinoDestructible>();

        public void Launch(BombardinoBoss source,Kind kind,Vector3 direction,Transform victim,Vector3 point,float amount,float blastRadius)
        {
            owner=source;owner.RegisterOrdnance(this);Payload=kind;target=victim;aim=point;damage=amount;radius=blastRadius;
            velocity=direction.normalized*(kind==Kind.Missile?12f:2f);
            if(kind==Kind.Bomb)
            {
                float flight=Mathf.Sqrt(Mathf.Max(.5f,transform.position.y-point.y)*2/25f);
                velocity=Vector3.ProjectOnPlane(point-transform.position,Vector3.up)/flight;
            }
            if(kind!=Kind.Fire)
            {
                var body=new GameObject(kind+" aerodynamic body");body.transform.SetParent(transform,false);
                body.AddComponent<MeshFilter>().sharedMesh=BombardinoPayloadMesh.Get(kind==Kind.Missile);
                var renderer=body.AddComponent<MeshRenderer>();renderer.sharedMaterial=PayloadMaterial(kind);
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                transform.rotation=Quaternion.LookRotation(velocity);
            }
            if(kind==Kind.Fire)
            {
                fire=BossCombatVfx.Flames(transform,"Ground fire",64,45,1.15f,1.1f,.7f);
                fire.transform.localRotation=Quaternion.Euler(-90,0,0);
                fire.transform.localPosition=Vector3.up*.55f;
                var fireMain=fire.main;fireMain.startSize3D=true;
                fireMain.startSizeX=new ParticleSystem.MinMaxCurve(.75f,1.3f);
                fireMain.startSizeY=new ParticleSystem.MinMaxCurve(1.4f,2.5f);fireMain.startSizeZ=1;
                var shape=fire.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=radius*.7f;
                var renderer=fire.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.VerticalBillboard;
                smokeMaterial=NaturalParticleEffects.SmokeMaterial("Burn smoke",new Color(.23f,.22f,.20f,.42f));
                var smoke=NaturalParticleEffects.Emitter(transform,"Burn smoke",smokeMaterial,24,-.12f);
                smoke.gameObject.AddComponent<BossVfxMaterialOwner>().material=smokeMaterial;smokeMaterial=null;
                smoke.transform.localRotation=Quaternion.Euler(-90,0,0);
                var main=smoke.main;main.startSpeed=1.8f;main.startSize=new ParticleSystem.MinMaxCurve(.65f,1.2f);main.startLifetime=2.1f;
                var smokeSize=smoke.sizeOverLifetime;smokeSize.enabled=true;smokeSize.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.7f,1,2));
                var emission=smoke.emission;emission.enabled=true;emission.rateOverTime=8;
                var smokeShape=smoke.shape;smokeShape.enabled=true;smokeShape.shapeType=ParticleSystemShapeType.Circle;smokeShape.radius=radius*.65f;
            }
            if(kind==Kind.Missile)
            {
                var nozzle=new GameObject("Rocket motor").transform;nozzle.SetParent(transform,false);nozzle.localPosition=Vector3.back*.8f;nozzle.localRotation=Quaternion.Euler(0,180,0);
                BossCombatVfx.Flames(nozzle,"Rocket flame",18,35,.24f,.16f,4);
                BossCombatVfx.SmokeTrail(nozzle,2,.3f,64);
            }
        }
        static Material PayloadMaterial(Kind kind)
        {
            ref Material material = ref (kind==Kind.Missile ? ref missileMaterial : ref bombMaterial);
            if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name=kind+" shared payload paint";
            material.SetColor("_BaseColor",kind==Kind.Missile?new Color(.46f,.48f,.45f):new Color(.15f,.17f,.18f));
            material.SetFloat("_Smoothness",.28f);
            var paint=Resources.Load<Texture2D>(kind==Kind.Missile?"BossCombat/MissilePaint":"BossCombat/BombPaint");
            if(paint){material.SetTexture("_BaseMap",paint);material.SetColor("_BaseColor",Color.white);}
            material.SetFloat("_Metallic",kind==Kind.Missile?.55f:.3f);
            return material;
        }
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if(resolved||PauseSettingsMenu.IsOpen||deltaTime<=0)return;
            if(owner==null||!owner.isActiveAndEnabled||owner.IsDead){Expire();return;}
            float dt=Mathf.Min(deltaTime,.1f);age+=dt;
            if(Payload==Kind.Fire)
            {
                if(fire!=null){var emission=fire.emission;emission.rateOverTime=45*Mathf.Clamp01((5-age)/1.3f);}
                if(age>=fireTick){fireTick=age+.65f;Blast(damage,false);}if(age>=5f)Expire();return;
            }
            if(Payload==Kind.Bomb)velocity+=Vector3.down*25f*dt;
            else
            {
                // Only steer during the first second, giving a committed dodge window.
                if(age<.85f&&target!=null){var body=target.GetComponent<Collider>();aim=body!=null?body.bounds.center:target.position;}
                Vector3 wanted=aim-transform.position;
                float speed=Mathf.Lerp(12,36,Mathf.Clamp01((age-.12f)/.65f));
                if(age<1.2f&&wanted.sqrMagnitude>.01f)velocity=Vector3.RotateTowards(velocity,wanted.normalized*speed,55f*Mathf.Deg2Rad*dt,80*dt);
                else velocity=velocity.normalized*speed;
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
            transform.position+=step;if(velocity.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(velocity);
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
            foreach(var player in playerDistances)
            {
                // Solid cover blocks blast damage; floor fire cannot burn someone far above it.
                var playerBody=player.Key.GetComponent<CharacterController>();
                var body=playerBody!=null?(Collider)playerBody:player.Key.GetComponent<Collider>();
                float feet=body!=null?body.bounds.min.y:player.Key.transform.position.y;
                if(Payload==Kind.Fire && feet>transform.position.y+2.2f)continue;
                Vector3 center=body!=null?body.bounds.center:player.Key.transform.position+Vector3.up;
                Vector3 origin=transform.position+Vector3.up*.25f,delta=center-origin;bool blocked=false;
                int rays=Physics.RaycastNonAlloc(origin,delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<rays;i++){var hit=hits[i].collider;if(!hit.transform.IsChildOf(owner.transform)&&hit.GetComponentInParent<PlayerHealth>()!=player.Key && hit.GetComponentInParent<BombardinoOrdnance>()==null){blocked=true;break;}}
                if(blocked)continue;
                float falloff=Payload==Kind.Fire?1:Mathf.Lerp(1,.45f,Mathf.Clamp01(player.Value/radius));player.Key.ApplyDamage(amount*falloff,transform.position);
            }
        }
        void Expire(){if(resolved)return;resolved=true;Release();}
        void Release(){if(Application.isPlaying){BossCombatVfx.DetachTrails(transform);Destroy(gameObject);}else DestroyImmediate(gameObject);}
        void OnDestroy(){if(flameMaterial!=null){if(Application.isPlaying)Destroy(flameMaterial);else DestroyImmediate(flameMaterial);}if(smokeMaterial!=null){if(Application.isPlaying)Destroy(smokeMaterial);else DestroyImmediate(smokeMaterial);}}
    }
}
