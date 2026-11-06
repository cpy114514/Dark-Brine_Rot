using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class InspectFreeze066
{
 public static object Run()
 {
  var encounter=UnityEngine.Object.FindFirstObjectByType<ChickenBroEncounter>(FindObjectsInactive.Include);
  var capri=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include);
  var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
  var islands=UnityEngine.Object.FindObjectsByType<ProceduralIsland>(FindObjectsSortMode.None);
  var island=islands.OrderByDescending(x=>x.shorelineRadius).First();
  var direction=Vector3.ProjectOnPlane(island.transform.position-capri.transform.position,Vector3.up).normalized;
  var rows=new System.Collections.Generic.List<object>();
  foreach(float distance in Enumerable.Range(0,7).Select(i=>70f+i*12))
  {
   var p=capri.transform.position+direction*distance;var hits=Physics.RaycastAll(p+Vector3.up*40,Vector3.down,80,~0,QueryTriggerInteraction.Ignore).Where(x=>x.collider&&!CombatBodyCollision.IsActor(x.collider)&&x.normal.y>=.6f&&(!ocean||x.point.y>=ocean.oceanHeight+.2f)).OrderBy(x=>x.distance).ToArray();
   rows.Add(new{distance,origin=p.ToString(),hits=hits.Select(x=>new{point=x.point.ToString(),name=x.collider.name}),requiredHeight=(ocean?ocean.oceanHeight:0)+island.beachHeight+3});
  }
  var result=new{encounter=encounter?new{encounter.name,active=encounter.gameObject.activeInHierarchy,tag=encounter.tag,position=encounter.transform.position.ToString(),prefab=encounter.prefab?AssetDatabase.GetAssetPath(encounter.prefab):null,dependencies=AssetDatabase.GetDependencies("Assets/Scenes/First Island/Enemies.unity").Where(x=>x.Contains("ChickenBro")).ToArray()}:null,capri=capri.transform.position.ToString(),island=island.transform.position.ToString(),island.shorelineRadius,island.beachHeight,rows,scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>{var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);return new{s.name,s.isDirty,roots=s.GetRootGameObjects().Select(g=>new{g.name,g.activeSelf,g.tag})};})};
  File.WriteAllText(".codex/freeze066/initial-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
