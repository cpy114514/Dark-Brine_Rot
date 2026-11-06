using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Mavis;
public static class VerifyBossSnapshot067
{
 public static object Run()
 {
  var type=typeof(RuntimeStallDiagnostics);const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;const BindingFlags statics=BindingFlags.NonPublic|BindingFlags.Static;
  var fields=new[]{type.GetField("capri",statics),type.GetField("nailong",statics),type.GetField("aircraft",statics)};var original=Array.ConvertAll(fields,x=>x.GetValue(null));
  var go=new GameObject("Owned inactive diagnostic snapshot verification");go.SetActive(false);var probe=go.AddComponent<RuntimeStallDiagnostics>();
  try
  {
   RuntimeStallDiagnostics.TrackBoss(UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>(FindObjectsInactive.Include));RuntimeStallDiagnostics.TrackBoss(UnityEngine.Object.FindFirstObjectByType<NailongAI>(FindObjectsInactive.Include));RuntimeStallDiagnostics.TrackBoss(UnityEngine.Object.FindFirstObjectByType<BombardinoBoss>(FindObjectsInactive.Include));
   type.GetMethod("Update",flags).Invoke(probe,null);string snapshot=(string)type.GetField("snapshot",flags).GetValue(probe);
   bool passed=snapshot.Contains("cappuccino=")&&snapshot.Contains("nailong=")&&snapshot.Contains("aircraft=")&&snapshot.Contains("vsync=")&&snapshot.Contains("delta=");
   if(!passed)throw new Exception("Boss diagnostic snapshot incomplete");var result=new{passed,snapshot};File.WriteAllText(".codex/island-stall067/boss-snapshot-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
  }finally{UnityEngine.Object.DestroyImmediate(go);for(int i=0;i<fields.Length;i++)fields[i].SetValue(null,original[i]);}
 }
}
