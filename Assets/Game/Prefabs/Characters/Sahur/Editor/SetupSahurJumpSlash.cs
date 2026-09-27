using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mavis
{
    /// <summary>Retarget KayKit's CC0 jump chop and wire it to Sahur's existing Animator.</summary>
    public static class SetupSahurJumpSlash
    {
        const string Root = "Assets/Game/Prefabs/Characters/Sahur";
        const string SourcePath = Root + "/Animations/Gameplay/KayKitSkeletons/Skeleton_Warrior.fbx";
        const string ClipPath = Root + "/Animations/Gameplay/KayKitSkeletons/SahurJumpSlash.anim";
        const string ControllerPath = Root + "/Animations/SahurGrounded.controller";
        const string PrefabPath = Root + "/SahurPlayer.prefab";

        [MenuItem("Mavis/Sahur/Setup Jump Slash")]
        public static void Run()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("KayKit source is missing; keep the extracted .anim or re-download the source FBX.");
                if (importer.animationType != ModelImporterAnimationType.Human ||
                    importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = true;
                    importer.SaveAndReimport();
                }

                var sourceClip = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>()
                    .FirstOrDefault(item => item.name == "1H_Melee_Attack_Jump_Chop");
                if (sourceClip == null || !sourceClip.humanMotion)
                    throw new InvalidOperationException("KayKit 1H_Melee_Attack_Jump_Chop did not import as a Humanoid clip.");
                clip = UnityEngine.Object.Instantiate(sourceClip);
                clip.name = "SahurJumpSlash";
                AssetDatabase.CreateAsset(clip, ClipPath);
                AssetDatabase.SaveAssets();
            }
            if (!clip.humanMotion) throw new InvalidOperationException("Extracted jump slash is not a Humanoid clip.");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new InvalidOperationException("Sahur Animator controller is missing.");
            var machine = controller.layers[0].stateMachine;
            var jumpLoop = machine.states.Select(item => item.state)
                .FirstOrDefault(state => state.name == "Jump Loop");
            if (jumpLoop == null) throw new InvalidOperationException("Jump Loop state is missing.");
            var jumpSlash = machine.states.Select(item => item.state)
                .FirstOrDefault(state => state.name == "Jump Slash") ?? machine.AddState("Jump Slash");
            jumpSlash.motion = clip;
            jumpSlash.speed = 1.15f;
            var exit = jumpSlash.transitions.FirstOrDefault(item => item.destinationState == jumpLoop)
                       ?? jumpSlash.AddTransition(jumpLoop);
            exit.hasExitTime = true;
            exit.exitTime = 0.90f;
            exit.hasFixedDuration = true;
            exit.duration = 0.08f;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var attack = root.GetComponent<SahurAttack>();
                if (attack == null) throw new InvalidOperationException("SahurAttack is missing from the prefab.");
                attack.jumpSlashState = "Jump Slash";
                attack.jumpSlashDamageMultiplier = 1.7f;
                attack.jumpSlashHitWindow = new Vector2(0.16f, 0.82f);
                attack.landingImpactRadius = 1.35f;
                attack.landingImpactForwardOffset = 0.55f;
                EditorUtility.SetDirty(attack);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"[Sahur] Jump Slash ready: {clip.name}, {clip.length:0.00}s Humanoid animation.");
        }
    }
}
