using System.Globalization;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEngine;

public static class InstallChickenRig
{
    const string ModelPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.fbx";
    const string PrefabPath = "Assets/Game/Prefabs/Enemies/ChickenBro/ChickenBro.prefab";

    public static string Bones()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var instance = Object.Instantiate(model);
        try
        {
            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
            string Line(string name)
            {
                var all = instance.GetComponentsInChildren<Transform>(true);
                var bone = all.FirstOrDefault(t => t.name == name);
                if (bone == null) return name + "=missing";
                var p = bone.localPosition;
                var parent = bone.parent != null ? bone.parent.name : "-";
                return name + " parent=" + parent
                    + " local=" + p.x.ToString("0.000", CultureInfo.InvariantCulture)
                    + "," + p.y.ToString("0.000", CultureInfo.InvariantCulture)
                    + "," + p.z.ToString("0.000", CultureInfo.InvariantCulture);
            }
            return string.Join(" | ", new[]
            {
                "avatar=" + (animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman),
                Line("LeftArm"), Line("LeftForeArm"), Line("LeftHand"),
                Line("LeftUpLeg"), Line("LeftLeg"), Line("LeftFoot"), Line("LeftToeBase"),
                Line("Hips"), Line("Head")
            });
        }
        finally { Object.DestroyImmediate(instance); }
    }

    public static string Sample()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var animator = instance.GetComponentInChildren<Animator>(true);
            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            string Pose(string state, float time)
            {
                animator.Play(state, 0, time);
                animator.Update(0f);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                Vector3 Local(HumanBodyBones bone)
                {
                    var t = animator.GetBoneTransform(bone);
                    return t == null ? Vector3.zero : instance.transform.InverseTransformPoint(t.position);
                }
                var leftFoot = Local(HumanBodyBones.LeftFoot);
                var rightFoot = Local(HumanBodyBones.RightFoot);
                var hand = Local(HumanBodyBones.LeftHand);
                var size = renderer.bounds.size;
                return state + "@" + time.ToString("0.00", CultureInfo.InvariantCulture)
                    + " Lfoot=" + P(leftFoot) + " Rfoot=" + P(rightFoot) + " Lhand=" + P(hand)
                    + " size=" + size.x.ToString("0.00", CultureInfo.InvariantCulture)
                    + "," + size.y.ToString("0.00", CultureInfo.InvariantCulture)
                    + "," + size.z.ToString("0.00", CultureInfo.InvariantCulture)
                    + " hipsY=" + hips.position.y.ToString("0.00", CultureInfo.InvariantCulture);
            }
            var material = renderer.sharedMaterial;
            return "material=" + (material != null ? material.name : "none")
                + " forward=" + P(instance.transform.forward)
                + " | " + Pose("Idle", 0f)
                + " | " + Pose("Walk", 0.05f)
                + " | " + Pose("Walk", 0.30f)
                + " | " + Pose("Walk", 0.55f)
                + " | " + Pose("Walk", 0.80f)
                + " | " + Pose("Attack", 0.15f)
                + " | " + Pose("Attack", 0.45f)
                + " | " + Pose("Run", 0.25f)
                + " | " + Pose("Run", 0.55f);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    public static string Measure()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try { return Describe(instance); }
        finally { Object.DestroyImmediate(instance); }
    }

    public static string Apply()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.autoGenerateAvatarMappingIfUnspecified = true;
        importer.importAnimation = false;
        var serialized = new SerializedObject(importer);
        serialized.FindProperty("m_HumanDescription.m_Human").ClearArray();
        serialized.FindProperty("m_HumanDescription.m_Skeleton").ClearArray();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        importer.SaveAndReimport();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try { return Describe(instance); }
        finally { Object.DestroyImmediate(instance); }
    }

    static string Describe(GameObject instance)
    {
        var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        {
            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var animator = instance.GetComponentInChildren<Animator>(true);
            var health = instance.GetComponent<Health>();
            var ai = instance.GetComponent<ChickenBroAI>();
            string bones = "none";
            string size = "none";
            if (renderer != null)
            {
                bones = renderer.bones != null ? renderer.bones.Length.ToString() : "0";
                var bounds = renderer.bounds.size;
                size = bounds.x.ToString("0.00", CultureInfo.InvariantCulture) + ","
                    + bounds.y.ToString("0.00", CultureInfo.InvariantCulture) + ","
                    + bounds.z.ToString("0.00", CultureInfo.InvariantCulture);
            }

            string Pose(string state, float time)
            {
                if (animator == null || animator.runtimeAnimatorController == null) return state + ":no-animator";
                animator.Play(state, 0, time);
                animator.Update(0f);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var thigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                var shin = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var arm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                var shinLength = shin != null && foot != null ? Vector3.Distance(shin.position, foot.position) : -1f;
                var armSpan = arm != null && hand != null ? Vector3.Distance(arm.position, hand.position) : -1f;
                return state + " hips=" + P(hips) + " thigh=" + P(thigh) + " shin=" + P(shin)
                    + " foot=" + P(foot) + " arm=" + P(arm) + " hand=" + P(hand)
                    + " shinLen=" + shinLength.ToString("0.00", CultureInfo.InvariantCulture)
                    + " armLen=" + armSpan.ToString("0.00", CultureInfo.InvariantCulture);
            }

            var rest = Pose("Idle", 0f);
            var walk = Pose("Walk", 0.35f);
            var punch = Pose("Attack", 0.4f);
            return "avatar=" + (avatar != null && avatar.isValid && avatar.isHuman)
                + " renderer=" + (renderer != null)
                + " bones=" + bones
                + " size=" + size
                + " controller=" + (animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "none")
                + " hp=" + (health != null ? health.maxHealth.ToString(CultureInfo.InvariantCulture) : "none")
                + " strike=" + (ai != null ? ai.strikeDamage.ToString(CultureInfo.InvariantCulture) : "none")
                + " lunge=" + (ai != null ? ai.lungeDamage.ToString(CultureInfo.InvariantCulture) : "none")
                + " | " + rest
                + " | " + walk
                + " | " + punch;
        }
    }

    static string P(Transform bone)
    {
        if (bone == null) return "missing";
        return P(bone.position);
    }

    static string P(Vector3 p)
    {
        return p.x.ToString("0.00", CultureInfo.InvariantCulture) + ","
            + p.y.ToString("0.00", CultureInfo.InvariantCulture) + ","
            + p.z.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
