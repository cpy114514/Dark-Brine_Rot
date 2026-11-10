using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ReviewBossCraftsmanship
{
    public static object Rake() => Capture(false);
    public static object Huang() => Capture(true);

    static object Capture(bool huang)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stopped editor studio review required");
        string evidence = ".codex/model-refinement/" + (huang ? "huang" : "bajie-rake");
        var reports = new List<object>();
        var preview = new PreviewRenderUtility();
        var originalMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();
        var refinedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();
        var materials = new Dictionary<SkinnedMeshRenderer, Material[]>();
        var originals = new Dictionary<SkinnedMeshRenderer, Material[]>();
        try
        {
            var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                huang ? "Assets/Resources/SecondIslandHuangMei/Huang Mei Boss.prefab" : "Assets/Resources/SecondIslandZhuBaJie/ZhuBaJie Boss.prefab"));
            preview.AddSingleGO(actor);
            var boss = actor.GetComponent<ZhuBaJieBoss>();
            if (boss)
            {
                boss.boarForm.SetActive(false);
                boss.humanForm.SetActive(true);
            }
            var model = huang ? actor.transform.Find("Huang Mei Weighted Model").gameObject :
                boss.humanForm.transform.Find("Human Model").gameObject;
            foreach (var a in actor.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            foreach (var a in actor.GetComponentsInChildren<Animation>(true)) a.enabled = false;
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in skins)
            {
                skin.updateWhenOffscreen = true;
                skin.forceMatrixRecalculationPerRender = true;
                refinedMeshes[skin] = skin.sharedMesh;
                originalMeshes[skin] = !huang && skin.name.Contains("Weapon") ?
                    AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/ZhuBaJiePigForm/Human/Models/ZhuBaJie_Human.fbx")
                        .OfType<Mesh>().Single(mesh => mesh.name == skin.sharedMesh.name || mesh.name == skin.name) : skin.sharedMesh;
                materials[skin] = skin.sharedMaterials;
                originals[skin] = skin.sharedMaterials.Select(material =>
                {
                    var original = new Material(material);
                    bool replaced = huang ? material.GetTexture("_MetallicGlossMap")?.name.StartsWith("Huang") == true :
                        material.name == "DingPa01" || material.name == "DingPa02";
                    if (replaced)
                    {
                        original.SetTexture("_MetallicGlossMap", null);
                        original.SetFloat("_Smoothness", huang ? .24f : .22f);
                        original.DisableKeyword("_METALLICSPECGLOSSMAP");
                    }
                    return original;
                }).ToArray();
            }
            preview.camera.fieldOfView = 38;
            preview.camera.nearClipPlane = .01f;
            preview.camera.farClipPlane = 200;
            preview.camera.clearFlags = CameraClearFlags.Color;
            preview.camera.backgroundColor = new Color(.055f, .065f, .080f);
            preview.ambientColor = new Color(.37f, .40f, .45f);
            preview.lights[0].intensity = 1.6f;
            preview.lights[0].transform.rotation = Quaternion.Euler(35, -35, 0);
            preview.lights[1].intensity = 1.1f;
            preview.lights[1].transform.rotation = Quaternion.Euler(18, 140, 0);
            foreach (var (view, clip, t, direction) in new[]
            {
                ("front", huang ? "HuangIdle" : "BajieIdle", .2f, new Vector3(.12f,.10f,-1)),
                ("side", huang ? "HuangIdle" : "BajieIdle", .2f, new Vector3(1,.10f,0)),
                ("back", huang ? "HuangIdle" : "BajieIdle", .2f, new Vector3(.1f,.10f,1)),
                ("action", huang ? "HuangSweep" : "BajieRake", 1.05f, new Vector3(.65f,.18f,-1)),
                ("detail", huang ? "HuangIdle" : "BajieIdle", .2f, new Vector3(.18f,.10f,-1))
            })
            {
                var motion = Resources.Load<AnimationClip>("SecondIslandMotion/" + clip);
                if (!motion) throw new InvalidOperationException("Missing authored motion " + clip);
                motion.SampleAnimation(model, t);
                var transforms = model.GetComponentsInChildren<Transform>();
                var left = transforms.First(bone => bone.name == "clavicle_l");
                var right = transforms.First(bone => bone.name == "clavicle_r");
                var shoulderAxis = Vector3.ProjectOnPlane(right.position - left.position, Vector3.up).normalized;
                var facing = Vector3.Cross(shoulderAxis, Vector3.up).normalized;
                var renderers = model.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                if (view == "detail")
                {
                    if (huang)
                    {
                        var head = model.GetComponentsInChildren<Transform>().First(bone => bone.name == "head");
                        bounds = new Bounds(head.position, Vector3.one * bounds.size.y * .27f);
                    }
                    else
                    {
                        var rake = skins.Single(skin => skin.name.Contains("Weapon"));
                        var points = rake.sharedMesh.vertices;
                        var indices = rake.sharedMesh.GetIndices(1);
                        var matrix = rake.bones[1].localToWorldMatrix * rake.sharedMesh.bindposes[1];
                        bounds = new Bounds(matrix.MultiplyPoint3x4(points[indices[0]]), Vector3.zero);
                        foreach (int i in indices) bounds.Encapsulate(matrix.MultiplyPoint3x4(points[i]));
                    }
                }
                var viewDirection = shoulderAxis * direction.x + Vector3.up * direction.y - facing * direction.z;
                preview.camera.transform.position = bounds.center + viewDirection.normalized *
                    bounds.size.magnitude * (view == "detail" ? 1.8f : 1.22f);
                preview.camera.transform.LookAt(bounds.center);
                var pose = model.GetComponentsInChildren<Transform>().Select(bone => bone.localToWorldMatrix).ToArray();
                foreach (bool refined in new[] { false, true })
                {
                    foreach (var skin in skins)
                    {
                        skin.sharedMesh = refined ? refinedMeshes[skin] : originalMeshes[skin];
                        skin.sharedMaterials = refined ? materials[skin] : originals[skin];
                    }
                    preview.BeginStaticPreview(new Rect(0, 0, 960, 960));
                    preview.Render(true);
                    var image = preview.EndStaticPreview();
                    File.WriteAllBytes(evidence + "/studio-" + view + (refined ? "-after" : "-before") + ".png", image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
                if (!pose.SequenceEqual(model.GetComponentsInChildren<Transform>().Select(bone => bone.localToWorldMatrix)))
                    throw new InvalidOperationException("Studio comparison changed production poses");
                reports.Add(new { view, clip, t, identicalPoseMatrices = true });
            }
            File.WriteAllText(evidence + "/unity-studio.json", Newtonsoft.Json.JsonConvert.SerializeObject(reports, Newtonsoft.Json.Formatting.Indented));
            return reports;
        }
        finally
        {
            preview.Cleanup();
            foreach (var mat in originals.Values.SelectMany(mats => mats)) UnityEngine.Object.DestroyImmediate(mat);
        }
    }
}
