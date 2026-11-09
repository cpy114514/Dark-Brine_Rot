using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Mavis;
public static class ConfigureBossExecutions
{
 const string Evidence=".codex/boss-execution-20261107/";
 public static object Run()
 {
  if(Application.isPlaying) throw new InvalidOperationException("Configure prefabs in Edit Mode, without saving runtime state.");
  var prefabs=new[]{
   "Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab",
   "Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab",
   "Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab",
   "Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab"};
  var controllers=new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
  var report=new List<object>();
  foreach(var path in prefabs.Concat(controllers))
  {
   string backup=Evidence+path.Replace('/','_');if(!File.Exists(backup))File.Copy(path,backup);
  }
  foreach(var path in controllers)
  {
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   var machine=controller.layers[0].stateMachine;
   foreach(var pair in new[]{new[]{"Execution Stick","Combo 3"},new[]{"Execution Blades","Capri Combo 3"}})
   {
    if(machine.states.Any(s=>s.state.name==pair[0]))continue;
    var original=machine.states.Single(s=>s.state.name==pair[1]).state;
    var added=machine.AddState(pair[0]);added.motion=original.motion;added.speed=original.speed;added.mirror=original.mirror;
    added.writeDefaultValues=original.writeDefaultValues;added.iKOnFeet=original.iKOnFeet;
    EditorUtility.SetDirty(added);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
   }
  }
  for(int i=0;i<prefabs.Length;i++)
  {
   var root=PrefabUtility.LoadPrefabContents(prefabs[i]);
   try
   {
    if(!root.GetComponent<BossExecution>())
    { Configure(root,i==3);PrefabUtility.SaveAsPrefabAsset(root,prefabs[i]); }
    var e=root.GetComponent<BossExecution>();report.Add(new{path=prefabs[i],ending=e.ending.ToString(),weak=e.vulnerableState,suspended=e.combatBehaviours.Length,colliders=e.attackColliders.Length});
   }
   finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  File.WriteAllText(Evidence+"configuration.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
 }
 static void Configure(GameObject root,bool scripted)
 {
  if(root.GetComponent<NailongHealth>()){if(!root.GetComponent<NailongPhaseAnimation>())root.AddComponent<NailongPhaseAnimation>();if(!root.GetComponent<NailongHitReaction>())root.AddComponent<NailongHitReaction>();}
  var e=root.GetComponent<BossExecution>();if(!e)e=root.AddComponent<BossExecution>();e.ending=scripted?BossExecution.Ending.Scripted:BossExecution.Ending.Execution;
  e.combatBehaviours=root.GetComponentsInChildren<Behaviour>(true).Where(b=>b is CappuccinoAI||b is CappuccinoEnemy||b is CappuccinoUltimate||
   b is NailongAI||b is NailongAttack||b is NailongAttackMotion||b is NailongPhaseAnimation||b is NailongHitReaction||
   b is PigRoninBoss||b is PigRoninBossVisual||b is EnemyAttack).ToArray();
  e.attackColliders=root.GetComponentsInChildren<Collider>(true).Where(c=>c.isTrigger&&!CombatBodyCollision.IsAnatomy(c)).ToArray();
  e.bossAnimator=root.GetComponentInChildren<Animator>(true);
  if(e.bossAnimator)
  {
   var c=e.bossAnimator.runtimeAnimatorController as AnimatorController;
   if(c)
   {
    if(c.layers[0].stateMachine.states.Any(s=>s.state.name=="Stumble"))e.vulnerableState="Base Layer.Stumble";
    else if(c.layers[0].stateMachine.states.Any(s=>s.state.name=="Retreat"))e.vulnerableState="Base Layer.Retreat";
   }
   e.weakVisual=e.bossAnimator.transform!=root.transform?e.bossAnimator.transform:null;
  }
  if(string.IsNullOrEmpty(e.vulnerableState))
  {
   // The ronin uses procedural bones, not a combat Animator state machine.
   e.bossAnimator=null;
   if(!e.weakVisual)e.weakVisual=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>r.rootBone?r.rootBone.parent:r.transform).FirstOrDefault(t=>t&&t!=root.transform);
  }
  e.resetDistance=65;
  EditorUtility.SetDirty(e);
 }
}

