using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class VerifyMeleeCompletionV239
{
    public static object Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Encounter/v006/SahurGameplay.controller");
        var target = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Equipment/M4A1/SahurRifle.controller");
        var checks = new System.Collections.Generic.List<string>();
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add(message); }
        string Signature(AnimatorState state) => Newtonsoft.Json.JsonConvert.SerializeObject(new {
            motion = AssetDatabase.GetAssetPath(state.motion), state.speed, state.speedParameter, state.speedParameterActive,
            state.timeParameter, state.timeParameterActive, state.mirror, state.mirrorParameter, state.mirrorParameterActive,
            state.cycleOffset, state.cycleOffsetParameter, state.cycleOffsetParameterActive, state.iKOnFeet, state.writeDefaultValues,
            transitions = state.transitions.Select(t => new { destination = t.destinationState ? t.destinationState.name : null,
                t.isExit, t.hasExitTime, t.exitTime, t.duration, t.offset, t.hasFixedDuration, t.interruptionSource,
                t.orderedInterruption, t.canTransitionToSelf, t.mute, t.solo, conditions = t.conditions.Select(c => new { c.parameter, c.mode, c.threshold }).ToArray() }).ToArray()
        });
        foreach (var name in new[] { "Capri Combo 1", "Capri Combo 2", "Capri Combo 3", "Capri Heavy", "Capri Charge", "Capri Jump Slash", "Blade Spin", "Blade Guard", "Club Spin", "Jump Slash" })
        {
            var a = source.layers[0].stateMachine.states.Single(s => s.state.name == name).state;
            var b = target.layers[0].stateMachine.states.Single(s => s.state.name == name).state;
            Check(Signature(a) == Signature(b), "Approved melee state, timing and transitions: " + name);
            foreach (var t in b.transitions)
                Check(!t.destinationState || AssetDatabase.GetAssetPath(t.destinationState) == AssetDatabase.GetAssetPath(target), "Transition stays inside shared controller: " + name);
        }
        var sa = source.layers.Single(l => l.name == "Twin Blade Charge");
        var ta = target.layers.Single(l => l.name == "Twin Blade Charge");
        Check(sa.defaultWeight == ta.defaultWeight && sa.blendingMode == ta.blendingMode && sa.iKPass == ta.iKPass &&
            sa.stateMachine.defaultState.name == ta.stateMachine.defaultState.name, "Charge layer starts empty with approved blend ownership");
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            Check(sa.avatarMask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i) == ta.avatarMask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i), "Charge mask: " + (AvatarMaskBodyPart)i);
        foreach (var state in sa.stateMachine.states)
            Check(Signature(state.state) == Signature(ta.stateMachine.states.Single(s => s.state.name == state.state.name).state), "Charge state: " + state.state.name);
        foreach (var p in source.parameters)
            Check(target.parameters.Any(t => t.name == p.name && t.type == p.type), "Shared animation parameter: " + p.name);
        Check(target.layers.Where(l => l.name.StartsWith("Rifle ")).Count() == 3, "All original rifle layers retained");
        foreach(var path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            foreach(var pair in new[]{("Club Spin","ClubSpinOneHand",.9f,true),("Jump Slash","ClubJumpChop",.85f,false),("Capri Jump Slash","BladeJumpChop",.85f,false)})
            {
                var state=controller.layers[0].stateMachine.states.Single(x=>x.state.name==pair.Item1).state;
                var clip=state.motion as AnimationClip;
                Check(clip && AssetDatabase.GetAssetPath(clip)=="Assets/Resources/Combat/Combinations/"+pair.Item2+"V003.anim",path+" binds "+pair.Item1+" to v003 full action");
                Check(clip.humanMotion && Mathf.Abs(clip.length-pair.Item3)<.001f && clip.isLooping==pair.Item4,path+" humanoid duration/loop "+pair.Item1);
                Check(state.speed==1 && !state.speedParameterActive && !state.timeParameterActive,path+" stable animation clock "+pair.Item1);
            }
        }
        var result = new { passed = true, checks };
        File.WriteAllText(".codex/melee-completion-20261022/controller-contracts.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
