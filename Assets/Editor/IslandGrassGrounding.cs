using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

internal static class IslandGrassGrounding
{
    const string Work = "ProjectRepairBackups/GrassGrounding-20261025";
    // Use the bottom of every connected grass card, rather than renderer bounds,
    // whose minimum moves sideways when a patch is tilted.
    internal static Vector3[] Roots(Mesh mesh)
    {
        var p=mesh.vertices; var parent=Enumerable.Range(0,p.Length).ToArray();
        Func<int,int> find=null; find=i=>parent[i]==i?i:parent[i]=find(parent[i]);
        var weld=new Dictionary<Vector3,int>();
        for(int i=0;i<p.Length;i++) { if(weld.TryGetValue(p[i],out int j)) parent[find(i)]=find(j); else weld[p[i]]=i; }
        var triangles=mesh.triangles;
        for(int i=0;i<triangles.Length;i+=3) { int a=find(triangles[i]); parent[find(triangles[i+1])]=a; parent[find(triangles[i+2])]=a; }
        var groups=new Dictionary<int,List<Vector3>>();
        for(int i=0;i<p.Length;i++) { int a=find(i); if(!groups.TryGetValue(a,out var list)) groups[a]=list=new List<Vector3>(); list.Add(p[i]); }
        var roots=groups.Values.Select(g=> { float low=g.Min(v=>v.z); var bottom=g.Where(v=>v.z<low+.02f).ToArray(); return bottom.Aggregate(Vector3.zero,(a,v)=>a+v)/bottom.Length; }).OrderBy(v=>v.x).ThenBy(v=>v.y).ToArray();
        if(roots.Length>64) roots=Enumerable.Range(0,64).Select(i=>roots[i*(roots.Length-1)/63]).ToArray();
        return roots;
    }
    internal static void Run(bool apply)
    {
        Directory.CreateDirectory(Work);
        var scene=GrassPlacementTools.GetScene();
        var islands=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProceduralIsland>(false)).Where(i=>i.isActiveAndEnabled).ToArray();
        var colliders=islands.Select(i=>i.GetComponent<MeshCollider>()).ToArray();
        Physics.SyncTransforms();
        var patches=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(false)).Where(m=>m.sharedMesh && m.sharedMesh.name=="Forest001").ToArray();
        var cache=new Dictionary<Mesh,Vector3[]>(); var report=new List<object>();
        if(apply && !File.Exists(Work+"/Environment.before.unity")) File.Copy(scene.path,Work+"/Environment.before.unity");
        int adjusted=0, skipped=0, checkedRoots=0, floatingRoots=0; float maxAfter=float.MinValue, minAfter=float.MaxValue; var backups=new List<object>();
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Ground island grass patches");
        try
        {
            foreach(var mf in patches)
            {
                if(adjusted%100==0) EditorUtility.DisplayProgressBar("Grounding grass",mf.name,(float)adjusted/patches.Length);
                if(!cache.TryGetValue(mf.sharedMesh,out var roots)) cache[mf.sharedMesh]=roots=Roots(mf.sharedMesh);
                var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(mf.gameObject);
                if(!instance || !PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance).Replace('\\','/').Contains("/Grass/")) { skipped++; continue; }
                var t=instance.transform;
                Vector3 center=roots.Select(mf.transform.TransformPoint).Aggregate(Vector3.zero,(a,v)=>a+v)/roots.Length;
                if(!GrassPlacementTools.Surface(center,colliders,out var hit)) { skipped++; continue; }
                var gaps=new List<float>();
                foreach(var v in roots) { var p=mf.transform.TransformPoint(v); if(GrassPlacementTools.Surface(p,colliders,out var h)) gaps.Add(p.y-h.point.y); }
                if(gaps.Count==0) { skipped++; continue; }
                gaps.Sort(); float before=gaps[gaps.Count*95/100];
                float tilt=Vector3.Angle(t.up,hit.normal);
                if(apply)
                {
                    backups.Add(new { id=GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(), position=new[]{t.position.x,t.position.y,t.position.z}, rotation=new[]{t.rotation.x,t.rotation.y,t.rotation.z,t.rotation.w} });
                    Undo.RecordObject(t,"Ground grass");
                    var oldCenter=center;
                    t.rotation=Quaternion.FromToRotation(t.up,hit.normal)*t.rotation;
                    center=roots.Select(mf.transform.TransformPoint).Aggregate(Vector3.zero,(a,v)=>a+v)/roots.Length;
                    t.position+=new Vector3(oldCenter.x-center.x,0,oldCenter.z-center.z);
                    gaps.Clear();
                    foreach(var v in roots) { var p=mf.transform.TransformPoint(v); if(GrassPlacementTools.Surface(p,colliders,out var h)) gaps.Add(p.y-h.point.y); }
                    // Bury the highest sampled roots slightly so no side of a clump hovers.
                    t.position+=Vector3.down*(gaps.Max()+.025f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    foreach(var v in roots)
                    {
                        var p=mf.transform.TransformPoint(v);
                        if(!GrassPlacementTools.Surface(p,colliders,out var h)) continue;
                        float after=p.y-h.point.y; maxAfter=Mathf.Max(maxAfter,after); minAfter=Mathf.Min(minAfter,after);
                        checkedRoots++; if(after>.035f) floatingRoots++;
                    }
                }
                report.Add(new { name=t.name, beforeRootGap95=before, angleCorrection=tilt }); adjusted++;
            }
            if(apply)
            {
                File.WriteAllText(Work+"/transforms.before.json",Newtonsoft.Json.JsonConvert.SerializeObject(backups));
                EditorSceneManager.MarkSceneDirty(scene); if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save Environment");
                Undo.CollapseUndoOperations(undo);
            }
            var values=report.Select(o=> (float)o.GetType().GetProperty("beforeRootGap95").GetValue(o)).OrderBy(v=>v).ToArray();
            File.WriteAllText(Work+(apply?"/result.json":"/probe.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {apply,patches=patches.Length,adjusted,skipped,beforeMedianRootGap=values.Length>0?values[values.Length/2]:0,maximumBeforeRootGap=values.Length>0?values.Last():0,maxAfter,minAfter,checkedRoots,floatingRoots,sceneSaved=!scene.isDirty,examples=report.Take(12)},Newtonsoft.Json.Formatting.Indented));
            SceneView.RepaintAll();
        }
        finally { EditorUtility.ClearProgressBar(); }
    }
    internal static void Details()
    {
        var scene=GrassPlacementTools.GetScene();
        var colliders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProceduralIsland>(false)).Where(i=>i.isActiveAndEnabled).Select(i=>i.GetComponent<MeshCollider>()).ToArray();
        var parts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(false)).Where(m=>m.sharedMesh && new[]{"r12_a","r12_b","r12_c","rostlinka_07c","rostlinka_7c_scater","ground_close_04"}.Contains(m.sharedMesh.name)).ToArray();
        var cache=new Dictionary<Mesh,Vector3[]>();
        int adjusted=0,hiddenGround=0,samples=0,floating=0; float worst=float.MinValue;
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Ground grass details");
        try
        {
            foreach(var mf in parts)
            {
                var root=PrefabUtility.GetOutermostPrefabInstanceRoot(mf.gameObject);
                if(!root || !PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root).Replace('\\','/').Contains("/Grass/")) continue;
                if(mf.sharedMesh.name=="ground_close_04")
                {
                    var r=mf.GetComponent<MeshRenderer>(); if(r && r.enabled) { Undo.RecordObject(r,"Hide raised ground card"); r.enabled=false; PrefabUtility.RecordPrefabInstancePropertyModifications(r); hiddenGround++; } continue;
                }
                if(adjusted%300==0) EditorUtility.DisplayProgressBar("Grounding grass details",mf.name,(float)adjusted/parts.Length);
                if(!cache.TryGetValue(mf.sharedMesh,out var roots)) cache[mf.sharedMesh]=roots=Roots(mf.sharedMesh);
                var t=mf.transform;
                var center=roots.Select(t.TransformPoint).Aggregate(Vector3.zero,(a,v)=>a+v)/roots.Length;
                if(!GrassPlacementTools.Surface(center,colliders,out var hit)) continue;
                Undo.RecordObject(t,"Ground grass detail");
                var oldCenter=center;
                t.rotation=Quaternion.FromToRotation(t.TransformDirection(Vector3.forward),hit.normal)*t.rotation;
                center=roots.Select(t.TransformPoint).Aggregate(Vector3.zero,(a,v)=>a+v)/roots.Length;
                t.position+=new Vector3(oldCenter.x-center.x,0,oldCenter.z-center.z);
                var gaps=new List<float>();
                foreach(var v in roots) { var p=t.TransformPoint(v); if(GrassPlacementTools.Surface(p,colliders,out var h)) gaps.Add(p.y-h.point.y); }
                if(gaps.Count==0) continue;
                t.position+=Vector3.down*(gaps.Max()+.025f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                foreach(var v in roots) { var p=t.TransformPoint(v); if(!GrassPlacementTools.Surface(p,colliders,out var h)) continue; float gap=p.y-h.point.y; worst=Mathf.Max(worst,gap); samples++; if(gap>.035f) floating++; }
                adjusted++;
            }
            Undo.CollapseUndoOperations(undo);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save Environment");
            File.WriteAllText(Work+"/details-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new { adjusted,hiddenGround,samples,floatingRoots=floating,highestRootGap=worst,sceneSaved=!scene.isDirty },Newtonsoft.Json.Formatting.Indented));
            Selection.activeGameObject=null;
            var view=SceneView.lastActiveSceneView;
            if(view) view.LookAt(new Vector3(119,56,533),view.rotation,80,false,true);
            SceneView.RepaintAll();
        }
        finally { EditorUtility.ClearProgressBar(); }
    }
}
