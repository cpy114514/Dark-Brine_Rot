using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Mavis;
public static class VerifyStallDiagnostics067
{
 public static async Task<object> Run()
 {
  var go=new GameObject("Owned heartbeat verification");go.hideFlags=HideFlags.HideAndDontSave;var probe=go.AddComponent<RuntimeStallDiagnostics>();probe.enabled=false;
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=probe.GetType();string path=Path.GetFullPath(".codex/island-stall067/heartbeat-check.log");
  if(File.Exists(path))throw new Exception("Preserve prior evidence");type.GetField("path",flags).SetValue(probe,path);type.GetField("monitor",flags).SetValue(probe,1);type.GetField("pulse",flags).SetValue(probe,System.Diagnostics.Stopwatch.GetTimestamp()-System.Diagnostics.Stopwatch.Frequency*6);
  try
  {
   using(RuntimeStallDiagnostics.Measure("verification simulated heartbeat gap"))await Task.Run(()=>type.GetMethod("CheckPulse",flags).Invoke(probe,new object[]{null}));
   string log=File.ReadAllText(path);if(!log.Contains("no heartbeat")||!log.Contains("verification simulated"))throw new Exception("Background diagnostic missing");
   var before=File.ReadAllBytes(path);await Task.Run(()=>type.GetMethod("CheckPulse",flags).Invoke(probe,new object[]{null}));
   if(File.ReadAllBytes(path).Length!=before.Length)throw new Exception("Duplicate heartbeat report");
   type.GetField("monitor",flags).SetValue(probe,0);type.GetField("reported",flags).SetValue(probe,0);await Task.Run(()=>type.GetMethod("CheckPulse",flags).Invoke(probe,new object[]{null}));
   if(File.ReadAllBytes(path).Length!=before.Length)throw new Exception("Inactive session produced false report");
   var result=new{passed=true,backgroundWorkerWorks=true,duplicateSuppressed=true,inactiveSuppressed=true};File.WriteAllText(".codex/island-stall067/heartbeat-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}

