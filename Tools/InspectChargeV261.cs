using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;
public static class InspectChargeV261
{
 const string Root=".codex/moving-charge-rifle-v261";
 public static object Inspect()
 {
  Directory.CreateDirectory(Root);
  var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;
  object Read(string n)=>p?typeof(ThirdPersonPlayerController).GetField(n,f)?.GetValue(p):null;
  var a=p?p.GetComponent<SahurAttack>():null;var l=p?p.GetComponent<SahurWeaponLoadout>():null;
  var r=new{playing=Application.isPlaying,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).path,SceneManager.GetSceneAt(i).isDirty}).ToArray(),player=p?new{p.name,p.enabled,p.Swimming,p.CanMaintainGroundCharge,p.CanUseGroundAttack,p.ExternalControlLock,p.ExternalMovementLock,state=Read("motionState"),speed=Read("verticalSpeed"),coyote=Read("coyoteTimer"),roll=Read("rollTimer"),flip=Read("airFlipTimer"),attack=a?new{a.enabled,a.IsCharging,a.ChargeElapsed,a.CanMoveDuringCombat,a.UsesTwinBlades}:null,weapon=l?l.CurrentWeapon.ToString():null}:null};
  File.WriteAllText(Root+"/live-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(r,Newtonsoft.Json.Formatting.Indented));return r;
 }
 public static object PreserveAndStop()
 {
  Inspect();string path=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
  if(File.Exists(path))File.Copy(path,Root+"/human-save-before.json",false);
  bool saved=Application.isPlaying&&GameSaveManager.SaveCurrentGame();
  if(File.Exists(path))File.Copy(path,Root+"/human-save-preserved.json",false);
  EditorApplication.isPlaying=false;return new{stopping=true,saved};
 }
 public static object CompareSceneRig()
 {
  if(Application.isPlaying)throw new Exception("Edit mode required");
  var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/First Island/Gameplay.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
  var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab").GetComponent<SahurAttack>();
  var live=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SahurAttack>(true)).First();
  object Details(SahurAttack a)=>new{a.name,clip=a.chargeClip?a.chargeClip.name:null,a.chargeState,a.heavyAttackState,controller=a.animator&&a.animator.runtimeAnimatorController?a.animator.runtimeAnimatorController.name:null,animator=a.animator?a.animator.name:null};
  var r=new{prefab=Details(source),scene=Details(live)};File.WriteAllText(Root+"/rig-compare.json",Newtonsoft.Json.JsonConvert.SerializeObject(r,Newtonsoft.Json.Formatting.Indented));return r;
 }
}
