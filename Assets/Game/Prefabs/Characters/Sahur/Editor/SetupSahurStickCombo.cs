using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mavis
{
    /// <summary>Rebuilds the three-hit combo with upright, stick-friendly clips and collision-safe steps.</summary>
    public static class SetupSahurStickCombo
    {
        const string Root = "Assets/Game/Prefabs/Characters/Sahur/";
        const string ControllerPath = Root + "Animations/SahurGrounded.controller";
        const string ClipRoot = Root + "Animations/Gameplay/KayKitSkeletons/";
        const string PrefabPath = Root + "SahurPlayer.prefab";

        [MenuItem("Mavis/Sahur/Setup Three-Hit Stick Combo")]
        public static void Run() => Debug.Log(Apply());

        public static string Apply()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new InvalidOperationException("Sahur Animator controller is missing.");
            var states = controller.layers[0].stateMachine.states;
            float[] speeds = { 1.25f, 1.18f, 1.25f };
            for (int i = 0; i < 3; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipRoot + $"SahurStickCombo{i + 1}.anim");
                var state = states.FirstOrDefault(item => item.state.name == $"Combo {i + 1}").state;
                if (clip == null || state == null)
                    throw new InvalidOperationException($"Combo {i + 1} clip or Animator state is missing.");
                state.motion = clip;
                state.speed = speeds[i];
                foreach (var transition in state.transitions.Where(t => t.destinationState != null &&
                             t.destinationState.name == "Locomotion"))
                {
                    transition.hasExitTime = true;
                    transition.exitTime = 0.92f;
                    transition.hasFixedDuration = true;
                    transition.duration = 0.09f;
                    EditorUtility.SetDirty(transition);
                }
                EditorUtility.SetDirty(state);
            }

            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var attack = prefab.GetComponent<SahurAttack>();
                if (attack == null || attack.stickHitbox == null)
                    throw new InvalidOperationException("SahurAttack or its stick is missing from the prefab.");

                // The previous two-metre-plus bat was sweeping through Sahur's body.
                // Scale its mesh and hand offset together to keep the same grip.
                Transform stick = attack.stickHitbox.transform;
                if (stick.parent != null && stick.parent.name == "Sahur Stick")
                    stick = stick.parent;
                stick.localScale = Vector3.one * 70f;
                stick.localPosition = new Vector3(0.1855f, 0.1505f, -0.5159f);
                Quaternion originalGrip = new Quaternion(-0.10834281f, 0.7552686f, 0.5405905f, 0.3543912f);
                stick.localRotation = originalGrip * Quaternion.Euler(0f, 45f, 0f);

                // A narrow capsule follows the actual shaft; the old mesh-sized
                // box covered the entire swing arc, including Sahur's torso.
                var oldBox = stick.GetComponent<BoxCollider>();
                if (oldBox != null) oldBox.enabled = false;
                Transform hitbox = stick.Find("Stick Hitbox");
                if (hitbox == null)
                {
                    var child = new GameObject("Stick Hitbox");
                    hitbox = child.transform;
                    hitbox.SetParent(stick, false);
                }
                hitbox.localPosition = new Vector3(0.00004f, 0.0036f, 0.00001f);
                hitbox.localRotation = Quaternion.FromToRotation(Vector3.up,
                    new Vector3(-0.53894f, 0.59857f, -0.59266f).normalized);
                hitbox.localScale = Vector3.one;
                var capsule = hitbox.GetComponent<CapsuleCollider>();
                if (capsule == null) capsule = hitbox.gameObject.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.center = Vector3.zero;
                capsule.height = 0.012f;
                capsule.radius = 0.00045f;
                capsule.isTrigger = true;
                capsule.enabled = false;
                attack.stickHitbox = capsule;

                attack.comboMotionProfiles = new[]
                {
                    Step(0.85f), Step(0.95f), Step(1.10f)
                };
                attack.comboMotionScale = 1f;
                attack.comboOneHitWindow = new Vector2(0.28f, 0.70f);
                attack.comboTwoHitWindow = new Vector2(0.25f, 0.70f);
                attack.comboThreeHitWindow = new Vector2(0.28f, 0.72f);
                EditorUtility.SetDirty(attack);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            AssetDatabase.SaveAssets();
            return "Sahur combo now uses three stick swings, forward steps, and a shaft-sized hitbox.";
        }

        static SahurAttack.ComboMotionProfile Step(float distance)
        {
            AnimationCurve still = AnimationCurve.Constant(0f, 1f, 0f);
            var forward = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.10f, 0f),
                new Keyframe(0.72f, distance),
                new Keyframe(1f, distance));
            forward.preWrapMode = WrapMode.ClampForever;
            forward.postWrapMode = WrapMode.ClampForever;
            return new SahurAttack.ComboMotionProfile
            {
                right = new AnimationCurve(still.keys),
                up = new AnimationCurve(still.keys),
                forward = forward,
                yaw = new AnimationCurve(still.keys)
            };
        }
    }
}
