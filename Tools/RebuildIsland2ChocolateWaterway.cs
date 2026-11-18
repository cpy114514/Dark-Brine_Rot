using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Waterway = SecondIslandChocolateWaterway;

public static class RebuildIsland2ChocolateWaterway
{
    public const string Candidate = "Assets/Scenes/Second Island/Waterway Preview/Second Island.unity";
    public const string RootName = "Chocolate Waterway - West Valley";
    const string Art = "Assets/Game/Art/Island2Waterway", Evidence = ".codex/chocolate-rebuild";
    const string MotionEvidence = ".codex/chocolate-motion";
    const string ChannelAsset = "West channel moving surface";
    const string FallAsset = "North cliff cascade";
    const string Original = "Assets/Game/Prefabs/Environment/CottonCandyIsland/Models/CottonCandy_Island01_HighRes.obj";
    static readonly Vector3 Lake = new Vector3(0,2523.5f,2070);
    static readonly Vector2[] Guides = {
        new Vector2(-111,2079),new Vector2(-144,2083),new Vector2(-176,2104),new Vector2(-204,2134),
        new Vector2(-219,2169),new Vector2(-218,2203),new Vector2(-209,2234),new Vector2(-221,2267),
        new Vector2(-222,2292),new Vector2(-227,2315),new Vector2(-226,2334)
    };
    static readonly Vector2[] FordSites={new Vector2(-151,2085),new Vector2(-217,2160),new Vector2(-219,2280)};
    static readonly (Vector2 center,float radius)[] Protected={
        (new Vector2(-105,1755),58),(new Vector2(-185,1850),63),(new Vector2(-147,1610),34),
        (new Vector2(98.6f,1945),37),(new Vector2(-147.47f,2210.31f),42)
    };

    sealed class Author
    {
        public Waterway data;
        public MeshCollider original,ground;
        public Island2DecorationMeshLibrary library;
        public Transform root;
        public Vector3 fallExit;
        float outletTravel;
        Vector2[] outline, route;
        float[] distances, ford;
        readonly Dictionary<Vector2Int,List<int>> grid=new Dictionary<Vector2Int,List<int>>();
        public static bool Ray(MeshCollider surface,Vector2 p,out RaycastHit hit) =>
            surface.Raycast(new Ray(new Vector3(p.x,surface.bounds.max.y+2,p.y),Vector3.down),out hit,surface.bounds.size.y+4);
        public static Vector3 Floor(MeshCollider surface,Vector2 p)
        {
            if(!Ray(surface,p,out var hit))throw new InvalidOperationException("No terrain at "+p);
            return hit.point;
        }

