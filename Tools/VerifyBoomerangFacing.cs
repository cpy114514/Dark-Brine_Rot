using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class VerifyBoomerangFacing
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    public static object Verify(){
        Require(Application.isPlaying,"Requires Play Mode.");
        float time=Time.timeScale;Time.timeScale=0;
        var cameraObject=new GameObject("Boomerang direction test camera");
        cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();
        cameraObject.transform.position=new Vector3(0,1000,0);
        var results=new List<object>();GameObject root=null;
        try{
            for(int yaw=0;yaw<360;yaw+=45){
                root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,500,0),Quaternion.Euler(0,yaw,0));
                var boom=root.GetComponent<SahurBoomerang>();var attack=root.GetComponent<SahurAttack>();var animator=attack.animator;
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
                var stick=root.GetComponent<SahurSwimmingWeapon>().stick;var mesh=stick.GetComponent<MeshFilter>();
                camera.transform.rotation=Quaternion.Euler(25,yaw+110,0);
                Vector3 facing=root.transform.forward;
                Require(boom.TryThrow(),"Throw refused.");
                Require(Vector3.Angle(facing,root.transform.forward)<.01f,"Throw turned the actor toward the orbiting camera.");
                Require(!boom.IsFlying&&stick.GetComponent<Renderer>().enabled,"Weapon detached during anticipation.");
                camera.transform.rotation=Quaternion.Euler(-35,yaw-70,0);
                for(int i=0;i<240&&!boom.IsFlying;i++){animator.Update(1f/240);boom.Simulate(1f/240);}
                Require(boom.IsFlying,"Animation never released the stick.");
                var visual=Field<GameObject>(boom,"visual");
                float centerError=Vector3.Distance(stick.TransformPoint(mesh.sharedMesh.bounds.center),boom.FlightPosition);
                float rotationError=Quaternion.Angle(stick.rotation,visual.transform.rotation);
                Require(centerError<.001f&&rotationError<.05f,"Release jumped away from the current animated grip.");
                Vector3 start=boom.FlightPosition;
                float maxHeadingError=0;
                // Moving the camera and turning the actor after release must
                // not steer an already flying projectile.
                root.transform.rotation=Quaternion.Euler(0,yaw+90,0);
                camera.transform.rotation=Quaternion.Euler(0,yaw+180,0);
                for(int step=0;step<12;step++){
                    boom.Simulate(1f/60);
                    Vector3 travel=Vector3.ProjectOnPlane(boom.FlightPosition-start,Vector3.up);
                    maxHeadingError=Mathf.Max(maxHeadingError,Vector3.Angle(facing,travel));
                }
                Require(maxHeadingError<.05f,"Flight did not follow the release heading: "+maxHeadingError);
                Require(Vector3.Dot(boom.FlightPosition-start,facing)>3f,"Projectile did not move forward.");
                boom.Catch();Require(stick.GetComponent<Renderer>().enabled,"Catch did not restore the held weapon.");
                results.Add(new{yaw,centerError,rotationError,maxHeadingError});
                UnityEngine.Object.DestroyImmediate(visual);UnityEngine.Object.DestroyImmediate(root);root=null;
            }
            return new{headings=results,cameraIndependent=true,releaseAtAnimatedGrip=true,flightHeadingLatched=true};
        }finally{if(root)UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);Time.timeScale=time;}
    }
}
