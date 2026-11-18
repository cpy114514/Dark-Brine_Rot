using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Native Editor authoring. Original island OBJ and every other scene remain untouched.
public static class BuildIsland2WaterSystem
{
    const string Art="Assets/Game/Art/Island2WaterSystem", Out=".codex/island2-water-system";
    const string Original="Assets/Game/Prefabs/Environment/CottonCandyIsland/Models/CottonCandy_Island01_HighRes.obj";
    const float LakeY=2523.5f;
    static readonly Vector2 Lake=new Vector2(0,2070), Radius=new Vector2(85,62);
    static readonly Vector2[] Controls={
        new Vector2(20,2025),new Vector2(40,2005),new Vector2(52,1957),new Vector2(30,1933),new Vector2(5,1910),
        new Vector2(-10,1883),new Vector2(-25,1857),new Vector2(-32,1832),new Vector2(-51,1810),new Vector2(-77,1795),new Vector2(-104,1784),new Vector2(-140,1781),
        new Vector2(-180,1780),new Vector2(-225,1790),new Vector2(-265,1820),new Vector2(-275,1860),new Vector2(-300,1900),new Vector2(-300,1940),new Vector2(-300,1980),
        new Vector2(-300,2020),new Vector2(-300,2060),new Vector2(-300,2100),new Vector2(-300,2140),new Vector2(-300,2180),new Vector2(-300,2220),new Vector2(-300,2260),
        new Vector2(-300,2300),new Vector2(-260,2340),new Vector2(-258,2352)};
    static Collider originalGround,ground;
    static Transform terrain,waterRoot;
    static Island2DecorationMeshLibrary library;
    static readonly List<Vector3> route=new List<Vector3>();
    static readonly List<float> widths=new List<float>(), distances=new List<float>();
    static readonly Dictionary<Vector2Int,List<int>> buckets=new Dictionary<Vector2Int,List<int>>();
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static Material current,lakeMat,fallsMat,bankMat;
    static Vector2 XZ(Vector3 p)=>new Vector2(p.x,p.z);
    static float Smooth(float a)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(a));
    static Vector3 Floor(Vector2 p)
    {
        if(!originalGround.Raycast(new Ray(new Vector3(p.x,2800,p.y),Vector3.down),out var h,1900))throw new Exception("No source ground at "+p);
        return h.point;
    }
    static float GroundY(Vector2 p)
    {return ground.Raycast(new Ray(new Vector3(p.x,2800,p.y),Vector3.down),out var h,1900)?h.point.y:ShapedY(p,Floor(p).y);}
    static float Shape(float angle)=>1+.06f*Mathf.Sin(angle*3+.5f)+.035f*Mathf.Cos(angle*7);
    static float LakeR(Vector2 p)
    {
        var q=p-Lake;q=new Vector2(q.x/Radius.x,q.y/Radius.y);
        return q.magnitude/Shape(Mathf.Atan2(q.y,q.x));
    }
    static Vector2 Curve(float t)
    {
        float u=Mathf.Clamp01(t)*(Controls.Length-1);int k=Mathf.Min(Controls.Length-2,Mathf.FloorToInt(u));u-=k;
        var a=Controls[Mathf.Max(0,k-1)];var b=Controls[k];var c=Controls[k+1];var d=Controls[Mathf.Min(Controls.Length-1,k+2)];
        float length=Vector2.Distance(b,c);var m0=(c-a).normalized*Mathf.Min(length, k==0?length:Vector2.Distance(a,b))*.65f;
        var m1=(d-b).normalized*Mathf.Min(length,k+2>=Controls.Length?length:Vector2.Distance(c,d))*.65f;
        return (2*u*u*u-3*u*u+1)*b+(u*u*u-2*u*u+u)*m0+(-2*u*u*u+3*u*u)*c+(u*u*u-u*u)*m1;
    }
    static void MakeRoute()
    {
        route.Clear();widths.Clear();distances.Clear();buckets.Clear();float y=LakeY,dist=0;var prev=Curve(0);
        for(int i=0;i<=920;i++){
            var p=Curve(i/920f);float step=Vector2.Distance(p,prev);dist+=step;
            if(LakeR(p)<.98f&&dist<80)y=LakeY;
            else {float desired=Floor(p).y+.34f;y=Mathf.Max(y-step*.58f,Mathf.Min(y-step*.006f,desired));}
            route.Add(new Vector3(p.x,y,p.y));distances.Add(dist);
            float half=10.5f+1.6f*Mathf.Sin(dist*.026f)+.9f*Mathf.Cos(dist*.067f);
            half+=4*Smooth((dist-940)/180);widths.Add(half);prev=p;
            var cell=new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.y/32));
            for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++){var key=cell+new Vector2Int(dx,dz);if(!buckets.TryGetValue(key,out var list))buckets[key]=list=new List<int>();list.Add(i);}
        }
    }
    static bool Reach(Vector2 p,out Vector3 at,out float width,out float dist)
    {
        at=Vector3.zero;width=0;dist=float.PositiveInfinity;
        if(!buckets.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.y/32)),out var list))return false;
        int best=-1;foreach(int i in list){float d=(p-XZ(route[i])).sqrMagnitude;if(d<dist){dist=d;best=i;}}
        dist=Mathf.Sqrt(dist);if(best<0)return false;at=route[best];width=widths[best];return true;
    }
    static float Protection(Vector2 p)
    {
        // Unchanged boss arenas and central cotton-hop route.
        float a=Smooth((Vector2.Distance(p,new Vector2(-185,1850))-51)/12);
        float b=Smooth((Vector2.Distance(p,new Vector2(-105,1755))-48)/12);
        return Mathf.Min(a,b);
    }
    static float ShapedY(Vector2 p,float original)
    {
        float y=original,r=LakeR(p);
        if(r<1.43f){
            float bed=r<.88f?LakeY-1.15f:Mathf.Lerp(LakeY-1.15f,LakeY+.72f,Smooth((r-.88f)/.19f));
            y=Mathf.Lerp(bed,original,Smooth((r-1.07f)/.36f));
        }
        if(Reach(p,out var at,out float w,out float d)&&d<w+28){
            float bed=at.y-Mathf.Lerp(.86f,.15f,Mathf.Pow(Mathf.Clamp01(d/w),4));
            float blend=1-Smooth((d-w)/28);
            if(r>1.03f||at.y<LakeY-.1f)y=Mathf.Lerp(y,at.z>2325?Mathf.Min(y,bed):bed,blend*Protection(p));
        }
        return y;
    }
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        mesh.name=name;
        if(meshes.TryGetValue(name,out var old)){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.AddObjectToAsset(mesh,library);meshes[name]=mesh;return mesh;
    }
    static Transform Group(string name,Transform parent)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Transform Surface(string name,Mesh mesh,Material mat,Transform parent)
    {
        var t=Group(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
        r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;r.motionVectorGenerationMode=MotionVectorGenerationMode.Camera;r.lightProbeUsage=LightProbeUsage.BlendProbes;return t;
    }
    static Material Material(string name,Shader shader,Color color,float flow=0,float lake=0)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
        m.SetColor("_BaseColor",color);m.enableInstancing=true;
        if(m.HasProperty("_FlowSpeed")){m.SetFloat("_FlowSpeed",flow);m.SetFloat("_LakeMode",lake);m.SetFloat("_Smoothness",.78f);m.SetFloat("_WaveHeight",lake>.5f?.09f:.11f);m.SetColor("_FoldColor",new Color(.49f,.223f,.095f));}
        else if(m.HasProperty("_Smoothness")) {m.SetFloat("_Smoothness",.26f);m.SetFloat("_Cull",0);}
        EditorUtility.SetDirty(m);return m;
    }
    static Mesh Mesh(Vector3[] v,Vector2[] uv,int[] tris,Vector4[] tangent=null)
    {
        var m=new Mesh{indexFormat=IndexFormat.UInt32,vertices=v,uv=uv,triangles=tris};m.RecalculateNormals();m.RecalculateBounds();
        if(tangent!=null)m.tangents=tangent;else m.RecalculateTangents();return m;
    }
    static int[] StripTriangles(int rows,int cols)
    {
        var t=new int[rows*cols*6];int k=0;for(int i=0;i<rows;i++)for(int j=0;j<cols;j++){int a=i*(cols+1)+j,b=a+cols+1;t[k++]=a;t[k++]=a+1;t[k++]=b;t[k++]=a+1;t[k++]=b+1;t[k++]=b;}return t;
    }
    static Vector2 Right(int i)
    {
        int a=i,b=i;while(a>0&&distances[i]-distances[a]<24)a--;while(b<route.Count-1&&distances[b]-distances[i]<24)b++;
        var d=XZ(route[b]-route[a]).normalized;return new Vector2(-d.y,d.x);
    }
    static Mesh RiverMesh()
    {
        const int cols=20;var v=new Vector3[route.Count*(cols+1)];var uv=new Vector2[v.Length];var tan=new Vector4[v.Length];
        for(int i=0;i<route.Count;i++){var right=Right(i);for(int j=0;j<=cols;j++){float u=j/(float)cols;var p=XZ(route[i])+right*((u*2-1)*widths[i]);int k=i*(cols+1)+j;
            v[k]=new Vector3(p.x,route[i].y+.025f*Mathf.Sin(u*Mathf.PI),p.y);uv[k]=new Vector2(u,distances[i]);tan[k]=new Vector4(right.x,0,right.y,1);}}
        return SaveMesh(Mesh(v,uv,StripTriangles(route.Count-1,cols),tan),"Continuous lake to cloud-cliff river");
    }
    static Mesh LakeMesh(bool shore)
    {
        const int cols=192;int rows=shore?8:32;var v=new Vector3[(rows+1)*(cols+1)];var uv=new Vector2[v.Length];
        for(int j=0;j<=cols;j++){float a=j/(float)cols*Mathf.PI*2;var edge=new Vector2(Mathf.Cos(a)*Radius.x,Mathf.Sin(a)*Radius.y)*Shape(a);
            for(int i=0;i<=rows;i++){float r=shore?Mathf.Lerp(.995f,1.12f,i/(float)rows):i/(float)rows;var p=Lake+edge*r;int k=i*(cols+1)+j;
                v[k]=new Vector3(p.x,shore?GroundY(p)+.04f:LakeY,p.y);uv[k]=shore?new Vector2(i/(float)rows,j/(float)cols*40):p/3;}}
        return SaveMesh(Mesh(v,uv,StripTriangles(rows,cols)),shore?"Cocoa butter shoreline":"Grand couverture lake");
    }
    static Mesh BanksMesh()
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int side=-1;side<=1;side+=2){int offset=v.Count;for(int i=0;i<route.Count;i++){var right=Right(i);for(int j=0;j<4;j++){float f=j/3f;var p=XZ(route[i])+right*side*(widths[i]+f*3.6f-.12f);float y=GroundY(p)+.04f;
                v.Add(new Vector3(p.x,y,p.y));uv.Add(new Vector2(f,distances[i]/3));}}
            var local=StripTriangles(route.Count-1,3);if(side<0)Array.Reverse(local);foreach(int n in local)tris.Add(n+offset);}
        return SaveMesh(Mesh(v.ToArray(),uv.ToArray(),tris.ToArray()),"Thin cocoa-stained river margins");
    }
    static Vector3 FallCurve(float t)
    {
        var lip=route[route.Count-1];float z=lip.z+278*t;float y=lip.y-610*Mathf.Pow(t,1.85f);return new Vector3(lip.x+20*t,y,z);
    }
    static Mesh WaterfallMesh(float offset,float scale,string name)
    {
        const int rows=160,cols=32;int front=(rows+1)*(cols+1);var v=new Vector3[front*2];var uv=new Vector2[v.Length];var tan=new Vector4[v.Length];float distance=distances.Last();var last=FallCurve(0);var lipRight=Right(route.Count-1);var startAcross=new Vector3(-lipRight.x,0,-lipRight.y);
        for(int i=0;i<=rows;i++){float t=i/(float)rows;var p=FallCurve(t);distance+=Vector3.Distance(p,last);last=p;float half=widths.Last()*scale*(1+.13f*Mathf.Sin(t*8));
            var across=Vector3.Slerp(startAcross,Vector3.right,Smooth(t/.18f));var direction=(FallCurve(Mathf.Min(1,t+.001f))-FallCurve(Mathf.Max(0,t-.001f))).normalized;var normal=Vector3.Cross(direction,across).normalized;
            for(int j=0;j<=cols;j++){float u=j/(float)cols,x=(u*2-1)*half;int k=i*(cols+1)+j;
                v[k]=p+across*(x+offset)+new Vector3(0,(1-Mathf.Abs(u*2-1))*.28f,Mathf.Sin(u*17+t*11)*.48f*Mathf.Sin(t*Mathf.PI));uv[k]=new Vector2(u,distance);tan[k]=new Vector4(across.x,across.y,across.z,1);
                v[k+front]=v[k]-normal*1.15f;uv[k+front]=uv[k];tan[k+front]=tan[k];}}
        var face=StripTriangles(rows,cols);Array.Reverse(face);var tris=new List<int>(face);var back=face.Select(n=>n+front).ToArray();Array.Reverse(back);tris.AddRange(back);
        for(int i=0;i<rows;i++)foreach(int side in new[]{0,cols}){int a=i*(cols+1)+side,b=a+cols+1;tris.AddRange(new[]{a,a+front,b,b,a+front,b+front});}
        return SaveMesh(Mesh(v,uv,tris.ToArray(),tan),name);
    }
    static void AddCocoaDroplets()
    {
        var go=Group("Sparse falling cocoa droplets - bounded spray",waterRoot).gameObject;go.transform.position=route.Last()+new Vector3(0,-.4f,8);
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.duration=6;main.loop=true;main.playOnAwake=true;main.maxParticles=72;
        main.startLifetime=new ParticleSystem.MinMaxCurve(2.8f,4.1f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.22f,.58f);main.gravityModifier=1.7f;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=14;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(widths.Last()*1.7f,.3f,2);
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(-1.1f,1.1f);velocity.y=new ParticleSystem.MinMaxCurve(-3,-1);velocity.z=new ParticleSystem.MinMaxCurve(9,15);
        var v=new List<Vector3>();var uv=new List<Vector2>();for(int i=0;i<=8;i++)for(int j=0;j<=12;j++){float a=i/8f*Mathf.PI,b=j/12f*Mathf.PI*2;v.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b)*.5f,Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)*.5f));uv.Add(new Vector2(j/12f,i/8f));}
        var mesh=SaveMesh(Mesh(v.ToArray(),uv.ToArray(),StripTriangles(8,12)),"Rounded falling cocoa beads");var r=ps.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Mesh;r.mesh=mesh;r.sharedMaterial=Material("Cocoa droplets",Shader.Find("Universal Render Pipeline/Lit"),new Color(.19f,.065f,.026f));r.sharedMaterial.SetFloat("_Smoothness",.75f);r.shadowCastingMode=ShadowCastingMode.Off;r.enableGPUInstancing=true;
    }
    static object SculptTerrain(Mesh source,MeshFilter[] surfaces)
    {
        var v=source.vertices;int changed=0;float maxCut=0,maxRaise=0;
        for(int i=0;i<v.Length;i++){var world=terrain.TransformPoint(v[i]);var p=XZ(world);
            if(world.y<2340||p.x< -370||p.x>135||p.y<1745||p.y>2370)continue;
            float target=ShapedY(p,world.y);float delta=target-world.y;if(Mathf.Abs(delta)<.002f)continue;
            // Only the exposed upper skin, never the underside of a marshmallow fold.
            if(!originalGround.Raycast(new Ray(new Vector3(p.x,2800,p.y),Vector3.down),out var hit,1900)||Mathf.Abs(hit.point.y-world.y)>.15f)continue;
            world.y=target;v[i]=terrain.InverseTransformPoint(world);changed++;maxCut=Mathf.Max(maxCut,-delta);maxRaise=Mathf.Max(maxRaise,delta);
        }
        var clone=UnityEngine.Object.Instantiate(source);clone.vertices=v;clone.RecalculateNormals();clone.RecalculateBounds();clone.RecalculateTangents();var mesh=SaveMesh(clone,"Island 2 local lake basin and continuous river bed");
        foreach(var f in surfaces){Undo.RecordObject(f,"Island 2 water basin");f.sharedMesh=mesh;EditorUtility.SetDirty(f);var c=f.GetComponent<MeshCollider>();if(c){Undo.RecordObject(c,"Island 2 water basin collider");c.sharedMesh=null;c.sharedMesh=mesh;EditorUtility.SetDirty(c);}}
        Physics.SyncTransforms();return new{surfaces=surfaces.Length,changedVertices=changed,maxCut,maxRaise,sourcePreserved=true};
    }
    // Rotated wafers have empty space inside the corners of their world bounds.
    // Find the actual support point before lowering a confection onto a sloping bank.
    static float SoleClearance(Transform prop)
    {
        float gap = float.PositiveInfinity;
        foreach (var filter in prop.GetComponentsInChildren<MeshFilter>())
        {
            if (!filter.sharedMesh) continue;
            foreach (var vertex in filter.sharedMesh.vertices)
            {
                Vector3 world = filter.transform.TransformPoint(vertex);
                if (ground.Raycast(new Ray(new Vector3(world.x, 2800f, world.z), Vector3.down), out var hit, 1900f))
                    gap = Mathf.Min(gap, world.y - hit.point.y);
            }
        }
        return gap;
    }

    public static object SeatConfections()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != SecondIslandAudio.ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open the saved Second Island scene in Edit mode.");
        ground = UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        var root = GameObject.Find("Island 2 - Environment Decoration").transform;
        int checkedProps = 0, settled = 0;
        float largestGap = 0f;
        foreach (var prop in root.GetComponentsInChildren<Transform>().Where(t => t.Find("Confection")))
        {
            checkedProps++;
            float gap = SoleClearance(prop);
            if (!float.IsFinite(gap) || gap <= .08f) continue;
            largestGap = Mathf.Max(largestGap, gap);
            prop.position -= Vector3.up * (gap + .04f);
            settled++;
        }
        if (settled > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save confection placements.");
        }
        return new { checkedProps, settled, largestGap };
    }

    static object AdjustDressing(Transform decoration)
    {
        int moved=0,hiddenCells=0;
        foreach(var cell in decoration.Find("Groundcover - spatially combined LOD cells").Cast<Transform>()){
            var renderers=cell.GetComponentsInChildren<Renderer>(true);bool wet=false;
            foreach(var r in renderers){var p=XZ(r.bounds.center);if(LakeR(p)<1.5f||(Reach(p,out var a,out float w,out float d)&&d<w+40)){wet=true;break;}}
            if(wet&&cell.gameObject.activeSelf){cell.gameObject.SetActive(false);hiddenCells++;}
        }
        // Keep authored scenery grounded on changed banks; remove baked old soil patches
        // whose individual vertices cannot follow the new basin/gorge.
        foreach(var patch in decoration.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Irregular weathered rootbed")){
            var p=XZ(patch.bounds.center);if(LakeR(p)<1.5f||(Reach(p,out var a,out float w,out float d)&&d<w+40))patch.gameObject.SetActive(false);
        }
        foreach(var t in decoration.GetComponentsInChildren<Transform>(true).Where(t=>t.Find("Confection")||t.name=="Weathered cocoa-side boulder"||t.name=="Wind shaped scrub"||t.name=="Crooked coastal 0"||t.name=="Upright woodland 1").ToArray()){
            var p=XZ(t.position);float r=LakeR(p);bool reach=Reach(p,out var at,out float width,out float distance);if(r>=1.44f&&(!reach||distance>width+35))continue;
            var next=p;
            if(r<1.14f){var delta=p-Lake;if(delta.sqrMagnitude<.01f)delta=Vector2.right;next=Lake+delta*(1.24f/Mathf.Max(.02f,r));}
            else if(reach&&distance<width+6){var side=(p-XZ(at)).normalized;if(side.sqrMagnitude<.1f)side=Vector2.right;next=XZ(at)+side*(width+12);}
            if(next!=p){var pos=t.position;t.position=new Vector3(next.x,pos.y,next.y);moved++;}
            var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;float bottom=rs.Min(renderer=>renderer.bounds.min.y);float correction=GroundY(next)-bottom-.15f;
            if (t.Find("Confection"))
            {
                float gap = SoleClearance(t);
                if (float.IsFinite(gap)) correction = -.12f - gap;
            }
            if(Mathf.Abs(correction)>.05f){t.position+=Vector3.up*correction;moved++;}
        }
        // Bank geology and skiff dressing stays grounded after the local terrain cut.
        foreach(string name in new[]{"Bank geology - clustered boulders, ferns and reeds","Split rock cocoa spring"}){
            var parent=decoration.Find("Chocolate River - The Couverture Run/"+name);if(!parent)continue;
            foreach(Transform t in parent){var p=XZ(t.position);if(t.position.y<2300)continue;float delta=ShapedY(p,Floor(p).y)-Floor(p).y;if(Mathf.Abs(delta)>.3f){var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;float bottom=rs.Min(r=>r.bounds.min.y);t.position+=Vector3.up*(ShapedY(p,Floor(p).y)-bottom-.12f);moved++;}}
        }
        foreach(var t in decoration.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Solid chocolate ford piece")){
            var p=XZ(t.position);if(!Reach(p,out var at,out float w,out float d))continue;var pos=t.position;pos.y=d<w?at.y+.025f:GroundY(p)+.03f;t.position=pos;
        }
        foreach(string name in new[]{"Abandoned cocoa sampling station","The stranded lighter - broken freight skiff"}){var t=decoration.Find(name);if(t){var p=t.position;p.y=GroundY(XZ(p));t.position=p;}}
        return new{movedConfectionsAndBankProps=moved,hiddenFloodedGroundcoverCells=hiddenCells};
    }
    static void AddLandmarks(Transform decoration)
    {
        // Reuse native biscuit/waffle meshes: two quiet shoreline arrangements and a cliff lip marker.
        var templates=decoration.GetComponentsInChildren<Transform>(true).Where(t=>t.Find("Confection")).ToArray();
        foreach(var spec in new[]{(name:"Biscuit shore - lake overlook",p:new Vector2(-100,2075),scale:1.55f),(name:"Waffle shore - outlet overlook",p:new Vector2(86,2035),scale:1.35f),(name:"Biscuit sentinels - cloudfall rim",p:new Vector2(-286,2335),scale:1.65f)}){
            var source=templates.FirstOrDefault(t=>t.name.Contains(spec.name.StartsWith("Waffle")?"waffle":"biscuit"));if(!source)source=templates.First();
            var copy=UnityEngine.Object.Instantiate(source.gameObject,waterRoot);copy.name=spec.name;float y=ShapedY(spec.p,Floor(spec.p).y);copy.transform.position=new Vector3(spec.p.x,y,spec.p.y);copy.transform.rotation=Quaternion.Euler(0,36,0);copy.transform.localScale*=spec.scale;
            // Scenic rim pieces never introduce a collision wall.
            foreach(var c in copy.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            float gap = SoleClearance(copy.transform);
            if (float.IsFinite(gap)) copy.transform.position -= Vector3.up * (gap + .12f);
        }
    }
    public static object Run()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=SecondIslandAudio.ScenePath||scene.isDirty)throw new Exception("Saved Island 2 Edit mode required");
        var currentAudio=UnityEngine.Object.FindFirstObjectByType<SecondIslandAudio>();
        if(currentAudio&&currentAudio.waterway)throw new InvalidOperationException("This scene uses RebuildIsland2ChocolateWaterway.");
        Directory.CreateDirectory(Out);Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        if(!File.Exists(Out+"/before/Second Island.unity")){Directory.CreateDirectory(Out+"/before");File.Copy(scene.path,Out+"/before/Second Island.unity");}
        var decoration=GameObject.Find("Island 2 - Environment Decoration").transform;var parent=decoration.Find("Chocolate River - The Couverture Run");
        ground=UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();terrain=ground.transform;
        var source=AssetDatabase.LoadAssetAtPath<Mesh>(Original);if(!source)source=AssetDatabase.LoadAllAssetsAtPath(Original).OfType<Mesh>().First();
        var surfaces=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.name=="default"&&f.GetComponent<MeshCollider>()&&f.sharedMesh.vertexCount==source.vertexCount).ToArray();
        var temporary=new GameObject("Original ground sampler"){hideFlags=HideFlags.HideAndDontSave};temporary.transform.SetPositionAndRotation(terrain.position,terrain.rotation);temporary.transform.localScale=terrain.lossyScale;originalGround=temporary.AddComponent<MeshCollider>();((MeshCollider)originalGround).sharedMesh=source;Physics.SyncTransforms();
        bool completed=false;
        try{
            string path=Art+"/Water system mesh library.asset";library=AssetDatabase.LoadAssetAtPath<Island2DecorationMeshLibrary>(path);
            if(!library){library=ScriptableObject.CreateInstance<Island2DecorationMeshLibrary>();library.name="Island 2 water system meshes";AssetDatabase.CreateAsset(library,path);}
            meshes.Clear();foreach(var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())meshes[m.name]=m;
            var old=parent.Find("Lake to Cloudfall - connected chocolate water system");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(string name in new[]{"Slow flowing couverture - walkable shallow spill","Cocoa mineral transition","Source spring pool","Terminal chocolate eddy","Sparse cocoa butter domes","Split rock cocoa spring"}){var t=parent.Find(name);if(t)t.gameObject.SetActive(false);}
            waterRoot=Group("Lake to Cloudfall - connected chocolate water system",parent);MakeRoute();
            var shader=Shader.Find("Dark Brine/Second Island/Viscous Chocolate");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Chocolate shader must compile");
            var cocoa = new Color(.175f, .068f, .031f);
            current = Material("Chocolate - continuous current", shader, cocoa, 2.1f);
            lakeMat = Material("Chocolate - slow lake circulation", shader, cocoa, .4f, 1);
            fallsMat = Material("Chocolate - accelerating cloudfall", shader, cocoa, 11);
            foreach (var material in new[] { current, lakeMat, fallsMat })
            {
                material.SetColor("_FoldColor", new Color(.30f, .132f, .065f));
                material.SetFloat("_Smoothness", .85f);
                material.SetFloat("_WaveHeight", .065f);
            }
            bankMat = Material("Cocoa stained marshmallow banks",
                Shader.Find("Dark Brine/Second Island/Ground Transition"), new Color(.25f, .105f, .056f));
            bankMat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Island/Materials/rocky_terrain_4k/textures/rocky_terrain_diff_4k.jpg"));
            bankMat.SetFloat("_Opacity", .85f);
            bankMat.SetFloat("_ChocolateBank", 1);
            bankMat.renderQueue = -1;
            var sculpt=SculptTerrain(source,surfaces);
            var lakeSurface = Surface("Grand Chocolate Lake - 170 metre headwater",LakeMesh(false),lakeMat,waterRoot);
            Surface("Cocoa butter shoreline - organic basin rim",LakeMesh(true),bankMat,waterRoot);
            var riverSurface = Surface("Continuous chocolate river - lake outlet to cliff",RiverMesh(),current,waterRoot);
            Surface("Cocoa stained channel edges",BanksMesh(),bankMat,waterRoot);
            Surface("The Cloudfall - broad falling chocolate curtain",WaterfallMesh(0,1,"Cloudfall main thick curtain"),fallsMat,waterRoot);
            AddCocoaDroplets();
            var contact = UnityEngine.Object.FindFirstObjectByType<SecondIslandChocolateContact>();
            if (contact)
            {
                var bindings = new SerializedObject(contact);
                bindings.FindProperty("lake").objectReferenceValue = lakeSurface.GetComponent<MeshRenderer>();
                bindings.FindProperty("river").objectReferenceValue = riverSurface.GetComponent<MeshRenderer>();
                bindings.ApplyModifiedPropertiesWithoutUndo();
            }
            var dressing=AdjustDressing(decoration);AddLandmarks(decoration);
            var sound=UnityEngine.Object.FindFirstObjectByType<SecondIslandAudio>();
            if(sound){sound.riverPoints=route.Where((p,i)=>i%4==0||i==route.Count-1).ToArray();sound.riverHalfWidths=widths.Where((w,i)=>i%4==0||i==widths.Count-1).ToArray();
                sound.chocolateLakeCenter=new Vector3(Lake.x,LakeY,Lake.y);sound.chocolateLakeRadii=Radius;sound.chocolateFallLip=route.Last();sound.confectionPoints=decoration.GetComponentsInChildren<Transform>().Where(t=>t.Find("Confection")).Where((t,i)=>i%5==0).Select(t=>t.position).ToArray();EditorUtility.SetDirty(sound);}
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var result=new{lakeCenter=new[]{Lake.x,LakeY,Lake.y},lakeDimensions=new[]{Radius.x*2,Radius.y*2},riverMetres=distances.Last(),riverWidthMin=widths.Min()*2,riverWidthMax=widths.Max()*2,fallLip=route.Last().ToString(),fallBottom=FallCurve(1).ToString(),fallDrop=610,sculpt,dressing,
                waterTriangles=waterRoot.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),newWaterColliders=waterRoot.GetComponentsInChildren<Collider>().Length};
            File.WriteAllText(Out+"/build.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            using(var w=new StreamWriter(Out+"/route.csv")){w.WriteLine("x,y,z,halfWidth,distance");for(int i=0;i<route.Count;i++)w.WriteLine($"{route[i].x},{route[i].y},{route[i].z},{widths[i]},{distances[i]}");}
            completed=true;return result;
        }finally{UnityEngine.Object.DestroyImmediate(temporary);Undo.FlushUndoRecordObjects();if(completed)EditorSceneManager.SaveScene(scene);}
    }
}
