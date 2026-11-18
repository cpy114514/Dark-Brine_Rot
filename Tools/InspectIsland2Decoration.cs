using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class InspectIsland2Decoration
{
    const string Art="Assets/Game/Art/Island2Decoration";
    public static object Run()
    {
        var root=GameObject.Find("Island 2 - Environment Decoration");if(!root)throw new Exception("Decoration root missing");
        var meshes=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh).Select(f=>f.sharedMesh).Distinct().ToArray();
        var bad=meshes.Where(m=>m.vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z))).Select(m=>m.name).ToArray();
        var mats=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var result=new{meshes=meshes.Length,vertices=meshes.Sum(m=>(long)m.vertexCount),triangles=meshes.Sum(m=>(long)m.triangles.Length/3),badMeshes=bad,unsupportedMaterials=mats.Where(m=>!m||!m.shader.isSupported||ShaderUtil.ShaderHasError(m.shader)).Select(m=>m?m.name:"missing").ToArray(),colliders=root.GetComponentsInChildren<Collider>(true).Length,
            lods=root.GetComponentsInChildren<LODGroup>(true).Length,materialCount=mats.Length};
        File.WriteAllText(".codex/island2-decoration/mesh-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object OptimizeScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");var root=GameObject.Find("Island 2 - Environment Decoration");
        var groundcover=root.transform.Find("Groundcover - spatially combined LOD cells");
        foreach(var l in groundcover.GetComponentsInChildren<LODGroup>()){var lods=l.GetLODs();lods[0].screenRelativeTransitionHeight=.5f;lods[1].screenRelativeTransitionHeight=.17f;l.SetLODs(lods);l.size=40;EditorUtility.SetDirty(l);}
        int shrubs=0,far=0;foreach(var l in root.GetComponentsInChildren<LODGroup>()){
            if(l.transform.IsChildOf(groundcover))continue;var ls=l.GetLODs();
            if(l.name=="Wind shaped scrub"){foreach(var r in l.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;shrubs++;}}
            if(ls.Length>=2){ls[ls.Length-1].screenRelativeTransitionHeight=.025f;foreach(var r in ls[ls.Length-1].renderers){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;far++;}l.SetLODs(ls);EditorUtility.SetDirty(l);}}
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);return new{groundcoverCells=groundcover.childCount,shrubs,farTreeRenderers=far};
    }
    public static object PackMeshes()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var root=GameObject.Find("Island 2 - Environment Decoration");
        var paths=AssetDatabase.FindAssets("t:Mesh",new[]{Art}).Select(AssetDatabase.GUIDToAssetPath).Distinct().Where(p=>AssetDatabase.LoadMainAssetAtPath(p) is Mesh).OrderBy(p=>p).ToArray();
        if(paths.Length==0)return "Already packed";
        var map=new System.Collections.Generic.Dictionary<Mesh,Mesh>();var libraries=new System.Collections.Generic.List<string>();
        int firstLibrary=AssetDatabase.FindAssets("t:Island2DecorationMeshLibrary",new[]{Art}).Length;
        long before=paths.Sum(p=>new FileInfo(p).Length);AssetDatabase.StartAssetEditing();
        try{Island2DecorationMeshLibrary library=null;for(int i=0;i<paths.Length;i++){
            if(i%350==0){string path=Art+"/Decoration mesh library "+(firstLibrary+i/350)+".asset";if(File.Exists(path))throw new Exception("Preserve existing mesh library");library=ScriptableObject.CreateInstance<Island2DecorationMeshLibrary>();AssetDatabase.CreateAsset(library,path);libraries.Add(path);}
            var m=AssetDatabase.LoadAssetAtPath<Mesh>(paths[i]);var copy=UnityEngine.Object.Instantiate(m);copy.name=m.name;AssetDatabase.AddObjectToAsset(copy,library);map.Add(m,copy);EditorUtility.SetDirty(library);
        }}finally{AssetDatabase.StopAssetEditing();}
        AssetDatabase.SaveAssets();
        foreach(var p in libraries){AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);if(AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().Count()==0)throw new Exception("Mesh archive import failed");}
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh&&map.TryGetValue(f.sharedMesh,out var replacement))f.sharedMesh=replacement;
        foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))if(c.sharedMesh&&map.TryGetValue(c.sharedMesh,out var replacement))c.sharedMesh=replacement;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene))throw new Exception("Packed mesh scene save failed");
        AssetDatabase.StartAssetEditing();try{foreach(var p in paths)if(!AssetDatabase.DeleteAsset(p))throw new Exception("Obsolete generated mesh removal failed: "+p);}finally{AssetDatabase.StopAssetEditing();}
        var result=new{meshes=paths.Length,libraries=libraries.Count,bytesBefore=before,bytesAfter=libraries.Sum(p=>new FileInfo(p).Length)};File.WriteAllText(".codex/island2-decoration/mesh-packing.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object PruneUnusedArchiveMeshes()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");var root=GameObject.Find("Island 2 - Environment Decoration");
        var used=new System.Collections.Generic.HashSet<Mesh>(root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh));
        foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))used.Add(c.sharedMesh);
        var libraries=AssetDatabase.FindAssets("t:Island2DecorationMeshLibrary",new[]{Art}).Select(AssetDatabase.GUIDToAssetPath).ToArray();int removed=0;
        foreach(var p in libraries){foreach(var m in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().ToArray())if(!used.Contains(m)){UnityEngine.Object.DestroyImmediate(m,true);removed++;}EditorUtility.SetDirty(AssetDatabase.LoadMainAssetAtPath(p));}
        AssetDatabase.SaveAssets();return new{removed,bytes=libraries.Sum(p=>new FileInfo(p).Length)};
    }
    public static object RouteClearance()
    {
        var root=GameObject.Find("Island 2 - Environment Decoration");var ground=UnityEngine.Object.FindFirstObjectByType<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        var routes=new[]{new[]{new Vector2(-147,1608),new Vector2(-130,1650),new Vector2(-94,1676),new Vector2(-79,1715),new Vector2(-105,1755)},new[]{new Vector2(-105,1755),new Vector2(-145,1800),new Vector2(-185,1850),new Vector2(-155,1940),new Vector2(-147,2198)},new[]{new Vector2(-79,1715),new Vector2(-45,1788),new Vector2(-25,1840),new Vector2(30,1907),new Vector2(98,1945)},new[]{new Vector2(98,1945),new Vector2(170,1890),new Vector2(410,1820),new Vector2(730,1725),new Vector2(1010,1610)},new[]{new Vector2(-155,1940),new Vector2(-255,2090),new Vector2(-448,2328)},new[]{new Vector2(-147,1608),new Vector2(-65,1410),new Vector2(65,1110),new Vector2(180,830)}};
        Physics.SyncTransforms();int probes=0;var blocked=new System.Collections.Generic.List<string>();
        foreach(var route in routes)for(int i=1;i<route.Length;i++){int count=Mathf.CeilToInt(Vector2.Distance(route[i-1],route[i])/3);for(int s=0;s<=count;s++){var p=Vector2.Lerp(route[i-1],route[i],(float)s/count);if(!ground.Raycast(new Ray(new Vector3(p.x,ground.bounds.max.y+5,p.y),Vector3.down),out var h,ground.bounds.size.y+15))continue;
            probes++;foreach(var c in Physics.OverlapCapsule(h.point+Vector3.up*.65f,h.point+Vector3.up*2.0f,.55f,~0,QueryTriggerInteraction.Ignore))if(c.transform.IsChildOf(root.transform))blocked.Add(c.name+" at "+p);}}
        var result=new{routes=routes.Length,capsuleProbes=probes,newDecorationObstructions=blocked.Distinct().ToArray()};File.WriteAllText(".codex/island2-decoration/route-clearance.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
