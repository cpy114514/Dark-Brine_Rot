using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using Mavis;
public static class VerifyExecutionRemoved
{
 public static object Run()
 {
  int checks=0;void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
  foreach(string name in new[]{"Mavis.BossExecution","Mavis.SahurExecution","Mavis.BossExecutionSequence","Mavis.ExecutionTarget"})
   Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetType(name)!=null),"execution type still loaded: "+name);
  Check(!AssetDatabase.IsValidFolder("Assets/Resources/Combat/Executions"),"runtime sequence resources remain");
  foreach(string path in new[]{"Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab","Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab","Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"})
   Check(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Component>(true).All(c=>c),"missing prefab script "+path);
  foreach(string path in new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller","Assets/Game/Prefabs/Bosses/CappuccinoAssassino/Animations/CapriCombat.controller","Assets/Game/Prefabs/Bosses/Nailong/NailongBoss.controller","Assets/Game/Prefabs/Characters/PigRonin/Animations/PigRonin.controller"})
  {
   var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);Check(c.layers.SelectMany(l=>l.stateMachine.states).All(s=>!s.state.name.StartsWith("Execute ")&&!s.state.name.StartsWith("Execution ")),"execution state "+path);
   Check(c.layers.SelectMany(l=>l.stateMachine.states).Where(s=>s.state.name=="Combo 3"||s.state.name=="Capri Combo 3"||s.state.name=="Death").All(s=>s.state.motion),"normal combat/death motion lost");
  }
  var original=SceneManager.GetActiveScene();var scene=Application.isPlaying?SceneManager.CreateScene("Temporary direct death verification"):UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  try
  {
   var root=new GameObject("Direct death Health fixture");var hp=root.AddComponent<Health>();hp.maxHealth=hp.currentHealth=100;int deaths=0,damaged=0;hp.OnDeath=new UnityEngine.Events.UnityEvent();hp.OnDeath.AddListener(()=>deaths++);hp.OnDamaged=new UnityEngine.Events.UnityEvent<float>();hp.OnDamaged.AddListener(x=>damaged++);
   hp.ApplyDamage(30,Vector3.zero);Check(hp.currentHealth==70&&!hp.IsDead&&deaths==0,"nonlethal behavior");hp.ApplyDamage(70,Vector3.zero);Check(hp.IsDead&&hp.currentHealth==0&&deaths==1,"immediate lethal death");hp.ApplyDamage(1000,Vector3.zero);hp.Heal(100);Check(deaths==1&&hp.currentHealth==0&&damaged==2,"no duplicate death or revive");
   var alternate=new GameObject("Direct death alternate health fixture");var nh=alternate.AddComponent<NailongHealth>();var loot=alternate.GetComponent<NailongLoot>();if(loot)UnityEngine.Object.DestroyImmediate(loot);nh.maxHealth=nh.currentHealth=100;int nd=0;nh.OnDeath.AddListener(()=>nd++);nh.ApplyDamage(100,Vector3.zero);Check(nh.IsDead&&nh.currentHealth==0&&nd==1,"alternate immediate lethal death");nh.ApplyDamage(1000,Vector3.zero);Check(nd==1,"alternate death once");
  }
  finally{foreach(var r in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(r);SceneManager.SetActiveScene(original);if(Application.isPlaying)_=SceneManager.UnloadSceneAsync(scene);else UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
  var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  var progress=player?GameWorldProgress.Capture(player):null;
  var result=new{passed=true,checks,playing=Application.isPlaying,player=player?new{position=player.transform.position.ToString(),health=player.GetComponent<PlayerHealth>().currentHealth,weapon=player.GetComponent<SahurWeaponLoadout>().CurrentWeapon.ToString(),progress=JsonUtility.ToJson(progress)}:null,scenes=Enumerable.Range(0,SceneManager.sceneCount).Where(i=>SceneManager.GetSceneAt(i)!=scene).Select(i=>new{path=SceneManager.GetSceneAt(i).path,dirty=SceneManager.GetSceneAt(i).isDirty}).ToArray()};
  File.WriteAllText(".codex/execution-removal-20261108/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
