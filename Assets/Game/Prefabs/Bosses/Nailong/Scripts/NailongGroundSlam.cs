using System.Collections.Generic;
using UnityEngine;
namespace Mavis
{
    public sealed class NailongGroundSlam : MonoBehaviour
    {
        const int Segments=64;
        const float HeightStep=.75f;
        public const float PulseDamage=15f;
        public const float JumpClearance=.6f;
        sealed class Pulse
        {
            public Mesh mesh;
            public MeshRenderer renderer;
            public readonly Vector3[] vertices=new Vector3[(Segments+1)*3];
            public readonly HashSet<PlayerHealth> crossed=new HashSet<PlayerHealth>();
            public readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
            public bool launched;
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
        float[,] heights;
        int heightBins;
        readonly RaycastHit[] groundHits=new RaycastHit[48];
        readonly Vector3[] directions=new Vector3[Segments];
        public int PulsesReleased {get;private set;}
        public int DamageHits {get;private set;}
        public int JumpDodges {get;private set;}
        public float Lifetime=>pulses==null?0:(pulses.Length-1)*interval+travelSeconds;
        public float PulseRadius(int index)=>radius*Mathf.Clamp01((age-index*interval)/travelSeconds);

        public void Initialize(NailongAttack source,float reach,int count=3,float spacing=1f,float duration=2.4f,float ringWidth=.4f)
        {
            owner=source;ownerHealth=source.GetComponent<NailongHealth>();radius=Mathf.Max(1,reach);
            interval=Mathf.Max(.4f,spacing);travelSeconds=Mathf.Max(.6f,duration);width=Mathf.Max(.1f,ringWidth);
            heightBins=Mathf.CeilToInt((radius+width)/HeightStep)+2;heights=new float[Segments,heightBins];
            for(int i=0;i<Segments;i++)
            {
                float angle=i/(float)Segments*Mathf.PI*2;directions[i]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                for(int j=0;j<heightBins;j++)heights[i,j]=float.NaN;
            }
            var template=Resources.Load<Material>("BossCombat/NailongSonar");
            material=template!=null?new Material(template):new Material(Shader.Find("DarkBrine/Sonar Ring"));
            material.name="Nailong sonar rings";
            var victims=FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);players=new PlayerSample[victims.Length];
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
            BossCombatVfx.Burst(transform.position,Vector3.up,new Color(.5f,.5f,.46f,.5f),12,2);
        }
        Pulse CreatePulse(int index)
        {
            var go=new GameObject("Expanding sonar ring "+(index+1));go.transform.SetParent(transform,false);
            var p=new Pulse{mesh=new Mesh{name="Terrain-following sonar ring"},renderer=go.AddComponent<MeshRenderer>()};
            p.mesh.MarkDynamic();go.AddComponent<MeshFilter>().sharedMesh=p.mesh;p.renderer.sharedMaterial=material;
            p.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;p.renderer.receiveShadows=false;p.renderer.enabled=false;
            var uv=new Vector2[p.vertices.Length];var normals=new Vector3[p.vertices.Length];var triangles=new int[Segments*12];
            for(int i=0;i<=Segments;i++)for(int row=0;row<3;row++){uv[i*3+row]=new Vector2(i/(float)Segments,row*.5f);normals[i*3+row]=Vector3.up;}
            for(int i=0;i<Segments;i++)for(int row=0;row<2;row++)
            {
                int a=i*3+row,b=a+3,k=(i*2+row)*6;
                triangles[k]=a;triangles[k+1]=a+1;triangles[k+2]=b;triangles[k+3]=a+1;triangles[k+4]=b+1;triangles[k+5]=b;
            }
            p.mesh.vertices=p.vertices;p.mesh.uv=uv;p.mesh.normals=normals;p.mesh.triangles=triangles;return p;
        }
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            if(PauseSettingsMenu.IsOpen||dt<=0||pulses==null)return;
            if(!owner||!owner.isActiveAndEnabled||!ownerHealth||ownerHealth.IsDead){Release();return;}
            float before=age;age+=dt;
            for(int i=0;i<pulses.Length;i++)
            {
                var p=pulses[i];float start=i*interval,elapsed=age-start;
                if(elapsed<0){p.renderer.enabled=false;continue;}
                if(before-start>=travelSeconds){p.renderer.enabled=false;continue;}
                if(!p.launched){p.launched=true;PulsesReleased++;if(audioSource)audioSource.PlayOneShot(ping,.45f);}
                float edge=radius*Mathf.Clamp01(elapsed/travelSeconds),previous=radius*Mathf.Clamp01((before-start)/travelSeconds);
                CheckCrossings(p,previous,edge,before,start,dt);
                p.renderer.enabled=elapsed<=travelSeconds;
                if(p.renderer.enabled)DrawPulse(p,edge,elapsed);
            }
            foreach(var player in players)if(player.health)player.previousFeet=Feet(player);
            if(age>Lifetime+.18f)Release();
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
                player.health.ApplyDamage(PulseDamage,contactFeet);DamageHits++;
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
            for(int i=0;i<=Segments;i++)for(int row=0;row<3;row++)
            {
                int sector=i%Segments;float r=Mathf.Max(0,edge+(row-1)*width*.5f);
                Vector3 point=directions[sector]*r;
                point.y=CachedHeight(sector,r)-transform.position.y+(row==1?.24f:.07f);
                pulse.vertices[i*3+row]=point;
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
