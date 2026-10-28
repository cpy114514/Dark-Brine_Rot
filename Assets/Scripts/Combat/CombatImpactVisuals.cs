using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    // Closed pressure meshes and real weapon ribbons. Fixed pools; cosmetic only.
    public sealed class CombatImpactVisuals : MonoBehaviour
    {
        public const int PulseCapacity=8;
        [Range(0,1)] public float pressureVisibility=.85f;
        [Range(.15f,.5f)] public float impactDuration=.28f;
        sealed class Pulse
        {
            public Transform transform;public MeshRenderer renderer;public readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
            public Vector3 point;public Quaternion rotation;public Color tint;public float age,duration,start,end,visibility,depth;public bool active;
        }
        sealed class Ribbon
        {
            public Mesh mesh;public MeshRenderer renderer;public int count;public bool wasActive;
            public readonly Vector3[] a=new Vector3[16],b=new Vector3[16],vertices=new Vector3[32],normals=new Vector3[32];
            public readonly Color[] colours=new Color[32];public readonly Vector2[] uv=new Vector2[32];
            public readonly float[] times=new float[16];public readonly int[] indices=new int[90];
        }
        readonly Pulse[] pulses=new Pulse[PulseCapacity];
        readonly Ribbon[] ribbons=new Ribbon[2];
        Material ringMaterial,trailMaterial;Mesh ringMesh;Camera viewer;float nextFeatures;int spawnFrame=-1,frameEmissions;
        static readonly int Fade=Shader.PropertyToID("_Fade"),Tint=Shader.PropertyToID("_Tint");
        static readonly Unity.Profiling.ProfilerMarker TickMarker=new Unity.Profiling.ProfilerMarker("Combat.PressureMeshes");
        public int Emissions { get; private set; }
        public int LandingEmissions { get; private set; }
        public int RushEmissions { get; private set; }
        public int ActivePulseCount { get {int n=0;for(int i=0;i<pulses.Length;i++)if(pulses[i]!=null&&pulses[i].active)n++;return n;} }
        void Awake() => EnsurePool();
        void OnEnable() => EnsurePool();
        void EnsurePool()
        {
            // Script reload preserves native children but recreates these managed caches.
            // Reconnect the same bounded pool instead of leaking a second set of effects.
            if(ribbons[0]!=null && pulses[0]!=null)return;
            var existingRing=transform.Find("Pooled pressure ring 0");
            var existingTrail=transform.Find("Weapon pressure ribbon");
            if(existingRing)
            {
                ringMaterial=existingRing.GetComponent<MeshRenderer>().sharedMaterial;
                ringMesh=existingRing.GetComponent<MeshFilter>().sharedMesh;
            }
            if(existingTrail)trailMaterial=existingTrail.GetComponent<MeshRenderer>().sharedMaterial;
            var template=Resources.Load<Material>("Combat/PressureAir");
            if(!ringMaterial)ringMaterial=template?new Material(template):new Material(Shader.Find("DarkBrine/Combat Pressure"));
            ringMaterial.name="Combat pressure rings";
            if(!trailMaterial)trailMaterial=new Material(ringMaterial){name="Transparent weapon air trails"};trailMaterial.SetFloat("_Ribbon",1);trailMaterial.SetFloat("_Cull",0);
            if(!ringMesh)ringMesh=CreateRing();
            for(int i=0;i<pulses.Length;i++)
            {
                var child=transform.Find("Pooled pressure ring "+i);
                var go=child?child.gameObject:new GameObject("Pooled pressure ring "+i);go.transform.SetParent(transform,false);
                var filter=go.GetComponent<MeshFilter>();if(!filter)filter=go.AddComponent<MeshFilter>();filter.sharedMesh=ringMesh;
                var renderer=go.GetComponent<MeshRenderer>();if(!renderer)renderer=go.AddComponent<MeshRenderer>();Configure(renderer,ringMaterial);pulses[i]=new Pulse{transform=go.transform,renderer=renderer};
            }
            for(int i=0;i<ribbons.Length;i++)
            {
                string label=i==0?"Weapon pressure ribbon":"Offhand pressure ribbon";var child=transform.Find(label);
                var go=child?child.gameObject:new GameObject(label);go.transform.SetParent(transform,false);
                var filter=go.GetComponent<MeshFilter>();if(!filter)filter=go.AddComponent<MeshFilter>();
                var renderer=go.GetComponent<MeshRenderer>();if(!renderer)renderer=go.AddComponent<MeshRenderer>();
                var r=new Ribbon{mesh=filter.sharedMesh?filter.sharedMesh:new Mesh{name="Bounded weapon motion ribbon"},renderer=renderer};
                r.mesh.MarkDynamic();filter.sharedMesh=r.mesh;Configure(r.renderer,trailMaterial);
                for(int j=0;j<15;j++){int k=j*6,v=j*2;r.indices[k]=v;r.indices[k+1]=v+2;r.indices[k+2]=v+1;r.indices[k+3]=v+1;r.indices[k+4]=v+2;r.indices[k+5]=v+3;}
                ribbons[i]=r;
            }
            RefreshFeatures();
        }
        static void Configure(MeshRenderer renderer,Material material)
        {
            renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;renderer.enabled=false;
        }
        static Mesh CreateRing()
        {
            const int segments=48,tube=8,stride=tube+1;
            var vertices=new Vector3[(segments+1)*stride];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];var colours=new Color[vertices.Length];var indices=new int[segments*tube*6];
            for(int i=0;i<=segments;i++)for(int j=0;j<=tube;j++)
            {
                float u=i/(float)segments*Mathf.PI*2,v=j/(float)tube*Mathf.PI*2;int k=i*stride+j;
                Vector3 radial=new Vector3(Mathf.Cos(u),Mathf.Sin(u),0);normals[k]=radial*Mathf.Cos(v)+Vector3.forward*Mathf.Sin(v);
                vertices[k]=radial+normals[k]*.065f;uv[k]=new Vector2(i/(float)segments,j/(float)tube);colours[k]=Color.white;
                if(i==segments||j==tube)continue;int t=(i*tube+j)*6,b=k+stride;
                indices[t]=k;indices[t+1]=b;indices[t+2]=k+1;indices[t+3]=k+1;indices[t+4]=b;indices[t+5]=b+1;
            }
            var mesh=new Mesh{name="Closed reusable 3D combat pressure torus"};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.colors=colours;mesh.triangles=indices;mesh.RecalculateBounds();mesh.UploadMeshData(true);return mesh;
        }
        void RefreshFeatures()
        {
            viewer=Camera.main;bool colour=NailongCombatEffects.SceneColourAvailable,depth=NailongCombatEffects.SceneDepthAvailable;
            ringMaterial.SetFloat("_UseRefraction",colour?1:0);trailMaterial.SetFloat("_UseRefraction",colour?1:0);
            ringMaterial.SetFloat("_UseDepth",depth?1:0);trailMaterial.SetFloat("_UseDepth",depth?1:0);nextFeatures=Time.unscaledTime+.5f;
        }
        bool Emit(Vector3 point,Vector3 normal,float start,float end,float duration,float visibility,Color tint,float depth=1)
        {
            if(!isActiveAndEnabled||pressureVisibility<=0||visibility<=0||Time.timeScale<=0)return false;
            if(viewer&&(viewer.transform.position-point).sqrMagnitude>10000)return false;
            if(spawnFrame!=Time.frameCount){spawnFrame=Time.frameCount;frameEmissions=0;}if(frameEmissions>=4)return false;
            Pulse p=null;float oldest=-1;
            for(int i=0;i<pulses.Length;i++){var candidate=pulses[i];if(!candidate.active){p=candidate;break;}float age=candidate.age/candidate.duration;if(age>oldest){oldest=age;p=candidate;}}
            p.point=point;p.rotation=Quaternion.LookRotation(normal.sqrMagnitude>.001f?normal.normalized:Vector3.forward);p.start=start;p.end=end;p.duration=Mathf.Max(.08f,duration);p.age=0;p.visibility=Mathf.Clamp01(visibility);p.tint=tint;p.depth=depth;p.active=true;
            Emissions++;frameEmissions++;Apply(p);return true;
        }
        public void Impact(CombatHitEvent hit,CombatFeelProfile.Impact settings,CombatMaterial surface,float brightness)
        {
            if(settings.vfx<=0||brightness<=0)return;
            bool heavy=hit.result==CombatHitResult.HeavyHit,blocked=hit.result==CombatHitResult.Blocked;
            float radius=(blocked?.8f:hit.kind==CombatHitKind.Rifle?.55f:heavy?1.65f:.95f)*Mathf.Clamp(settings.vfx,.4f,1.5f);
            Color tint=surface==CombatMaterial.Metal?new Color(.94f,.95f,1):surface==CombatMaterial.Wood?new Color(.94f,.91f,.84f):new Color(.86f,.94f,1);
            // Hit direction enters the victim; pressure must leave that surface.
            Vector3 normal=hit.direction.sqrMagnitude>.001f?-hit.direction.normalized:-transform.forward;
            Emit(hit.point+normal*.12f,normal,.12f,radius,impactDuration*(heavy?1.15f:1),brightness,tint);
            if(heavy)Emit(hit.point+normal*.05f,normal,.08f,radius*.65f,impactDuration*.82f,brightness*.55f,tint,.65f);
        }
        public void Landing(Vector3 feet)
        {
            if(Emit(feet+Vector3.up*.13f,Vector3.up,.3f,2.5f,.38f,.85f,new Color(.9f,.94f,1),.35f))LandingEmissions++;
        }
        public void Rush(Vector3 centre,Vector3 direction)
        {
            if(Emit(centre,direction,.8f,2.15f,.32f,.65f,new Color(.86f,.94f,1),.65f))RushEmissions++;
        }
        void Apply(Pulse p)
        {
            float u=Mathf.Clamp01(p.age/p.duration),radius=Mathf.Lerp(p.start,p.end,1-Mathf.Pow(1-u,2.5f));
            p.transform.SetPositionAndRotation(p.point,p.rotation);var parent=transform.lossyScale;
            p.transform.localScale=new Vector3(radius/Mathf.Max(.001f,Mathf.Abs(parent.x)),radius/Mathf.Max(.001f,Mathf.Abs(parent.y)),radius*p.depth/Mathf.Max(.001f,Mathf.Abs(parent.z)));
            float fade=Mathf.SmoothStep(0,1,Mathf.Clamp01(u/.08f))*(1-u)*(1-u)*p.visibility*pressureVisibility;
            p.properties.SetFloat(Fade,fade);p.properties.SetColor(Tint,p.tint);p.renderer.SetPropertyBlock(p.properties);p.renderer.enabled=true;
        }
        void Update()
        {
            if(Time.unscaledTime>=nextFeatures)RefreshFeatures();
            if(Time.deltaTime<=0||PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput)return;
            using(var marker=TickMarker.Auto())for(int i=0;i<pulses.Length;i++)
            {
                var p=pulses[i];if(!p.active)continue;p.age+=Time.deltaTime;
                if(p.age>=p.duration){p.active=false;p.renderer.enabled=false;}else Apply(p);
            }
        }
        public void Sample(Vector3 a,Vector3 b,bool active,float lifetime,int hand=0)
        {
            if(!isActiveAndEnabled)return;
            var r=ribbons[Mathf.Clamp(hand,0,1)];float now=Time.time;
            if(active&&!r.wasActive)r.count=0;r.wasActive=active;
            while(r.count>0&&now-r.times[0]>lifetime)Shift(r);
            if(active&&(r.count==0||(r.b[r.count-1]-b).sqrMagnitude>.0001f))
            {
                if(r.count>0&&(r.b[r.count-1]-b).sqrMagnitude>64)r.count=0;if(r.count==16)Shift(r);
                r.a[r.count]=Vector3.Lerp(a,b,.35f);r.b[r.count]=b;r.times[r.count++]=now;
            }
            r.renderer.enabled=r.count>1&&pressureVisibility>0;if(r.count<2)return;
            for(int i=0;i<r.count;i++)
            {
                float life=Mathf.Clamp01(1-(now-r.times[i])/Mathf.Max(.01f,lifetime));Vector3 middle=(r.a[i]+r.b[i])*.5f;
                r.vertices[i*2]=transform.InverseTransformPoint(Vector3.Lerp(middle,r.a[i],life));r.vertices[i*2+1]=transform.InverseTransformPoint(Vector3.Lerp(middle,r.b[i],life));
                Vector3 motion=r.b[i]-r.b[i==0?Mathf.Min(1,r.count-1):i-1];Vector3 normal=Vector3.Cross(r.b[i]-r.a[i],motion).normalized;if(normal.sqrMagnitude<.01f)normal=transform.up;
                r.normals[i*2]=r.normals[i*2+1]=transform.InverseTransformDirection(normal);
                r.colours[i*2]=r.colours[i*2+1]=new Color(.94f,.97f,1,life*life*.65f*pressureVisibility);
                r.uv[i*2]=new Vector2(i/15f,0);r.uv[i*2+1]=new Vector2(i/15f,1);
            }
            r.mesh.Clear();r.mesh.SetVertices(r.vertices,0,r.count*2);r.mesh.SetColors(r.colours,0,r.count*2);r.mesh.SetUVs(0,r.uv,0,r.count*2);r.mesh.SetNormals(r.normals,0,r.count*2);r.mesh.SetIndices(r.indices,0,(r.count-1)*6,MeshTopology.Triangles,0,true);
        }
        static void Shift(Ribbon r){for(int i=1;i<r.count;i++){r.a[i-1]=r.a[i];r.b[i-1]=r.b[i];r.times[i-1]=r.times[i];}r.count--;}
        public void ClearTrail(){for(int i=0;i<ribbons.Length;i++){var r=ribbons[i];if(r==null)continue;r.count=0;r.wasActive=false;r.renderer.enabled=false;}}
        void OnDisable(){ClearTrail();for(int i=0;i<pulses.Length;i++)if(pulses[i]!=null){pulses[i].active=false;pulses[i].renderer.enabled=false;}}
        void OnDestroy()
        {
            if(ringMaterial)Destroy(ringMaterial);if(trailMaterial)Destroy(trailMaterial);if(ringMesh)Destroy(ringMesh);
            for(int i=0;i<ribbons.Length;i++)if(ribbons[i]!=null){if(ribbons[i].mesh)Destroy(ribbons[i].mesh);if(ribbons[i].renderer)Destroy(ribbons[i].renderer.gameObject);}
            for(int i=0;i<pulses.Length;i++)if(pulses[i]!=null&&pulses[i].transform)Destroy(pulses[i].transform.gameObject);
        }
    }
}
