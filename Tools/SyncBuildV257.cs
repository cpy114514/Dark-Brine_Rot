using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
public static class SyncBuildV257
{
 const string Root=".codex/sync-build-20261025/";
 public static object Prepare()
 {
  if(Application.isPlaying)throw new Exception("Preserve the current Play session before updating assets");
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
  if(scenes.Any(s=>s.isDirty))throw new Exception("Save and review dirty scenes before updating");
  string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
  if(File.Exists(save)&&!File.Exists(Root+"human-save-before.json"))File.Copy(save,Root+"human-save-before.json");
  var state=new {scenes=scenes.Select(s=>s.path).ToArray(),active=SceneManager.GetActiveScene().path,campaign=HealingPackSupply.Campaign,packs=HealingPackSupply.Remaining,savePath=save,backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString(),version=PlayerSettings.bundleVersion};
  File.WriteAllText(Root+"editor-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(state,Newtonsoft.Json.Formatting.Indented));
  AssetDatabase.DisallowAutoRefresh();AssetDatabase.StartAssetEditing();return state;
 }
 public static object ResumeImport()
 {
  if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve dirty work before importing");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  AssetDatabase.StopAssetEditing();AssetDatabase.AllowAutoRefresh();AssetDatabase.Refresh();return new {resumed=true};
 }
 public static object Reopen()
 {
  if(Application.isPlaying)throw new Exception("Stopped Editor required");
  var state=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"editor-before.json"));
  var paths=state["scenes"].Select(x=>(string)x).ToArray();
  for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
  var active=SceneManager.GetSceneByPath((string)state["active"]);if(active.IsValid())SceneManager.SetActiveScene(active);
  return new {reopened=true,dirty=Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)};
 }
 public static object Audit()
 {
  var environment=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
  if(!environment.isLoaded)throw new Exception("Load the merged Environment before validation");
  var objects=environment.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
  var checkpoint=objects.FirstOrDefault(t=>t.name=="Checkpoint Watchtower");
  int generated=objects.Sum(t=>t.GetComponents<MeshFilter>().Count(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/Generated/")));
  var missing=objects.Where(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0).Select(t=>t.name).ToArray();
  var pressure=Resources.Load<Material>("Combat/PressureAir");
  var result=new {environmentObjects=objects.Length,checkpointPresent=checkpoint!=null,generatedGrassMeshes=generated,missingScripts=missing,pressureShaderValid=pressure&&!ShaderUtil.ShaderHasError(pressure.shader),scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),windowsSupport=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64)};
  File.WriteAllText(Root+"native-preflight.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  if(!checkpoint||generated==0||missing.Length>0||!result.pressureShaderValid||!result.windowsSupport)throw new Exception("Native release preflight failed; see native-preflight.json");
  return result;
 }
}
