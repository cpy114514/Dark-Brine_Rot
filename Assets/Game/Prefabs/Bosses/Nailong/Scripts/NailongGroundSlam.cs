using System.Collections.Generic;
using UnityEngine;
namespace Mavis
{
    [DefaultExecutionOrder(510)]
    public sealed class NailongGroundSlam : MonoBehaviour
    {
        const int Segments=64;
        const int TubeSegments=12;
        const int TubeStride=TubeSegments+1;
        const float HeightStep=.75f;
        public const float PulseDamage=15f;
        public const float JumpClearance=.6f;
        sealed class Pulse
        {
            public Mesh mesh;
            public MeshRenderer renderer;
            public readonly Vector3[] vertices=new Vector3[(Segments+1)*TubeStride];
            public readonly HashSet<PlayerHealth> crossed=new HashSet<PlayerHealth>();
            public readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
            public bool launched;
            public Vector3 origin,forward;
        }
        sealed class PlayerSample
        {
            public PlayerHealth health;
            public Collider body;
            public Vector3 previousFeet;
        }
        NailongAttack owner;
        NailongHealth ownerHealth;
        Pulse[] pulses;
        PlayerSample[] players;
        Material material;
        AudioSource audioSource;
        static AudioClip ping;
        float radius,age,interval,travelSeconds,width;
        bool visualOnly;
        bool fromMouth;
        NailongAttackMotion mouthMotion;
        Transform voiceTarget;
        Collider voiceTargetBody;
        float voiceStartRadius;
        float voiceDamage=PulseDamage,voicePush;
        bool emissionStopped,voiceJumpable=true;
        const float VoiceSpread=.28f;
        [Header("Pressure-wave visibility")]
        [SerializeField, Range(1f,3f)] float visualWidthScale=1.8f;
        [Tooltip("Visible pressure-front height above terrain, in world metres. Independent of the jumpable ground-contact band.")]
        [SerializeField, Range(.3f,6f)] float crestHeight=3.2f;
        float[,] heights;
        int heightBins;
        readonly RaycastHit[] groundHits=new RaycastHit[48];
        readonly Vector3[] directions=new Vector3[Segments];
        readonly Vector2[] crossSection=new Vector2[TubeStride];
        public int PulsesReleased {get;private set;}
        public int DamageHits {get;private set;}
        public int JumpDodges {get;private set;}
        public float Lifetime=>pulses==null?0:(pulses.Length-1)*interval+travelSeconds;
        public float PulseRadius(int index)=>radius*Mathf.Clamp01((age-index*interval)/travelSeconds);
        public bool FromMouth=>fromMouth;

        public void InitializeVoice(NailongAttack source,NailongAttackMotion motion,Transform target,float reach,int count=3,float spacing=1f,float duration=2.4f,float ringWidth=.4f,bool presentationOnly=false,float damage=PulseDamage,float push=0,bool jumpable=true)
        {
            fromMouth=true;mouthMotion=motion;voiceTarget=target;
            voiceDamage=Mathf.Max(0,damage);voicePush=Mathf.Max(0,push);voiceJumpable=jumpable;
            voiceTargetBody=target?target.GetComponent<Collider>():null;
            voiceStartRadius=.24f*NailongSize.RangeFactor(source.transform);
            Initialize(source,reach,count,spacing,duration,ringWidth,presentationOnly);
        }
        public void StopEmission()=>emissionStopped=true;

