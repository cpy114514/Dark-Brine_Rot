using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class VerifyStableJumpV278
{
 public static object Run(){
  var checks=new List<string>();void Check(bool c,string s){if(!c)throw new Exception(s);checks.Add(s);}
  foreach(string path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}){
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   foreach(var pair in new[]{("Jump Slash","Club"),("Capri Jump Slash","Blade")}){
    var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==pair.Item1).state;var clip=state.motion as AnimationClip;
    Check(AssetDatabase.GetAssetPath(clip)=="Assets/Resources/Combat/Combinations/"+pair.Item2+"JumpChopV278.anim",path+" binds "+pair.Item1);
    Check(clip.humanMotion&&Mathf.Abs(clip.length-.85f)<.001f&&clip.frameRate==60&&!clip.isLooping,"Humanoid/timing contract "+path+pair.Item1);
    Check(state.speed==1&&!state.speedParameterActive&&!state.timeParameterActive,"Single clock "+path+pair.Item1);
    var bindings=AnimationUtility.GetCurveBindings(clip);
    Check(bindings.Where(b=>b.propertyName=="RootQ.y"||b.propertyName=="RootQ.z").All(b=>AnimationUtility.GetEditorCurve(clip,b).keys.All(k=>Mathf.Abs(k.value)<.000001f)),"Forward body has no lateral root turn "+path+pair.Item1);
    Check(bindings.Where(b=>b.propertyName.StartsWith("RootT.")).All(b=>AnimationUtility.GetEditorCurve(clip,b).keys.All(k=>Mathf.Abs(k.value-AnimationUtility.GetEditorCurve(clip,b).Evaluate(0))<.000001f)),"Controller owns travel "+path+pair.Item1);
   }
  }
  var result=new{passed=true,checks};File.WriteAllText(".codex/jump-stable-v278/contracts.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{passed=true,checks=checks.Count};
 }
}
