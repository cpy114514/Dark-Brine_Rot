using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using Mavis;
public static class AuditFeel072{
const string Work=".codex/game-feel072";
public static object Inspect(){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");var motor=prefab.GetComponent<ThirdPersonPlayerController>();var attack=prefab.GetComponent<SahurAttack>();var feel=attack.feel?attack.feel:CombatFeelProfile.Default;
 var path=AssetDatabase.GetAssetPath(feel);if(!File.Exists(Work+"/profile-before.asset"))File.Copy(path,Work+"/profile-before.asset");
 var controllers=new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}.Select(p=>AssetDatabase.LoadAssetAtPath<AnimatorController>(p)).ToArray();
 var result=new{playing=Application.isPlaying,prefab=new{movement=EditorJsonUtility.ToJson(motor),attack=EditorJsonUtility.ToJson(attack),profile=path,feel=EditorJsonUtility.ToJson(feel)},controllers=controllers.Select(c=>new{c.name,layers=c.layers.Select(l=>new{l.name,weight=l.defaultWeight,states=l.stateMachine.states.Select(s=>new{s.state.name,s.state.speed,s.state.speedParameter,s.state.speedParameterActive,motion=s.state.motion?s.state.motion.name:null,length=s.state.motion is AnimationClip clip?clip.length:0,transitions=s.state.transitions.Select(t=>new{t.exitTime,t.duration,t.hasExitTime,to=t.destinationState?t.destinationState.name:null}).ToArray()}).ToArray()}).ToArray()}).ToArray()};
 File.WriteAllText(Work+"/audit-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 return new{file=Work+"/audit-before.json",motor.moveSpeed,motor.acceleration,motor.deceleration,motor.turnSpeedDegrees,motor.rollDuration,motor.rollSpeed,motor.cameraFollowSharpness,feel.lightDodgeCancel,feel.heavyDodgeCancel,feel.inputBuffer,profile=path};
}
}
