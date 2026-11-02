using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Mavis
{
    // One bounded pool for the whole scene, independent of the number of combatants.
    public sealed class BrineVfxWorld : MonoBehaviour
    {
        sealed class Contact
        {
            public MeshRenderer renderer,dustRenderer,smokeRenderer;public Transform root,dustRoot,smokeRoot;public readonly MaterialPropertyBlock block=new MaterialPropertyBlock(),dustBlock=new MaterialPropertyBlock(),smokeBlock=new MaterialPropertyBlock();
            public bool active,dust;public float age,duration,power,heat;public int mode,owner;public CombatMaterial surface;
        }
        sealed class Flash{public Light light;public float age,duration,peak;}
        static BrineVfxWorld instance;
        Contact[] contacts;BrineGroundWake[] grounds;BrineContactFragments[] fragments;readonly Flash[] flashes=new Flash[2];
        BrineVfxStyle style;Mesh contactMesh,lightMesh,blastMesh,chargeMesh,smokeMesh;Camera viewer;int frame=-1,emittedThisFrame,qualityLevels;
        bool ReducedQuality=>qualityLevels>1&&QualitySettings.GetQualityLevel()==0;
        UniversalAdditionalCameraData leasedDepth;CameraOverrideOption previousDepth,previousColor;bool colorLeased;
        public int Accepted {get;private set;}
        public int Dropped {get;private set;}
        public int ActiveCount{get{int count=0;if(contacts!=null)foreach(var p in contacts)if(p.active)count++;return count;}}
        public int ActiveGroundCount{get{int count=0;if(grounds!=null)foreach(var g in grounds)if(g.Active)count++;return count;}}
        public BrineVfxStyle Style=>style;
        public int ActiveFragmentCount{get{int n=0;if(fragments!=null)foreach(var f in fragments)if(f.Active)n++;return n;}}
        public Mesh ChargeMesh=>chargeMesh;
        public static BrineVfxWorld Get()
        {
            if(instance)return instance;
            var style=BrineVfxStyle.Load();if(!style||!style.surface||!style.stone||!style.blast)return null;
            var go=new GameObject("Brine combat VFX • bounded world pool");
            DontDestroyOnLoad(go);instance=go.AddComponent<BrineVfxWorld>();instance.Initialize(style);return instance;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetInstance(){instance=null;}
        void Initialize(BrineVfxStyle source)
        {
            style=source;contactMesh=BrineVfxGeometry.Veils();lightMesh=BrineVfxGeometry.Veils(false,2);blastMesh=BrineVfxGeometry.VolumeBounds();chargeMesh=BrineVfxGeometry.Veils(true);
            qualityLevels=QualitySettings.names.Length;
            smokeMesh=BrineSmokeDetail.CreateMesh();
            contacts=new Contact[Mathf.Clamp(style.contactCapacity,8,32)];grounds=new BrineGroundWake[Mathf.Clamp(style.groundCapacity,2,12)];
            for(int i=0;i<contacts.Length;i++)
            {
                var renderer=BrineVfxGeometry.Renderer("Pooled folded pressure "+i,transform,contactMesh,style.surface,out var obj);
                var dust=BrineVfxGeometry.Renderer("Pooled contact dust volume "+i,transform,blastMesh,style.blast,out var dustObject);
                contacts[i]=new Contact{renderer=renderer,root=obj.transform,dustRenderer=dust,dustRoot=dustObject.transform};
                if(style.smokeDetail)
                {
                    var smoke=BrineVfxGeometry.Renderer("Pooled six-way secondary smoke "+i,transform,smokeMesh,style.smokeDetail,out var smokeObject);
                    contacts[i].smokeRenderer=smoke;contacts[i].smokeRoot=smokeObject.transform;
                }
            }
            for(int i=0;i<grounds.Length;i++)grounds[i]=new BrineGroundWake(transform,style,i);
            fragments=new BrineContactFragments[6];
            for(int i=0;i<fragments.Length;i++)fragments[i]=new BrineContactFragments(transform,style.stone,i);
            for(int i=0;i<flashes.Length;i++)
            {
                var obj=new GameObject("Bounded contact bounce light "+i);obj.transform.SetParent(transform,false);
                var light=obj.AddComponent<Light>();light.type=LightType.Point;light.shadows=LightShadows.None;light.enabled=false;
                flashes[i]=new Flash{light=light};
            }
            SceneManager.sceneUnloaded+=SceneUnloaded;
        }
        bool Near(Vector3 point)
        {
            if(!viewer||!viewer.isActiveAndEnabled)viewer=Camera.main;
            return !viewer||(viewer.transform.position-point).sqrMagnitude<style.maximumDistance*style.maximumDistance;
        }
        public bool Emit(int owner,Vector3 point,Vector3 direction,float radius,float power,bool heavy,bool blocked=false,bool blast=false,float durationScale=1,CombatMaterial surface=CombatMaterial.Flesh,Vector3 cutTangent=default,bool ambientDust=false)
        {
            if(!isActiveAndEnabled||power<=0||Time.timeScale<=0||!Near(point))return false;
            if(frame!=Time.frameCount){frame=Time.frameCount;emittedThisFrame=0;}
            if(emittedThisFrame>=4){Dropped++;return false;}
            int owned=0,large=0,dustCount=0;foreach(var c in contacts)if(c.active){if(c.owner==owner)owned++;if(c.mode==3)large++;if(c.dust)dustCount++;}
            if(ambientDust&&dustCount>=3){Dropped++;return false;}
            if((owner!=0&&owned>=CombatImpactVisuals.PulseCapacity)||(blast&&large>=2)){Dropped++;return false;}
            Contact p=null;foreach(var candidate in contacts)if(!candidate.active){p=candidate;break;}
            if(p==null){Dropped++;return false;}
            p.active=true;p.age=0;p.duration=ambientDust?.7f:blast?1.4f:heavy?.55f:blocked?.20f:.34f;p.power=Mathf.Clamp(power,0,1.5f);
            p.duration*=Mathf.Clamp(durationScale,.5f,2);p.surface=surface;
            p.mode=ambientDust?5:blast?3:1;p.heat=blast?1:blocked||surface==CombatMaterial.Metal?.85f:heavy?.35f:.1f;p.owner=owner;
            Vector3 forward=direction.sqrMagnitude>.001f?direction.normalized:Vector3.forward;
            p.root.SetPositionAndRotation(point,blast?Quaternion.identity:Quaternion.LookRotation(forward,cutTangent.sqrMagnitude>.001f?cutTangent.normalized:Vector3.up));
            p.root.localScale=Vector3.one*Mathf.Clamp(radius*style.scale,.15f,15);
            p.dust=ambientDust||!blast&&!blocked&&surface!=CombatMaterial.Metal&&dustCount<3;
            p.dustRoot.SetPositionAndRotation(point+forward*.08f,Quaternion.identity);
            p.dustRoot.localScale=Vector3.one*(radius*style.scale*(heavy?1.2f:.85f));
            p.dustRenderer.enabled=false;
            if(p.smokeRenderer)
            {
                p.smokeRenderer.enabled=false;p.smokeRoot.position=point;
                p.smokeRoot.localScale=Vector3.one*(radius*style.scale);
            }
            p.renderer.GetComponent<MeshFilter>().sharedMesh=blast?blastMesh:heavy?contactMesh:lightMesh;
            p.renderer.sharedMaterial=blast&&style.blast?style.blast:style.surface;
            Apply(p);Accepted++;emittedThisFrame++;
            if(blast||blocked||surface==CombatMaterial.Metal||surface==CombatMaterial.Wood)
                foreach(var f in fragments)if(!f.Active){f.Begin(owner,point,forward,radius,p.power,blocked?CombatMaterial.Metal:surface,blast);break;}
            if(heavy&&!blocked)Ground(point,Mathf.Clamp(radius*(blast?1.15f:1.6f),.8f,12),blast?1.5f:1);
            if(blast||blocked)FlashAt(point+(blast?Vector3.up*.6f:forward*.2f),radius,blast?.75f:.18f,blast?.22f:.065f);
            return true;
        }
        public bool Ground(Vector3 point,float radius,float power)
        {
            if(!Near(point)||Time.timeScale<=0)return false;
            foreach(var ground in grounds)if(!ground.Active)return ground.Begin(point,radius,power);
            return false;
        }
        void FlashAt(Vector3 position,float radius,float intensity,float duration)
        {
            foreach(var f in flashes)if(!f.light.enabled)
            {
                f.age=0;f.duration=duration;f.peak=intensity;f.light.color=Color.Lerp(Color.white,style.contact,.65f);
                f.light.range=Mathf.Clamp(radius*2,2,9);f.light.transform.position=position;f.light.intensity=0;f.light.enabled=true;return;
            }
        }
        public int CountOwner(int id){int n=0;foreach(var p in contacts)if(p.active&&p.owner==id)n++;return n;}
        public void ClearOwner(int id){foreach(var p in contacts)if(p.owner==id){p.active=false;p.renderer.enabled=false;p.dustRenderer.enabled=false;if(p.smokeRenderer)p.smokeRenderer.enabled=false;}foreach(var f in fragments)if(f.Owner==id)f.Clear();}
        void Apply(Contact p)
        {
            float dustAge=Mathf.Clamp01(p.age/p.duration);
            float u=Mathf.Clamp01(p.age/(p.mode==3?1.05f:!p.dust?p.duration:p.duration*.45f));
            // Finite attack, hold and dissolution phases. Shader folds keep moving inside the envelope.
            float envelope=Mathf.SmoothStep(0,1,u/(p.mode==3?.065f:.04f))*(1-Mathf.SmoothStep(.10f,1,u));
            p.block.SetFloat("_Mode",p.mode);p.block.SetFloat("_Age",u);p.block.SetFloat("_Clock",Time.time);
            p.block.SetFloat("_Fade",style.visibility*p.power*envelope);p.block.SetFloat("_Heat",p.heat);
            p.block.SetFloat("_Steps",ReducedQuality?24:style.volumeSteps);
            p.block.SetFloat("_Dust",0);
            p.block.SetColor("_FireCore",style.fireCore);p.block.SetColor("_FireBody",style.fireBody);p.block.SetColor("_Smoke",style.smoke);
            p.block.SetColor("_Body",p.surface==CombatMaterial.Wood?new Color(.19f,.145f,.09f):style.body);
            p.block.SetColor("_Edge",p.surface==CombatMaterial.Metal?new Color(.95f,.57f,.19f):p.surface==CombatMaterial.Wood?new Color(.73f,.55f,.30f):new Color(.95f,.90f,.72f));
            var data=viewer?viewer.GetComponent<UniversalAdditionalCameraData>():null;
            if((p.mode==3||p.dust)&&data)
            {
                if(leasedDepth!=data){ReleaseDepth();leasedDepth=data;previousDepth=data.requiresDepthOption;previousColor=data.requiresColorOption;}
                data.requiresDepthTexture=true;
                if(p.mode==3&&p.age<1.05f&&style.heatRefraction&&style.heatRefractionPixels>0&&!ReducedQuality){data.requiresColorTexture=true;colorLeased=true;}
            }
            p.block.SetFloat("_DepthAvailable",data&&data.requiresDepthTexture?1:0);
            p.block.SetFloat("_ColorAvailable",p.mode==3&&style.heatRefraction&&style.heatRefractionPixels>0&&colorLeased&&data==leasedDepth?1:0);
            p.block.SetFloat("_RefractionPixels",style.heatRefractionPixels);
            p.renderer.SetPropertyBlock(p.block);p.renderer.enabled=u<1&&p.mode!=5;
            if(p.smokeRenderer)
            {
                p.smokeRenderer.enabled=p.mode==3&&style.smokeDetailOpacity>0;
                if(p.smokeRenderer.enabled)
                {
                    Vector3 facing=viewer?Vector3.ProjectOnPlane(viewer.transform.position-p.smokeRoot.position,Vector3.up):Vector3.back;
                    if(facing.sqrMagnitude>.001f)p.smokeRoot.rotation=Quaternion.LookRotation(facing);
                    p.smokeBlock.SetFloat("_Age",dustAge);p.smokeBlock.SetFloat("_Fade",style.smokeDetailOpacity*style.visibility*p.power);
                    p.smokeBlock.SetFloat("_DepthAvailable",data&&data.requiresDepthTexture?1:0);
                    p.smokeRenderer.SetPropertyBlock(p.smokeBlock);
                }
            }
            if(p.dust)
            {
                p.dustBlock.SetFloat("_Dust",1);p.dustBlock.SetFloat("_Age",dustAge);p.dustBlock.SetFloat("_Fade",p.power*style.visibility);
                p.dustBlock.SetFloat("_Steps",ReducedQuality?16:20);
                p.dustBlock.SetFloat("_DepthAvailable",data&&data.requiresDepthTexture?1:0);
                p.dustBlock.SetColor("_Smoke",p.surface==CombatMaterial.Wood?new Color(.29f,.22f,.14f):new Color(.34f,.31f,.25f));
                p.dustRenderer.SetPropertyBlock(p.dustBlock);p.dustRenderer.enabled=true;
            }
        }
        static readonly Unity.Profiling.ProfilerMarker UpdateMarker=new Unity.Profiling.ProfilerMarker("BrineVFX.BoundedWorld");
        void Update()
        {
            if(contacts==null||Time.deltaTime<=0||PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput)return;
            using(UpdateMarker.Auto())
            {
                foreach(var p in contacts)if(p.active){p.age+=Time.deltaTime;if(p.age>=p.duration){p.active=false;p.renderer.enabled=false;p.dustRenderer.enabled=false;if(p.smokeRenderer)p.smokeRenderer.enabled=false;}else Apply(p);}
                foreach(var g in grounds)if(g.Active)g.Tick(Time.deltaTime);
                foreach(var f in fragments)if(f.Active)f.Tick(Time.deltaTime);
                bool volumeActive=false,blastActive=false;foreach(var p in contacts)if(p.active){if(p.mode==3||p.dust)volumeActive=true;if(p.mode==3&&p.age<1.05f&&style.heatRefraction&&style.heatRefractionPixels>0)blastActive=true;}
                if(!blastActive&&colorLeased){if(leasedDepth)leasedDepth.requiresColorOption=previousColor;colorLeased=false;}
                if(!volumeActive)ReleaseDepth();
                foreach(var f in flashes)if(f.light.enabled)
                {
                    f.age+=Time.deltaTime;float u=f.age/f.duration;
                    if(u>=1)f.light.enabled=false;else f.light.intensity=f.peak*Mathf.Sin(Mathf.PI*Mathf.Sqrt(u))*(1-u);
                }
            }
        }
        public void ClearAll()
        {
            if(contacts==null)return;foreach(var p in contacts){p.active=false;p.renderer.enabled=false;p.dustRenderer.enabled=false;if(p.smokeRenderer)p.smokeRenderer.enabled=false;}
            foreach(var g in grounds)g.Clear();foreach(var f in flashes)f.light.enabled=false;
            foreach(var f in fragments)f.Clear();
            ReleaseDepth();
        }
        void ReleaseDepth(){if(leasedDepth){leasedDepth.requiresDepthOption=previousDepth;if(colorLeased)leasedDepth.requiresColorOption=previousColor;}leasedDepth=null;colorLeased=false;}
        void SceneUnloaded(Scene scene)=>ClearAll();
        public void ReleaseBeforeReload()
        {
            ClearAll();if(grounds!=null)foreach(var g in grounds)g.Dispose(true);
            if(fragments!=null)foreach(var f in fragments)f.Dispose(true);
            BrineVfxGeometry.Release(contactMesh,true);BrineVfxGeometry.Release(lightMesh,true);BrineVfxGeometry.Release(blastMesh,true);BrineVfxGeometry.Release(chargeMesh,true);
            BrineVfxGeometry.Release(smokeMesh,true);
            DestroyImmediate(gameObject);
        }
        void OnDisable()=>ClearAll();
        void OnDestroy()
        {
            SceneManager.sceneUnloaded-=SceneUnloaded;if(instance==this)instance=null;
            ReleaseDepth();if(contactMesh)Destroy(contactMesh);if(lightMesh)Destroy(lightMesh);if(blastMesh)Destroy(blastMesh);if(chargeMesh)Destroy(chargeMesh);
            if(grounds!=null)foreach(var g in grounds)g.Dispose();
            if(smokeMesh)Destroy(smokeMesh);
            if(fragments!=null)foreach(var f in fragments)f.Dispose();
        }
    }
}
