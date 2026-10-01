using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class InspectNailongCombat
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static object Inspect()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var boss = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        var player = new GameObject("Nailong visual test target");
        player.transform.position = boss.transform.position + Vector3.forward * 8f;
        var cameraObject = new GameObject("Nailong inspection camera");
        var lightObject = new GameObject("Nailong inspection light");
        var target = new RenderTexture(400, 400, 24);
        var frame = new Texture2D(400, 400, TextureFormat.RGB24, false);
        var sheet = new Texture2D(1200, 1200, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var extras = new List<UnityEngine.Object>();
        try
        {
            var ai = boss.GetComponent<NailongAI>();
            ai.enabled = false;
            var animator = ai.animator;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var motion = boss.GetComponent<NailongAttackMotion>();
            var effects = boss.GetComponent<NailongCombatEffects>();
            var skins = boss.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var obj in boss.GetComponentsInChildren<Transform>(true)) obj.gameObject.layer = 31;
            var camera = cameraObject.AddComponent<Camera>();
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.17f, .21f, .26f);
            camera.orthographic = true;
            camera.orthographicSize = 2.5f;
            camera.targetTexture = target;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(30, -35, 0);
            var report = new List<object>();
            var rows = new[] { NailongAttackMotion.Style.Flurry, NailongAttackMotion.Style.Roar, NailongAttackMotion.Style.PointAndSpit };
            var times = new[] { new[] { .20f, .45f, .73f }, new[] { .55f, 1.01f, 1.40f }, new[] { .50f, .95f, 1.25f } };
            for (int row = 0; row < 3; row++)
            {
                motion.Stop();
                motion.Begin(rows[row], row == 0 ? 1.89f : row == 1 ? 1.7f : 2.16f);
                motion.ConfigureFlurry(ai.flurryWindup, ai.flurryInterval, ai.flurryCount);
                motion.ConfigureCue(row == 1 ? ai.roarWindup : ai.spitWindup);
                for (int col = 0; col < 3; col++)
                {
                    animator.Play("Idle", 0, 0f);
                    animator.Update(.02f);
                    typeof(NailongAttackMotion).GetField("startedAt", Private).SetValue(motion, Time.time - times[row][col]);
                    typeof(NailongAttackMotion).GetMethod("LateUpdate", Private).Invoke(motion, null);
                    if (row == 1 && col == 1)
                    {
                        effects.Roar(ai.roarRadius);
                        typeof(NailongCombatEffects).GetField("waveStarted", Private).SetValue(effects, Time.time - .12f);
                        typeof(NailongCombatEffects).GetMethod("LateUpdate", Private).Invoke(effects, null);
                    }
                    else if (row == 2 && col > 0)
                    {
                        var projectile = effects.Spit(player.transform, ai.spitSpeed, 0f);
                        projectile.gameObject.layer = 31;
                        projectile.Simulate(.025f);
                        extras.Add(projectile.gameObject);
                    }
                    var bakedObjects = new List<GameObject>();
                    var meshes = new List<Mesh>();
                    Bounds bounds = new Bounds();
                    bool initialized = false;
                    foreach (var skin in skins)
                    {
                        var mesh = new Mesh();
                        skin.BakeMesh(mesh, true);
                        var obj = new GameObject("Nailong baked inspection pose");
                        extras.Add(obj);
                        extras.Add(mesh);
                        obj.layer = 31;
                        obj.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                        obj.transform.localScale = skin.transform.lossyScale;
                        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = obj.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = skin.sharedMaterials;
                        if (!initialized) { bounds = renderer.bounds; initialized = true; }
                        else bounds.Encapsulate(renderer.bounds);
                        skin.enabled = false;
                        bakedObjects.Add(obj);
                        meshes.Add(mesh);
                    }
                    camera.orthographicSize = Mathf.Max(2f, bounds.size.y * .65f);
                    camera.transform.position = bounds.center + new Vector3(5, 1.2f, 7);
                    camera.transform.LookAt(bounds.center);
                    camera.Render();
                    RenderTexture.active = target;
                    frame.ReadPixels(new Rect(0, 0, 400, 400), 0, 0);
                    frame.Apply();
                    sheet.SetPixels32(col * 400, (2 - row) * 400, 400, 400, frame.GetPixels32());
                    foreach (var skin in skins) skin.enabled = true;
                    foreach (var obj in bakedObjects) UnityEngine.Object.DestroyImmediate(obj);
                    foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
                    report.Add(new { style = rows[row].ToString(), elapsed = times[row][col], height = bounds.size.y, mouth = (motion.MouthPosition - boss.transform.position).ToString() });
                }
            }
            sheet.Apply();
            string path = System.IO.Path.GetFullPath("Tools/NailongCombatInspection.png");
            System.IO.File.WriteAllBytes(path, sheet.EncodeToPNG());
            return new { path, rows = "Flurry, Roar, PointAndSpit", report };
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var obj in extras) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(boss);
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(frame);
            UnityEngine.Object.DestroyImmediate(sheet);
        }
    }
}
