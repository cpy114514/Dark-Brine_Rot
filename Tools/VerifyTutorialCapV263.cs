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
public static class VerifyTutorialCapV263 {
 const string Root=".codex/tutorial-cap-v263";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static string Save=>Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
 static object Call(object o,string m,params object[] args)=>o.GetType().GetMethod(m,F).Invoke(o,args);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 public static object Prepare(){
 if(Application.isPlaying)throw new Exception("Stop Play first");
 if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve dirty scenes");
 if(File.Exists(Save))File.Copy(Save,Root+"/save-preserved.json",true);
 string path="Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab";
 var go=PrefabUtility.LoadPrefabContents(path);
 try{var h=go.GetComponent<Health>();float ratio=h.maxHealth>0?Mathf.Clamp01(h.currentHealth/h.maxHealth):1;h.maxHealth=999;h.currentHealth=999*ratio;go.GetComponent<CappuccinoUltimate>().healthMultiplier=1;go.GetComponent<CappuccinoUltimate>().damageMultiplier=1;PrefabUtility.SaveAsPrefabAsset(go,path);}
 finally{PrefabUtility.UnloadPrefabContents(go);}
 EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 EditorApplication.isPlaying=true;return new{starting=true};
 }
 public static async Task<object> Boss(){
 var checks=new List<string>();var damage=new List<object>();var errors=new List<string>();
 void Check(bool c,string n){if(!c)throw new Exception(n);checks.Add(n);}
 void Log(string m,string t,LogType l){if(l==LogType.Error||l==LogType.Exception)errors.Add(m);}
 Application.logMessageReceived+=Log;
 var root=new GameObject("Excluded V263 test actors");root.SetActive(false);
 try{
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(4000,-.5f,4000);floor.transform.localScale=new Vector3(600,1,600);
 var cap=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab"),root.transform);cap.transform.position=new Vector3(4000,0,4000);
 var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);player.transform.position=new Vector3(4000,0,4005);player.AddComponent<GameSaveExcluded>();
 root.SetActive(true);await Task.Delay(100);
 var ai=cap.GetComponent<CappuccinoAI>();var hp=cap.GetComponent<Health>();var ult=cap.GetComponent<CappuccinoUltimate>();var ph=player.GetComponent<PlayerHealth>();var pc=player.GetComponent<ThirdPersonPlayerController>();
 ai.enabled=false;pc.ExternalMovementLock=true;pc.enabled=false;
 Call(ai,"BindTarget",player.transform);Physics.SyncTransforms();
 Check(hp.maxHealth==999&&hp.currentHealth==999,"Native prefab spawns at 999 / 999");
 Check(Mathf.Approximately(ai.AttackFrequencyMultiplier,1),"Initial attack frequency is unchanged");
 hp.currentHealth=400;Check((bool)Call(ult,"TryActivate",10f),"Low health begins existing ultimate animation");
 Call(ult,"Simulate",11f);Check(hp.maxHealth==999&&hp.currentHealth==400&&ai.damage==18,"Charge and empowered phases never multiply health or damage");
 Call(ult,"Simulate",17f);Check(hp.maxHealth==999&&hp.currentHealth==600,"One recovery heals 200 without changing the 999 cap");
 Check(ult.HasHealed&&Mathf.Approximately(ai.AttackFrequencyMultiplier,1.1f),"Healing enables exactly 1.10 timing multiplier");
 Call(ult,"Simulate",21f);Check(ult.CurrentPhase==CappuccinoUltimate.Phase.Spent,"Existing recovery lock ends normally");
 Check(!(bool)Call(ult,"TryActivate",22f),"Recovery does not retrigger in the same encounter");
 Set(ai,"engaged",true);Call(ai,"BeginEncounter",30f);player.transform.position=new Vector3(4000,0,4150);Physics.SyncTransforms();Call(ai,"Tick",.02f,32f);
 Check(ai.FightActive&&ai.CurrentState!=CappuccinoAI.State.Returning,"Boss retains target 150 metres outside the old leash and arena");
 player.transform.position=new Vector3(4000,0,4004);cap.transform.rotation=Quaternion.identity;Physics.SyncTransforms();
 var balance=CombatFeelProfile.Default;
 foreach(CappuccinoAI.AttackKind kind in Enum.GetValues(typeof(CappuccinoAI.AttackKind))){
 ph.currentHealth=100;Set(ph,"recoveryUntil",0f);Set(ph,"protectedUntil",0f);
 typeof(CappuccinoAI).GetProperty("CurrentAttack").SetValue(ai,kind);
 float before=ph.currentHealth;bool resolved=(bool)Call(ai,"TryHitTarget");float lost=before-ph.currentHealth;
 float mult=kind==CappuccinoAI.AttackKind.TwinSlash?.75f:kind==CappuccinoAI.AttackKind.DashThrust?1.15f:1.25f;
 float expected=Mathf.Min(ai.damage*.75f*mult*balance.incomingDamageMultiplier,100*balance.maximumHitHealthFraction);
 Check(resolved&&lost>0&&Mathf.Abs(lost-expected)<.001f,"Real "+kind+" contact applies reduced damage once");damage.Add(new{kind=kind.ToString(),lost,expected});
 }
 Call(ai,"BeginWindup",CappuccinoAI.AttackKind.TwinSlash,40f);float wind=(float)typeof(CappuccinoAI).GetField("windupDuration",F).GetValue(ai);
 Check(Mathf.Abs(wind-ai.telegraphSeconds/1.1f)<.0001f,"Post-heal windup duration uses the 10 percent frequency increase");
 Call(ai,"FinishAttack",41f,1.1f);Check(Mathf.Abs((float)typeof(CappuccinoAI).GetField("actionAt",F).GetValue(ai)-42f)<.0001f,"Post-heal recovery timing uses the same multiplier");
 Call(ult,"ResetEncounter");Check(!ult.HasHealed&&ai.AttackFrequencyMultiplier==1&&hp.maxHealth==999,"Death/reset path clears phase speed while retaining fixed health cap");
 Check(errors.Count==0,"No runtime errors during boss verification");
 var result=new{passed=true,checks,damage,errors};File.WriteAllText(Root+"/boss-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }catch(Exception ex){File.WriteAllText(Root+"/boss-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,checks,error=ex.ToString(),errors},Newtonsoft.Json.Formatting.Indented));throw;}
 finally{Application.logMessageReceived-=Log;UnityEngine.Object.Destroy(root);}
 }
 public static object Record(){
 if(Application.isPlaying)throw new Exception("Stop Play first");
 File.WriteAllText("Temp/opening-review-request.txt",Root+"/opening-final/review.json");File.WriteAllText("Temp/opening-review-helm.flag","true");
 EditorSceneManager.OpenScene("Assets/Scenes/First story/Story1.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;return new{recording=true};
 }
 public static object Restore(){
 if(Application.isPlaying)throw new Exception("Stop Play first");
 var state=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/before.json"));
 string[] paths=state["scenes"].Select(s=>(string)s["path"]).ToArray();
 for(int i=0;i<paths.Length;i++)EditorSceneManager.OpenScene(paths[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
 bool same=File.ReadAllBytes(Save).SequenceEqual(File.ReadAllBytes(Root+"/save-preserved.json"));
 if(!same)throw new Exception("Campaign save changed during isolated tests");
 return new{scenes=paths,saveUnchanged=same};
 }
}

