using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;
public static class InstallMobileBladeFootwork20261109
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stopped Editor required");
  foreach(string path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
  {
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   var legs=controller.layers.First(l=>l.name=="Lock-on Footwork");
   var original=legs.stateMachine.states.Single().state.motion as BlendTree;
   var locomotion=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Locomotion").state.motion as BlendTree;
   if(!original||!locomotion||!legs.avatarMask)throw new Exception("Missing existing leg animation architecture");
   var idle=locomotion.children[0];var walk=locomotion.children[1];var run=locomotion.children[2];
   var layer=controller.layers.FirstOrDefault(l=>l.name=="Mobile Blade Legs");
   if(layer==null)
   {
    var machine=new AnimatorStateMachine{name="Mobile Blade Legs",hideFlags=HideFlags.HideInHierarchy};AssetDatabase.AddObjectToAsset(machine,controller);
    layer=new AnimatorControllerLayer{name="Mobile Blade Legs",avatarMask=legs.avatarMask,defaultWeight=0,blendingMode=AnimatorLayerBlendingMode.Override,stateMachine=machine};controller.AddLayer(layer);
   }
   var state=layer.stateMachine.states.FirstOrDefault().state;if(!state)state=layer.stateMachine.AddState("Directional walking and running");
   var tree=state.motion as BlendTree;
   if(!tree){tree=new BlendTree{name="Moving blade lower-body directions",hideFlags=HideFlags.HideInHierarchy};AssetDatabase.AddObjectToAsset(tree,controller);state.motion=tree;}
   tree.blendType=BlendTreeType.FreeformDirectional2D;tree.blendParameter="LockMoveX";tree.blendParameterY="LockMoveY";tree.useAutomaticThresholds=false;
   var children=original.children.Select(c=>new ChildMotion{motion=c.motion,position=c.position,timeScale=c.timeScale}).ToList();
   for(int i=0;i<children.Count;i++)if(children[i].position==Vector2.up)children[i]=new ChildMotion{motion=walk.motion,position=Vector2.up,timeScale=walk.timeScale};
   children.Add(new ChildMotion{motion=run.motion,position=Vector2.up*1.75f,timeScale=run.timeScale});
   foreach(var c in original.children.Where(c=>c.position.sqrMagnitude>.5f&&c.position!=Vector2.up))children.Add(new ChildMotion{motion=c.motion,position=c.position*1.75f,timeScale=c.timeScale*1.75f});
   tree.children=children.ToArray();state.writeDefaultValues=true;layer.stateMachine.defaultState=state;
   foreach(var item in controller.layers[0].stateMachine.states.Where(s=>s.state.name=="Capri Combo 1"||s.state.name=="Capri Combo 2"))
   {item.state.speedParameter="LightPlaybackSpeed";item.state.speedParameterActive=true;EditorUtility.SetDirty(item.state);}
   EditorUtility.SetDirty(tree);EditorUtility.SetDirty(state);EditorUtility.SetDirty(layer.stateMachine);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
  }
  var profile=CombatFeelProfile.Default;profile.balanced.groundFollowDistance=profile.responsive.groundFollowDistance=profile.weighty.groundFollowDistance=.6f;
  EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
  return new{controllers=3,legMaskOnly=true,groundFollowDistance=.6f};
 }
}
