using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;
using Mavis;
public static class FinalizeComfortV253
{
 public static object Audit()
 {
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var original=JObject.Parse(File.ReadAllText(".codex/comfort-v253/runtime-before.json"));
  var point=UnityEngine.Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None).First(x=>x.transform.Find("Checkpoint Watchtower"));
  var tower=point.transform.Find("Checkpoint Watchtower");var material=tower.GetComponentInChildren<Renderer>().sharedMaterial;
  var result=new{stopped=!Application.isPlaying,capacity=HealingPackSupply.Total,originalCampaignRestored=HealingPackSupply.Campaign==(string)original["player"]["campaign"]&&HealingPackSupply.Remaining==(int)original["player"]["packs"],buildingMaterialCompiles=!ShaderUtil.ShaderHasError(material.shader),buildingTextureAssigned=material.GetTexture("_BaseMap")!=null,buildingRendererCount=tower.GetComponentsInChildren<Renderer>().Length,buildingColliderCount=tower.GetComponentsInChildren<Collider>().Length,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
  File.WriteAllText(".codex/comfort-v253/final-editor.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 public static object RestoreWorkspace()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Retain open work before restoring the editing workspace");
  var original=JObject.Parse(File.ReadAllText(".codex/comfort-v253/runtime-before.json"));
  var paths=original["scenes"].Select(s=>(string)s["path"]).ToArray();
  for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
  HealingPackSupply.RestoreCampaign((string)original["player"]["campaign"],(int)original["player"]["packs"]);
  var point=UnityEngine.Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None).First(x=>x.transform.Find("Checkpoint Watchtower"));
  Selection.activeGameObject=point.transform.Find("Checkpoint Watchtower").gameObject;
  var scene=point.gameObject.scene;SceneManager.SetActiveScene(scene);AssetDatabase.Refresh();
  return new{restoredCampaign=HealingPackSupply.Campaign,packs=HealingPackSupply.Remaining,scene=scene.path,checkpointCount=point.GetComponentsInChildren<Renderer>().Length,dirty=scene.isDirty};
 }
}
