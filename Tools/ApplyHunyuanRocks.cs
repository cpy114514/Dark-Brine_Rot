using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ApplyHunyuanRocks {
 static Bounds ExactBounds(MeshFilter f){var b=f.sharedMesh.bounds;Bounds res=new Bounds(f.transform.TransformPoint(b.center),Vector3.zero);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)res.Encapsulate(f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));return res;}
 public static object Run(){
  if(Application.isPlaying)throw new Exception("Edit mode required");const string scenePath="Assets/Scenes/First Island/Enemies.unity";var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);if(!scene.isLoaded||scene.isDirty)throw new Exception("Preserve unsaved enemy scene changes");
  string backup=".codex/hunyuan-environment/Enemies-before-hunyuan.unity";if(File.Exists(backup))throw new Exception("Existing backup; inspect before repeat");File.Copy(scenePath,backup);
  const string dir="Assets/Game/Prefabs/Environment/HunyuanCandidates/";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(dir+"rock-clean.fbx");var mat=AssetDatabase.LoadAssetAtPath<Material>(dir+"Hunyuan Rock.mat");if(!prefab||!mat)throw new Exception("Missing candidate");
  var eligible=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene&&r.sharedMaterial&&r.sharedMaterial.name=="Meadow Rocks"&&(r.name=="rock_tallA"||r.name=="rock_tallB")&&r.GetComponent<MeshCollider>()&&!r.GetComponent<LODGroup>()).OrderBy(r=>r.transform.position.x).ThenBy(r=>r.transform.position.z).ToArray();if(eligible.Length<12)throw new Exception("Unexpected rock selection");
  var selected=Enumerable.Range(0,12).Select(i=>eligible[(int)((i+.5f)*eligible.Length/12)]).ToArray();var reports=new System.Collections.Generic.List<object>();
  foreach(var original in selected){
   var target=original.bounds;var collider=original.GetComponent<MeshCollider>();var colliderMesh=collider.sharedMesh;var model=UnityEngine.Object.Instantiate(prefab);model.name="Hunyuan Rock Near LOD";
   model.transform.rotation=Quaternion.Euler(0,original.transform.eulerAngles.y,0)*model.transform.rotation;var renderer=model.GetComponentInChildren<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=original.shadowCastingMode;var filter=renderer.GetComponent<MeshFilter>();var b=ExactBounds(filter);float scale=target.size.y/b.size.y;model.transform.localScale*=scale;b=ExactBounds(filter);model.transform.position+=target.center-b.center;model.transform.SetParent(original.transform,true);
   var group=original.gameObject.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.16f,new Renderer[]{renderer}),new LOD(0f,new Renderer[]{original})});group.RecalculateBounds();
   if(collider.sharedMesh!=colliderMesh)throw new Exception("Unexpected collider change");reports.Add(new{original=original.name,position=original.transform.position.ToString(),colliderPreserved=true,nearTriangles=filter.sharedMesh.triangles.Length/3,farTriangles=original.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3});
  }
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Unable to save Enemies scene; backup retained");var result=new{changed=reports.Count,nearLODThreshold=.16f,farLODThreshold=0f,items=reports,backup,scene=scenePath};File.WriteAllText(".codex/hunyuan-environment/rock-install.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
