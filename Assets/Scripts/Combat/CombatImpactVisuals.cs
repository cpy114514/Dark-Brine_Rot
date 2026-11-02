using UnityEngine;
namespace Mavis
{
    // Accepted contacts and final rendered weapon pose own all effect timing.
    [DefaultExecutionOrder(18000)]
    public sealed class CombatImpactVisuals : MonoBehaviour
    {
        public const int PulseCapacity=8;
        [Range(0,1)]public float pressureVisibility=.85f;
        [Range(.15f,.5f)]public float impactDuration=.28f;
        readonly BrineWeaponTrail[] trails=new BrineWeaponTrail[2];
        readonly Collider[] weapons=new Collider[2];readonly bool[] queuedActive=new bool[2];
        readonly float[] queuedLifetime=new float[2];readonly int[] queuedFrame=new int[2];
        MaterialPropertyBlock chargeBlock;
        BrineVfxWorld world;MeshRenderer chargeRenderer;Transform chargeRoot;
        CombatHitKind kind;bool charging;float progress,chargeFade;int configured=-1;
        readonly int[] sampledFrame=new[]{-1,-1};
        public int Emissions{get;private set;}
        public int LandingEmissions{get;private set;}
        public int RushEmissions{get;private set;}
        public int ActivePulseCount=>world?world.CountOwner(GetInstanceID()):0;
        public int TrailVertexCount=>(trails[0]?.VertexCount??0)+(trails[1]?.VertexCount??0);
        public bool ChargeVisible=>chargeRenderer&&chargeRenderer.enabled;
        void Awake()=>Ensure();
        bool Ensure()
        {
            if(world&&trails[0]!=null)return true;
            world=BrineVfxWorld.Get();if(!world)return false;
            chargeBlock=new MaterialPropertyBlock();
            for(int i=0;i<2;i++)trails[i]=new BrineWeaponTrail(transform,i==0?"Saltwind weapon loft":"Saltwind offhand loft",world.Style);
            chargeRenderer=BrineVfxGeometry.Renderer("Weapon-bound converging brine",transform,world.ChargeMesh,world.Style.surface,out var go);
            chargeRoot=go.transform;return true;
        }
        public void SetAttack(CombatHitKind attackKind,bool isCharging,float chargeProgress)
        {kind=attackKind;charging=isCharging;progress=Mathf.Clamp01(chargeProgress);configured=Time.frameCount;}
        public void Impact(CombatHitEvent hit,CombatFeelProfile.Impact settings,CombatMaterial surface,float brightness)
        {
            if(!isActiveAndEnabled||!Ensure()||settings.vfx<=0||brightness<=0)return;
            bool heavy=hit.result==CombatHitResult.HeavyHit||hit.kind==CombatHitKind.ChargedHeavy||hit.kind==CombatHitKind.ComboFinisher||hit.kind==CombatHitKind.JumpSlash;
            bool blocked=hit.result==CombatHitResult.Blocked;
            float radius=blocked?.55f:hit.kind==CombatHitKind.Rifle?.3f:heavy?1.3f:.78f;
            Vector3 normal=hit.direction.sqrMagnitude>.001f?-hit.direction.normalized:-transform.forward;
            Vector3 contact=hit.point;
            // Swing velocity is not the victim's surface normal. Using it as a
            // normal buries downward-cut effects inside the target geometry.
            var collider=hit.target?hit.target.GetComponent<Collider>():null;
            if(collider)
            {
                Vector3 outward=hit.point-collider.bounds.center;
                if(outward.sqrMagnitude>.005f)normal=outward.normalized;
                if(hit.attacker&&hit.kind!=CombatHitKind.Rifle&&hit.kind!=CombatHitKind.Boomerang&&!(hit.target is Story1SharkHealth))
                {
                    var attackerBody=hit.attacker.GetComponent<CharacterController>();
                    Vector3 source=attackerBody?attackerBody.bounds.center:hit.attacker.transform.position+Vector3.up;
                    Vector3 ray=hit.point-source;
                    if(ray.sqrMagnitude>.001f&&collider.Raycast(new Ray(source,ray.normalized),out var entry,ray.magnitude+.15f))
                    {contact=entry.point;normal=entry.normal;}
                }
            }
            Vector3 tangent=Vector3.ProjectOnPlane(hit.direction,normal);
            if(world.Emit(GetInstanceID(),contact+normal*.04f,normal,radius,pressureVisibility*settings.vfx*brightness,heavy,blocked,false,impactDuration/.28f,surface,tangent))Emissions++;
        }
        public void Landing(Vector3 feet)
        {
            if(isActiveAndEnabled&&pressureVisibility>0&&Ensure()&&world.Ground(feet,1.45f,pressureVisibility))LandingEmissions++;
        }
        public void Rush(Vector3 centre,Vector3 direction)
        {
            if(isActiveAndEnabled&&Ensure()&&world.Emit(GetInstanceID(),centre,-direction,.75f,pressureVisibility*.4f,false))RushEmissions++;
        }
        public void Sample(Vector3 a,Vector3 b,bool active,float lifetime,int hand=0)
        {
            if(!isActiveAndEnabled||!Ensure())return;
            int i=Mathf.Clamp(hand,0,1);sampledFrame[i]=Time.frameCount;trails[i].Configure(kind,pressureVisibility);trails[i].Sample(a,b,active,lifetime);
            if(i!=0)return;
            chargeFade=Mathf.MoveTowards(chargeFade,charging?1:0,Time.deltaTime/(charging?.16f:.14f));
            chargeRenderer.enabled=chargeFade>.001f&&pressureVisibility>0;if(!chargeRenderer.enabled)return;
            Vector3 axis=b-a;float length=axis.magnitude;
            chargeRoot.SetPositionAndRotation((a+b)*.5f,Quaternion.LookRotation(length>.001f?axis/length:transform.up));
            float radius=Mathf.Clamp(length*.15f,.12f,.42f);Vector3 s=transform.lossyScale;
            chargeRoot.localScale=new Vector3(radius/Mathf.Max(.001f,Mathf.Abs(s.x)),radius/Mathf.Max(.001f,Mathf.Abs(s.y)),Mathf.Max(.15f,length*.5f)/Mathf.Max(.001f,Mathf.Abs(s.z)));
            chargeBlock.SetFloat("_Mode",4);chargeBlock.SetFloat("_Clock",Time.time);chargeBlock.SetFloat("_Heat",progress);
            chargeBlock.SetFloat("_Fade",chargeFade*Mathf.SmoothStep(.08f,.65f,progress)*pressureVisibility*world.Style.visibility);
            chargeBlock.SetColor("_Body",world.Style.body);chargeBlock.SetColor("_Edge",world.Style.edge);chargeRenderer.SetPropertyBlock(chargeBlock);
        }
        public void QueueWeapon(Collider weapon,bool active,float lifetime,int hand=0)
        {int i=Mathf.Clamp(hand,0,1);weapons[i]=weapon;queuedActive[i]=active;queuedLifetime[i]=lifetime;queuedFrame[i]=Time.frameCount;}
        void LateUpdate()
        {
            if(Time.deltaTime<=0)return;if(configured<Time.frameCount-1)charging=false;
            // Final rig/cinematic correction passes run through order 16000.
            for(int i=0;i<2;i++)if(queuedFrame[i]==Time.frameCount&&weapons[i])
            {BrineVfxGeometry.WeaponEnds(weapons[i],transform.position,out var a,out var b);Sample(a,b,queuedActive[i],queuedLifetime[i],i);}
            for(int i=0;i<2;i++)if(sampledFrame[i]<Time.frameCount){trails[i]?.EndStroke();trails[i]?.Tick(Time.time);}
            if(sampledFrame[0]<Time.frameCount)
            {
                chargeFade=Mathf.MoveTowards(chargeFade,0,Time.deltaTime/.14f);
                if(chargeRenderer){chargeRenderer.enabled=chargeFade>.001f;chargeBlock.SetFloat("_Clock",Time.time);chargeBlock.SetFloat("_Fade",chargeFade*.4f*pressureVisibility);chargeRenderer.SetPropertyBlock(chargeBlock);}
            }
        }
        public void ClearTrail()
        {
            for(int i=0;i<2;i++){trails[i]?.Clear();queuedFrame[i]=-1;weapons[i]=null;sampledFrame[i]=-1;}
            charging=false;chargeFade=0;if(chargeRenderer)chargeRenderer.enabled=false;
        }
        void OnDisable(){ClearTrail();if(world)world.ClearOwner(GetInstanceID());}
        public void ReleaseBeforeReload()
        {
            ClearTrail();for(int i=0;i<trails.Length;i++){trails[i]?.Dispose(true);trails[i]=null;}
            if(chargeRoot)DestroyImmediate(chargeRoot.gameObject);chargeRenderer=null;chargeRoot=null;world=null;chargeBlock=null;
        }
        void OnDestroy(){foreach(var trail in trails)trail?.Dispose();if(chargeRoot)Destroy(chargeRoot.gameObject);}
    }
}
