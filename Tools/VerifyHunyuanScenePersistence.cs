using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class VerifyHunyuanScenePersistence {
 public static object Run(){
  if(Application.isPlaying)throw new Exception("Edit mode required");var paths=new[]{"Assets/Scenes/First Island/Environment.unity","Assets/Scenes/First Island/Enemies.unity"};
  foreach(var p in paths){var s=SceneManager.GetSceneByPath(p);if(!s.isLoaded||s.isDirty)throw new Exception("Preserve unsaved scene: "+p);}
  foreach(var p in paths){var s=SceneManager.GetSceneByPath(p);if(!EditorSceneManager.CloseScene(s,true))throw new Exception("Unable to close "+p);EditorSceneManager.OpenScene(p,OpenSceneMode.Additive);}
  var all=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);var grass=all.Where(r=>r.gameObject.scene.path==paths[0]&&(r.name=="r12_b"||r.name=="r12_c")&&r.sharedMaterial&&r.sharedMaterial.name=="Hunyuan Grass Card").ToArray();
  var rocks=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(g=>g.gameObject.scene.path==paths[1]&&g.GetLODs().Length==2&&g.GetLODs()[0].renderers.Any(r=>r&&r.sharedMaterial&&r.sharedMaterial.name=="Hunyuan Rock")).ToArray();
  var checks=new{grass=grass.Length,grassMeshesFourTriangles=grass.All(r=>r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh.triangles.Length==12),grassWindShader=grass.All(r=>r.sharedMaterial.shader.name=="Mavis/FoliageWind"),rocks=rocks.Length,allRockColliders=rocks.All(g=>g.GetComponent<MeshCollider>()&&g.GetComponent<MeshCollider>().sharedMesh),farUsesOriginal=rocks.All(g=>g.GetLODs()[1].renderers.Contains(g.GetComponent<MeshRenderer>())),scenesClean=paths.All(p=>!SceneManager.GetSceneByPath(p).isDirty)};
  File.WriteAllText(".codex/hunyuan-environment/persistence.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));if(grass.Length!=10694||rocks.Length!=12||!checks.grassMeshesFourTriangles||!checks.grassWindShader||!checks.allRockColliders||!checks.farUsesOriginal||!checks.scenesClean)throw new Exception("Saved scene verification failed");return checks;
 }
}
