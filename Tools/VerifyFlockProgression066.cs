using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Mavis;
public static class VerifyFlockProgression066
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stopped editor required");
  var activeField=typeof(ChickenBroEncounter).GetField("<Active>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var old=activeField.GetValue(null);
  var entries=(Dictionary<string,int>)typeof(GameWorldProgress).GetField("changes",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var backup=new Dictionary<string,int>(entries);
  var rows=new List<object>();void Check(bool p,string name){rows.Add(new{passed=p,name});if(!p)throw new Exception(name);}
  var flock=UnityEngine.Object.FindFirstObjectByType<ChickenBroEncounter>();var capri=UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include);var nailong=UnityEngine.Object.FindFirstObjectByType<NailongAI>(FindObjectsInactive.Include);var aircraft=UnityEngine.Object.FindFirstObjectByType<BombardinoBoss>(FindObjectsInactive.Include);
  try
  {
   activeField.SetValue(null,flock);FirstIslandBossProgression.Register(capri);FirstIslandBossProgression.Register(nailong);FirstIslandBossProgression.Register(aircraft);GameWorldProgress.Reset();
   Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Capri)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Fresh progression begins with Cappuccino");
   GameWorldProgress.Record(capri,0);Check(FirstIslandBossProgression.FlockPending&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Flock is required after Cappuccino");
   Check(FirstIslandBossProgression.RequiredBossName(FirstIslandObjectives.BossKind.Nailong)=="Chicken Bro flock","Blocked boss explains the flock prerequisite");
   GameWorldProgress.Record(flock,0);Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Saved flock completion unlocks Nailong");
   GameWorldProgress.Reset();GameWorldProgress.Record(capri,0);GameWorldProgress.Record(nailong,0);
   Check(!FirstIslandBossProgression.FlockPending&&FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Legacy confirmed Nailong defeat never relocks later progress");
   var result=new{passed=true,checks=rows};File.WriteAllText(".codex/freeze066/progression-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }finally{GameWorldProgress.Reset();foreach(var x in backup)entries.Add(x.Key,x.Value);activeField.SetValue(null,old);}
 }
}
