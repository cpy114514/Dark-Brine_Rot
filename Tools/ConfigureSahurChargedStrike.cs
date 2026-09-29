using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Imports the Blender strike and gives the heavy state its own take.</summary>
public static class ConfigureSahurChargedStrike
{
    const string Root = "Assets/Game/Prefabs/Characters/Sahur/Animations/";
    const string Source = Root + "Source/SwordAndShieldSlash_ThreeHit.fbx";
    const string Strike = Root + "Gameplay/SahurRightHandChargedStrike.fbx";
    const string Controller = Root + "SahurGrounded.controller";

    public static string Apply()
    {
        AssetDatabase.Refresh();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Avatar>()
            .FirstOrDefault(item => item.isHuman && item.isValid);
        if (avatar == null) throw new InvalidOperationException("Source Humanoid Avatar is missing.");
        var importer = AssetImporter.GetAtPath(Strike) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Blender strike FBX is missing.");
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.importCameras = false;
        importer.importLights = false;
        var clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length != 1)
            throw new InvalidOperationException("Expected one Blender strike take.");
        var clip = clips[0];
        clip.name = "SahurRightHandChargedStrike";
        clip.firstFrame = 1f;
        clip.lastFrame = 37f;
        clip.loopTime = false;
        importer.clipAnimations = new[] { clip };
        importer.SaveAndReimport();

        var imported = AssetDatabase.LoadAllAssetsAtPath(Strike).OfType<AnimationClip>()
            .FirstOrDefault(item => item.name == clip.name);
        if (imported == null || !imported.humanMotion)
            throw new InvalidOperationException("Charged strike did not import as Humanoid animation.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        if (controller == null) throw new InvalidOperationException("Sahur Animator controller is missing.");
        var heavy = controller.layers[0].stateMachine.states.Select(child => child.state)
            .FirstOrDefault(state => state.name == "Heavy Attack");
        if (heavy == null) throw new InvalidOperationException("Heavy Attack state is missing.");
        heavy.motion = imported;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return $"Heavy Attack now plays {imported.name}, {imported.length:F2}s, with the shared three-hit clip untouched.";
    }

    public static string Inspect()
    {
        var imported = AssetDatabase.LoadAllAssetsAtPath(Strike).OfType<AnimationClip>()
            .FirstOrDefault(item => item.name == "SahurRightHandChargedStrike");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        var heavy = controller?.layers[0].stateMachine.states.Select(child => child.state)
            .FirstOrDefault(state => state.name == "Heavy Attack");
        if (imported == null || !imported.humanMotion || heavy?.motion != imported)
            throw new InvalidOperationException("Heavy Attack is not using the dedicated Humanoid strike.");
        return $"Heavy Attack uses {imported.name}, {imported.length:F2}s; original combo is separate.";
    }
}
