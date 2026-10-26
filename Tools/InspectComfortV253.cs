using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
public static class InspectComfortV253
{
 public static object Run(){var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();var data=new{playing=Application.isPlaying,timeScale=Time.timeScale,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray(),player=p?new{position=new[]{p.transform.position.x,p.transform.position.y,p.transform.position.z},health=p.GetComponent<PlayerHealth>().currentHealth,campaign=HealingPackSupply.Campaign,packs=HealingPackSupply.Remaining}:null,battle=battle?new{active=battle.RedesignActive,stage=battle.RedesignStage.ToString(),lesson=battle.CurrentLesson.ToString()}:null,checkpoints=UnityEngine.Object.FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None).Select(x=>new{name=x.name,position=x.transform.position.ToString(),scene=x.gameObject.scene.path}).ToArray()};File.WriteAllText(".codex/comfort-v253/runtime-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;}
}
