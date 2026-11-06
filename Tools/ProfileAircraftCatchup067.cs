using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Mavis;
public static class ProfileAircraftCatchup067
{
 public static object Run(string label)
 {
  if(!Application.isPlaying)throw new Exception("Play required");foreach(var p in UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))if(!p.GetComponent<GameSaveExcluded>())p.gameObject.AddComponent<GameSaveExcluded>();
  var owner=UnityEngine.Object.FindFirstObjectByType<BombardinoBoss>();bool enabled=owner.enabled;owner.enabled=false;var rows=new List<object>();
  try
  {
   foreach(float delta in new[]{1f/60f,1f/30f,.10f,.333f})
   {
    for(int repeat=0;repeat<4;repeat++){var watch=System.Diagnostics.Stopwatch.StartNew();owner.Tick(delta);watch.Stop();rows.Add(new{delta,repeat,ms=watch.Elapsed.TotalMilliseconds});}
   }
   var result=new{label,rows};File.WriteAllText(".codex/island-stall067/catchup-"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }finally{owner.enabled=enabled;}
 }
}
