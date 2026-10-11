if(Application.isPlaying)throw new System.Exception("Stop Play Mode first.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
if(!scene.IsValid()||!scene.isLoaded||scene.isDirty)throw new System.Exception("Environment must be loaded and clean before repair.");
var clouds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("Cloud Entity ")&&t.parent!=null&&t.parent.name=="Celestial Cloudscape"&&t.childCount==0&&t.GetComponent<MeshFilter>()!=null&&t.GetComponent<MeshFilter>().sharedMesh==null&&t.GetComponent<MeshRenderer>()!=null&&t.GetComponent<MeshRenderer>().sharedMaterials.All(m=>m==null)&&t.GetComponents<Component>().All(c=>c is Transform||c is MeshFilter||c is MeshRenderer)).Select(t=>t.gameObject).ToArray();
var roots=scene.GetRootGameObjects().Where(g=>g.name.StartsWith("20260917")||g.name.StartsWith("20260918")).Where(g=>g.transform.childCount==0&&g.GetComponent<MeshFilter>()!=null&&g.GetComponent<MeshFilter>().sharedMesh==null&&g.GetComponent<MeshRenderer>()!=null&&g.GetComponent<MeshRenderer>().sharedMaterials.All(m=>m==null)).ToArray();
var components=roots.SelectMany(g=>g.GetComponents<Component>().Where(c=>c is MeshFilter||c is MeshRenderer||c.GetType().FullName=="KS.SceneFusion.Client.Unity.sfMissingPrefab")).Concat(clouds.SelectMany(g=>g.GetComponents<Component>().Where(c=>c is MeshFilter||c is MeshRenderer))).ToArray();
// Keep the empty transforms: Scene Fusion's GUID registry still identifies them.
var candidates=new System.Collections.Generic.HashSet<UnityEngine.Object>(components);
var references=new System.Collections.Generic.List<string>();
for(int s=0;s<UnityEngine.SceneManagement.SceneManager.sceneCount;s++)foreach(var root in UnityEngine.SceneManagement.SceneManager.GetSceneAt(s).GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true)){
 if(b==null||candidates.Contains(b))continue;var so=new UnityEditor.SerializedObject(b);var it=so.GetIterator();while(it.Next(true))if(it.propertyType==UnityEditor.SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null&&candidates.Contains(it.objectReferenceValue))references.Add(b.name+"/"+it.propertyPath);
}
if(references.Count>0)throw new System.Exception("Referenced orphan components require inspection: "+string.Join(",",references));
var manifest=new {clouds=clouds.Select(g=>new{g.name,parent=g.transform.parent.name,position=g.transform.localPosition.ToString("R"),rotation=g.transform.localRotation.ToString("R"),scale=g.transform.localScale.ToString("R")}),emptyRoots=roots.Select(g=>new{g.name,position=g.transform.position.ToString("R"),components=g.GetComponents<Component>().Where(c=>!(c is Transform)).Select(c=>new{type=c.GetType().FullName,data=UnityEditor.EditorJsonUtility.ToJson(c)})})};
System.IO.File.WriteAllText("ProjectRepairBackups/IslandAudit-20261008/orphans-manifest.json",Newtonsoft.Json.JsonConvert.SerializeObject(manifest,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Repair island obsolete empty renderers");
foreach(var c in components)UnityEditor.Undo.DestroyObjectImmediate(c);
UnityEditor.Undo.CollapseUndoOperations(group);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{cleanedEmptyClouds=clouds.Length,retainedEmptyRootTransforms=roots.Length,removedInvalidComponents=components.Length,sceneSaved=!scene.isDirty};
