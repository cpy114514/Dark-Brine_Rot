using System;
using System.IO;
using Mavis;
using UnityEditor;
using UnityEngine;

/// <summary>Imports the supplied humanoid skate clip and connects the two tool assets.</summary>
[InitializeOnLoad]
public static class SetupSkateboardingTools
{
    const string Request = "Temp/SetupSkateboarding.request";
    const string Folder = "Assets/Game/Prefabs/Items/Skyboard/";
    const string ClipPath = Folder + "Skateboarding.fbx";
    const string Course = "Assets/Game/Prefabs/Environment/PlainMarshmallows/Marshmallow Hop Challenge.prefab";
    const string Tools = "Assets/Resources/Equipment/Tools";
    static SetupSkateboardingTools()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += Install;
    }
    [MenuItem("Tools/Dark Brine/Install Skateboarding and Tools")]
    public static void Install()
    {
        try
        {
            var importer = AssetImporter.GetAtPath(ClipPath) as ModelImporter;
            if (!importer) throw new Exception("Skateboarding FBX has not imported.");
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true; clip.lockRootRotation = true; clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true; clip.keepOriginalPositionY = false;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            // This in-place copy retains the supplied pose but removes the source's
            // horizontal travel; otherwise Sahur drifts off the moving board.
            var animation = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "Sahur Skateboarding.anim");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ClipPath);
            var animator = source ? source.GetComponentInChildren<Animator>() : null;
            if (!animation || !animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new Exception("The supplied FBX could not be imported as a valid humanoid animation.");
            var board = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Skateboard.fbx");
            var scroll = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Speed Scroll.glb");
            if (!board || !scroll) throw new Exception("Skyboard or scroll model missing.");
            Directory.CreateDirectory(Tools);
            AssetDatabase.Refresh();
            CreateItem(Tools + "/Skyboard.asset", "skyboard", "Skyboard", board);
            CreateItem(Tools + "/SpeedScroll.asset", "speed_scroll", "Speed Scroll", scroll);
            var root = PrefabUtility.LoadPrefabContents(Course);
            try
            {
                var reward = root.GetComponent<SkyboardReward>();
                if (!reward) throw new Exception("Skyboard reward is missing from course prefab.");
                reward.skateboardingClip = animation;
                PrefabUtility.SaveAsPrefabAsset(root, Course);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/SetupSkateboarding.result", "installed: " + animation.name + " length=" + animation.length + " humanoid=" + animation.isHumanMotion);
            Debug.Log("Skateboarding animation and tool inventory installed: " + animation.name);
        }
        catch (Exception e) { File.WriteAllText("Temp/SetupSkateboarding.result", "FAILED: " + e); Debug.LogException(e); }
        finally { if (File.Exists(Request)) File.Delete(Request); }
    }
    static void CreateItem(string path, string id, string title, GameObject model)
    {
        var item = AssetDatabase.LoadAssetAtPath<EquipmentItem>(path);
        if (!item) { item = ScriptableObject.CreateInstance<EquipmentItem>(); AssetDatabase.CreateAsset(item, path); }
        item.id = id; item.displayName = title; item.slot = SahurLoadoutUI.EquipmentSlot.Tools; item.worldModel = model;
        EditorUtility.SetDirty(item);
    }
}
