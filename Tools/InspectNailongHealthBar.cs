using System;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class InspectNailongHealthBar
{
    public static object Inspect()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var boss = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab"), new Vector3(0, 500, 0), Quaternion.identity);
        var oldCamera = Camera.main;
        var cameraObject = new GameObject("Health bar inspection camera");
        var lightObject = new GameObject("Health bar inspection light");
        var target = new RenderTexture(600, 600, 24);
        var frame = new Texture2D(600, 600, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            boss.GetComponent<NailongAI>().enabled = false;
            var animator = boss.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play("Idle", 0, 0f);
            animator.Update(.02f);
            var skin = boss.GetComponentInChildren<SkinnedMeshRenderer>();
            var bounds = skin.bounds;
            var camera = cameraObject.AddComponent<Camera>();
            if (oldCamera != null) oldCamera.tag = "Untagged";
            cameraObject.tag = "MainCamera";
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.17f, .21f, .26f);
            camera.orthographic = true;
            camera.orthographicSize = bounds.size.y * .72f;
            camera.targetTexture = target;
            camera.transform.position = bounds.center + new Vector3(5, 1.2f, 7);
            camera.transform.LookAt(bounds.center + Vector3.up * .4f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(30, -35, 0);
            var health = boss.GetComponent<NailongHealth>();
            health.ApplyDamage(health.maxHealth * .5f, boss.transform.position);
            var bar = boss.GetComponent<NailongHealthBar>();
            bar.Refresh(.2f);
            foreach (var child in boss.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            frame.ReadPixels(new Rect(0, 0, 600, 600), 0, 0);
            frame.Apply();
            string path = System.IO.Path.GetFullPath("Tools/NailongHealthBarInspection.png");
            System.IO.File.WriteAllBytes(path, frame.EncodeToPNG());
            return new { path, visible = bar.IsVisible, fraction = bar.DisplayedFraction };
        }
        finally
        {
            RenderTexture.active = previous;
            if (oldCamera != null) oldCamera.tag = "MainCamera";
            UnityEngine.Object.DestroyImmediate(boss);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(frame);
        }
    }
}
