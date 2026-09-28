using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mavis
{
    /// <summary>Restores the approved in-project Sword And Shield three-hit combo and original stick grip.</summary>
    public static class SetupSahurStickCombo
    {
        const string Root = "Assets/Game/Prefabs/Characters/Sahur/";
        const string ControllerPath = Root + "Animations/SahurGrounded.controller";
        const string ComboSource = Root + "Animations/Source/SwordAndShieldSlash_ThreeHit.fbx";
        const string PrefabPath = Root + "SahurPlayer.prefab";

        [MenuItem("Mavis/Sahur/Setup Sword-And-Shield Combo")]
        public static void Run() => Debug.Log(Apply());

        /// <summary>Fix only combo orientation, preserving clip slices, travel, timing, and the stick grip.</summary>
        public static string AlignComboRootOrientation()
        {
            var importer = AssetImporter.GetAtPath(ComboSource) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Sword And Shield source importer is missing.");
            var clips = importer.clipAnimations;
            var comboClips = clips.Where(clip => clip.name.StartsWith("SahurSwordCombo")).ToArray();
            if (comboClips.Length != 3)
                throw new InvalidOperationException("Expected all three Sword And Shield combo slices.");
            bool changed = false;
            foreach (var clip in comboClips) changed |= ConfigureComboRootOrientation(clip);
            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            return "Three combo slices extract trajectory yaw with Body Orientation, preserving natural local joint twists.";
        }

        static bool ConfigureComboRootOrientation(ModelImporterClipAnimation clip)
        {
            bool changed = clip.keepOriginalOrientation || clip.lockRootRotation ||
                           !Mathf.Approximately(clip.rotationOffset, 0f);
            // Separate whole-body trajectory yaw from the pose. Never cancel
            // a chest/head rotation by counter-rotating the entire model.
            clip.keepOriginalOrientation = false;
            clip.lockRootRotation = false;
            clip.rotationOffset = 0f;
            return changed;
        }

        public static string Apply()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new InvalidOperationException("Sahur Animator controller is missing.");
            var importer = AssetImporter.GetAtPath(ComboSource) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Sword And Shield source importer is missing.");
            var importedClips = importer.clipAnimations;
            bool importChanged = false;
            foreach (var clip in importedClips)
            {
                if (!clip.name.StartsWith("SahurSwordCombo")) continue;
                importChanged |= ConfigureComboRootOrientation(clip);
                importChanged |= clip.keepOriginalPositionY || !clip.heightFromFeet || clip.lockRootHeightY;
                // Feet-based Y keeps crouching in the pose while preserving
                // the source's real jump, instead of raising the capsule on recovery.
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                clip.lockRootHeightY = false;
            }
            if (importChanged)
            {
                importer.clipAnimations = importedClips;
                importer.SaveAndReimport();
            }
            var states = controller.layers[0].stateMachine.states;
            var clips = AssetDatabase.LoadAllAssetsAtPath(ComboSource).OfType<AnimationClip>().ToArray();
            for (int i = 0; i < 3; i++)
            {
                var clip = clips.FirstOrDefault(item => item.name == $"SahurSwordCombo{i + 1}");
                var state = states.FirstOrDefault(item => item.state.name == $"Combo {i + 1}").state;
                if (clip == null || state == null)
                    throw new InvalidOperationException($"Combo {i + 1} clip or Animator state is missing.");
                state.motion = clip;
                state.speed = 1.25f;
                foreach (var transition in state.transitions.Where(t => t.destinationState != null &&
                             t.destinationState.name == "Locomotion"))
                {
                    transition.hasExitTime = true;
                    transition.exitTime = 1f;
                    transition.hasFixedDuration = true;
                    transition.duration = 0.06f;
                    EditorUtility.SetDirty(transition);
                }
                EditorUtility.SetDirty(state);
            }

            const string chargePhase = "ChargePhase";
            for (int index = controller.parameters.Length - 1; index >= 0; index--)
                if (controller.parameters[index].name == "ChargeSpeed") controller.RemoveParameter(index);
            if (!controller.parameters.Any(item => item.name == chargePhase))
                controller.AddParameter(chargePhase, AnimatorControllerParameterType.Float);
            var charge = states.FirstOrDefault(item => item.state.name == "Charge Windup").state;
            var heavy = states.FirstOrDefault(item => item.state.name == "Heavy Attack").state;
            if (charge == null || heavy == null || heavy.motion is not AnimationClip heavyClip)
                throw new InvalidOperationException("The charge and heavy attack states need a source clip.");
            charge.motion = heavyClip;
            charge.speed = 1f;
            charge.speedParameterActive = false;
            charge.speedParameter = "";
            charge.timeParameterActive = true;
            charge.timeParameter = chargePhase;
            EditorUtility.SetDirty(charge);
            EditorUtility.SetDirty(controller);

            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var attack = prefab.GetComponent<SahurAttack>();
                if (attack == null || attack.stickHitbox == null)
                    throw new InvalidOperationException("SahurAttack or its stick is missing from the prefab.");

                // Restore the hand grip used before the KayKit replacement.
                Transform stick = attack.stickHitbox.transform;
                if (stick.parent != null && stick.parent.name == "Sahur Stick")
                    stick = stick.parent;
                stick.localScale = Vector3.one * 100f;
                stick.localPosition = new Vector3(0.265f, 0.215f, -0.737f);
                stick.localRotation = new Quaternion(-0.10834281f, 0.7552686f, 0.5405905f, 0.3543912f);

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

                attack.comboQueueOpen = 0.12f;
                attack.comboQueueClose = 0.96f;
                attack.comboChainPoint = 0.98f;
                attack.comboOneHitWindow = new Vector2(0.57f, 0.86f);
                attack.comboTwoHitWindow = new Vector2(0.34f, 0.75f);
                attack.comboThreeHitWindow = new Vector2(0.32f, 0.55f);
                attack.chargeTimeParameter = chargePhase;
                attack.chargeClip = heavyClip;
                attack.chargePoseTime = SahurAttack.SafeChargePoseTime;

                Transform visual = prefab.transform.Find("Pbr Sahur Visual");
                if (visual != null && visual.GetComponent<SahurRootMotionRelay>() == null)
                    visual.gameObject.AddComponent<SahurRootMotionRelay>();
                EditorUtility.SetDirty(attack);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            AssetDatabase.SaveAssetIfDirty(controller);
            return "Sahur now uses the in-project Sword And Shield three-hit clips, native root motion, and the original stick grip.";
        }
    }
}
