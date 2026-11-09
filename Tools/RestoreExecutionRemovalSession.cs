using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Mavis;
public static class RestoreExecutionRemovalSession
{
 public static object Run()
 {
  const string dir=".codex/execution-removal-20261108/";
  if(File.Exists(dir+"session-restored.json"))throw new Exception("Already restored; do not replay an old snapshot.");
  if(!Application.isPlaying)throw new Exception("The original running session is required.");
  var root=JObject.Parse(File.ReadAllText(dir+"before-runtime.json"));
  var baseline=JsonUtility.FromJson<GameWorldProgress.Snapshot>((string)root["player"]["progress"]);
  var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var changes=(Dictionary<string,int>)typeof(GameWorldProgress).GetField("changes",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
  int restoredChanges=0,restoredItems=0;
  foreach(var change in baseline.changes)if(!changes.ContainsKey(change.id)){changes[change.id]=change.value;restoredChanges++;}
  var inventory=p.GetComponent<EquipmentInventory>();var items=Resources.LoadAll<EquipmentItem>("Equipment");
  foreach(var stack in baseline.inventory)
  {var item=items.FirstOrDefault(i=>i.id==stack.id);if(!item)throw new Exception("Missing original inventory definition: "+stack.id);if(inventory.Count(item)<stack.count){inventory.RestoreStack(item,stack.count);restoredItems++;}}
  foreach(var hp in UnityEngine.Object.FindObjectsByType<Health>(FindObjectsInactive.Include,FindObjectsSortMode.None))FirstIslandBossProgression.Register(hp);
  foreach(var hp in UnityEngine.Object.FindObjectsByType<NailongHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None))FirstIslandBossProgression.Register(hp);
  RepairRegistry();
  var after=GameWorldProgress.Capture(p);
  bool sameChanges=baseline.changes.All(e=>after.changes.Any(a=>a.id==e.id&&a.value==e.value));
  bool sameInventory=baseline.inventory.All(e=>after.inventory.Any(a=>a.id==e.id&&a.count>=e.count));
  var result=new{passed=sameChanges&&sameInventory,restoredChanges,restoredItems,position=p.transform.position.ToString(),health=p.GetComponent<PlayerHealth>().currentHealth,weapon=p.GetComponent<SahurWeaponLoadout>().CurrentWeapon.ToString(),progress=JsonUtility.ToJson(after),capriDefeated=FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Capri)};
  File.WriteAllText(dir+"session-restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 public static object RepairRegistry()
 {
  const string dir=".codex/execution-removal-20261108/";
  var root=JObject.Parse(File.ReadAllText(dir+"before-runtime.json"));var baseline=JsonUtility.FromJson<GameWorldProgress.Snapshot>((string)root["player"]["progress"]);
  var registry=(HashSet<string>[])typeof(FirstIslandBossProgression).GetField("identities",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
  // A defeated boss may already have despawned, so Register(live objects) alone
  // cannot rebuild its identity after Unity's assembly reload.
  foreach(var b in baseline.tutorial.bosses)if(b!=null&&!string.IsNullOrEmpty(b.bossId)&&(int)b.kind>=0&&(int)b.kind<registry.Length)registry[(int)b.kind].Add(b.bossId);
  bool capri=FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Capri);
  var result=new{capriDefeated=capri,expected=baseline.tutorial.bosses.Any(b=>b!=null&&b.kind==FirstIslandObjectives.BossKind.Capri&&b.defeated)};
  File.WriteAllText(dir+"encounter-registry-restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));if(result.expected&&!capri)throw new Exception("Original boss prerequisite not restored");return result;
 }
}
