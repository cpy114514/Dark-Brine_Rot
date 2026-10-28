using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class VerifyJumpContractsV266 {
 public static object Run(){
 var checks=new System.Collections.Generic.List<string>();void Check(bool c,string n){if(!c)throw new Exception(n);checks.Add(n);}
 foreach(var path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}){
 var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
 foreach(var pair in new[]{("Jump Slash","Club"),("Capri Jump Slash","Blade")}){
 var state=c.layers[0].stateMachine.states.Single(s=>s.state.name==pair.Item1).state;var clip=state.motion as AnimationClip;
 Check(AssetDatabase.GetAssetPath(clip)=="Assets/Resources/Combat/Combinations/"+pair.Item2+"JumpChopV266.anim",path+" keeps "+pair.Item1+" on the replacement revision");
 Check(clip.humanMotion&&Mathf.Abs(clip.length-.85f)<.001f&&clip.frameRate==60&&!clip.isLooping,"Retained Humanoid full chop timing "+path+pair.Item1);
 Check(state.speed==1&&!state.speedParameterActive&&!state.timeParameterActive,"One predictable animation clock "+path+pair.Item1);
 }
 var layer=c.layers.Single(l=>l.name=="Twin Blade Recovery");
 Check(layer.defaultWeight==0&&layer.avatarMask!=null&&!layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg)&&!layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg),path+" recovery leaves locomotion legs free");
 Check(layer.stateMachine.states.Single(s=>s.state.name=="Capri Recovery").state.motion==c.layers[0].stateMachine.states.Single(s=>s.state.name=="Capri Heavy").state.motion,path+" recovery uses the existing complete rush clip");
 }
 var result=new{passed=true,checks};File.WriteAllText(".codex/jump-diagonal-v266/controller-contracts.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
