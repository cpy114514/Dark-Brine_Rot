using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mavis
{
    /// <summary>Retarget KayKit's one-handed strikes as Sahur's three stick hits.</summary>
    public static class SetupSahurCombo
    {
        const string Root = "Assets/Game/Prefabs/Characters/Sahur";
        const string ControllerPath = Root + "/Animations/SahurGrounded.controller";
        const string AnimationRoot = Root + "/Animations/Gameplay/KayKitSkeletons/";
        const string SourcePath = AnimationRoot + "Skeleton_Warrior.fbx";
        const string PrefabPath = Root + "/SahurPlayer.prefab";
        const string AudioPath = Root + "/Audio/";

        [MenuItem("Mavis/Sahur/Setup Stick Three-Hit Combo")]
        public static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new InvalidOperationException("Sahur Animator controller is missing.");

            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.states.Select(item => item.state)
                .FirstOrDefault(state => state.name == "Locomotion");
            if (locomotion == null) throw new InvalidOperationException("Locomotion state is missing.");

            string[] sourceNames =
            {
                "1H_Melee_Attack_Slice_Horizontal",
                "1H_Melee_Attack_Slice_Diagonal",
                "1H_Melee_Attack_Chop"
            };
            float[] speeds = { 1.15f, 1.2f, 1.15f };
            for (int index = 0; index < sourceNames.Length; index++)
            {
                var clip = LoadOrExtractClip(sourceNames[index], index + 1);

                string stateName = "Combo " + (index + 1);
                var state = machine.states.Select(item => item.state)
                    .FirstOrDefault(item => item.name == stateName) ?? machine.AddState(stateName);
                state.motion = clip;
                state.speed = speeds[index];

                var exit = state.transitions.FirstOrDefault(item => item.destinationState == locomotion)
                           ?? state.AddTransition(locomotion);
                exit.hasExitTime = true;
                exit.exitTime = index == 2 ? 0.82f : 0.92f;
                exit.hasFixedDuration = true;
                exit.duration = index == 2 ? 0.12f : 0.08f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var attack = root.GetComponent<SahurAttack>();
                if (attack == null) throw new InvalidOperationException("SahurAttack is missing from the prefab.");
                ConfigureStickHitbox(root, attack);
                var audio = root.GetComponent<AudioSource>();
                if (audio == null) audio = root.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.loop = false;
                audio.spatialBlend = 0.35f;
                audio.volume = 0.75f;
                attack.swingAudio = audio;
                attack.comboSwishes = new[]
                {
                    LoadAudio("swish-1.wav"),
                    LoadAudio("swish-5.wav"),
                    LoadAudio("swish-9.wav")
                };
                attack.heavySwish = LoadAudio("swish-12.wav");
                attack.comboOneHitWindow = new Vector2(0.24f, 0.58f);
                attack.comboTwoHitWindow = new Vector2(0.22f, 0.60f);
                attack.comboThreeHitWindow = new Vector2(0.28f, 0.70f);
                EditorUtility.SetDirty(attack);
                EditorUtility.SetDirty(audio);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("[Sahur] Three CC0 stick strikes and swing sounds are ready.");
        }

        static AnimationClip LoadOrExtractClip(string sourceName, int stage)
        {
            string clipPath = AnimationRoot + "SahurStickCombo" + stage + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip != null)
            {
                if (!clip.humanMotion) throw new InvalidOperationException("Stick combo clip is not Humanoid: " + clipPath);
                return clip;
            }

            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("KayKit source FBX is missing; retain the extracted .anim clips or re-download the source.");
            if (importer.animationType != ModelImporterAnimationType.Human ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.SaveAndReimport();
            }

            var source = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>()
                .FirstOrDefault(item => item.name == sourceName);
            if (source == null || !source.humanMotion)
                throw new InvalidOperationException("KayKit Humanoid clip is missing: " + sourceName);
            clip = UnityEngine.Object.Instantiate(source);
            clip.name = "SahurStickCombo" + stage;
            AssetDatabase.CreateAsset(clip, clipPath);
            AssetDatabase.SaveAssets();
            return clip;
        }

        static void ConfigureStickHitbox(GameObject root, SahurAttack attack)
        {
            var stick = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Sahur Stick");
            var mesh = stick != null ? stick.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (mesh == null) throw new InvalidOperationException("Sahur stick mesh is missing.");

            var box = stick.GetComponent<BoxCollider>();
            if (box == null) box = stick.gameObject.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size * 1.08f;
            box.isTrigger = true;
            box.enabled = false;
            attack.stickHitbox = box;

            var oldSphere = stick.GetComponent<SphereCollider>();
            if (oldSphere != null) UnityEngine.Object.DestroyImmediate(oldSphere);
            EditorUtility.SetDirty(box);
        }

        static AudioClip LoadAudio(string file)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + file);
            if (clip == null) throw new InvalidOperationException("Sahur swing sound is missing: " + file);
            return clip;
        }
    }
}
