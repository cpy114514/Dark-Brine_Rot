using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class WholeIslandGrassCover
{
    const string Work = "ProjectRepairBackups/WholeIslandGrass-20261025";
    static double next;
    static WholeIslandGrassCover() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        next = EditorApplication.timeSinceStartup + 1;
        string request = Work + "/request.txt";
        if (!File.Exists(request)) return;
        string command = File.ReadAllText(request).Trim();
        File.Delete(request);
        if(command=="densify-normal") { try { IslandGrassVisualRepair.Densify(); } catch(Exception e) { File.WriteAllText(Work+"/density-error.txt",e.ToString()); Debug.LogException(e); } return; }
        if(command=="preview-normal" || command=="repair-normal") { try { IslandGrassVisualRepair.Run(command=="preview-normal"); } catch(Exception e) { File.WriteAllText(Work+"/visual-error.txt",e.ToString()); Debug.LogException(e); } return; }
        try { if (command == "probe") Probe(); else if (command == "fill") Fill(); else if (command == "infill-density") Fill(true); else if (command == "audit") Audit(); else if (command == "ground-probe") IslandGrassGrounding.Run(false); else if (command == "ground-all") IslandGrassGrounding.Run(true); else if (command == "ground-details") IslandGrassGrounding.Details(); }
        catch (Exception e) { File.WriteAllText(Work + "/error.txt", e.ToString()); Debug.LogException(e); }
    }
    static Scene Scene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required");
        var s = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        if (!s.IsValid() || !s.isLoaded) throw new Exception("Environment is not loaded");
        return s;
    }
    static void Probe()
    {
        var s = Scene();
        var islands = s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProceduralIsland>(true)).Select(i => new { i.name, active = i.isActiveAndEnabled, position = V(i.transform.position), scale = V(i.transform.lossyScale), center = V(i.GetComponent<MeshRenderer>().bounds.center), size = V(i.GetComponent<MeshRenderer>().bounds.size), i.shorelineRadius, i.peakHeight }).ToArray();
        string path = "Assets/Game/Prefabs/Environment/Grass/SimpleGrassChunks/rostlinka_07c_ske (1330).prefab";
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var parts = asset.GetComponentsInChildren<MeshFilter>(true).Select(m => new { m.name, mesh = m.sharedMesh.name, vertices = m.sharedMesh.vertexCount, center = V(m.sharedMesh.bounds.center), size = V(m.sharedMesh.bounds.size), local = V(m.transform.localPosition), scale = V(m.transform.localScale), materials = m.GetComponent<MeshRenderer>().sharedMaterials.Select(x => x.name).ToArray() }).ToArray();
        File.WriteAllText(Work + "/probe.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { sceneDirty = s.isDirty, islands, rootScale = V(asset.transform.localScale), parts }, Newtonsoft.Json.Formatting.Indented));
    }
    static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
    static void Audit()
    {
        var scene = Scene();
        var root = scene.GetRootGameObjects().Single(g => g.name == "Whole Island Grass Cover");
        var island = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProceduralIsland>(false)).First(i => i.isActiveAndEnabled);
        var collider = island.GetComponent<MeshCollider>();
        int samples = 0, outside = 0, missingMeshes = 0;
        float lowest = float.MaxValue, highest = float.MinValue;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (!mf.sharedMesh) { missingMeshes++; continue; }
            var vertices = mf.sharedMesh.vertices;
            for (int i = 0; i < vertices.Length; i += 227)
            {
                var p = mf.transform.TransformPoint(vertices[i]);
                if (!collider.Raycast(new Ray(new Vector3(p.x,300,p.z),Vector3.down),out var hit,500)) { outside++; continue; }
                float gap = p.y-hit.point.y; lowest=Mathf.Min(lowest,gap); highest=Mathf.Max(highest,gap); samples++;
            }
        }
        File.WriteAllText(Work+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(new { chunks=root.transform.childCount, samples, outside, missingMeshes, lowestVertexAboveGround=lowest, highestVertexAboveGround=highest, sceneSaved=!scene.isDirty, grounded=outside==0 && missingMeshes==0 && lowest>-.10f && highest<.75f },Newtonsoft.Json.Formatting.Indented));
        var view=SceneView.lastActiveSceneView;
        if(view) { view.orthographic=false; view.LookAt(collider.bounds.center,Quaternion.Euler(50,25,0),collider.bounds.extents.magnitude*.42f,false,true); }
        Selection.activeGameObject=null; SceneView.RepaintAll();
    }
    sealed class Tile
    {
        internal readonly List<Vector3> positions = new List<Vector3>();
        internal readonly List<Vector3> normals = new List<Vector3>();
        internal readonly List<Vector2> uv = new List<Vector2>();
        internal readonly List<int> indices = new List<int>();
        internal int count;
    }
    sealed class Blade
    {
        internal Vector3[] p, n;
        internal Vector2[] uv;
        internal int[] triangles;
    }
    static void Fill(bool infill = false)
    {
        string rootName = infill ? "Matched Density Grass Cover" : "Whole Island Grass Cover";
        string folder = "Assets/Generated/WholeIslandGrass20261025" + (infill ? "Infill" : "");
        var scene = Scene();
        if (scene.GetRootGameObjects().Any(g => g.name == rootName)) throw new Exception("Cover already exists; refusing duplicate generation.");
        var island = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ProceduralIsland>(false)).First(i => i.isActiveAndEnabled);
        var collider = island.GetComponent<MeshCollider>();
        var bounds = collider.bounds;
        const float spacing = 0.78f;
        var filledCells = new HashSet<Vector2Int>();
        var oldRoots = new Dictionary<Vector2Int,List<Vector2>>();
        if(infill)
        {
            var oldCover=scene.GetRootGameObjects().Single(g=>g.name=="Whole Island Grass Cover");
            foreach(var mf in oldCover.GetComponentsInChildren<MeshFilter>())
            foreach(var vertex in mf.sharedMesh.vertices)
            {
                var p=mf.transform.TransformPoint(vertex);
                filledCells.Add(new Vector2Int(Mathf.RoundToInt((p.x-bounds.min.x)/spacing),Mathf.RoundToInt((p.z-bounds.min.z)/spacing)));
            }
            var rootCache=new Dictionary<Mesh,Vector3[]>();
            foreach(var mf in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(false)))
            {
                if(!mf.sharedMesh || !(mf.sharedMesh.name=="Forest001" || mf.sharedMesh.name.StartsWith("r12_") || mf.sharedMesh.name.StartsWith("rostlinka_"))) continue;
                var instance=PrefabUtility.GetOutermostPrefabInstanceRoot(mf.gameObject);
                if(!instance || !PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance).Replace('\\','/').Contains("/Grass/")) continue;
                if(!rootCache.TryGetValue(mf.sharedMesh,out var roots)) rootCache[mf.sharedMesh]=roots=IslandGrassGrounding.Roots(mf.sharedMesh);
                foreach(var vertex in roots)
                {
                    var p=mf.transform.TransformPoint(vertex);
                    if(!collider.Raycast(new Ray(new Vector3(p.x,300,p.z),Vector3.down),out var h,500) || p.y<h.point.y-.35f || p.y>h.point.y+.08f) continue;
                    var key=new Vector2Int(Mathf.FloorToInt(p.x/.5f),Mathf.FloorToInt(p.z/.5f));
                    if(!oldRoots.TryGetValue(key,out var list)) oldRoots[key]=list=new List<Vector2>();
                    list.Add(new Vector2(p.x,p.z));
                }
            }
            if(!File.Exists(Work+"/Environment.before-density.unity")) File.Copy(scene.path,Work+"/Environment.before-density.unity");
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/Grass/SimpleGrassChunks/rostlinka_07c_ske (1330).prefab");
        var sources = prefab.GetComponentsInChildren<MeshFilter>().Where(m => m.name == "r12_a" || m.name == "r12_b" || m.name == "r12_c").ToArray();
        var blades = sources.Select(m =>
        {
            var rotation = Quaternion.Euler(-90, 0, 0);
            var p = m.sharedMesh.vertices.Select(v => rotation * v).ToArray();
            var b = new Bounds(p[0], Vector3.zero); foreach (var v in p) b.Encapsulate(v);
            // Keep each clump inside its own cell; put its lowest roots on the ground.
            float width = Mathf.Max(b.size.x, b.size.z);
            for (int j = 0; j < p.Length; j++) p[j] = new Vector3((p[j].x-b.center.x)/width, (p[j].y-b.min.y)/b.size.y, (p[j].z-b.center.z)/width);
            return new Blade { p = p, n = m.sharedMesh.normals.Select(v => rotation * v).ToArray(), uv = m.sharedMesh.uv, triangles = m.sharedMesh.triangles };
        }).ToArray();
        if (blades.Length != 3) throw new Exception("Expected the three grass card meshes.");
        if (!File.Exists(Work + "/Environment.before.unity")) File.Copy(scene.path, Work + "/Environment.before.unity");
        var occupied = new Dictionary<Vector2Int, List<Bounds>>();
        int oldPatches = 0;
        foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)))
        {
            if (r.name != "Forest001" && !r.name.StartsWith("Forest00")) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh || !mf.sharedMesh.name.StartsWith("Forest")) continue;
            var b = r.bounds;
            if (b.size.x > 25 || b.size.z > 25) continue;
            oldPatches++;
            for (int x = Mathf.FloorToInt(b.min.x/8); x <= Mathf.FloorToInt(b.max.x/8); x++)
            for (int z = Mathf.FloorToInt(b.min.z/8); z <= Mathf.FloorToInt(b.max.z/8); z++)
            { var key = new Vector2Int(x,z); if (!occupied.TryGetValue(key,out var list)) occupied[key] = list = new List<Bounds>(); list.Add(b); }
        }
        var tiles = new Dictionary<Vector2Int, Tile>();
        var random = new System.Random(20261025);
        int count = 0, skippedExisting = 0, skippedWater = 0;
        int checkedRoots = 0; float maxRootError = 0;
        Physics.SyncTransforms();
        int row = 0;
        for (float z = bounds.min.z; z <= bounds.max.z; z += spacing, row++)
        {
            if (row % 70 == 0) EditorUtility.DisplayProgressBar("Covering island in grass", "Sampling ground and filling empty areas", (z-bounds.min.z)/bounds.size.z);
            for (float x = bounds.min.x; x <= bounds.max.x; x += spacing)
            {
                float px = x + (float)(random.NextDouble()-.5)*.06f;
                float pz = z + (float)(random.NextDouble()-.5)*.06f;
                if(infill && filledCells.Contains(new Vector2Int(Mathf.RoundToInt((x-bounds.min.x)/spacing),row))) { skippedExisting++; continue; }
                var p = new Vector3(px, bounds.max.y+10, pz);
                if (!collider.Raycast(new Ray(p, Vector3.down),out var hit,bounds.size.y+30) || hit.point.y < .65f || hit.normal.y < .70f) { skippedWater++; continue; }
                bool overlap = false;
                if (!infill && occupied.TryGetValue(new Vector2Int(Mathf.FloorToInt(px/8),Mathf.FloorToInt(pz/8)),out var patches))
                    foreach (var b in patches) if (px >= b.min.x-.15f && px <= b.max.x+.15f && pz >= b.min.z-.15f && pz <= b.max.z+.15f && Mathf.Abs(hit.point.y-b.min.y)<3f) { overlap=true; break; }
                if(infill)
                {
                    int cx=Mathf.FloorToInt(px/.5f),cz=Mathf.FloorToInt(pz/.5f);
                    for(int dx=-1;dx<=1&&!overlap;dx++) for(int dz=-1;dz<=1&&!overlap;dz++)
                        if(oldRoots.TryGetValue(new Vector2Int(cx+dx,cz+dz),out var list))
                            foreach(var old in list) if((old-new Vector2(px,pz)).sqrMagnitude<.42f*.42f) {overlap=true;break;}
                }
                if (overlap) { skippedExisting++; continue; }
                var key = new Vector2Int(Mathf.FloorToInt(px/24),Mathf.FloorToInt(pz/24));
                if (!tiles.TryGetValue(key,out var tile)) tiles[key] = tile = new Tile();
                var blade = blades[random.Next(blades.Length)];
                var q = Quaternion.AngleAxis((float)random.NextDouble()*360,Vector3.up);
                float width = .49f + (float)random.NextDouble()*.025f;
                float height = .40f + (float)random.NextDouble()*.25f;
                var origin = new Vector3(key.x*24,0,key.y*24);
                int offset = tile.positions.Count;
                foreach (var v in blade.p)
                {
                    var horizontal = q * new Vector3(v.x*width,0,v.z*width);
                    // Each card follows the local ground plane; roots sit 2 cm into it.
                    float ground = hit.point.y - (hit.normal.x*horizontal.x+hit.normal.z*horizontal.z)/hit.normal.y;
                    tile.positions.Add(new Vector3(px+horizontal.x,ground+v.y*height-.02f,pz+horizontal.z)-origin);
                    if(infill && v.y<.001f && count%31==0 && collider.Raycast(new Ray(new Vector3(px+horizontal.x,300,pz+horizontal.z),Vector3.down),out var rootHit,500))
                    { checkedRoots++; maxRootError=Mathf.Max(maxRootError,Mathf.Abs(ground-.02f-rootHit.point.y)); }
                }
                foreach (var n in blade.n) tile.normals.Add(q*n);
                tile.uv.AddRange(blade.uv);
                foreach (int index in blade.triangles) tile.indices.Add(offset+index);
                tile.count++; count++;
            }
        }
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var material = new Material(sources[0].GetComponent<MeshRenderer>().sharedMaterial) { name = "Island Meadow Grass" };
        // Combined meshes span slopes, so use stationary roots without a shared world-height wind pivot.
        if (material.HasProperty("_WindBend")) material.SetFloat("_WindBend",0);
        AssetDatabase.CreateAsset(material,folder+"/Grass.mat");
        var parent = new GameObject(rootName); SceneManager.MoveGameObjectToScene(parent,scene);
        Undo.RegisterCreatedObjectUndo(parent,"Cover whole island with grass");
        int written = 0; long vertexCount = 0;
        try
        {
            foreach (var entry in tiles)
            {
                var t = entry.Value;
                var mesh = new Mesh { name = "Island grass "+entry.Key, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(t.positions); mesh.SetNormals(t.normals); mesh.SetUVs(0,t.uv); mesh.SetTriangles(t.indices,0); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,folder+"/Grass_"+entry.Key.x+"_"+entry.Key.y+".asset");
                var go = new GameObject("Grass "+entry.Key+" ("+t.count+")",typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(parent.transform,false); go.transform.position = new Vector3(entry.Key.x*24,0,entry.Key.y*24);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
                vertexCount+=mesh.vertexCount; written++;
                if (written%50==0) EditorUtility.DisplayProgressBar("Saving grass patches",written+" / "+tiles.Count,(float)written/tiles.Count);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save Environment scene");
            File.WriteAllText(Work+(infill?"/density-result.json":"/result.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new { addedClumps=count, chunks=written, vertices=vertexCount, existingPatches=oldPatches, skippedExisting, skippedWater, spacing, matchedExistingCells=filledCells.Count, checkedRoots, maxRootError, sceneSaved=!scene.isDirty },Newtonsoft.Json.Formatting.Indented));
            var view=SceneView.lastActiveSceneView;
            if(view) { view.orthographic=false; if(!infill) view.LookAt(bounds.center,Quaternion.Euler(50,25,0),bounds.extents.magnitude*.85f,false,true); }
            Selection.activeGameObject=infill?null:island.gameObject; SceneView.RepaintAll();
            Debug.Log("[Whole Island Grass] Added "+count+" grounded clumps in "+written+" chunks.");
        }
        finally { EditorUtility.ClearProgressBar(); }
    }
}
