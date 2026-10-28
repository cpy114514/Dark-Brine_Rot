using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Revise only body curves; retain approved muscles, timing, grip and source assets.
public static class TuneTwinBladeV272
{
    public const string Slash = "Assets/Resources/Combat/Combinations/BladeSlashV272.anim";
    public const string Finisher = "Assets/Resources/Combat/Combinations/BladeFinisherV272.anim";
    const string Folder = ".codex/twin-blade-v272";
    static readonly string[] Controllers = {
        "Assets/Resources/Encounter/v006/SahurGameplay.controller",
        "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller",
        "Assets/Resources/Equipment/M4A1/SahurRifle.controller"
    };
    static EditorCurveBinding Binding(string name) => EditorCurveBinding.FloatCurve("", typeof(Animator), name);
    static AnimationCurve Curve(AnimationClip clip, string name) => AnimationUtility.GetEditorCurve(clip, Binding(name));
    static float Signed(float angle) => Mathf.DeltaAngle(0, angle);
    static AnimationClip Revise(string source, string destination, bool spin)
    {
        var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(source);
        var clip = UnityEngine.Object.Instantiate(original); clip.name = Path.GetFileNameWithoutExtension(destination);
        var q = new[] { "x", "y", "z", "w" }.Select(s => Curve(original, "RootQ." + s)).ToArray();
        var p = new[] { "x", "y", "z" }.Select(s => Curve(original, "RootT." + s)).ToArray();
        var rotations = Enumerable.Range(0, 4).Select(_ => new AnimationCurve()).ToArray();
        var positions = Enumerable.Range(0, 3).Select(_ => new AnimationCurve()).ToArray();
        Quaternion previous = Quaternion.identity;
        int count = Mathf.CeilToInt(original.length * 120);
        for (int i = 0; i <= count; i++)
        {
            float phase = (float)i / count, time = phase * original.length;
            var rotation = new Quaternion(q[0].Evaluate(time), q[1].Evaluate(time), q[2].Evaluate(time), q[3].Evaluate(time)).normalized;
            var position = new Vector3(p[0].Evaluate(time), p[1].Evaluate(time), p[2].Evaluate(time));
            if (spin)
            {
                var extra = Quaternion.AngleAxis(-360 * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.28f, .74f, phase)), Vector3.up);
                rotation = extra * rotation;
                position = extra * position;
            }
            else
            {
                float weight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.08f, .24f, phase)) *
                    (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.76f, .96f, phase)));
                Vector3 euler = rotation.eulerAngles;
                rotation = Quaternion.Euler(Signed(euler.x), Signed(euler.y) * (1 - .28f * weight),
                    Signed(euler.z) * (1 - .25f * weight));
                position.x *= 1 - .25f * weight;
            }
            if (i > 0 && Quaternion.Dot(previous, rotation) < 0)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
            previous = rotation;
            for (int axis = 0; axis < 4; axis++) rotations[axis].AddKey(time, rotation[axis]);
            for (int axis = 0; axis < 3; axis++) positions[axis].AddKey(time, position[axis]);
        }
        for (int axis = 0; axis < 4; axis++) AnimationUtility.SetEditorCurve(clip, Binding("RootQ." + "xyzw"[axis]), rotations[axis]);
        for (int axis = 0; axis < 3; axis++) AnimationUtility.SetEditorCurve(clip, Binding("RootT." + "xyz"[axis]), positions[axis]);
        clip.EnsureQuaternionContinuity();
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destination);
        if (existing) { EditorUtility.CopySerialized(clip, existing); UnityEngine.Object.DestroyImmediate(clip); clip = existing; }
        else AssetDatabase.CreateAsset(clip, destination);
        EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip); return clip;
    }
    public static object Run()
    {
        if (Application.isPlaying) throw new Exception("Use stopped Editor");
        Directory.CreateDirectory(Folder);
        var slash = Revise("Assets/Resources/Equipment/Capri/CapriSlash.anim", Slash, false);
        var finisher = Revise("Assets/Resources/Combat/Combinations/BladeFinisherV002.anim", Finisher, true);
        foreach (var path in Controllers)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            foreach (var state in controller.layers[0].stateMachine.states.Select(s => s.state).Where(s => s.name.StartsWith("Capri Combo ")))
            { state.motion = state.name == "Capri Combo 3" ? finisher : slash; EditorUtility.SetDirty(state); }
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        }
        return new { installed = true, Slash, Finisher, controllers = Controllers };
    }
}
