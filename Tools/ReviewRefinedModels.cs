using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReviewRefinedModels
{
    const string Evidence = ".codex/model-refinement/hero-surfaces";
    const string Art = "Assets/Game/Art/ModelRefinement/";

    public static object MeasureMemory()
    {
        var original = AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Models/stick.fbx")
            .OfType<Mesh>().Single(mesh => mesh.name == "node_0");
        var refined = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "Sahur Stick Worked Wood.asset");
        var names = new[]
        {
            "SahurStickColour.png", "SahurStickNormal.png", "SahurWoodSmoothness.png",
            "CapriPaperAndSleeveNormal.png", "NailongClothAndSkinSmoothness.png"
        };
        var textures = names.Select(name =>
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name);
            return new { name, texture.width, texture.height, texture.mipmapCount,
                format = texture.graphicsFormat.ToString(), bytes = Profiler.GetRuntimeMemorySizeLong(texture) };
        }).ToArray();
        var result = new { originalStickBytes = Profiler.GetRuntimeMemorySizeLong(original),
            refinedStickBytes = Profiler.GetRuntimeMemorySizeLong(refined), textures };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "/asset-memory.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    public static object Run()
    {
        if (!Application.isPlaying || AdditiveSceneBootstrap.IsLoading)
            throw new InvalidOperationException("Load First Island in isolated Play");
        Directory.CreateDirectory(Evidence);
        var report = new List<object>();
        var motor = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var arrival = UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
        if (arrival)
        {
            arrival.enabled = false;
            arrival.RestoreProgress(true, true);
        }
        var terrain = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<Collider>();
        if (!terrain.Raycast(new Ray(new Vector3(288, 150, 346), Vector3.down), out var ground, 400))
            throw new InvalidOperationException("Missing model review ground");
        motor.RestoreSavedPose(ground.point + Vector3.up * (-motor.LowestFootWorldOffset + .05f), Quaternion.identity);
        motor.GetComponent<SahurWeaponLoadout>().Restore(true, SahurWeaponLoadout.Weapon.Stick, false);
        var boss = UnityEngine.Object.FindFirstObjectByType<CappuccinoAI>();
        var nailong = UnityEngine.Object.FindFirstObjectByType<NailongAI>();
        var go = new GameObject("Refinement review camera", typeof(Camera));
        var camera = go.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 38;
        var target = new RenderTexture(960, 960, 24);
        var screenshot = new Texture2D(960, 960, TextureFormat.RGB24, false);
        var oldRt = RenderTexture.active;
        float time = Time.timeScale;
        var manager = Mavis.VisualStyles.VisualStyleManager.Instance;
        Time.timeScale = 0;
        manager.Select("realistic", false);
        try
        {
            foreach (string name in new[] { "Sahur", "Cappuccino", "Nailong" })
            {
                bool player = name == "Sahur";
                bool capri = name == "Cappuccino";
                var actor = player ? motor.gameObject : capri ? boss.gameObject : nailong.gameObject;
                var skin = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .First(renderer => renderer.sharedMesh.name == (player ? "SahurFingerSkinnedMesh" : capri ? "Capri Body Rigged" : "Mesh_0.001"));
                var anim = player ? motor.CharacterAnimator : capri ? actor.GetComponent<CappuccinoEnemy>().animator : nailong.animator;
                var refinedSkin = skin.sharedMaterial;
                var originalSkin = new Material(refinedSkin);
                var stick = actor.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(filter => filter.name == "Sahur Stick");
                var refinedStick = stick ? stick.sharedMesh : null;
                var refinedStickMaterial = stick ? stick.GetComponent<Renderer>().sharedMaterial : null;
                var originalStick = player ? AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Models/stick.fbx")
                    .OfType<Mesh>().Single(mesh => mesh.name == "node_0") : null;
                var originalStickMaterial = stick ? new Material(refinedStickMaterial) : null;
                var savedCulling = anim.cullingMode;
                bool savedForce = skin.forceMatrixRecalculationPerRender;
                bool savedOffscreen = skin.updateWhenOffscreen;
                float savedSpeed = anim.speed;
                try
                {
                    skin.updateWhenOffscreen = true;
                    skin.forceMatrixRecalculationPerRender = true;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    if (player)
                    {
                        anim.SetFloat("Speed", 0);
                        anim.Play("Base Layer.Locomotion", 0, .2f);
                    }
                    else if (capri) SetCapriPose(actor, "Idle", .15f);
                    else anim.Play("Base Layer.Idle", 0, .2f);
                    anim.Update(0);
                    if (player)
                    {
                        originalSkin.SetTexture("_MetallicGlossMap", null);
                        originalSkin.DisableKeyword("_METALLICSPECGLOSSMAP");
                        originalSkin.SetFloat("_Smoothness", .42f);
                    }
                    else if (capri)
                    {
                        originalSkin.SetTexture("_BumpMap", null);
                        originalSkin.DisableKeyword("_NORMALMAP");
                    }
                    else
                    {
                        originalSkin.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                            "Assets/Game/Prefabs/Bosses/Nailong/Textures/texture_pbr_20250901_metallic.png"));
                        originalSkin.SetFloat("_Smoothness", .35f);
                        originalSkin.SetFloat("_Metallic", .2f);
                    }
                    if (originalStickMaterial)
                    {
                        originalStickMaterial.SetTexture("_BaseMap", null);
                        originalStickMaterial.SetTexture("_BumpMap", null);
                        originalStickMaterial.DisableKeyword("_NORMALMAP");
                        originalStickMaterial.SetColor("_BaseColor", new Color(.2f, .075f, .022f));
                        originalStickMaterial.SetFloat("_Smoothness", .24f);
                    }

                    foreach (var (view, direction) in new[]
                    {
                        ("front", Vector3.forward + Vector3.up * .12f),
                        ("side", Vector3.right + Vector3.up * .12f),
                        ("back", Vector3.back + Vector3.up * .12f),
                        ("stride", new Vector3(.48f,.22f,1)),
                        ("action", new Vector3(.48f,.22f,1))
                    })
                    {
                        if (view == "stride")
                        {
                            if (player)
                            {
                                anim.SetFloat("Speed", 9.6f);
                                anim.Play("Base Layer.Locomotion", 0, .38f);
                                anim.Update(0);
                            }
                            else if (capri) SetCapriPose(actor, "CapriP2Dive", .36f);
                            else
                            {
                                anim.Play("Base Layer.Walk", 0, .36f);
                                anim.Update(0);
                            }
                        }
                        if (view == "action")
                        {
                            if (player)
                            {
                                anim.Play("Base Layer.Combo 1", 0, .65f);
                                anim.Update(0);
                            }
                            else if (capri) SetCapriPose(actor, "CapriP2Transition", .58f);
                            else
                            {
                                anim.Play("Base Layer.Stumble", 0, .48f);
                                anim.Update(0);
                            }
                        }
                        var collision = actor.GetComponent<CharacterController>();
                        var bounds = collision ? collision.bounds : skin.bounds;
                        float height = bounds.size.y;
                        var center = bounds.center;
                        camera.transform.position = center + actor.transform.TransformDirection(direction).normalized * (height * 2.0f);
                        camera.transform.LookAt(center);
                        var pose = skin.bones.Select(bone => bone ? bone.localToWorldMatrix : Matrix4x4.zero).ToArray();
                        foreach (bool refined in new[] { false, true })
                        {
                            skin.sharedMaterial = refined ? refinedSkin : originalSkin;
                            if (stick)
                            {
                                stick.sharedMesh = refined ? refinedStick : originalStick;
                                stick.GetComponent<Renderer>().sharedMaterial = refined ? refinedStickMaterial : originalStickMaterial;
                            }
                            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                            RenderTexture.active = target;
                            screenshot.ReadPixels(new Rect(0, 0, 960, 960), 0, 0);
                            screenshot.Apply();
                            File.WriteAllBytes(Evidence + "/" + name + "-" + view + (refined ? "-after" : "-before") + ".png", screenshot.EncodeToPNG());
                        }
                        if (!pose.SequenceEqual(skin.bones.Select(bone => bone ? bone.localToWorldMatrix : Matrix4x4.zero)))
                            throw new InvalidOperationException("Material comparison changed the rig pose");
                    }
                    report.Add(new { name, mesh = skin.sharedMesh.name, bones = skin.bones.Length, skin.sharedMesh.vertexCount,
                        blendShapes = skin.sharedMesh.blendShapeCount, avatarValid = anim.avatar && anim.avatar.isValid,
                        tangents = skin.sharedMesh.HasVertexAttribute(VertexAttribute.Tangent),
                        stickTriangles = stick ? stick.sharedMesh.GetIndexCount(0) / 3 : 0, poseMatricesIdentical = true });
                }
                finally
                {
                    skin.sharedMaterial = refinedSkin;
                    skin.updateWhenOffscreen = savedOffscreen;
                    skin.forceMatrixRecalculationPerRender = savedForce;
                    anim.speed = savedSpeed;
                    anim.cullingMode = savedCulling;
                    if (stick)
                    {
                        stick.sharedMesh = refinedStick;
                        stick.GetComponent<Renderer>().sharedMaterial = refinedStickMaterial;
                    }
                    UnityEngine.Object.DestroyImmediate(originalSkin);
                    if (originalStickMaterial) UnityEngine.Object.DestroyImmediate(originalStickMaterial);
                }
            }
        }
        finally
        {
            RenderTexture.active = oldRt;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(screenshot);
            UnityEngine.Object.DestroyImmediate(go);
            Time.timeScale = time;
            manager.RestorePreference();
        }
        File.WriteAllText(Evidence + "/matched-models.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }

    static void SetCapriPose(GameObject actor, string state, float phase) =>
        typeof(CappuccinoEnemy).GetMethod("SetPhaseTwoPose", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(actor.GetComponent<CappuccinoEnemy>(), new object[] { state, phase });
}
