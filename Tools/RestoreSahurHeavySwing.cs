using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Keep the original heavy swing while gameplay corrects its travel.</summary>
public static class RestoreSahurHeavySwing
{
    const string ControllerPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";

    public static string Apply()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) throw new InvalidOperationException("Sahur controller is missing.");
        var states = controller.layers[0].stateMachine.states.Select(child => child.state).ToArray();
        var original = states.FirstOrDefault(state => state.name == "Attack")?.motion as AnimationClip;
        var heavy = states.FirstOrDefault(state => state.name == "Heavy Attack");
        if (original == null || heavy == null)
            throw new InvalidOperationException("Original Sahur swing or heavy state is missing.");
        heavy.motion = original;
        EditorUtility.SetDirty(heavy);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return Inspect();
    }

    public static string Inspect()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var states = controller?.layers[0].stateMachine.states.Select(child => child.state).ToArray();
        var original = states?.FirstOrDefault(state => state.name == "Attack")?.motion;
        var heavy = states?.FirstOrDefault(state => state.name == "Heavy Attack");
        if (original == null || heavy?.motion != original)
            throw new InvalidOperationException("Heavy Attack is not using the original swing.");
        return $"Heavy Attack restored to original clip {original.name}; heading and travel remain gameplay-controlled.";
    }

    public static string CleanLegacyGuardFields()
    {
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var guard = prefab.GetComponentInChildren<SahurCombatGuardIK>(true);
            if (guard == null) throw new InvalidOperationException("Sahur guard component is missing.");
            EditorUtility.SetDirty(guard);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            return "Sahur prefab saved without retired runtime wrist-turn fields.";
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
}
