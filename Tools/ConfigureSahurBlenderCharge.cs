using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Import Blender's dedicated Sahur charge on the established Mixamo Avatar.</summary>
public static class ConfigureSahurBlenderCharge
{
    const string Source = "Assets/Game/Prefabs/Characters/Sahur/Animations/Source/SwordAndShieldSlash_ThreeHit.fbx";
    const string Windup = "Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurRightHandCharge.fbx";

    public static string Apply()
    {
        AssetDatabase.Refresh();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Avatar>()
            .FirstOrDefault(item => item.isHuman && item.isValid);
        if (avatar == null) throw new InvalidOperationException("Source Mixamo Humanoid Avatar is missing.");
        var importer = AssetImporter.GetAtPath(Windup) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Blender charge FBX is missing.");
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.importCameras = false;
        importer.importLights = false;
        var clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length != 1)
            throw new InvalidOperationException("Expected one Blender charge take.");
        var clip = clips[0];
        clip.name = "SahurRightHandCharge";
        clip.firstFrame = 1f;
        clip.lastFrame = 41f;
        clip.loopTime = false;
        importer.clipAnimations = new[] { clip };
        importer.SaveAndReimport();

        var imported = AssetDatabase.LoadAllAssetsAtPath(Windup).OfType<AnimationClip>()
            .FirstOrDefault(item => item.name == clip.name);
        if (imported == null || !imported.humanMotion)
            throw new InvalidOperationException("Blender charge did not import as Humanoid animation.");
        return $"Imported {imported.name}, {imported.length:F2}s, Humanoid Avatar {avatar.name}.";
    }
}
