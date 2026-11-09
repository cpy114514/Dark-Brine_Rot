using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using Mavis;
public static class PlaytestExecutionSequences
{
 const string Dir=".codex/execution-sequences-20261108/";
 public static async Task<object> Run()
 {
  if(!Application.isPlaying)throw new Exception("Start dedicated Play session first");
  var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
  if(!p)throw new Exception("Player unavailable");
  var savePathField=typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static);
  var previousSavePath=savePathField.GetValue(null);
  savePathField.SetValue(null,Path.GetFullPath(Dir+"isolated-save.json"));
  if(!p.GetComponent<GameSaveExcluded>())p.gameObject.AddComponent<GameSaveExcluded>();
  var originalScene=p.gameObject.scene;var previousScene=SceneManager.GetActiveScene();var originalPosition=p.transform.position;var originalRotation=p.transform.rotation;
  var scene=SceneManager.CreateScene("Isolated paired execution test");SceneManager.SetActiveScene(scene);
  var originalKeyboard=Keyboard.current;var keyboard=InputSystem.AddDevice<Keyboard>();float oldScale=Time.timeScale;var rows=new List<object>();var errors=new List<string>();bool failure=false;
  void Log(string m,string t,LogType kind){if(kind==LogType.Exception||kind==LogType.Error)errors.Add(m);}
  Application.logMessageReceived+=Log;
  try
  {
   var arrival=p.GetComponent<FirstIslandArrival>();if(arrival)UnityEngine.Object.DestroyImmediate(arrival);
   var objectives=p.GetComponent<FirstIslandObjectives>();if(objectives)objectives.enabled=false;
   var guidance=p.GetComponent<MainQuestGuidance>();if(guidance)guidance.enabled=false;
   p.enabled=true;p.ExternalControlLock=false;p.ExternalMovementLock=false;p.PrepareRuntime();
   SceneManager.MoveGameObjectToScene(p.gameObject,scene);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Execution test floor";floor.transform.position=new Vector3(10000,19.5f,10000);floor.transform.localScale=new Vector3(50,1,50);
   var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.35f,.30f,.22f);floor.GetComponent<Renderer>().material=mat;
   var cc=p.GetComponent<CharacterController>();var loadout=p.GetComponent<SahurWeaponLoadout>();loadout.Initialize();
   var exec=p.GetComponent<SahurExecution>();if(!exec)exec=p.gameObject.AddComponent<SahurExecution>();
   var hp=p.GetComponent<PlayerHealth>();hp.currentHealth=hp.maxHealth;
   foreach(int combination in Enumerable.Range(0,9))
   {
    int bossIndex=combination/3;var weapon=(SahurWeaponLoadout.Weapon)(combination%3);bool ronin=bossIndex==2;string caseName=new[]{"Cup","Dragon","Ronin"}[bossIndex]+"-"+weapon;hp.currentHealth=hp.maxHealth;p.ExecutionControlLock=false;
    var parent=new GameObject("Inactive test spawn");parent.SetActive(false);
    string prefab=ronin?"Assets/Game/Prefabs/Bosses/PigRonin/Pig Ronin Boss.prefab":bossIndex==1?"Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab":"Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab";
    var boss=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),parent.transform);
    if(ronin)boss.transform.localScale=Vector3.one*3; // Match the existing Second Island scene override.
    boss.transform.position=new Vector3(10000,20,10003);boss.transform.rotation=Quaternion.Euler(0,180,0);
    foreach(var agent in boss.GetComponentsInChildren<NavMeshAgent>(true))agent.enabled=false;
    parent.SetActive(true);await Task.Delay(180);
    var e=boss.GetComponent<BossExecution>();var h=boss.GetComponent<Health>();var n=boss.GetComponent<NailongHealth>();
    var pair=e.SequenceFor(weapon);var collider=e.BodyCollider;if(h)h.ApplyDamage(h.maxHealth*2,boss.transform.position);else n.ApplyDamage(n.maxHealth*2,boss.transform.position);
    float radius=Mathf.Max(collider.bounds.extents.x,collider.bounds.extents.z);
    float own=cc.radius*Mathf.Abs(p.transform.lossyScale.x);
    float foot=(cc.center.y-cc.height*.5f)*p.transform.lossyScale.y;
    cc.enabled=false;p.transform.SetPositionAndRotation(new Vector3(collider.bounds.center.x,20-foot+.02f,collider.bounds.center.z-radius-own-.75f),Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();cc.Move(Vector3.down*.1f);
    loadout.Restore(true,weapon,true);Cursor.lockState=CursorLockMode.Locked;
    await Task.Delay(160);
    if(h)h.ApplyDamage(h.maxHealth*2,boss.transform.position);else n.ApplyDamage(n.maxHealth*2,boss.transform.position);
    int deaths=0;if(h){h.OnDeath??=new UnityEngine.Events.UnityEvent();h.OnDeath.AddListener(()=>deaths++);}else n.OnDeath.AddListener(()=>deaths++);
    await Task.Delay(180);
    p.GetComponent<EnemyLockOn>()?.Clear();
    bool zero=h?h.currentHealth==0:n.currentHealth==0;
    bool pending=e.IsPending&&(h?!h.IsDead:!n.IsDead)&&deaths==0;
    Capture(p,boss,caseName+"-pending");
    ScreenCapture.CaptureScreenshot(Dir+caseName+"-prompt.png");await Task.Delay(120);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));await Task.Delay(100);bool began=exec.IsActive;InputSystem.QueueStateEvent(keyboard,new KeyboardState());bool cancelRestored=true,pauseHeld=true,bossDisableRestored=true;
    if(weapon==SahurWeaponLoadout.Weapon.Stick && began){exec.enabled=false;cancelRestored=e.IsPending&&!p.ExecutionControlLock&&p.ExternalCameraFocus==null&&deaths==0;exec.enabled=true;began=exec.TryBegin(e.HealthComponent);Time.timeScale=0;await Task.Delay(160);pauseHeld=exec.IsActive&&deaths==0;Time.timeScale=1;e.enabled=false;bossDisableRestored=!p.ExecutionControlLock&&p.ExternalCameraFocus==null&&deaths==0;e.enabled=true;if(h)h.ApplyDamage(h.maxHealth*2,boss.transform.position);else n.ApplyDamage(n.maxHealth*2,boss.transform.position);began=exec.TryBegin(e.HealthComponent);}
    float beganAt=Time.realtimeSinceStartup;bool captured=false;float rifleBore=0;var trace=new List<object>();int captureIndex=0;float lastCapture=-1;float actionAge=0;var frames=new List<object>();float firstAction=-1;bool noEarlyDeath=true;string selectedState="";float maximumPhase=0,maxHipOffset=0;bool bossPlayed=false,midCancelled=false,midRestore=true;
    while(exec.IsActive&&Time.realtimeSinceStartup-beganAt<9)
    {
     await Task.Delay(35);if(weapon==SahurWeaponLoadout.Weapon.M4A1)trace.Add(new{active=exec.IsActive,animating=(bool)typeof(SahurExecution).GetField("animating",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(exec),state=e.State.ToString(),pos=p.transform.position.ToString(),goal=typeof(SahurExecution).GetField("alignmentGoal",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(exec).ToString(),narrative=p.NarrativeControlLock,external=p.ExternalMovementLock,scripted=p.Motion&&p.Motion.Scripted,controller=cc.enabled,health=hp.currentHealth,animator=p.CharacterAnimator.isActiveAndEnabled});
     var state=p.CharacterAnimator.GetCurrentAnimatorStateInfo(0);if(state.fullPathHash==Animator.StringToHash(pair.playerState)){if(firstAction<0)firstAction=Time.time;selectedState=pair.playerState;maximumPhase=Mathf.Max(maximumPhase,state.normalizedTime);actionAge=state.normalizedTime*pair.Duration;if(actionAge<pair.Duration-.07f)noEarlyDeath&=deaths==0;}
     if(exec.IsActive&&actionAge>.2f){var hip=p.CharacterAnimator.GetBoneTransform(HumanBodyBones.Hips);maxHipOffset=Mathf.Max(maxHipOffset,Vector3.ProjectOnPlane(hip.position-p.transform.position,Vector3.up).magnitude);bossPlayed|=e.bossAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash==Animator.StringToHash(pair.bossState);}
     if(exec.IsActive&&!midCancelled&&actionAge>.4f&&(bossIndex==0||ronin&&weapon==SahurWeaponLoadout.Weapon.Stick))
     {
      exec.enabled=false;midRestore=e.IsPending&&deaths==0&&!p.ExecutionControlLock&&p.ExternalCameraFocus==null;exec.enabled=true;await Task.Delay(160);
      // Rifle alignment deliberately backs away beyond interaction range.
      // A cancelled player must approach again, as in normal gameplay.
      if(weapon==SahurWeaponLoadout.Weapon.M4A1){cc.Move(Vector3.ProjectOnPlane(boss.transform.position-p.transform.position,Vector3.up).normalized*1.25f);await Task.Delay(100);}
      began=exec.TryBegin(e.HealthComponent);midCancelled=true;firstAction=-1;actionAge=0;frames.Clear();captureIndex=0;lastCapture=-1;
      if(began){Time.timeScale=0;await Task.Delay(120);pauseHeld&=exec.IsActive&&deaths==0;Time.timeScale=1;}
      continue;
     }
     if(exec.IsActive&&Time.time-lastCapture>.13f){string frame=caseName+"-frame-"+captureIndex.ToString("D3");Capture(p,boss,frame);frames.Add(new{file=frame+".png",time=Time.time});captureIndex++;lastCapture=Time.time;}
     if(!captured&&actionAge>=1.2f){Capture(p,boss,caseName+"-action");captured=true;if(weapon==SahurWeaponLoadout.Weapon.M4A1){var gun=loadout.HeldRifle;rifleBore=Vector3.Dot(-gun.right,(e.AimPoint(pair)-gun.TransformPoint(SahurRifleCombat.MuzzleLocal)).normalized);}}
    }
    bool done=e.State==BossExecution.Phase.Completed&&(h?h.IsDead:n.IsDead)&&deaths==1;
    bool restored=!p.ExecutionControlLock&&p.ExternalCameraFocus==null;
    File.WriteAllText(Dir+caseName+"-frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(frames,Newtonsoft.Json.Formatting.Indented));
    rows.Add(new{weapon=caseName,rifleBore,zero,pending,began,done,deaths,restored,heldWeapon=loadout.CurrentWeapon.ToString(),state=e.State.ToString(),cancelRestored,pauseHeld,bossDisableRestored,midCancelled,midRestore,selectedState,maximumPhase,noEarlyDeath,bossPlayed,maxHipOffset,frameCount=captureIndex,trace});
    File.WriteAllText(Dir+"play-progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
    failure|=!zero||!pending||!began||!done||!restored||!cancelRestored||!pauseHeld||!bossDisableRestored||!midRestore||!noEarlyDeath||!bossPlayed||maxHipOffset>2||selectedState!=pair.playerState||weapon==SahurWeaponLoadout.Weapon.M4A1&&rifleBore<.98f;
    UnityEngine.Object.DestroyImmediate(parent);await Task.Delay(100);foreach(var stray in scene.GetRootGameObjects())if(stray!=p.gameObject&&stray!=floor)UnityEngine.Object.DestroyImmediate(stray);
   }
   UnityEngine.Object.Destroy(mat);
  }
  catch(Exception ex){errors.Add(ex.ToString());failure=true;}
  finally
  {
   Time.timeScale=oldScale;InputSystem.RemoveDevice(keyboard);if(originalKeyboard!=null)originalKeyboard.MakeCurrent();
   var exec=p?p.GetComponent<SahurExecution>():null;if(exec&&exec.IsActive)exec.enabled=false;
   if(p){SceneManager.MoveGameObjectToScene(p.gameObject,originalScene);var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.SetPositionAndRotation(originalPosition,originalRotation);cc.enabled=true;}
   foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
   SceneManager.SetActiveScene(previousScene);_ = SceneManager.UnloadSceneAsync(scene);Application.logMessageReceived-=Log;
   savePathField.SetValue(null,previousSavePath);
  }
  var result=new{passed=!failure&&errors.Count==0,rows,errors};File.WriteAllText(Dir+"play-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
 static void Capture(ThirdPersonPlayerController p,GameObject boss,string name)
 {
  var go=new GameObject("Temporary execution capture",typeof(Camera));var cam=go.GetComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.farClipPlane=60;
  Vector3 focus=Vector3.Lerp(p.GetComponent<CharacterController>().bounds.center,boss.GetComponent<BossExecution>().FocusPoint,.45f);
  cam.transform.SetPositionAndRotation(focus+new Vector3(8,3,-8),Quaternion.LookRotation(new Vector3(-8,-3,8)));cam.fieldOfView=50;
  var rt=new RenderTexture(1000,700,24){antiAliasing=4};var tex=new Texture2D(1000,700,TextureFormat.RGB24,false);var old=RenderTexture.active;
  try{rt.Create();cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1000,700),0,0);tex.Apply();File.WriteAllBytes(Dir+name+".png",tex.EncodeToPNG());}
  finally{RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}





