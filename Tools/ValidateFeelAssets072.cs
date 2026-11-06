using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using Mavis;
public static class ValidateFeelAssets072
{
 public static object Run()
 {
  var checks=new List<object>();void Check(bool ok,string name){checks.Add(new{ok,name});if(!ok)throw new Exception(name);}
  var j=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/game-feel072/audit-before.json"));var profile=AssetDatabase.LoadAssetAtPath<CombatFeelProfile>("Assets/Resources/Combat/SahurCombatFeel.asset");
  Check(profile.controlPreset==CombatFeelProfile.ControlPreset.Balanced,"Balanced remains the persisted default");
  Check(new[]{profile.balanced,profile.responsive,profile.weighty}.All(p=>p!=null&&p.acceleration>0&&p.braking>0&&p.turning>0),"All three serialized experiments contain valid independent control tuning");
  foreach(var path in new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
  {
   var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var before=j["controllers"].First(x=>(string)x["name"]==c.name);
   foreach(var oldLayer in before["layers"])
   {
    var layer=c.layers.First(l=>l.name==(string)oldLayer["name"]);
    foreach(var oldState in oldLayer["states"])
    {
     var state=layer.stateMachine.states.First(s=>s.state.name==(string)oldState["name"]).state;
     Check((!state.motion&&(string)oldState["motion"]==null)||(state.motion&&state.motion.name==(string)oldState["motion"]),c.name+" retains authored motion "+layer.name+" / "+state.name);
     Check(Mathf.Abs(state.speed-(float)oldState["speed"])<.0001f,c.name+" retains authored base speed "+state.name);
    }
   }
   var roll=c.layers[0].stateMachine.states.First(s=>s.state.name=="Roll").state;
   Check(roll.speedParameterActive&&roll.speedParameter=="RollPlaybackSpeed"&&c.parameters.Any(p=>p.name=="RollPlaybackSpeed"&&p.defaultFloat==1),c.name+" has the isolated roll playback parameter");
   var foot=c.layers.First(l=>l.name=="Lock-on Footwork");Check(foot.defaultWeight==0&&!foot.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root)&&!foot.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body)&&!foot.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm)&&foot.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg),c.name+" footwork never replaces root, torso or weapon arms");
  }
  var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");Check(player.GetComponent<SahurAttack>().feel==profile&&player.GetComponent<ThirdPersonPlayerController>().useControlPreset,"Actual player prefab references the selected shared control profile");
  Check(player.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Player prefab has no missing script references");
  var result=new{passed=true,checks};File.WriteAllText(".codex/game-feel072/asset-contracts.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{passed=true,checks=checks.Count};
 }
}
