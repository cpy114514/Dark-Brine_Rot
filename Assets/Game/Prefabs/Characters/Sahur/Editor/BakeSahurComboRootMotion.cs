using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mavis
{
    /// <summary>Bakes the source FBX root pose into collision-safe combo motion curves.</summary>
    public static class BakeSahurComboRootMotion
    {
        const string Root = "Assets/Game/Prefabs/Characters/Sahur/";
        const string Source = Root + "Animations/Source/SwordAndShieldSlash_ThreeHit.fbx";
        const string Prefab = Root + "SahurPlayer.prefab";

        [MenuItem("Mavis/Sahur/Legacy/Bake Sword-And-Shield Root Motion")]
        public static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                Root + "Animations/SahurGrounded.controller");
            if (controller == null || controller.layers[0].stateMachine.states
                    .Where(item => item.state.name.StartsWith("Combo "))
                    .Any(item => item.state.motion == null ||
                                 !item.state.motion.name.StartsWith("SahurSwordCombo")))
                throw new InvalidOperationException(
                    "Legacy root-motion curves require the Sword And Shield combo clips. " +
                    "Use Setup Three-Hit Stick Combo for the current Sahur animations.");
            var clips = AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().ToArray();
            var prefab = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var attack = prefab.GetComponent<SahurAttack>();
                if (attack == null) throw new InvalidOperationException("SahurAttack is missing.");
                var profiles = new SahurAttack.ComboMotionProfile[3];
                for (int index = 0; index < profiles.Length; index++)
                {
                    string name = "SahurSwordCombo" + (index + 1);
                    var clip = clips.FirstOrDefault(item => item.name == name);
                    if (clip == null) throw new InvalidOperationException("Animation clip missing: " + name);
                    profiles[index] = Bake(clip);
                    Debug.Log($"[Sahur] {name}: forward {profiles[index].forward.Evaluate(1f):F3}m, " +
                              $"right {profiles[index].right.Evaluate(1f):F3}m, " +
                              $"yaw {profiles[index].yaw.Evaluate(1f) - profiles[index].yaw.Evaluate(0f):F1}deg");
                }
                attack.comboMotionProfiles = profiles;
                attack.comboMotionScale = 1f;
                EditorUtility.SetDirty(attack);
                PrefabUtility.SaveAsPrefabAsset(prefab, Prefab);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        static SahurAttack.ComboMotionProfile Bake(AnimationClip clip)
        {
            AnimationCurve x = RootCurve(clip, "RootT.x");
            AnimationCurve y = RootCurve(clip, "RootT.y");
            AnimationCurve z = RootCurve(clip, "RootT.z");
            AnimationCurve qx = RootCurve(clip, "RootQ.x");
            AnimationCurve qy = RootCurve(clip, "RootQ.y");
            AnimationCurve qz = RootCurve(clip, "RootQ.z");
            AnimationCurve qw = RootCurve(clip, "RootQ.w");
            int count = Mathf.Max(2, Mathf.CeilToInt(clip.length * 120f));
            var xs = new float[count + 1];
            var ys = new float[count + 1];
            var zs = new float[count + 1];
            var angles = new float[count + 1];
            float x0 = x.Evaluate(0f), y0 = y.Evaluate(0f), z0 = z.Evaluate(0f);
            float previousRawYaw = 0f;
            for (int i = 0; i <= count; i++)
            {
                float time = clip.length * i / count;
                xs[i] = x.Evaluate(time) - x0;
                ys[i] = y.Evaluate(time) - y0;
                zs[i] = z.Evaluate(time) - z0;
                var rotation = new Quaternion(qx.Evaluate(time), qy.Evaluate(time),
                    qz.Evaluate(time), qw.Evaluate(time));
                rotation.Normalize();
                Vector3 direction = rotation * Vector3.forward;
                float rawYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                angles[i] = i == 0 ? rawYaw : angles[i - 1] + Mathf.DeltaAngle(previousRawYaw, rawYaw);
                previousRawYaw = rawYaw;
            }
            return new SahurAttack.ComboMotionProfile
            {
                right = Curve(xs), up = Curve(ys), forward = Curve(zs), yaw = Curve(angles)
            };
        }

        static AnimationCurve RootCurve(AnimationClip clip, string property)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .FirstOrDefault(item => item.propertyName == property);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"{clip.name} lacks {property} root motion.");
            return curve;
        }

        static AnimationCurve Curve(float[] samples)
        {
            var keys = new Keyframe[samples.Length];
            int last = samples.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                int before = Mathf.Max(0, i - 1), after = Mathf.Min(last, i + 1);
                float slope = (samples[after] - samples[before]) * last / (after - before);
                keys[i] = new Keyframe((float)i / last, samples[i], slope, slope);
            }
            return new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever };
        }
    }
}
