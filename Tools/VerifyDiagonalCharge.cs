using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class VerifyDiagonalCharge
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string PrefabPath="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    static void Call(object o,string m)=>o.GetType().GetMethod(m,Private).Invoke(o,null);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Private).SetValue(o,v);
    static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    static readonly HumanBodyBones[] Untouched={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,
        HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
    static GameObject Spawn(){
        Require(Application.isPlaying,"Requires Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),new Vector3(0,500,0),Quaternion.identity);
        root.GetComponent<ThirdPersonPlayerController>().enabled=false;root.GetComponent<CharacterController>().enabled=false;
        foreach(var input in root.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>())input.enabled=false;
        var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;
        var animator=attack.animator;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        animator.GetComponent<SahurRootMotionRelay>().enabled=false;
        return root;
    }
    static float HeadDistance(Mavis.SahurAttack attack){
        var cap=(CapsuleCollider)attack.stickHitbox;var head=attack.animator.GetBoneTransform(HumanBodyBones.Head);
        Vector3 axis=cap.direction==0?Vector3.right:cap.direction==1?Vector3.up:Vector3.forward;
        Vector3 center=cap.transform.TransformPoint(cap.center),extent=cap.transform.TransformVector(axis*Mathf.Max(0,cap.height*.5f-cap.radius));
        Vector3 a=center-extent,d=extent*2;return Vector3.Distance(head.position,a+d*Mathf.Clamp01(Vector3.Dot(head.position-a,d)/d.sqrMagnitude));
    }
    public static object Runtime(){
        var root=Spawn();float time=Time.timeScale;
        try{
            Time.timeScale=0;var attack=root.GetComponent<Mavis.SahurAttack>();var animator=attack.animator;
            int layer=animator.GetLayerIndex("Charge Upper Body");var stamina=root.GetComponent<Mavis.PlayerStamina>();
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var bones=Untouched.Select(animator.GetBoneTransform).ToArray();
            bool stabilizedLegacy=attack.chargeClip.name=="SahurDiagonalChargedStrike"&&!attack.chargedStrikeClip;
            float otherError=0,minHead=float.MaxValue;string closest="";
            foreach(float speed in new[]{0f,16.8f})for(int k=0;k<=120;k++){
                float phase=k/120f;animator.SetFloat("Speed",speed);animator.SetLayerWeight(layer,0);animator.Play("Locomotion",0,.31f);animator.Update(0);
                var baseline=bones.Select(b=>b.localRotation).ToArray();animator.SetLayerWeight(layer,1);animator.Play("Heavy Attack",layer,phase);animator.Update(0);
                for(int b=0;b<bones.Length;b++)otherError=Mathf.Max(otherError,Quaternion.Angle(baseline[b],bones[b].localRotation));
                float distance=HeadDistance(attack);if(distance<minHead){minHead=distance;closest="phase="+phase+" speed="+speed+" hand="+root.transform.InverseTransformPoint(hand.position)+" head="+root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Head).position);}
            }
            Require(otherError<.2f,"Left arm/body/legs changed: "+otherError);Require(minHead>.7f,"Club approached head: "+minHead+" "+closest);
            attack.SuspendForSwimming();animator.SetFloat("Speed",0);animator.Play("Locomotion",0,0);animator.Update(0);
            stamina.currentStamina=100;Call(attack,"StartCharge");animator.SetLayerWeight(layer,1);
            Vector3 held=Vector3.zero;Quaternion heldWrist=Quaternion.identity;float drift=0;
            var holdCases=new List<object>();
            float windup=attack.chargeWindupTime;
            foreach(float elapsed in new[]{windup*.5f,windup,windup+.4f,1.8f,4f}){
                Set(attack,"chargeStartedAt",Time.time-elapsed);Call(attack,"UpdateChargePose");animator.Play("Charge Windup",layer,0);animator.Update(0);
                if(elapsed==windup){held=hand.position;heldWrist=hand.rotation;}
                if(elapsed>windup)drift=Mathf.Max(drift,Vector3.Distance(held,hand.position)+Quaternion.Angle(heldWrist,hand.rotation)*.01f);
                Require(!attack.stickHitbox.enabled,"Windup enabled damage.");
                holdCases.Add(new{elapsed,attack.Charge01,attack.ChargePose01,hand=root.transform.InverseTransformPoint(hand.position).ToString()});
            }
            Require(drift<.001f,"Held pose drifted: "+drift);
            Require(stamina.currentStamina==100,"Holding spent stamina before release.");
            var cases=new List<object>();
            foreach(float elapsed in new[]{.08f,.21f,.42f,.9f,1.8f,4f}){
                attack.SuspendForSwimming();stamina.currentStamina=100;animator.SetFloat("Speed",elapsed==.9f?16.8f:0);animator.Play("Locomotion",0,.31f);animator.Update(0);
                Call(attack,"StartCharge");Set(attack,"chargeStartedAt",Time.time-elapsed);Call(attack,"UpdateChargePose");animator.SetLayerWeight(layer,1);animator.Update(0);
                Vector3 before=hand.position;float expected=attack.damage*Mathf.Lerp(attack.minChargeDamageMultiplier,attack.maxChargeDamageMultiplier,Mathf.Clamp01(elapsed/attack.fullChargeTime));
                Call(attack,"ReleaseCharge");Set(attack,"attackStartedAt",Time.time-1);animator.Update(1f/240);
                float firstStep=Vector3.Distance(before,hand.position);Require(firstStep<.12f,"Release snapped hand: "+firstStep);
                Require(attack.IsHeavyAttackActive&&!attack.UsesAnimationRootMotion,"Release did not use right-arm strike.");
                float actual=(float)typeof(Mavis.SahurAttack).GetField("swingDamage",Private).GetValue(attack);
                Require(Mathf.Abs(actual-expected)<.02f,"Charge damage changed.");Require(Mathf.Abs(stamina.currentStamina-(100-stamina.chargedAttackCost))<.01f,"Wrong stamina payment.");
                int hitFrames=0;float firstHit=-1,lastHit=-1,maxHandVelocity=0,maxRecoveryVelocity=0,maxLateShaftY=-1,maxUpwardStep=0;
                float previousShaftY=float.NaN;
                Quaternion previousHand=hand.rotation;
                var cap=(CapsuleCollider)attack.stickHitbox;Vector3 shaftAxis=cap.direction==0?Vector3.right:cap.direction==1?Vector3.up:Vector3.forward;
                for(int k=0;k<800&&attack.IsHeavyAttackActive;k++){
                    Call(attack,"UpdateChargeLayerWeight");
                    animator.Update(1f/240);Call(attack,"UpdateAttackProgress");Call(attack,"UpdateHitbox");
                    float phase=attack.HeavyAttackPhase,velocity=Quaternion.Angle(previousHand,hand.rotation)*240;previousHand=hand.rotation;
                    bool inSwing=!stabilizedLegacy?phase>=attack.heavySwingWindowStart&&phase<=attack.heavySwingWindowEnd:phase>.52f&&phase<.98f;
                    if(inSwing)maxHandVelocity=Mathf.Max(maxHandVelocity,velocity);
                    if(phase>(attack.chargedStrikeClip ? .52f : .82f)&&phase<.98f)maxRecoveryVelocity=Mathf.Max(maxRecoveryVelocity,velocity);
                    Vector3 shaft=cap.transform.TransformDirection(shaftAxis);if(Vector3.Dot(shaft,cap.transform.TransformPoint(cap.center)-hand.position)<0)shaft=-shaft;
                    float shaftY=root.transform.InverseTransformDirection(shaft).y;
                    if(inSwing&&!float.IsNaN(previousShaftY))maxUpwardStep=Mathf.Max(maxUpwardStep,shaftY-previousShaftY);
                    previousShaftY=shaftY;
                    if(phase>.72f&&phase<.93f)maxLateShaftY=Mathf.Max(maxLateShaftY,root.transform.InverseTransformDirection(shaft).y);
                    if(attack.stickHitbox.enabled){hitFrames++;if(firstHit<0)firstHit=attack.HeavyAttackPhase;lastHit=attack.HeavyAttackPhase;}
                    Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),"Heavy attack replaced locomotion.");
                }
                Require(hitFrames>30&&!attack.IsHeavyAttackActive&&!attack.stickHitbox.enabled,"Strike hit window or completion failed.");
                if(stabilizedLegacy){
                    Require(maxHandVelocity<1500,"Wrist snapped during chop: "+maxHandVelocity+" degrees/sec.");
                    Require(maxRecoveryVelocity<400,"Wrist flipped again after impact: "+maxRecoveryVelocity+" degrees/sec.");
                    Require(maxLateShaftY<-.15f,"Club rebounded upward during downswing: "+maxLateShaftY);
                }else{
                    Require(maxHandVelocity<1600,"Native downswing snapped: "+maxHandVelocity);
                    Require(maxRecoveryVelocity<1200,"Native recovery repeatedly flipped the wrist: "+maxRecoveryVelocity);
                    Require(maxUpwardStep<.025f,"Native damaging stroke rebounded upward: "+maxUpwardStep);
                }
                attack.SuspendForSwimming();Require(animator.GetLayerWeight(layer)==0,"Water/cancel left arm layer active.");
                cases.Add(new{elapsed,firstStep,damage=actual,hitFrames,firstHit,lastHit,maxHandVelocity,maxRecoveryVelocity,maxLateShaftY,maxUpwardStep});
            }
            return new{holdCases,holdDrift=drift,otherBoneDegrees=otherError,minHeadDistance=minHead,cases,rightArmOnly=true,rootMotionOff=true};
        }finally{Time.timeScale=time;UnityEngine.Object.DestroyImmediate(root);}
    }
    public static async Task<object> Preview(){
        var root=Spawn();float time=Time.timeScale;var cameraObject=new GameObject("Diagonal charge preview");var lightObject=new GameObject("Diagonal charge light");
        var target=new RenderTexture(600,660,24);var frame=new Texture2D(600,660,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{
            Time.timeScale=0;var attack=root.GetComponent<Mavis.SahurAttack>();var animator=attack.animator;int layer=animator.GetLayerIndex("Charge Upper Body");
            foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.18f,.22f,.25f);camera.orthographic=true;camera.orthographicSize=4.2f;camera.targetTexture=target;
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-35,0);
            animator.SetFloat("Speed",0);animator.SetLayerWeight(layer,0);animator.Play("Locomotion",0,0);animator.Update(0);
            var focus=(animator.GetBoneTransform(HumanBodyBones.Head).position+animator.GetBoneTransform(HumanBodyBones.RightFoot).position)*.5f+Vector3.up;
            camera.transform.position=focus+new Vector3(9,1,9);camera.transform.LookAt(focus);
            string dir=Path.GetFullPath(".codex/diagonal-charge-preview");Directory.CreateDirectory(dir);
            root.GetComponent<Mavis.PlayerStamina>().currentStamina=100;Call(attack,"StartCharge");
            bool released=false;float finishedAt=-1;
            AnimationClip playingClip=attack.chargedStrikeClip?attack.chargedStrikeClip:attack.chargeClip;
            var controller=(UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
            float playbackSpeed=controller.layers[layer].stateMachine.states.Single(s=>s.state.name=="Heavy Attack").state.speed;
            float remaining=playingClip.length*(attack.chargedStrikeClip?1:1-attack.maxChargePosePhase)/playbackSpeed;
            const int previewFps=60;
            int frames=Mathf.CeilToInt((1.95f+remaining+.4f)*previewFps);
            for(int k=0;k<frames;k++){
                float elapsed=k/(float)previewFps;
                if(!released){Set(attack,"chargeStartedAt",Time.time-elapsed);Call(attack,"UpdateChargePose");}
                if(!released&&elapsed>=1.95f){Call(attack,"ReleaseCharge");Set(attack,"attackStartedAt",Time.time-1);released=true;}
                float weight=elapsed<.1f?elapsed/.1f:finishedAt>=0?Mathf.Clamp01(1-(elapsed-finishedAt)/.18f):1;
                animator.SetLayerWeight(layer,weight);Call(attack,"UpdateChargeLayerWeight");animator.Update(k==0?0:1f/previewFps);
                if(released){Call(attack,"UpdateAttackProgress");Call(attack,"UpdateHitbox");if(!attack.IsHeavyAttackActive&&finishedAt<0)finishedAt=elapsed;}
                await Task.Delay(25);camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,600,660),0,0);frame.Apply();
                File.WriteAllBytes(Path.Combine(dir,k.ToString("D3")+".png"),frame.EncodeToPNG());
                if(k==80)File.WriteAllBytes(Path.GetFullPath("Tools/SahurDiagonalChargeHold.png"),frame.EncodeToPNG());
            }
            return new{frames,fps=previewFps,pose="Tools/SahurDiagonalChargeHold.png",directory=dir};
        }finally{Time.timeScale=time;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
}
