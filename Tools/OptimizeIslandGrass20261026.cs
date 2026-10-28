using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class OptimizeIslandGrass20261026
{
    const string Folder="Assets/Generated/NormalIslandGrass20261025";
    const string Work="ProjectRepairBackups/GrassPerformance-20261026";
    static Mesh ThinCards(Mesh source)
    {
        string path=Folder+"/VerifiedGrassLow.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(existing) return existing;
        var vertices=source.vertices; var normals=source.normals; var uv=source.uv;
        var parents=Enumerable.Range(0,vertices.Length).ToArray();
        Func<int,int> find=null;find=i=>parents[i]==i?i:parents[i]=find(parents[i]);
        var weld=new Dictionary<Vector3,int>();
        for(int i=0;i<vertices.Length;i++) {if(weld.TryGetValue(vertices[i],out int j)) parents[find(i)]=find(j);else weld[vertices[i]]=i;}
        var all=Enumerable.Range(0,source.subMeshCount).Select(source.GetTriangles).ToArray();
        foreach(var tris in all) for(int i=0;i<tris.Length;i+=3) {int root=find(tris[i]);parents[find(tris[i+1])]=root;parents[find(tris[i+2])]=root;}
        var p=new List<Vector3>();var n=new List<Vector3>();var tex=new List<Vector2>();var map=new Dictionary<int,int>();var output=new List<int[]>();
        int Map(int old) {if(map.TryGetValue(old,out int index))return index;index=p.Count;map[old]=index;p.Add(vertices[old]);n.Add(normals[old]);tex.Add(uv[old]);return index;}
        foreach(var tris in all)
        {
            var groups=new Dictionary<int,List<int>>();
            for(int i=0;i<tris.Length;i+=3) {int root=find(tris[i]);if(!groups.TryGetValue(root,out var list))groups[root]=list=new List<int>();list.Add(tris[i]);list.Add(tris[i+1]);list.Add(tris[i+2]);}
            var kept=new List<int>();int index=0;
            foreach(var group in groups.OrderBy(g=>g.Key)) if(index++%4==0) foreach(int v in group.Value)kept.Add(Map(v));
            if(kept.Count==0)throw new Exception("Empty grass material after thinning");output.Add(kept.ToArray());
        }
        var mesh=new Mesh{name="Verified grass low detail",indexFormat=IndexFormat.UInt32};mesh.SetVertices(p);mesh.SetNormals(n);mesh.SetUVs(0,tex);mesh.subMeshCount=output.Count;
        for(int i=0;i<output.Count;i++)mesh.SetTriangles(output[i],i,false);mesh.bounds=source.bounds;AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static long Triangles(Mesh mesh) {long count=0;for(int i=0;i<mesh.subMeshCount;i++)count+=(long)mesh.GetIndexCount(i)/3;return count;}
    static long Estimate(IEnumerable<LODGroup> groups,Vector3 camera,float fov)
    {
        long count=0;
        foreach(var group in groups)
        {
            if(!group.gameObject.activeInHierarchy)continue;
            float size=group.size*Mathf.Max(group.transform.lossyScale.x,Mathf.Max(group.transform.lossyScale.y,group.transform.lossyScale.z));
            float distance=Vector3.Distance(camera,group.transform.TransformPoint(group.localReferencePoint));
            float relative=size*QualitySettings.lodBias/(2*Mathf.Max(distance,.01f)*Mathf.Tan(fov*Mathf.Deg2Rad*.5f));
            foreach(var lod in group.GetLODs()) if(relative>=lod.screenRelativeTransitionHeight) {foreach(var r in lod.renderers) {if(!r)continue;var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh)count+=Triangles(mesh);}break;}
        }
        return count;
    }
    public static object Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");if(!scene.isLoaded)throw new Exception("Environment not loaded");
        Directory.CreateDirectory(Work);if(!File.Exists(Work+"/Environment.before.unity"))File.Copy(scene.path,Work+"/Environment.before.unity");
        var layers=scene.GetRootGameObjects().Where(g=>g.name=="Normal Island Grass Cover"||g.name=="Denser Normal Grass Cover").ToArray();
        if(layers.Length!=2)throw new Exception("Expected both normal grass layers");
        if(layers.Any(g=>g.GetComponentsInChildren<MeshFilter>(true).Any(m=>m.sharedMesh && m.sharedMesh.name=="Verified grass low detail")))throw new Exception("Already optimized");
        var groups=layers.SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true)).ToArray();
        var near=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/VerifiedGrassNear.asset");var mid=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/VerifiedGrassFar.asset");
        var materials=new[]{AssetDatabase.LoadAssetAtPath<Material>(Folder+"/VerifiedGrass_0.mat"),AssetDatabase.LoadAssetAtPath<Material>(Folder+"/VerifiedGrass_1.mat")};
        if(!near||!mid||materials.Any(m=>!m))throw new Exception("Verified grass assets missing");
        var camera=Camera.main;Vector3 pos=camera?camera.transform.position:new Vector3(0,41,357);float fov=camera?camera.fieldOfView:60;
        int before=groups.Count(g=>g.gameObject.activeInHierarchy);long baseline=Estimate(groups,pos,fov);
        var low=ThinCards(mid);
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Optimize island grass");
        int thinned=0;
        try
        {
            foreach(var layer in layers)
            {
                bool dense=layer.name=="Denser Normal Grass Cover";int index=0;
                foreach(Transform patch in layer.transform)
                {
                    if(dense && (++index%2==0) && patch.gameObject.activeSelf) {Undo.RecordObject(patch.gameObject,"Reduce excessive infill");patch.gameObject.SetActive(false);thinned++;}
                    var group=patch.GetComponent<LODGroup>();if(!group)continue;
                    var existing=group.GetLODs();if(existing.Length!=2)throw new Exception("Unexpected original LOD setup");
                    var child=new GameObject("Lightweight distant grass",typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(patch,false);Undo.RegisterCreatedObjectUndo(child,"Add distant grass detail");
                    child.GetComponent<MeshFilter>().sharedMesh=low;var r=child.GetComponent<MeshRenderer>();r.sharedMaterials=materials;r.shadowCastingMode=ShadowCastingMode.Off;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;
                    Undo.RecordObject(group,"Set bounded grass LOD");
                    // Convert desired ranges to screen thresholds, accounting for
                    // the project's LOD bias rather than changing global quality.
                    float size=group.size*Mathf.Max(patch.lossyScale.x,Mathf.Max(patch.lossyScale.y,patch.lossyScale.z));
                    float factor=size*QualitySettings.lodBias/(2*Mathf.Tan(30*Mathf.Deg2Rad));
                    float a=Mathf.Clamp(factor/25f,.08f,.95f),b=Mathf.Clamp(factor/65f,.025f,a-.01f),c=Mathf.Clamp(factor/160f,.005f,b-.01f);
                    group.SetLODs(new[]{new LOD(a,existing[0].renderers),new LOD(b,existing[1].renderers),new LOD(c,new Renderer[]{r})});group.fadeMode=LODFadeMode.None;
                    if(index%500==0)EditorUtility.DisplayProgressBar("Optimizing grass",layer.name,(float)index/Mathf.Max(layer.transform.childCount,1));
                }
            }
            Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
            long optimized=Estimate(groups,pos,fov);
            var result=new {activeGroupsBefore=before,activeGroupsAfter=groups.Count(g=>g.gameObject.activeInHierarchy),removedExcessInfill=thinned,nearVertices=near.vertexCount,midVertices=mid.vertexCount,distantVertices=low.vertexCount,nearTriangles=Triangles(near),midTriangles=Triangles(mid),distantTriangles=Triangles(low),estimatedGrassTrianglesBefore=baseline,estimatedGrassTrianglesAfter=optimized,estimateIncludesFrustum=false,nearRangeMetres=25,midRangeMetres=65,farRangeMetres=160,legacyRuntimeCullMetres=180,sceneSaved=!scene.isDirty};
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));SceneView.RepaintAll();return result;
        }
        finally{EditorUtility.ClearProgressBar();}
    }
}
