using System;using System.Linq;using UnityEditor;using UnityEditor.Animations;using UnityEngine;using Mavis;
public static class InstallFootwork072
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Stop before asset authoring");
  AnimationClip Clip(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
  var left=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Equipment/M4A1/Animations/RifleStrafeLeft.anim");var right=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Equipment/M4A1/Animations/RifleStrafeRight.anim");var back=Clip("Assets/Game/Prefabs/Characters/Sahur/Animations/Source/Walking Backwards.fbx");
  foreach(var clip in new[]{left,right,back})if(!clip||!clip.isHumanMotion)throw new Exception("Expected retained Humanoid source");
  var paths=new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
  foreach(var path in paths)
  {
   var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(c.layers.Any(l=>l.name=="Lock-on Footwork"))throw new Exception("Already installed");
   foreach(var name in new[]{"LockMoveX","LockMoveY"})if(!c.parameters.Any(p=>p.name==name))c.AddParameter(name,AnimatorControllerParameterType.Float);
   var walk=(BlendTree)c.layers[0].stateMachine.states.First(s=>s.state.name=="Locomotion").state.motion;var children=walk.children;
   var idle=children.OrderBy(x=>x.threshold).First().motion;
   var forward=children.OrderBy(x=>x.threshold).Skip(1).First().motion;
   var mask=new AvatarMask{name="Lock-on legs only 072"};for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
   mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg,true);mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg,true);mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK,true);mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK,true);AssetDatabase.AddObjectToAsset(mask,c);
   var tree=new BlendTree{name="Lock-on directional legs 072",blendType=BlendTreeType.FreeformDirectional2D,blendParameter="LockMoveX",blendParameterY="LockMoveY",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,c);
   tree.AddChild(idle,Vector2.zero);tree.AddChild(forward,Vector2.up);tree.AddChild(back,Vector2.down);tree.AddChild(left,Vector2.left);tree.AddChild(right,Vector2.right);
   var sm=new AnimatorStateMachine{name="Lock-on Footwork"};AssetDatabase.AddObjectToAsset(sm,c);var state=sm.AddState("Directional legs");state.motion=tree;state.iKOnFeet=true;sm.defaultState=state;
   var layer=new AnimatorControllerLayer{name="Lock-on Footwork",defaultWeight=0,blendingMode=AnimatorLayerBlendingMode.Override,avatarMask=mask,stateMachine=sm};c.AddLayer(layer);EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
  }
  var feel=AssetDatabase.LoadAssetAtPath<CombatFeelProfile>("Assets/Resources/Combat/SahurCombatFeel.asset");feel.normal.stop=.045f;feel.finisher.stop=.085f;feel.heavy.stop=.08f;
  var keys=feel.lightSpeed.keys;for(int i=0;i<keys.Length;i++)if(Mathf.Abs(keys[i].time-.88f)<.01f)keys[i].value=1.05f;feel.lightSpeed.keys=keys;EditorUtility.SetDirty(feel);AssetDatabase.SaveAssetIfDirty(feel);
  return new{controllers=3,left=left.name,right=right.name,back=back.name,upperBodyMaskedOut=true};
 }
}
