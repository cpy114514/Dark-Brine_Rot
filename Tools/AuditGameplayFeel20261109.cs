using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;
public static class AuditGameplayFeel20261109
{
 const string Root=".codex/gameplay-feel-20261109/";
 public static object Run()
 {
  Directory.CreateDirectory(Root);
  var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
  var data=new{version=Application.unityVersion,profile=EditorJsonUtility.ToJson(CombatFeelProfile.Default),player=EditorJsonUtility.ToJson(player.GetComponent<ThirdPersonPlayerController>()),body=EditorJsonUtility.ToJson(player.GetComponent<CharacterController>()),controllers=new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}.Select(path=>{
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   return new{path,layers=controller.layers.Select(l=>new{l.name,mask=l.avatarMask?l.avatarMask.name:null,activeBodyParts=l.avatarMask?Enumerable.Range(0,(int)AvatarMaskBodyPart.LastBodyPart).Where(i=>l.avatarMask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i)).Select(i=>((AvatarMaskBodyPart)i).ToString()).ToArray():null,states=l.stateMachine.states.Select(s=>new{s.state.name,speed=s.state.speed,speedParameter=s.state.speedParameter,active=s.state.speedParameterActive,motion=s.state.motion?s.state.motion.name:null,children=s.state.motion is BlendTree tree?tree.children.Select(c=>new{motion=c.motion?c.motion.name:null,c.threshold,c.timeScale,position=c.position.ToString()}).ToArray():null}).ToArray()}).ToArray()};
  }).ToArray()};
  File.WriteAllText(Root+"audit-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
  return new{file=Root+"audit-before.json",profileBuffer=CombatFeelProfile.Default.inputBuffer,profile=AssetDatabase.GetAssetPath(CombatFeelProfile.Default)};
 }
}
