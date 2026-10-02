using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class VerifyFastSwimming
{
    const string PrefabPath="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    public static async Task<object> Preview(bool animate=false)
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),new Vector3(0,500,0),Quaternion.identity);
        var cameraObject=new GameObject("Swimming preview camera");var lightObject=new GameObject("Swimming preview light");
        var target=new RenderTexture(600,420,24);var sheet=new Texture2D(2400,840,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;float time=Time.timeScale;
        try
        {
            Time.timeScale=0;root.GetComponent<ThirdPersonPlayerController>().enabled=false;root.GetComponent<CharacterController>().enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;
            var animator=attack.animator;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.GetComponent<SahurRootMotionRelay>().enabled=false;
            var guard=animator.GetComponent<SahurCombatGuardIK>();if(guard)guard.enabled=false;
            root.GetComponent<SahurSwimmingWeapon>().stick.gameObject.SetActive(false);
            animator.SetLayerWeight(1,0);
            foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.22f,.25f);camera.orthographic=true;camera.targetTexture=target;
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-35,0);
            var positions=new List<object>();
            for(int row=0;row<2;row++)for(int k=0;k<4;k++)
            {
                animator.Play(row==0?"Swim Forward":"Swim Fast",0,k*.25f);animator.Update(0);
                var head=animator.GetBoneTransform(HumanBodyBones.Head);var foot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Vector3 focus=(head.position+foot.position)*.5f;
                camera.orthographicSize=3.2f;camera.transform.position=focus+new Vector3(10,5,4);camera.transform.LookAt(focus);
                await Task.Delay(80);camera.Render();RenderTexture.active=target;
                sheet.ReadPixels(new Rect(0,0,600,420),k*600,(1-row)*420);
                positions.Add(new{stroke=row,phase=k*.25f,head=root.transform.InverseTransformPoint(head.position).ToString(),foot=root.transform.InverseTransformPoint(foot.position).ToString(),leftHand=root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftHand).position).ToString(),rightHand=root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position).ToString()});
            }
            sheet.Apply();File.WriteAllBytes(Path.GetFullPath("Tools/SahurSwimmingPreview.png"),sheet.EncodeToPNG());
            if(animate)
            {
                var frame=new Texture2D(600,420,TextureFormat.RGB24,false);string dir=Path.GetFullPath(".codex/swim-preview");Directory.CreateDirectory(dir);
                try{for(int k=0;k<66;k++){
                    animator.Play("Swim Fast",0,k/33f);animator.Update(0);
                    var head=animator.GetBoneTransform(HumanBodyBones.Head);var foot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    var focus=(head.position+foot.position)*.5f;
                    camera.transform.position=focus+new Vector3(10,5,4);camera.transform.LookAt(focus);
                    await Task.Delay(45);camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,600,420),0,0);frame.Apply();
                    File.WriteAllBytes(Path.Combine(dir,k.ToString("D3")+".png"),frame.EncodeToPNG());
                }}finally{UnityEngine.Object.DestroyImmediate(frame);}
            }
            return positions;
        }
        finally{Time.timeScale=time;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(sheet);}
    }
    public static async Task<object> Runtime()
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        var oceanObject=new GameObject("Swimming verification ocean");oceanObject.SetActive(false);
        var ocean=oceanObject.AddComponent<OceanWorld>();ocean.resolution=16;ocean.oceanSize=40;ocean.followCamera=false;ocean.oceanHeight=500;
        ocean.wave1.amplitude=ocean.wave2.amplitude=ocean.wave3.amplitude=ocean.wave4.amplitude=0;
        oceanObject.SetActive(true);
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),new Vector3(0,498,0),Quaternion.identity);
        try
        {
            var p=root.GetComponent<ThirdPersonPlayerController>();p.enabled=false;p.seaLevel=500;p.waterSplashes=false;
            typeof(ThirdPersonPlayerController).GetField("swimmingOcean",Private).SetValue(p,ocean);
            var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;
            var animator=attack.animator;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var stamina=root.GetComponent<Mavis.PlayerStamina>();stamina.enabled=false;
            var regeneration=typeof(Mavis.PlayerStamina).GetField("nextRegenerationTime",Private);
            var tick=typeof(ThirdPersonPlayerController).GetMethod("UpdateSwimming",Private);
            var velocity=typeof(ThirdPersonPlayerController).GetField("planarVelocity",Private);
            var results=new List<object>();
            Require(p.Swimming,"Test must be in deep water.");
            foreach(bool fast in new[]{false,true})
            {
                p.RestoreSavedPose(new Vector3(0,498,0),Quaternion.identity);velocity.SetValue(p,Vector3.zero);stamina.currentStamina=80;
                regeneration.SetValue(stamina,Time.time+100);
                Vector3 start=root.transform.position;float elapsed=0;
                while(elapsed<2){
                    tick.Invoke(p,new object[]{Vector3.forward,Vector3.forward,fast,false});elapsed+=Time.deltaTime;
                    await Task.Delay(16);
                }
                float distance=Vector3.Distance(new Vector3(start.x,0,start.z),new Vector3(root.transform.position.x,0,root.transform.position.z));
                float speed=((Vector3)velocity.GetValue(p)).magnitude;
                string state=fast?"Swim Fast":"Swim Forward";
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName(state),"Wrong swimming state: "+state);
                Require(Mathf.Abs(speed-p.swimSpeed*(fast?p.fastSwimMultiplier:1))<.01f,"Wrong actual velocity.");
                float expected=speed*elapsed-speed*speed/(2*p.swimAcceleration);
                Require(Mathf.Abs(distance-expected)<speed*.2f,"CharacterController displacement differs from configured speed.");
                Require(fast==p.FastSwimming,"Wrong fast swim flag.");
                Require(p.Swimming,"Prone stroke lost water contact.");
                Require(!attack.stickHitbox.enabled,"Swimming hitbox must remain off.");
                results.Add(new{fast,elapsed,distance,speed,state,staminaSpent=80-stamina.currentStamina});
            }
            stamina.currentStamina=0;regeneration.SetValue(stamina,Time.time+100);
            tick.Invoke(p,new object[]{Vector3.forward,Vector3.forward,true,false});await Task.Delay(250);
            Require(!p.FastSwimming&&animator.GetCurrentAnimatorStateInfo(0).IsName("Swim Forward"),"Exhaustion did not restore normal stroke.");
            stamina.currentStamina=50;
            for(int k=0;k<45;k++){tick.Invoke(p,new object[]{Vector3.zero,Vector3.zero,true,false});await Task.Delay(16);}
            Require(!p.FastSwimming&&stamina.currentStamina==50,"Idle Shift must not spend stamina.");
            await Task.Delay(200);Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Swim Idle"),"Releasing movement did not return to idle.");
            results.Add(new{exhaustionFallback=true,idleFallback=true});return results;
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(oceanObject);}
    }
}
