using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;
public static class VerifyExecutionSequenceAssets
{
 public static object Run()
 {
  if(Application.isPlaying)throw new Exception("Exit owned Play session first.");
  var players=new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"}.Select(p=>AssetDatabase.LoadAssetAtPath<AnimatorController>(p)).ToArray();
  var prefabs=new[]{"Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab"};
  int pairs=0,checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
  foreach(string path in prefabs)
  {
   var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var config=root.GetComponent<BossExecution>();Check(config.sequences.Length==3,"three configured weapons");
   Check(config.sequences.Select(s=>s.weapon).Distinct().Count()==3,"unique weapon selection");var controller=config.bossAnimator.runtimeAnimatorController as AnimatorController;
   foreach(var pair in config.sequences)
   {
    pairs++;Check(pair&&pair.playerClip&&pair.bossClip,"paired references");Check(Mathf.Abs(pair.playerClip.length-pair.bossClip.length)<.04f,"paired duration");
    Check(!AnimationUtility.GetAnimationClipSettings(pair.playerClip).loopTime&&!AnimationUtility.GetAnimationClipSettings(pair.bossClip).loopTime,"nonlooping");Check(AnimationUtility.GetAnimationEvents(pair.playerClip).Length==0,"no gameplay damage events");
    Check(pair.contacts.Length>0&&pair.contacts.All(t=>t>0&&t<pair.Duration),"contact schedule inside clip");
    foreach(var player in players)Check(player.layers[0].stateMachine.states.Any(s=>"Base Layer."+s.state.name==pair.playerState&&s.state.motion==pair.playerClip&&s.state.speed==1&&s.state.transitions.Length==0),"player state reference");
    Check(controller.layers[0].stateMachine.states.Any(s=>"Base Layer."+s.state.name==pair.bossState&&s.state.motion==pair.bossClip),"boss state reference");
   }
  }
  var story=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab");Check(story.GetComponent<BossExecution>().ending==BossExecution.Ending.Scripted,"story exclusion");
  typeof(GameSaveManager).GetField("editorSavePath",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,null);
  var result=new{passed=true,checks,pairs,scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray(),active=SceneManager.GetActiveScene().path,temporaryFocus=GameObject.Find("Execution camera focus")!=null};
  File.WriteAllText(".codex/execution-sequences-20261108/final-assets-workspace.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
