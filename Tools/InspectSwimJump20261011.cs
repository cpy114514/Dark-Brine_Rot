using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectSwimJump20261011
{
    public static object Inspect()
    {
        var live=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        object liveState=live==null ? null : new {position=live.transform.position.ToString(), swimming=live.Swimming,
            velocity=live.WorldVelocity.ToString(), controller=live.CharacterAnimator?.runtimeAnimatorController?.name,
            state=live.CharacterAnimator?.GetCurrentAnimatorStateInfo(0).shortNameHash,
            movementLock=live.ExternalMovementLock, controlLock=live.ExternalControlLock};
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var holder=new GameObject("Temporary swimming/jump pose inspection");holder.SetActive(false);
        holder.transform.position=new Vector3(3000,500,3000);
        try
        {
            var visual=UnityEngine.Object.Instantiate(prefab.transform.Find("Pbr Sahur Visual").gameObject,holder.transform);
            foreach(var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            var animator=visual.GetComponent<Animator>();var root=prefab.GetComponent<Animator>();
            animator.avatar=root.avatar;animator.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("Encounter/v006/SahurGameplay");
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;
            holder.SetActive(true);animator.Rebind();animator.Update(0);
            var samples=new System.Collections.Generic.List<object>();
            var controller=animator.runtimeAnimatorController;
            foreach(string variant in new[]{"Original","Body","Feet"})
            {
            if(variant!="Original" && !System.IO.File.Exists($"Assets/AnimationStaging/SwimJumpFix20261011/PLAYER_JumpStart_v006_{variant}.fbx"))continue;
            AnimatorOverrideController replacement=null;
            if(variant!="Original")
            {
                replacement=new AnimatorOverrideController(controller);
                foreach(string clipName in new[]{"PLAYER_JumpStart_v006","PLAYER_JumpAir_v006","PLAYER_JumpLand_v006"})
                {
                    var clip=AssetDatabase.LoadAllAssetsAtPath($"Assets/AnimationStaging/SwimJumpFix20261011/{clipName}_{variant}.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
                    replacement[clipName]=clip;
                }
                animator.runtimeAnimatorController=replacement;
            }
            foreach(var name in (variant=="Original" ? new[]{"Locomotion","Swim Idle","Swim Forward","Swim Fast","Jump Start","Jump Loop","Land"} : new[]{"Jump Start","Jump Loop","Land"}))
            foreach(float phase in new[]{0f,.25f,.5f,.75f,.99f})
            {
                animator.Play(name,0,phase);animator.Update(0);
                var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var head=animator.GetBoneTransform(HumanBodyBones.Head);
                var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var feet=(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position+animator.GetBoneTransform(HumanBodyBones.RightFoot).position)*.5f;
                samples.Add(new{variant,name,phase,headAhead=Vector3.Dot(head.position-hips.position,holder.transform.forward),
                    shouldersForward=Vector3.Dot(Vector3.Cross(right.position-left.position,Vector3.up).normalized,holder.transform.forward),
                    headY=head.position.y-holder.transform.position.y,feetY=feet.y-holder.transform.position.y,
                    clip=animator.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name,visualYaw=visual.transform.localEulerAngles.y});
            }
            animator.runtimeAnimatorController=controller;
            if(replacement!=null)UnityEngine.Object.DestroyImmediate(replacement);
            }
            return new{liveState,samples};
        }
        finally{UnityEngine.Object.DestroyImmediate(holder);}
    }
    public static object Preview()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var holder=new GameObject("Temporary swim jump visual review");holder.SetActive(false);holder.transform.position=new Vector3(3000,500,3000);
        var render=new RenderTexture(800,600,24);var texture=new Texture2D(800,600,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try
        {
            var visual=UnityEngine.Object.Instantiate(prefab.transform.Find("Pbr Sahur Visual").gameObject,holder.transform);
            foreach(var b in visual.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
            foreach(var c in visual.GetComponentsInChildren<Collider>(true))c.enabled=false;
            var animator=visual.GetComponent<Animator>();animator.avatar=prefab.GetComponent<Animator>().avatar;
            animator.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("Encounter/v006/SahurGameplay");animator.applyRootMotion=false;animator.enabled=true;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(holder.transform,false);
            floor.transform.localPosition=new Vector3(0,-1.12f,0);floor.transform.localScale=new Vector3(4,.1f,4);
            var cameraObject=new GameObject("Review camera");cameraObject.transform.SetParent(holder.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.17f,.19f,.21f);camera.fieldOfView=34;camera.targetTexture=render;
            var lightObject=new GameObject("Review light");lightObject.transform.SetParent(holder.transform,false);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<31;
            lightObject.transform.rotation=Quaternion.Euler(40,-25,0);
            foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            holder.SetActive(true);animator.Rebind();
            var result=new System.Collections.Generic.List<string>();
            foreach(var sample in new[]{("Swimming",0f,.25f),("Swimming",1f,.25f),("Jump Loop",0f,.5f),("Land",0f,.25f)})
            {
                floor.SetActive(sample.Item1!="Swimming");
                animator.SetFloat("SwimAmount",1);animator.SetFloat("SwimFastBlend",sample.Item2);animator.Play(sample.Item1,0,sample.Item3);animator.Update(0);
                // Multiple native renders in one Editor frame can reuse GPU
                // skinning data. Render a baked snapshot of each sampled pose.
                var snapshots=new System.Collections.Generic.List<GameObject>();var meshes=new System.Collections.Generic.List<Mesh>();
                var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach(var skin in skins)
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
                    var snapshot=new GameObject("Sampled skin");snapshot.transform.SetParent(skin.transform,false);snapshot.layer=31;
                    snapshot.AddComponent<MeshFilter>().sharedMesh=mesh;snapshot.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                    skin.enabled=false;snapshots.Add(snapshot);
                }
                Vector3 target=holder.transform.position+new Vector3(0,-.42f,0);
                var offset=new Vector3(2.5f,1,3.5f).normalized*4;
                camera.transform.SetPositionAndRotation(target+offset,Quaternion.LookRotation(-offset));camera.Render();
                RenderTexture.active=render;texture.ReadPixels(new Rect(0,0,800,600),0,0);texture.Apply();
                string path=$".codex/swim-jump-fix-20261011/{sample.Item1.Replace(" ","-")}-{sample.Item2}.png";
                System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());result.Add(path);
                foreach(var snapshot in snapshots)UnityEngine.Object.DestroyImmediate(snapshot);
                foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
                foreach(var skin in skins)skin.enabled=true;
            }
            return result;
        }
        finally
        {
            RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(holder);UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
