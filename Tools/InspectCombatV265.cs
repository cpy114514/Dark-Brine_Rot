using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
public static class InspectCombatV265 {
 const string R=".codex/combat-v265";
 public static object Stop(){
 bool saved=Application.isPlaying&&GameSaveManager.SaveCurrentGame();
 string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");if(File.Exists(save))File.Copy(save,R+"/human-save-before-stop.json",true);
 var result=new{playing=Application.isPlaying,saved,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new{s.path,s.isDirty}).ToArray(),players=UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>new{p.name,p.enabled,p.Swimming,p.VerticalSpeed,p.CanUseGroundAttack,p.CanMaintainGroundCharge,p.ExternalControlLock,p.ExternalMovementLock,position=p.transform.position.ToString(),motion=typeof(ThirdPersonPlayerController).GetField("motionState",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p),charge=p.GetComponent<SahurAttack>()?.IsCharging,stamina=p.GetComponent<PlayerStamina>()?.currentStamina}).ToArray(),nailong=UnityEngine.Object.FindObjectsByType<NailongAttack>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(n=>new{n.name,n.damage,sonar=n.GetComponent<NailongAI>()?.sonarDamage}).ToArray()};
 File.WriteAllText(R+"/workspace-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));EditorApplication.isPlaying=false;return result;
 }
}
