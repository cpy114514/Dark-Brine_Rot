using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Mavis;
using Object=UnityEngine.Object;

public static class IslandShipwreckPass
{
    const string W = ".codex/island-shipwreck-20261107/";
    const string EnvironmentPath = "Assets/Scenes/First Island/Environment.unity";
    const string StoryPath = "Assets/Scenes/First story/Story1.unity";
    const string LibraryPath = "Assets/Resources/Story1Wreck/ShipWreckPieces.asset";
    public const string RootName = "Island 1 - Sahur's washed-ashore shipwreck";
    static float[] V(Vector3 p) => new[]{p.x,p.y,p.z};
    static string Hash(string path){using(var sha=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(f));}
    static void Write(string file,object value)=>File.WriteAllText(W+file,JsonConvert.SerializeObject(value,Formatting.Indented));
    static string Saves()=>JsonConvert.SerializeObject(Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*").OrderBy(p=>p).ToDictionary(Path.GetFileName,p=>Convert.ToBase64String(File.ReadAllBytes(p))));
    public static object Audit()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stopped workspace required.");
        if(File.Exists(W+"workspace.json"))throw new Exception("Keep the existing baseline evidence; use a new evidence directory for another audit.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        if(setup.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve unsaved scene edits.");
        Directory.CreateDirectory(W);Directory.CreateDirectory(W+"scenes-before");
        Write("workspace.json",setup);File.WriteAllText(W+"saves.json",Saves());
        foreach(var s in setup)File.Copy(s.path,W+"scenes-before/"+Path.GetFileName(s.path),false);
        File.Copy(StoryPath,W+"scenes-before/Story1.unity",false);
        var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(LibraryPath);
        var deps=AssetDatabase.GetDependencies(LibraryPath,true).Append(StoryPath).Distinct().Where(File.Exists).ToDictionary(p=>p,Hash);
        Write("opening-source-hashes.json",deps);
        object Piece(Story1ShipWreckAsset.Fragment p,int i)=>new{index=i,p.name,p.sourceObject,mesh=AssetDatabase.GetAssetPath(p.mesh),material=AssetDatabase.GetAssetPath(p.material),center=V(p.sourceCenter),size=V(p.sourceSize),p.floatRotation,vertices=p.mesh.vertexCount,triangles=p.mesh.triangles.Length/3,splinters=p.splinters?.Length??0};
        Write("fragments.json",new{originalShip=AssetDatabase.GetAssetPath(asset.originalShip),asset.sourceSha256,decks=asset.decks.Select(Piece),hulls=asset.hulls.Select(Piece),details=asset.details.Select(Piece)});
        var previous=SceneManager.GetActiveScene();var story=EditorSceneManager.OpenScene(StoryPath,OpenSceneMode.Additive);
        try
        {
            var sequence=story.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Story1SharkCollisionSequence>(true)).Single();
            var ship=sequence.ship.transform;
            var result=new{ship=ship.name,scale=V(ship.lossyScale),rotation=V(ship.eulerAngles),modelScale=V(ship.lossyScale*.01f),decks=asset.decks.Length,hulls=asset.hulls.Length,details=asset.details.Length,hasReferences=sequence.sahur&&sequence.swimmer&&sequence.storyCamera&&sequence.awakening,shipMaterials=ship.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(AssetDatabase.GetAssetPath)};
            Write("opening-audit.json",result);return result;
        }
        finally{EditorSceneManager.CloseScene(story,true);SceneManager.SetActiveScene(previous);}
    }
    public static object CoastSurvey()
    {
        var ground=Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i=>i.isActiveAndEnabled).GetComponent<MeshCollider>();
        var points=new List<object>();
        for(int z=280;z<=450;z+=10)for(int x=190;x<=400;x+=10)
            if(ground.Raycast(new Ray(new Vector3(x,ground.bounds.max.y+30,z),Vector3.down),out var hit,ground.bounds.size.y+60)&&hit.point.y>-2&&hit.point.y<9)
                points.Add(new{point=V(hit.point),normal=V(hit.normal)});
        Write("coast-survey.json",points);return new{candidates=points.Count,ground=ground.bounds.ToString()};
    }
    public static string Prepare()
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Keep dirty workspace.");
        EditorSceneManager.OpenScene(EnvironmentPath,OpenSceneMode.Single);EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);return "Environment + Lighting only";
    }
    static MeshCollider Ground()=>Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i=>i.isActiveAndEnabled&&i.gameObject.scene.path==EnvironmentPath).GetComponent<MeshCollider>();
    static RaycastHit Surface(float x,float z)
    {
        var ground=Ground();if(!ground.Raycast(new Ray(new Vector3(x,ground.bounds.max.y+30,z),Vector3.down),out var hit,ground.bounds.size.y+60))throw new Exception("Wreck placement has no terrain support.");return hit;
    }
    static GameObject MeshObject(Transform parent,string name,Mesh mesh,Material material,Vector3 position,Quaternion rotation,Vector3 scale,bool solid=false)
    {
        if(!mesh||!material)throw new Exception("Source mesh/material is missing.");
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(position,rotation);go.transform.localScale=scale;go.isStatic=true;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;
        if(solid){var c=go.AddComponent<MeshCollider>();c.sharedMesh=mesh;c.convex=true;}
        return go;
    }
    static Vector3[] Points(MeshFilter filter)=>filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
    static void Seat(GameObject go,float burial)
    {
        var points=go.GetComponentsInChildren<MeshFilter>().SelectMany(Points).ToArray();
        float minimum=float.PositiveInfinity;
        foreach(var p in points){var hit=Surface(p.x,p.z);minimum=Mathf.Min(minimum,p.y-hit.point.y);}
        go.transform.position+=Vector3.up*(-minimum-burial);
    }
    static Quaternion FlatRotation(Mesh mesh,Vector3 size,float yaw,Vector3 normal,float tilt=0)
    {
        var points=mesh.vertices.Select(v=>Vector3.Scale(v,size)).ToArray();var t=mesh.triangles;Vector3 face=Vector3.up;float maximum=0;
        for(int i=0;i<t.Length;i+=3){var n=Vector3.Cross(points[t[i+1]]-points[t[i]],points[t[i+2]]-points[t[i]]);if(n.sqrMagnitude>maximum){maximum=n.sqrMagnitude;face=n.normalized;}}
        if(face.y<0)face=-face;
        return Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,yaw,0)*Quaternion.Euler(tilt,0,0)*Quaternion.FromToRotation(face,Vector3.up);
    }
    public static object BuildPreview()
    {
        var scene=SceneManager.GetSceneByPath(EnvironmentPath);
        if(Application.isPlaying||!scene.isLoaded||scene.isDirty||scene.GetRootGameObjects().Any(g=>g.name==RootName))throw new Exception("Clean stopped Environment without an existing wreck is required.");
        var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(LibraryPath);
        var root=new GameObject(RootName);SceneManager.MoveGameObjectToScene(root,scene);Undo.RegisterCreatedObjectUndo(root,"Place opening shipwreck on Island 1");root.isStatic=true;
        try
        {
            var bow=new GameObject("Recognizable bow - original opening sections");bow.transform.SetParent(root.transform,false);bow.transform.position=Surface(378,395).point;
            var hit=Surface(378,395);bow.transform.rotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,70,0)*Quaternion.Euler(28,0,-7);
            Vector3 origin=new Vector3(20,4,0);float sourceScale=2;
            void Assemble(Story1ShipWreckAsset.Fragment p,bool collision)
            {
                var child=MeshObject(bow.transform,p.name,p.mesh,p.material,Vector3.zero,Quaternion.identity,Vector3.Scale(p.sourceSize,Vector3.one*sourceScale),collision);
                child.transform.localPosition=(p.sourceCenter-origin)*sourceScale;
                child.transform.localRotation=Quaternion.LookRotation(Vector3.right)*Quaternion.Euler(0,0,-p.floatRotation);
            }
            foreach(int index in new[]{0,1,2,3})Assemble(asset.hulls[index],true);
            foreach(int index in new[]{0,1,2})Assemble(asset.decks[index],false);
            foreach(var p in asset.details.Where(p=>p.sourceCenter.x>16&&p.sourceCenter.x<26&&p.sourceCenter.y<13&&p.mesh.vertexCount<16000))Assemble(p,false);
            Seat(bow,.65f);

            void Loose(Story1ShipWreckAsset.Fragment p,float x,float z,float yaw,float burial,float tilt,bool collision)
            {
                var ground=Surface(x,z);var size=p.sourceSize*sourceScale;
                var go=MeshObject(root.transform,"Washed ashore | "+p.name,p.mesh,p.material,ground.point,FlatRotation(p.mesh,size,yaw,ground.normal,tilt),size,collision);Seat(go,burial);
            }
            Loose(asset.hulls[5],369,420,42,.38f,13,true);
            Loose(asset.hulls[21],381,441,126,.55f,-18,true);
            Loose(asset.details[130],368,382,32,.08f,2,true);
            Loose(asset.details[157],366,392,57,.08f,12,false);
            var rng=new System.Random(7110726);
            for(int i=0;i<19;i++)
            {
                var source=asset.decks[new[]{4,5,6,8,9,10}[i%6]];var splinter=source.splinters[i%source.splinters.Length];
                float z=364+i*4.7f+(float)rng.NextDouble()*2;float shoreX= z<400 ? 378+(z-360)*.22f : 390;
                float x=shoreX-3-(float)rng.NextDouble()*10;
                var ground=Surface(x,z);var size=Vector3.Scale(source.sourceSize*sourceScale,splinter.localSize);
                float yaw=35+(float)rng.NextDouble()*90;
                var go=MeshObject(root.transform,"Tide scatter | "+source.name+" splinter "+i,splinter.mesh,source.material,ground.point,FlatRotation(splinter.mesh,size,yaw,ground.normal,(float)rng.NextDouble()*8-4),size);
                Seat(go,.035f+(float)rng.NextDouble()*.09f);
            }
            var ecology=AssetDatabase.LoadAssetAtPath<IslandEcologyData>("Assets/Generated/Island1Realism/Woodland and coastal dressing.asset");
            var ledge=ecology.species.First(s=>s.name=="Fractured ledge A");
            var shingle=ecology.species.First(s=>s.name=="Shingle cluster A");
            for(int i=0;i<11;i++)
            {
                float x=375+(float)rng.NextDouble()*10,z=377+(float)rng.NextDouble()*43;var h=Surface(x,z);var s=i<2?ledge:shingle;float scale=i<2?.7f:.65f+(float)rng.NextDouble()*.6f;
                var go=MeshObject(root.transform,"Coastal contact | "+s.name+" "+i,s.near,s.material,h.point,Quaternion.FromToRotation(Vector3.up,h.normal)*Quaternion.Euler(0,(float)rng.NextDouble()*360,0),Vector3.one*scale,i<2);Seat(go,.09f);
            }
            var kelpMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Generated/Island1Decoration/Kelp near.asset");var kelpMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/Island1Decoration/Damp kelp.mat");
            foreach(var p in new[]{new Vector2(387,381),new Vector2(389,400),new Vector2(392,417),new Vector2(390,435),new Vector2(375,365)})
            {
                var h=Surface(p.x,p.y);var go=MeshObject(root.transform,"Tidal wrack | existing kelp",kelpMesh,kelpMat,h.point,Quaternion.FromToRotation(Vector3.up,h.normal)*Quaternion.Euler(0,(float)rng.NextDouble()*360,0),Vector3.one*(.8f+(float)rng.NextDouble()*.5f));Seat(go,.025f);
            }
            root.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);
            var result=new{renderers=root.GetComponentsInChildren<Renderer>(true).Length,colliders=root.GetComponentsInChildren<Collider>(true).Length,rigidbodies=root.GetComponentsInChildren<Rigidbody>(true).Length,originalScale=sourceScale,bowBounds=BoundsOf(bow).ToString(),previewInactive=true};Write("build-preview.json",result);return result;
        }
        catch{Undo.DestroyObjectImmediate(root);throw;}
    }
    static Bounds BoundsOf(GameObject go){var parts=go.GetComponentsInChildren<Renderer>(true);var b=parts[0].bounds;foreach(var r in parts.Skip(1))b.Encapsulate(r.bounds);return b;}
    public static object SmallPieceAudit()
    {
        var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(LibraryPath);
        var rows=asset.decks.Concat(asset.hulls).Concat(asset.details).SelectMany(p=>(p.splinters??Array.Empty<Story1ShipWreckAsset.Shard>()).Select((s,i)=>new{p.name,index=i,size=Vector3.Scale(p.sourceSize*2,s.localSize),s.mesh.vertexCount})).Where(p=>Mathf.Max(p.size.x,p.size.y,p.size.z)<=6.5f&&Mathf.Min(p.size.x,p.size.y,p.size.z)<.9f).ToArray();
        Write("small-piece-options.json",rows.Select(r=>new{r.name,r.index,size=V(r.size),r.vertexCount}));return new{count=rows.Length,examples=rows.Take(6).Select(r=>new{r.name,r.index,size=V(r.size)})};
    }
    public static object RefineScatter()
    {
        var scene=SceneManager.GetSceneByPath(EnvironmentPath);if(Application.isPlaying||scene.isDirty)throw new Exception("Clean stopped Environment required");
        var root=scene.GetRootGameObjects().Single(g=>g.name==RootName);var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(LibraryPath);
        if(PrefabUtility.IsPartOfPrefabInstance(root))throw new Exception("Published landmark: edit the prefab or its overrides; do not rerun prototype refinement.");
        foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Tide scatter |")).ToArray())Undo.DestroyObjectImmediate(t.gameObject);
        var small=asset.decks.Concat(asset.hulls).SelectMany(p=>(p.splinters??Array.Empty<Story1ShipWreckAsset.Shard>()).Select(s=>new{source=p,shard=s,size=Vector3.Scale(p.sourceSize*2,s.localSize)})).Where(p=>Mathf.Max(p.size.x,p.size.y,p.size.z)<=6.5f&&Mathf.Min(p.size.x,p.size.y,p.size.z)<.9f).ToArray();
        var clusters=new[]{new Vector2(371,375),new Vector2(378,400),new Vector2(380,425),new Vector2(382,443)};var rng=new System.Random(7110727);int colliders=0;
        for(int i=0;i<19;i++)
        {
            var center=clusters[i%clusters.Length];float x=center.x+(float)rng.NextDouble()*7-3.5f,z=center.y+(float)rng.NextDouble()*9-4.5f;
            Mesh mesh;Material material;Vector3 size;string name;
            if(i<7){var p=small[i%small.Length];mesh=p.shard.mesh;material=p.source.material;size=p.size;name=p.source.name+" original splinter";}
            else{var p=asset.details[new[]{4,6,27,40,75,78,81}[(i-7)%7]];mesh=p.mesh;material=p.material;size=p.sourceSize*2;name=p.name;}
            var h=Surface(x,z);var go=MeshObject(root.transform,"Tide scatter | "+i.ToString("00")+" | "+name,mesh,material,h.point,FlatRotation(mesh,size,45+(float)rng.NextDouble()*70,h.normal),size);
            if(Mathf.Max(size.x,size.y,size.z)>4){var box=go.AddComponent<BoxCollider>();box.center=mesh.bounds.center;box.size=mesh.bounds.size*.92f;colliders++;}
            Undo.RegisterCreatedObjectUndo(go,"Refine original ship fragments");Seat(go,.075f+(float)rng.NextDouble()*.06f);
        }
        foreach(var pair in new[]{(0,new Vector2(371,420)),(1,new Vector2(374,423))})
        {
            var rock=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Coastal contact | Fractured ledge A "+pair.Item1);Undo.RecordObject(rock,"Seat hull beside coastal rock");
            var h=Surface(pair.Item2.x,pair.Item2.y);rock.SetPositionAndRotation(h.point,Quaternion.FromToRotation(Vector3.up,h.normal)*Quaternion.Euler(0,36+pair.Item1*71,0));rock.localScale=Vector3.one*(pair.Item1==0?1.35f:1.0f);Seat(rock.gameObject,.12f);
        }
        EditorSceneManager.MarkSceneDirty(scene);var result=new{smallPieces=19,smallPieceColliders=colliders,rigidbodies=root.GetComponentsInChildren<Rigidbody>(true).Length};Write("scatter-refinement.json",result);return result;
    }
    public static string PrepareOpening()
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Clean stopped workspace required");
        EditorSceneManager.OpenScene(StoryPath,OpenSceneMode.Single);return "Original opening scene, unchanged";
    }
    public static object Validate()
    {
        var scene=SceneManager.GetSceneByPath(EnvironmentPath);if(!scene.isLoaded)throw new Exception("Load Environment");
        var root=scene.GetRootGameObjects().Single(g=>g.name==RootName);bool active=root.activeSelf;root.SetActive(true);
        try
        {
            Physics.SyncTransforms();
            var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(LibraryPath);
            var fragments=asset.decks.Concat(asset.hulls).Concat(asset.details).ToArray();
            var meshes=fragments.Select(p=>p.mesh).Concat(fragments.SelectMany(p=>(p.splinters??Array.Empty<Story1ShipWreckAsset.Shard>()).Select(s=>s.mesh))).ToHashSet();
            var mats=fragments.Select(p=>p.material).ToHashSet();
            var filters=root.GetComponentsInChildren<MeshFilter>();var renderers=root.GetComponentsInChildren<MeshRenderer>();var colliders=root.GetComponentsInChildren<Collider>();var owned=colliders.ToHashSet();
            var shipFilters=filters.Where(f=>!f.name.StartsWith("Coastal contact |")&&!f.name.StartsWith("Tidal wrack |")).ToArray();
            var invalidSources=shipFilters.Where(f=>!meshes.Contains(f.sharedMesh)||!mats.Contains(f.GetComponent<MeshRenderer>().sharedMaterial)).Select(f=>f.name).ToArray();
            int missing=filters.Count(f=>!f.sharedMesh)+renderers.Count(r=>r.sharedMaterials.Any(m=>!m));
            var settings=AssetDatabase.LoadAssetAtPath<IslandEcologySettings>("Assets/Generated/IslandEcology074/Settings.asset");
            int routeSamples=0,blocked=0;var blockedAt=new List<float[]>();
            void Capsule(Vector2 p)
            {
                var h=Surface(p.x,p.y);routeSamples++;
                if(Physics.OverlapCapsule(h.point+Vector3.up*.75f,h.point+Vector3.up*4.1f,.65f,~0,QueryTriggerInteraction.Ignore).Any(c=>owned.Contains(c))){blocked++;blockedAt.Add(V(h.point));}
            }
            foreach(var corridor in settings.corridors)
            for(int i=1;i<corridor.points.Length;i++)
            {
                var a=corridor.points[i-1];var b=corridor.points[i];int steps=Mathf.CeilToInt(Vector2.Distance(a,b));var direction=(b-a).normalized;var side=new Vector2(-direction.y,direction.x);
                for(int j=0;j<=steps;j++)foreach(float lane in new[]{-.8f,0,.8f})Capsule(Vector2.Lerp(a,b,j/(float)Mathf.Max(1,steps))+side*(corridor.halfWidth*lane));
            }
            foreach(var clearing in settings.clearings)
            for(int i=0;i<37;i++){float r=clearing.radius*Mathf.Sqrt(i/37f),angle=i*2.399963f;Capsule(clearing.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r);}
            for(int z=347;z<=467;z++)foreach(float x in new[]{351f,353f,355f})Capsule(new Vector2(x,z));
            int unqueryable=0;
            foreach(var c in colliders)
            {
                bool hit=false;var b=c.bounds;
                foreach(var direction in new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                {
                    var from=b.center+direction*(b.extents.magnitude+2);if(c.Raycast(new Ray(from,-direction),out _,b.extents.magnitude*2+4)){hit=true;break;}
                }
                if(!hit)unqueryable++;
            }
            var contacts=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Tide scatter |")).Select(t=>
            {
                var points=t.GetComponentsInChildren<MeshFilter>().SelectMany(Points).ToArray();var gaps=points.Select(p=>p.y-Surface(p.x,p.z).point.y).ToArray();return new{name=t.name,minimumGap=gaps.Min(),maximumGap=gaps.Max(),size=V(t.GetComponent<MeshRenderer>().bounds.size)};
            }).ToArray();
            var shaderErrors=renderers.Select(r=>r.sharedMaterial.shader).Distinct().SelectMany(ShaderUtil.GetShaderMessages).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
            var result=new{passed=missing==0&&invalidSources.Length==0&&blocked==0&&unqueryable==0&&shaderErrors.Length==0&&root.GetComponentsInChildren<Rigidbody>().Length==0,renderers=renderers.Length,originalShipParts=shipFilters.Length,originalMeshesAndMaterials=invalidSources.Length==0,invalidSources,missing,colliders=colliders.Length,unqueryable,routeSamples,blocked,blockedAt,rigidbodies=root.GetComponentsInChildren<Rigidbody>().Length,uniqueMaterials=renderers.Select(r=>r.sharedMaterial).Distinct().Count(),triangles=filters.Sum(f=>f.sharedMesh.triangles.Length/3),contacts,shaderErrors,bounds=BoundsOf(root).ToString()};
            Write("validation.json",result);return new{result.passed,result.renderers,result.originalShipParts,result.colliders,result.routeSamples,result.blocked,result.triangles};
        }
        finally{root.SetActive(active);Physics.SyncTransforms();}
    }
    public static object PublishPrefab()
    {
        var scene=SceneManager.GetSceneByPath(EnvironmentPath);if(Application.isPlaying||scene.isDirty)throw new Exception("Clean stopped Environment required");
        var check=Validate();var json=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(W+"validation.json"));if(!(bool)json["passed"])throw new Exception("Wreck validation failed");
        const string folder="Assets/Generated/Island1Shipwreck";const string path=folder+"/Sahur opening wreck - washed ashore.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path))throw new Exception("Keep existing wreck prefab edits");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Generated","Island1Shipwreck");
        var root=scene.GetRootGameObjects().Single(g=>g.name==RootName);Undo.RecordObject(root,"Activate validated coastal wreck");root.SetActive(true);
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root,path,InteractionMode.AutomatedAction,out bool success);if(!success||!prefab)throw new Exception("Wreck prefab save failed");
        EditorSceneManager.MarkSceneDirty(scene);Write("published.json",new{prefab=path,parts=root.GetComponentsInChildren<Renderer>().Length,originalAssetsUnmodified=true});return new{prefab=path,connected=PrefabUtility.IsPartOfPrefabInstance(root)};
    }
    public static object RefinePublishedHull()
    {
        var scene=SceneManager.GetSceneByPath(EnvironmentPath);if(Application.isPlaying||scene.isDirty)throw new Exception("Clean stopped Environment required");
        var instance=scene.GetRootGameObjects().Single(g=>g.name==RootName);var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);
        if(path!="Assets/Generated/Island1Shipwreck/Sahur opening wreck - washed ashore.prefab")throw new Exception("Unexpected prefab owner");
        var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(LibraryPath);var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var wall=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Washed ashore | "+asset.hulls[21].name);var source=asset.hulls[9];var h=Surface(381,441);var size=source.sourceSize*2;
            wall.name="Washed ashore | "+source.name;wall.sharedMesh=source.mesh;wall.GetComponent<MeshRenderer>().sharedMaterial=source.material;wall.transform.localScale=size;
            wall.transform.SetPositionAndRotation(h.point,FlatRotation(source.mesh,size,126,h.normal,3));wall.GetComponent<MeshCollider>().sharedMesh=source.mesh;Seat(wall.gameObject,.35f);
            var panel=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Washed ashore | "+asset.hulls[5].name);var p=asset.hulls[5];h=Surface(369,420);panel.transform.SetPositionAndRotation(h.point,FlatRotation(p.mesh,p.sourceSize*2,42,h.normal,4));Seat(panel.gameObject,.38f);
            var pivot=Surface(378,395).point;
            foreach(Transform child in root.transform)child.localPosition-=pivot;
            PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);if(!saved)throw new Exception("Hull refinement save failed");
            Undo.RecordObject(instance.transform,"Center coastal wreck landmark pivot");instance.transform.position=pivot;PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);EditorSceneManager.MarkSceneDirty(scene);
            Write("hull-refinement.json",new{replacedUprightPanel=asset.hulls[21].name,withOriginalPanel=source.name,pivot=V(pivot),sourceScale=2,sourceMaterialsPreserved=true});return new{saved,pivot=V(pivot)};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object RefineFinalContacts()
    {
        var scene=SceneManager.GetSceneByPath(EnvironmentPath);if(Application.isPlaying||scene.isDirty)throw new Exception("Clean stopped Environment required");
        var instance=scene.GetRootGameObjects().Single(g=>g.name==RootName);var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);var asset=AssetDatabase.LoadAssetAtPath<Story1ShipWreckAsset>(LibraryPath);
        if(path!="Assets/Generated/Island1Shipwreck/Sahur opening wreck - washed ashore.prefab")throw new Exception("Unexpected prefab owner");
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.transform.position=instance.transform.position;
            var plank=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Washed ashore | "+asset.hulls[9].name);var source=asset.decks[4];var splinter=source.splinters[4];var size=Vector3.Scale(source.sourceSize*2,splinter.localSize);var h=Surface(381,441);
            plank.name="Washed ashore | "+source.name+" original splinter";plank.sharedMesh=splinter.mesh;plank.GetComponent<MeshRenderer>().sharedMaterial=source.material;plank.transform.localScale=size;plank.transform.SetPositionAndRotation(h.point,FlatRotation(splinter.mesh,size,126,h.normal));plank.GetComponent<MeshCollider>().sharedMesh=splinter.mesh;Seat(plank.gameObject,.12f);
            var panel=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Washed ashore | "+asset.hulls[5].name);panel.transform.position-=Vector3.up*.8f;
            root.transform.position=Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);if(!saved)throw new Exception("Contact refinement save failed");EditorSceneManager.MarkSceneDirty(scene);Write("contact-refinement.json",new{saved,smallPanelUsesOriginalSplinter=true,largePanelAdditionalBurial=.8f});return new{saved};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object Restore()
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Do not discard unsaved edits.");
        EditorSceneManager.RestoreSceneManagerSetup(JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(W+"workspace.json")));
        var hashes=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(W+"opening-source-hashes.json"));
        var result=new{savesUnchanged=Saves()==File.ReadAllText(W+"saves.json"),openingUnchanged=hashes.All(p=>Hash(p.Key)==p.Value),scenes=SceneManager.sceneCount};Write("restored.json",result);return result;
    }
}
