using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class VerifyRightArmHeavy
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Call(object o,string m)=>o.GetType().GetMethod(m,Private).Invoke(o,null);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Private).SetValue(o,v);
    static readonly HumanBodyBones[] Untouched={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,
        HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftShoulder,HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
    public static async Task<object> Run()
    {
        if(!Application.isPlaying) throw new Exception("Requires Play mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,500,0),Quaternion.identity);
        var cameraObject=new GameObject("Right arm preview camera");
        var lightObject=new GameObject("Right arm preview light");
        var target=new RenderTexture(400,400,24); var sheet=new Texture2D(1600,800,TextureFormat.RGB24,false);
        var previous=RenderTexture.active; float time=Time.timeScale;
        try
        {
            Time.timeScale=0;
            root.GetComponent<ThirdPersonPlayerController>().enabled=false;
            root.GetComponent<CharacterController>().enabled=false;
            foreach(var input in root.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>()) input.enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>(); attack.enabled=false;
            var animator=attack.animator; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var relay=animator.GetComponent<SahurRootMotionRelay>(); if(relay) relay.enabled=false;
            var guard=animator.GetComponent<SahurCombatGuardIK>(); if(guard) guard.enabled=false;
            int layer=animator.GetLayerIndex("Charge Upper Body");
            var bones=Untouched.Select(animator.GetBoneTransform).Where(b=>b).ToArray();
            float maxUnwantedAngle=0,maxRightArmChange=0; int poses=0;
            foreach(float speed in new[]{0f,16.8f}) foreach(bool strike in new[]{false,true})
            foreach(float phase in strike ? new[]{.12f,.18f,.25f,.30f,.50f,.80f} : new[]{0f,.058f,.116071f})
            {
                animator.SetFloat("Speed",speed); animator.SetFloat("ChargePhase",phase);
                animator.SetLayerWeight(layer,0); animator.Play("Locomotion",0,.31f);
                animator.Play(strike ? "Heavy Attack" : "Charge Windup",layer,phase); animator.Update(0);
                var rotations=bones.Select(b=>b.localRotation).ToArray();
                var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm); var baseRight=right.localRotation;
                animator.SetLayerWeight(layer,1); animator.Play("Locomotion",0,.31f);
                animator.Play(strike ? "Heavy Attack" : "Charge Windup",layer,phase); animator.Update(0);
                for(int i=0;i<bones.Length;i++) maxUnwantedAngle=Mathf.Max(maxUnwantedAngle,Quaternion.Angle(rotations[i],bones[i].localRotation));
                maxRightArmChange=Mathf.Max(maxRightArmChange,Quaternion.Angle(baseRight,right.localRotation)); poses++;
            }
            if(maxUnwantedAngle>.15f || maxRightArmChange<15) throw new Exception("Arm isolation failed: unwanted="+maxUnwantedAngle+", right="+maxRightArmChange);
            foreach(var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=31;
            var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.22f,.26f,.30f);
            camera.orthographic=true; camera.orthographicSize=3.3f; camera.targetTexture=target;
            var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.6f;
            light.cullingMask=1<<31; light.transform.rotation=Quaternion.Euler(35,-35,0);
            var skin=animator.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First();
            var mesh=new Mesh();
            try
            {
                for(int i=0;i<8;i++)
                {
                    float phase=new[]{0f,.058f,.116071f,.16f,.22f,.28f,.40f,.80f}[i];
                    animator.SetFloat("Speed",0); animator.SetLayerWeight(layer,1); animator.SetFloat("ChargePhase",phase);
                    animator.Play("Locomotion",0,0); animator.Play(i<3 ? "Charge Windup" : "Heavy Attack",layer,phase);
                    animator.Update(0); await Task.Delay(60); skin.BakeMesh(mesh);
                    var bounds=new Bounds(skin.transform.TransformPoint(mesh.vertices[0]),Vector3.zero);
                    foreach(var vertex in mesh.vertices) bounds.Encapsulate(skin.transform.TransformPoint(vertex));
                    camera.orthographicSize=Mathf.Max(1,bounds.size.y*.7f);
                    camera.transform.position=bounds.center+root.transform.right*8+root.transform.forward*8+Vector3.up*1.5f;
                    camera.transform.LookAt(bounds.center); camera.Render(); RenderTexture.active=target;
                    sheet.ReadPixels(new Rect(0,0,400,400),(i%4)*400,(1-i/4)*400);
                }
                sheet.Apply(); File.WriteAllBytes(Path.GetFullPath("Tools/RightArmHeavyPreview.png"),sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
            // Exercise the real release, hit window and completion, not just sampled poses.
            attack.SuspendForSwimming(); animator.SetFloat("Speed",0); animator.Play("Locomotion",0,0); animator.Update(0);
            root.GetComponent<Mavis.PlayerStamina>().currentStamina=100;
            Call(attack,"StartCharge"); Set(attack,"chargeStartedAt",Time.time-attack.fullChargeTime);
            Call(attack,"UpdateChargePose"); animator.SetLayerWeight(layer,1); animator.Update(0);
            Call(attack,"ReleaseCharge"); Set(attack,"attackStartedAt",Time.time-1f);
            if(attack.UsesAnimationRootMotion) throw new Exception("Right-arm strike still enables full-body root motion.");
            int activeHitFrames=0;
            for(int i=0;i<180 && attack.IsHeavyAttackActive;i++)
            {
                animator.Update(1f/120); Call(attack,"UpdateAttackProgress"); Call(attack,"UpdateHitbox");
                if(attack.stickHitbox.enabled) activeHitFrames++;
                if(animator.GetCurrentAnimatorStateInfo(0).shortNameHash!=Animator.StringToHash("Locomotion")) throw new Exception("Release replaced base locomotion.");
            }
            if(attack.IsHeavyAttackActive || activeHitFrames<10 || attack.stickHitbox.enabled) throw new Exception("Heavy completion/hit window failed.");
            attack.SuspendForSwimming();
            if(animator.GetLayerWeight(layer)>.001f) throw new Exception("Cancellation did not clear arm layer.");
            return new { poses,maxUnwantedBoneDegrees=maxUnwantedAngle,rightArmPoseChangeDegrees=maxRightArmChange,
                stationaryAndRunning=true,baseLocomotionPreserved=true,rootMotionDisabled=true,activeHitFrames,
                releaseCompletes=true,cancelClearsLayer=true,preview="Tools/RightArmHeavyPreview.png" };
        }
        finally
        {
            Time.timeScale=time; RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject);
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(sheet);
        }
    }
}
