using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Retargets the CC0 Quaternius humanoid set onto Chicken Bro and trims his size and damage.</summary>
public static class WireChickenAnimations
{
    const string Fbx = "Assets/Game/Prefabs/Enemies/ChickenBro/Animations/UAL1_Standard.fbx";
    const string Controller = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.controller";
    const string Prefab = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab";

    public static string List()
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => c && !c.name.StartsWith("__"));
        return string.Join("\n", clips.Select(c => c.name + " " + c.length.ToString("0.00")));
    }

    public static string Apply()
    {
        if (Application.isPlaying) return "playing";
        if (AssetImporter.GetAtPath(Fbx) == null) AssetDatabase.ImportAsset(Fbx, ImportAssetOptions.ForceUpdate);
        var importer = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        if (!importer) return "missing " + Fbx;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
        var defaults = importer.defaultClipAnimations;
        if (defaults == null || defaults.Length == 0) return "no clips imported";
        foreach (var clip in defaults)
        {
            string n = clip.name.ToLowerInvariant();
            clip.loopTime = n.Contains("idle_loop") || n.Contains("walk_loop") || n.Contains("jog_fwd") || n.Contains("sprint_loop");
        }
        importer.clipAnimations = defaults;
        importer.SaveAndReimport();

        var clips = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => c && !c.name.StartsWith("__")).ToArray();
        var chosen = new Dictionary<string, AnimationClip>
        {
            ["Idle"] = Named(clips, "Idle_Loop"),
            ["Walk"] = Named(clips, "Walk_Loop"),
            ["Run"] = Named(clips, "Sprint_Loop"),
            ["Attack"] = Named(clips, "Punch_Jab"),
            ["Lunge"] = Named(clips, "Sword_Attack"),
            ["Hit"] = Named(clips, "Hit_Chest"),
            ["Heavy"] = Named(clips, "Hit_Head"),
            ["Death"] = Named(clips, "Death01")
        };
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        var machine = controller.layers[0].stateMachine;
        foreach (var child in machine.states)
            if (chosen.TryGetValue(child.state.name, out var clip) && clip) child.state.motion = clip;
        EditorUtility.SetDirty(controller);

        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        try
        {
            var visual = root.transform.GetChild(0);
            visual.localScale = Vector3.one * 2.8608f;
            var p = visual.localPosition; p.y = 0.7371f; visual.localPosition = p;
            var motor = root.GetComponent<CharacterController>();
            motor.height = 4.765f; motor.radius = 0.48f; motor.center = new Vector3(0f, 2.382f, 0f); motor.stepOffset = 0.35f;
            var ai = root.GetComponent<Mavis.ChickenBroAI>();
            ai.strikeDamage = 5f; ai.lungeDamage = 8f;
            ai.meleeRange = 1.72f; ai.lungeMin = 2.05f; ai.lungeMax = 4f;
            ai.separationRadius = 1.16f;
            if (chosen["Attack"]) ai.strikeDuration = Mathf.Max(.6f, chosen["Attack"].length);
            if (chosen["Lunge"]) ai.lungeDuration = Mathf.Max(.7f, chosen["Lunge"].length);
            var animator = root.GetComponentInChildren<Animator>();
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        }
        finally { Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        return string.Join("\n", chosen.Select(p => p.Key + "=" + (p.Value ? p.Value.name + " " + p.Value.length.ToString("0.00") + "s" : "MISSING")))
            + "\nclips=" + clips.Length;
    }

    static AnimationClip Named(AnimationClip[] clips, string token) =>
        clips.FirstOrDefault(c => c.name.EndsWith("|" + token) || c.name == token);

    static AnimationClip Pick(AnimationClip[] clips, string want, params string[] avoid)
    {
        AnimationClip best = null;
        int bestScore = int.MinValue;
        foreach (var clip in clips)
        {
            string n = clip.name.ToLowerInvariant().Replace(" ", "").Replace("_", "");
            if (!n.Contains(want.Replace("_", ""))) continue;
            int score = 20 - clip.name.Length / 4;
            foreach (var word in avoid) if (n.Contains(word.Replace("_", ""))) score -= 8;
            if (n == want.Replace("_", "") || n.EndsWith(want.Replace("_", ""))) score += 6;
            if (score > bestScore) { bestScore = score; best = clip; }
        }
        return best;
    }
}
