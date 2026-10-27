using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mavis;
public static class VerifyBossOrderV259
{
 const string Root=".codex/boss-order-v259";
 public static object Inspect()
 {
  var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
  var result=new{playing=Application.isPlaying,scenes=scenes.Select(s=>new{s.path,s.isDirty}).ToArray(),bosses=UnityEngine.Object.FindObjectsByType<WorldSaveIdentity>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(i=>FirstIslandBossProgression.TryGetKind(i,out _)).Select(i=>new{i.name,i.id,scene=i.gameObject.scene.path,active=i.gameObject.activeSelf}).ToArray()};
  File.WriteAllText(Root+"/native-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 public static object OpenTest()
 {
  if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve current scene work");
  if(!File.Exists(Root+"/native-before.json"))Inspect();
  // Native enemy scene retains the real three stable identities. An unsaved
  // empty companion scene keeps this verification isolated from the campaign.
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  EditorSceneManager.OpenScene("Assets/Scenes/First Island/Enemies.unity",OpenSceneMode.Additive);
  EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static object Reopen()
 {
  if(Application.isPlaying)throw new Exception("Stop the review first");
  var state=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/native-before.json"));
  var paths=state["scenes"].Select(s=>(string)s["path"]).Where(p=>!string.IsNullOrEmpty(p)).ToArray();
  for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
  return new{reopened=paths};
 }
 public static async Task<object> ReloadCheckpoint()
 {
  if(!Application.isPlaying)throw new Exception("A fresh Play session is required");
  var checks=new List<string>();void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
  const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");
  var holder=new GameObject("Temporary checkpoint reload verification");holder.SetActive(false);SceneManager.MoveGameObjectToScene(holder,scene);
  try
  {
   var snapshot=JsonUtility.FromJson<GameWorldProgress.Snapshot>(File.ReadAllText(Root+"/checkpoint-roundtrip.json"));
   string capId=snapshot.tutorial.bosses.First(b=>b.kind==FirstIslandObjectives.BossKind.Capri).bossId;
   var cap=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),holder.transform);
   cap.transform.localPosition=new Vector3(4000,0,4000);(cap.GetComponent<WorldSaveIdentity>()??cap.AddComponent<WorldSaveIdentity>()).id=capId;
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
   actor.transform.localPosition=new Vector3(4000,100,4000);actor.AddComponent<GameSaveExcluded>();
   var objective=actor.GetComponent<FirstIslandObjectives>()??actor.AddComponent<FirstIslandObjectives>();
   var motor=actor.GetComponent<ThirdPersonPlayerController>();holder.SetActive(true);motor.ExternalMovementLock=true;
   await Task.Delay(150);
   int deaths=0;cap.GetComponent<Health>().OnDeath.AddListener(()=>deaths++);
   var nai=UnityEngine.Object.FindFirstObjectByType<NailongHealth>();var plane=UnityEngine.Object.FindFirstObjectByType<BombardinoBoss>();
   Check(!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong),"Fresh session has no stale unlock cache");
   GameWorldProgress.Restore(snapshot,motor);
   Check(!cap.activeSelf&&deaths==0,"Disk checkpoint hides defeated Cappuccino without replaying death");
   Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Fresh-session disk reload restores only the earned unlock");
   Check(!nai.IsDead&&!plane.IsDead,"Reload keeps the next two bosses alive");
   typeof(FirstIslandObjectives).GetField("nextRefresh",F).SetValue(objective,0f);
   typeof(FirstIslandObjectives).GetMethod("Update",F).Invoke(objective,null);Canvas.ForceUpdateCanvases();
   var label=(UnityEngine.UI.Text)typeof(FirstIslandObjectives).GetField("label",F).GetValue(objective);
   File.WriteAllText(Root+"/objective-after-reload.txt",label.text);
   string bossName=GameLocalization.Text("enemy.name");
   Check(label.text.Contains("Next: "+bossName)&&label.text.Contains("Locked — defeat "+bossName),"Actual objective UI displays the next boss and aircraft prerequisite consistently with the existing localized name");
   Check(label.preferredHeight<=label.rectTransform.rect.height,"Objective text fits the card height");
   var bar=nai.GetComponent<NailongHealthBar>();bar.Refresh(0);
   var name=(UnityEngine.UI.Text)typeof(NailongHealthBar).GetField("label",F).GetValue(bar);
   Check(!name.text.Contains("defeat Cappuccino"),"Unlocked Nailong world label returns to its health display");
   File.WriteAllText(Root+"/objective-after-reload.txt",label.text);
   var result=new{passed=true,checks};File.WriteAllText(Root+"/fresh-session-reload.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  finally{UnityEngine.Object.Destroy(holder);EditorApplication.isPlaying=false;}
 }
 public static async Task<object> Verify()
 {
  if(!Application.isPlaying)throw new Exception("Play mode required");
  var checks=new List<string>();var errors=new List<string>();
  void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add(name);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
  Application.logMessageReceived+=Log;
  const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
  GameObject holder=null;
  try
  {
   GameWorldProgress.Reset();
   var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");
   var nai=UnityEngine.Object.FindFirstObjectByType<NailongHealth>();
   var plane=UnityEngine.Object.FindFirstObjectByType<BombardinoBoss>();
   Check(nai&&plane,"Native Nailong and aircraft scene instances loaded");
   var before=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/native-before.json"));
   string capId=(string)before["bosses"].First(b=>(string)b["name"]=="CappuccinoAssassino")["id"];
   holder=new GameObject("Temporary boss progression verification");holder.SetActive(false);SceneManager.MoveGameObjectToScene(holder,scene);
   var cap=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),holder.transform);
   cap.transform.localPosition=new Vector3(4000,0,4000);
   var identity=cap.GetComponent<WorldSaveIdentity>()??cap.AddComponent<WorldSaveIdentity>();identity.id=capId;
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
   actor.transform.localPosition=nai.transform.position+nai.transform.forward*3;actor.transform.localRotation=Quaternion.identity;
   actor.AddComponent<GameSaveExcluded>();
   var objective=actor.GetComponent<FirstIslandObjectives>()??actor.AddComponent<FirstIslandObjectives>();
   var motor=actor.GetComponent<ThirdPersonPlayerController>();
   var playerHealth=actor.GetComponent<PlayerHealth>()??actor.AddComponent<PlayerHealth>();
   if(!actor.GetComponent<PlayerDeathRespawn>())actor.AddComponent<PlayerDeathRespawn>();
   holder.SetActive(true);
   motor.ExternalControlLock=motor.ExternalMovementLock=true;
   await Task.Delay(150);
   var capHealth=cap.GetComponent<Health>();var planeHealth=plane.GetComponent<Health>();var naiAi=nai.GetComponent<NailongAI>();
   FirstIslandBossProgression.Register(nai);FirstIslandBossProgression.Register(plane);FirstIslandBossProgression.Register(capHealth);
   int capDeaths=0,naiDeaths=0,planeDeaths=0,blockedDamageEvents=0;
   capHealth.OnDeath.AddListener(()=>capDeaths++);nai.OnDeath.AddListener(()=>naiDeaths++);planeHealth.OnDeath.AddListener(()=>planeDeaths++);
   nai.Damaged+=_=>blockedDamageEvents++;planeHealth.OnDamaged.AddListener(_=>blockedDamageEvents++);
   void Refresh()
   {
    typeof(FirstIslandObjectives).GetMethod("RefreshBosses",F).Invoke(objective,null);
   }
   string Text()=>(string)typeof(FirstIslandObjectives).GetMethod("BossText",F).Invoke(objective,new object[]{true});
   objective.Restore(new FirstIslandObjectives.Progress{stage=4,combatLesson=4});Refresh();
   Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Capri)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Fresh campaign unlocks only Cappuccino");
   float nHp=nai.currentHealth,pHp=planeHealth.currentHealth;
   nai.ApplyDamage(100000,nai.transform.position);nai.ApplyCombatDamage(100000,nai.transform.position,CombatHitKind.ChargedHeavy);
   planeHealth.ApplyDamage(100000,plane.transform.position);planeHealth.Kill();
   Check(nai.currentHealth==nHp&&planeHealth.currentHealth==pHp&&blockedDamageEvents==0&&naiDeaths==0&&planeDeaths==0,"Locked bosses reject lethal normal, charged and Kill damage without hit/death events");
   typeof(NailongAI).GetField("target",F).SetValue(naiAi,actor.transform);
   typeof(NailongAI).GetMethod("Update",F).Invoke(naiAi,null);
   plane.target=actor.transform;typeof(BombardinoBoss).GetMethod("Acquire",F).Invoke(plane,null);
   Check(!naiAi.FightActive&&!plane.FightActive,"Locked bosses do not engage a present target");
   float h=playerHealth.currentHealth;
   Check(!nai.GetComponent<NailongAttack>().TryHit(actor.transform,10000,10,0,true)&&playerHealth.currentHealth==h,"Locked Nailong cannot deal a direct melee hit");
   Check(objective.RecommendedBossId==capId&&Text().Contains("Locked — defeat Cappuccino"),"Objective recommends Cappuccino even beside locked Nailong and explains prerequisite");
   File.WriteAllText(Root+"/objective-locked.txt",Text());
   capHealth.ApplyDamage(100000,cap.transform.position);Refresh();
   Check(capDeaths==1&&GameWorldProgress.IsDefeated(capId)&&FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Cappuccino death immediately unlocks Nailong only");
   Check(objective.RecommendedBossId==GameWorldProgress.Key(nai),"Objective advances to Nailong");
   typeof(NailongAI).GetMethod("Update",F).Invoke(naiAi,null);
   Check(naiAi.FightActive,"Unlocked Nailong actually engages the nearby player");
   var snapshot=GameWorldProgress.Capture(motor);
   string saved=JsonUtility.ToJson(snapshot);File.WriteAllText(Root+"/checkpoint-roundtrip.json",saved);
   GameWorldProgress.Restore(JsonUtility.FromJson<GameWorldProgress.Snapshot>(saved),motor);
   Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino)&&capDeaths==1,"Serialized checkpoint restore preserves unlock and does not repeat boss death");
   motor.ExternalControlLock=motor.ExternalMovementLock=false;
   playerHealth.ApplyDamage(100000,actor.transform.position);
   var life=actor.GetComponent<PlayerDeathRespawn>();
   Check(life.IsDead&&life.Respawn()&&FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Actual player death and respawn retain boss sequence");
   motor.ExternalControlLock=motor.ExternalMovementLock=true;
   nai.ApplyDamage(10,nai.transform.position);
   Check(nai.currentHealth==nHp-10,"Unlocked Nailong accepts real damage");
   nai.ApplyDamage(100000,nai.transform.position);Refresh();
   Check(naiDeaths==1&&FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Nailong death unlocks aircraft");
   Check(objective.RecommendedBossId==GameWorldProgress.Key(plane),"Objective advances to aircraft");
   var home=(Vector3)typeof(BombardinoBoss).GetField("home",F).GetValue(plane);
   actor.transform.position=home+Vector3.forward*10;plane.target=actor.transform;
   typeof(BombardinoBoss).GetMethod("Acquire",F).Invoke(plane,null);
   Check(plane.FightActive,"Unlocked aircraft actually starts its encounter");
   planeHealth.ApplyDamage(10,plane.transform.position);
   Check(planeHealth.currentHealth==pHp-10,"Unlocked aircraft accepts real damage");
   planeHealth.Kill();Refresh();
   Check(planeDeaths==1&&objective.DefeatedBossCount==3,"Aircraft defeat completes all three encounters");
   snapshot=GameWorldProgress.Capture(motor);saved=JsonUtility.ToJson(snapshot);
   GameWorldProgress.Reset();GameWorldProgress.Restore(JsonUtility.FromJson<GameWorldProgress.Snapshot>(saved),motor);
   Check(FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Capri)&&FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Nailong)&&FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Bombardino)&&capDeaths==1&&naiDeaths==1&&planeDeaths==1,"Completed save roundtrip restores all defeats without replaying rewards");
   var legacy=new GameWorldProgress.Snapshot{tutorial=new FirstIslandObjectives.Progress{stage=4,combatLesson=4}};
   legacy.tutorial.bosses.Add(new FirstIslandObjectives.BossProgress{bossId=capId,kind=FirstIslandObjectives.BossKind.Capri,defeated=true});
   GameWorldProgress.Restore(legacy,motor);
   Check(FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Legacy objective-only Cappuccino defeat migrates without granting aircraft access");
   legacy.tutorial.bosses.Clear();legacy.tutorial.bosses.Add(new FirstIslandObjectives.BossProgress{bossId=GameWorldProgress.Key(nai),kind=FirstIslandObjectives.BossKind.Nailong,defeated=true});
   GameWorldProgress.Restore(legacy,motor);
   Check(FirstIslandBossProgression.IsDefeated(FirstIslandObjectives.BossKind.Nailong)&&!FirstIslandBossProgression.IsUnlocked(FirstIslandObjectives.BossKind.Bombardino),"Legacy out-of-order Nailong defeat is retained but does not bypass Cappuccino");
   var preview=new GameObject("Temporary outside-island preview");SceneManager.MoveGameObjectToScene(preview,SceneManager.GetSceneAt(0));
   Check(FirstIslandBossProgression.CanChallenge(preview.transform,FirstIslandObjectives.BossKind.Bombardino),"Combat previews outside first-island scenes remain unrestricted");UnityEngine.Object.Destroy(preview);
   Check(errors.Count==0,"No runtime errors during progression verification");
   var result=new{passed=true,checks,capDeaths,naiDeaths,planeDeaths,errors};File.WriteAllText(Root+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
  }
  catch(Exception e){File.WriteAllText(Root+"/failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{error=e.ToString(),checks,errors},Newtonsoft.Json.Formatting.Indented));throw;}
  finally{Application.logMessageReceived-=Log;if(holder)UnityEngine.Object.Destroy(holder);EditorApplication.isPlaying=false;}
 }
}
