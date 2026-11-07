using System;using System.IO;using Newtonsoft.Json.Linq;using UnityEngine;
public static class VerifyVfxPreferencesRestored
{
 public static object Run()
 {
  var source=JObject.Parse(File.ReadAllText(".codex/vfx-quality/quality-final/preferences-before.json"));int count=0;
  foreach(var pair in (JObject)source["savedFloats"]){string key="DarkBrine.Settings."+pair.Key;bool exists=pair.Value.Type!=JTokenType.Null;if(PlayerPrefs.HasKey(key)!=exists||exists&&PlayerPrefs.GetFloat(key)!=(float)pair.Value)throw new Exception("Changed float preference: "+key);count++;}
  foreach(var pair in (JObject)source["savedInts"]){string key="DarkBrine.Settings."+pair.Key;bool exists=pair.Value.Type!=JTokenType.Null;if(PlayerPrefs.HasKey(key)!=exists||exists&&PlayerPrefs.GetInt(key)!=(int)pair.Value)throw new Exception("Changed integer preference: "+key);count++;}
  var result=new{preferencesRestored=true,count};File.WriteAllText(".codex/vfx-quality/preferences-restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
 }
}
