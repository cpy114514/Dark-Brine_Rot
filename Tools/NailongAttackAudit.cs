using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

public static class NailongAttackAudit
{
    public static string PreviewLeft() => Preview(Mavis.NailongAttackMotion.Style.LeftClaw);
    public static string PreviewDouble() => Preview(Mavis.NailongAttackMotion.Style.DoubleClaw);
    public static string PreviewBump() => Preview(Mavis.NailongAttackMotion.Style.ShoulderBump);
    public static string StopPreview()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");
        var boss = Array.Find(scene.GetRootGameObjects(), x => x.name == "Nailong");
        boss.GetComponent<Mavis.NailongAttackMotion>().Stop();
        return "Stopped preview";
    }

    public static string PoseReport()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");
        var boss = Array.Find(scene.GetRootGameObjects(), x => x.name == "Nailong");
        var skin = boss.GetComponentInChildren<SkinnedMeshRenderer>();
        return string.Join("\n", skin.bones.Where(x => x != null &&
            (x.name.Contains("Hand") || x.name.Contains("Shoulder") || x.name == "Head"))
            .Select(x => x.name + " local=" + boss.transform.InverseTransformPoint(x.position)));
    }

    static string Preview(Mavis.NailongAttackMotion.Style style)
    {
        if (!Application.isPlaying) return "Enter Play mode first.";
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Enemies.unity");
        var boss = Array.Find(scene.GetRootGameObjects(), x => x.name == "Nailong");
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        if (player != null) player.enabled = false;
        var camera = Camera.main;
        camera.transform.position = boss.transform.position + boss.transform.forward * 5f + Vector3.up * 1.7f;
        camera.transform.LookAt(boss.transform.position + Vector3.up * 1.35f);
        var motion = boss.GetComponent<Mavis.NailongAttackMotion>();
        motion.Begin(style, 1000f);
        typeof(Mavis.NailongAttackMotion).GetField("startedAt", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(motion, Time.time - 500f);
        return "preview=" + style + " boss=" + boss.transform.position +
            " armatureY=" + boss.transform.Find("Armature").localPosition.y;
    }

    public static string Apply()
    {
        if (Application.isPlaying) return "Stop Play mode before editing the prefab.";
        NailongBossSetup.Build();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nailong/Nailong.prefab");
        var motion = prefab.GetComponent<Mavis.NailongAttackMotion>();
        var ai = prefab.GetComponent<Mavis.NailongAI>();
        return "motion=" + (motion != null) + " visualGroundDrop=" + ai.visualGroundDrop +
            " armatureY=" + prefab.transform.Find("Armature").localPosition.y + "\n" + Inspect();
    }

    public static string Inspect()
    {
        const string path = "Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx";
        var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
        var b = new StringBuilder();
        b.AppendLine("Clip count=" + clips.Length);
        foreach (var clip in clips.Where(c =>
            c.name.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0 ||
            c.name.IndexOf("punch", StringComparison.OrdinalIgnoreCase) >= 0 ||
            c.name.IndexOf("hit", StringComparison.OrdinalIgnoreCase) >= 0 ||
            c.name.IndexOf("swipe", StringComparison.OrdinalIgnoreCase) >= 0 ||
            c.name.IndexOf("kick", StringComparison.OrdinalIgnoreCase) >= 0 ||
            c.name.IndexOf("stomp", StringComparison.OrdinalIgnoreCase) >= 0))
            b.AppendLine(clip.name + " length=" + clip.length + " humanoid=" + clip.humanMotion);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Models/Nailong/NailongBoss.controller");
        foreach (var state in controller.layers[0].stateMachine.states)
            b.AppendLine("STATE " + state.state.name + " motion=" + state.state.motion?.name);
        return b.ToString();
    }
}
