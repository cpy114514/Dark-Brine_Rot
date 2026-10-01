using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InspectDownloadedHeavy
{
    public static async Task<object> Run()
    {
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), new Vector3(0,500,0), Quaternion.identity);
        var camObj = new GameObject("Charge inspection camera");
        var lightObj = new GameObject("Charge inspection light");
        var sheet = new Texture2D(1600,800,TextureFormat.RGB24,false);
        var target = new RenderTexture(400,400,24);
        var baked = new Mesh(); var previous = RenderTexture.active; float time = Time.timeScale;
        var temporary = new AnimatorController();
        try
        {
            Time.timeScale = 0;
            root.GetComponent<ThirdPersonPlayerController>().enabled = false;
            var attack = root.GetComponent<Mavis.SahurAttack>(); attack.enabled = false;
            var animator = attack.animator;
            var guard = animator.GetComponent<SahurCombatGuardIK>(); if(guard) guard.enabled = false;
            var relay = animator.GetComponent<SahurRootMotionRelay>(); if(relay) UnityEngine.Object.DestroyImmediate(relay);
            temporary.AddLayer("Base Layer");
            var state = temporary.layers[0].stateMachine.AddState("Sample");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/DownloadedHeavy/SahurChargedSwing.anim");
            state.motion = clip;
            animator.runtimeAnimatorController = temporary; animator.Rebind();
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach(var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            var camera = camObj.AddComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.22f,.26f,.30f);
            camera.orthographic = true; camera.orthographicSize = 3.3f; camera.targetTexture = target;
            var light = lightObj.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.6f;
            light.cullingMask = 1 << 31; light.transform.rotation = Quaternion.Euler(35,-35,0);
            var report = new System.Collections.Generic.List<object>();
            for(int i=0;i<8;i++)
            {
                float phase = i/8f;
                animator.Play("Sample",0,phase); animator.Update(0); await Task.Delay(60);
                var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First(); skin.BakeMesh(baked);
                var bounds = new Bounds(skin.transform.TransformPoint(baked.vertices[0]),Vector3.zero);
                foreach(var vertex in baked.vertices) bounds.Encapsulate(skin.transform.TransformPoint(vertex));
                camera.orthographicSize = Mathf.Max(1f,bounds.size.y*.65f);
                camera.transform.position = bounds.center + root.transform.right*8 + root.transform.forward*8 + Vector3.up*1.5f; camera.transform.LookAt(bounds.center);
                camera.Render(); RenderTexture.active=target; sheet.ReadPixels(new Rect(0,0,400,400),(i%4)*400,(1-i/4)*400);
                report.Add(new { phase, hand = root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position).ToString(), bat = root.transform.InverseTransformPoint(attack.stickHitbox.transform.TransformPoint(((CapsuleCollider)attack.stickHitbox).center)).ToString() });
            }
            sheet.Apply(); File.WriteAllBytes(System.IO.Path.GetFullPath("Tools/DownloadedHeavyInspection.png"),sheet.EncodeToPNG()); return report;
        }
        finally
        {
            Time.timeScale=time; RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(camObj); UnityEngine.Object.DestroyImmediate(lightObj);
            UnityEngine.Object.DestroyImmediate(sheet); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(baked); UnityEngine.Object.DestroyImmediate(temporary);
        }
    }
}
