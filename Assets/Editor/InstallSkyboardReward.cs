using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Installs the skyboard reward on the existing moving-course prefab.</summary>
[InitializeOnLoad]
public static class InstallSkyboardReward
{
    const string Folder = "Assets/Game/Prefabs/Items/Skyboard/";
    const string Course = "Assets/Game/Prefabs/Environment/PlainMarshmallows/Marshmallow Hop Challenge.prefab";
    const string Request = "Temp/InstallSkyboard.request";
    static InstallSkyboardReward()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += Install;
    }
    [MenuItem("Tools/Dark Brine/Install Skyboard Reward")]
    public static void Install()
    {
        try
        {
            var board = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Skateboard.fbx");
            var scroll = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Speed Scroll.glb");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Skateboard BaseColor.png");
            if (!board || !scroll || !texture) throw new System.Exception("Skyboard assets have not finished importing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Skateboard.mat");
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Folder + "Skateboard.mat");
            }
            material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            var root = PrefabUtility.LoadPrefabContents(Course);
            try
            {
                var reward = root.GetComponent<SkyboardReward>();
                if (!reward) reward = root.AddComponent<SkyboardReward>();
                reward.skateboardModel = board; reward.scrollModel = scroll; reward.skateboardMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(root, Course);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/InstallSkyboard.result", "installed");
            Debug.Log("Skyboard reward installed on the marshmallow challenge.");
        }
        catch (System.Exception e)
        {
            File.WriteAllText("Temp/InstallSkyboard.result", "FAILED: " + e);
            Debug.LogException(e);
        }
        finally { if (File.Exists(Request)) File.Delete(Request); }
    }
}
