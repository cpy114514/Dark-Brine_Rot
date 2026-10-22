using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Restore the already approved melee states in the rifle's shared gameplay graph.
// Never replace the graph at runtime or change the player's loaded scene.
public static class RestoreTwinBladeControllerV235
{
    const string Source = "Assets/Resources/Encounter/v006/SahurGameplay.controller";
    const string Target = "Assets/Resources/Equipment/M4A1/SahurRifle.controller";
    static readonly string[] Names = { "Capri Combo 1", "Capri Combo 2", "Capri Combo 3", "Capri Heavy", "Capri Charge", "Capri Jump Slash", "Blade Spin", "Blade Guard", "Club Spin", "Jump Slash" };

    static void CopyState(AnimatorState source, AnimatorState target)
    {
        if (source.behaviours.Length != 0) throw new Exception("Unexpected state behaviour: " + source.name);
        target.motion = source.motion;
        target.speed = source.speed;
        target.speedParameter = source.speedParameter;
        target.speedParameterActive = source.speedParameterActive;
        target.timeParameter = source.timeParameter;
        target.timeParameterActive = source.timeParameterActive;
        target.cycleOffset = source.cycleOffset;
        target.cycleOffsetParameter = source.cycleOffsetParameter;
        target.cycleOffsetParameterActive = source.cycleOffsetParameterActive;
        target.mirror = source.mirror;
        target.mirrorParameter = source.mirrorParameter;
        target.mirrorParameterActive = source.mirrorParameterActive;
        target.iKOnFeet = source.iKOnFeet;
        target.writeDefaultValues = source.writeDefaultValues;
        target.tag = source.tag;
        EditorUtility.SetDirty(target);
    }

    static void CopyTransitions(AnimatorState source, AnimatorState target, AnimatorStateMachine machine)
    {
        foreach (var old in target.transitions) target.RemoveTransition(old);
        foreach (var transition in source.transitions)
        {
            if (transition.destinationStateMachine) throw new Exception("Unexpected nested destination");
            var copy = transition.isExit ? target.AddExitTransition() : target.AddTransition(machine.states.Single(x => x.state.name == transition.destinationState.name).state);
            // Conditions and timing are serialized through Unity; remap asset-local references.
            var destination = copy.destinationState;
            EditorUtility.CopySerialized(transition, copy);
            copy.destinationState = destination;
            copy.destinationStateMachine = null;
            EditorUtility.SetDirty(copy);
        }
    }

    public static object Run()
    {
        if (Application.isPlaying) throw new Exception("Ready Edit Mode required");
        var source = AssetDatabase.LoadAssetAtPath<AnimatorController>(Source);
        var target = AssetDatabase.LoadAssetAtPath<AnimatorController>(Target);
        var sm = source.layers[0].stateMachine;
        var tm = target.layers[0].stateMachine;
        foreach (var parameter in source.parameters)
            if (!target.parameters.Any(x => x.name == parameter.name)) target.AddParameter(parameter);
        foreach (var name in Names)
        {
            var state = sm.states.Single(x => x.state.name == name).state;
            var existing = tm.states.FirstOrDefault(x => x.state.name == name).state;
            if (!existing) existing = tm.AddState(name);
            CopyState(state, existing);
            CopyTransitions(state, existing, tm);
        }
        const string charge = "Twin Blade Charge";
        if (!target.layers.Any(x => x.name == charge))
        {
            var original = source.layers.Single(x => x.name == charge);
            var mask = UnityEngine.Object.Instantiate(original.avatarMask);
            mask.name = original.avatarMask.name;
            AssetDatabase.AddObjectToAsset(mask, target);
            var machine = new AnimatorStateMachine { name = charge };
            AssetDatabase.AddObjectToAsset(machine, target);
            foreach (var state in original.stateMachine.states)
                CopyState(state.state, machine.AddState(state.state.name, state.position));
            foreach (var state in original.stateMachine.states)
                CopyTransitions(state.state, machine.states.Single(x => x.state.name == state.state.name).state, machine);
            machine.defaultState = machine.states.Single(x => x.state.name == original.stateMachine.defaultState.name).state;
            target.AddLayer(new AnimatorControllerLayer { name = charge, stateMachine = machine, avatarMask = mask,
                defaultWeight = original.defaultWeight, blendingMode = original.blendingMode, iKPass = original.iKPass });
        }
        EditorUtility.SetDirty(tm);
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssetIfDirty(target);
        var result = new { source = Source, target = Target, restoredStates = Names, layers = target.layers.Select(x => x.name).ToArray() };
        File.WriteAllText(".codex/twin-blade-restore-20261021/restored.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
