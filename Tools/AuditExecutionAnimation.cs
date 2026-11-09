using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using Mavis;
public static class AuditExecutionAnimation
{
 public static object Run()
 {
  var paths=new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller","Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab"};
  var data=paths.Select(path=>{
   var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var a=prefab?prefab.GetComponentInChildren<Animator>(true):null;if(!c&&a)c=a.runtimeAnimatorController as AnimatorController;
   return new {path,animator=a?a.name:null,human=a&&a.avatar?a.avatar.isHuman:false,controller=c?AssetDatabase.GetAssetPath(c):null,layers=c?c.layers.Select(l=>new{l.name,states=l.stateMachine.states.Select(s=>new{name=s.state.name,speed=s.state.speed,motion=s.state.motion?s.state.motion.name:null,path=AssetDatabase.GetAssetPath(s.state.motion),length=s.state.motion is AnimationClip clip?clip.length:0,curves=s.state.motion is AnimationClip ac?AnimationUtility.GetCurveBindings(ac).Take(12).Select(b=>new{b.path,b.propertyName,type=b.type.Name}).ToArray():null})}).ToArray():null};
  }).ToArray();
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray();
  var saves=new[]{Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json"),Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json.bak")}.Where(File.Exists).ToDictionary(p=>p,p=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(p))));
  var result=new{data,scenes,saves};File.WriteAllText(".codex/execution-sequences-20261108/baseline.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return data;
 }
}
