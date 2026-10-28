using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ApplyHunyuanGrass {
 public static object Run(){
  if(Application.isPlaying)throw new Exception("Stop Play before saving a scene");
  const string path="Assets/Scenes/First Island/Environment.unity";var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);if(!scene.isLoaded||scene.isDirty)throw new Exception("Environment must be loaded and clean to preserve edits");
  string backup=".codex/hunyuan-environment/Environment-before-hunyuan.unity";Directory.CreateDirectory(Path.GetDirectoryName(backup));if(File.Exists(backup))throw new Exception("Existing backup; inspect before repeating");File.Copy(path,backup);
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Prefabs/Environment/HunyuanCandidates/Hunyuan Grass Card.mat");var meshes=new[]{"r12_b","r12_c"}.ToDictionary(n=>n,n=>AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Game/Prefabs/Environment/HunyuanCandidates/Hunyuan "+n+".asset"));if(!mat||meshes.Values.Any(m=>!m))throw new Exception("Missing staged assets");
  var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene&&meshes.ContainsKey(r.name)&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh&&r.GetComponent<MeshFilter>().sharedMesh.name==r.name&&r.sharedMaterials.Length==1&&r.transform.childCount==0&&!r.GetComponent<Collider>()).ToArray();
  if(renderers.Length<5000)throw new Exception("Unexpected grass selection; no edits made");
  long before=0,after=0;foreach(var r in renderers){var f=r.GetComponent<MeshFilter>();before+=f.sharedMesh.triangles.Length/3;f.sharedMesh=meshes[r.name];r.sharedMaterial=mat;after+=f.sharedMesh.triangles.Length/3;PrefabUtility.RecordPrefabInstancePropertyModifications(f);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Unable to save Environment scene; backup retained");
  var result=new{changed=renderers.Length,originalTriangles=before,replacementTriangles=after,meshVertices=8,windShader=mat.shader.name,scene=path,backup,transformsAndCollidersChanged=false};File.WriteAllText(".codex/hunyuan-environment/grass-install.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
