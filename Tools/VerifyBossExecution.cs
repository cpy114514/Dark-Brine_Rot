using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;
public sealed class ExecutionProbeComponent:MonoBehaviour{}
public static class VerifyBossExecution
{
 public static object Run()
 {
  
  var previous=SceneManager.GetActiveScene();var scene=Application.isPlaying?SceneManager.CreateScene("Temporary execution verification"):UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  int checks=0;void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
  try
  {
   var actor=new GameObject("Execution owner fixture");var owner=actor.AddComponent<SahurExecution>();
   var root=new GameObject("Configured boss fixture");var hp=root.AddComponent<Health>();var e=root.AddComponent<BossExecution>();
   var attack=root.AddComponent<ExecutionProbeComponent>();var box=root.AddComponent<BoxCollider>();box.isTrigger=true;
   e.combatBehaviours=new Behaviour[]{attack};e.attackColliders=new Collider[]{box};hp.maxHealth=100;hp.currentHealth=100;
   int deaths=0;hp.OnDeath=new UnityEngine.Events.UnityEvent();hp.OnDeath.AddListener(()=>deaths++);
   hp.ApplyDamage(100,Vector3.zero);
   Check(hp.currentHealth==0&&!hp.IsDead&&e.IsPending&&deaths==0,"zero health must defer death");
   Check(!attack.enabled&&!box.enabled,"suspend attacks");
   hp.ApplyDamage(500,Vector3.zero);hp.Heal(50);Check(hp.currentHealth==0&&deaths==0,"no extra damage or healing while pending");
   Check(e.Claim(owner)&&!e.Claim(owner),"exclusive ownership");e.Release(owner);Check(e.IsPending&&deaths==0,"cancel returns pending");
   e.ResetEncounter(true);Check(hp.currentHealth==100&&!hp.IsDead&&attack.enabled&&box.enabled,"reset restores encounter");
   hp.ApplyDamage(100,Vector3.zero);Check(e.Claim(owner),"claim retry");Check(e.Complete(owner)&&hp.IsDead&&deaths==1,"complete death once");
   Check(!e.Complete(owner)&&deaths==1&&!box.enabled,"duplicate complete blocked");
   var story=new GameObject("Configured scripted ending");var sh=story.AddComponent<Health>();var se=story.AddComponent<BossExecution>();se.ending=BossExecution.Ending.Scripted;int sd=0;sh.OnDeath=new UnityEngine.Events.UnityEvent();sh.OnDeath.AddListener(()=>sd++);sh.ApplyDamage(1000,Vector3.zero);
   Check(sh.IsDead&&!se.HoldsDeath&&sd==1,"scripted ending remains direct");
   var mob=new GameObject("Ordinary creature fixture").AddComponent<Health>();mob.ApplyDamage(1000,Vector3.zero);Check(mob.IsDead,"unconfigured mob direct death");
   var nr=new GameObject("Alternate health fixture");var nh=nr.AddComponent<NailongHealth>();var loot=nr.GetComponent<NailongLoot>();if(loot)UnityEngine.Object.DestroyImmediate(loot);
   var ne=nr.AddComponent<BossExecution>();int nd=0;nh.OnDeath.AddListener(()=>nd++);nh.ApplyDamage(1000,Vector3.zero);
   Check(nh.currentHealth==0&&!nh.IsDead&&ne.IsPending&&nd==0,"alternate health pending");ne.Claim(owner);ne.Complete(owner);Check(nh.IsDead&&nd==1,"alternate health final death");
   var result=new{passed=true,checks};File.WriteAllText(".codex/boss-execution-20261107/health-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
  }
  finally
  {
   foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
   SceneManager.SetActiveScene(previous);if(Application.isPlaying)SceneManager.UnloadSceneAsync(scene);else UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
  }
 }
}

