using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class InstallMeleeCompletionV239
{
    public static readonly string[] Controllers = {
        "Assets/Resources/Encounter/v006/SahurGameplay.controller",
        "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller",
        "Assets/Resources/Equipment/M4A1/SahurRifle.controller" };
    public static object Run()
    {
        if (Application.isPlaying || EditorApplication.isCompiling) throw new Exception("Ready Edit Mode required.");
        var rows = new List<object>();
        foreach (var name in new[] { "ClubSpinOneHand", "ClubJumpChop", "BladeJumpChop" })
        {
            string action = "PLAYER_" + name + "_v003";
            string source = "Assets/AnimationStaging/" + action + "/" + action + ".fbx";
            var clip = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
            if (!clip.humanMotion) throw new Exception("Expected retained Humanoid: " + action);
            string path = "Assets/Resources/Combat/Combinations/" + name + "V003.anim";
            if (AssetDatabase.LoadMainAssetAtPath(path)) throw new Exception("Preserve revision: " + path);
            var copy = UnityEngine.Object.Instantiate(clip); copy.name = action;
            AssetDatabase.CreateAsset(copy, path);
        }
        foreach (string path in Controllers)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var pair in new[] { ("Club Spin", "ClubSpinOneHand"), ("Jump Slash", "ClubJumpChop"), ("Capri Jump Slash", "BladeJumpChop") })
            {
                var state = machine.states.Single(s => s.state.name == pair.Item1).state;
                string before = AssetDatabase.GetAssetPath(state.motion);
                state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Combat/Combinations/" + pair.Item2 + "V003.anim");
                state.speed = 1; state.speedParameterActive = false; state.timeParameterActive = false;
                EditorUtility.SetDirty(state);
                rows.Add(new { controller = path, state = pair.Item1, before, after = AssetDatabase.GetAssetPath(state.motion) });
            }
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        }
        var result = new { installed = true, rows };
        File.WriteAllText(".codex/melee-completion-20261022/installed.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
