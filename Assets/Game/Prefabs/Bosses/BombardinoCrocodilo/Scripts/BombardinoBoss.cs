using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    [DisallowMultipleComponent,RequireComponent(typeof(Health))]
    public sealed class BombardinoBoss : MonoBehaviour
    {
        public enum State { Patrol, Telegraph, Attack, Exposed, Returning, Dead }
        public enum AttackKind { HighBombing, SpeedRun, Missiles, FlameRun }
        public Transform visual;
        public Transform target;
        public Material effectMaterial;
        public LayerMask groundMask=~0;
        public float engageDistance=65f, leashRadius=100f, cruiseAltitude=24f;
        public float cruiseSpeed=20f, rushSpeed=72f, lowAltitude=3.6f;
        public float telegraphSeconds=1.5f, exposedSeconds=3.3f;
        public float bombDamage=28f, missileDamage=23f, rushDamage=30f, fireDamage=8f;
        public UnityEvent onDefeated=new UnityEvent();
        public State CurrentState { get; private set; }
        public AttackKind CurrentAttack { get; private set; }
        public bool FightActive { get; private set; }
        public bool IsDead=>health!=null&&health.IsDead;
        public bool Enraged=>health!=null&&health.Ratio<=.5f;
        public bool IsExposed=>CurrentState==State.Exposed;
        public int BombsReleased { get; private set; }
        public int MissilesLaunched { get; private set; }
        public int FlameZonesCreated { get; private set; }
        public int RushHits { get; private set; }
        public string AttackName=>CurrentAttack==AttackKind.HighBombing?"HIGH-ALTITUDE BOMBING":CurrentAttack==AttackKind.SpeedRun?"SUPERSONIC STRAFE":CurrentAttack==AttackKind.Missiles?"MISSILE LOCK":"NAPALM RUN";
        public string Hint=>IsDead?"BOMBARDINO DOWN":IsExposed?"ENGINE COOLING — ATTACK NOW!":CurrentState==State.Telegraph?
            CurrentAttack==AttackKind.HighBombing?"BOMBING INCOMING — MOVE OUT OF THE TARGET AREA!":CurrentAttack==AttackKind.SpeedRun?"LOW PASS — DODGE SIDEWAYS!":CurrentAttack==AttackKind.Missiles?"MISSILE LOCK — KEEP MOVING!":"NAPALM INCOMING — LEAVE THE MARKED STRIP!":AttackName;
        Health health;
        PlayerHealth targetHealth;
        Vector3 home, committed, heading, runStart, previousTarget, targetVelocity;
        Quaternion visualRest;
        float clock, age, acquireAt, shotAt, deathGround;
        int attackCounter, shots;
        bool initialized,crashed;
        readonly List<BombardinoOrdnance> ordnance=new List<BombardinoOrdnance>();
        readonly List<GameObject> effects=new List<GameObject>();
        readonly List<LineRenderer> markers=new List<LineRenderer>();
        readonly HashSet<PlayerHealth> rushVictims=new HashSet<PlayerHealth>();
        readonly RaycastHit[] casts=new RaycastHit[96];
        Renderer[] bodyRenderers;
        Collider[] bodyColliders;
        CharacterController[] playerBodies=new CharacterController[0];
        readonly List<(Collider body, Collider player)> ignoredPlayerPairs=new List<(Collider, Collider)>();
        AudioSource audioSource;

        void Awake()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;initialized=true;health=GetComponent<Health>();
            health.OnDamaged??=new UnityEvent<float>();health.OnDeath??=new UnityEvent();health.OnDamaged.AddListener(OnHit);health.OnDeath.AddListener(OnDeath);
            Physics.SyncTransforms();home=Ground(transform.position);visualRest=visual!=null?visual.localRotation:Quaternion.identity;
            bodyRenderers=visual!=null?visual.GetComponentsInChildren<Renderer>():new Renderer[0];
            ConfigureBodyCollision();
            bodyColliders=GetComponentsInChildren<Collider>(true);
            ProtectPlayersFromBodyCollision();
            if(effectMaterial==null)effectMaterial=Resources.Load<Material>("StoryBattle/Sea");
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=.75f;audioSource.minDistance=8;audioSource.maxDistance=100;
            transform.position=new Vector3(transform.position.x,Mathf.Max(transform.position.y,home.y+cruiseAltitude),transform.position.z);
            if(health.IsDead)OnDeath();
        }
        void OnHit(float damage){if(!FightActive&&CurrentState!=State.Dead){acquireAt=0;Acquire();}}
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if(!initialized)Initialize();if(PauseSettingsMenu.IsOpen||deltaTime<=0)return;
            float dt=Mathf.Min(deltaTime,.1f);clock+=dt;age+=dt;
            if(CurrentState==State.Dead)
            {
                if(!crashed){transform.position+=Vector3.down*Mathf.Min(35,age*13)*dt;
                    if(visual!=null)visual.localRotation=visualRest*Quaternion.Euler(age*43,0,age*90);
                    if(transform.position.y<=deathGround+1){transform.position=new Vector3(transform.position.x,deathGround+1,transform.position.z);Explosion(transform.position,5);crashed=true;age=0;}}
                if(crashed&&age>3&&Application.isPlaying)Destroy(gameObject);return;
            }
            if(clock>=acquireAt){acquireAt=clock+.5f;ProtectPlayersFromBodyCollision();Acquire();}
            if(FightActive&&(targetHealth==null||targetHealth.currentHealth<=0||Vector3.Distance(Ground(target.position),home)>leashRadius))
            {FightActive=false;SetState(State.Returning);ClearHazards();}
            if(target!=null){targetVelocity=Vector3.ClampMagnitude((target.position-previousTarget)/Mathf.Max(.001f,dt),12);previousTarget=target.position;}
            // Ground clearance must use this frame's banking pose, including scaled wings.
            if(visual!=null)
            {
                float bank=CurrentState==State.Attack&&CurrentAttack==AttackKind.SpeedRun?18:Mathf.Sin(clock*1.9f)*3;
                visual.localRotation=visualRest*Quaternion.Euler(Mathf.Sin(clock*2.4f)*1.8f,0,bank);
            }
            switch(CurrentState)
            {
                case State.Patrol:
                    Fly(home+new Vector3(Mathf.Sin(clock*.28f)*14,cruiseAltitude,Mathf.Cos(clock*.28f)*14),cruiseSpeed*.65f,dt);break;
                case State.Returning:
                    Fly(home+Vector3.up*cruiseAltitude,cruiseSpeed,dt);
                    if(Vector3.Distance(transform.position,home+Vector3.up*cruiseAltitude)<2){health.currentHealth=health.maxHealth;attackCounter=0;SetState(State.Patrol);}break;
                case State.Telegraph:
                    float windup=telegraphSeconds*(Enraged?.8f:1);
                    Fly(runStart,cruiseSpeed*1.8f,dt);UpdateMarkers();
                    if(age>=windup&&Vector3.Distance(transform.position,runStart)<2){SetState(State.Attack);shotAt=0;shots=0;}break;
                case State.Attack:UpdateMarkers();RunAttack(dt);break;
                case State.Exposed:
                    Vector3 p=target!=null?Ground(target.position):home;
                    var landing=p-heading*7;landing=Ground(landing)+Vector3.up*lowAltitude;
                    landing=ClearGround(landing);
                    Fly(landing,cruiseSpeed*1.5f,dt);
                    // The damage window starts only once the plane is within stick reach.
                    if(Vector3.Distance(transform.position,landing)>2)age=0;
                    else if(age>=exposedSeconds*(Enraged?.85f:1)){attackCounter++;StartAttack((AttackKind)(attackCounter%4));}break;
            }
        }
        void Acquire()
        {
            if(CurrentState==State.Dead||CurrentState==State.Returning)return;
            if(target==null||targetHealth==null||targetHealth.currentHealth<=0)
            {
                target=null;targetHealth=null;float best=engageDistance*engageDistance;
                foreach(var player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))if(player.currentHealth>0&&player.isActiveAndEnabled){float d=(Ground(player.transform.position)-home).sqrMagnitude;if(d<best){best=d;target=player.transform;targetHealth=player;}}
                if(target!=null)previousTarget=target.position;
            }
            if(!FightActive&&targetHealth!=null&&(Ground(target.position)-home).sqrMagnitude<engageDistance*engageDistance){FightActive=true;StartAttack(AttackKind.HighBombing);}
        }
        void StartAttack(AttackKind kind)
        {
            if(target==null||IsDead)return;CurrentAttack=kind;committed=Ground(target.position+targetVelocity*.3f);
            heading=Vector3.ProjectOnPlane(committed-transform.position,Vector3.up).normalized;if(heading.sqrMagnitude<.1f)heading=transform.forward;
            runStart=kind==AttackKind.HighBombing?committed+Vector3.up*(cruiseAltitude+8):kind==AttackKind.Missiles?committed-heading*24+Vector3.up*16:Ground(committed-heading*34)+Vector3.up*(kind==AttackKind.SpeedRun?5:12);
            rushVictims.Clear();SetState(State.Telegraph);BuildMarkers(kind);
        }
        void SetState(State state){CurrentState=state;age=0;if(state==State.Exposed)ClearMarkers();}
        void RunAttack(float dt)
        {
            float power=Enraged?1.15f:1;
            if(CurrentAttack==AttackKind.HighBombing)
            {
                Fly(committed+Vector3.up*(cruiseAltitude+8),cruiseSpeed,dt);
                if(age>=shotAt&&shots<6){var ground=Ground(committed+heading*((shots-2.5f)*3));Spawn(BombardinoOrdnance.Kind.Bomb,ground+Vector3.up*(cruiseAltitude+7),Vector3.down,ground,bombDamage*power,4.3f);BombsReleased++;shots++;shotAt+=.22f;}
                if(age>3.6f)SetState(State.Exposed);
            }
            else if(CurrentAttack==AttackKind.Missiles)
            {
                Fly(runStart+Vector3.right*Mathf.Sin(age)*3,cruiseSpeed*.4f,dt);
                if(age>=shotAt&&shots<3){var body=target!=null?target.GetComponent<Collider>():null;Vector3 aim=body!=null?body.bounds.center:committed;Spawn(BombardinoOrdnance.Kind.Missile,transform.position+transform.forward*3,(aim-transform.position).normalized,aim,missileDamage*power,3.5f);MissilesLaunched++;shots++;shotAt+=.45f;}
                if(age>3.8f)SetState(State.Exposed);
            }
            else
            {
                Vector3 from=transform.position;float speed=CurrentAttack==AttackKind.SpeedRun?rushSpeed*(Enraged?1.2f:1):cruiseSpeed*1.6f;
                Vector3 destination=committed+heading*48;destination=Ground(destination)+Vector3.up*(CurrentAttack==AttackKind.SpeedRun?5:12);Fly(destination,speed,dt);
                if(CurrentAttack==AttackKind.SpeedRun)
                {
                    // Keep the low-pass attack volume on its ground strip even when
                    // the physical aircraft clears a player's head safely.
                    var strikeFrom=Ground(from)+Vector3.up*2.6f;
                    var strikeTo=Ground(transform.position)+Vector3.up*2.6f;
                    var step=strikeTo-strikeFrom;int count=Physics.SphereCastNonAlloc(strikeFrom,2.6f,step.normalized,casts,step.magnitude,~0,QueryTriggerInteraction.Ignore);
                    for(int i=0;i<count;i++){var hp=casts[i].collider.GetComponentInParent<PlayerHealth>();if(hp!=null&&rushVictims.Add(hp)){hp.ApplyDamage(rushDamage*power,transform.position);RushHits++;}}
                }
                else if(age>=shotAt&&shots<5)
                {
                    Vector3 point=Ground(committed+heading*((shots-2)*3.8f));Spawn(BombardinoOrdnance.Kind.Fire,point+Vector3.up*.1f,Vector3.down,point,fireDamage*power,2.8f);FlameZonesCreated++;shots++;shotAt+=.3f;
                }
                if(age>(CurrentAttack==AttackKind.SpeedRun?1.6f:2.4f))SetState(State.Exposed);
            }
        }
        void Spawn(BombardinoOrdnance.Kind kind,Vector3 point,Vector3 direction,Vector3 aim,float damage,float radius)
        {
            var go=new GameObject("Bombardino "+kind);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);go.transform.position=point;
            var payload=go.AddComponent<BombardinoOrdnance>();payload.Launch(this,kind,direction,target,aim,damage,radius);
        }
        public void RegisterOrdnance(BombardinoOrdnance payload){ordnance.RemoveAll(p=>p==null);if(payload!=null&&!ordnance.Contains(payload))ordnance.Add(payload);}
        void Fly(Vector3 point,float speed,float dt)
        {
            point=ClearGround(point);
            Vector3 travel=point-transform.position;Vector3 flat=Vector3.ProjectOnPlane(travel,Vector3.up);
            if(flat.sqrMagnitude>.2f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),180*dt);
            transform.position=Vector3.MoveTowards(transform.position,point,speed*dt);
            transform.position=ClearGround(transform.position);
        }
        public void ConfigureBodyCollision()
        {
            if(visual==null)return;
            var mesh=visual.GetComponentInChildren<MeshFilter>();
            if(mesh==null||mesh.sharedMesh==null)return;
            // Separate primitives follow the banked body, wings and engines. They
            // support combat ClosestPoint queries and cost less than a moving triangle mesh.
            var oldMesh=mesh.GetComponent<MeshCollider>();
            if(oldMesh!=null){oldMesh.enabled=false;if(Application.isPlaying)Destroy(oldMesh);else DestroyImmediate(oldMesh);}
            foreach(var box in GetComponents<BoxCollider>())box.enabled=false;
            var group=visual.Find("Physical body colliders");
            if(group==null){var go=new GameObject("Physical body colliders");go.transform.SetParent(visual,false);group=go.transform;}
            var vertices=mesh.sharedMesh.vertices;
            void Part(string name,int side,params int[] sections)
            {
                bool found=false;Bounds bounds=default;
                foreach(int section in sections)
                {
                    if(section>=mesh.sharedMesh.subMeshCount)continue;
                    foreach(int index in mesh.sharedMesh.GetTriangles(section))
                    {
                        Vector3 v=vertices[index];if(side!=0&&v.x*side<0)continue;
                        v=visual.InverseTransformPoint(mesh.transform.TransformPoint(v));
                        if(!found){bounds=new Bounds(v,Vector3.zero);found=true;}else bounds.Encapsulate(v);
                    }
                }
                if(!found)return;
                var child=group.Find(name);if(child==null){var go=new GameObject(name);go.transform.SetParent(group,false);child=go.transform;}
                var collider=child.GetComponent<BoxCollider>();if(collider==null)collider=child.gameObject.AddComponent<BoxCollider>();
                collider.center=bounds.center;collider.size=Vector3.Max(bounds.size,Vector3.one*.08f);collider.isTrigger=false;collider.enabled=true;
            }
            Part("Fuselage and crocodile",0,0,1,8);
            Part("Main wings",0,2);
            Part("Tail",0,3,7);
            Part("Left engine",-1,4);Part("Right engine",1,4);
            Part("Left propeller",-1,6);Part("Right propeller",1,6);
        }
        void ProtectPlayersFromBodyCollision()
        {
            var players=FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            var controllers=new List<CharacterController>();
            foreach(var player in players)
            {
                var controller=player.GetComponent<CharacterController>();
                if(controller!=null)controllers.Add(controller);
                foreach(var collider in player.GetComponentsInChildren<Collider>(true))
                    foreach(var body in bodyColliders)
                    {
                        // The live character has solid collision; only inactive ragdoll
                        // parts are excluded, so they cannot snag the flying body.
                        if(collider is CharacterController)continue;
                        if(body==null||!body.enabled||collider==null||body==collider||Physics.GetIgnoreCollision(body,collider))continue;
                        Physics.IgnoreCollision(body,collider,true);
                        ignoredPlayerPairs.Add((body,collider));
                    }
            }
            playerBodies=controllers.ToArray();
        }
        Vector3 ClearGround(Vector3 point)
        {
            var bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var r in bodyRenderers)if(r!=null&&r.enabled)bounds.Encapsulate(r.bounds);
            foreach(var c in bodyColliders)if(c!=null&&c.enabled&&!c.isTrigger)bounds.Encapsulate(c.bounds);
            float underside=Mathf.Max(0,transform.position.y-bounds.min.y);
            float floor=Ground(point).y;
            // Sample the complete footprint on low passes; checking only the centre
            // allows large or banked wings to enter the island's slopes.
            if(point.y-floor<Mathf.Max(cruiseAltitude,underside+4))
            {
                Vector3 center=point+(bounds.center-transform.position);
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                {
                    if(x==0&&z==0)continue;
                    var sample=center+new Vector3(bounds.extents.x*x,0,bounds.extents.z*z);
                    floor=Mathf.Max(floor,Ground(sample).y);
                }
            }
            point.y=Mathf.Max(point.y,floor+Mathf.Max(2.5f,underside+.4f));
            // Stop descent above a player's head instead of disabling physical
            // collision or letting overlap resolution squeeze them through the ground.
            foreach(var player in playerBodies)
            {
                if(player==null||!player.enabled||!player.gameObject.activeInHierarchy)continue;
                var capsule=player.bounds;
                foreach(var body in bodyColliders)
                {
                    if(body==null||!body.enabled||body.isTrigger)continue;
                    var destination=body.bounds;float bodyUnderside=transform.position.y-destination.min.y;
                    destination.center+=point-transform.position;
                    if(destination.max.x<capsule.min.x||destination.min.x>capsule.max.x||destination.max.z<capsule.min.z||destination.min.z>capsule.max.z)continue;
                    if(destination.max.y<capsule.min.y||destination.min.y>capsule.max.y+.2f)continue;
                    point.y=Mathf.Max(point.y,capsule.max.y+bodyUnderside+.2f);
                }
            }
            return point;
        }
        public Vector3 Ground(Vector3 p)
        {
            int count=Physics.RaycastNonAlloc(new Ray(new Vector3(p.x,1000,p.z),Vector3.down),casts,2000,groundMask,QueryTriggerInteraction.Ignore);float best=float.NegativeInfinity,terrain=float.NegativeInfinity;
            for(int i=0;i<count;i++){var c=casts[i].collider;if(c.transform.IsChildOf(transform)||c.GetComponentInParent<PlayerHealth>()!=null||c.GetComponentInParent<Health>()!=null||c.GetComponentInParent<BombardinoOrdnance>()!=null||c.GetComponentInParent<BombardinoDestructible>()!=null)continue;
                if(c is TerrainCollider||c.GetComponentInParent<ProceduralIsland>()!=null){terrain=Mathf.Max(terrain,casts[i].point.y);continue;}
                if(c is CapsuleCollider||c is SphereCollider||c.attachedRigidbody!=null&&!c.attachedRigidbody.isKinematic||casts[i].normal.y<.65f)continue;best=Mathf.Max(best,casts[i].point.y);}
            // Over open water there may be no collider. Reusing the aircraft's current
            // height as the floor would add clearance every frame and make it climb forever.
            float y=terrain>float.NegativeInfinity?terrain:best>float.NegativeInfinity?best:home.y;
            return new Vector3(p.x,y,p.z);
        }
        void BuildMarkers(AttackKind kind)
        {
            ClearMarkers();int count=kind==AttackKind.HighBombing?6:kind==AttackKind.FlameRun?5:1;
            for(int n=0;n<count;n++)
            {
                var go=new GameObject("Bombardino attack warning");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=effectMaterial;line.positionCount=65;line.startWidth=line.endWidth=.065f;line.startColor=line.endColor=new Color(.58f,.49f,.36f,.32f);line.useWorldSpace=true;
                var point=Ground(committed+heading*((n-(count-1)*.5f)*(kind==AttackKind.HighBombing?3:3.8f)))+Vector3.up*.15f;
                float radius=kind==AttackKind.HighBombing?4.3f:kind==AttackKind.SpeedRun?7:2.8f;
                for(int i=0;i<65;i++){float a=i/64f*Mathf.PI*2;line.SetPosition(i,Ground(point+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius))+Vector3.up*.15f);}markers.Add(line);
            }
        }
        void UpdateMarkers(){foreach(var marker in markers)if(marker!=null)marker.startWidth=marker.endWidth=.065f;}
        void ClearMarkers(){foreach(var line in markers)if(line!=null)Release(line.gameObject);markers.Clear();}
        public void Explosion(Vector3 p,float radius)
        {
            var go=new GameObject("Bombardino impact smoke");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);go.transform.position=p;
            var fx=go.AddComponent<BombardinoBlastFX>();fx.Initialize(radius);effects.RemoveAll(e=>e==null);effects.Add(go);
            var clip=Resources.Load<AudioClip>("StoryBattle/Audio/TidalHit");if(audioSource!=null&&clip!=null)audioSource.PlayOneShot(clip,.6f);
        }
        void OnDeath(){if(CurrentState==State.Dead)return;FightActive=false;SetState(State.Dead);deathGround=Ground(transform.position).y;ClearHazards();foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;onDefeated.Invoke();}
        void ClearHazards(){ClearMarkers();foreach(var p in ordnance)if(p!=null)Release(p.gameObject);ordnance.Clear();foreach(var e in effects)if(e!=null)Release(e);effects.Clear();}
        void OnDisable(){ClearHazards();if(audioSource!=null)audioSource.Stop();}
        void OnDestroy(){ClearHazards();foreach(var pair in ignoredPlayerPairs)if(pair.body!=null&&pair.player!=null)Physics.IgnoreCollision(pair.body,pair.player,false);if(health!=null){health.OnDamaged?.RemoveListener(OnHit);health.OnDeath?.RemoveListener(OnDeath);}}
        static void Release(Object o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
