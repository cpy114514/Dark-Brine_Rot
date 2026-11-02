using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
public static class AuditRelease051
{
 public static object Run()
 {
  if(Application.isPlaying||EditorApplication.isCompiling)throw new Exception("Idle edit mode required");
  var original=EditorSceneManager.GetSceneManagerSetup();var results=new List<object>();
  try
  {
   var env=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");if(!env.isLoaded)env=EditorSceneManager.OpenScene("Assets/Scenes/First Island/Environment.unity",OpenSceneMode.Additive);
   foreach(var name in new[]{"ExperienceRegression","PacingExplorationRegression","PriorityFixRegression"})
   {
    var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    try{results.Add(new{name,passed=true,details=type.GetMethod("Run").Invoke(null,null)});}catch(Exception e){results.Add(new{name,passed=false,details=e.ToString()});}
   }
   var plan=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/release051/merge-plan.json"));
   var missing=plan["apply"].Select(t=>(string)t).Where(p=>p.StartsWith("Assets/")&&p.EndsWith(".meta")).Select(p=>p.Substring(0,p.Length-5)).Where(p=>!AssetDatabase.LoadMainAssetAtPath(p)).ToArray();
   results.Add(new{name="ImportedAssets",passed=missing.Length==0,missing});
   var shaderErrors=new List<string>();foreach(var p in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Resources/Combat","Assets/Resources/Guidance"}).Select(AssetDatabase.GUIDToAssetPath)){var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m.shader||ShaderUtil.ShaderHasError(m.shader))shaderErrors.Add(p);}
   results.Add(new{name="CombatGuidanceShaders",passed=shaderErrors.Count==0,shaderErrors});
   var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).ToArray();results.Add(new{name="EightBuildScenes",passed=scenes.Length==8&&scenes.All(s=>AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)),paths=scenes.Select(s=>s.path).ToArray()});
  }
  finally{foreach(var s in Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray())if(!original.Any(o=>o.path==s.path))EditorSceneManager.CloseScene(s,true);var active=original.FirstOrDefault(s=>s.isActive);if(active!=null)SceneManager.SetActiveScene(SceneManager.GetSceneByPath(active.path));}
  bool passed=results.All(x=>(bool)x.GetType().GetProperty("passed").GetValue(x));var report=new{passed,results};File.WriteAllText(".codex/release051/native-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
}
