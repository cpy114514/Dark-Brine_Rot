using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class VerifyOneHandHeavy
{
    const BindingFlags Private = BindingFlags.Instance|BindingFlags.NonPublic;
    static void Call(object obj,string method) => obj.GetType().GetMethod(method,Private).Invoke(obj,null);
    static void Set(object obj,string field,object value) => obj.GetType().GetField(field,Private).SetValue(obj,value);
    const string PrefabPath="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    static readonly HumanBodyBones[] Unchanged = {HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,
        HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};

    public static async Task<object> Preview(bool animate=false)
    {
        if(!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),new Vector3(0,500,0),Quaternion.identity);
        var cameraObject=new GameObject("One-handed preview camera"); var lightObject=new GameObject("One-handed preview light");
        var target=new RenderTexture(500,600,24); var sheet=new Texture2D(2000,1200,TextureFormat.RGB24,false);
        var previous=RenderTexture.active; float time=Time.timeScale;
        try
        {
            Time.timeScale=0; root.GetComponent<ThirdPersonPlayerController>().enabled=false; root.GetComponent<CharacterController>().enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>(); attack.enabled=false;
            var animator=attack.animator; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var relay=animator.GetComponent<SahurRootMotionRelay>();if(relay) relay.enabled=false;
            int layer=animator.GetLayerIndex("Charge Upper Body");
            foreach(var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=31;
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.22f,.25f);camera.orthographic=true;camera.targetTexture=target;
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-35,0);
            animator.SetLayerWeight(layer,0);animator.SetFloat("Speed",0);animator.Play("Locomotion",0,0);animator.Update(0);
            var head=animator.GetBoneTransform(HumanBodyBones.Head);var foot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var focus=(head.position+foot.position)*.5f+root.transform.right*.35f+Vector3.up*.7f;
            camera.orthographicSize=Mathf.Max(3.4f,Vector3.Distance(head.position,foot.position)*1.2f);
            camera.transform.position=focus+root.transform.right*6+root.transform.forward*9+Vector3.up*.6f;camera.transform.LookAt(focus);
            float maxOther=0,minHead=float.MaxValue;
            var results=new List<object>(); var bones=Unchanged.Select(animator.GetBoneTransform).Where(b=>b).ToArray();
            float[] seconds={0,.14f,.45f,.58f,.67f,.77f,.92f,1.12f};
            var cap=(CapsuleCollider)attack.stickHitbox;
            for(int k=0;k<seconds.Length;k++)
            {
                float phase=seconds[k]/attack.chargeClip.length;
                animator.SetLayerWeight(layer,0);animator.Play("Locomotion",0,0);animator.Update(0);
                var baseline=bones.Select(b=>b.localRotation).ToArray();
                animator.SetFloat("ChargePhase",phase);animator.SetLayerWeight(layer,1);
                animator.Play("Locomotion",0,0);animator.Play(k<3?"Charge Windup":"Heavy Attack",layer,phase);animator.Update(0);
                for(int i=0;i<bones.Length;i++) maxOther=Mathf.Max(maxOther,Quaternion.Angle(baseline[i],bones[i].localRotation));
                var axis=cap.direction==0?Vector3.right:cap.direction==1?Vector3.up:Vector3.forward;
                Vector3 center=cap.transform.TransformPoint(cap.center),extent=cap.transform.TransformVector(axis*Mathf.Max(0,cap.height*.5f-cap.radius));
                Vector3 a=center-extent,b=center+extent,d=b-a;
                float distance=Vector3.Distance(head.position,a+d*Mathf.Clamp01(Vector3.Dot(head.position-a,d)/d.sqrMagnitude));
                minHead=Mathf.Min(minHead,distance);
                await Task.Delay(80);
                camera.Render();RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,500,600),(k%4)*500,(1-k/4)*600);
                results.Add(new{seconds=seconds[k],hand=root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position).ToString(),club=root.transform.InverseTransformPoint(center).ToString(),headDistance=distance});
            }
            sheet.Apply();File.WriteAllBytes(Path.GetFullPath("Tools/SahurOneHandHeavyPreview.png"),sheet.EncodeToPNG());
            if(maxOther>.2f) throw new Exception("Charged attack changed left arm/body/legs: "+maxOther);
            if(animate)
            {
                string directory=Path.GetFullPath(".codex/onehand-preview");Directory.CreateDirectory(directory);
                var frame=new Texture2D(500,600,TextureFormat.RGB24,false);
                try
                {
                    for(int i=0;i<75;i++)
                    {
                        float elapsed=i/24f;
                        float sampleTime=elapsed<1.8f?.45f*elapsed/1.8f:elapsed<2.05f?.45f:Mathf.Min(1.12f,.45f+elapsed-2.05f);
                        float phase=sampleTime/attack.chargeClip.length;
                        animator.SetFloat("ChargePhase",phase);animator.SetLayerWeight(layer,1);
                        animator.Play("Locomotion",0,0);animator.Play("Charge Windup",layer,0);animator.Update(0);
                        await Task.Delay(40);
                        camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,500,600),0,0);frame.Apply();
                        File.WriteAllBytes(Path.Combine(directory,i.ToString("D3")+".png"),frame.EncodeToPNG());
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(frame);}
            }
            return new{results,maxOtherBoneDegrees=maxOther,minHeadDistance=minHead,preview="Tools/SahurOneHandHeavyPreview.png"};
        }
        finally
        {
            Time.timeScale=time;RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(sheet);
        }
    }

    public static object Runtime()
    {
        if(!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),new Vector3(0,500,0),Quaternion.identity);
        float time=Time.timeScale;
        try
        {
            Time.timeScale=0;
            root.GetComponent<ThirdPersonPlayerController>().enabled=false;root.GetComponent<CharacterController>().enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;
            var animator=attack.animator;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var stamina=root.GetComponent<Mavis.PlayerStamina>();
            int layer=animator.GetLayerIndex("Charge Upper Body");
            var cases=new List<object>();
            float otherBoneError=0;
            var bones=Unchanged.Select(animator.GetBoneTransform).Where(b=>b).ToArray();
            foreach(float speed in new[]{0f,16.8f}) foreach(float phase in new[]{.1f,.4017857f,.55f,.68f,.90f})
            {
                animator.SetFloat("Speed",speed);animator.SetFloat("ChargePhase",phase);animator.SetLayerWeight(layer,0);
                animator.Play("Locomotion",0,.31f);animator.Update(0);var rotations=bones.Select(b=>b.localRotation).ToArray();
                animator.SetLayerWeight(layer,1);animator.Play("Locomotion",0,.31f);animator.Play("Charge Windup",layer,0);animator.Update(0);
                for(int b=0;b<bones.Length;b++)otherBoneError=Mathf.Max(otherBoneError,Quaternion.Angle(rotations[b],bones[b].localRotation));
            }
            if(otherBoneError>.2f)throw new Exception("Walking body/left arm changed: "+otherBoneError);
            foreach(float charge in new[]{.08f,.5f,1f,1.8f})
            {
                attack.SuspendForSwimming();stamina.currentStamina=100;
                animator.Play("Locomotion",0,0);animator.SetFloat("Speed",charge==.5f?16.8f:0);animator.Update(0);
                Call(attack,"StartCharge");Set(attack,"chargeStartedAt",Time.time-attack.fullChargeTime*charge);
                Call(attack,"UpdateChargePose");animator.SetLayerWeight(layer,1);animator.Update(0);
                Vector3 held=animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                float expected=attack.damage*Mathf.Lerp(attack.minChargeDamageMultiplier,attack.maxChargeDamageMultiplier,Mathf.Clamp01(charge));
                Call(attack,"ReleaseCharge");Set(attack,"attackStartedAt",Time.time-1);
                animator.Update(1f/240);
                float firstStep=Vector3.Distance(held,animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                if(!attack.IsHeavyAttackActive || attack.UsesAnimationRootMotion) throw new Exception("Release did not use right-arm heavy.");
                float actual=(float)typeof(Mavis.SahurAttack).GetField("swingDamage",Private).GetValue(attack);
                if(Mathf.Abs(actual-expected)>.02f) throw new Exception("Charge damage changed.");
                int hitFrames=0;float firstHit=-1,lastHit=-1;
                for(int i=0;i<360 && attack.IsHeavyAttackActive;i++)
                {
                    animator.Update(1f/240);Call(attack,"UpdateAttackProgress");Call(attack,"UpdateHitbox");
                    if(attack.stickHitbox.enabled){hitFrames++;if(firstHit<0)firstHit=attack.HeavyAttackPhase;lastHit=attack.HeavyAttackPhase;}
                    if(animator.GetCurrentAnimatorStateInfo(0).shortNameHash!=Animator.StringToHash("Locomotion")) throw new Exception("Heavy replaced locomotion.");
                }
                if(hitFrames<20 || attack.IsHeavyAttackActive || attack.stickHitbox.enabled || firstStep>.12f) throw new Exception("Release continuity/hit/completion failed.");
                attack.SuspendForSwimming();if(animator.GetLayerWeight(layer)>.001f)throw new Exception("Cancel did not clear arm layer.");
                cases.Add(new{charge,firstStep,hitFrames,firstHit,lastHit,damage=actual});
            }
            var relay=animator.GetComponent<SahurRootMotionRelay>();if(relay)relay.enabled=false;
            attack.chargeClip=null;stamina.currentStamina=100;animator.SetFloat("Speed",0);animator.Play("Locomotion",0,0);animator.Update(0);
            Call(attack,"StartCharge");Call(attack,"ReleaseCharge");Set(attack,"attackStartedAt",Time.time-1);animator.Update(.05f);
            bool baseHeavy=animator.GetCurrentAnimatorStateInfo(0).shortNameHash==Animator.StringToHash("Heavy Attack") ||
                animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash==Animator.StringToHash("Heavy Attack");
            if(!baseHeavy || !attack.UsesAnimationRootMotion)throw new Exception("Null-clip fallback watched the wrong layer.");
            attack.SuspendForSwimming();
            return new{cases,otherBoneError,leftArmOnLocomotion=true,rootMotionOff=true,cancelClearsLayer=true,nullClipFallback=true};
        }
        finally{Time.timeScale=time;UnityEngine.Object.DestroyImmediate(root);}
    }
}
