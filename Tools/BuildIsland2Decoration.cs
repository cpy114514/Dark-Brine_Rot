using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Run through Unity CLI run_script. All native objects/assets are authored in the live Editor.
public static class BuildIsland2Decoration
{
    const string Art="Assets/Game/Art/Island2Decoration";
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    const string RootName="Island 2 - Environment Decoration";
    static Collider ground;static Transform root;static System.Random rng;
    static int trees,rocks,shrubs,tufts,ferns,branches;static Material grass,fern,stone,wood,bank,chocolate,metal,soil;
    static readonly Dictionary<string,Cell> cells=new Dictionary<string,Cell>();
    static readonly Dictionary<Material,Material> treeMats=new Dictionary<Material,Material>();
    static Dictionary<string,Mesh> archivedMeshes;
    static Mesh biscuitMesh,chipMesh,barMesh;static Material biscuitMat,darkChocolate,milkChocolate;static int desserts;
    static readonly Vector2[][] Routes={
        new[]{new Vector2(-147,1608),new Vector2(-130,1650),new Vector2(-94,1676),new Vector2(-79,1715),new Vector2(-105,1755)},
        new[]{new Vector2(-105,1755),new Vector2(-145,1800),new Vector2(-185,1850),new Vector2(-155,1940),new Vector2(-147,2198)},
        new[]{new Vector2(-79,1715),new Vector2(-45,1788),new Vector2(-25,1840),new Vector2(30,1907),new Vector2(98,1945)},
        new[]{new Vector2(98,1945),new Vector2(170,1890),new Vector2(410,1820),new Vector2(730,1725),new Vector2(1010,1610)},
        new[]{new Vector2(-155,1940),new Vector2(-255,2090),new Vector2(-448,2328)},
        new[]{new Vector2(-147,1608),new Vector2(-65,1410),new Vector2(65,1110),new Vector2(180,830)}};
    static readonly Vector2[] River={new Vector2(40,2005),new Vector2(52,1957),new Vector2(30,1933),new Vector2(5,1910),new Vector2(-10,1883),new Vector2(-25,1857),new Vector2(-32,1832),new Vector2(-51,1810),new Vector2(-77,1795),new Vector2(-104,1784),new Vector2(-140,1781)};
    class Cell {public Vector3 origin;public Material material;public List<CombineInstance> near=new List<CombineInstance>(),far=new List<CombineInstance>();}
    static float F(float a,float b)=>(float)(a+rng.NextDouble()*(b-a));
    static Vector2 J(float radius){float a=F(0,Mathf.PI*2),r=Mathf.Sqrt(F(0,1))*radius;return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}
    static Vector3 V(Vector2 p)=>new Vector3(p.x,0,p.y);
    static bool Hit(Vector2 p,out RaycastHit h)=>ground.Raycast(new Ray(new Vector3(p.x,ground.bounds.max.y+20,p.y),Vector3.down),out h,ground.bounds.size.y+50);
    static Vector3 Floor(Vector2 p){if(!Hit(p,out var h))throw new Exception("No ground: "+p);return h.point;}
    static Transform Group(string name,Transform parent=null){var g=new GameObject(name).transform;g.SetParent(parent?parent:root,false);return g;}
    static Material Mat(string name,Color c,float smooth=.25f,Material source=null)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=source?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;AssetDatabase.CreateAsset(m,path);}
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);
        m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static T Load<T>(string p) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(p);if(!a)throw new Exception("Missing asset: "+p);return a;}
    static Mesh Detail(string n,string lod)=>Load<Mesh>("Assets/Generated/Island1Realism/"+n+" - "+lod+".asset");
    static Mesh SaveMesh(Mesh m,string n){m.name=n;var old=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/"+n+".asset");
        if(!old){if(archivedMeshes==null)archivedMeshes=AssetDatabase.FindAssets("t:Island2DecorationMeshLibrary",new[]{Art}).Select(AssetDatabase.GUIDToAssetPath).SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Mesh>().GroupBy(a=>a.name).ToDictionary(a=>a.Key,a=>a.First());archivedMeshes.TryGetValue(n,out old);}
        if(old){EditorUtility.CopySerialized(m,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,Art+"/"+n+".asset");return m;}
    static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
    static float PathDistance(Vector2 p,Vector2[] pts){float d=float.MaxValue;for(int i=1;i<pts.Length;i++)d=Mathf.Min(d,Segment(p,pts[i-1],pts[i]));return d;}
    static bool Safe(Vector2 p,float size=0,bool riverAllowed=false)
    {
        if(Vector2.Distance(p,new Vector2(-105,1755))<49+size||Vector2.Distance(p,new Vector2(-185,1850))<53+size||Vector2.Distance(p,new Vector2(-79,1715))<13+size||Vector2.Distance(p,new Vector2(-147,1610))<17+size||Vector2.Distance(p,new Vector2(98.6f,1945))<20+size||Vector2.Distance(p,new Vector2(-147,2205))<34+size)return false;
        foreach(var path in Routes)if(PathDistance(p,path)<4.5f+size)return false;
        if(!riverAllowed&&PathDistance(p,River)<16+size)return false;
        // Keep every existing collectible/NPC/candy interaction accessible.
        foreach(var q in protectedPoints)if(Vector2.Distance(p,q)<6.5f+size)return false;
        return Hit(p,out var h)&&h.normal.y>.65f;
    }
    static Vector2[] protectedPoints;
    static void AddDetail(string name,Material mat,Vector2 p,Vector3 scale,float yaw,bool allowRiver=false)
    {
        if(!Safe(p,0,allowRiver)||!Hit(p,out var h))return;
        int x=Mathf.FloorToInt(p.x/28),z=Mathf.FloorToInt(p.y/28);string key=mat.name+"_"+x+"_"+z;
        if(!cells.TryGetValue(key,out var c)){c=new Cell{origin=new Vector3(x*28,Mathf.Floor(h.point.y/28)*28,z*28),material=mat};cells.Add(key,c);}
        var rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,h.normal,.7f))*Quaternion.Euler(0,yaw,0);
        var transform=Matrix4x4.TRS(h.point-c.origin-h.normal*.025f,rotation,scale);
        // These small plants are seen from a third-person camera. Reuse existing
        // medium grass / reduced fern meshes instead of duplicating hero detail.
        string nearLod=name.StartsWith("Tuft")?"middle":name.Contains("fern")?"far":"near";
        c.near.Add(new CombineInstance{mesh=Detail(name,nearLod),transform=transform});c.far.Add(new CombineInstance{mesh=Detail(name,"far"),transform=transform});
        if(name.StartsWith("Tuft"))tufts++;else if(name.Contains("fern"))ferns++;else if(name.Contains("branch"))branches++;
    }
    static void FlushCells()
    {
        var parent=Group("Groundcover - spatially combined LOD cells");
        AssetDatabase.StartAssetEditing();
        try { foreach(var pair in cells){var c=pair.Value;var holder=Group(pair.Key,parent);holder.position=c.origin;var rr=new Renderer[2];
            for(int l=0;l<2;l++){var combines=l==0?c.near:c.far;var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines.ToArray(),true,true);mesh.RecalculateBounds();mesh=SaveMesh(mesh,pair.Key+"_LOD"+l);
                var o=Group("LOD"+l,holder).gameObject;o.AddComponent<MeshFilter>().sharedMesh=mesh;var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=c.material;r.shadowCastingMode=ShadowCastingMode.Off;rr[l]=r;}
            var lod=holder.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.5f,new[]{rr[0]}),new LOD(.17f,new[]{rr[1]})});lod.RecalculateBounds();lod.size=40;} }
        finally {AssetDatabase.StopAssetEditing();}
    }
    static Transform Prop(string path,string name,Vector2 site,float height,Transform parent,float yaw,bool collider=false)
    {
        if(!Hit(site,out var h))return null;var prefab=Load<GameObject>(path);var holder=Group(name,parent);holder.position=h.point;holder.rotation=Quaternion.Euler(0,yaw,0);
        var instance=UnityEngine.Object.Instantiate(prefab,holder,false);instance.name=prefab.name;
        foreach(var co in instance.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(co);
        var box=new Bounds();bool first=true;foreach(var r in instance.GetComponentsInChildren<Renderer>(true)){if(first){box=r.bounds;first=false;}else box.Encapsulate(r.bounds);}
        float scale=height/Mathf.Max(.01f,box.size.y);instance.transform.localScale*=scale;
        box=new Bounds();first=true;foreach(var r in instance.GetComponentsInChildren<Renderer>(true)){if(first){box=r.bounds;first=false;}else box.Encapsulate(r.bounds);}
        instance.transform.position+=h.point-new Vector3(box.center.x,box.min.y+.08f,box.center.z);
        if(collider){var c=holder.gameObject.AddComponent<CapsuleCollider>();c.radius=Mathf.Min(1.1f,height*.12f);c.height=height*.67f;c.center=Vector3.up*(c.height*.5f);}
        return holder;
    }
    static void Tree(Vector2 p,Transform parent,float height)
    {
        if(!Safe(p,3))return;string model=rng.Next(2)==0?"Crooked coastal 0":"Upright woodland 1";
        var t=Prop("Assets/Generated/Island1Woodland/"+model+".prefab",model,p,height,parent,F(0,360),true);
        foreach(var r in t.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var m=mats[i];if(!treeMats.TryGetValue(m,out var copy)){copy=Mat("Cloud grove "+m.name,m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white,.32f,m);if(copy.HasProperty("_ClipBelowSea"))copy.SetFloat("_ClipBelowSea",0);treeMats.Add(m,copy);}mats[i]=copy;}r.sharedMaterials=mats;}
        var lod=t.GetComponentInChildren<LODGroup>();if(lod){var ls=lod.GetLODs();if(ls.Length>=2){ls[0].screenRelativeTransitionHeight=.12f;ls[1].screenRelativeTransitionHeight=.025f;lod.SetLODs(ls);foreach(var r in ls[1].renderers)r.shadowCastingMode=ShadowCastingMode.Off;}}trees++;
    }
    static void Rock(Vector2 p,Transform parent,float height,bool safe=true)
    {
        if(safe&&!Safe(p,height*.6f))return;if(!Hit(p,out var h)||h.normal.y<.5f)return;
        var source=Load<GameObject>("Assets/Resources/SecondIslandRealism/rock_moss_set_01.prefab");var filters=source.GetComponentsInChildren<MeshFilter>();var f=filters[rng.Next(filters.Length)];
        var o=Group("Weathered cocoa-side boulder",parent);var mesh=f.sharedMesh;o.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=o.gameObject.AddComponent<MeshRenderer>();r.sharedMaterials=f.GetComponent<Renderer>().sharedMaterials;
        float s=height/mesh.bounds.size.y;o.localScale=new Vector3(s*F(.85f,1.3f),s,s*F(.9f,1.2f));o.rotation=Quaternion.FromToRotation(Vector3.up,h.normal)*Quaternion.Euler(F(-4,4),F(0,360),F(-4,4));
        o.position=h.point-o.TransformVector(new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z))-h.normal*(height*.23f);
        if(height>3.8f&&safe){var c=o.gameObject.AddComponent<SphereCollider>();c.center=mesh.bounds.center;c.radius=Mathf.Min(mesh.bounds.extents.x,mesh.bounds.extents.z)*.75f;}
        var lod=o.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.013f,new Renderer[]{r})});lod.RecalculateBounds();rocks++;
    }
    static void Grove(Vector2 center,float radius,int count,Transform parent,bool tall=true)
    {
        if(Safe(center,0))GroundPatch(center,radius*.83f,parent);
        for(int i=0;i<count;i++){var p=center+J(radius);if(!Safe(p))continue;float s=F(.7f,1.7f);AddDetail("Tuft "+rng.Next(4),grass,p,Vector3.one*s,F(0,360));
            if(i%7==0)AddDetail("Coppercoil fern",fern,center+J(radius*.82f),Vector3.one*F(.9f,1.8f),F(0,360));
            if(i%19==0)AddDetail("Shingle cluster A",stone,center+J(radius),Vector3.one*F(.8f,2),F(0,360));
            if(i%37==0)AddDetail("Stranded branch",wood,center+J(radius),Vector3.one*F(.7f,1.6f),F(0,360));}
        for(int i=0;i<2;i++)Rock(center+J(radius*.7f),parent,F(1.2f,3.2f));
        if(tall&&Mathf.Abs(Mathf.RoundToInt(center.x+center.y))%3==0)Tree(center+J(radius*.55f),parent,F(5.5f,8));
        for(int i=0;i<4;i++)Dessert(center+J(radius*.75f),parent,F(4.5f,8f),rng.Next(4));
        for(int i=0;i<1;i++){var p=center+J(radius*.65f);if(Safe(p,1)){var t=Prop("Assets/ThirdParty/SecondIslandCloudSea/HighlandShrub.glb","Wind shaped scrub",p,F(.9f,1.65f),parent,F(0,360));
            if(t){var rs=t.GetComponentsInChildren<Renderer>();foreach(var r in rs)r.shadowCastingMode=ShadowCastingMode.Off;var lod=t.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,rs)});lod.RecalculateBounds();shrubs++;}}}
    }
    static void GroundPatch(Vector2 center,float radius,Transform parent)
    {
        const int rings=7,sides=32;var v=new List<Vector3>();var uv=new List<Vector2>();var edge=new List<Vector2>();var tr=new List<int>();
        float phase=center.x*.23f+center.y*.18f;
        for(int r=0;r<=rings;r++)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;float f=(float)r/rings;float w=radius*f*(1+.16f*Mathf.Sin(a*3+phase)+.1f*Mathf.Cos(a*7-phase));var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a)*.8f)*w;
            v.Add(Floor(p)+Vector3.up*.034f);uv.Add(p*.18f);edge.Add(new Vector2(.5f+.5f*f,.5f));int k=r*(sides+1)+i;if(r<rings&&i<sides){int b=k+sides+1;tr.AddRange(new[]{k,k+1,b,k+1,b+1,b});}}
        var m=new Mesh{vertices=v.ToArray(),uv=uv.ToArray(),uv2=edge.ToArray(),triangles=tr.ToArray()};m.RecalculateNormals();var mesh=SaveMesh(m,"Rootbed "+center.x+" "+center.y);var t=MeshObject("Irregular weathered rootbed",mesh,soil,parent);t.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void Environment()
    {
        var valley=Group("Biscuit groves - chocolate and waffle clearings");
        var centers=new[]{new Vector2(-193,1650),new Vector2(-220,1710),new Vector2(-37,1722),new Vector2(-21,1793),new Vector2(-73,1855),new Vector2(13,1855),new Vector2(58,1885),new Vector2(146,1930),new Vector2(152,1982),new Vector2(64,2010),new Vector2(-7,1990),new Vector2(-84,1960),new Vector2(-203,1948),new Vector2(-244,1890),new Vector2(-258,1808),new Vector2(-233,1763),new Vector2(-138,2050),new Vector2(-190,2110),new Vector2(-93,2140),new Vector2(-80,2072),new Vector2(24,2059),new Vector2(102,2095),new Vector2(-281,1994),new Vector2(203,1855)};
        foreach(var p in centers)Grove(p,21,185,valley);
        var connections=Group("Confection trails - wafer and biscuit landmarks");
        for(int r=3;r<Routes.Length;r++){var route=Routes[r];for(int i=1;i<route.Length;i++){float length=Vector2.Distance(route[i-1],route[i]);for(float d=30;d<length;d+=70){var along=(route[i]-route[i-1]).normalized;var side=new Vector2(-along.y,along.x);var p=Vector2.Lerp(route[i-1],route[i],d/length)+side*(rng.Next(2)==0?-16:16);Grove(p,14,90,connections,rng.Next(3)==0);}}}
        var cliffs=Group("Cloud coast - fractured shelves and drift wrack");
        var shores=new[]{new Vector2(1010,1610),new Vector2(-448,2328),new Vector2(180,830)};
        foreach(var p in shores){for(int i=0;i<6;i++){var s=p+J(38);Rock(s,cliffs,F(4,10));}Grove(p+new Vector2(-25,18),19,160,cliffs,false);for(int i=0;i<12;i++)AddDetail("Stranded branch",wood,p+J(34),Vector3.one*F(1,2.8f),F(0,360));}
    }
    static Transform MeshObject(string name,Mesh mesh,Material mat,Transform parent){var t=Group(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;return t;}
    static Transform Part(Transform p,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Quaternion rot)
    {var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.transform.localRotation=rot;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=mat;return o.transform;}
    static Mesh RoundedBlock()
    {
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();float[] grid={-.5f,-.42f,.42f,.5f};
        for(int face=0;face<6;face++){int start=vertices.Count;var normal=new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}[face];var axis=face<2?Vector3.up:Vector3.right;var other=Vector3.Cross(normal,axis);
            for(int y=0;y<4;y++)for(int x=0;x<4;x++){var p=normal*.5f+axis*grid[x]+other*grid[y];var inner=new Vector3(Mathf.Clamp(p.x,-.42f,.42f),Mathf.Clamp(p.y,-.42f,.42f),Mathf.Clamp(p.z,-.42f,.42f));var n=(p-inner).normalized;vertices.Add(inner+n*.08f);normals.Add(n);uv.Add(new Vector2(x/3f,y/3f));}
            for(int y=0;y<3;y++)for(int x=0;x<3;x++){int a=start+y*4+x,b=a+4;indices.AddRange(new[]{a,a+1,b,a+1,b+1,b});}}
        var mesh=new Mesh{vertices=vertices.ToArray(),normals=normals.ToArray(),uv=uv.ToArray(),triangles=indices.ToArray()};mesh.RecalculateBounds();return mesh;
    }
    static void PrepareDesserts()
    {
        desserts=0;biscuitMat=Mat("Baked biscuit crumb",new Color(.94f,.66f,.33f),.24f);darkChocolate=Mat("Solid dark chocolate",new Color(.21f,.065f,.025f),.58f);milkChocolate=Mat("Solid milk chocolate",new Color(.43f,.19f,.075f),.5f);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Biscuit baked pores.asset");
        if(!texture){texture=new Texture2D(256,256,TextureFormat.RGB24,true){name="Biscuit baked pores",wrapMode=TextureWrapMode.Repeat};var pixels=new Color[256*256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){float fine=Mathf.PerlinNoise(x*.31f,y*.31f),coarse=Mathf.PerlinNoise(x*.04f,y*.04f);float pores=Mathf.Pow(Mathf.Max(0,(fine-.57f)*3.4f),2);float v=Mathf.Clamp01(.82f+coarse*.18f-pores*.37f);pixels[y*256+x]=new Color(v,v*.96f,v*.87f);}
            texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,Art+"/Biscuit baked pores.asset");}
        biscuitMat.SetTexture("_BaseMap",texture);biscuitMat.SetTextureScale("_BaseMap",Vector2.one*3);EditorUtility.SetDirty(biscuitMat);
        var vv=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int sides=80;
        var profile=new[]{new Vector2(0,.16f),new Vector2(.42f,.15f),new Vector2(.82f,.12f),new Vector2(.98f,.06f),new Vector2(1,-.04f),new Vector2(.94f,-.14f),new Vector2(.1f,-.15f),new Vector2(0,-.15f)};
        for(int r=0;r<profile.Length;r++)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;float radius=profile[r].x*(1+.025f*Mathf.Sin(a*13)+.018f*Mathf.Cos(a*23));var p=new Vector3(Mathf.Cos(a)*radius,profile[r].y+.008f*Mathf.Sin(a*17)*profile[r].x,Mathf.Sin(a)*radius);vv.Add(p);uv.Add(new Vector2(p.x*.5f+.5f,p.z*.5f+.5f));if(r<profile.Length-1&&i<sides){int k=r*(sides+1)+i,b=k+sides+1;tr.AddRange(new[]{k,k+1,b,k+1,b+1,b});}}
        var cookie=new Mesh{vertices=vv.ToArray(),uv=uv.ToArray(),triangles=tr.ToArray()};cookie.RecalculateNormals();biscuitMesh=SaveMesh(cookie,"Bevelled baked biscuit");
        var block=RoundedBlock();var chips=new List<CombineInstance>();var random=new System.Random(613);
        for(int i=0;i<19;i++){float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble())*.83f;float s=.07f+(float)random.NextDouble()*.08f;chips.Add(new CombineInstance{mesh=block,transform=Matrix4x4.TRS(new Vector3(Mathf.Cos(a)*r,.155f,Mathf.Sin(a)*r),Quaternion.Euler(8,i*137.5f,12),new Vector3(s,.06f,s*.9f))});}
        var cm=new Mesh();cm.CombineMeshes(chips.ToArray(),true,true);chipMesh=SaveMesh(cm,"Biscuit chocolate chips");
        var tiles=new List<CombineInstance>{new CombineInstance{mesh=block,transform=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(2,.19f,1.5f))}};
        for(int z=0;z<3;z++)for(int x=0;x<4;x++)tiles.Add(new CombineInstance{mesh=block,transform=Matrix4x4.TRS(new Vector3((x-1.5f)*.48f,.18f,(z-1)*.48f),Quaternion.identity,new Vector3(.43f,.27f,.43f))});
        var bm=new Mesh();bm.CombineMeshes(tiles.ToArray(),true,true);barMesh=SaveMesh(bm,"Segmented bevelled chocolate bar");UnityEngine.Object.DestroyImmediate(block);
    }
    static void Dessert(Vector2 site,Transform parent,float size,int variant)
    {
        if(!Safe(site,size*.55f)||!Hit(site,out var contact))return;var holder=Group(new[]{"Choc chip biscuit outcrop","Layered biscuit stack","Leaning waffle slabs","Broken chocolate bar cluster"}[variant],parent);holder.position=contact.point;holder.rotation=Quaternion.FromToRotation(Vector3.up,contact.normal);
        var visual=Group("Confection",holder);visual.localRotation=Quaternion.Euler(0,F(0,360),0);
        if(variant==0){visual.localScale=Vector3.one*(size*.5f);visual.localRotation*=Quaternion.Euler(F(56,82),0,F(-15,15));MeshObject("Baked biscuit",biscuitMesh,biscuitMat,visual);MeshObject("Chocolate chips",chipMesh,darkChocolate,visual);}
        if(variant==1){visual.localScale=Vector3.one*(size*.5f);for(int i=0;i<3;i++){var layer=Group("Biscuit layer",visual);layer.localPosition=new Vector3(i*.04f,i*.36f,0);layer.localRotation=Quaternion.Euler(0,i*19,0);MeshObject("Toasted biscuit",biscuitMesh,biscuitMat,layer);MeshObject("Chocolate chips",chipMesh,darkChocolate,layer);}}
        if(variant==2){var source=Load<GameObject>("Assets/Game/Resources/SecondIslandProps/Kenney/waffle.fbx");for(int i=0;i<2;i++){var o=UnityEngine.Object.Instantiate(source,visual,false);o.transform.localRotation=Quaternion.Euler(60+i*17,i*25,8);o.transform.localScale=Vector3.one*(size/.32f)*(i==0?1:.65f);o.transform.localPosition=new Vector3(i*size*.35f,0,i*.4f);foreach(var c in o.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var r in o.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=biscuitMat;}}
        if(variant==3){visual.localScale=Vector3.one*(size*.5f);for(int i=0;i<2;i++){var t=MeshObject("Segmented chocolate",barMesh,i==0?darkChocolate:milkChocolate,visual);t.localPosition=new Vector3(i*.6f,i*.44f,i*.27f);t.localRotation=Quaternion.Euler(i*23,i*27,i*12);}}
        // Fit actual mesh vertices, not the corners of a rotated disk's AABB.
        // The latter can leave round biscuits and detached chips visibly hovering.
        float clearance=float.PositiveInfinity;var localBounds=new Bounds();bool measured=false;
        foreach(var f in holder.GetComponentsInChildren<MeshFilter>()){var vertices=f.sharedMesh.vertices;for(int i=0;i<vertices.Length;i++){var world=f.transform.TransformPoint(vertices[i]);var local=holder.InverseTransformPoint(world);if(measured)localBounds.Encapsulate(local);else{localBounds=new Bounds(local,Vector3.zero);measured=true;}
            if(i%4==0&&Hit(new Vector2(world.x,world.z),out var floor))clearance=Mathf.Min(clearance,world.y-floor.point.y);}}
        holder.position-=Vector3.up*(clearance+.15f);
        var renderers=holder.GetComponentsInChildren<Renderer>();
        var lod=holder.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.018f,renderers)});lod.RecalculateBounds();
        if(localBounds.size.y>2.5f){var collider=holder.gameObject.AddComponent<BoxCollider>();collider.center=localBounds.center;collider.size=localBounds.size*.83f;}
        desserts++;
    }
    static Vector2 Curve(float t)
    {float f=t*(River.Length-1);int i=Mathf.Min(Mathf.FloorToInt(f),River.Length-2);float u=f-i;var a=River[Mathf.Max(0,i-1)];var b=River[i];var c=River[i+1];var d=River[Mathf.Min(River.Length-1,i+2)];return .5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);}
    static float Width(float t)=>1.9f*(3.8f+1.1f*Mathf.Sin(t*13)+.5f*Mathf.Sin(t*31)+2.5f*Mathf.Pow(t,9));
    static Mesh Ribbon(bool isBank)
    {
        const int rows=500,cols=20;var v=new Vector3[(rows+1)*(cols+1)];var uv=new Vector2[v.Length];var tangent=new Vector4[v.Length];var triangles=new int[rows*cols*6];float distance=0;var last=Curve(0);
        for(int i=0;i<=rows;i++){float t=(float)i/rows;var at=Curve(t);distance+=Vector2.Distance(at,last);last=at;var dir=(Curve(Mathf.Min(1,t+.001f))-Curve(Mathf.Max(0,t-.001f))).normalized;var right=new Vector2(-dir.y,dir.x);float w=Width(t)+(isBank?2.6f:0);
            for(int j=0;j<=cols;j++){float a=(float)j/cols;var p=at+right*((a*2-1)*w);int k=i*(cols+1)+j;var h=Floor(p);v[k]=h+Vector3.up*(isBank?.045f:.17f+Mathf.Sin(a*Mathf.PI)*.045f);uv[k]=new Vector2(a,distance);tangent[k]=new Vector4(right.x,0,right.y,1);}
            if(i<rows)for(int j=0;j<cols;j++){int k=(i*cols+j)*6,a=i*(cols+1)+j,b=a+cols+1;triangles[k]=a;triangles[k+1]=a+1;triangles[k+2]=b;triangles[k+3]=a+1;triangles[k+4]=b+1;triangles[k+5]=b;}}
        var m=new Mesh{indexFormat=IndexFormat.UInt32,vertices=v,uv=uv,triangles=triangles};m.RecalculateNormals();m.tangents=tangent;m.uv2=uv.Select((a,k)=>new Vector2(a.x,(float)(k/(cols+1))/rows)).ToArray();m.RecalculateBounds();return SaveMesh(m,isBank?"Cocoa stained banks":"Chocolate river ribbon");
    }
    static Mesh Basin(Vector2 at,float radius,string name)
    {
        const int rings=12,sides=80;var v=new Vector3[(rings+1)*(sides+1)];var uv=new Vector2[v.Length];var tan=new Vector4[v.Length];var tr=new List<int>();
        for(int r=0;r<=rings;r++)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;var p=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius*r/rings*(1+Mathf.Sin(a*5)*.07f));int k=r*(sides+1)+i;v[k]=Floor(p)+Vector3.up*.18f;uv[k]=new Vector2(.5f+.45f*Mathf.Cos(a)*r/rings,r*.35f+Mathf.Sin(a)*.6f);tan[k]=new Vector4(1,0,0,1);if(r<rings&&i<sides){int b=k+sides+1;tr.AddRange(new[]{k,b,k+1,k+1,b,b+1});}}
        for(int k=0;k<tr.Count;k+=3){int swap=tr[k+1];tr[k+1]=tr[k+2];tr[k+2]=swap;}
        var m=new Mesh{vertices=v,uv=uv,triangles=tr.ToArray(),tangents=tan};m.RecalculateNormals();return SaveMesh(m,name);
    }
    static void RiverPass()
    {
        var river=Group("Chocolate River - The Couverture Run");
        var water=MeshObject("Slow flowing couverture - walkable shallow spill",Ribbon(false),chocolate,river);water.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        MeshObject("Cocoa mineral transition",Ribbon(true),bank,river);
        MeshObject("Source spring pool",Basin(River[0],10,"Spring pool"),chocolate,river);
        MeshObject("Terminal chocolate eddy",Basin(River[River.Length-1],16,"Terminal eddy"),chocolate,river);
        var banks=Group("Bank geology - clustered boulders, ferns and reeds",river);
        for(int i=4;i<97;i+=3){float t=i/100f;var p=Curve(t);var dir=(Curve(t+.005f)-Curve(t-.005f)).normalized;var side=new Vector2(-dir.y,dir.x);int sign=i%2==0?1:-1;var site=p+side*(Width(t)+F(1.8f,4.8f))*sign;
            if(Safe(site,1,true)){Rock(site,banks,F(.7f,2));Dessert(site+J(5),banks,F(3.2f,5),i%4);for(int j=0;j<8;j++){var q=site+J(3);AddDetail("Tuft 1",grass,q,Vector3.one*F(.8f,1.7f),F(0,360),true);}}}
        var spring=Group("Split rock cocoa spring",river);for(int i=0;i<6;i++)Rock(River[0]+new Vector2(Mathf.Cos(i*1.15f),Mathf.Sin(i*1.15f))*8,spring,F(3,6),false);
        // Crossing markers sit on existing ground. No bridge/river collider changes traversal.
        foreach(float t in new[]{.22f,.52f,.77f}){var at=Curve(t);var dir=(Curve(t+.003f)-Curve(t-.003f)).normalized;var side=new Vector2(-dir.y,dir.x);var group=Group("Shallow ford - flush stepping shale",river);
            int steps=Mathf.CeilToInt(Width(t)/1.3f);for(int i=-steps;i<=steps;i++){var p=at+side*i*1.3f;var pos=Floor(p)+Vector3.up*.15f;Part(group,"Solid chocolate ford piece",PrimitiveType.Sphere,pos,new Vector3(1.7f,.27f,2.1f),milkChocolate,Quaternion.Euler(0,F(-20,20),0));}}
        var bubbles=Group("Sparse cocoa butter domes",river);var glaze=Mat("Chocolate bubbles",new Color(.22f,.072f,.025f),.88f);
        for(int i=0;i<24;i++){float t=F(.05f,.95f);var p=Curve(t)+J(Width(t)*.55f);float s=F(.12f,.36f);Part(bubbles,"Heavy trapped bubble",PrimitiveType.Sphere,Floor(p)+Vector3.up*.15f,new Vector3(s*2,s*.65f,s*2),glaze,Quaternion.identity);}
    }
    static void Timber(Transform parent,string name,Vector3 a,Vector3 b,float width,Material mat)
    {var d=b-a;Part(parent,name,PrimitiveType.Cube,(a+b)*.5f,new Vector3(width,width,d.magnitude),mat,Quaternion.LookRotation(d));}
    static void WreckAndStation()
    {
        var wreck=Group("The stranded lighter - broken freight skiff");wreck.position=Floor(new Vector2(-246,1970));wreck.rotation=Quaternion.Euler(0,27,-7);
        for(int i=0;i<11;i++){float z=(i-5)*1.25f;float w=2.9f*Mathf.Sqrt(Mathf.Max(.05f,1-z*z/65));
            Timber(wreck,"Exposed port rib",new Vector3(-w,.3f,z),new Vector3(-w*.8f,2.3f,z),.22f,wood);Timber(wreck,"Exposed starboard rib",new Vector3(w,.3f,z),new Vector3(w*.8f,2.3f,z),.22f,wood);
            Timber(wreck,"Broken floor frame",new Vector3(-w,.3f,z),new Vector3(w,.3f,z),.24f,wood);}
        for(int side=-1;side<=1;side+=2)for(int level=0;level<3;level++)for(int k=0;k<3;k++){if((side+level+k)%4==0)continue;float z=-5+k*4;Timber(wreck,"Splintered hull strake",new Vector3(side*2.5f,.55f+level*.57f,z),new Vector3(side*2.7f,.65f+level*.6f,z+3.4f),.25f,wood);}
        Timber(wreck,"Broken keel",new Vector3(0,.6f,-8),new Vector3(0,.4f,8),.48f,wood);
        for(int i=0;i<13;i++){float x=(i%5-2)*.42f,z=-4+(i/5)*3;Timber(wreck,"Torn deck plank",new Vector3(x,.57f,z),new Vector3(x+.08f,.64f,z+F(1.8f,3.7f)),.31f,wood);}
        Timber(wreck,"Snapped cargo mast",new Vector3(0,.5f,-1),new Vector3(2,7,-3),.36f,wood);
        for(int i=0;i<10;i++){var p=new Vector2(-246,1970)+J(16);AddDetail("Stranded branch",wood,p,Vector3.one*F(1,2.5f),F(0,360));}
        for(int i=0;i<3;i++)Prop("Assets/Resources/SecondIslandRealism/wooden_crate_01.prefab","Scattered freight crate",new Vector2(-246,1970)+J(11),F(.8f,1.5f),wreck,F(0,360));
        var station=Group("Abandoned cocoa sampling station");station.position=Floor(new Vector2(17,1963));station.rotation=Quaternion.Euler(0,20,0);
        // Oversized weathered sampling ladle: a restrained absurd landmark.
        Timber(station,"Eight metre sampling ladle handle",new Vector3(-2,1,0),new Vector3(3,8.5f,1),.28f,metal);
        const string assayArt="Assets/Game/Art/Coastal Landmarks/";
        var ladle=Group("Tipped assay ladle",station);
        ladle.localPosition=new Vector3(3.4f,8.7f,1);
        ladle.localRotation=Quaternion.Euler(18,20,-38);
        var pan=MeshObject("Hammered ladle",Load<Mesh>(assayArt+"Hammered ladle.asset"),metal,ladle);
        var residue=MeshObject("Set couverture in bowl",Load<Mesh>(assayArt+"Set couverture in bowl.asset"),darkChocolate,ladle);
        pan.localRotation=residue.localRotation=Quaternion.Euler(0,0,65);
        MeshObject("Sampler pin and graduations",Load<Mesh>(assayArt+"Sampler pin and graduations.asset"),Load<Material>(assayArt+"Salt flax.mat"),ladle);
        Timber(station,"Weathered sampler A frame",new Vector3(-3,0,-1),new Vector3(-1,4,0),.3f,wood);Timber(station,"Weathered sampler A frame",new Vector3(1,0,-1),new Vector3(-1,4,0),.3f,wood);
        for(int i=0;i<2;i++)
        {
            var cargo=Prop("Assets/Resources/SecondIslandRealism/wooden_crate_01.prefab","Old cocoa cargo",new Vector2(13+i*2.5f,1965),1.1f,station,20+i*15);
            if(i!=0 || !cargo)continue;
            var deck=cargo.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            var samples=Group("Assay samples on cargo",station);
            samples.SetPositionAndRotation(new Vector3(deck.center.x,deck.max.y,deck.center.z),cargo.rotation);
            MeshObject("Assay Bevelled baked biscuit",Load<Mesh>(assayArt+"Assay Bevelled baked biscuit.asset"),biscuitMat,samples);
            MeshObject("Assay Biscuit chocolate chips",Load<Mesh>(assayArt+"Assay Biscuit chocolate chips.asset"),darkChocolate,samples);
        }
        Prop("Assets/Resources/SecondIslandRealism/wooden_lantern_01.prefab","Sampler lantern",new Vector2(16,1967),1,station,30);
    }
    public static object Run()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ScenePath)throw new Exception("Open Island 2 in Edit mode");
        if(scene.isDirty)throw new Exception("Preserve unsaved scene before rebuilding decoration");
        ground=UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();rng=new System.Random(211611);cells.Clear();treeMats.Clear();trees=rocks=shrubs=tufts=ferns=branches=0;
        protectedPoints=scene.GetRootGameObjects().Where(g=>g.name=="Candy Hunt"||g.name=="Marshmallow Friends - Second Island"||g.name.Contains("Fairy")).SelectMany(g=>g.GetComponentsInChildren<Transform>(true).Where(t=>t==g.transform||t.parent==g.transform)).Select(t=>new Vector2(t.position.x,t.position.z)).ToArray();
        grass=Mat("Dusty sage grasses",new Color(.74f,.79f,.63f),.28f,Load<Material>("Assets/Generated/Island1Realism/Grass - meadow, fescue and sedge.mat"));
        fern=Mat("Copper sage fern",new Color(.81f,.71f,.54f),.35f,Load<Material>("Assets/Generated/Island1Realism/Coppercoil fern.mat"));
        foreach(var m in new[]{grass,fern}){if(m.HasProperty("_ClipBelowSea"))m.SetFloat("_ClipBelowSea",0);if(m.HasProperty("_EcologyDistance"))m.SetVector("_EcologyDistance",new Vector4(150,220,0,0));}
        stone=Mat("Weathered shale",new Color(.31f,.25f,.29f),.22f);wood=Mat("Salt worn freight timber",new Color(.65f,.52f,.38f),.2f,Load<Material>("Assets/Generated/Island1Realism/Salt-bleached wood.mat"));
        metal=Mat("Oxidised brass sampler",new Color(.28f,.22f,.12f),.42f);metal.SetFloat("_Metallic",.68f);
        bank=Mat("Cocoa gravel bank",new Color(.41f,.26f,.18f),.24f);bank.shader=Shader.Find("Dark Brine/Second Island/Ground Transition");bank.SetFloat("_Opacity",.55f);bank.SetTexture("_BaseMap",Load<Texture2D>("Assets/Island/Materials/rocky_terrain_4k/textures/rocky_terrain_diff_4k.jpg"));
        soil=Mat("Weathered grove rootbeds",new Color(.82f,.73f,.65f),.2f);soil.shader=bank.shader;soil.SetFloat("_Opacity",.23f);soil.SetTexture("_BaseMap",bank.GetTexture("_BaseMap"));soil.renderQueue=2974;
        chocolate=Mat("Flowing dark couverture",new Color(.35f,.135f,.055f),.76f);chocolate.shader=Shader.Find("Dark Brine/Second Island/Viscous Chocolate");chocolate.SetColor("_FoldColor",new Color(.58f,.28f,.13f));chocolate.SetFloat("_Smoothness",.78f);chocolate.SetFloat("_FlowSpeed",1.6f);chocolate.SetFloat("_WaveHeight",.075f);
        PrepareDesserts();
        if(!chocolate.shader||ShaderUtil.ShaderHasError(chocolate.shader))throw new Exception("Chocolate shader must compile first");
        var previous=scene.GetRootGameObjects().FirstOrDefault(o=>o.name==RootName);var staged=new GameObject(RootName+" staging");staged.SetActive(false);root=staged.transform;
        try {Environment();RiverPass();WreckAndStation();FlushCells();if(previous)Undo.DestroyObjectImmediate(previous);staged.name=RootName;staged.SetActive(true);Undo.RegisterCreatedObjectUndo(staged,"Island 2 decoration pass");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
            var result=new{trees,rocks,shrubs,tufts,ferns,branches,desserts,riverWidthMultiplier=1.9f,flowMetresPerSecond=1.6f,detailCells=cells.Count,renderers=staged.GetComponentsInChildren<Renderer>(true).Length,colliders=staged.GetComponentsInChildren<Collider>(true).Length,riverControlPoints=River.Select(p=>new{x=p.x,z=p.y,y=Floor(p).y}).ToArray()};
            Directory.CreateDirectory(".codex/island2-decoration");File.WriteAllText(".codex/island2-decoration/build.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;}
        catch {if(staged)UnityEngine.Object.DestroyImmediate(staged);throw;}
    }
    public static object Polish()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        AssetDatabase.ImportAsset("Assets/Shaders/SecondIslandGroundTransition.shader",ImportAssetOptions.ForceSynchronousImport);
        root=GameObject.Find(RootName).transform;ground=UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();rng=new System.Random(61116);
        var river=root.Find("Chocolate River - The Couverture Run");
        river.Find("Slow flowing couverture - walkable shallow spill").GetComponent<MeshFilter>().sharedMesh=Ribbon(false);
        river.Find("Cocoa mineral transition").GetComponent<MeshFilter>().sharedMesh=Ribbon(true);
        river.Find("Source spring pool").GetComponent<MeshFilter>().sharedMesh=Basin(River[0],10,"Spring pool");
        var old=river.Find("Split rock cocoa spring");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var spring=Group("Split rock cocoa spring",river);
        for(int i=0;i<6;i++)Rock(River[0]+new Vector2(Mathf.Cos(i*1.15f),Mathf.Sin(i*1.15f))*8,spring,F(3,6),false);
        bank=Load<Material>(Art+"/Cocoa gravel bank.mat");bank.shader=Shader.Find("Dark Brine/Second Island/Ground Transition");bank.SetColor("_BaseColor",new Color(.41f,.26f,.18f));bank.SetFloat("_Opacity",.55f);EditorUtility.SetDirty(bank);
        soil=Load<Material>(Art+"/Weathered grove rootbeds.mat");soil.shader=bank.shader;soil.SetColor("_BaseColor",new Color(.82f,.73f,.65f));soil.SetFloat("_Opacity",.23f);EditorUtility.SetDirty(soil);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);
        return new{source=Floor(River[0]).ToString(),next=Floor(River[1]).ToString(),featheredGroundTransitions=true};
    }
}