        public void Layout(Vector3[] legacy)
        {
            var angles=Enumerable.Range(0,160).Select(i=>i*Mathf.PI/80).Where(a=>Mathf.Abs(a-3.02f)>.145f).ToArray();
            outline=angles.Select(a=>
            {
                float edge=1+.077f*Mathf.Sin(a*3+.6f)+.035f*Mathf.Cos(a*5-1)+.025f*Mathf.Sin(a*8);
                edge*=1-.085f*Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(a*Mathf.Rad2Deg,116)/15,2));
                return XZ(Lake)+new Vector2(85*Mathf.Cos(a),62*Mathf.Sin(a))*edge;
            }).ToArray();
            int chord=Array.FindIndex(angles,a=>a>3.02f);
            Vector2 a0=outline[chord-1], b0=outline[chord], mouth=(a0+b0)*.5f;
            Vector2 across=(b0-a0).normalized, heading=new Vector2(across.y,-across.x);
            if(heading.x>0)heading=-heading;
            var keys=new[]{mouth,mouth+heading*10}.Concat(Guides).ToArray();
            var points=new List<Vector2>{mouth};
            for(int k=0;k<keys.Length-1;k++)
            {
                Vector2 a=keys[k],b=keys[k+1],p=keys[Mathf.Max(0,k-1)],q=keys[Mathf.Min(keys.Length-1,k+2)];
                int n=Mathf.CeilToInt(Vector2.Distance(a,b)/1.65f);
                for(int j=1;j<=n;j++)
                {
                    float t=j/(float)n;
                    points.Add((2*a+(-p+b)*t+(2*p-5*a+4*b-q)*t*t+(-p+3*a-3*b+q)*t*t*t)*.5f);
                }
            }
            route=points.ToArray();
            distances=new float[route.Length];
            float[] floor=route.Select(p=>Floor(original,p).y).ToArray();
            for(int i=1;i<route.Length;i++) distances[i]=distances[i-1]+Vector2.Distance(route[i],route[i-1]);
            int[] crossings=FordSites.Select(p=>Enumerable.Range(0,route.Length).OrderBy(i=>(route[i]-p).sqrMagnitude).First()).ToArray();
            ford=crossings.Select(i=>distances[i]).ToArray();
            var sections=new Waterway.Section[route.Length];
            float height=Lake.y, mouthWidth=Vector2.Distance(a0,b0)*.5f;
            for(int i=0;i<route.Length;i++)
            {
                float arc=distances[i], calm=FordBlend(arc), grade=0;
                if(i>0)
                {
                    float smooth=0;
                    for(int k=-7;k<=7;k++)smooth+=floor[Mathf.Clamp(i+k,0,floor.Length-1)]*(8-Mathf.Abs(k))/64;
                    float cliff=Ease(distances.Last()-64,distances.Last()-12,arc);
                    float limit=Mathf.Lerp(Mathf.Lerp(.98f,1.57f,cliff),.18f,calm);
                    grade=Mathf.Clamp((height-smooth-.30f)/Mathf.Lerp(7,2.6f,cliff),.021f,limit)*Ease(2,27,arc);
                    height-=grade*(arc-distances[i-1]);
                }
                Vector2 axis=(route[Mathf.Min(i+4,route.Length-1)]-route[Mathf.Max(i-4,0)]).normalized;
                Vector2 side=new Vector2(-axis.y,axis.x);
                float width=Mathf.Clamp(11.8f+2.1f*Mathf.Sin(arc*.028f)+1.4f*Mathf.Sin(arc*.011f+1),9.1f,15.5f);
                width=Mathf.Lerp(mouthWidth,Mathf.Lerp(width,10.6f,calm*.67f),Ease(0,29,arc));
                width=Mathf.Lerp(width,15.1f,Ease(distances.Last()-45,distances.Last(),arc));
                Vector2 before=route[i]-route[Mathf.Max(i-8,0)],after=route[Mathf.Min(i+8,route.Length-1)]-route[i];
                sections[i]=new Waterway.Section{left=At(route[i]-side*width,height),right=At(route[i]+side*width,height),
                    distance=arc,speed=Mathf.Lerp(.28f,.68f,Mathf.InverseLerp(.01f,.71f,grade)),
                    eddy=Mathf.Clamp(Vector2.SignedAngle(before,after)/35,-1,1)*(1-calm*.78f)};
                if(i==0)
                {
                    bool order=Vector2.Dot(b0-a0,side)>0;
                    sections[i].left=At(order?a0:b0,height);
                    sections[i].right=At(order?b0:a0,height);
                }
                var cell=Cell(route[i]);
                for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++)
                {
                    var key=cell+new Vector2Int(x,z);
                    if(!grid.TryGetValue(key,out var set))grid.Add(key,set=new List<int>());
                    set.Add(i);
                }
            }
            data.lakeCenter=Lake;
            data.lakeOutline=outline.Select(p=>At(p,Lake.y)).ToArray();
            data.outletDirection=At(heading,0);
            data.sections=sections;
            if(legacy.Length>0)data.previousChannel=legacy;
            data.crossings=crossings.Select((i,n)=>
            {
                var s=sections[i];var side=(s.right-s.left).normalized;
                return new Waterway.Crossing{label=new[]{"Upper valley ford","West valley ford","Cloudfall lookout ford"}[n],from=s.left-side*7,to=s.right+side*7};
            }).ToArray();
        }
        static float Ease(float a,float b,float p)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,p));
        float FordBlend(float d)=>1-Ease(3,20,ford.Min(f=>Mathf.Abs(d-f)));
        static Vector2Int Cell(Vector2 p)=>new Vector2Int(Mathf.FloorToInt(p.x/28),Mathf.FloorToInt(p.y/28));

        bool Channel(Vector2 p,out float shore,out float height,out float arc)
        {
            shore=float.PositiveInfinity;height=arc=0;
            if(!grid.TryGetValue(Cell(p),out var candidates)) return false;
            float best=float.PositiveInfinity,t=0;int nearest=0;
            foreach(int i in candidates)
            {
                if(i==0)continue;
                Vector2 a=route[i-1],d=route[i]-a;
                float k=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));
                float distance=(p-a-d*k).sqrMagnitude;
                if(distance<best){best=distance;nearest=i;t=k;}
            }
            if(nearest==0 || nearest==1 && Vector2.Dot(p-route[0],route[1]-route[0])<0 ||
                nearest==route.Length-1 && Vector2.Dot(p-route[nearest],route[nearest]-route[nearest-1])>0)return false;
            var before=data.sections[nearest-1];var after=data.sections[nearest];
            shore=Mathf.Sqrt(best)-Mathf.Lerp(before.HalfWidth,after.HalfWidth,t);
            height=Mathf.Lerp(before.Center.y,after.Center.y,t);
            arc=Mathf.Lerp(before.distance,after.distance,t);
            return true;
        }

        float LakeDistance(Vector2 p)
        {
            if((p-XZ(Lake)).sqrMagnitude>130*130)return 45;
            float best=float.PositiveInfinity;bool inside=false;
            for(int i=0;i<outline.Length;i++)
            {
                Vector2 a=outline[i],b=outline[(i+1)%outline.Length],d=b-a;
                float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
                best=Mathf.Min(best,(p-a-d*t).sqrMagnitude);
                if((a.y>p.y)!=(b.y>p.y)&&p.x<a.x+(b.x-a.x)*(p.y-a.y)/(b.y-a.y))inside=!inside;
            }
            return Mathf.Sqrt(best)*(inside?-1:1);
        }
        Vector2 FeetOf(Vector2 p,float originalHeight)
        {
            foreach(var keep in Protected)
                if((p-keep.center).sqrMagnitude<keep.radius*keep.radius)return new Vector2(originalHeight,0);
            float shore=LakeDistance(p),height=originalHeight,wet=0;
            if(shore<29)
            {
                float bank=shore<0?-1.15f*Ease(0,7,-shore):Mathf.Min(4.4f,shore*.34f);
                height=Mathf.Lerp(Lake.y+bank,originalHeight,Ease(4,29,shore));
                wet=1-Ease(.2f,6.3f,shore);
            }
            if(Channel(p,out float river,out float surface,out float arc)&&river<29&&(shore>-.6f||surface<Lake.y-.08f))
            {
                float calm=FordBlend(arc);
                float bank=river<0?-Mathf.Lerp(.94f,.62f,calm)*Ease(0,4.7f,-river):Mathf.Min(4.4f,river*Mathf.Lerp(.47f,.19f,calm));
                float desired=surface+bank;
                if(arc>distances.Last()-10 && river>0)desired=Mathf.Min(desired,height);
                height=Mathf.Lerp(height,desired,1-Ease(3.4f,29,river));
                wet=Mathf.Max(wet,1-Ease(.2f,6.2f,river));
            }
            // The lake polygon closes across the outlet chord for point queries.
            // Excavate that chord as part of the submerged throat, not as a shore.
            Vector2 exitOffset=p-route[0];
            if(exitOffset.sqrMagnitude<27f*27f)
            {
                Vector2 downstream=XZ(data.outletDirection);
                float along=Vector2.Dot(exitOffset,downstream);
                float sideways=Mathf.Abs(Vector2.Dot(exitOffset,new Vector2(-downstream.y,downstream.x)));
                float halfWidth=data.sections[0].HalfWidth;
                float throat=(1-Ease(3,16,Mathf.Abs(along)))*(1-Ease(halfWidth-2,halfWidth+.24f,sideways));
                float bottom=Lake.y-.95f;
                if(along>0 && surface>0)bottom=Mathf.Min(bottom,surface-.95f);
                height=Mathf.Lerp(height,Mathf.Min(height,bottom),throat);
                wet=Mathf.Max(wet,throat);
            }
            // Use the actual water polygon as the final authority. In tight bends a
            // nearest-centre estimate alone can put the original slope across the surface.
            float queryY=shore<=0 || surface<=0 ? Lake.y : surface;
            if((shore<4f || river<4f) && data.TrySample(At(p,queryY),out var visible,out _))
            {
                height=Mathf.Min(height,visible.y-.16f);
                wet=Mathf.Max(wet,.85f);
            }
            return new Vector2(height,wet);
        }

        public object Sculpt(Mesh source,MeshFilter[] copies)
        {
            var transform=copies[0].transform;
            var vertices=source.vertices.ToList();
            var uv=source.uv.ToList();
            var triangles=source.triangles.ToList();
            int sourceVertices=vertices.Count;
            // Preserve the previous island's authored grade inside protected sites, including the cottage's apron.
            var legacy=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Art/Island2WaterSystem/Water system mesh library.asset")
                .OfType<Mesh>().FirstOrDefault(m=>m.name=="Island 2 local lake basin and continuous river bed");
            if(legacy&&legacy.vertexCount==sourceVertices)
            {
                Vector3[] old=legacy.vertices;
                for(int i=0;i<sourceVertices;i++)
                {
                    Vector2 p=XZ(transform.TransformPoint(vertices[i]));
                    foreach(var keep in Protected)
                    {
                        float distance=Vector2.Distance(p,keep.center);
                        if(distance>=keep.radius+5)continue;
                        vertices[i]=Vector3.Lerp(old[i],vertices[i],Ease(keep.radius,keep.radius+5,distance));
                    }
                }
            }
            for(int pass=0;pass<2;pass++)
                RefineBanks(transform,vertices,uv,ref triangles);
            var colors=new Color32[vertices.Count];
            int adjusted=0;float cut=0,raise=0;
            for(int i=0;i<vertices.Count;i++)
            {
                Vector3 world=transform.TransformPoint(vertices[i]);
                if(world.x < -273 || world.x > 130 || world.z<1950 || world.z>2363 || world.y<2310)continue;
                Vector2 p=XZ(world),shape=FeetOf(p,world.y);
                float diff=shape.x-world.y;
                if(Mathf.Abs(diff)<.001f&&shape.y<.001f)continue;
                if(!Ray(original,p,out var hit)||Mathf.Abs(hit.point.y-world.y)>.15f)continue;
                world.y=shape.x;vertices[i]=transform.InverseTransformPoint(world);
                colors[i]=new Color32((byte)Mathf.RoundToInt(shape.y*255),0,0,255);
                cut=Mathf.Max(cut,-diff);raise=Mathf.Max(raise,diff);adjusted++;
            }
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);
            if(uv.Count==vertices.Count)mesh.SetUVs(0,uv);
            mesh.subMeshCount=1;
            mesh.SetTriangles(triangles,0);mesh.colors32=colors;
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
            var baked=Store(mesh,"West valley terrain and wet cocoa");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Marshmallow with cocoa banks.mat");
            if(!material)
            {
                material=new Material(copies[0].GetComponent<Renderer>().sharedMaterial);
                AssetDatabase.CreateAsset(material,Art+"/Marshmallow with cocoa banks.mat");
            }
            material.SetFloat("_CocoaShore",1);EditorUtility.SetDirty(material);
            foreach(var filter in copies)
            {
                filter.sharedMesh=baked;
                var renderer=filter.GetComponent<Renderer>();renderer.sharedMaterial=material;
                var collider=filter.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=baked;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            Physics.SyncTransforms();
            return new {adjusted,cut,raise,newBankVertices=vertices.Count-sourceVertices};
        }

        void RefineBanks(Transform terrain,List<Vector3> vertices,List<Vector2> uv,ref List<int> indices)
        {
            const float maximumEdge=1.55f;
            var midpoints=new Dictionary<(int,int),int>();
            var world=vertices.Select(terrain.TransformPoint).ToArray();
            bool haveUv=uv.Count==vertices.Count;
            (int,int) Key(int a,int b)=>a<b?(a,b):(b,a);
            void Check(int a,int b)
            {
                Vector3 p=(world[a]+world[b])*.5f;
                if(p.x< -273||p.x>130||p.z<1950||p.z>2363||p.y<2310 ||
                    (world[a]-world[b]).sqrMagnitude<maximumEdge*maximumEdge)return;
                float shore=LakeDistance(XZ(p));
                bool lake=Mathf.Abs(shore)<11&&Mathf.Abs(p.y-Lake.y)<29;
                bool river=Channel(XZ(p),out var edge,out var surface,out _)&&edge<14&&Mathf.Abs(p.y-surface)<35;
                if(!(lake||river))return;
                var key=Key(a,b);
                if(midpoints.ContainsKey(key))return;
                midpoints.Add(key,vertices.Count);
                vertices.Add((vertices[a]+vertices[b])*.5f);
                if(haveUv)uv.Add((uv[a]+uv[b])*.5f);
            }
            for(int i=0;i<indices.Count;i+=3)
            {
                int a=indices[i],b=indices[i+1],c=indices[i+2];
                Check(a,b);Check(b,c);Check(c,a);
            }
            if(midpoints.Count==0)return;
            var refined=new List<int>(indices.Count+midpoints.Count*9);
            var ring=new List<int>(6);
            for(int i=0;i<indices.Count;i+=3)
            {
                int a=indices[i],b=indices[i+1],c=indices[i+2];
                bool ab=midpoints.TryGetValue(Key(a,b),out int m0);
                bool bc=midpoints.TryGetValue(Key(b,c),out int m1);
                bool ca=midpoints.TryGetValue(Key(c,a),out int m2);
                if(!(ab||bc||ca)){refined.Add(a);refined.Add(b);refined.Add(c);continue;}
                int center=vertices.Count;
                vertices.Add((vertices[a]+vertices[b]+vertices[c])/3);
                if(haveUv)uv.Add((uv[a]+uv[b]+uv[c])/3);
                ring.Clear();
                ring.Add(a);if(ab)ring.Add(m0);
                ring.Add(b);if(bc)ring.Add(m1);
                ring.Add(c);if(ca)ring.Add(m2);
                for(int j=0;j<ring.Count;j++)
                {
                    refined.Add(ring[j]);refined.Add(ring[(j+1)%ring.Count]);refined.Add(center);
                }
            }
            indices=refined;
        }
        Mesh Store(Mesh mesh,string name)
        {
            string path=AssetDatabase.GetAssetPath(library);mesh.name=name;
            var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m=>m.name==name);
            if(!existing){AssetDatabase.AddObjectToAsset(mesh,library);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);
            return existing;
        }
        static Mesh StoreSurface(Mesh mesh,string name)
        {
            string path=Art+"/"+name+".asset";
            mesh.name=name;
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!existing)
            {
                AssetDatabase.CreateAsset(mesh,path);
                return mesh;
            }
            var motion=new List<Vector4>();
            mesh.GetUVs(1,motion);
            existing.Clear();
            existing.indexFormat=mesh.indexFormat;
            existing.vertices=mesh.vertices;
            existing.normals=mesh.normals;
            existing.tangents=mesh.tangents;
            existing.uv=mesh.uv;
            existing.SetUVs(1,motion);
            existing.triangles=mesh.triangles;
            existing.bounds=mesh.bounds;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }
        sealed class Geometry
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<Vector4> flow=new List<Vector4>(),tangents=new List<Vector4>();
            public readonly List<int> indices=new List<int>();
            public int Count=>vertices.Count;
            public void Add(Vector3 p,float u,float distance,Vector4 motion,Vector3 across)
            {
                vertices.Add(p);uv.Add(new Vector2(u,distance));flow.Add(motion);
                tangents.Add(new Vector4(across.x,across.y,across.z,1));
            }
            public void Quad(int a,int b,int c,int d)
            {
                indices.Add(a);indices.Add(b);indices.Add(c);indices.Add(b);indices.Add(d);indices.Add(c);
            }
            public Mesh Bake()
            {
                var m=new Mesh{indexFormat=vertices.Count>65534?IndexFormat.UInt32:IndexFormat.UInt16};
                m.SetVertices(vertices);m.SetUVs(0,uv);m.SetUVs(1,flow);m.SetTangents(tangents);m.SetTriangles(indices,0);
                m.RecalculateNormals();m.RecalculateBounds();return m;
            }
        }

        Mesh LakeMesh()
        {
            var g=new Geometry();
            const int rings=24;
            int count=outline.Length;
            for(int j=0;j<=rings;j++)for(int i=0;i<count;i++)
            {
                Vector2 rim=outline[i],direction=rim-XZ(Lake);
                float t=j/(float)rings;
                bool mouth=Vector2.Distance(rim,route[0])<17.5f;
                Vector2 p=XZ(Lake)+direction*(.001f+.999f*t);
                if(!mouth)p+=direction.normalized*(1.95f*Mathf.Pow(t,8));
                float pull=1-Ease(12,39,Vector2.Distance(p,route[0]));
                var drift=Vector2.Lerp(new Vector2(.13f,.04f),XZ(data.outletDirection),pull);
                g.Add(At(p,Lake.y),.5f,0,new Vector4(drift.x,drift.y,Mathf.Lerp(.28f,1.25f,pull),pull),Vector3.right);
                if(j<rings){int a=j*count+i,b=j*count+(i+1)%count;g.Quad(a,b,a+count,b+count);}
            }
            return Store(g.Bake(),"Lake with west mouth");
        }
        public Mesh ChannelMesh()
        {
            var g=new Geometry();
            const int cols=14;
            float travel=0,lastSpeed=0;
            for(int i=0;i<data.sections.Length;i++)
            {
                var s=data.sections[i];Vector3 side=(s.right-s.left).normalized;
                float mouth=Ease(0,13,s.distance);
                float toLip=data.sections.Last().distance-s.distance;
                float margin=1.95f*Mathf.Min(mouth,Ease(0,10,toLip));
                float length=i==0?0:Vector3.Distance(s.Center,data.sections[i-1].Center);
                float centerSpeed=Mathf.Lerp(Mathf.Lerp(1.25f,2.75f,mouth)+s.speed*4.5f,8.5f,1-Ease(0,21,toLip));
                if(i>0)travel+=length*.5f*(1/centerSpeed+1/lastSpeed);
                lastSpeed=centerSpeed;
                for(int j=0;j<=cols;j++)
                {
                    float u=j/(float)cols,lateral=u*2-1;
                    Vector3 p=s.Center+side*lateral*(s.HalfWidth+margin);
                    p=SpillShoulder(p,u,1-Ease(0,21,toLip));
                    g.Add(p,u,s.distance,
                        new Vector4(data.outletDirection.x,data.outletDirection.z,s.eddy,travel),side);
                    if(i<data.sections.Length-1&&j<cols){int k=i*(cols+1)+j;g.Quad(k,k+1,k+cols+1,k+cols+2);}
                }
            }
            outletTravel=travel;
            return StoreSurface(g.Bake(),ChannelAsset);
        }

        Vector3 SpillShoulder(Vector3 point,float u,float blend)
        {
            float edge=Ease(.73f,1,Mathf.Abs(u*2-1))*blend;
            if(edge>0&&Ray(ground,XZ(point),out var hit))
                point.y=Mathf.Lerp(point.y,Mathf.Min(point.y,hit.point.y+.10f),edge);
            return point;
        }

        Vector3 FallForward()
        {
            Vector3 direction=data.FallLip-data.sections[data.sections.Length-6].Center;direction.y=0;
            return direction.normalized;
        }
        public Mesh Curtain()
        {
            var g=new Geometry();
            var lip=data.sections.Last();
            Vector3 across=(lip.right-lip.left).normalized;
            Vector3 north=Vector3.forward;
            const int splitRow=62,cascadeRows=91,fallRows=32;
            const float cascadeRun=187f;
            Vector3[] center=new Vector3[cascadeRows+1];
            float[] time=new float[cascadeRows+fallRows+1];
            for(int row=0;row<=cascadeRows;row++)
            {
                center[row]=CascadePoint(row,.5f);
                if(row==0)continue;
                float speed=Mathf.Lerp(8.5f,24f,Ease(0,cascadeRows,row));
                time[row]=time[row-1]+Vector3.Distance(center[row],center[row-1])/speed;
            }
            fallExit=center[cascadeRows];
            for(int row=1;row<=fallRows;row++)
            {
                float t=row/(float)fallRows;
                var end=FallPosition(fallExit,t,1);
                var previous=FallPosition(fallExit,(row-1)/(float)fallRows,1);
                time[cascadeRows+row]=time[cascadeRows+row-1]+Vector3.Distance(end,previous)/Mathf.Lerp(24f,48f,t);
            }
            Span(0,1,0,splitRow,30,-1);
            Span(0,.38f,splitRow,cascadeRows+fallRows,12,0);
            Span(.38f,.76f,splitRow,cascadeRows+fallRows,12,1);
            Span(.76f,1,splitRow,cascadeRows+fallRows,9,2);
            return StoreSurface(g.Bake(),FallAsset);

            Vector3 CascadePoint(int row,float u)
            {
                if(row==0)return SpillShoulder(Vector3.Lerp(lip.left,lip.right,u),u,1);
                float run=cascadeRun*row/cascadeRows;
                float lateral=(u-.5f)*2;
                float opening=Ease(0,18,run);
                float width=lip.HalfWidth*Mathf.Lerp(1,.64f,opening)+Ease(39,100,run)*1.8f+
                    .8f*Mathf.Sin(run*.11f)*opening;
                Vector3 axis=Vector3.Slerp(across,Vector3.left,Ease(0,28,run));
                float hook=-3.3f*opening*(1-Ease(30,65,run))+5.5f*Mathf.Sin(run*.034f)*Ease(0,40,run)+8f*Ease(80,172,run);
                Vector3 centerPoint=lip.Center+north*run+axis*hook;
                Vector3 p=centerPoint+axis*width*lateral;
                if(Ray(ground,XZ(p),out var hit))
                {
                    float volume=(1.18f+.78f*Mathf.Sin(u*Mathf.PI))*(.87f+.13f*Mathf.Sin(run*.31f+u*7));
                    p.y=hit.point.y+volume;
                    if(run<68 && Ray(ground,XZ(centerPoint),out var midHit))
                    {
                        float drop=lip.Center.y-Mathf.Max(0,run-.45f)*1.27f;
                        float slope=hit.point.y-midHit.point.y;
                        float blend=Ease(0,10,run);
                        float section=Mathf.Lerp(drop,midHit.point.y+volume,opening);
                        section+=Mathf.Clamp(slope,-10,1.7f)*blend;
                        p.y=Mathf.Lerp(section,p.y,Ease(37,68,run));
                    }
                    p.y-=1.8f*Ease(.68f,1,Mathf.Abs(lateral))*Ease(0,5,run)*(1-Ease(146,177,run));
                }
                else
                {
                    // Continue the steep lower contour if this lane has already cleared the rock.
                    const float back=6;
                    p.y=Ray(ground,XZ(p-north*back),out hit)?hit.point.y-3.4f*back:
                        lip.Center.y-255f-3f*(run-177f);
                }
                return p;
            }

            Vector3 FallPosition(Vector3 edge,float t,int lobe)
            {
                float run=9.4f*(1-Mathf.Exp(-t*7f))+4f*t;
                float descent=(61f*t+69f*t*t)*(1+.054f*lobe);
                return edge+north*run+Vector3.down*descent+across*((lobe-1)*t*3.1f);
            }

            void Span(float startU,float endU,int first,int last,int cols,int lobe)
            {
                int start=g.Count;
                int rowSize=(cols+1)*2;
                for(int i=first;i<=last;i++)
                {
                    int localRow=Mathf.Min(i,cascadeRows);
                    float drop=Mathf.Max(0,i-cascadeRows)/(float)fallRows;
                    float split=Ease(splitRow,cascadeRows,i);
                    float gap=split*(.043f+.008f*Mathf.Sin(i*.38f));
                    float left=startU+(startU>0?gap:0);
                    float right=endU-(endU<1?gap:0);
                    float mid=(left+right)*.5f;
                    Vector3 heading=i<cascadeRows
                        ? center[i+1]-center[Mathf.Max(0,i-1)]
                        : FallPosition(Vector3.zero,Mathf.Min(1,drop+.01f),lobe)-FallPosition(Vector3.zero,Mathf.Max(0,drop-.01f),lobe);
                    Vector3 normal=Vector3.Cross(across,heading).normalized;
                    for(int face=0;face<2;face++)for(int j=0;j<=cols;j++)
                    {
                        float x=j/(float)cols,u=Mathf.Lerp(startU,endU,x);
                        float lane=Mathf.Lerp(left,right,x);
                        lane=Mathf.Lerp(lane,mid,drop*.29f);
                        float edge=Mathf.Lerp(Mathf.Sin(u*Mathf.PI),Mathf.Sin(x*Mathf.PI),split);
                        float crown=(.78f+.44f*Mathf.Sin(i*.28f+u*11))*edge*Ease(0,11,i);
                        float thickness=Mathf.Lerp(1.1f,3.8f,split)*(.32f+.68f*edge)*(1-drop*.45f);
                        Vector3 p=CascadePoint(localRow,lane);
                        if(drop>0)p=FallPosition(p,drop,lobe);
                        p+=normal*(crown-(face==1?thickness:0));
                        float phase=outletTravel+time[i];
                        g.Add(p,u,lip.distance+time[i]*10,new Vector4(Ease(0,7,i),0,drop,phase),across);
                    }
                    if(i==last)continue;
                    int top=start+(i-first)*rowSize,bottom=top+cols+1;
                    for(int j=0;j<cols;j++)
                    {
                        g.Quad(top+j,top+j+1,top+j+rowSize,top+j+rowSize+1);
                        g.Quad(bottom+j+1,bottom+j,bottom+j+rowSize+1,bottom+j+rowSize);
                    }
                    if(startU==0||i>=splitRow)g.Quad(bottom,top,bottom+rowSize,top+rowSize);
                    if(endU==1||i>=splitRow)g.Quad(top+cols,bottom+cols,top+cols+rowSize,bottom+cols+rowSize);
                }
            }
        }
        Material FlowMaterial(string name,float mode,float gloss)
        {
            var shader=Shader.Find("Dark Brine/Second Island/Chocolate Flow");
            if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Chocolate Flow shader unavailable.");
            var path=Art+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.SetFloat("_Mode",mode);material.SetFloat("_Gloss",gloss);
            if(mode==0)SetLakeDirection(material);
            EditorUtility.SetDirty(material);
            return material;
        }
        public void SetLakeDirection(Material material)
        {
            material.SetVector("_LakeOutlet",new Vector4(data.outletDirection.x,data.outletDirection.z,0,0));
            EditorUtility.SetDirty(material);
        }
        MeshRenderer Render(string name,Mesh mesh,Material material)
        {
            var t=Group(root,name);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            return renderer;
        }
        public (MeshRenderer lake,MeshRenderer channel,MeshRenderer fall) Surfaces() =>
            (Render("Chocolate headwater lake",LakeMesh(),FlowMaterial("Quiet couverture",0,.79f)),
             Render("Chocolate west channel",ChannelMesh(),FlowMaterial("Viscous west current",1,.79f)),
             Render("Chocolate Cloudfall",Curtain(),FlowMaterial("Cocoa curtain",2,.68f)));

        public void Droplets(Transform old)
        {
            var system=old?old.GetComponentInChildren<ParticleSystem>(true):null;
            if(!system)return;
            var drop=Object.Instantiate(system.gameObject,root);
            drop.name="Cloudfall heavy droplets";
            drop.SetActive(true);
            ConfigureDroplets(drop.GetComponent<ParticleSystem>());
        }
        public void ConfigureDroplets(ParticleSystem particles)
        {
            particles.transform.SetPositionAndRotation(fallExit+Vector3.forward*2,Quaternion.LookRotation(FallForward()));
            var main=particles.main;main.maxParticles=72;main.startLifetime=new ParticleSystem.MinMaxCurve(1.7f,2.6f);
            main.startSize=new ParticleSystem.MinMaxCurve(.26f,.63f);
            var emission=particles.emission;emission.rateOverTime=16;
            var shape=particles.shape;shape.scale=new Vector3(data.sections.Last().HalfWidth*.96f,1,2);
            var velocity=particles.velocityOverLifetime;velocity.space=ParticleSystemSimulationSpace.Local;
            velocity.x=new ParticleSystem.MinMaxCurve(-2.3f,2.3f);
            velocity.y=new ParticleSystem.MinMaxCurve(-17,-11);
            velocity.z=new ParticleSystem.MinMaxCurve(3,6);
        }

        public int Regroup(Transform decor,Transform old)
        {
            var station=decor.Find("Abandoned cocoa sampling station");
            var upper=data.sections.First(s=>s.distance>40);
            Vector3 axis=(upper.right-upper.left).normalized;
            if(station)station.SetPositionAndRotation(Floor(ground,XZ(upper.left-axis*15)),Quaternion.LookRotation(axis));
            var targets=decor.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.Find("Confection")||t.name=="Weathered cocoa-side boulder"||t.name=="Wind shaped scrub"||
                    t.name=="Crooked coastal 0"||t.name=="Upright woodland 1").ToArray();
            int moved=0;
            foreach(var prop in targets)
            {
                if((old&&prop.IsChildOf(old))||prop.IsChildOf(root)||!data.IsInChangedGround(prop.position))continue;
                if(!Ray(ground,XZ(prop.position),out var hit))continue;
                if(data.TrySample(hit.point,out var surface,out _)&&surface.y>hit.point.y+.05f)
                {
                    Vector2 at=XZ(prop.position);
                    var section=data.sections.OrderBy(s=>(XZ(s.Center)-at).sqrMagnitude).First();
                    Vector3 right=(section.right-section.left).normalized;
                    Vector3 bank=Vector3.Dot(prop.position-section.Center,right)<0?section.left-right*9:section.right+right*9;
                    Vector2 lakeOffset=at-XZ(Lake);
                    if(lakeOffset.magnitude<91 && !Channel(at,out _,out _,out _))
                    {
                        Vector2 shell=new Vector2(lakeOffset.x/85,lakeOffset.y/62).normalized;
                        bank=Lake+new Vector3(shell.x*100,0,shell.y*77);
                    }
                    if(!Ray(ground,XZ(bank),out _))continue;
                    prop.position=new Vector3(bank.x,prop.position.y,bank.z);
                }
                if(Seat(prop))moved++;
            }
            foreach(var prop in decor.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Solid chocolate ford piece"))
                prop.gameObject.SetActive(false);

            var shelf=Group(root,"Back-bank markers");
            Transform rock=targets.FirstOrDefault(t=>t.name=="Weathered cocoa-side boulder"&&t.gameObject.activeSelf);
            Transform biscuit=targets.FirstOrDefault(t=>t.name=="Choc chip biscuit outcrop"&&t.gameObject.activeSelf);
            for(int i=0;i<data.crossings.Length;i++)
            {
                var c=data.crossings[i];Vector3 span=(c.to-c.from).normalized,down=Vector3.Cross(span,Vector3.up);
                Vector3 edge=i==1?c.from-span*4:c.to+span*4;
                Place(rock,edge+down*11,.71f,i*55);
                Place(biscuit,edge+down*14+span*2,.54f,i*42+18);
            }
            return moved;

            void Place(Transform template,Vector3 at,float scale,float yaw)
            {
                if(!template||!Ray(ground,XZ(at),out var hit))return;
                var prop=Object.Instantiate(template.gameObject,shelf);
                prop.transform.SetPositionAndRotation(hit.point,Quaternion.AngleAxis(yaw,hit.normal)*Quaternion.FromToRotation(Vector3.up,hit.normal));
                prop.transform.localScale*=scale;
                foreach(var c in prop.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
                Seat(prop.transform);
            }
        }
        bool Seat(Transform prop)
        {
            float low=float.PositiveInfinity;
            foreach(var filter in prop.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
                var vertices=filter.sharedMesh.vertices;
                for(int i=0;i<vertices.Length;i+=Mathf.Max(1,vertices.Length/100))
                {
                    Vector3 world=filter.transform.TransformPoint(vertices[i]);
                    if(Ray(ground,XZ(world),out var hit))low=Mathf.Min(low,world.y-hit.point.y);
                }
            }
            if(!float.IsFinite(low)||Mathf.Abs(low)<.12f)return false;
            prop.position-=Vector3.up*(low+.075f);
            return true;
        }
    }

    public static object Stage()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=SecondIslandAudio.ScenePath)
            throw new InvalidOperationException("Open the saved Second Island in Edit mode.");
        Directory.CreateDirectory(Evidence);
        string backup=Evidence+"/Second Island.before.unity";
        if(!File.Exists(backup))File.Copy(scene.path,backup);
        if(!AssetDatabase.IsValidFolder("Assets/Scenes/Second Island/Waterway Preview"))
            AssetDatabase.CreateFolder("Assets/Scenes/Second Island","Waterway Preview");
        if(!EditorSceneManager.SaveScene(scene,Candidate,true))
            throw new IOException("Could not stage scene copy.");
        EditorSceneManager.OpenScene(Candidate,OpenSceneMode.Single);
        return BuildCandidate();
    }

    public static object BuildCandidate()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=Candidate)
            throw new InvalidOperationException("Open the saved waterway candidate in Edit mode.");
        if(!AssetDatabase.IsValidFolder(Art))AssetDatabase.CreateFolder("Assets/Game/Art","Island2Waterway");
        var ground=Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<MeshCollider>();
        var source=AssetDatabase.LoadAllAssetsAtPath(Original).OfType<Mesh>().OrderByDescending(m=>m.vertexCount).First();
        var surfaces=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true))
            .Where(f=>f.sharedMesh==ground.sharedMesh&&f.GetComponent<MeshCollider>()).ToArray();
        if(surfaces.Length!=2)throw new InvalidOperationException("Expected the two local island terrain copies.");
        var sampler=new GameObject("Read-only original terrain sampler"){hideFlags=HideFlags.HideAndDontSave};
        try
        {
            sampler.transform.SetPositionAndRotation(ground.transform.position,ground.transform.rotation);
            sampler.transform.localScale=ground.transform.lossyScale;
            var original=sampler.AddComponent<MeshCollider>();original.sharedMesh=source;Physics.SyncTransforms();
            var author=new Author{ground=ground,original=original,library=Asset<Island2DecorationMeshLibrary>("West valley meshes.asset"),
                data=Asset<Waterway>("West valley waterway.asset")};
            var decor=GameObject.Find("Island 2 - Environment Decoration").transform;
            var old=decor.Find("Chocolate River - The Couverture Run/Lake to Cloudfall - connected chocolate water system");
            var stage=decor.Find(RootName);if(stage)Object.DestroyImmediate(stage.gameObject);
            author.root=Group(decor,RootName);
            var audio=Object.FindFirstObjectByType<SecondIslandAudio>();
            author.Layout(audio.riverPoints);
            var sculpt=author.Sculpt(source,surfaces);
            var water=author.Surfaces();author.Droplets(old);
            int shifted=author.Regroup(decor,old);
            if(old)old.gameObject.SetActive(false);
            var contact=Object.FindFirstObjectByType<SecondIslandChocolateContact>();
            var link=new SerializedObject(contact);
            link.FindProperty("lake").objectReferenceValue=water.lake;
            link.FindProperty("river").objectReferenceValue=water.channel;
            link.FindProperty("waterway").objectReferenceValue=author.data;
            link.ApplyModifiedPropertiesWithoutUndo();
            audio.waterway=author.data;EditorUtility.SetDirty(audio);EditorUtility.SetDirty(author.data);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save candidate.");
            var data=author.data;
            var result=new{length=data.sections.Last().distance,samples=data.sections.Length,lip=data.FallLip.ToString(),
                widthMin=data.sections.Min(s=>s.HalfWidth)*2,widthMax=data.sections.Max(s=>s.HalfWidth)*2,sculpt,shifted,
                fords=data.crossings.Select(f=>new{f.label,from=f.from.ToString(),to=f.to.ToString()}).ToArray(),
                triangles=author.root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)};
            File.WriteAllText(Evidence+"/candidate.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally{Object.DestroyImmediate(sampler);}
    }

    public static string Promote()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=Candidate||!GameObject.Find(RootName))
            throw new InvalidOperationException("A saved waterway candidate is required.");
        var backup=Evidence+"/Second Island.before.unity";
        if(!File.Exists(backup)||!File.ReadAllBytes(backup).SequenceEqual(File.ReadAllBytes(SecondIslandAudio.ScenePath)))
            throw new InvalidOperationException("The formal scene changed after staging. Preserve those edits before promoting.");
        if(!EditorSceneManager.SaveScene(scene,SecondIslandAudio.ScenePath,true))throw new IOException("Could not promote waterway.");
        return EditorSceneManager.OpenScene(SecondIslandAudio.ScenePath,OpenSceneMode.Single).path;
    }
    public static object StageFlow()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=SecondIslandAudio.ScenePath)
            throw new InvalidOperationException("Open the saved Second Island in Edit mode.");
        Directory.CreateDirectory(MotionEvidence);
        string backup=MotionEvidence+"/Second Island.before.unity";
        if(!File.Exists(backup))File.Copy(scene.path,backup);
        if(!EditorSceneManager.SaveScene(scene,Candidate,true))throw new IOException("Could not copy Second Island.");
        EditorSceneManager.OpenScene(Candidate,OpenSceneMode.Single);
        return BakeFlow();
    }

    public static object BakeFlow()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=Candidate)
            throw new InvalidOperationException("Open the saved waterway candidate in Edit mode.");
        var root=GameObject.Find(RootName).transform;
        var author=new Author{
            ground=Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<MeshCollider>(),
            data=Object.FindFirstObjectByType<SecondIslandAudio>().waterway};
        var channel=author.ChannelMesh();
        var fall=author.Curtain();
        author.SetLakeDirection(root.Find("Chocolate headwater lake").GetComponent<MeshRenderer>().sharedMaterial);
        root.Find("Chocolate west channel").GetComponent<MeshFilter>().sharedMesh=channel;
        root.Find("Chocolate Cloudfall").GetComponent<MeshFilter>().sharedMesh=fall;
        author.ConfigureDroplets(root.Find("Cloudfall heavy droplets").GetComponent<ParticleSystem>());
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save the motion candidate.");
        return new{channel=channel.vertexCount,curtain=fall.vertexCount,exit=author.fallExit.ToString(),bounds=fall.bounds.ToString()};
    }

    public static string PromoteFlow()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!=Candidate||!GameObject.Find(RootName))
            throw new InvalidOperationException("A saved waterway candidate is required.");
        string backup=MotionEvidence+"/Second Island.before.unity";
        if(!File.Exists(backup)||!File.ReadAllBytes(backup).SequenceEqual(File.ReadAllBytes(SecondIslandAudio.ScenePath)))
            throw new InvalidOperationException("The formal scene changed after motion staging.");
        if(!EditorSceneManager.SaveScene(scene,SecondIslandAudio.ScenePath,true))
            throw new IOException("Could not promote the motion candidate.");
        return EditorSceneManager.OpenScene(SecondIslandAudio.ScenePath,OpenSceneMode.Single).path;
    }
    static T Asset<T>(string name) where T:ScriptableObject
    {
        string path=Art+"/"+name;
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if(asset)return asset;
        asset=ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset,path);
        return asset;
    }
    static Transform Group(Transform parent,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    static Vector3 At(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
    static Vector2 XZ(Vector3 p)=>new Vector2(p.x,p.z);
}
