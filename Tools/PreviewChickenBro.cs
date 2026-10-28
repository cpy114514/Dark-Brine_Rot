using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PreviewChickenBro
{
    const string ModelPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.fbx";
    const string PrefabPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab";
    const string Folder = "Temp/chicken-pose/unity-preview";

    public static string Capture()
    {
        Directory.CreateDirectory(Folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var sourceAnimator = prefab.GetComponentInChildren<Animator>(true);
        var sourceRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
        var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        SceneManager.MoveGameObjectToScene(model, scene);
        model.transform.SetPositionAndRotation(new Vector3(0f, 0.7371f, 0f), Quaternion.identity);
        model.transform.localScale = Vector3.one * 2.8608f;

        var animator = model.GetComponentInChildren<Animator>(true);
        animator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Rebind();
        var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
        renderer.sharedMaterial = sourceRenderer.sharedMaterial;
        renderer.updateWhenOffscreen = true;

        var lightGo = new GameObject("Preview light");
        SceneManager.MoveGameObjectToScene(lightGo, scene);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.2f;
        light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        var fillGo = new GameObject("Preview fill");
        SceneManager.MoveGameObjectToScene(fillGo, scene);
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.7f;
        fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

        try
        {
            Shoot(scene, model, animator, "bind", null, 0f);
            Shoot(scene, model, animator, "idle", "Idle", 0f);
            Shoot(scene, model, animator, "walk-a", "Walk", 0.12f);
            Shoot(scene, model, animator, "walk-b", "Walk", 0.4f);
            Shoot(scene, model, animator, "walk-c", "Walk", 0.68f);
            Shoot(scene, model, animator, "run", "Run", 0.32f);
            Shoot(scene, model, animator, "punch-a", "Attack", 0.2f);
            Shoot(scene, model, animator, "punch-b", "Attack", 0.48f);
            Shoot(scene, model, animator, "hit", "Hit", 0.35f);
            Shoot(scene, model, animator, "death", "Death", 0.7f);
            return Folder;
        }
        finally
        {
            Object.DestroyImmediate(model);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(fillGo);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static void Shoot(Scene scene, GameObject model, Animator animator, string name, string state, float time)
    {
        if (state == null) animator.enabled = false;
        else
        {
            animator.enabled = true;
            animator.Play(state, 0, time);
            animator.Update(0f);
        }
        model.transform.SetPositionAndRotation(new Vector3(0f, 0.7371f, 0f), Quaternion.identity);
        var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
        var baked = new Mesh();
        renderer.BakeMesh(baked);
        var show = new GameObject("baked-pose");
        SceneManager.MoveGameObjectToScene(show, scene);
        show.transform.SetParent(renderer.transform, false);
        show.AddComponent<MeshFilter>().sharedMesh = baked;
        var bakedRenderer = show.AddComponent<MeshRenderer>();
        bakedRenderer.sharedMaterial = renderer.sharedMaterial;
        renderer.enabled = false;
        var bounds = bakedRenderer.bounds;
        var focus = bounds.center;
        var size = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * 1.15f;
        Save(scene, name + "-front", focus + new Vector3(0f, 0f, -12f), focus, size);
        Save(scene, name + "-side", focus + new Vector3(12f, 0f, 0f), focus, size);
        renderer.enabled = true;
        Object.DestroyImmediate(show);
        Object.DestroyImmediate(baked);
    }

    static void Save(Scene scene, string name, Vector3 position, Vector3 focus, float size)
    {
        var cameraGo = new GameObject("preview-camera");
        SceneManager.MoveGameObjectToScene(cameraGo, scene);
        var camera = cameraGo.AddComponent<Camera>();
        camera.enabled = false;
        camera.scene = scene;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 40f;
        camera.transform.position = position;
        camera.transform.LookAt(focus, Vector3.up);
        var rt = RenderTexture.GetTemporary(640, 900, 24);
        var pixels = new Texture2D(640, 900, TextureFormat.RGB24, false);
        var active = RenderTexture.active;
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            pixels.ReadPixels(new Rect(0, 0, 640, 900), 0, 0, false);
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
