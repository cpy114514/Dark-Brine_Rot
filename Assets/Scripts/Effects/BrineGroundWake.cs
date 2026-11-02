using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    // Project once onto real collision surfaces; animate cached geometry thereafter.
    // Debris uses shaded, depth-writing solid geometry, not glowing particle quads.
    public sealed class BrineGroundWake : IDisposable
    {
        const int Sectors=24,Bands=4,PieceCount=7,PieceVertices=72;
        readonly Vector3[] rest=new Vector3[(Sectors+1)*Bands],surfaceNormals=new Vector3[(Sectors+1)*Bands],waveVertices=new Vector3[(Sectors+1)*Bands];
        readonly Vector2[] uv=new Vector2[(Sectors+1)*Bands];readonly Color[] waveColors=new Color[(Sectors+1)*Bands];readonly int[] triangles=new int[Sectors*(Bands-1)*6];
        readonly Vector3[] rocks=new Vector3[PieceCount*PieceVertices],rockNormals=new Vector3[PieceCount*PieceVertices];readonly Color[] rockColors=new Color[PieceCount*PieceVertices];
        readonly int[] rockTriangles=new int[PieceCount*PieceVertices];
        readonly Vector3[] origins=new Vector3[PieceCount],radials=new Vector3[PieceCount],settled=new Vector3[PieceCount];readonly Quaternion[] rotations=new Quaternion[PieceCount];
        readonly Vector3[] corners=new Vector3[6];readonly RaycastHit[] hits=new RaycastHit[32];
        readonly Mesh waveMesh,rockMesh;readonly MeshRenderer wave,stone;readonly Transform root;readonly BrineVfxStyle style;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock(),stoneBlock=new MaterialPropertyBlock();
        float age,radius,power;Color soil;bool timber;
        public bool Active{get;private set;}
        public BrineGroundWake(Transform parent,BrineVfxStyle style,int index)
        {
            this.style=style;waveMesh=new Mesh{name="Projected terrain compression "+index};waveMesh.MarkDynamic();
            wave=BrineVfxGeometry.Renderer("Pooled terrain wake "+index,parent,waveMesh,style.surface,out var go);root=go.transform;
            rockMesh=new Mesh{name="Seven solid stratified chips "+index};rockMesh.MarkDynamic();
            stone=BrineVfxGeometry.Renderer("Pooled shaded ground chips "+index,root,rockMesh,style.stone,out _);
            stone.shadowCastingMode=ShadowCastingMode.On;stone.receiveShadows=true;
            int k=0;for(int i=0;i<Sectors;i++)for(int j=0;j<Bands-1;j++)
            {
                int a=i*Bands+j,b=a+Bands;triangles[k++]=a;triangles[k++]=b;triangles[k++]=a+1;triangles[k++]=a+1;triangles[k++]=b;triangles[k++]=b+1;
            }
            for(int i=0;i<rockTriangles.Length;i++)rockTriangles[i]=i;
        }
        bool Surface(Vector3 point,out RaycastHit best)
        {
            int n=Physics.RaycastNonAlloc(point+Vector3.up*3,Vector3.down,hits,8,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            best=default;float distance=float.MaxValue;
            for(int i=0;i<n;i++)
            {
                var h=hits[i];if(!h.collider||h.normal.y<.55f||h.distance>=distance||h.point.y>point.y+.35f)continue;
                if(h.collider.GetComponentInParent<CharacterController>()||h.collider.GetComponentInParent<Health>()||h.collider.GetComponentInParent<NailongHealth>()||h.collider.GetComponentInParent<PlayerHealth>())continue;
                distance=h.distance;best=h;
            }
            return distance<float.MaxValue;
        }
        public bool Begin(Vector3 point,float radius,float power)
        {
            if(!Surface(point,out var ground)||point.y-ground.point.y>3.5f)return false;
            root.SetPositionAndRotation(ground.point,Quaternion.identity);this.radius=radius;this.power=power;age=0;soil=new Color(.23f,.22f,.19f);
            var renderer=ground.collider.GetComponent<Renderer>();
            string surfaceName=((renderer&&renderer.sharedMaterial?renderer.sharedMaterial.name:"")+" "+ground.collider.name).ToLowerInvariant();
            timber=surfaceName.Contains("wood")||surfaceName.Contains("timber")||surfaceName.Contains("deck")||surfaceName.Contains("plank");
            if(renderer&&renderer.sharedMaterial&&renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                Color tint=renderer.sharedMaterial.GetColor("_BaseColor");if(tint.maxColorComponent<.85f)soil=Color.Lerp(soil,tint,.55f);
            }
            for(int i=0;i<=Sectors;i++)for(int j=0;j<Bands;j++)
            {
                float angle=i/(float)Sectors*Mathf.PI*2,r=.06f+j/(float)(Bands-1)*radius*(.72f+.2f*Mathf.Sin(angle*3+.7f)+.08f*Mathf.Sin(angle*7));
                var delta=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*r;
                int k=i*Bands+j;var sample=ground.point+delta;
                bool valid=Surface(sample,out var hit)&&Mathf.Abs(hit.point.y-ground.point.y)<Mathf.Max(.45f,delta.magnitude*.65f);
                rest[k]=(valid?hit.point:sample)-root.position+(valid?hit.normal:ground.normal)*.025f;
                surfaceNormals[k]=valid?hit.normal:ground.normal;
                uv[k]=new Vector2(i/(float)Sectors,j/(float)(Bands-1));waveColors[k]=new Color(1,1,1,valid?1:0);
            }
            for(int i=0;i<PieceCount;i++)
            {
                int sector=Mathf.FloorToInt(Mathf.Repeat(i*.618034f,1)*Sectors),k=sector*Bands+3;
                Vector3 candidate=root.position+rest[k]*(.28f+Mathf.Repeat(i*.371f+.13f,1)*.65f);
                if(Surface(candidate,out var source)){origins[i]=source.point-root.position;rotations[i]=Quaternion.FromToRotation(Vector3.up,source.normal);}
                else{origins[i]=rest[k];rotations[i]=Quaternion.FromToRotation(Vector3.up,surfaceNormals[k]);}
                radials[i]=Vector3.ProjectOnPlane(origins[i],Vector3.up).normalized;rotations[i]*=Quaternion.Euler(0,i*137.5f,0);
                float speed=.95f+(i%4)*.2f;
                Vector3 destination=root.position+origins[i]+radials[i]*(speed/6.5f*.45f);
                settled[i]=Surface(destination,out var landing)?landing.point-root.position:origins[i];
            }
            waveMesh.Clear();waveMesh.vertices=rest;waveMesh.normals=surfaceNormals;waveMesh.uv=uv;waveMesh.colors=waveColors;waveMesh.triangles=triangles;
            rockMesh.Clear();rockMesh.vertices=rocks;rockMesh.normals=rockNormals;rockMesh.colors=rockColors;rockMesh.triangles=rockTriangles;
            Active=true;Tick(.001f);return true;
        }
        public void Tick(float dt)
        {
            age+=dt;float u=age/.9f;
            if(u>=1){Clear();return;}
            float travel=1-Mathf.Pow(1-Mathf.Clamp01(age/.48f),2);
            for(int i=0;i<rest.Length;i++)
            {
                float r=uv[i].y,crest=Mathf.Exp(-Mathf.Pow((r-travel)/.13f,2));
                float lobe=.4f+.6f*Mathf.Pow(.5f+.5f*Mathf.Sin(uv[i].x*19+1.4f),2);
                waveVertices[i]=rest[i]+surfaceNormals[i]*(crest*(.05f+.20f*lobe)*power*Mathf.Sin(Mathf.PI*Mathf.Clamp01(age/.55f)));
            }
            waveMesh.SetVertices(waveVertices);waveMesh.RecalculateBounds();
            block.SetFloat("_Mode",2);block.SetFloat("_Age",u);block.SetFloat("_Clock",Time.time);
            block.SetFloat("_Heat",power>1.3f?1:0);
            block.SetFloat("_Fade",style.visibility*Mathf.Sin(Mathf.PI*Mathf.Clamp01(u*1.4f))*.7f);
            block.SetColor("_Body",soil);block.SetColor("_Edge",new Color(.39f,.34f,.24f));wave.SetPropertyBlock(block);wave.enabled=age<.64f;
            for(int i=0;i<PieceCount;i++)
            {
                float t=Mathf.Max(0,age-.018f*(i%3)),speed=.95f+(i%4)*.2f;
                float y=Mathf.Max(0,speed*t-6.5f*t*t),slide=Mathf.Min(t,speed/6.5f)*.45f;
                Vector3 position=Vector3.Lerp(origins[i],settled[i],Mathf.Clamp01(slide/(speed/6.5f*.45f)))+Vector3.up*(y+.018f);
                var rotation=rotations[i]*Quaternion.Euler(Mathf.Min(t,.3f)*180,0,Mathf.Min(t,.3f)*50*(i%2==0?1:-1));
                float size=(.045f+(i%4)*.013f)*Mathf.Min(radius,2)*Mathf.Sqrt(power);
                int cursor=i*PieceVertices;
                for(int j=0;j<6;j++){float a=j*Mathf.PI/3;corners[j]=new Vector3(Mathf.Cos(a)*(1+.20f*Mathf.Sin(i+j*3)),.04f*Mathf.Sin(i+j*2),Mathf.Sin(a)*(timber?.20f:.65f));}
                for(int j=0;j<6;j++)
                {
                    int next=(j+1)%6;Vector3 top=Vector3.up*.18f,low=-Vector3.up*.17f;
                    Triangle(ref cursor,top,corners[j]+top,corners[next]+top,position,rotation,size,i);
                    Triangle(ref cursor,low,corners[next]+low,corners[j]+low,position,rotation,size,i);
                    Triangle(ref cursor,corners[j]+top,corners[j]+low,corners[next]+top,position,rotation,size,i);
                    Triangle(ref cursor,corners[next]+top,corners[j]+low,corners[next]+low,position,rotation,size,i);
                }
            }
            rockMesh.SetVertices(rocks);rockMesh.SetNormals(rockNormals);rockMesh.SetColors(rockColors);rockMesh.RecalculateBounds();
            stoneBlock.SetFloat("_Life",Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.9f,age)));stone.SetPropertyBlock(stoneBlock);stone.enabled=true;
        }
        void Triangle(ref int cursor,Vector3 a,Vector3 b,Vector3 c,Vector3 position,Quaternion rotation,float size,int piece)
        {
            Vector3 normal=rotation*Vector3.Cross(c-a,b-a).normalized;Color color=soil*(.8f+(piece%4)*.09f);
            rocks[cursor]=position+rotation*(a*size);rockNormals[cursor]=normal;rockColors[cursor++]=color;
            rocks[cursor]=position+rotation*(c*size);rockNormals[cursor]=normal;rockColors[cursor++]=color;
            rocks[cursor]=position+rotation*(b*size);rockNormals[cursor]=normal;rockColors[cursor++]=color;
        }
        public void Clear(){Active=false;if(wave)wave.enabled=false;if(stone)stone.enabled=false;}
        public void Dispose()=>Dispose(false);
        public void Dispose(bool immediate)
        {BrineVfxGeometry.Release(waveMesh,immediate);BrineVfxGeometry.Release(rockMesh,immediate);if(root)BrineVfxGeometry.Release(root.gameObject,immediate);}
    }
}
