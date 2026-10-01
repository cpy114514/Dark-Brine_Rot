using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mavis;
public static class ConfigureFoliage
{
    public static Task<object> Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before saving foliage configuration");
        var shader = Shader.Find("Mavis/FoliageWind");
        if (!shader || !shader.isSupported) throw new Exception("Wind shader unsupported");
        string folder = "Assets/Game/Prefabs/Environment/Shared/Foliage/Materials/";
        int changed = 0;
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = r.name.ToLowerInvariant();
            if (!n.Contains("island_tree_01") && !n.Contains("tree_small_02")) continue;
            bool island = n.Contains("island_tree");
            var mats = r.sharedMaterials;
            for (int i=0;i<mats.Length;i++)
            {
                if (!mats[i]) continue;
                string name = mats[i].name.ToLowerInvariant();
                string key = name.Contains("leav") || name.Contains("leaf") ? "Leaves" : name.Contains("branch") ? "Branches" : name.Contains("trunk") || name.Contains("bark") ? (island ? "Trunk" : "Bark") : "";
                if (key.Length == 0) continue;
                string path = folder + (island ? "Island_" : "Small_") + key + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    material = new Material(mats[i]);
                    AssetDatabase.CreateAsset(material, path);
                }
                if (material.shader != shader)
                {
                    Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
                    Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                    Vector2 scale = material.mainTextureScale, offset = material.mainTextureOffset;
                    material.shader = shader; material.SetTexture("_MainTex",texture); material.SetTexture("_BaseMap",texture);
                    material.SetColor("_BaseColor",color); material.mainTextureScale = scale; material.mainTextureOffset = offset;
                }
                material.SetFloat("_WindBend", key == "Leaves" ? .8f : .4f);
                material.SetFloat("_WindFrequency", .75f);
                material.SetFloat("_Cull", key == "Leaves" || key == "Branches" ? 0f : 2f);
                EditorUtility.SetDirty(material);
                if (mats[i] != material) { mats[i] = material; changed++; }
            }
            r.sharedMaterials = mats;
            EditorUtility.SetDirty(r);
        }
        var driver = UnityEngine.Object.FindFirstObjectByType<FoliageWindDriver>();
        if (!driver) driver = new GameObject("FoliageWindDriver").AddComponent<FoliageWindDriver>();
        driver.enabled = false; driver.enabled = true;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(driver.gameObject.scene);
        EditorSceneManager.SaveScene(driver.gameObject.scene);
        return Task.FromResult<object>(new { success=true, changedSlots=changed, errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray() });
    }
}
