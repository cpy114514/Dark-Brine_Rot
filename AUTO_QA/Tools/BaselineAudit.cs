using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;

public static class AutoQaBaselineAudit
{
    public static object Run(string label = null)
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var objects = scenes.SelectMany(s => s.GetRootGameObjects()).SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
        var missingScripts = objects.Select(g => new { scene = g.scene.path, name = g.name, count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g) }).Where(x => x.count > 0).ToArray();
        var missingPrefabs = objects.Where(PrefabUtility.IsPrefabAssetMissing).Select(g => new { scene = g.scene.path, name = g.name }).ToArray();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var pools = UnityEngine.Object.FindObjectsByType<CombatImpactVisuals>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(v => new {
            owner = v.name, active = v.isActiveAndEnabled,
            pulseSlots = ((Array)typeof(CombatImpactVisuals).GetField("pulses", flags).GetValue(v)).Cast<object>().Count(x => x != null),
            ribbonSlots = ((Array)typeof(CombatImpactVisuals).GetField("ribbons", flags).GetValue(v)).Cast<object>().Count(x => x != null),
            children = v.transform.childCount
        }).ToArray();
        var controllers = new List<object>();
        foreach (var path in new[] { "Assets/Resources/Encounter/v006/SahurGameplay.controller", "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller", "Assets/Resources/Equipment/M4A1/SahurRifle.controller" })
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            controllers.Add(new { path, layers = controller.layers.Select(l => new {
                name = l.name, weight = l.defaultWeight, blending = l.blendingMode.ToString(), mask = l.avatarMask ? l.avatarMask.name : null,
                states = l.stateMachine.states.Select(s => new { name = s.state.name, motion = AssetDatabase.GetAssetPath(s.state.motion), speed = s.state.speed,
                    speedParameter = s.state.speedParameterActive ? s.state.speedParameter : null,
                    transitions = s.state.transitions.Select(t => new { target = t.destinationState ? t.destinationState.name : "exit", t.duration, t.hasFixedDuration, t.hasExitTime, t.exitTime, conditions = t.conditions.Select(c => c.parameter + ":" + c.mode + ":" + c.threshold).ToArray() }).ToArray()
                }).ToArray()
            }).ToArray() });
        }
        var result = new {
            capturedUtc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion, playing = Application.isPlaying,
            compilationFailed = EditorUtility.scriptCompilationFailed,
            scenes = scenes.Select(s => new { s.path, s.isDirty, roots = s.rootCount }).ToArray(), objects = objects.Length,
            missingScripts, missingPrefabs,
            missingGuidPaths = new[] { "2bde5fdeb204bc6418608a5fe3993ad1", "8e67ad946c4264b479267ee1643fd2ce", "ad3d230ef428f6049ab82c5136f00a47" }.Select(g => new { guid = g, path = AssetDatabase.GUIDToAssetPath(g) }).ToArray(),
            pools, animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Select(a => new { a.name, controller = a.runtimeAnimatorController ? a.runtimeAnimatorController.name : null, a.applyRootMotion, a.speed }).ToArray(),
            controllers
        };
        Directory.CreateDirectory("AUTO_QA/Evidence");
        string file = "AUTO_QA/Evidence/" + (label ?? (Application.isPlaying ? "baseline-play" : "baseline-editor")) + ".json";
        File.WriteAllText(file, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { file, objects = objects.Length, missingScripts = missingScripts.Length, missingPrefabs = missingPrefabs.Length, pools };
    }
}
