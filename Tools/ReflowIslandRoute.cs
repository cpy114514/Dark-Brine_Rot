using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
using Object=UnityEngine.Object;
public static class ReflowIslandRoute
{
 const string W=".codex/island1-route";
 static readonly Vector2[] Arenas={new Vector2(241,465),new Vector2(142,565),new Vector2(20,710),new Vector2(-114,826)};
 static readonly float[] ArenaRadii={30,40,38,46};
 static readonly Vector2[] Checkpoints={new Vector2(275,408),new Vector2(191,518),new Vector2(62,660),new Vector2(-32,754)};
 static readonly string[] Names={"Cappuccino approach","Chicken flock approach","Nailong approach","Bombardino approach"};
 static readonly Vector2[] Route={new Vector2(307,319),new Vector2(279,357),new Vector2(275,408),new Vector2(259,438),new Vector2(241,465),new Vector2(213,491),new Vector2(191,518),new Vector2(142,565),new Vector2(110,608),new Vector2(85,633),new Vector2(62,660),new Vector2(20,710),new Vector2(-32,754),new Vector2(-75,793),new Vector2(-114,826)};
 static Vector2 Xz(Vector3 p)=>new Vector2(p.x,p.z);
 static Vector3 Ground(MeshCollider mesh,Vector2 p){if(!mesh.Raycast(new Ray(new Vector3(p.x,200,p.y),Vector3.down),out var h,400))throw new Exception("Off island: "+p);return h.point;}
 static Vector2 NearestRoute(Vector2 p){float best=float.PositiveInfinity;var chosen=Vector2.zero;for(int i=1;i<Route.Length;i++){var d=Route[i]-Route[i-1];var point=Route[i-1]+d*Mathf.Clamp01(Vector2.Dot(p-Route[i-1],d)/d.sqrMagnitude);float distance=(point-p).sqrMagnitude;if(distance<best){best=distance;chosen=point;}}return chosen;}
 public static object Survey(){
  if(Application.isPlaying)throw new Exception("Stop play first");
  Directory.CreateDirectory(W);
  var scenes=EditorSceneManager.GetSceneManagerSetup();
  if(scenes.Any(s=>SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve existing dirty scenes first");
  if(!File.Exists(W+"/setup.json"))File.WriteAllText(W+"/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(scenes));
  foreach(var s in scenes)if(s.isLoaded&&!File.Exists(W+"/"+Path.GetFileName(s.path)))File.Copy(s.path,W+"/"+Path.GetFileName(s.path));
  Directory.CreateDirectory(W+"/save-before");foreach(var f in Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save.json*"))if(!File.Exists(W+"/save-before/"+Path.GetFileName(f)))File.Copy(f,W+"/save-before/"+Path.GetFileName(f));
  var island=Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i=>i.isActiveAndEnabled&&i.gameObject.scene.name=="Environment");
  var mesh=island.GetComponent<MeshCollider>();Physics.SyncTransforms();
  var sites=new[]{new Vector2(241,465),new Vector2(142,565),new Vector2(20,710),new Vector2(-114,826),new Vector2(275,408),new Vector2(191,518),new Vector2(62,660),new Vector2(-32,754)};
  var sample=new List<object>();foreach(var p in sites){
   var heights=new List<float>();float slope=0;
   for(int x=-24;x<=24;x+=4)for(int z=-24;z<=24;z+=4)if(mesh.Raycast(new Ray(new Vector3(p.x+x,200,p.y+z),Vector3.down),out var hit,400)){heights.Add(hit.point.y);slope=Mathf.Max(slope,Vector3.Angle(Vector3.up,hit.normal));}
   sample.Add(new{p=p.ToString(),min=heights.Min(),max=heights.Max(),maxSlope=slope,obstacles=Physics.OverlapSphere(new Vector3(p.x,heights.Average(),p.y),25,~0,QueryTriggerInteraction.Ignore).Where(c=>c!=mesh).Select(c=>c.name).ToArray()});
  }
  var result=new{island=island.transform.position.ToString(),scale=island.transform.lossyScale.ToString(),island.radialSegments,island.rings,island.shorelineRadius,island.peakHeight,sites=sample,checkpoints=Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None).Select(p=>new{p.name,p.pointName,p.isDefault,position=p.transform.position.ToString(),id=p.GetComponent<WorldSaveIdentity>()?.id}).ToArray()};
  File.WriteAllText(W+"/survey.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 public static object RemovePatrols(){
  if(Application.isPlaying)throw new Exception("Stop play first");
  var s=SceneManager.GetSceneByName("Enemies");var root=s.GetRootGameObjects().SingleOrDefault(g=>g.name=="Island 1 - roadside patrols");
  int count=root?root.GetComponentsInChildren<ChickenBroAI>(true).Length:0;
  if(root){Undo.DestroyObjectImmediate(root);EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");}
  return new{removed=count};
 }
 public static object Supplies(){
  return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
   .Where(c=>c&&c.gameObject.scene.IsValid()&&(c is IslandSupplyCache||c is PlacedEquipmentPickup||c is RifleAmmoPickup||c.GetType().Name.Contains("Abduc")))
   .Select(c=>new{c.name,type=c.GetType().Name,scene=c.gameObject.scene.name,p=c.transform.position.ToString("F2"),json=EditorJsonUtility.ToJson(c)}).ToArray();
 }
 public static object Apply(){
  if(Application.isPlaying)throw new Exception("Stop play first");
  var env=SceneManager.GetSceneByName("Environment");var enemies=SceneManager.GetSceneByName("Enemies");var gameplay=SceneManager.GetSceneByName("Gameplay");
  if(new[]{env,enemies,gameplay}.Any(s=>!s.isLoaded||s.isDirty))throw new Exception("Clean loaded scene set required");
  var island=Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i=>i.isActiveAndEnabled&&i.gameObject.scene==env);
  if(island.landforms.Length>0)throw new Exception("Route already authored; inspect existing landforms before reapplying");
  var ground=island.GetComponent<MeshCollider>();Physics.SyncTransforms();
  var cap=Object.FindFirstObjectByType<CappuccinoAI>();var flock=Object.FindFirstObjectByType<ChickenBroEncounter>();var nail=Object.FindFirstObjectByType<NailongAI>();var plane=Object.FindFirstObjectByType<BombardinoBoss>();
  var before=ground.sharedMesh.vertices;var forms=new List<ProceduralIsland.Landform>();
  for(int i=0;i<Arenas.Length;i++){var local=island.transform.InverseTransformPoint(Ground(ground,Arenas[i]));forms.Add(new ProceduralIsland.Landform{name=Names[i]+" clearing",center=Xz(local),height=local.y,innerRadius=ArenaRadii[i]*.7f,blendWidth=24,maxCutFill=4,strength=.85f});}
  foreach(var p in new[]{new Vector2(110,608),new Vector2(-55,777)}){var local=island.transform.InverseTransformPoint(Ground(ground,p));forms.Add(new ProceduralIsland.Landform{name="Route saddle",center=Xz(local),height=local.y,innerRadius=10,blendWidth=22,maxCutFill=2,strength=.7f});}
  var treeRoots=env.GetRootGameObjects().Where(g=>g.name.ToLowerInvariant().Contains("tree")||g.name.StartsWith("20260918140331")).SelectMany(g=>g.name=="Island 1 - varied woodland trees"?g.transform.Cast<Transform>().Select(t=>t.gameObject):new[]{g}).ToArray();
  var trees=new List<(GameObject go,Vector3 root,float oldGround)>();
  foreach(var tree in treeRoots){
   var f=tree.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&!f.name.Contains("geometry_nodes")&&!f.name.Contains("Leaves")&&!f.name.Contains("Branches")&&!f.name.Contains("LOD1")).OrderByDescending(f=>f.sharedMesh.bounds.size.y).FirstOrDefault();
   if(!f)continue;var verts=f.sharedMesh.vertices;float min=verts.Min(v=>v.y);var bottom=verts.Where(v=>v.y<min+.05f).ToArray();var root=f.transform.TransformPoint(bottom.Aggregate(Vector3.zero,(a,v)=>a+v)/bottom.Length);
   try{trees.Add((tree,root,Ground(ground,Xz(root)).y));}catch{ }
  }
  var moveObjects=new List<(Transform t,Vector2 goal,float offset)>();
  void Queue(Component c,Vector2 p){moveObjects.Add((c.transform,p,c.transform.position.y-Ground(ground,Xz(c.transform.position)).y));}
  Queue(cap,Arenas[0]);Queue(nail,Arenas[2]);Queue(plane,Arenas[3]);
  var points=Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None);
  var ordered=new[]{points.Single(p=>p.isDefault),points.Single(p=>p.GetComponent<WorldSaveIdentity>()?.id=="island1-approach-checkpoint-0"),points.Single(p=>p.GetComponent<WorldSaveIdentity>()?.id=="island1-approach-checkpoint-1"),points.Single(p=>p.GetComponent<WorldSaveIdentity>()?.id=="island1-approach-checkpoint-2")};
  for(int i=0;i<ordered.Length;i++)Queue(ordered[i],Checkpoints[i]);
  var caches=Object.FindObjectsByType<IslandSupplyCache>(FindObjectsSortMode.None);
  Queue(caches.Single(c=>c.name=="Ridge Cache"),new Vector2(67,654));Queue(caches.Single(c=>c.name=="North Lookout"),new Vector2(-36,750));
  var rifle=Object.FindObjectsByType<PlacedEquipmentPickup>(FindObjectsSortMode.None).Single(p=>p.name=="M4A1");Queue(rifle,new Vector2(5,727));
  var ammo=Object.FindObjectsByType<RifleAmmoPickup>(FindObjectsSortMode.None).OrderBy(p=>p.name).ToArray();
  var ammoSites=new[]{new Vector2(1,734),new Vector2(-25,751),new Vector2(-61,780),new Vector2(-94,804),new Vector2(-133,823),new Vector2(-105,848)};
  if(ammo.Length!=ammoSites.Length)throw new Exception("Unexpected existing ammo count");for(int i=0;i<ammo.Length;i++)Queue(ammo[i],ammoSites[i]);
  island.landforms=forms.ToArray();island.Rebuild();Physics.SyncTransforms();
  int movedTrees=0,regrounded=0;var placements=new List<Vector2>();
  foreach(var item in trees){
   var p=Xz(item.root);var original=p;
   for(int i=0;i<Arenas.Length;i++)if(Vector2.Distance(p,Arenas[i])<ArenaRadii[i]+7){var d=(p-Arenas[i]).normalized;if(d.sqrMagnitude<.1f)d=Vector2.right;p=Arenas[i]+d*(ArenaRadii[i]+17+placements.Count%5*2);}
   var onRoute=NearestRoute(p);if(Vector2.Distance(p,onRoute)<8){var d=(p-onRoute).normalized;if(d.sqrMagnitude<.1f)d=Vector2.right;p=onRoute+d*13;}
   // Keep relocated stems separate; untouched stands retain their spacing.
   if(p!=original)for(int attempt=0;attempt<12&&placements.Any(x=>Vector2.Distance(x,p)<8);attempt++)p+=new Vector2(3,2);
   float height=Ground(ground,p).y;var delta=new Vector3(p.x-original.x,height-item.oldGround,p.y-original.y);
   if(delta.sqrMagnitude>.0001f){Undo.RecordObject(item.go.transform,"Seat route woodland");item.go.transform.position+=delta;PrefabUtility.RecordPrefabInstancePropertyModifications(item.go.transform);regrounded++;if(p!=original)movedTrees++;}
   placements.Add(p);
  }
  foreach(var move in moveObjects){Undo.RecordObject(move.t,"Reflow island route");move.t.position=Ground(ground,move.goal)+Vector3.up*move.offset;PrefabUtility.RecordPrefabInstancePropertyModifications(move.t);}
  flock.transform.position=Ground(ground,Arenas[1]);flock.placeBesideCappuccino=false;flock.triggerRadius=40;
  // The default remains the first checkpoint; all later points still require a visit.
  for(int i=0;i<ordered.Length;i++){
   var p=ordered[i];p.pointName=Names[i];p.gameObject.name=Names[i];p.activationRadius=5;p.transform.position=Ground(ground,Checkpoints[i]);
   p.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(Ground(ground,Arenas[i])-p.transform.position,Vector3.up));
   foreach(Transform marker in p.transform){var at=marker.position;at.y=Ground(ground,Xz(at)).y+.2f+marker.GetSiblingIndex()*.25f;marker.position=at;}
   EditorUtility.SetDirty(p);
  }
  // Move the existing cover group with the aircraft (it may be empty in this scene).
  var cover=enemies.GetRootGameObjects().SingleOrDefault(g=>g.name=="Bombardino encounter cover");if(cover&&cover.transform.childCount>0)cover.transform.position+=new Vector3(Arenas[3].x-108.6f,0,Arenas[3].y-635.9f);
  var settings=AssetDatabase.LoadAssetAtPath<IslandEcologySettings>("Assets/Generated/IslandEcology074/Settings.asset");
  if(!File.Exists(W+"/ecology-settings-before.json"))File.WriteAllText(W+"/ecology-settings-before.json",EditorJsonUtility.ToJson(settings,true));
  var keep=settings.clearings.Where(c=>c.reason.StartsWith("Arrival")||c.reason.StartsWith("Authored player")||c.reason.StartsWith("Story stick")||c.reason.StartsWith("Trigger: Sahur")||c.reason.StartsWith("Trigger: Stick")||c.reason.StartsWith("Shore supply")).ToList();
  for(int i=0;i<Arenas.Length;i++){keep.Add(new IslandEcologySettings.Clearing{reason=Names[i]+" combat clearing",center=Arenas[i],radius=ArenaRadii[i]});keep.Add(new IslandEcologySettings.Clearing{reason=Names[i]+" checkpoint",center=Checkpoints[i],radius=8});}
  foreach(var c in caches)keep.Add(new IslandEcologySettings.Clearing{reason=c.name+" supply",center=Xz(c.transform.position),radius=8});
  keep.Add(new IslandEcologySettings.Clearing{reason="Post-Nailong rifle",center=Xz(rifle.transform.position),radius=6});
  settings.clearings=keep.ToArray();settings.corridors=new[]{new IslandEcologySettings.Corridor{reason="Arrival - Cappuccino - flock - Nailong - aircraft",points=Route,halfWidth=5},new IslandEcologySettings.Corridor{reason="Nailong reward and ammunition",points=new[]{Arenas[2],new Vector2(5,727),new Vector2(-25,751),Checkpoints[3]},halfWidth=4}};
  EditorUtility.SetDirty(settings);EditorUtility.SetDirty(island);EditorUtility.SetDirty(flock);EditorUtility.SetDirty(flock.transform);
  AssetDatabase.SaveAssets();foreach(var s in new[]{env,enemies,gameplay}){EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed: "+s.name);}
  var after=ground.sharedMesh.vertices;float maxDelta=0;int changed=0;for(int i=0;i<before.Length;i++){float d=Mathf.Abs(before[i].y-after[i].y);maxDelta=Mathf.Max(maxDelta,d);if(d>.001f)changed++;}
  var report=new{arenas=Arenas.Select((p,i)=>new{name=Names[i],position=Ground(ground,p).ToString("F2")}),checkpoints=ordered.Select(p=>new{p.pointName,p.isDefault,p=p.transform.position.ToString("F2")}),landforms=forms.Count,changedVertices=changed,maxHeightChange=maxDelta,movedTrees,regrounded,existingAmmoMoved=ammo.Length,newEnemies=0};
  File.WriteAllText(W+"/layout.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static object ValidateAndSave(){
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var island=Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None).Single(i=>i.isActiveAndEnabled&&i.gameObject.scene.name=="Environment");var ground=island.GetComponent<MeshCollider>();Physics.SyncTransforms();
  int samples=0,missing=0,blocked=0;float maxSlope=0;var obstacles=new HashSet<string>();
  for(int i=1;i<Route.Length;i++){
   int count=Mathf.CeilToInt(Vector2.Distance(Route[i-1],Route[i])/2);
   for(int j=0;j<=count;j++){
    var p=Vector2.Lerp(Route[i-1],Route[i],j/(float)count);samples++;
    if(!ground.Raycast(new Ray(new Vector3(p.x,200,p.y),Vector3.down),out var hit,400)){missing++;continue;}
    maxSlope=Mathf.Max(maxSlope,Vector3.Angle(hit.normal,Vector3.up));
    foreach(var c in Physics.OverlapCapsule(hit.point+Vector3.up,hit.point+Vector3.up*3,.6f,~0,QueryTriggerInteraction.Ignore)){
     if(c==ground||CombatBodyCollision.IsActor(c)||c.GetComponentInParent<IslandSupplyCache>())continue;
     obstacles.Add(c.name);blocked++;
    }
   }
  }
  var points=Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None);
  var first=points.Single(p=>p.isDefault);var source=points.First(p=>p.transform.childCount==3);
  if(first.transform.childCount==0)foreach(Transform child in source.transform){var clone=Object.Instantiate(child.gameObject,first.transform,false);clone.name=child.name;clone.transform.localPosition=child.localPosition;var p=clone.transform.position;p.y=Ground(ground,Xz(p)).y+.2f+clone.transform.GetSiblingIndex()*.25f;clone.transform.position=p;}
  var enemies=Object.FindObjectsByType<ChickenBroAI>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var cp=points.OrderBy(p=>p.transform.position.z).Select((p,i)=>new{p.pointName,distanceToEncounter=Vector2.Distance(Xz(p.transform.position),Arenas[i]),groundError=Mathf.Abs(p.transform.position.y-Ground(ground,Xz(p.transform.position)).y)}).ToArray();
  var report=new{passed=missing==0&&blocked==0&&maxSlope<30&&enemies.Length==0&&cp.All(p=>p.groundError<.04f),samples,missing,blocked,maxSlope,obstacles,authoredRegularEnemies=enemies.Length,checkpoints=cp,flockWaves=Object.FindFirstObjectByType<ChickenBroEncounter>().waveCounts};
  File.WriteAllText(W+"/route-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  Undo.FlushUndoRecordObjects();foreach(var name in new[]{"Environment","Enemies","Gameplay"}){var s=SceneManager.GetSceneByName(name);EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed: "+name);}
  return report;
 }
 public static object PrepareTest(){
  if(Application.isPlaying)throw new Exception("Stop play first");
  if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save owned scene changes first");
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity");EditorSceneManager.OpenScene("Assets/Scenes/First Island/Lighting.unity",OpenSceneMode.Additive);return "Isolated environment for disposable route test";
 }
 public static object Restore(){
  if(Application.isPlaying)throw new Exception("Stop test first");
  typeof(GameSaveManager).GetField("editorSavePath",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,null);
  foreach(var f in Directory.GetFiles(W+"/save-before"))if(!File.ReadAllBytes(f).SequenceEqual(File.ReadAllBytes(Path.Combine(Application.persistentDataPath,Path.GetFileName(f)))))throw new Exception("Player save changed");
  EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(W+"/setup.json")));return new{restored=true,saveUnchanged=true};
 }
}
