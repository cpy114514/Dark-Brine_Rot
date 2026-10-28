using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ImportJumpV265 {
 public static object Run(){
 if(Application.isPlaying)throw new Exception("Edit mode");
 foreach(string name in new[]{"Club","Blade"}){
 string id="PLAYER_"+name+"JumpChop_v265",folder="Assets/AnimationStaging/"+id;
 if(!AssetDatabase.IsValidFolder(folder)){
 var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityAnimationImport")).FirstOrDefault(t=>t!=null);if(type==null)throw new Exception("Run the existing Tools/InstallCombatCombinations.cs once to load import helper");
 type.GetMethod("Stage").Invoke(null,new object[]{"ArtSource/Combat/v265/"+id+".fbx","ArtSource/Combat/v265/"+id+".animation.json",folder,"Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx"});
 }
 var clip=AssetDatabase.LoadAllAssetsAtPath(folder+"/"+id+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
 string dest="Assets/Resources/Combat/Combinations/"+name+"JumpChopV265.anim";
 if(!AssetDatabase.LoadMainAssetAtPath(dest)){var copy=UnityEngine.Object.Instantiate(clip);copy.name=id;AssetDatabase.CreateAsset(copy,dest);}
 foreach(string path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}){
 var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var state=c.layers[0].stateMachine.states.Single(s=>s.state.name==(name=="Club"?"Jump Slash":"Capri Jump Slash")).state;state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(dest);EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
 }
 }
 return new{installed=true};
 }
}
