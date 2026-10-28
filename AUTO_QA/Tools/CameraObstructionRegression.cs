using System;using System.IO;using System.Reflection;using System.Collections.Generic;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;
public static class AutoQaCameraObstruction {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 public static async Task<object> Run(string take){
 if(!Application.isPlaying||SceneManager.sceneCount!=1||!string.IsNullOrEmpty(SceneManager.GetActiveScene().path)||CombatFeelPreview.Player)throw new Exception("Isolated unowned Play required");
 var rows=new List<object>();string error=null;float oldCapture=Time.captureDeltaTime;GameObject fixture=null;SessionState.SetBool("AUTO_QA.Camera015",true);
 try{Time.captureDeltaTime=1f/60;CombatFeelPreview.Spawn();var motor=CombatFeelPreview.Player.GetComponent<ThirdPersonPlayerController>();motor.ExternalControlLock=true;int f=Time.frameCount+5;while(Time.frameCount<f)await Task.Delay(5);motor.enabled=false;
 fixture=new GameObject("AUTO_QA Camera Obstruction");var focusObject=new GameObject("focus");focusObject.transform.SetParent(fixture.transform);Vector3 focus=new Vector3(2050,550,2050);focusObject.transform.position=focus-Vector3.up*.25f;motor.ExternalCameraFocus=focusObject.transform;motor.cameraDistance=10;motor.cameraFollowSharpness=10;motor.cameraCollision=true;Set(motor,"playerCamera",CombatFeelPreview.Camera);Set(motor,"pitch",0f);Set(motor,"yaw",0f);Set(motor,"cameraInitialized",true);Set(motor,"continuingCinematicCamera",false);Set(motor,"rifleCameraShoulder",0f);
 var camera=CombatFeelPreview.Camera;var late=typeof(ThirdPersonPlayerController).GetMethod("LateUpdate",F);Vector3 start=focus+Vector3.right*6;Vector3 desired=focus-Vector3.forward*10;float blend=1-Mathf.Exp(-10*Time.deltaTime);Vector3 expected=Vector3.Lerp(start,desired,blend);
 var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Moving wall";wall.transform.SetParent(fixture.transform);wall.transform.localScale=new Vector3(.6f,4,.6f);var box=wall.GetComponent<BoxCollider>();
 foreach(float angle in new[]{0f,45f,90f,180f}){Set(motor,"yaw",angle);var rotation=Quaternion.Euler(0,angle,0);start=focus+rotation*Vector3.right*6;desired=focus-rotation*Vector3.forward*10;expected=Vector3.Lerp(start,desired,blend);foreach(string mode in new[]{"clear","moving wall","trigger","disabled collision","release"}){
 wall.transform.position=mode=="clear"||mode=="release"?focus+Vector3.up*20:expected;box.isTrigger=mode=="trigger";motor.cameraCollision=mode!="disabled collision";camera.transform.position=start;Physics.SyncTransforms();
 bool desiredRayClear=!Physics.SphereCast(focus,.12f,(desired-focus).normalized,out var hit,10,~0,QueryTriggerInteraction.Ignore);late.Invoke(motor,null);Vector3 actual=camera.transform.position;float distance=Vector3.Distance(box.ClosestPoint(actual),actual);bool blocked=mode=="moving wall";rows.Add(new{ok=blocked?distance>=.119f:Vector3.Distance(actual,expected)<.002f,mode,angle,desiredRayClear,cameraSurfaceDistance=distance,position=actual.ToString("F4"),expected=expected.ToString("F4"),dt=Time.deltaTime});
 }}
 }catch(Exception e){error=e.ToString();}finally{if(fixture)UnityEngine.Object.DestroyImmediate(fixture);CombatFeelPreview.Clear();Time.captureDeltaTime=oldCapture;SessionState.SetBool("AUTO_QA.Camera015",false);}
 var result=new{error,rows};File.WriteAllText("AUTO_QA/Evidence/"+take+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
