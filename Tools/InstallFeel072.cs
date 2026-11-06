using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using Mavis;
public static class InstallFeel072
{
 public static object Profile()
 {
  if(Application.isPlaying)throw new Exception("Stop before authoring");var feel=AssetDatabase.LoadAssetAtPath<CombatFeelProfile>("Assets/Resources/Combat/SahurCombatFeel.asset");
  feel.useControlTuning=true;feel.controlPreset=CombatFeelProfile.ControlPreset.Balanced;if(feel.balanced==null||feel.responsive==null||feel.weighty==null)throw new Exception("Missing initialized presets");EditorUtility.SetDirty(feel);AssetDatabase.SaveAssetIfDirty(feel);
  const string path="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{root.GetComponent<ThirdPersonPlayerController>().useControlPreset=true;root.GetComponent<SahurAttack>().feel=feel;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  return new{feel.controlPreset,acceleration=feel.balanced.acceleration,braking=feel.balanced.braking};
 }
 public static object DodgePlayback()
 {
  if(Application.isPlaying)throw new Exception("Stop before authoring");
  var paths=new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
  foreach(var path in paths){var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(!c.parameters.Any(p=>p.name=="RollPlaybackSpeed")){c.AddParameter(new AnimatorControllerParameter{name="RollPlaybackSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1});}var state=c.layers[0].stateMachine.states.First(s=>s.state.name=="Roll").state;state.speedParameter="RollPlaybackSpeed";state.speedParameterActive=true;EditorUtility.SetDirty(state);EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(state);AssetDatabase.SaveAssetIfDirty(c);}
  return "Retained roll clips/base speeds, added isolated speed parameter to all three controllers";
 }
}
