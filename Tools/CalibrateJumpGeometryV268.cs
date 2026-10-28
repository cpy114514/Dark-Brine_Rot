using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
public static class CalibrateJumpGeometryV268 {
 public static async Task<object> Run(){
  CombatFeelPreview.Spawn();await Task.Delay(1200);
  var player=CombatFeelPreview.Player;var animator=player.GetComponent<Mavis.SahurAttack>().animator;
  var skin=player.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First();
  var mesh=new Mesh();skin.BakeMesh(mesh,true);
  var scene=SceneManager.CreateScene("Temporary probe calibration",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
  var go=new GameObject("Temporary probe calibration");SceneManager.MoveGameObjectToScene(go,scene);
  var c=go.AddComponent<MeshCollider>();c.sharedMesh=mesh;c.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);c.transform.localScale=skin.transform.parent?skin.transform.parent.lossyScale:Vector3.one;scene.GetPhysicsScene().Simulate(.0001f);
  Vector3 chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
  bool control=c.Raycast(new Ray(chest-Vector3.right*3,Vector3.right),out var hit,6);
  var result=new{passed=control,skinBounds=skin.bounds,probeBounds=c.bounds,chest,hitPoint=control?hit.point:Vector3.zero};
  string path=".codex/jump-meshy-v268/geometry-calibration-parent-scale.json";
  File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));
  UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);SceneManager.UnloadSceneAsync(scene);CombatFeelPreview.Clear();EditorApplication.isPlaying=false;
  if(!control)throw new Exception("Probe cannot see known torso ray: geometry result is invalid");return new{passed=control};
 }
}
