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
        public float engageDistance=100f, leashRadius=160f, cruiseAltitude=55f;
        [Min(10f)] public float patrolRadius=32f;
        public float cruiseSpeed=20f, rushSpeed=72f, lowAltitude=3.6f;
        public float telegraphSeconds=1.5f, exposedSeconds=3.3f;
        public float bombDamage=28f, missileDamage=23f, rushDamage=30f, fireDamage=8f;
        public UnityEvent onDefeated=new UnityEvent();
        public State CurrentState { get; private set; }
        public AttackKind CurrentAttack { get; private set; }
        public bool FightActive { get; private set; }
        public bool IsDead=>health!=null&&health.IsDead;
        public bool Enraged=>health!=null&&health.Ratio<=.5f;
        public bool IsExposed=>CurrentState==State.Exposed && exposureSettled;
        public int BombsReleased { get; private set; }
        public int MissilesLaunched { get; private set; }
        public int FlameZonesCreated { get; private set; }
        public int RushHits { get; private set; }
        public string AttackName=>CurrentAttack==AttackKind.HighBombing?"HIGH-ALTITUDE BOMBING":CurrentAttack==AttackKind.SpeedRun?"SUPERSONIC STRAFE":CurrentAttack==AttackKind.Missiles?"MISSILE LOCK":"NAPALM RUN";
        public string Hint=>IsDead?"BOMBARDINO DOWN":IsExposed?"ENGINE COOLING — ATTACK NOW!":CurrentState==State.Exposed?"DESCENDING — GET READY TO COUNTER!":CurrentState==State.Telegraph?
            CurrentAttack==AttackKind.HighBombing?"BOMBING INCOMING — MOVE OUT OF THE TARGET AREA!":CurrentAttack==AttackKind.SpeedRun?"LOW PASS — DODGE SIDEWAYS!":CurrentAttack==AttackKind.Missiles?"MISSILE LOCK — KEEP MOVING!":"NAPALM INCOMING — LEAVE THE MARKED STRIP!":AttackName;
        Health health;
        PlayerHealth targetHealth;
        Vector3 home, committed, heading, runStart, previousTarget, targetVelocity;
        Quaternion visualRest;
        float clock, age, acquireAt, shotAt, deathGround;
        float turnBank;
        int attackCounter, shots;
        bool initialized,crashed;
        Vector3 exposureLanding;
        bool exposureSettled;
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
        const int MarkerSegments=32;
        readonly Vector3[] markerPoints=new Vector3[MarkerSegments+1];
        float protectPlayersAt;
        Bounds launchBounds;
        readonly List<(Vector3 point,float at,float damage)> fireImpacts=new List<(Vector3,float,float)>();
        public Vector3 WeaponOrigin(bool nose, int side = 0)
        {
            Vector3 local=nose?new Vector3(launchBounds.center.x,launchBounds.center.y,launchBounds.max.z):
                new Vector3(launchBounds.center.x+launchBounds.extents.x*.52f*side,launchBounds.min.y-.3f,launchBounds.center.z+launchBounds.extents.z*.15f);
            return transform.TransformPoint(local);
        }
        public Vector3 FlameContact => Ground(WeaponOrigin(true)+transform.forward*3);

        void Awake()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;initialized=true;health=GetComponent<Health>();
            BossCombatVfx.PrewarmImpacts();
            health.OnDamaged??=new UnityEvent<float>();health.OnDeath??=new UnityEvent();health.OnDamaged.AddListener(OnHit);health.OnDeath.AddListener(OnDeath);
            Physics.SyncTransforms();home=Ground(transform.position);visualRest=visual!=null?visual.localRotation:Quaternion.identity;
            bodyRenderers=visual!=null?visual.GetComponentsInChildren<Renderer>():new Renderer[0];
            launchBounds=new Bounds(Vector3.zero,Vector3.one*2);
            foreach(var r in bodyRenderers){var b=r.bounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)launchBounds.Encapsulate(transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));}
            if(GetComponent<BombardinoFlameJet>()==null)gameObject.AddComponent<BombardinoFlameJet>();
            ConfigureBodyCollision(false);
            bodyColliders=GetComponentsInChildren<Collider>(true);
            ProtectPlayersFromBodyCollision();
            protectPlayersAt=clock+2f;
            if(effectMaterial==null)effectMaterial=Resources.Load<Material>("StoryBattle/Sea");
            if(GameAudioPolicy.SoundEnabled){audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=.75f;audioSource.minDistance=8;audioSource.maxDistance=100;}
            transform.position=new Vector3(transform.position.x,Mathf.Max(transform.position.y,home.y+cruiseAltitude),transform.position.z);
            // Allocate warning renderers at encounter loading, not on a weapon hit.
            for(int i=0;i<6;i++)
            {
                var go=new GameObject("Bombardino attack warning");go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=effectMaterial;line.positionCount=MarkerSegments+1;
                line.startWidth=line.endWidth=.065f;line.startColor=line.endColor=new Color(.58f,.49f,.36f,.32f);
                line.useWorldSpace=true;line.enabled=false;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows=false;markers.Add(line);
            }
            if(health.IsDead)OnDeath();
        }
        void OnHit(float damage){if(!FightActive&&CurrentState!=State.Dead)acquireAt=clock;}
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if(!initialized)Initialize();if(PauseSettingsMenu.IsOpen||deltaTime<=0)return;
            float dt=Mathf.Min(deltaTime,.1f);clock+=dt;age+=dt;
            for(int i=fireImpacts.Count-1;i>=0;i--)if(clock>=fireImpacts[i].at)
            {
                var impact=fireImpacts[i];fireImpacts.RemoveAt(i);
                if(FightActive&&!IsDead){Spawn(BombardinoOrdnance.Kind.Fire,impact.point+Vector3.up*.1f,Vector3.down,impact.point,impact.damage,2.8f);FlameZonesCreated++;}
            }
            if(CurrentState==State.Dead)
            {
                if(!crashed){transform.position+=Vector3.down*Mathf.Min(35,age*13)*dt;
                    if(visual!=null)visual.localRotation=visualRest*Quaternion.Euler(age*43,0,age*90);
                    if(transform.position.y<=deathGround+1){transform.position=new Vector3(transform.position.x,deathGround+1,transform.position.z);Explosion(transform.position,5);crashed=true;age=0;}}
                if(crashed&&age>3&&Application.isPlaying)Destroy(gameObject);return;
            }
            if(clock>=protectPlayersAt){protectPlayersAt=clock+2f;ProtectPlayersFromBodyCollision();}
            if(clock>=acquireAt){acquireAt=clock+.5f;Acquire();}
            if(FightActive&&(targetHealth==null||targetHealth.currentHealth<=0||Vector3.Distance(Ground(target.position),home)>leashRadius))
            {FightActive=false;SetState(State.Returning);ClearHazards();}
            if(target!=null){targetVelocity=Vector3.ClampMagnitude((target.position-previousTarget)/Mathf.Max(.001f,dt),12);previousTarget=target.position;}
            // Ground clearance must use this frame's banking pose, including scaled wings.
            if(visual!=null)
            {
                float bank=turnBank+Mathf.Sin(clock*1.9f)*1.8f;
                visual.localRotation=Quaternion.Euler(Mathf.Sin(clock*2.4f)*1.8f,0,bank)*visualRest;
            }
            switch(CurrentState)
            {
                case State.Patrol:
                    Fly(home+new Vector3(Mathf.Sin(clock*.28f)*patrolRadius,cruiseAltitude,Mathf.Cos(clock*.28f)*patrolRadius),cruiseSpeed*.65f,dt);break;
                case State.Returning:
                    Fly(home+Vector3.up*cruiseAltitude,cruiseSpeed,dt);
                    if(Vector3.Distance(transform.position,home+Vector3.up*cruiseAltitude)<2){health.currentHealth=health.maxHealth;attackCounter=0;SetState(State.Patrol);}break;
                case State.Telegraph:
                    float windup=telegraphSeconds*(Enraged?.8f:1);
                    Fly(runStart,cruiseSpeed*1.8f,dt);UpdateMarkers();
                    if(age>=windup&&Vector3.Distance(transform.position,runStart)<2){SetState(State.Attack);shotAt=0;shots=0;}break;
                case State.Attack:UpdateMarkers();RunAttack(dt);break;
                case State.Exposed:
                    var landing=Fly(exposureLanding,cruiseSpeed*1.5f,dt);
                    // Commit the landing once. Running away must not reset the opening.
                    if(!exposureSettled)
                    {
                        age=0;
                        if(Vector3.Distance(transform.position,landing)<=2)exposureSettled=true;
                    }
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
            runStart=kind==AttackKind.HighBombing?committed-heading*30+Vector3.up*(cruiseAltitude+8):kind==AttackKind.Missiles?committed-heading*42+Vector3.up*(cruiseAltitude*.6f):Ground(committed-heading*44)+Vector3.up*(kind==AttackKind.SpeedRun?5:12);
            rushVictims.Clear();SetState(State.Telegraph);BuildMarkers(kind);
        }
        void SetState(State state)
        {
            CurrentState=state;age=0;exposureSettled=false;
            if(state==State.Exposed)
            {
                ClearMarkers();
                var point=(target!=null?target.position:home)-heading*7;
                exposureLanding=Ground(point)+Vector3.up*lowAltitude;
            }
        }
        void RunAttack(float dt)
        {
            float power=Enraged?1.15f:1;
            if(CurrentAttack==AttackKind.HighBombing)
            {
                Fly(committed+heading*22+Vector3.up*(cruiseAltitude+8),cruiseSpeed*.75f,dt);
                if(age>=shotAt&&shots<6){var ground=Ground(committed+heading*((shots-2.5f)*3));Spawn(BombardinoOrdnance.Kind.Bomb,WeaponOrigin(false,shots%2==0?-1:1),Vector3.down,ground,bombDamage*power,4.3f);BombsReleased++;shots++;shotAt+=.22f;}
                if(age>3.6f)SetState(State.Exposed);
            }
            else if(CurrentAttack==AttackKind.Missiles)
            {
                Fly(runStart+Vector3.right*Mathf.Sin(age)*3,cruiseSpeed*.4f,dt);
                if(age>=shotAt&&shots<3){var body=target!=null?target.GetComponent<Collider>():null;Vector3 aim=body!=null?body.bounds.center:committed;Vector3 mount=WeaponOrigin(false,shots%2==0?-1:1);Spawn(BombardinoOrdnance.Kind.Missile,mount,(aim-mount).normalized,aim,missileDamage*power,3.5f);MissilesLaunched++;shots++;shotAt+=.6f;}
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
                else if(age>=shotAt&&shots<7)
                {
                    // Burn the strip the aircraft actually crosses, after the visible jet reaches it.
                    Vector3 point=FlameContact;float flight=Vector3.Distance(point,WeaponOrigin(true))/30f;
                    fireImpacts.Add((point,clock+flight,fireDamage*power));shots++;shotAt=age+.23f;
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
        Vector3 Fly(Vector3 point,float speed,float dt)
        {
            point=ClearGround(point);
            Vector3 travel=point-transform.position;Vector3 flat=Vector3.ProjectOnPlane(travel,Vector3.up);
            float wantedBank=flat.sqrMagnitude>.2f?-Mathf.Clamp(Vector3.SignedAngle(transform.forward,flat,Vector3.up)*.4f,-24,24):0;
            turnBank=Mathf.Lerp(turnBank,wantedBank,1-Mathf.Exp(-4*dt));
            if(flat.sqrMagnitude>.2f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(flat),180*dt);
            transform.position=Vector3.MoveTowards(transform.position,point,speed*dt);
            transform.position=ClearGround(transform.position);
            return point;
        }
        public void ConfigureBodyCollision(bool rebuild=true)
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
            // Scaling the actor automatically scales these fitted boxes. Re-reading
            // all mesh triangles is only needed when authoring a new collision fit.
            if(!rebuild && group!=null)
            {
                var children=group.GetComponentsInChildren<BoxCollider>();
                if(children.Length>=7){bool valid=true;foreach(var box in children)if(!box.enabled||box.isTrigger||box.size.sqrMagnitude<.001f){valid=false;break;}if(valid)return;}
            }
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
                var line=markers[n];
                var point=Ground(committed+heading*((n-(count-1)*.5f)*(kind==AttackKind.HighBombing?3:3.8f)))+Vector3.up*.15f;
                float radius=kind==AttackKind.HighBombing?4.3f:kind==AttackKind.SpeedRun?7:2.8f;
                for(int i=0;i<=MarkerSegments;i++){float a=i/(float)MarkerSegments*Mathf.PI*2;markerPoints[i]=Ground(point+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius))+Vector3.up*.15f;}
                line.SetPositions(markerPoints);line.enabled=true;
            }
        }
        void UpdateMarkers(){ }
        void ClearMarkers(){foreach(var line in markers)if(line!=null)line.enabled=false;}
        public void Explosion(Vector3 p,float radius)
        {
            var go=new GameObject("Bombardino impact smoke");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);go.transform.position=p;
            var fx=go.AddComponent<BombardinoBlastFX>();fx.Initialize(radius);effects.RemoveAll(e=>e==null);effects.Add(go);
            if(audioSource!=null){var clip=Resources.Load<AudioClip>("StoryBattle/Audio/TidalHit");if(clip!=null)audioSource.PlayOneShot(clip,.6f);}
        }
        void OnDeath(){if(CurrentState==State.Dead)return;FightActive=false;SetState(State.Dead);deathGround=Ground(transform.position).y;ClearHazards();foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;onDefeated.Invoke();}
        void ClearHazards(){fireImpacts.Clear();ClearMarkers();foreach(var p in ordnance)if(p!=null)Release(p.gameObject);ordnance.Clear();foreach(var e in effects)if(e!=null)Release(e);effects.Clear();}
        void OnDisable(){ClearHazards();if(audioSource!=null)audioSource.Stop();}
        void OnDestroy(){ClearHazards();foreach(var pair in ignoredPlayerPairs)if(pair.body!=null&&pair.player!=null)Physics.IgnoreCollision(pair.body,pair.player,false);if(health!=null){health.OnDamaged?.RemoveListener(OnHit);health.OnDeath?.RemoveListener(OnDeath);}}
        static void Release(Object o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
