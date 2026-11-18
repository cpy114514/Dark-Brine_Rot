using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallIsland2ChocolateContact
{
    const string Art = "Assets/Game/Art/Island2WaterSystem/";
    const string MaskPath = Art + "Sahur chocolate adhesion mask.asset";

    static float Noise(Vector3 p, float frequency)
    {
        return Mathf.PerlinNoise((p.x + p.y * .47f) * frequency + 12.4f,
            (p.z + p.y * .63f) * frequency + 34.7f);
    }

    // Rasterize the standing model's coverage into its original UV atlas. The
    // material can then follow skinning without changing the mesh or its maps.
    static Texture2D BakeMask(Mesh mesh)
    {
        const int size = 512;
        var positions = mesh.vertices;
        var uv = mesh.uv;
        var triangles = mesh.triangles;
        var pixels = new Color32[size * size];
        var valid = new bool[pixels.Length];
        float bottom = positions.Min(p => p.z);
        float top = positions.Max(p => p.z);

        for (int index = 0; index < triangles.Length; index += 3)
        {
            int ia = triangles[index], ib = triangles[index + 1], ic = triangles[index + 2];
            Vector2 a = uv[ia] * size, b = uv[ib] * size, c = uv[ic] * size;
            Vector2 ab = b - a, ac = c - a;
            float determinant = ab.x * ac.y - ab.y * ac.x;
            if (Mathf.Abs(determinant) < .00001f) continue;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)));
            int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)));
            int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var ap = new Vector2(x + .5f, y + .5f) - a;
                float u = (ap.x * ac.y - ap.y * ac.x) / determinant;
                float v = (ab.x * ap.y - ab.y * ap.x) / determinant;
                if (u < 0 || v < 0 || u + v > 1) continue;
                Vector3 p = positions[ia] + (positions[ib] - positions[ia]) * u + (positions[ic] - positions[ia]) * v;
                float height = Mathf.InverseLerp(bottom, top, p.z);
                float streak = Noise(new Vector3(p.x * 3, p.y * 3, p.z * .58f), 19);
                float reach = .115f + streak * .145f + Noise(p, 39) * .07f;
                float wet = Mathf.Clamp01((reach - height) * 8);
                float flecks = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.48f, .69f, Noise(p, 53)));
                float splash = flecks * Mathf.Lerp(1, .6f, Mathf.InverseLerp(.22f, .64f, height)) *
                    (1 - Mathf.InverseLerp(.65f, .75f, height));
                int pixel = y * size + x;
                pixels[pixel] = new Color32((byte)(wet * 255), (byte)(splash * 255), 0, 255);
                valid[pixel] = true;
            }
        }

        // Pad the UV islands for bilinear filtering and the first few mips.
        for (int pass = 0; pass < 5; pass++)
        {
            var previous = (bool[])valid.Clone();
            for (int y = 1; y < size - 1; y++)
            for (int x = 1; x < size - 1; x++)
            {
                int i = y * size + x;
                if (previous[i]) continue;
                int source = previous[i - 1] ? i - 1 : previous[i + 1] ? i + 1 :
                    previous[i - size] ? i - size : previous[i + size] ? i + size : -1;
                if (source < 0) continue;
                pixels[i] = pixels[source];
                valid[i] = true;
            }
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
        if (!texture)
        {
            texture = new Texture2D(size, size, TextureFormat.RG16, true, true);
            texture.name = "Sahur chocolate adhesion mask";
            AssetDatabase.CreateAsset(texture, MaskPath);
        }
        else if (texture.width != size)
            texture.Reinitialize(size, size, TextureFormat.RG16, true);
        texture.SetPixels32(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        texture.Apply();
        EditorUtility.SetDirty(texture);
        return texture;
    }

    public static object Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != SecondIslandAudio.ScenePath)
            throw new InvalidOperationException("Open Second Island in Edit mode.");
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var skin = player.GetComponentInChildren<SkinnedMeshRenderer>();
        var sound = UnityEngine.Object.FindFirstObjectByType<SecondIslandAudio>();
        var waterway = sound ? sound.waterway : null;
        var lake = GameObject.Find(waterway ? "Chocolate headwater lake" : "Grand Chocolate Lake - 170 metre headwater").GetComponent<MeshRenderer>();
        var river = GameObject.Find(waterway ? "Chocolate west channel" : "Continuous chocolate river - lake outlet to cliff").GetComponent<MeshRenderer>();
        var falls = GameObject.Find(waterway ? "Chocolate Cloudfall" : "The Cloudfall - broad falling chocolate curtain").GetComponent<MeshRenderer>();
        var fallout = GameObject.Find(waterway ? "Cloudfall heavy droplets" : "Sparse falling cocoa droplets - bounded spray").GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Dark Brine/Second Island/Sahur Chocolate Coating");
        if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader) ||
            ShaderUtil.ShaderHasError(river.sharedMaterial.shader))
            throw new InvalidOperationException("Compile the chocolate shaders before installing.");

        string path = Art + "Sahur chocolate coating.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var original = skin.sharedMaterial;
        var authored = new Material(shader) { name = "Sahur chocolate coating" };
        foreach (string texture in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
            authored.SetTexture(texture, original.GetTexture(texture));
        authored.SetTextureScale("_BaseMap", original.GetTextureScale("_BaseMap"));
        authored.SetTextureOffset("_BaseMap", original.GetTextureOffset("_BaseMap"));
        foreach (string property in new[] { "_BumpScale", "_Metallic", "_Smoothness", "_OcclusionStrength" })
            authored.SetFloat(property, original.GetFloat(property));
        authored.SetColor("_BaseColor", original.GetColor("_BaseColor"));
        authored.EnableKeyword("_NORMALMAP");
        authored.EnableKeyword("_METALLICSPECGLOSSMAP");
        var mask = BakeMask(skin.sharedMesh);
        authored.SetTexture("_ChocolateBodyMask", mask);
        authored.SetShaderPassEnabled("MotionVectors", false);
        if (!material)
        {
            material = authored;
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.CopySerialized(authored, material);
            UnityEngine.Object.DestroyImmediate(authored);
        }
        if (material.GetTexture("_ChocolateBodyMask") != mask)
            throw new InvalidOperationException("Sahur chocolate mask did not bind to the coating shader.");
        EditorUtility.SetDirty(material);

        var contact = player.GetComponent<SecondIslandChocolateContact>();
        if (!contact) contact = Undo.AddComponent<SecondIslandChocolateContact>(player.gameObject);
        var serialized = new SerializedObject(contact);
        serialized.FindProperty("lake").objectReferenceValue = lake;
        serialized.FindProperty("river").objectReferenceValue = river;
        serialized.FindProperty("waterway").objectReferenceValue = waterway;
        serialized.FindProperty("sahurSkin").objectReferenceValue = skin;
        serialized.FindProperty("chocolateSkin").objectReferenceValue = material;
        serialized.FindProperty("dropletMaterial").objectReferenceValue = fallout.sharedMaterial;
        serialized.FindProperty("dropletMesh").objectReferenceValue = fallout.mesh;
        serialized.ApplyModifiedProperties();

        if (!waterway)
        {
            foreach (var surface in new[] {lake, river, falls})
            {
                Material water = surface.sharedMaterial;
                water.SetColor("_BaseColor", new Color(.175f, .068f, .031f));
                water.SetColor("_FoldColor", new Color(.30f, .132f, .065f));
                water.SetFloat("_Smoothness", .85f);
                water.SetFloat("_WaveHeight", .065f);
                EditorUtility.SetDirty(water);
            }
            var shore = GameObject.Find("Cocoa butter shoreline - organic basin rim").GetComponent<Renderer>();
            var bank = shore.sharedMaterial;
            bank.shader = Shader.Find("Dark Brine/Second Island/Ground Transition");
            bank.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Island/Materials/rocky_terrain_4k/textures/rocky_terrain_diff_4k.jpg"));
            bank.SetColor("_BaseColor", new Color(.25f, .105f, .056f));
            bank.SetFloat("_Opacity", .85f);
            bank.SetFloat("_ChocolateBank", 1);
            bank.renderQueue = -1;
            EditorUtility.SetDirty(bank);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        return new { scene = scene.path, mask = MaskPath, body = skin.name };
    }
}
