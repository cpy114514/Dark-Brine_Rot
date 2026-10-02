using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class InspectDiagonalStrike
{
    public static async Task<object> Run(int choice=0)
    {
        if(!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),new Vector3(0,500,0),Quaternion.identity);
        var temp=new AnimatorController();var camObject=new GameObject("Diagonal strike camera");var lightObject=new GameObject("Diagonal strike light");
        var target=new RenderTexture(500,600,24);var sheet=new Texture2D(3000,1200,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;float time=Time.timeScale;
        try{
            Time.timeScale=0;root.GetComponent<ThirdPersonPlayerController>().enabled=false;root.GetComponent<CharacterController>().enabled=false;
            var attack=root.GetComponent<Mavis.SahurAttack>();attack.enabled=false;var animator=attack.animator;
            animator.GetComponent<SahurRootMotionRelay>().enabled=false;animator.GetComponent<SahurCombatGuardIK>().enabled=false;
            var live=(AnimatorController)animator.runtimeAnimatorController;
            var idle=((BlendTree)live.layers[0].stateMachine.states.Single(s=>s.state.name=="Locomotion").state.motion).children[0].motion;
            temp.AddLayer("Base Layer");temp.layers[0].stateMachine.AddState("Idle").motion=idle;
            temp.AddLayer("Right arm");var layers=temp.layers;layers[1].avatarMask=live.layers[1].avatarMask;layers[1].defaultWeight=1;temp.layers=layers;
            var sample=temp.layers[1].stateMachine.AddState("Sample");
            sample.motion=choice==0?AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/DownloadedHeavy/SahurChargedSwing.anim"):
                choice==1?AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Source/SwordAndShieldSlash_ThreeHit.fbx").OfType<AnimationClip>().Single(c=>c.name=="SahurSwordCombo1"):
                choice<4?AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/DownloadedHeavy/UAL2_HeavySource.fbx").OfType<AnimationClip>().Single(c=>c.name=="__preview__Armature|Sword_Regular_"+(choice==2?"A":"B")):
                choice<6?AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Source/SwordAndShieldSlash_ThreeHit.fbx").OfType<AnimationClip>().Single(c=>c.name=="SahurSwordCombo"+(choice-2)):
                choice==6?AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/KayKitSkeletons/SahurStickCombo1.anim"):
                AssetDatabase.LoadAllAssetsAtPath(choice==8?"Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx":"Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/GreatSwordSlash.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            animator.runtimeAnimatorController=temp;animator.Rebind();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
            var cam=camObject.AddComponent<Camera>();cam.enabled=false;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.18f,.22f,.25f);cam.orthographic=true;cam.orthographicSize=4.5f;cam.targetTexture=target;
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(35,-35,0);
            animator.Play("Idle",0,0);animator.Update(0);
            var focus=(animator.GetBoneTransform(HumanBodyBones.Head).position+animator.GetBoneTransform(HumanBodyBones.RightFoot).position)*.5f+Vector3.up;
            cam.transform.position=focus+new Vector3(8,1,10);cam.transform.LookAt(focus);
            float[] phases=choice==0?new[]{0,.058f,.116071f,.14f,.16f,.18f,.20f,.22f,.24f,.26f,.30f,.80f}:new[]{0,.10f,.20f,.25f,.30f,.35f,.40f,.50f,.60f,.70f,.80f,.95f};var report=new List<object>();
            var cap=(CapsuleCollider)attack.stickHitbox;
            for(int k=0;k<phases.Length;k++){
                animator.Play("Idle",0,0);animator.Play("Sample",1,phases[k]);animator.Update(0);await Task.Delay(80);
                var head=animator.GetBoneTransform(HumanBodyBones.Head);var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
                Vector3 axis=cap.direction==0?Vector3.right:cap.direction==1?Vector3.up:Vector3.forward;
                Vector3 center=cap.transform.TransformPoint(cap.center);Vector3 extent=cap.transform.TransformVector(axis*Mathf.Max(0,cap.height*.5f-cap.radius));
                var a=center-extent;var d=2*extent;float distance=Vector3.Distance(head.position,a+d*Mathf.Clamp01(Vector3.Dot(head.position-a,d)/d.sqrMagnitude));
                report.Add(new{phase=phases[k],seconds=phases[k]*((AnimationClip)sample.motion).length,hand=root.transform.InverseTransformPoint(hand.position).ToString(),head=root.transform.InverseTransformPoint(head.position).ToString(),clubAxis=root.transform.InverseTransformDirection(extent.normalized).ToString(),headDistance=distance});
                cam.Render();RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,500,600),(k%6)*500,(1-k/6)*600);
            }
            sheet.Apply();File.WriteAllBytes(Path.GetFullPath("Tools/SahurDiagonalSource"+choice+".png"),sheet.EncodeToPNG());return new{choice,clip=sample.motion.name,report};
        }
        finally{Time.timeScale=time;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(temp);UnityEngine.Object.DestroyImmediate(camObject);UnityEngine.Object.DestroyImmediate(lightObject);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(sheet);}
    }
}
