using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifySahurSteps
{
 public static object Baseline()=>Run(false);
 public static object Proposed()=>Run(true);
 static object Run(bool assisted)
 {
  if(Application.isPlaying)throw new Exception("Stopped editor required");
  var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var rows=new List<object>();
  int failures=0;
  var method=typeof(ThirdPersonPlayerController).GetMethod("MoveWithLowStep",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  var move=(Func<CharacterController,Vector3,bool,CollisionFlags>)Delegate.CreateDelegate(typeof(Func<CharacterController,Vector3,bool,CollisionFlags>),method);
  try
  {
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(10000,-.5f,10000);floor.transform.localScale=new Vector3(25,1,30);
   var ledge=GameObject.CreatePrimitive(PrimitiveType.Cube);ledge.transform.position=new Vector3(10000,.2f,10004);ledge.transform.localScale=new Vector3(4,.4f,2);
   var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(10000,6,10004);ceiling.transform.localScale=new Vector3(4,1,3);ceiling.SetActive(false);
   var actor=new GameObject("Temporary Sahur capsule");actor.transform.localScale=Vector3.one*3;var cc=actor.AddComponent<CharacterController>();
   cc.height=1.7668991f;cc.radius=.15f;cc.center=new Vector3(0,-.25484467f,0);cc.stepOffset=.22f;cc.skinWidth=.015f;cc.slopeLimit=45;cc.minMoveDistance=0;
   foreach(bool lowCeiling in new[]{false,true})
   foreach(float height in lowCeiling?new[]{.4f}:new[]{.15f,.3f,.4f,.6f,.8f,1.2f,2f})
   foreach(float speed in new[]{9.6f,17.76f})
   foreach(int fps in new[]{30,60,144})
   {
    ceiling.SetActive(lowCeiling);
    ledge.transform.position=new Vector3(10000,height/2,10004);ledge.transform.localScale=new Vector3(4,height,2);
    cc.enabled=false;actor.transform.position=new Vector3(10000,3.46f,10000);cc.enabled=true;Physics.SyncTransforms();cc.Move(Vector3.down*.1f);
    float peak=0;
    for(int i=0;i<fps;i++){move(cc,new Vector3(0,-11,speed)/fps,assisted && cc.isGrounded);peak=Mathf.Max(peak,cc.bounds.min.y);}
    bool crossed=actor.transform.position.z>10005.5f;
    bool expected=!lowCeiling && height<=(assisted?.4f:.15f);
    if(crossed!=expected || cc.stepOffset!=.22f)failures++;
    rows.Add(new{height,speed,fps,lowCeiling,settingRestored=cc.stepOffset==.22f,passed=crossed,expected,advance=actor.transform.position.z-10000,peak});
   }
   return new{assisted,passed=failures==0,failures,rows};
  }
  finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
 }
}


