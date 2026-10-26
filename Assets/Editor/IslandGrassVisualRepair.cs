using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

internal static class IslandGrassVisualRepair
{
    const string Folder="Assets/Generated/NormalIslandGrass20261025";
    const string Work="ProjectRepairBackups/GrassVisualRepair-20261025";
    internal static void Densify()
    {
        const string rootName="Denser Normal Grass Cover";
        const float spacing=4.4f, separation=2.2f;
        var scene=GrassPlacementTools.GetScene();
        if(scene.GetRootGameObjects().Any(g=>g.name==rootName)) throw new Exception("Density pass already applied");
        var near=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/VerifiedGrassNear.asset");
        var far=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/VerifiedGrassFar.asset");
        var materials=new[]{AssetDatabase.LoadAssetAtPath<Material>(Folder+"/VerifiedGrass_0.mat"),AssetDatabase.LoadAssetAtPath<Material>(Folder+"/VerifiedGrass_1.mat")};
        if(!near || !far || materials.Any(m=>!m)) throw new Exception("Verified grass assets missing");
        var island=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProceduralIsland>(false)).First(i=>i.isActiveAndEnabled);
        var collider=island.GetComponent<MeshCollider>(); var bounds=collider.bounds;
        Directory.CreateDirectory(Work);
        if(!File.Exists(Work+"/Environment.before-denser.unity")) File.Copy(scene.path,Work+"/Environment.before-denser.unity");
        var occupied=new Dictionary<Vector2Int,List<Vector2>>();
        Vector2Int Key(Vector3 p)=>new Vector2Int(Mathf.FloorToInt(p.x/separation),Mathf.FloorToInt(p.z/separation));
        void Register(Vector3 p) { var key=Key(p); if(!occupied.TryGetValue(key,out var list)) occupied[key]=list=new List<Vector2>(); list.Add(new Vector2(p.x,p.z)); }
        int before=0;
        foreach(var root in scene.GetRootGameObjects().Where(g=>g.activeInHierarchy))
        {
            if(root.name=="Normal Island Grass Cover") { foreach(Transform patch in root.transform) {Register(patch.position);before++;} }
            else if(root.name.StartsWith("rostlinka_")) { var b=GrassPlacementTools.GetBounds(root.transform); Register(b.center);before++; }
        }
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Make normal island grass denser");
        var cover=new GameObject(rootName); SceneManager.MoveGameObjectToScene(cover,scene); Undo.RegisterCreatedObjectUndo(cover,"Add dense normal grass");
        var support=new[]{new Vector3(-2.5f,0,-2.5f),new Vector3(2.5f,0,-2.5f),new Vector3(-2.5f,0,2.5f),new Vector3(2.5f,0,2.5f),Vector3.zero};
        int count=0,tooClose=0,water=0,samples=0; float highestSupport=float.MinValue;
        Physics.SyncTransforms();
        try
        {
            for(float z=bounds.min.z+1.7f;z<=bounds.max.z;z+=spacing)
            {
                EditorUtility.DisplayProgressBar("Densifying normal grass","Filling gaps with the verified grass clumps",(z-bounds.min.z)/bounds.size.z);
                for(float x=bounds.min.x+2.3f;x<=bounds.max.x;x+=spacing)
                {
                    var candidate=new Vector3(x,0,z); var cell=Key(candidate); bool close=false;
                    for(int dx=-1;dx<=1&&!close;dx++) for(int dz=-1;dz<=1&&!close;dz++)
                        if(occupied.TryGetValue(cell+new Vector2Int(dx,dz),out var list)) foreach(var p in list) if((p-new Vector2(x,z)).sqrMagnitude<separation*separation) {close=true;break;}
                    if(close) {tooClose++;continue;}
                    if(!collider.Raycast(new Ray(new Vector3(x,300,z),Vector3.down),out var hit,500) || hit.point.y<1.1f || hit.normal.y<.7f) {water++;continue;}
                    float yaw=(Mathf.Sin(x*8.571f+z*17.743f)*.5f+.5f)*360;
                    var q=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,yaw,0);
                    var position=hit.point; float maximum=float.MinValue; int hits=0;
                    foreach(var v in support) {var p=position+q*v;if(collider.Raycast(new Ray(new Vector3(p.x,300,p.z),Vector3.down),out var h,500)) {maximum=Mathf.Max(maximum,p.y-h.point.y);hits++;}}
                    if(hits!=support.Length) continue;
                    position.y-=maximum+.025f;
                    var patch=new GameObject("Grass infill "+count,typeof(LODGroup)); patch.transform.SetParent(cover.transform,false);patch.transform.SetPositionAndRotation(position,q);
                    MeshRenderer Make(string label,Mesh mesh)
                    {
                        var go=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(patch.transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                        var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=materials;r.shadowCastingMode=ShadowCastingMode.Off;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;return r;
                    }
                    var a=Make("Original grass",near);var b=Make("Distant grass",far);
                    var lod=patch.GetComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.075f,new Renderer[]{a}),new LOD(.001f,new Renderer[]{b})});lod.RecalculateBounds();
                    Register(position);
                    if(count%23==0) foreach(var v in support) {var p=position+q*v;if(collider.Raycast(new Ray(new Vector3(p.x,300,p.z),Vector3.down),out var h,500)){highestSupport=Mathf.Max(highestSupport,p.y-h.point.y);samples++;}}
                    count++;
                }
            }
            Undo.CollapseUndoOperations(undo);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save Environment");
            File.WriteAllText(Work+"/denser-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{previousPatches=before,addedPatches=count,totalPatches=before+count,densityMultiplier=(float)(before+count)/Mathf.Max(before,1),spacing,minimumCenterSeparation=separation,skippedClose=tooClose,skippedWater=water,samples,highestSupport,sceneSaved=!scene.isDirty},Newtonsoft.Json.Formatting.Indented));
            Selection.activeGameObject=null;SceneView.RepaintAll();
        }
        finally {EditorUtility.ClearProgressBar();}
    }
    static (Mesh,Mesh,Material[],string) CopyVerifiedGrass(UnityEngine.SceneManagement.Scene scene,MeshCollider ground)
    {
        var selected=Selection.activeGameObject;
        var template=selected?PrefabUtility.GetOutermostPrefabInstanceRoot(selected):null;
        if(!template || !template.name.StartsWith("rostlinka_")) template=scene.GetRootGameObjects().First(g=>g.name=="rostlinka_07c_ske (5300)");
        var parts=template.GetComponentsInChildren<MeshFilter>(false).Where(m=>m.sharedMesh && m.GetComponent<MeshRenderer>() && m.GetComponent<MeshRenderer>().enabled).ToArray();
        var bounds=parts[0].GetComponent<MeshRenderer>().bounds;
        foreach(var part in parts) bounds.Encapsulate(part.GetComponent<MeshRenderer>().bounds);
        var anchor=bounds.center;
        if(!ground.Raycast(new Ray(new Vector3(anchor.x,300,anchor.z),Vector3.down),out var h,500)) throw new Exception("Template ground missing");
        anchor.y=h.point.y;
        var level=Matrix4x4.Rotate(Quaternion.FromToRotation(template.transform.up,Vector3.up))*Matrix4x4.Translate(-anchor);
        var originals=parts.SelectMany(m=>m.GetComponent<MeshRenderer>().sharedMaterials).Distinct().ToArray();
        var mats=new List<Material>();
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        for(int j=0;j<originals.Length;j++)
        {
            string path=Folder+"/VerifiedGrass_"+j+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) {mat=new Material(originals[j]){enableInstancing=true}; mat.SetFloat("_WindBend",0); mat.SetFloat("_PlayerPushStrength",0); AssetDatabase.CreateAsset(mat,path);}
            mats.Add(mat);
        }
        Mesh Bake(bool far)
        {
            string path=Folder+(far?"/VerifiedGrassFar.asset":"/VerifiedGrassNear.asset");
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(mesh) return mesh;
            var groups=new List<CombineInstance>(); var temporary=new List<Mesh>();
            foreach(var material in originals)
            {
                var combine=new List<CombineInstance>();
                foreach(var part in parts)
                {
                    var materials=part.GetComponent<MeshRenderer>().sharedMaterials;
                    var source=part.sharedMesh;
                    if(far && source.name=="Forest001") source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/FoliagePerformance/0_Forest001_OptimizedFar.asset");
                    for(int sub=0;sub<materials.Length && sub<source.subMeshCount;sub++) if(materials[sub]==material) combine.Add(new CombineInstance{mesh=source,subMeshIndex=sub,transform=level*part.transform.localToWorldMatrix});
                }
                var merged=new Mesh{indexFormat=IndexFormat.UInt32}; merged.CombineMeshes(combine.ToArray(),true,true); temporary.Add(merged); groups.Add(new CombineInstance{mesh=merged,transform=Matrix4x4.identity});
            }
            mesh=new Mesh{name=far?"Verified grass distant":"Verified grass",indexFormat=IndexFormat.UInt32}; mesh.CombineMeshes(groups.ToArray(),false,false); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path);
            foreach(var temp in temporary) UnityEngine.Object.DestroyImmediate(temp);
            return mesh;
        }
        return (Bake(false),Bake(true),mats.ToArray(),template.name);
    }
    static Mesh Canonical(Mesh source,string name,Vector3 center,float baseY)
    {
        string path=Folder+"/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(existing) return existing;
        var q=Quaternion.Euler(-90,0,0);
        var p=source.vertices.Select(v=>Vector3.Scale(q*v,new Vector3(.001f,.00065f,.001f))-new Vector3(center.x,baseY,center.z)).ToArray();
        var n=source.normals.Select(v=>Vector3.Scale(q*v,new Vector3(1000,1538.4615f,1000)).normalized).ToArray();
        var mesh=new Mesh {name=name,indexFormat=source.indexFormat}; mesh.vertices=p; mesh.normals=n; mesh.uv=source.uv;
        mesh.subMeshCount=source.subMeshCount;
        for(int i=0;i<mesh.subMeshCount;i++) mesh.SetTriangles(source.GetTriangles(i),i);
        mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
    }
    internal static void Run(bool preview)
    {
        var scene=GrassPlacementTools.GetScene();
        Directory.CreateDirectory(Work);
        if(!File.Exists(Work+"/Environment.before.unity")) File.Copy(scene.path,Work+"/Environment.before.unity");
        var island=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProceduralIsland>(false)).First(i=>i.isActiveAndEnabled);
        var collider=island.GetComponent<MeshCollider>(); var bounds=collider.bounds;
        var (near,far,materials,templateName)=CopyVerifiedGrass(scene,collider);
        var basePoints=new[]{new Vector3(-2.5f,0,-2.5f),new Vector3(2.5f,0,-2.5f),new Vector3(-2.5f,0,2.5f),new Vector3(2.5f,0,2.5f),Vector3.zero};
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Replace dotted grass with normal grass");
        foreach(var g in scene.GetRootGameObjects().Where(g=>g.name=="Whole Island Grass Cover" || g.name=="Matched Density Grass Cover")) { Undo.RecordObject(g,"Hide dotted grass"); g.SetActive(false); }
        var previous=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Normal Grass Preview");
        if(previous) Undo.DestroyObjectImmediate(previous);
        if(scene.GetRootGameObjects().Any(g=>g.name=="Normal Island Grass Cover")) throw new Exception("Normal cover already exists");
        var cover=new GameObject(preview?"Normal Grass Preview":"Normal Island Grass Cover"); SceneManager.MoveGameObjectToScene(cover,scene); Undo.RegisterCreatedObjectUndo(cover,"Add normal grass");
        var oldCoverage=new HashSet<Vector2Int>();
        foreach(var mf in scene.GetRootGameObjects().Where(g=>g!=cover).SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(false)).Where(m=>m.sharedMesh && m.sharedMesh.name=="Forest001"))
        {
            var r=mf.GetComponent<MeshRenderer>(); if(!r || !r.enabled) continue;
            var b=r.bounds;
            if(b.size.x>25 || b.size.z>25) continue;
            for(int x=Mathf.CeilToInt(b.min.x);x<=Mathf.FloorToInt(b.max.x);x++) for(int z=Mathf.CeilToInt(b.min.z);z<=Mathf.FloorToInt(b.max.z);z++) oldCoverage.Add(new Vector2Int(x,z));
        }
        var view=SceneView.lastActiveSceneView;
        var focus=view?view.pivot:bounds.center;
        if(!collider.Raycast(new Ray(new Vector3(focus.x,300,focus.z),Vector3.down),out var focusHit,500)) focusHit=new RaycastHit {point=island.transform.position};
        if(preview) focus=focusHit.point;
        float minX=preview?focus.x-24:bounds.min.x,maxX=preview?focus.x+24:bounds.max.x;
        float minZ=preview?focus.z-24:bounds.min.z,maxZ=preview?focus.z+24:bounds.max.z;
        const float spacing=6.55f;
        int count=0,skippedExisting=0; float highestRoot=float.MinValue; int samples=0;
        Physics.SyncTransforms();
        try
        {
            for(float z=minZ;z<=maxZ;z+=spacing)
            {
                EditorUtility.DisplayProgressBar("Planting normal grass","Using original grass meshes and cutout material",(z-minZ)/(maxZ-minZ));
                for(float x=minX;x<=maxX;x+=spacing)
                {
                    if(!collider.Raycast(new Ray(new Vector3(x,300,z),Vector3.down),out var hit,500) || hit.point.y<1.1f || hit.normal.y<.7f) continue;
                    int occupied=0;
                    for(int dx=-1;dx<=1;dx++) for(int dz=-1;dz<=1;dz++) if(oldCoverage.Contains(new Vector2Int(Mathf.RoundToInt(x+dx*2.2f),Mathf.RoundToInt(z+dz*2.2f)))) occupied++;
                    if(!preview && occupied>=7) {skippedExisting++;continue;}
                    float yaw=(Mathf.Sin(x*12.47f+z*7.37f)*.5f+.5f)*360;
                    var q=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,yaw,0);
                    var position=hit.point; float maximum=float.MinValue; int hits=0;
                    foreach(var v in basePoints)
                    {
                        var p=position+q*v;
                        if(!collider.Raycast(new Ray(new Vector3(p.x,300,p.z),Vector3.down),out var h,500)) continue;
                        maximum=Mathf.Max(maximum,p.y-h.point.y); hits++;
                    }
                    if(hits<basePoints.Length*.85f) continue;
                    position.y-=maximum+.025f;
                    var go=new GameObject("Grass patch "+count,typeof(LODGroup)); go.transform.SetParent(cover.transform,false); go.transform.SetPositionAndRotation(position,q);
                    MeshRenderer Make(string label,Mesh mesh)
                    {
                        var child=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer)); child.transform.SetParent(go.transform,false); child.GetComponent<MeshFilter>().sharedMesh=mesh;
                        var renderer=child.GetComponent<MeshRenderer>(); renderer.sharedMaterials=materials; renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.lightProbeUsage=LightProbeUsage.Off; renderer.reflectionProbeUsage=ReflectionProbeUsage.Off; return renderer;
                    }
                    var a=Make("Original grass",near); var b=Make("Distant grass",far);
                    var lod=go.GetComponent<LODGroup>(); lod.SetLODs(new[]{new LOD(.075f,new Renderer[]{a}),new LOD(.001f,new Renderer[]{b})}); lod.RecalculateBounds();
                    if(count%17==0) foreach(var v in basePoints) {var p=position+q*v; if(collider.Raycast(new Ray(new Vector3(p.x,300,p.z),Vector3.down),out var h,500)) {highestRoot=Mathf.Max(highestRoot,p.y-h.point.y);samples++;}}
                    count++;
                }
            }
            Undo.CollapseUndoOperations(undo);
            if(preview && view) {view.orthographic=false; view.LookAt(focus,Quaternion.Euler(48,25,0),22,false,true);}
            Selection.activeGameObject=null; SceneView.RepaintAll();
            if(!preview) {EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Environment save failed");}
            File.WriteAllText(Work+(preview?"/preview.json":"/result.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {preview,patches=count,skippedExisting,spacing,templateName,nearVertices=near.vertexCount,farVertices=far.vertexCount,materials=materials.Select(AssetDatabase.GetAssetPath).ToArray(),samples,highestRoot,sceneSaved=!scene.isDirty},Newtonsoft.Json.Formatting.Indented));
        }
        finally {EditorUtility.ClearProgressBar();}
    }
}
