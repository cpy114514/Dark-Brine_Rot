using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class InspectNailongLootModels
{
    public static object Inspect()
    {
        string[] files = { "20260930130250_547090e7", "20260930130258_d69f77fb", "20260930131057_77c279ab" };
        var cameraObject = new GameObject("Loot inspection camera");
        var lightObject = new GameObject("Loot inspection light");
        var target = new RenderTexture(500, 500, 24);
        var image = new Texture2D(1500, 500, TextureFormat.RGB24, false);
        var frame = new Texture2D(500, 500, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var instances = new List<GameObject>();
        var report = new List<object>();
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.16f, .2f, .24f);
            camera.targetTexture = target;
            camera.orthographic = true;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.6f;
            light.transform.rotation = Quaternion.Euler(35, -30, 0); light.cullingMask = 1 << 31;
            for (int i = 0; i < 3; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Equipment/Nailong/" + files[i] + ".fbx");
                var obj = Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
                instances.Add(obj);
                foreach (var child in obj.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
                var renderer = obj.GetComponentInChildren<Renderer>();
                var bounds = renderer.bounds;
                float scale = 2f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                obj.transform.localScale *= scale;
                bounds = renderer.bounds;
                camera.orthographicSize = 1.45f;
                camera.transform.position = bounds.center + new Vector3(2, 1.3f, 3);
                camera.transform.LookAt(bounds.center);
                camera.Render();
                RenderTexture.active = target;
                frame.ReadPixels(new Rect(0, 0, 500, 500), 0, 0); frame.Apply();
                image.SetPixels(i * 500, 0, 500, 500, frame.GetPixels());
                report.Add(new { file = files[i], normalizeScale = scale, size = bounds.size.ToString(),
                    textures = AssetDatabase.FindAssets("t:Texture", new[] { "Assets/Game/Equipment/Nailong/Textures/" + files[i] }) });
                obj.SetActive(false);
            }
            image.Apply();
            string path = System.IO.Path.GetFullPath("Tools/NailongLootModels.png");
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            return new { path, report };
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var obj in instances) Object.DestroyImmediate(obj);
            Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject);
            Object.DestroyImmediate(target); Object.DestroyImmediate(frame); Object.DestroyImmediate(image);
        }
    }
}
