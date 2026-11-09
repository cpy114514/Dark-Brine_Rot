using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Mavis;
public static class BossDeathSession
{
 const string Dir=".codex/boss-death-polish-20261108/";
 public static object Capture(string label = "session")
 {
  Directory.CreateDirectory(Dir);if(File.Exists(Dir+label+"-before.json"))throw new Exception("Keep the original snapshot; do not recapture.");
  var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var result=new{playing=Application.isPlaying,position=p.transform.position.ToString(),health=p.GetComponent<PlayerHealth>().currentHealth,weapon=p.GetComponent<SahurWeaponLoadout>().CurrentWeapon.ToString(),progress=JsonUtility.ToJson(GameWorldProgress.Capture(p)),scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
  File.WriteAllText(Dir+label+"-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  var files=new[]{Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json"),Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json.bak")}.Where(File.Exists).ToArray();var saves=files.ToDictionary(f=>f,f=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(f))));
  if(!File.Exists(Dir+"saves-before.json"))
  {File.WriteAllText(Dir+"saves-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(saves));foreach(var f in files)File.Copy(f,Dir+Path.GetFileName(f)+".baseline");}
  return result;
 }
 public static object RestoreCaches(string label = "session")
 {
  if(File.Exists(Dir+label+"-restored.json"))throw new Exception("One-time recovery only; do not replay old progress.");
  var baseline=JObject.Parse(File.ReadAllText(Dir+label+"-before.json"));var snapshot=JsonUtility.FromJson<GameWorldProgress.Snapshot>((string)baseline["progress"]);var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var changes=(Dictionary<string,int>)typeof(GameWorldProgress).GetField("changes",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
  foreach(var c in snapshot.changes)if(!changes.ContainsKey(c.id))changes[c.id]=c.value;
  var inventory=p.GetComponent<EquipmentInventory>();var items=Resources.LoadAll<EquipmentItem>("Equipment");foreach(var stack in snapshot.inventory){var item=items.FirstOrDefault(i=>i.id==stack.id);if(!item)throw new Exception("Missing inventory item "+stack.id);if(inventory.Count(item)<stack.count)inventory.RestoreStack(item,stack.count);}
  foreach(var h in UnityEngine.Object.FindObjectsByType<Health>(FindObjectsInactive.Include,FindObjectsSortMode.None))FirstIslandBossProgression.Register(h);foreach(var h in UnityEngine.Object.FindObjectsByType<NailongHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None))FirstIslandBossProgression.Register(h);
  var ids=(HashSet<string>[])typeof(FirstIslandBossProgression).GetField("identities",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);foreach(var b in snapshot.tutorial.bosses)if(b!=null&&(int)b.kind>=0&&(int)b.kind<ids.Length&&!string.IsNullOrEmpty(b.bossId))ids[(int)b.kind].Add(b.bossId);
  var after=GameWorldProgress.Capture(p);bool passed=snapshot.changes.All(c=>after.changes.Any(a=>a.id==c.id&&a.value==c.value))&&snapshot.inventory.All(c=>after.inventory.Any(a=>a.id==c.id&&a.count>=c.count));
  var result=new{passed,playing=Application.isPlaying,position=p.transform.position.ToString(),health=p.GetComponent<PlayerHealth>().currentHealth,weapon=p.GetComponent<SahurWeaponLoadout>().CurrentWeapon.ToString(),progress=JsonUtility.ToJson(after)};File.WriteAllText(Dir+label+"-restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
