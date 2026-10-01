using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ConfigureSahurThrow
{
    public static object Install()
    {
        if (Application.isPlaying) throw new Exception("Stop Play mode before installing.");
        var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx")
            .OfType<AnimationClip>().Single(c => c.name == "Armature|OverhandThrow");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Boomerang Throw") ?? machine.AddState("Boomerang Throw");
        state.motion = clip;
        state.speed = 1.65f;
        state.writeDefaultValues = false;
        if (state.transitions.Length == 0)
        {
            var transition = state.AddTransition(machine.states.Single(s => s.state.name == "Locomotion").state);
            transition.hasExitTime = true;
            transition.exitTime = .94f;
            transition.hasFixedDuration = true;
            transition.duration = .12f;
        }
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return new { clip = clip.name, durationSeconds = clip.length / state.speed, releasePhase = .30f, controller = AssetDatabase.GetAssetPath(controller) };
    }
    public static object Inspect()
    {
        return AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Game/Prefabs/Characters/Sahur/Animations" })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct()
            .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Select(clip => new { path, clip.name, clip.length, human = clip.isHumanMotion })).ToArray();
    }
}
