var folder = System.IO.Path.Combine(Application.dataPath, "../ProjectRepairBackups/IslandAudit-20261008");
System.IO.Directory.CreateDirectory(folder);
var problems = new System.Collections.Generic.List<object>();
var renderers = new System.Collections.Generic.List<Renderer>();
var objects = new System.Collections.Generic.List<GameObject>();
var scenes = new System.Collections.Generic.List<object>();
System.Func<GameObject,string> path = go => { var t=go.transform; string p=t.name; while(t.parent!=null){t=t.parent;p=t.name+"/"+p;} return go.scene.name+"/"+p; };
for(int s=0;s<UnityEngine.SceneManagement.SceneManager.sceneCount;s++){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);if(!scene.isLoaded)continue;
 var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();objects.AddRange(all);
 scenes.Add(new {scene.name,scene.path,scene.isDirty,objects=all.Length,active=all.Count(g=>g.activeInHierarchy)});
 foreach(var go in all){
  string p=path(go);int missing=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
  if(missing>0)problems.Add(new {kind="MissingScript",path=p,count=missing});
  var v=go.transform.localScale;if(!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)||v.x==0||v.y==0||v.z==0)problems.Add(new {kind="InvalidScale",path=p,scale=v.ToString()});
  foreach(var b in go.GetComponents<MonoBehaviour>()){
   if(b==null)continue;try{var so=new UnityEditor.SerializedObject(b);var it=so.GetIterator();while(it.Next(true))if(it.propertyType==UnityEditor.SerializedPropertyType.ObjectReference && it.objectReferenceValue==null && it.objectReferenceInstanceIDValue!=0)problems.Add(new {kind="BrokenReference",path=p,component=b.GetType().FullName,property=it.propertyPath});}catch(System.Exception e){problems.Add(new {kind="ScanException",path=p,message=e.Message});}
  }
  foreach(var r in go.GetComponents<Renderer>()){
   renderers.Add(r);var mats=r.sharedMaterials;
   for(int i=0;i<mats.Length;i++){var m=mats[i];if(m==null)problems.Add(new {kind="MissingMaterial",path=p,slot=i,active=go.activeInHierarchy});else if(m.shader==null||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader")problems.Add(new {kind="BrokenShader",path=p,material=m.name,shader=m.shader!=null?m.shader.name:"null"});}
   var sm=r as SkinnedMeshRenderer;if(sm!=null && sm.sharedMesh==null)problems.Add(new {kind="MissingSkinnedMesh",path=p});
  }
  var mf=go.GetComponent<MeshFilter>();if(mf!=null && mf.sharedMesh==null && go.GetComponent<MeshRenderer>()!=null)problems.Add(new {kind="MissingMesh",path=p,active=go.activeInHierarchy});
  foreach(var mc in go.GetComponents<MeshCollider>()){
   if(mc.sharedMesh==null)problems.Add(new {kind="EmptyMeshCollider",path=p,active=go.activeInHierarchy,mc.enabled});
   var rb=mc.attachedRigidbody;if(mc.enabled && go.activeInHierarchy && !mc.convex && rb!=null && !rb.isKinematic)problems.Add(new {kind="DynamicConcaveCollider",path=p});
  }
 }
}
System.Func<Mesh,long> tris = m => m==null?0:Enumerable.Range(0,m.subMeshCount).Sum(i=>(long)m.GetIndexCount(i)/3);
var geometry=renderers.Select(r=>{var f=r.GetComponent<MeshFilter>();var sk=r as SkinnedMeshRenderer;var m=sk!=null?sk.sharedMesh:f!=null?f.sharedMesh:null;return new {path=path(r.gameObject),active=r.gameObject.activeInHierarchy && r.enabled,mesh=m,triangles=tris(m),materials=r.sharedMaterials.Length,shadows=r.shadowCastingMode.ToString(),bounds=r.bounds.size.ToString()};}).ToArray();
var components=objects.SelectMany(o=>o.GetComponents<Component>()).Where(c=>c!=null).GroupBy(c=>c.GetType().FullName).Select(g=>new{type=g.Key,count=g.Count()}).OrderByDescending(g=>g.count).ToArray();
var colliders=objects.SelectMany(o=>o.GetComponents<Collider>()).ToArray();
var textures=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().SelectMany(m=>m.GetTexturePropertyNames().Select(n=>m.GetTexture(n))).Where(t=>t!=null).Distinct().Select(t=>new{t.name,path=UnityEditor.AssetDatabase.GetAssetPath(t),t.width,t.height,bytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)}).OrderByDescending(t=>t.bytes).ToArray();
var report=new {scenes,objectCount=objects.Count,problemCount=problems.Count,problems,components,rendererCount=renderers.Count,activeRenderers=geometry.Count(g=>g.active),activeTriangles=geometry.Where(g=>g.active).Sum(g=>g.triangles),heaviest=geometry.OrderByDescending(g=>g.triangles).Take(40).Select(g=>new{g.path,g.active,mesh=g.mesh!=null?g.mesh.name:"null",g.triangles,g.materials,g.shadows,g.bounds}),meshGroups=geometry.Where(g=>g.mesh!=null).GroupBy(g=>g.mesh).Select(g=>new{mesh=g.Key.name,path=UnityEditor.AssetDatabase.GetAssetPath(g.Key),instances=g.Count(),active=g.Count(x=>x.active),triangles=tris(g.Key),total=g.Where(x=>x.active).Sum(x=>x.triangles)}).OrderByDescending(g=>g.total),colliderCount=colliders.Length,activeColliders=colliders.Count(c=>c.enabled&&c.gameObject.activeInHierarchy),meshColliders=colliders.OfType<MeshCollider>().Select(c=>new{path=path(c.gameObject),active=c.enabled&&c.gameObject.activeInHierarchy,c.convex,triangles=tris(c.sharedMesh)}).OrderByDescending(c=>c.triangles).Take(30),textures,cameras=objects.SelectMany(o=>o.GetComponents<Camera>()).Select(c=>new{path=path(c.gameObject),c.enabled,active=c.isActiveAndEnabled,c.farClipPlane,c.targetDisplay}),listeners=objects.SelectMany(o=>o.GetComponents<AudioListener>()).Select(c=>new{path=path(c.gameObject),active=c.isActiveAndEnabled}),lights=objects.SelectMany(o=>o.GetComponents<Light>()).Select(l=>new{path=path(l.gameObject),l.type,l.shadows,l.lightmapBakeType,active=l.isActiveAndEnabled})};
System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"objects-after.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return new{report=folder,scenes,objectCount=objects.Count,problemCount=problems.Count,problems=problems.Take(40),activeRenderers=geometry.Count(g=>g.active),activeTriangles=geometry.Where(g=>g.active).Sum(g=>g.triangles),heavyMeshGroups=geometry.Where(g=>g.mesh!=null).GroupBy(g=>g.mesh).Select(g=>new{mesh=g.Key.name,instances=g.Count(),triangles=tris(g.Key),total=g.Where(x=>x.active).Sum(x=>x.triangles)}).OrderByDescending(g=>g.total).Take(12)};
