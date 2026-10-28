using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PreviewChickenGait
{
    const string ModelPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.fbx";
    const string PrefabPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab";
    const string ClipPath = "Assets/Game/Prefabs/Enemies/ChickenBro/Animations/UAL1_Standard.fbx";
    const string Folder = "Temp/chicken-pose/gait";

    public static string Capture()
    {
        Directory.CreateDirectory(Folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var material = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial;
        var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        SceneManager.MoveGameObjectToScene(model, scene);
        model.transform.localScale = Vector3.one * 2.8608f;
        model.transform.position = new Vector3(0f, 0.7371f, 0f);

        var animator = model.GetComponentInChildren<Animator>(true);
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
        renderer.sharedMaterial = material;
        renderer.updateWhenOffscreen = true;

        var controller = new AnimatorController { hideFlags = HideFlags.HideAndDontSave };
        controller.AddLayer("Base Layer");
        var machine = controller.layers[0].stateMachine;
        var clips = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
        foreach (var clip in clips)
        {
            var state = machine.AddState(clip.name);
            state.motion = clip;
        }
        animator.runtimeAnimatorController = controller;

        var lightGo = new GameObject("key");
        SceneManager.MoveGameObjectToScene(lightGo, scene);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.2f;
        light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

        string[] names = { "Armature|Idle_Loop", "Armature|Walk_Loop", "Armature|Jog_Fwd_Loop", "Armature|Sprint_Loop" };
        try
        {
            var report = "";
            foreach (var name in names)
            {
                animator.Rebind();
                animator.Play(name, 0, 0.3f);
                animator.Update(0f);
                model.transform.SetPositionAndRotation(new Vector3(0f, 0.7371f, 0f), Quaternion.identity);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var chest = head.position - hips.position;
                float lean = Mathf.Atan2(Vector3.Dot(chest, hips.forward), Vector3.Dot(chest, Vector3.up)) * Mathf.Rad2Deg;
                var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                var right = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                float stride = Vector3.Dot(left - right, hips.forward);
                report += name.Replace("Armature|", "")
                    + " lean=" + lean.ToString("0", CultureInfo.InvariantCulture)
                    + " stride=" + stride.ToString("0.00", CultureInfo.InvariantCulture) + " | ";
                Shoot(scene, model, renderer, name.Replace("Armature|", ""));
            }
            return report;
        }
        finally
        {
            Object.DestroyImmediate(model);
            Object.DestroyImmediate(controller);
            Object.DestroyImmediate(lightGo);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static void Shoot(Scene scene, GameObject model, SkinnedMeshRenderer renderer, string name)
    {
        var baked = new Mesh();
        renderer.BakeMesh(baked);
        var show = new GameObject("baked");
        SceneManager.MoveGameObjectToScene(show, scene);
        show.transform.SetParent(renderer.transform, false);
        show.AddComponent<MeshFilter>().sharedMesh = baked;
        var visible = show.AddComponent<MeshRenderer>();
        visible.sharedMaterial = renderer.sharedMaterial;
        renderer.enabled = false;
        var bounds = visible.bounds;
        var focus = bounds.center;
        var size = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * 1.2f;
        var hips = model.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Hips);
        var forward = Vector3.ProjectOnPlane(hips.forward, Vector3.up).normalized;
        var right = Vector3.Cross(Vector3.up, forward);
        Save(scene, name + "-face", focus + forward * 12f, focus, size);
        Save(scene, name + "-side", focus + right * 12f, focus, size);
        renderer.enabled = true;
        Object.DestroyImmediate(show);
        Object.DestroyImmediate(baked);
    }

    static void Save(Scene scene, string name, Vector3 position, Vector3 focus, float size)
    {
        var cameraGo = new GameObject("camera");
        SceneManager.MoveGameObjectToScene(cameraGo, scene);
        var camera = cameraGo.AddComponent<Camera>();
        camera.enabled = false;
        camera.scene = scene;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 50f;
        camera.transform.position = position;
        camera.transform.LookAt(focus, Vector3.up);
        var rt = RenderTexture.GetTemporary(720, 900, 24);
        var pixels = new Texture2D(720, 900, TextureFormat.RGB24, false);
        var active = RenderTexture.active;
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            pixels.ReadPixels(new Rect(0, 0, 720, 900), 0, 0, false);
            File.WriteAllBytes(Folder + "/" + name + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(cameraGo);
        }
    }
}
