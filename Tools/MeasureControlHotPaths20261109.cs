using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Mavis;
public static class MeasureControlHotPaths20261109
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 public static object Run()
 {
  if(!Application.isPlaying)throw new Exception("Isolated Play required");
  var scene=SceneManager.CreateScene("Control allocation microbench");var root=new GameObject("Excluded hot-path fixture");SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(false);root.transform.position=new Vector3(22000,700,22000);
  var rows=new List<object>();var checks=new List<object>();
  try
  {
   var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);actor.AddComponent<GameSaveExcluded>();
   foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;root.SetActive(true);foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
   var motor=actor.GetComponent<ThirdPersonPlayerController>();var body=actor.GetComponent<CharacterController>();var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(200,1,200);
   motor.RestoreSavedPose(root.transform.position+Vector3.up*(.03f-(body.center.y-body.height*.5f)*Mathf.Abs(actor.transform.lossyScale.y)),Quaternion.identity);typeof(ThirdPersonPlayerController).GetField("verticalSpeed",F).SetValue(motor,-5f);motor.seaLevel=-1000;Physics.SyncTransforms();
   var follow=(Func<Vector3,bool,Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3,bool,Vector3>),motor,typeof(ThirdPersonPlayerController).GetMethod("FollowWalkableGround",F));
   var accelerate=(Func<Vector3,bool,float,Vector3>)Delegate.CreateDelegate(typeof(Func<Vector3,bool,float,Vector3>),motor,typeof(ThirdPersonPlayerController).GetMethod("AdvanceLocomotion",F));
   var feet=(Action)Delegate.CreateDelegate(typeof(Action),motor,typeof(ThirdPersonPlayerController).GetMethod("UpdateLockFootwork",F));
   void Measure(string name,Action work)
   {
    for(int i=0;i<80;i++)work();long bytes=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();
    const int count=1000;for(int i=0;i<count;i++)work();long end=Stopwatch.GetTimestamp(),allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;
    rows.Add(new{name,calls=count,managedBytes=allocated,microsecondsPerCall=(end-start)*1000000d/Stopwatch.Frequency/count});
   }
   Vector3 delta=new Vector3(0,-.09f,.16f),desired=Vector3.forward*9.6f;
   motor.useControlPreset=false;Measure("Ground follow disabled",()=>follow(delta,true));
   motor.useControlPreset=true;Measure("Ground follow with nonalloc physics",()=>follow(delta,true));
   Measure("Acceleration response",()=>accelerate(desired,true,1f/60));
   Measure("Footwork parameter update (idle)",()=>feet());
   void Check(bool ok,string name){checks.Add(new{ok,name});if(!ok)throw new Exception(name);}
   SetVertical(5);Check(follow(delta,true)==delta,"Rising jumps receive no downward ground follow");
   SetVertical(-5);Check(follow(new Vector3(0,.1f,.16f),true)==new Vector3(0,.1f,.16f),"Ascending authored displacement remains unchanged");
   Check(follow(delta,false)==delta,"Already-airborne motion remains unchanged");
   motor.ExternalMovementLock=true;Check(follow(delta,true)==delta,"External movement owner excludes ground follow");motor.ExternalMovementLock=false;
   floor.SetActive(false);Physics.SyncTransforms();Check(follow(delta,true)==delta,"A true ledge without ground never gains downward snap");floor.SetActive(true);
   floor.transform.localPosition=Vector3.down*2;Physics.SyncTransforms();Check(follow(delta,true)==delta,"A drop beyond the configured follow range remains airborne");
   void SetVertical(float value)=>typeof(ThirdPersonPlayerController).GetField("verticalSpeed",F).SetValue(motor,value);
  }
  finally{UnityEngine.Object.DestroyImmediate(root);SceneManager.UnloadSceneAsync(scene);}
  File.WriteAllText(".codex/gameplay-feel-20261109/hot-path-microbench.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rows,checks,scope="Warmed main-thread methods only; excludes input/review harness, whole-frame GC, GPU and rendering."},Newtonsoft.Json.Formatting.Indented));return new{rows,checks};
 }
}
