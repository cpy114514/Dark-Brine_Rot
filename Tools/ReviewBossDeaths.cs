using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
public static class ReviewBossDeaths
{
 const string Dir=".codex/boss-death-polish-20261108/";
 public static async Task<object> Record(string version="before")
 {
  if(!Application.isPlaying)throw new Exception("Keep the existing Play session running.");Directory.CreateDirectory(Dir+version);
  var scene=SceneManager.CreateScene("Isolated boss death review");var holder=new GameObject("Death review actors");SceneManager.MoveGameObjectToScene(holder,scene);holder.AddComponent<GameSaveExcluded>();
  var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(floor,scene);floor.transform.position=new Vector3(10000,19.5f,10000);floor.transform.localScale=new Vector3(50,1,50);var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.32f,.29f,.23f);floor.GetComponent<Renderer>().sharedMaterial=mat;
  var cameraObject=new GameObject("Isolated death review camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.GetComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.farClipPlane=75;camera.fieldOfView=48;
  var rt=new RenderTexture(960,640,24){antiAliasing=4};rt.Create();camera.targetTexture=rt;
  var rows=new List<object>();var errors=new List<string>();void Log(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(s);}Application.logMessageReceived+=Log;
  try
  {
   foreach(int combination in Enumerable.Range(0,6))
   {
    int kind=combination%3;bool slope=combination>=3;string label=new[]{"Cup","Dragon","Ronin"}[kind]+(slope?"-slope":"-flat");
    floor.transform.rotation=Quaternion.Euler(0,0,slope?10:0);Physics.SyncTransforms();
    var staging=new GameObject("Inactive death staging");staging.transform.SetParent(holder.transform);staging.SetActive(false);
    string path=new[]{"Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab"}[kind];
    var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),staging.transform);actor.AddComponent<GameSaveExcluded>();if(kind==2)actor.transform.localScale=Vector3.one*3;actor.transform.SetPositionAndRotation(new Vector3(10000,22,10000),Quaternion.identity);
    foreach(var agent in actor.GetComponentsInChildren<NavMeshAgent>(true))agent.enabled=false;
    foreach(var b in actor.GetComponentsInChildren<Behaviour>(true))if(b is CappuccinoAI||b is CappuccinoUltimate||b is NailongAI||b is NailongAttack||b is PigRoninBoss)b.enabled=false;
    staging.SetActive(true);await Task.Delay(160);
    foreach(var loot in actor.GetComponents<Component>())if(loot is CappuccinoWeaponLoot||loot is NailongLoot)loot.GetType().GetProperty("HasDropped").SetValue(loot,true);
    var motor=actor.GetComponent<CharacterController>();var collider=floor.GetComponent<Collider>();collider.Raycast(new Ray(new Vector3(10000,80,10000),Vector3.down),out var contact,100);
    var body=motor?(Collider)motor:actor.GetComponents<Collider>().First(c=>c.enabled&&!c.isTrigger);
    float rootToSole=actor.transform.position.y-body.bounds.min.y;
    body.enabled=false;actor.transform.position=new Vector3(10000,contact.point.y+rootToSole+.03f,10000);body.enabled=true;
    await Task.Delay(100);var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>!s.name.ToLowerInvariant().Contains("katana"));var bounds=skin.bounds;float size=Mathf.Max(bounds.size.y,bounds.size.x);Vector3 focus=new Vector3(10000,contact.point.y+size*.38f,10000);camera.transform.SetPositionAndRotation(focus+new Vector3(.7f,.45f,1).normalized*size*1.95f,Quaternion.LookRotation(-new Vector3(.7f,.45f,1).normalized));
    int deaths=0;var health=actor.GetComponent<Health>();var nailong=actor.GetComponent<NailongHealth>();if(health)health.OnDeath.AddListener(()=>deaths++);else nailong.OnDeath.AddListener(()=>deaths++);
    Vector3 point=bounds.center+actor.transform.forward;if(health)health.ApplyDamage(health.currentHealth+1,point);else nailong.ApplyDamage(nailong.currentHealth+1,point);
    var death=actor.GetComponent<EnemyDeathPresentation>();float began=Time.time;int index=0;var frames=new List<object>();Vector3 lastPosition=Vector3.zero;Quaternion lastRotation=Quaternion.identity;float finalDrift=0;
    foreach(float time in new[]{0f,.12f,.28f,.48f,.72f,1.0f,1.3f,1.65f,2.1f,2.7f,3.8f})
    {
     while(Time.time-began<time)await Task.Delay(20);
     string file=label+"-"+(index++).ToString("D2")+".png";Capture(camera,rt,Dir+version+"/"+file);
     var visual=actor.GetComponentInChildren<Animator>().transform;if(time>=2.7f&&lastPosition!=Vector3.zero)finalDrift=Vector3.Distance(lastPosition,visual.position);
     lastPosition=visual.position;lastRotation=visual.rotation;
     var row=new{label,file,age=Time.time-began,deaths,dead=health?health.IsDead:nailong.IsDead,correction=death.GroundCorrection,position=visual.position.ToString(),rotation=visual.eulerAngles.ToString(),geometry=time>=3.8f?Measure(actor,collider):null};rows.Add(row);frames.Add(new{file,time=Time.time-began});
    }
    File.WriteAllText(Dir+version+"/"+label+"-frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(frames,Newtonsoft.Json.Formatting.Indented));
    if(deaths!=1)errors.Add(label+" death event count "+deaths);
    if(actor.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))errors.Add(label+" active corpse collider");
    UnityEngine.Object.DestroyImmediate(staging);
   }
  }
  finally
  {
   Application.logMessageReceived-=Log;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(mat);foreach(var go in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(go);_ = SceneManager.UnloadSceneAsync(scene);
  }
  var result=new{passed=errors.Count==0,rows,errors};File.WriteAllText(Dir+version+"/review.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{result.passed,frames=rows.Count,errors};
 }
 static void Capture(Camera camera,RenderTexture rt,string file)
 {
  var old=RenderTexture.active;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);try{camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(file,tex.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);}
 }
 static object Measure(GameObject actor,Collider floor)
 {
  int samples=0;float gap=float.PositiveInfinity;
  foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
  {
   if(!skin.enabled||skin.name.ToLowerInvariant().Contains("katana"))continue;var mesh=new Mesh();skin.BakeMesh(mesh);var vertices=mesh.vertices;
   for(int i=0;i<vertices.Length;i+=3){var p=skin.transform.position+skin.transform.rotation*vertices[i];if(floor.Raycast(new Ray(p+Vector3.up*20,Vector3.down),out var hit,50)){gap=Mathf.Min(gap,p.y-hit.point.y);samples++;}}
   UnityEngine.Object.DestroyImmediate(mesh);
  }
  return new{samples,minGap=gap};
 }
}
