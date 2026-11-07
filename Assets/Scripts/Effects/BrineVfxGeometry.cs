using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    // Meshes describe actual surfaces, not camera-facing billboards. UV2.x selects a rib.
    public static class BrineVfxGeometry
    {
        public static void Release(UnityEngine.Object item,bool immediate)
        {if(!item)return;if(immediate)UnityEngine.Object.DestroyImmediate(item);else UnityEngine.Object.Destroy(item);}
        public static void WeaponEnds(Collider collider,Vector3 actor,out Vector3 a,out Vector3 b)
        {
            if(collider is CapsuleCollider capsule)
            {
                Story1CapsuleContact.Segment(capsule,out a,out b,out float radius);
                Vector3 axis=(b-a).sqrMagnitude>.00001f?(b-a).normalized:capsule.transform.up;
                a-=axis*radius;b+=axis*radius;
            }
            else if(collider is BoxCollider box)
            {
                int axis=box.size.x>box.size.y&&box.size.x>box.size.z?0:box.size.y>box.size.z?1:2;
                Vector3 offset=Vector3.zero;offset[axis]=box.size[axis]*.5f;
                a=box.transform.TransformPoint(box.center-offset);b=box.transform.TransformPoint(box.center+offset);
            }
            else{var bounds=collider.bounds;a=b=bounds.center;a.y-=bounds.extents.y;b.y+=bounds.extents.y;}
            if((a-actor).sqrMagnitude>(b-actor).sqrMagnitude){var swap=a;a=b;b=swap;}
        }
        public static Mesh VolumeBounds()
        {
            var mesh=new Mesh{name="Thermal volume ray bounds"};
            mesh.vertices=new[]{new Vector3(-1.25f,-.1f,-1.25f),new Vector3(1.25f,-.1f,-1.25f),new Vector3(1.25f,2,-1.25f),new Vector3(-1.25f,2,-1.25f),
                new Vector3(-1.25f,-.1f,1.25f),new Vector3(1.25f,-.1f,1.25f),new Vector3(1.25f,2,1.25f),new Vector3(-1.25f,2,1.25f)};
            mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,0,1,5,0,5,4,3,7,6,3,6,2};
            mesh.RecalculateBounds();mesh.UploadMeshData(true);return mesh;
        }
        public static Mesh Veils(bool charge=false,int layers=5)
        {
            if(!charge)return ContactWake(layers);
            var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();
            var extra=new List<Vector2>();var indices=new List<int>();
            int ribs=charge?3:layers,steps=40,cols=7;
            for(int rib=0;rib<ribs;rib++)
            {
                int first=v.Count;
                for(int i=0;i<=steps;i++)for(int side=0;side<2;side++)for(int j=0;j<cols;j++)
                {
                    float u=i/(float)steps,w=j/(float)(cols-1),s=side==0?1:-1;
                    float taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*u)),.65f);
                    float angle=charge?(u*.9f*Mathf.PI+rib*2.0944f):Mathf.Lerp(-1.9f+rib*.62f,-.15f+rib*.49f,u);
                    // Sit just outside the real weapon, instead of burying the
                    // restrained charge fibres inside its opaque shaft.
                    float r=.70f+Mathf.Pow(u,3)*.10f;
                    r+=(w-.5f)*taper*.045f;
                    var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    float z=charge?Mathf.Lerp(-.8f,.8f,u):(.32f*Mathf.Sin(u*3.4f+rib*.23f)+u*.19f-rib*.045f);
                    float thickness=s*Mathf.Sin(w*Mathf.PI)*taper*(charge?.018f:.036f);
                    v.Add(radial*r+Vector3.forward*(z+thickness));n.Add((Vector3.forward*s+radial*.3f).normalized);
                    uv.Add(new Vector2(u,w));extra.Add(new Vector2(rib/(float)ribs,0));
                }
                int stride=cols*2;
                for(int i=0;i<steps;i++)for(int side=0;side<2;side++)for(int j=0;j<cols-1;j++)
                {
                    int a=first+i*stride+side*cols+j,b=a+stride;
                    if(side==0){indices.Add(a);indices.Add(b);indices.Add(a+1);indices.Add(a+1);indices.Add(b);indices.Add(b+1);}
                    else{indices.Add(a);indices.Add(a+1);indices.Add(b);indices.Add(a+1);indices.Add(b+1);indices.Add(b);}
                }
            }
            return Finish(charge?"Converging helical blade surfaces":"Five asymmetrical pressure crescents",v,n,uv,extra,indices);
        }
        static Mesh ContactWake(int layers)
        {
            var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var extra=new List<Vector2>();var ix=new List<int>();
            int strands=Mathf.Clamp(layers*2+1,5,11);const int steps=12,cols=3;
            for(int strand=0;strand<strands;strand++)
            {
                int first=v.Count;float seed=Mathf.Repeat(strand*.618034f+.17f,1),angle=strand*2.39996f;
                var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);var across=new Vector3(-radial.y,radial.x,0);
                for(int i=0;i<=steps;i++)for(int j=0;j<cols;j++)
                {
                    float u=i/(float)steps,w=j/(float)(cols-1);
                    // sin(PI) can be slightly negative in float precision.
                    // A fractional power otherwise injects NaNs at every tip.
                    float taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.8f);
                    float reach=.24f+seed*.7f;
                    var centre=radial*(.02f+u*reach)+Vector3.forward*(u*(.16f+seed*.24f)+Mathf.Sin(u*Mathf.PI)*.09f);
                    centre+=across*Mathf.Sin(u*3.8f+seed*5)*u*.045f;
                    float width=(.055f+seed*.075f)*taper;
                    v.Add(centre+across*(w-.5f)*width);n.Add(Vector3.Cross(across,radial+Vector3.forward*.7f).normalized);
                    uv.Add(new Vector2(u,w));extra.Add(new Vector2(seed,0));
                    if(i==steps||j==cols-1)continue;int k=first+i*cols+j;
                    ix.Add(k);ix.Add(k+cols);ix.Add(k+1);ix.Add(k+1);ix.Add(k+cols);ix.Add(k+cols+1);
                }
            }
            return Finish("Directional contact brush and ejected grit",v,n,uv,extra,ix);
        }
        public static Mesh Blast()
        {
            var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var extra=new List<Vector2>();var ix=new List<int>();
            const int sectors=28,rings=14;
            for(int lobe=0;lobe<6;lobe++)
            {
                int first=v.Count;float direction=lobe*Mathf.PI*.4f;
                Vector3 centre=lobe==0?new Vector3(0,.43f,0):new Vector3(Mathf.Cos(direction)*.42f,.18f+.06f*Mathf.Sin(direction*3),Mathf.Sin(direction)*.42f);
                Vector3 scale=lobe==0?new Vector3(.62f,.75f,.62f):new Vector3(.51f,.56f,.51f);
                for(int j=0;j<=rings;j++)for(int i=0;i<=sectors;i++)
                {
                    float u=i/(float)sectors,w=j/(float)rings,a=u*Mathf.PI*2,p=w*Mathf.PI;
                    float fold=1+.13f*Mathf.Sin(a*4+p*3+lobe)+.07f*Mathf.Sin(a*7-p*5);
                    var normal=new Vector3(Mathf.Cos(a)*Mathf.Sin(p),Mathf.Cos(p),Mathf.Sin(a)*Mathf.Sin(p));
                    v.Add(centre+Vector3.Scale(normal,scale)*fold);n.Add(normal);uv.Add(new Vector2(u,w));extra.Add(new Vector2(lobe/6f,0));
                    if(j==rings||i==sectors)continue;int k=first+j*(sectors+1)+i;
                    ix.Add(k);ix.Add(k+sectors+1);ix.Add(k+1);ix.Add(k+1);ix.Add(k+sectors+1);ix.Add(k+sectors+2);
                }
            }
            return Finish("Six interlocking curled thermal volumes",v,n,uv,extra,ix);
        }
        static Mesh Finish(string name,List<Vector3> v,List<Vector3> n,List<Vector2> uv,List<Vector2> extra,List<int> ix)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetUVs(1,extra);
            var colors=new Color[v.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;mesh.colors=colors;
            mesh.SetTriangles(ix,0);mesh.bounds=new Bounds(Vector3.zero,Vector3.one*3);mesh.UploadMeshData(true);return mesh;
        }
        public static MeshRenderer Renderer(string name,Transform parent,Mesh mesh,Material material,out GameObject go)
        {
            go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
            r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;r.enabled=false;return r;
        }
    }

    // A closed, cambered loft sampled in world space from the rendered weapon volume.
    // All staging buffers are allocated once; no List growth or LINQ in the frame loop.
    public sealed class BrineWeaponTrail : IDisposable
    {
        const int Samples=32,Cols=5,Stride=Cols*2;
        readonly Vector3[] a=new Vector3[Samples],b=new Vector3[Samples];readonly float[] times=new float[Samples],distance=new float[Samples];
        readonly bool[] breaks=new bool[Samples];
        readonly Vector3[] vertices=new Vector3[Samples*Stride],normals=new Vector3[Samples*Stride];
        readonly Vector2[] uv=new Vector2[Samples*Stride];readonly Color[] colours=new Color[Samples*Stride];
        readonly int[] indices=new int[(Samples-1)*(Cols-1)*12];
        readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        readonly Transform root;readonly Mesh mesh;readonly MeshRenderer renderer;readonly BrineVfxStyle style;
        int count;bool wasActive,newStroke,sharp;float strength=1,lifetime=.19f;CombatHitKind kind;
        public int SampleCount=>count;
        public int VertexCount=>renderer.enabled?count*Stride:0;
        public bool Visible=>renderer&&renderer.enabled;
        public BrineWeaponTrail(Transform parent,string label,BrineVfxStyle style)
        {
            this.style=style;mesh=new Mesh{name=label+" cambered loft"};mesh.MarkDynamic();
            renderer=BrineVfxGeometry.Renderer(label,parent,mesh,style.surface,out var go);root=go.transform;
            int t=0;
            for(int i=0;i<Samples-1;i++)for(int side=0;side<2;side++)for(int j=0;j<Cols-1;j++)
            {
                int x=i*Stride+side*Cols+j,y=x+Stride;
                if(side==0){indices[t++]=x;indices[t++]=y;indices[t++]=x+1;indices[t++]=x+1;indices[t++]=y;indices[t++]=y+1;}
                else{indices[t++]=x;indices[t++]=x+1;indices[t++]=y;indices[t++]=x+1;indices[t++]=y+1;indices[t++]=y;}
            }
        }
        public void Configure(CombatHitKind kind,float visibility=1,bool sharp=false){this.kind=kind;strength=visibility;this.sharp=sharp;}
        public void Sample(Vector3 start,Vector3 end,bool active,float seconds)
        {
            bool heavy=kind==CombatHitKind.ChargedHeavy||kind==CombatHitKind.ComboFinisher||kind==CombatHitKind.JumpSlash;
            lifetime=Mathf.Clamp(seconds*(heavy?.85f:.65f),.055f,heavy?.12f:.085f);if(sharp)lifetime*=.78f;float now=Time.time;
            while(count>0&&now-times[0]>lifetime)Shift();
            if(active&&!wasActive)newStroke=true;wasActive=active;
            if(active)
            {
                start=Vector3.Lerp(start,end,sharp?.72f:heavy?.46f:.62f);
                if(count>0&&(end-b[count-1]).sqrMagnitude>36)count=0;
                if(count==0||newStroke){Append(start,end,now,0,true);newStroke=false;}
                else
                {
                    float travel=Vector3.Distance(end,b[count-1]);
                    if(travel>.006f)
                    {
                        Vector3 fromA=a[count-1],fromB=b[count-1];float fromTime=times[count-1],fromDistance=distance[count-1];
                        int steps=Mathf.Clamp(Mathf.CeilToInt(travel/CombatVfxSettings.TrailSpacing),1,CombatVfxSettings.TrailSubsteps);
                        for(int i=1;i<=steps;i++){float t=i/(float)steps;Append(Vector3.Lerp(fromA,start,t),Vector3.Lerp(fromB,end,t),Mathf.Lerp(fromTime,now,t),fromDistance+travel*t);}
                    }
                }
            }
            Tick(now);
        }
        void Append(Vector3 x,Vector3 y,float t,float d,bool separate=false){if(count==Samples)Shift();a[count]=x;b[count]=y;times[count]=t;distance[count]=d;breaks[count++]=separate;}
        void Shift(){for(int i=1;i<count;i++){a[i-1]=a[i];b[i-1]=b[i];times[i-1]=times[i];distance[i-1]=distance[i];breaks[i-1]=breaks[i];}count--;}
        public void Tick(float now)
        {
            while(count>0&&now-times[0]>lifetime)Shift();
            renderer.enabled=count>1&&strength>0;if(!renderer.enabled)return;
            bool heavy=kind==CombatHitKind.ChargedHeavy||kind==CombatHitKind.ComboFinisher||kind==CombatHitKind.JumpSlash;
            for(int i=0;i<count;i++)
            {
                float age=Mathf.Clamp01((now-times[i])/lifetime),life=Mathf.Pow(1-age,1.35f);
                Vector3 tangent=b[Mathf.Min(count-1,i+1)]-b[Mathf.Max(0,i-1)],span=b[i]-a[i];
                Vector3 normal=Vector3.Cross(span,tangent).normalized;if(normal.sqrMagnitude<.01f)normal=Vector3.up;
                for(int side=0;side<2;side++)for(int j=0;j<Cols;j++)
                {
                    float w=j/(float)(Cols-1),sign=side==0?1:-1;int k=i*Stride+side*Cols+j;
                    float camber=Mathf.Sin(w*Mathf.PI)*(heavy?.018f:.007f)*life;
                    float leaf=Mathf.Pow(Mathf.Max(0,Mathf.Sin(age*Mathf.PI)),2.4f);
                    Vector3 heel=Vector3.Lerp(b[i],a[i],leaf);
                    Vector3 p=Vector3.Lerp(heel,b[i],w)+normal*sign*camber*leaf;
                    vertices[k]=root.InverseTransformPoint(p);
                    normals[k]=root.InverseTransformDirection((normal*sign-span.normalized*Mathf.Cos(w*Mathf.PI)*.42f).normalized);
                    uv[k]=new Vector2(distance[i]*.7f,w);colours[k]=new Color(1,1,1,life*Mathf.SmoothStep(0,1,(now-times[i])/.012f));
                }
            }
            mesh.Clear();mesh.SetVertices(vertices,0,count*Stride);mesh.SetNormals(normals,0,count*Stride);mesh.SetUVs(0,uv,0,count*Stride);
            int triangles=0;
            for(int i=1;i<count;i++)if(!breaks[i])for(int side=0;side<2;side++)for(int j=0;j<Cols-1;j++)
            {
                int x=(i-1)*Stride+side*Cols+j,y=x+Stride;
                if(side==0){indices[triangles++]=x;indices[triangles++]=y;indices[triangles++]=x+1;indices[triangles++]=x+1;indices[triangles++]=y;indices[triangles++]=y+1;}
                else{indices[triangles++]=x;indices[triangles++]=x+1;indices[triangles++]=y;indices[triangles++]=x+1;indices[triangles++]=y+1;indices[triangles++]=y;}
            }
            renderer.enabled=triangles>0;mesh.SetColors(colours,0,count*Stride);mesh.SetIndices(indices,0,triangles,MeshTopology.Triangles,0,true);
            properties.SetColor("_Body",sharp?new Color(.24f,.27f,.28f):style.body);properties.SetColor("_Edge",sharp?new Color(.68f,.74f,.76f):style.edge);properties.SetFloat("_Mode",0);
            properties.SetFloat("_Fade",style.visibility*strength*(sharp?.43f:heavy?.75f:.5f));properties.SetFloat("_Age",0);
            properties.SetFloat("_Clock",now);properties.SetFloat("_Heat",heavy?.55f:0);renderer.SetPropertyBlock(properties);
        }
        public void Clear(){count=0;wasActive=false;newStroke=false;if(renderer)renderer.enabled=false;}
        public void EndStroke(){wasActive=false;}
        public void Dispose()=>Dispose(false);
        public void Dispose(bool immediate){BrineVfxGeometry.Release(mesh,immediate);if(root)BrineVfxGeometry.Release(root.gameObject,immediate);}
    }
}