        public void Initialize(NailongAttack source,float reach,int count=3,float spacing=1f,float duration=2.4f,float ringWidth=.4f,bool presentationOnly=false)
        {
            owner=source;ownerHealth=source.GetComponent<NailongHealth>();radius=Mathf.Max(1,reach);
            visualOnly=presentationOnly;
            interval=Mathf.Max(.4f,spacing);travelSeconds=Mathf.Max(.6f,duration);width=Mathf.Max(.1f,ringWidth);
            heightBins=Mathf.CeilToInt((radius+width)/HeightStep)+2;heights=new float[Segments,heightBins];
            for(int i=0;i<Segments;i++)
            {
                float angle=i/(float)Segments*Mathf.PI*2;
                directions[i]=fromMouth?new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0):new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                for(int j=0;j<heightBins;j++)heights[i,j]=float.NaN;
            }
            for(int j=0;j<=TubeSegments;j++)
            {
                float angle=(j%TubeSegments)/(float)TubeSegments*Mathf.PI*2;
                crossSection[j]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            }
            var template=Resources.Load<Material>("BossCombat/NailongSonar");
            material=template!=null?new Material(template):new Material(Shader.Find("DarkBrine/Sonar Ring"));
            material.name="Nailong sonar rings";
            material.SetColor("_Tint",new Color(.88f,.94f,1,1));
            material.SetFloat("_UseRefraction",NailongCombatEffects.SceneColourAvailable?1:0);
            material.SetFloat("_UseDepth",NailongCombatEffects.SceneDepthAvailable?1:0);
            var victims=visualOnly?System.Array.Empty<PlayerHealth>():FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);players=new PlayerSample[victims.Length];
            for(int i=0;i<victims.Length;i++)
            {
                var body=victims[i].GetComponent<CharacterController>();
                players[i]=new PlayerSample{health=victims[i],body=body!=null?(Collider)body:victims[i].GetComponent<Collider>()};
                players[i].previousFeet=Feet(players[i]);
            }
            pulses=new Pulse[Mathf.Clamp(count,1,6)];
            for(int i=0;i<pulses.Length;i++)pulses[i]=CreatePulse(i);
            if(GameAudioPolicy.SoundEnabled)
            {
                audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;
                audioSource.spatialBlend=1;audioSource.minDistance=5;audioSource.maxDistance=50;
                if(ping==null)
                {
                    var samples=new float[3308];
                    for(int i=0;i<samples.Length;i++){float t=i/22050f;float envelope=Mathf.Sin(Mathf.PI*i/samples.Length)*Mathf.Exp(-t*22);samples[i]=Mathf.Sin(Mathf.PI*2*(1050*t-1100*t*t))*.3f*envelope;}
                    ping=AudioClip.Create("Nailong sonar ping",samples.Length,1,22050,false);ping.SetData(samples,0);
                }
            }
            if(!visualOnly&&!fromMouth)BossCombatVfx.Burst(transform.position,Vector3.up,new Color(.5f,.5f,.46f,.5f),12,2);
        }
        Pulse CreatePulse(int index)
        {
            var go=new GameObject("Expanding sonar ring "+(index+1));go.transform.SetParent(transform,false);
            var p=new Pulse{mesh=new Mesh{name="Closed 3D pressure torus"},renderer=go.AddComponent<MeshRenderer>()};
            p.mesh.MarkDynamic();go.AddComponent<MeshFilter>().sharedMesh=p.mesh;p.renderer.sharedMaterial=material;
            p.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;p.renderer.receiveShadows=false;p.renderer.enabled=false;
            var uv=new Vector2[p.vertices.Length];var normals=new Vector3[p.vertices.Length];var triangles=new int[Segments*TubeSegments*6];
            float horizontal=fromMouth?width*.35f:width*visualWidthScale*.5f,vertical=fromMouth?horizontal:(crestHeight-.08f)*.5f;
            for(int i=0;i<=Segments;i++)for(int row=0;row<=TubeSegments;row++)
            {
                int vertex=i*TubeStride+row;Vector2 section=crossSection[row];
                uv[vertex]=new Vector2(i/(float)Segments,row/(float)TubeSegments);
                normals[vertex]=(directions[i%Segments]*(section.x/horizontal)+(fromMouth?Vector3.forward:Vector3.up)*(section.y/vertical)).normalized;
            }
            for(int i=0;i<Segments;i++)for(int row=0;row<TubeSegments;row++)
            {
                int a=i*TubeStride+row,b=a+TubeStride,k=(i*TubeSegments+row)*6;
                // XY mouth rings have the opposite parameter winding to the legacy XZ ground ring.
                triangles[k]=a;triangles[k+1]=fromMouth?b:a+1;triangles[k+2]=fromMouth?a+1:b;
                triangles[k+3]=a+1;triangles[k+4]=fromMouth?b:b+1;triangles[k+5]=fromMouth?b+1:b;
            }
            p.mesh.vertices=p.vertices;p.mesh.uv=uv;p.mesh.normals=normals;p.mesh.triangles=triangles;return p;
        }
        void Update(){if(!fromMouth)Tick(Time.deltaTime);}
        void LateUpdate(){if(fromMouth)Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(PauseSettingsMenu.IsOpen||dt<=0||pulses==null)return;
            if(!owner||!owner.isActiveAndEnabled||!ownerHealth||ownerHealth.IsDead){Release();return;}
            float before=age;age+=dt;
            for(int i=0;i<pulses.Length;i++)
            {
                var p=pulses[i];float start=i*interval,elapsed=age-start;
                if(emissionStopped&&!p.launched){p.renderer.enabled=false;continue;}
                if(elapsed<0){p.renderer.enabled=false;continue;}
                if(before-start>=travelSeconds){p.renderer.enabled=false;continue;}
                if(!p.launched)
                {
                    p.launched=true;PulsesReleased++;
                    if(fromMouth)
                    {
                        p.origin=mouthMotion?mouthMotion.MouthPosition:owner.transform.TransformPoint(new Vector3(0,1.9f,.6f));
                        Vector3 aim=voiceTargetBody?voiceTargetBody.bounds.center+Vector3.up*(voiceTargetBody.bounds.extents.y*.65f):voiceTarget?voiceTarget.position+Vector3.up: p.origin+owner.transform.forward;
                        Vector3 wanted=(aim-p.origin).sqrMagnitude>.001f?(aim-p.origin).normalized:owner.transform.forward;
                        // Shout toward the upper body, keeping the ring visibly upright instead of firing into the floor.
                        Vector3 horizontal=Vector3.ProjectOnPlane(wanted,Vector3.up);
                        if(horizontal.sqrMagnitude<.001f)horizontal=owner.transform.forward;
                        float pitch=Mathf.Clamp(Mathf.Asin(Mathf.Clamp(wanted.y,-1,1)),-12*Mathf.Deg2Rad,12*Mathf.Deg2Rad);
                        wanted=horizontal.normalized*Mathf.Cos(pitch)+Vector3.up*Mathf.Sin(pitch);
                        p.forward=Vector3.RotateTowards(owner.transform.forward,wanted,35*Mathf.Deg2Rad,0).normalized;
                        p.renderer.transform.SetPositionAndRotation(p.origin,Quaternion.LookRotation(p.forward,Vector3.up));
                    }
                    if(audioSource)audioSource.PlayOneShot(ping,.45f);
                }
                float edge=radius*Mathf.Clamp01(elapsed/travelSeconds),previous=radius*Mathf.Clamp01((before-start)/travelSeconds);
                if(!visualOnly){if(fromMouth)CheckVoiceCrossings(p,previous,edge,before,start,dt);else CheckCrossings(p,previous,edge,before,start,dt);}
                p.renderer.enabled=elapsed<=travelSeconds;
                if(p.renderer.enabled){if(fromMouth)DrawVoice(p,edge,elapsed);else DrawPulse(p,edge,elapsed);}
            }
            foreach(var player in players)if(player.health)player.previousFeet=Feet(player);
            if(age>Lifetime+.18f)Release();
        }
        void CheckVoiceCrossings(Pulse pulse,float previous,float edge,float before,float born,float dt)
        {
            foreach(var player in players)
            {
                if(!player.health||!player.health.isActiveAndEnabled||player.health.currentHealth<=0||pulse.crossed.Contains(player.health))continue;
                Vector3 feet=Feet(player),oldFeet=player.previousFeet;
                if(before<born)oldFeet=Vector3.Lerp(oldFeet,feet,Mathf.Clamp01((born-before)/dt));
                Vector3 centre=player.body&&player.body.enabled?player.body.bounds.center:feet+Vector3.up;
                Vector3 oldCentre=centre+oldFeet-feet;
                Vector3 extents=player.body&&player.body.enabled?player.body.bounds.extents:Vector3.one*.3f;
                float bodyDepth=Vector3.Dot(new Vector3(Mathf.Abs(pulse.forward.x),Mathf.Abs(pulse.forward.y),Mathf.Abs(pulse.forward.z)),extents);
                float thickness=width*.35f+bodyDepth;
                float from=Vector3.Dot(oldCentre-pulse.origin,pulse.forward)-previous,to=Vector3.Dot(centre-pulse.origin,pulse.forward)-edge;
                if(Mathf.Min(from,to)>thickness||Mathf.Max(from,to)<-thickness)continue;
                float boundary=from>thickness?thickness:from<-thickness?-thickness:from;
                float fraction=Mathf.Abs(to-from)>.0001f?Mathf.Clamp01((boundary-from)/(to-from)):0;
                Vector3 contact=Vector3.Lerp(oldCentre,centre,fraction),offset=contact-pulse.origin;
                float axial=Vector3.Dot(offset,pulse.forward);
                if(axial<0||axial>radius+thickness)continue;
                float discRadius=voiceStartRadius+Mathf.Max(0,Mathf.Lerp(previous,edge,fraction))*VoiceSpread;
                if(Vector3.ProjectOnPlane(offset,pulse.forward).magnitude>discRadius+extents.magnitude)continue;
                pulse.crossed.Add(player.health);
                float clearance=Vector3.Lerp(oldFeet,feet,fraction).y-GroundHeight(Vector3.Lerp(oldFeet,feet,fraction));
                if(voiceJumpable&&clearance>JumpClearance){JumpDodges++;continue;}
                if(clearance<-.4f||player.health.IsProtected)continue;
                Vector3 hit=player.body?player.body.ClosestPoint(pulse.origin+pulse.forward*Mathf.Lerp(previous,edge,fraction)):contact;
                if(!player.health.ApplyAttackDamage(voiceDamage,hit,pulse.origin))continue;
                DamageHits++;
                if(voicePush>0&&player.body is CharacterController controller&&controller.enabled&&player.health.currentHealth>0)
                {
                    var knockback=controller.GetComponent<CombatKnockback>();
                    if(!knockback)knockback=controller.gameObject.AddComponent<CombatKnockback>();
                    knockback.Push(Vector3.ProjectOnPlane(pulse.forward,Vector3.up).normalized*voicePush);
                }
                if(Camera.main)Camera.main.GetComponent<CombatCameraShake>()?.Pulse(.022f,.1f);
            }
        }
        void DrawVoice(Pulse pulse,float distance,float elapsed)
        {
            float ringRadius=voiceStartRadius+distance*VoiceSpread;
            float tube=Mathf.Min(width*.35f,ringRadius*.45f);
            for(int i=0;i<=Segments;i++)for(int row=0;row<=TubeSegments;row++)
            {
                Vector2 section=crossSection[row];
                pulse.vertices[i*TubeStride+row]=directions[i%Segments]*(ringRadius+section.x*tube)+Vector3.forward*(distance+section.y*tube);
            }
            pulse.mesh.vertices=pulse.vertices;pulse.mesh.RecalculateBounds();
            float opacity=Mathf.Min(Mathf.Clamp01(elapsed/.055f),Mathf.Clamp01((travelSeconds-elapsed)/.3f));
            pulse.properties.SetFloat("_Opacity",opacity);pulse.renderer.SetPropertyBlock(pulse.properties);
        }
        void CheckCrossings(Pulse pulse,float previous,float edge,float before,float born,float dt)
        {
            foreach(var player in players)
            {
                if(!player.health||!player.health.isActiveAndEnabled||player.health.currentHealth<=0||pulse.crossed.Contains(player.health))continue;
                Vector3 feet=Feet(player),oldFeet=player.previousFeet;
                if(before<born)oldFeet=Vector3.Lerp(oldFeet,feet,Mathf.Clamp01((born-before)/dt));
                float oldDistance=Vector3.ProjectOnPlane(oldFeet-transform.position,Vector3.up).magnitude;
                float distance=Vector3.ProjectOnPlane(feet-transform.position,Vector3.up).magnitude;
                float bodyRadius=player.body?Mathf.Max(player.body.bounds.extents.x,player.body.bounds.extents.z):.3f;
                float thickness=width*.5f+bodyRadius;
                float from=oldDistance-previous,to=distance-edge;
                if(Mathf.Min(from,to)>thickness || Mathf.Max(from,to)<-thickness)continue;
                pulse.crossed.Add(player.health);
                float boundary=from>thickness?thickness:from<-thickness?-thickness:from;
                float fraction=Mathf.Abs(to-from)>.0001f?Mathf.Clamp01((boundary-from)/(to-from)):0;
                Vector3 contactFeet=Vector3.Lerp(oldFeet,feet,fraction);
                float clearance=contactFeet.y-GroundHeight(contactFeet);
                if(clearance>JumpClearance){JumpDodges++;continue;}
                if(clearance<-.4f||player.health.IsProtected)continue;
                // Fixed sonar damage is independent of claw damage, rage, and collider count.
                if(!player.health.ApplyAttackDamage(PulseDamage,contactFeet,transform.position))continue;
                DamageHits++;
                if(Camera.main)Camera.main.GetComponent<CombatCameraShake>()?.Pulse(.022f,.1f);
            }
        }
        static Vector3 Feet(PlayerSample sample)
        {
            if(sample.body && sample.body.enabled)return new Vector3(sample.body.bounds.center.x,sample.body.bounds.min.y,sample.body.bounds.center.z);
            return sample.health?sample.health.transform.position:Vector3.zero;
        }
        void DrawPulse(Pulse pulse,float edge,float elapsed)
        {
            // A closed elliptical tube gives the wave an inner wall, outer wall and rounded crest.
            // Grow the tube out of the source without an inverted/self-intersecting torus at birth.
            float halfWidth=Mathf.Min(width*visualWidthScale*.5f,edge*.45f);
            float growth=halfWidth/(width*visualWidthScale*.5f);
            float halfHeight=(crestHeight-.08f)*.5f*growth;
            for(int i=0;i<=Segments;i++)for(int row=0;row<=TubeSegments;row++)
            {
                int sector=i%Segments;Vector2 section=crossSection[row];float r=edge+section.x*halfWidth;
                Vector3 point=directions[sector]*r;
                point.y=CachedHeight(sector,r)-transform.position.y+.08f+(1+section.y)*halfHeight;
                pulse.vertices[i*TubeStride+row]=point;
            }
            pulse.mesh.vertices=pulse.vertices;pulse.mesh.RecalculateBounds();
            float opacity=Mathf.Min(Mathf.Clamp01(elapsed/.07f),Mathf.Clamp01((travelSeconds-elapsed)/.22f));
            pulse.properties.SetFloat("_Opacity",opacity);pulse.renderer.SetPropertyBlock(pulse.properties);
        }
        float CachedHeight(int sector,float r)
        {
            float bin=r/HeightStep;int low=Mathf.Min(heightBins-2,Mathf.FloorToInt(bin)),high=low+1;
            if(float.IsNaN(heights[sector,low]))heights[sector,low]=GroundHeight(transform.position+directions[sector]*low*HeightStep);
            if(float.IsNaN(heights[sector,high]))heights[sector,high]=GroundHeight(transform.position+directions[sector]*high*HeightStep);
            return Mathf.Lerp(heights[sector,low],heights[sector,high],bin-low);
        }
        float GroundHeight(Vector3 point)
        {
            int count=Physics.RaycastNonAlloc(point+Vector3.up*20,Vector3.down,groundHits,60,~0,QueryTriggerInteraction.Ignore);
            float terrain=float.NegativeInfinity,best=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var c=groundHits[i].collider;
                if(c.transform.IsChildOf(owner.transform)||c.GetComponentInParent<IDamageable>()!=null)continue;
                if(c is TerrainCollider || c.GetComponentInParent<ProceduralIsland>()!=null){terrain=Mathf.Max(terrain,groundHits[i].point.y);continue;}
                if(groundHits[i].normal.y>.6f)best=Mathf.Max(best,groundHits[i].point.y);
            }
            return terrain>float.NegativeInfinity?terrain:best>float.NegativeInfinity?best:transform.position.y;
        }
        void Release(){if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);}
        void OnDisable(){if(pulses!=null)foreach(var p in pulses)if(p.renderer)p.renderer.enabled=false;if(audioSource)audioSource.Stop();}
        void OnDestroy(){if(pulses!=null)foreach(var p in pulses)if(p.mesh)Destroy(p.mesh);if(material)Destroy(material);}
    }
}
