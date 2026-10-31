using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
public static class SyncVersion033
{
 const string Root=".codex/sync033";
 public static object Apply()
 {
  if(Application.isPlaying||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped idle Editor required");
  var setup=EditorSceneManager.GetSceneManagerSetup();if(setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))throw new Exception("Preserve dirty scenes");
  var plan=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/merge-plan.json"));if(plan["conflicts"].Any())throw new Exception("Resolve conflicts before applying");
  var files=plan["apply"].Select(t=>(string)t).ToArray();string workspace=Path.GetFullPath(".")+Path.DirectorySeparatorChar;
  foreach(var path in files)
  {
   if(!Path.GetFullPath(path).StartsWith(workspace,StringComparison.OrdinalIgnoreCase)||path.Split('/').Contains(".."))throw new Exception("Invalid revision path");
   var before=Root+"/local-before/"+path;
   if(File.Exists(before)){if(!File.Exists(path)||!File.ReadAllBytes(before).SequenceEqual(File.ReadAllBytes(path)))throw new Exception("Preserve newer local file: "+path);}
   else if(File.Exists(path))throw new Exception("Preserve newly created local file: "+path);
  }
  File.WriteAllText(Root+"/editor-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup,Newtonsoft.Json.Formatting.Indented));
  AssetDatabase.DisallowAutoRefresh();AssetDatabase.StartAssetEditing();
  try
  {
   foreach(var path in files){Directory.CreateDirectory(Path.GetDirectoryName(path));File.Copy(Root+"/candidate/"+path,path,false==File.Exists(path)?false:true);}
  }
  finally{AssetDatabase.StopAssetEditing();AssetDatabase.AllowAutoRefresh();}
  AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
  var report=new{applied=files.Length,existingSceneSetupPreserved=true,exactVersionedRevisions=true};File.WriteAllText(Root+"/applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 public static object Audit()
 {
  if(Application.isPlaying||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)throw new Exception("Stopped idle Editor required");
  var paths=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/merge-plan.json"))["apply"].Select(t=>(string)t).Where(p=>p.StartsWith("Assets/")&&p.EndsWith(".meta")).Select(p=>p.Substring(0,p.Length-5)).ToArray();
  var checks=new System.Collections.Generic.List<object>();Action<bool,string> check=(ok,name)=>checks.Add(new{ok,name});
  foreach(var path in paths)check(AssetDatabase.LoadMainAssetAtPath(path)!=null,"Imported native asset: "+path);
  var duck=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/LongLeggedDuck/LongLeggedDuck.prefab").GetComponent<Mavis.ChickenBroAI>();
  var chicken=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab").GetComponent<Mavis.ChickenBroAI>();
  check(chicken.sightRange==28&&chicken.giveUpRange==46,"Chicken authored awareness preserved");check(duck.sightRange==34&&duck.giveUpRange==52,"Duck authored awareness preserved");
  check(EditorBuildSettings.scenes.Count(s=>s.enabled)==8&&EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path=="Assets/Scenes/Second Island.unity"),"Second Island included in8 enabled scenes");
  check(Resources.LoadAll<Mavis.VisualStyles.VisualStylePreset>("VisualStyles/Presets").Length==6,"All6 visual styles retained");
  var setup=EditorSceneManager.GetSceneManagerSetup();var original=Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/editor-setup.json"));check(setup.Select(s=>s.path).SequenceEqual(original.Select(s=>s.path))&&!setup.Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty),"Original clean scene setup retained");
  var report=new{passed=checks.All(x=>(bool)x.GetType().GetProperty("ok").GetValue(x)),count=checks.Count,checks};File.WriteAllText(Root+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{report.passed,report.count};
 }
}
