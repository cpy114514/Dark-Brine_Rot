using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Exposes the active Heavy Attack's pullback without editing its poses.</summary>
public static class UseOriginalSahurWindup
{
    const string ControllerPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";
    const string ClipName = "SahurHeavyWindup";
    const string RejectedWindupPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurRightHandCharge.fbx";

    public static string Apply()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var heavy = controller?.layers[0].stateMachine.states
            .Select(child => child.state).FirstOrDefault(state => state.name == "Heavy Attack");
        var heavyClip = heavy?.motion as AnimationClip;
        if (heavyClip == null) throw new InvalidOperationException("Heavy Attack has no animation clip.");
        var source = AssetDatabase.GetAssetPath(heavyClip);
        var importer = AssetImporter.GetAtPath(source) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Heavy Attack is not sourced from an FBX.");
        var clips = importer.clipAnimations.ToList();
        var heavyTake = clips.FirstOrDefault(clip => clip.name == heavyClip.name);
        if (heavyTake == null || heavyTake.firstFrame != 0f || heavyTake.lastFrame != 32f)
            throw new InvalidOperationException("Heavy Attack is not the expected frames 0–32 take.");

        var windup = clips.FirstOrDefault(clip => clip.name == ClipName);
        if (windup == null)
        {
            windup = new ModelImporterClipAnimation();
            clips.Add(windup);
        }
        windup.name = ClipName;
        windup.takeName = heavyTake.takeName;
        windup.firstFrame = 0f;
        windup.lastFrame = 8f;
        windup.wrapMode = heavyTake.wrapMode;
        windup.heightFromFeet = heavyTake.heightFromFeet;
        windup.loopTime = false;
        importer.clipAnimations = clips.ToArray();
        importer.SaveAndReimport();

        var imported = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == ClipName);
        if (imported == null || !imported.humanMotion ||
            Mathf.Abs(imported.length - 8f / 30f) > 0.02f)
            throw new InvalidOperationException("The original windup clip failed to import as frames 0–8.");
        return $"Exposed {ClipName} from Heavy Attack ({heavyClip.name}): 0–8/32 frames, {imported.length:F3}s, source={source}.";
    }

    public static string CleanupRejectedAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) throw new InvalidOperationException("Sahur controller is missing.");
        if (controller.layers.SelectMany(layer => layer.stateMachine.states)
            .Any(child => AssetDatabase.GetAssetPath(child.state.motion) == RejectedWindupPath))
            throw new InvalidOperationException("Rejected windup is still connected to the controller.");
        if (AssetDatabase.LoadMainAssetAtPath(RejectedWindupPath) == null)
            return "Rejected generated windup is already absent.";
        if (!AssetDatabase.DeleteAsset(RejectedWindupPath))
            throw new InvalidOperationException("Could not remove rejected generated windup FBX.");
        return "Removed the rejected generated windup FBX and its meta file.";
    }
}
