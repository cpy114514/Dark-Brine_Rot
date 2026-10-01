using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Mavis;
public static class InspectSahurThrow
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play mode.");
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), new Vector3(0,500,0), Quaternion.identity);
        var cameraObject = new GameObject("Throw inspection camera");
        var lightObject = new GameObject("Throw inspection light");
        var sheet = new Texture2D(1600,400,TextureFormat.RGB24,false);
        var target = new RenderTexture(400,400,24);
        var baked = new Mesh();
        var previous = RenderTexture.active;
        float previousTime = Time.timeScale;
        try
        {
            var attack = root.GetComponent<SahurAttack>();
            var boom = root.GetComponent<SahurBoomerang>();
            var animator = attack.animator;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            var relay = animator.GetComponent<SahurRootMotionRelay>();
            if (relay) UnityEngine.Object.DestroyImmediate(relay);
            if (!boom.TryThrow()) throw new Exception("Throw refused.");
            Time.timeScale = 0f;
            foreach(var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.22f,.26f,.30f);
            camera.orthographic = true; camera.orthographicSize = 3.3f; camera.targetTexture = target;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.6f; light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(35,-35,0);
            var report = new System.Collections.Generic.List<object>();
            float[] phases = { .05f, .25f, .30f, .55f };
            for(int i=0;i<phases.Length;i++)
            {
                animator.Play("Boomerang Throw",0,phases[i]); animator.Update(0);
                boom.Simulate(.0001f);
                await Task.Delay(60);
                if (boom.IsFlying)
                    foreach(var child in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Sahur Boomerang (Flying)"))
                        foreach(var part in child.GetComponentsInChildren<Transform>()) part.gameObject.layer=31;
                var skin = animator.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.vertexCount).First();
                skin.BakeMesh(baked);
                var bounds = new Bounds(skin.transform.TransformPoint(baked.vertices[0]),Vector3.zero);
                foreach(var vertex in baked.vertices) bounds.Encapsulate(skin.transform.TransformPoint(vertex));
                camera.orthographicSize = Mathf.Max(1f,bounds.size.y * .7f);
                camera.transform.position = bounds.center + root.transform.right * 6 + root.transform.forward * 8 + Vector3.up * 1.5f;
                camera.transform.LookAt(bounds.center);
                camera.Render(); RenderTexture.active = target;
                sheet.ReadPixels(new Rect(0,0,400,400),i*400,0);
                report.Add(new { phase = phases[i], flying = boom.IsFlying, hand = root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position).ToString() });
            }
            sheet.Apply(); File.WriteAllBytes(Path.GetFullPath("Tools/SahurThrowInspection.png"),sheet.EncodeToPNG());
            return report;
        }
        finally
        {
            RenderTexture.active = previous;
            Time.timeScale = previousTime;
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(sheet); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(baked);
        }
    }
}
