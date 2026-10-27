using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    [DisallowMultipleComponent,RequireComponent(typeof(Health))]
    public sealed class BombardinoBoss : MonoBehaviour
    {
        public enum State { Patrol, Telegraph, Attack, Exposed, Returning, Dead, Circling }
        public enum AttackKind { HighBombing, SpeedRun, Missiles, FlameRun, MissileSalvo, CrossBombing }
        public Transform visual;
        public Transform target;
        public Material effectMaterial;
        public LayerMask groundMask=~0;
        public float engageDistance=100f, leashRadius=160f, cruiseAltitude=55f;
        [Min(10f)] public float patrolRadius=32f;
        public float cruiseSpeed=20f, rushSpeed=72f;
        public float telegraphSeconds=1.5f;
        [Min(1f)] public float attackInterval=5f;
        [Min(.5f)] public float counterWindowSeconds=2.2f;
        // Retain old serialized fields for existing scenes; landing/cooling is no longer used.
        [HideInInspector] public float lowAltitude=3.6f, exposedSeconds;
        public float bombDamage=28f, missileDamage=23f, rushDamage=30f, fireDamage=8f;
        public UnityEvent onDefeated=new UnityEvent();
        public State CurrentState { get; private set; }
        public AttackKind CurrentAttack { get; private set; }
        public bool FightActive { get; private set; }
        public bool IsDead=>health!=null&&health.IsDead;
        public static int PhaseForHealth(float ratio)=>ratio<=.5f?2:1;
        public int FightPhase=>health!=null?PhaseForHealth(health.Ratio):1;
        public bool Enraged=>FightPhase==2;
        int announcedPhase=1;
        float AttackInterval=>Enraged?Mathf.Max(4.6f,attackInterval*.9f):Mathf.Max(1f,attackInterval);
        public static AttackKind AttackForPhase(int phase,int serial)
        {
            if(phase<2)return (AttackKind)(serial%4);
            switch(serial%4){case 0:return AttackKind.MissileSalvo;case 1:return AttackKind.CrossBombing;case 2:return AttackKind.FlameRun;default:return AttackKind.SpeedRun;}
        }
        public bool IsExposed=>CurrentState==State.Exposed;
        public int BombsReleased { get; private set; }
        public int MissilesLaunched { get; private set; }
        public int FlameZonesCreated { get; private set; }
        public int RushHits { get; private set; }
        public string AttackName=>CurrentAttack==AttackKind.MissileSalvo?"TWIN MISSILE SALVO":CurrentAttack==AttackKind.CrossBombing?"CROSS CARPET BOMBING":CurrentAttack==AttackKind.HighBombing?"HIGH-ALTITUDE BOMBING":CurrentAttack==AttackKind.SpeedRun?"SUPERSONIC STRAFE":CurrentAttack==AttackKind.Missiles?"MISSILE LOCK":"NAPALM RUN";
        public string Hint=>IsDead?"BOMBARDINO DOWN":IsExposed?"COUNTER WINDOW — AIM FOR THE ENGINES!":CurrentState==State.Circling?"CIRCLING — WATCH FOR THE NEXT ATTACK!":CurrentState==State.Telegraph?
            CurrentAttack==AttackKind.HighBombing?"BOMBING INCOMING — MOVE OUT OF THE TARGET AREA!":CurrentAttack==AttackKind.SpeedRun?"LOW PASS — DODGE SIDEWAYS!":CurrentAttack==AttackKind.Missiles||CurrentAttack==AttackKind.MissileSalvo?"MISSILE SALVO — KEEP MOVING!":CurrentAttack==AttackKind.CrossBombing?"CROSS BOMBING — MOVE BETWEEN THE PASSES!":"NAPALM INCOMING — KEEP MOVING!":AttackName;
        Health health;
        PlayerHealth targetHealth;
        Vector3 home, committed, heading;
        readonly BossTargetPrediction prediction = new BossTargetPrediction();
        Quaternion visualRest;
        float clock, age, acquireAt, shotAt, deathGround, nextMarkerAt;
        float turnBank;
        float impactAge=1,impactStrength=1;
        Vector3 impactLocal=Vector3.back;
        float orbitAngle, attackAt, nextAttackAt;
        int attackCounter, shots;
        bool initialized,crashed;
        readonly List<BombardinoOrdnance> ordnance=new List<BombardinoOrdnance>();
        readonly List<GameObject> effects=new List<GameObject>();

        readonly List<LineRenderer> markers=new List<LineRenderer>();
        Material warningMaterial;
        const int MarkerSegments=32;
        readonly Vector3[] markerPoints=new Vector3[MarkerSegments+1];
        readonly HashSet<PlayerHealth> rushVictims=new HashSet<PlayerHealth>();
        readonly RaycastHit[] casts=new RaycastHit[96];
        Renderer[] bodyRenderers;
        Collider[] bodyColliders;
        CharacterController[] playerBodies=new CharacterController[0];
        readonly List<(Collider body, Collider player)> ignoredPlayerPairs=new List<(Collider, Collider)>();
        AudioSource audioSource;
        float protectPlayersAt;
        Bounds launchBounds;
        readonly List<(Vector3 point,float at,float damage)> fireImpacts=new List<(Vector3,float,float)>();
        public Vector3 WeaponOrigin(bool nose, int side = 0)
        {
            Vector3 local=nose?new Vector3(launchBounds.center.x,launchBounds.center.y,launchBounds.max.z):
                new Vector3(launchBounds.center.x+launchBounds.extents.x*.52f*side,launchBounds.min.y-.3f,launchBounds.center.z+launchBounds.extents.z*.15f);
            return transform.TransformPoint(local);
        }
        public Vector3 FlameContact => Ground(transform.position);

        void Awake()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;initialized=true;health=GetComponent<Health>();
            if(GetComponent<BossWeaknessSetup>()==null)gameObject.AddComponent<BossWeaknessSetup>();
            BossCombatVfx.PrewarmImpacts();
            health.OnDamaged??=new UnityEvent<float>();health.OnDeath??=new UnityEvent();health.OnDamaged.AddListener(OnHit);health.OnDeath.AddListener(OnDeath);
            Physics.SyncTransforms();home=Ground(transform.position);visualRest=visual!=null?visual.localRotation:Quaternion.identity;
            bodyRenderers=visual!=null?visual.GetComponentsInChildren<Renderer>():new Renderer[0];
            launchBounds=new Bounds(Vector3.zero,Vector3.one*2);
            foreach(var r in bodyRenderers){var b=r.bounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)launchBounds.Encapsulate(transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));}
            if(GetComponent<BombardinoFlameJet>()==null)gameObject.AddComponent<BombardinoFlameJet>();
            ConfigureBodyCollision(false);
            bodyColliders=GetComponentsInChildren<Collider>(true);
            CombatBodyCollision.Ensure(gameObject);
            ProtectPlayersFromBodyCollision();
            protectPlayersAt=clock+2f;
            if(effectMaterial==null)effectMaterial=Resources.Load<Material>("StoryBattle/Sea");
            if(GameAudioPolicy.SoundEnabled){audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=.75f;audioSource.minDistance=8;audioSource.maxDistance=100;}
            transform.position=ClearGround(OrbitPoint(orbitAngle)+Vector3.up*cruiseAltitude);
            transform.rotation=Quaternion.LookRotation(OrbitTangent(orbitAngle));
            if(!BossAttackWarning.HiddenOnIsland(gameObject))
            {
                warningMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                warningMaterial.SetColor("_BaseColor",new Color(1,.24f,.05f));
                for(int i=0;i<8;i++)
                {
                    var go=new GameObject("Bombardino attack warning");go.transform.SetParent(transform,false);
                    var line=go.AddComponent<LineRenderer>();line.sharedMaterial=warningMaterial;line.positionCount=MarkerSegments+1;
                    line.startWidth=line.endWidth=.13f;line.startColor=line.endColor=Color.white;
                    line.useWorldSpace=true;line.enabled=false;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.receiveShadows=false;markers.Add(line);
                }
            }
            if(health.IsDead)OnDeath();
        }
        void OnHit(float damage){if(!FightActive&&CurrentState!=State.Dead)acquireAt=clock;impactAge=0;}
        public void ApplyCombatImpact(Vector3 direction,float strength)
        {
            impactLocal=transform.InverseTransformDirection(direction.normalized);
            impactStrength=Mathf.Clamp(strength,.3f,1.8f);impactAge=0;
        }
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if(!initialized)Initialize();if(PauseSettingsMenu.IsOpen||deltaTime<=0)return;
            float dt=Mathf.Min(deltaTime,.1f);clock+=dt;age+=dt;
            if(FightActive&&!FirstIslandBossProgression.CanChallenge(this,FirstIslandObjectives.BossKind.Bombardino))
            {FightActive=false;ClearHazards();SetState(State.Returning);}
            impactAge=Mathf.Min(.3f,impactAge+dt);
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
            if(FightActive && FightPhase>announcedPhase && CurrentState!=State.Attack)
            {announcedPhase=FightPhase;BossPhaseVfx.For(gameObject).Burst(WeaponOrigin(true),true);}
            if(clock>=protectPlayersAt){protectPlayersAt=clock+2f;ProtectPlayersFromBodyCollision();}
            if(clock>=acquireAt){acquireAt=clock+.5f;Acquire();}
            if(FightActive&&(targetHealth==null||targetHealth.currentHealth<=0||Vector3.Distance(Ground(target.position),home)>leashRadius))
            {FightActive=false;SetState(State.Returning);ClearHazards();}
            prediction.Observe(target);
            // Schedule warning + attack against the previous attack's start, so travel,
            // burst length and the enraged phase never add an extra cooling delay.
            if(FightActive&&CurrentState!=State.Telegraph&&CurrentState!=State.Attack&&
                (!IsExposed || age>=counterWindowSeconds)&&clock>=nextAttackAt-Windup)
            {
                attackCounter++;StartAttack(AttackForPhase(FightPhase,attackCounter));
            }
            // Ground clearance must use this frame's banking pose, including scaled wings.
            if(visual!=null)
            {
                float roll=CurrentState==State.Attack && CurrentAttack==AttackKind.CrossBombing?360*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/AttackDuration)):0;
                float bank=turnBank+Mathf.Sin(clock*1.9f)*1.8f+roll;
                float hit=Mathf.Sin(Mathf.Clamp01(impactAge/.04f)*Mathf.PI*.5f)*Mathf.Pow(1-impactAge/.3f,1.6f)*impactStrength;
                visual.localRotation=Quaternion.Euler(Mathf.Sin(clock*2.4f)*1.8f-impactLocal.z*hit*7f,0,bank+impactLocal.x*hit*8f)*visualRest;
            }
            switch(CurrentState)
            {
                case State.Patrol:
                case State.Circling:
                    FlyOrbit(cruiseAltitude,cruiseSpeed,dt);break;
                case State.Exposed:
                    FlyOrbit(Mathf.Min(cruiseAltitude,18f),cruiseSpeed*.45f,dt);break;
                case State.Returning:
                    FlyOrbit(cruiseAltitude,cruiseSpeed,dt);
                    if(age>=1f){health.currentHealth=health.maxHealth;attackCounter=0;announcedPhase=1;nextAttackAt=0;SetState(State.Patrol);}break;
                case State.Telegraph:
                    var from=transform.position;
                    FlyOrbit(AttackAltitude,cruiseSpeed,dt);
                    if (target && attackAt - clock > Windup * .4f) committed = PredictedGround(attackAt - clock);
                    UpdateMarkers();
                    if(clock>=attackAt)
                    {
                        SetState(State.Attack);shotAt=0;shots=0;
                        nextAttackAt=attackAt+AttackInterval;
                        RunAttack(dt,from);
                    }
                    break;
                case State.Attack:UpdateMarkers();RunAttack(dt);break;
            }
        }
        void Acquire()
        {
            if(!FirstIslandBossProgression.CanChallenge(this,FirstIslandObjectives.BossKind.Bombardino))return;
            if(CurrentState==State.Dead||CurrentState==State.Returning)return;
            if(target==null||targetHealth==null||targetHealth.currentHealth<=0)
            {
                target=null;targetHealth=null;float best=engageDistance*engageDistance;
                foreach(var player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))if(player.currentHealth>0&&player.isActiveAndEnabled){float d=(Ground(player.transform.position)-home).sqrMagnitude;if(d<best){best=d;target=player.transform;targetHealth=player;}}
                prediction.Observe(target);
            }
            if(!FightActive&&targetHealth!=null&&(Ground(target.position)-home).sqrMagnitude<engageDistance*engageDistance){FightActive=true;StartAttack(FightPhase==2?AttackKind.MissileSalvo:AttackKind.HighBombing);}
        }
        void StartAttack(AttackKind kind)
        {
            if(!FirstIslandBossProgression.CanChallenge(this,FirstIslandObjectives.BossKind.Bombardino))return;
            if(target==null||IsDead)return;CurrentAttack=kind;
            prediction.Observe(target); committed=PredictedGround(Windup);
            heading=OrbitTangent(orbitAngle);
            attackAt=FightActive&&nextAttackAt>clock?nextAttackAt:clock+Windup;
            rushVictims.Clear();SetState(State.Telegraph);BuildMarkers(kind);
            GameplayAudio.Play(GameplayAudio.Cue.Warning,target.position);
        }
        Vector3 PredictedGround(float remainingWindup)
        {
            float flight = Mathf.Sqrt(Mathf.Max(.5f, AttackAltitude) * 2 / 25f);
            return Ground(prediction.PointAfter(Mathf.Min(.85f, remainingWindup + flight * .5f), transform));
        }
        internal Vector3 PredictMissilePoint(Vector3 origin, float speed)
        {
            prediction.Observe(target);
            return prediction.BodyIntercept(origin, speed, transform);
        }
        float Windup=>Mathf.Min(Mathf.Max(.1f,telegraphSeconds),AttackInterval*.5f);
        float AttackAltitude=>CurrentAttack==AttackKind.MissileSalvo?cruiseAltitude*.65f:CurrentAttack==AttackKind.CrossBombing?cruiseAltitude*.8f:CurrentAttack==AttackKind.HighBombing?cruiseAltitude+8:
            CurrentAttack==AttackKind.Missiles?cruiseAltitude*.6f:CurrentAttack==AttackKind.SpeedRun?5:12;
        float AttackSpeed=>CurrentAttack==AttackKind.SpeedRun?rushSpeed*(Enraged?1.2f:1):cruiseSpeed;
        float AttackDuration=>CurrentAttack==AttackKind.MissileSalvo?2.4f:CurrentAttack==AttackKind.CrossBombing?2.5f:CurrentAttack==AttackKind.SpeedRun?1.6f:CurrentAttack==AttackKind.FlameRun?2.4f:
            CurrentAttack==AttackKind.Missiles?1.8f:1.5f;
        void SetState(State state)
        {
            CurrentState=state;age=0;
            if(CurrentState==State.Circling || CurrentState==State.Exposed)ClearMarkers();
        }
        void RunAttack(float dt)=>RunAttack(dt,FlyAttack(dt));
        Vector3 FlyAttack(float dt)
        {
            var from=transform.position;FlyOrbit(AttackAltitude,AttackSpeed,dt);return from;
        }
        void RunAttack(float dt,Vector3 from)
        {
            float power=Enraged?1.15f:1;
            if(CurrentAttack==AttackKind.MissileSalvo)
            {
                if(age>=shotAt && shots<4)
                {
                    for(int side=-1;side<=1;side+=2)
                    {
                        Vector3 mount=WeaponOrigin(false,side);
                        Vector3 aim=committed+Vector3.up*2+Vector3.Cross(Vector3.up,heading)*(side*(2+shots));
                        Spawn(BombardinoOrdnance.Kind.Missile,mount,(aim-mount).normalized,aim,missileDamage*power,3f);
                        MissilesLaunched++;BossPhaseVfx.For(gameObject).Burst(mount,true);
                    }
                    shots++;shotAt+=.4f;
                }
            }
            else if(CurrentAttack==AttackKind.CrossBombing)
            {
                if(age>=shotAt && shots<8)
                {
                    Vector3 side=Vector3.Cross(Vector3.up,heading);
                    Vector3 point=committed+heading*((shots%4-1.5f)*4)+side*(shots<4?-5:5);
                    point=Ground(point);
                    Spawn(BombardinoOrdnance.Kind.Bomb,WeaponOrigin(false,shots%2==0?-1:1),Vector3.down,point,bombDamage*power,3.2f);
                    BombsReleased++;shots++;shotAt+=.23f;
                }
            }
            else if(CurrentAttack==AttackKind.HighBombing)
            {
                if(age>=shotAt&&shots<6){var ground=Ground(committed+heading*((shots-2.5f)*3));Spawn(BombardinoOrdnance.Kind.Bomb,WeaponOrigin(false,shots%2==0?-1:1),Vector3.down,ground,bombDamage*power,4.3f);BombsReleased++;shots++;shotAt+=.22f;}
            }
            else if(CurrentAttack==AttackKind.Missiles)
            {
                if(age>=shotAt&&shots<3){Vector3 mount=WeaponOrigin(false,shots%2==0?-1:1);Vector3 aim=target!=null?PredictMissilePoint(mount,36):committed;Spawn(BombardinoOrdnance.Kind.Missile,mount,(aim-mount).normalized,aim,missileDamage*power,3.5f);MissilesLaunched++;shots++;shotAt+=.6f;}
            }
            else
            {
                if(CurrentAttack==AttackKind.SpeedRun)
                {
                    // Keep the low-pass attack volume on its ground strip even when
                    // the physical aircraft clears a player's head safely.
                    var strikeFrom=Ground(from)+Vector3.up*2.6f;
                    var strikeTo=Ground(transform.position)+Vector3.up*2.6f;
                    var step=strikeTo-strikeFrom;int count=Physics.SphereCastNonAlloc(strikeFrom,2.6f,step.normalized,casts,step.magnitude,~0,QueryTriggerInteraction.Ignore);
                    for(int i=0;i<count;i++){var hp=casts[i].collider.GetComponentInParent<PlayerHealth>();if(hp!=null&&rushVictims.Add(hp)){if(hp.ApplyAttackDamage(rushDamage*power,casts[i].point,strikeFrom))RushHits++;}}
                }
                else if(age>=shotAt&&shots<7)
                {
                    // Burn the strip the aircraft actually crosses, after the visible jet reaches it.
                    Vector3 point=FlameContact;float flight=Vector3.Distance(point,WeaponOrigin(true))/30f;
                    fireImpacts.Add((point,clock+flight,fireDamage*power));shots++;shotAt=age+.23f;
                }
            }
            if(age>=AttackDuration)
            {
                SetState(State.Exposed);
                nextAttackAt=Mathf.Max(nextAttackAt,clock+counterWindowSeconds+Windup);
            }
        }
        void Spawn(BombardinoOrdnance.Kind kind,Vector3 point,Vector3 direction,Vector3 aim,float damage,float radius)
        {
            var go=new GameObject("Bombardino "+kind);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);go.transform.position=point;
            var payload=go.AddComponent<BombardinoOrdnance>();payload.Launch(this,kind,direction,target,aim,damage,radius);
        }
        public void RegisterOrdnance(BombardinoOrdnance payload){ordnance.RemoveAll(p=>p==null);if(payload!=null&&!ordnance.Contains(payload))ordnance.Add(payload);}
        Vector3 OrbitTangent(float angle)=>new Vector3(Mathf.Cos(angle),0,-Mathf.Sin(angle));
        Vector3 OrbitPoint(float angle,float radiusOffset=0)=>Ground(home+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(Mathf.Max(10f,patrolRadius)+radiusOffset));
        void FlyOrbit(float altitude,float speed,float dt)
        {
            orbitAngle=Mathf.Repeat(orbitAngle+Mathf.Max(1f,speed)/Mathf.Max(10f,patrolRadius)*dt,Mathf.PI*2);
            var point=OrbitPoint(orbitAngle)+Vector3.up*altitude;
            point.y=Mathf.MoveTowards(transform.position.y,point.y,cruiseSpeed*dt);
            turnBank=Mathf.Lerp(turnBank,-Mathf.Clamp(speed*.8f,12,28),1-Mathf.Exp(-4*dt));
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(OrbitTangent(orbitAngle)),180*dt);
            transform.position=ClearGround(point);
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
                        // Keep the movement capsule solid and weapon triggers hittable.
                        // Animated anatomical hurtboxes must not snag the flying body.
                        if(!CombatBodyCollision.IsAnatomy(collider))continue;
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
            ClearMarkers();
            if(markers.Count==0)return;
            int count=kind==AttackKind.CrossBombing || kind==AttackKind.MissileSalvo?8:kind==AttackKind.HighBombing?6:1;
            if(kind==AttackKind.SpeedRun || kind==AttackKind.FlameRun)
            {
                float start=orbitAngle+cruiseSpeed/Mathf.Max(10f,patrolRadius)*Mathf.Max(0,attackAt-clock);
                float sweep=AttackSpeed/Mathf.Max(10f,patrolRadius)*AttackDuration;
                float width=kind==AttackKind.SpeedRun?2.6f:2.8f;
                const int segments=15;
                for(int i=0;i<=segments;i++)markerPoints[i]=OrbitPoint(start+sweep*i/segments,width)+Vector3.up*.15f;
                for(int i=0;i<=segments;i++)markerPoints[segments+1+i]=OrbitPoint(start+sweep*(segments-i)/segments,-width)+Vector3.up*.15f;
                markerPoints[MarkerSegments]=markerPoints[0];
                markers[0].SetPositions(markerPoints);markers[0].enabled=true;return;
            }
            for(int n=0;n<count;n++)
            {
                var line=markers[n];
                Vector3 offset=kind==AttackKind.CrossBombing?heading*((n%4-1.5f)*4)+Vector3.Cross(Vector3.up,heading)*(n<4?-5:5):
                    kind==AttackKind.MissileSalvo?Vector3.Cross(Vector3.up,heading)*((n%2==0?-1:1)*(2+n/2)):
                    heading*((n-(count-1)*.5f)*(kind==AttackKind.HighBombing?3:3.8f));
                var point=Ground(committed+offset)+Vector3.up*.15f;
                float radius=kind==AttackKind.HighBombing?4.3f:kind==AttackKind.CrossBombing?3.2f:kind==AttackKind.MissileSalvo?3f:3.5f;
                for(int i=0;i<=MarkerSegments;i++){float a=i/(float)MarkerSegments*Mathf.PI*2;markerPoints[i]=Ground(point+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius))+Vector3.up*.15f;}
                line.SetPositions(markerPoints);line.enabled=true;
            }
        }
        void UpdateMarkers()
        {
            if(CurrentState!=State.Telegraph || clock<nextMarkerAt)return;
            nextMarkerAt=clock+.1f;BuildMarkers(CurrentAttack);
        }
        void ClearMarkers(){foreach(var line in markers)if(line!=null)line.enabled=false;GetComponent<BossAttackWarning>()?.Clear();}
        public void Explosion(Vector3 p,float radius)
        {
            var go=new GameObject("Bombardino impact smoke");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);go.transform.position=p;
            var fx=go.AddComponent<BombardinoBlastFX>();fx.Initialize(radius);effects.RemoveAll(e=>e==null);effects.Add(go);
            if(audioSource!=null){var clip=Resources.Load<AudioClip>("StoryBattle/Audio/TidalHit");if(clip!=null)audioSource.PlayOneShot(clip,.6f);}
        }
        void OnDeath(){if(CurrentState==State.Dead)return;FightActive=false;SetState(State.Dead);deathGround=Ground(transform.position).y;ClearHazards();foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;onDefeated.Invoke();}
        void ClearHazards(){fireImpacts.Clear();ClearMarkers();foreach(var p in ordnance)if(p!=null)Release(p.gameObject);ordnance.Clear();foreach(var e in effects)if(e!=null)Release(e);effects.Clear();}
        void OnDisable(){ClearHazards();if(audioSource!=null)audioSource.Stop();}
        void OnDestroy(){ClearHazards();if(warningMaterial!=null)Release(warningMaterial);foreach(var pair in ignoredPlayerPairs)if(pair.body!=null&&pair.player!=null)Physics.IgnoreCollision(pair.body,pair.player,false);if(health!=null){health.OnDamaged?.RemoveListener(OnHit);health.OnDeath?.RemoveListener(OnDeath);}}
        static void Release(Object o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}
