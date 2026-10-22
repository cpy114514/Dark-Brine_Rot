using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;
public static class InspectTwinBladeRestoreV235 {
 public static object Run(){
 var players=UnityEngine.Object.FindObjectsByType<SahurWeaponLoadout>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(w=>{
 var a=w.GetComponent<SahurAttack>();var m=w.GetComponent<ThirdPersonPlayerController>();var an=a?a.animator:null;
 return new {w.name,active=w.gameObject.activeInHierarchy,weapon=w.CurrentWeapon.ToString(),w.HasTwinBlades,w.twinBladeSizeMultiplier,attack=a&&a.enabled,twins=a&&a.UsesTwinBlades,handoff=a&&a.CinematicHandoffPending,attackActive=a&&a.IsCombatMotionActive,motor=m&&m.enabled,swimming=m&&m.Swimming,control=m&&m.ExternalControlLock,movement=m&&m.ExternalMovementLock,controller=an?AssetDatabase.GetAssetPath(an.runtimeAnimatorController):null,state=an?an.GetCurrentAnimatorStateInfo(0).fullPathHash:0,layerWeights=an?Enumerable.Range(0,an.layerCount).Select(i=>new{name=an.GetLayerName(i),weight=an.GetLayerWeight(i)}).ToArray():null,components=w.GetComponents<Behaviour>().Select(x=>new{type=x.GetType().Name,x.enabled}).ToArray()};}).ToArray();
 var controllers=new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}.Select(p=>{var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(p);return new{path=p,exists=(bool)c,layers=c?c.layers.Select(l=>new{l.name,states=l.stateMachine.states.Where(s=>l.name!="Base Layer"||s.state.name.Contains("Capri")||s.state.name.Contains("Blade")).Select(s=>new{s.state.name,motion=AssetDatabase.GetAssetPath(s.state.motion),s.state.speed}).ToArray()}).ToArray():null};}).ToArray();
 var result=new{playing=Application.isPlaying,paused=EditorApplication.isPaused,players,controllers};Directory.CreateDirectory(".codex/twin-blade-restore-20261021");File.WriteAllText(".codex/twin-blade-restore-20261021/live-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}