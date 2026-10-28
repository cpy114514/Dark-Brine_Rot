using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Scales Chicken Bro just under Sahur and assigns the textures extracted from the FBX.</summary>
public static class FinishChickenBro
{
    const string Root = "Assets/Game/Prefabs/Enemies/ChickenBro";
    const string PrefabPath = Root + "/ChickenBro.prefab";
    const string SahurPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
    const string AlbedoPath = Root + "/Textures/texture_pbr_20250901_albedo.png";
    const string NormalPath = Root + "/Textures/texture_pbr_20250901_normal.png";
    const string RoughPath = Root + "/Textures/texture_pbr_20250901_roughness.png";
    const string MetalPath = Root + "/Textures/texture_pbr_20250901_metallic.png";
    const string MaterialPath = Root + "/ChickenBro.mat";
    const string PackedPath = Root + "/Textures/texture_pbr_20250901_metallicSmoothness.png";

    public static string Report()
    {
        if (Application.isPlaying) return "playing";
        var sahur = AssetDatabase.LoadAssetAtPath<GameObject>(SahurPath);
        var chicken = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        return "SAHUR\n" + Dump(sahur) + "\nCHICKEN\n" + Dump(chicken);
    }

    static string Dump(GameObject prefab)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            var text = new System.Text.StringBuilder();
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer) continue;
                text.Append(renderer.name).Append(" h=").Append(renderer.bounds.size.y.ToString("0.00"))
                    .Append(" active=").Append(renderer.gameObject.activeInHierarchy).Append('\n');
            }
            var motor = instance.GetComponent<CharacterController>();
            if (motor) text.Append("capsule=").Append(motor.height.ToString("0.00")).Append('\n');
            return text.ToString();
        }
        finally { Object.DestroyImmediate(instance); }
    }

    public static string Prepare()
    {
        if (!Application.isPlaying) return "idle";
        EditorApplication.isPlaying = false;
        return "stopping";
    }

    public static string Apply()
    {
        if (Application.isPlaying) return "still playing";
        ConfigureTextures();
        var material = BuildMaterial();
        var sahur = AssetDatabase.LoadAssetAtPath<GameObject>(SahurPath);
        var chicken = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        float sahurHeight = Measure(sahur);
        float before = Measure(chicken);
        float target = sahurHeight * .92f;
        float factor = target / Mathf.Max(.05f, before);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(chicken);
        try
        {
            var visual = root.transform.childCount > 0 ? root.transform.GetChild(0) : root.transform;
            visual.localScale *= factor;
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
            Bounds bounds = WorldBounds(root);
            visual.position -= new Vector3(0f, bounds.min.y - root.transform.position.y, 0f);
            bounds = WorldBounds(root);
            var motor = root.GetComponent<CharacterController>();
            motor.height = Mathf.Max(.8f, bounds.size.y * .94f);
            motor.radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * .42f, .28f, .55f);
            motor.center = new Vector3(0f, motor.height * .5f, 0f);
            motor.stepOffset = Mathf.Min(.4f, motor.height * .25f);
            var ai = root.GetComponent<Mavis.ChickenBroAI>();
            if (ai)
            {
                ai.meleeRange = motor.radius * 2f + .85f;
                ai.lungeMin = ai.meleeRange + .35f;
                ai.lungeMax = ai.lungeMin + 2.2f;
                ai.separationRadius = motor.radius * 2.4f;
                ai.surroundDistance = motor.radius * 2f + 2.4f;
            }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            float after = bounds.size.y;
            return "sahur=" + sahurHeight.ToString("0.00") + " before=" + before.ToString("0.00") +
                " after=" + after.ToString("0.00") + " factor=" + factor.ToString("0.00") +
                " material=" + material.name;
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void ConfigureTextures()
    {
        SetImporter(AlbedoPath, TextureImporterType.Default, true);
        SetImporter(NormalPath, TextureImporterType.NormalMap, false);
        SetImporter(RoughPath, TextureImporterType.Default, false);
        SetImporter(MetalPath, TextureImporterType.Default, false);
    }

    static void SetImporter(string path, TextureImporterType type, bool srgb)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = type;
        importer.sRGBTexture = srgb;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    static Material BuildMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(shader) { name = "ChickenBro" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else material.shader = shader;
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        var packed = PackMetallicSmoothness();
        material.SetTexture("_BaseMap", albedo);
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", normal);
        material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap", packed);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.SetFloat("_Metallic", 1f);
        material.SetFloat("_Smoothness", .45f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Texture2D PackMetallicSmoothness()
    {
        var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalPath);
        var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(RoughPath);
        var previous = RenderTexture.active;
        int size = 1024;
        var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(metal, rt);
        var metalRead = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        RenderTexture.active = rt;
        metalRead.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        Graphics.Blit(rough, rt);
        var roughRead = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        roughRead.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        var packed = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        var m = metalRead.GetPixels32();
        var r = roughRead.GetPixels32();
        var o = new Color32[m.Length];
        for (int i = 0; i < o.Length; i++)
            o[i] = new Color32(m[i].r, m[i].r, m[i].r, (byte)(255 - r[i].r));
        packed.SetPixels32(o);
        packed.Apply(true, false);
        File.WriteAllBytes(PackedPath, packed.EncodeToPNG());
        Object.DestroyImmediate(metalRead);
        Object.DestroyImmediate(roughRead);
        Object.DestroyImmediate(packed);
        AssetDatabase.ImportAsset(PackedPath);
        SetImporter(PackedPath, TextureImporterType.Default, false);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(PackedPath);
    }

    static float Measure(GameObject prefab)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            return WorldBounds(instance).size.y;
        }
        finally { Object.DestroyImmediate(instance); }
    }

    static Bounds WorldBounds(GameObject root)
    {
        Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
        bool any = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer || renderer is ParticleSystemRenderer) continue;
            if (!any) { bounds = renderer.bounds; any = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return any ? bounds : new Bounds(root.transform.position, Vector3.one);
    }
}
