using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ConfigureSahurNaturalCombo
{
    public static string Inspect()
    {
        const string path = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
        bool Valid(ThirdPersonPlayerController movement) => movement != null &&
            movement.comboSourceX != null && movement.comboSourceZ != null &&
            movement.comboSourceX.Length == 3 && movement.comboSourceZ.Length == 3 &&
            movement.comboSourceX.All(curve => curve != null && curve.length > 0) &&
            movement.comboSourceZ.All(curve => curve != null && curve.length > 0);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ThirdPersonPlayerController>();
        if (!Valid(source)) throw new InvalidOperationException("Prefab source-travel curves are incomplete.");
        var instances = UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var instance in instances)
            if (!Valid(instance)) throw new InvalidOperationException(instance.name + " lacks source-travel curves.");
        return "Prefab source X/Z curves verified; loaded scene instances verified: " +
            string.Join(", ", instances.Select(instance => instance.name + " [" +
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance.gameObject) + "]"));
    }

    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        Mavis.SetupSahurStickCombo.AlignComboRootOrientation();
        const string root = "Assets/Game/Prefabs/Characters/Sahur/";
        var prefab = PrefabUtility.LoadPrefabContents(root + "SahurPlayer.prefab");
        try
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(root + "Animations/Source/SwordAndShieldSlash_ThreeHit.fbx").OfType<AnimationClip>().ToArray();
            var x = new AnimationCurve[3]; var z = new AnimationCurve[3];
            for (int stage = 0; stage < 3; stage++)
            {
                var clip = clips.First(c => c.name == "SahurSwordCombo" + (stage + 1));
                AnimationCurve Normalize(string property)
                {
                    var source = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), property));
                    if (source == null) throw new InvalidOperationException(property + " source curve missing.");
                    var keys = source.keys;
                    for (int index = 0; index < keys.Length; index++)
                    {
                        keys[index].time /= clip.length;
                        keys[index].inTangent *= clip.length;
                        keys[index].outTangent *= clip.length;
                    }
                    return new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
                }
                x[stage] = Normalize("RootT.x"); z[stage] = Normalize("RootT.z");
            }
            var movement = prefab.GetComponent<ThirdPersonPlayerController>();
            movement.comboSourceX = x; movement.comboSourceZ = z;
            PrefabUtility.SaveAsPrefabAsset(prefab, root + "SahurPlayer.prefab");
            return "Saved source-authored RootT X/Z curves for all three slices. Native jump Y, grip, timing and pose retained; no manually tuned attack travel.";
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
}
