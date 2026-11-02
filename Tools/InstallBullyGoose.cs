using System.Collections.Generic;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallBullyGoose
{
    const string Folder = "Assets/Game/Prefabs/Enemies/BullyGoose";
    const string ModelPath = Folder + "/BullyGoose.fbx";
    const string ControllerPath = Folder + "/BullyGoose.controller";
    const string MaterialPath = Folder + "/BullyGoose.mat";
    const string PrefabPath = Folder + "/BullyGoose.prefab";
    const string EncounterPath = "Assets/Scenes/First Island/Enemies.unity";
    const string ArenaPath = "Assets/Scenes/Combat Test/CombatTest.unity";
    const float SizeFactor = 1.3f;
    const float TargetHeight = 4.6f * SizeFactor;
    static string footReport = "";

    public static string Apply()
    {
        if (Application.isPlaying) return "playing";
        if (!System.IO.File.Exists(ModelPath)) return "missing " + ModelPath;
        AssetDatabase.Refresh();
        var model = ConfigureModel();
        var controller = BuildController();
        var prefab = BuildPrefab(controller);
        AssignTextures();
        string encounter = WireEncounter(prefab);
        string arena = WireArena(prefab);
        AssetDatabase.SaveAssets();
        return Verify(prefab) + " | " + footReport + " | " + encounter + " | " + arena + " | clips=" + model;
    }

    public static string Tune()
    {
        if (Application.isPlaying) return "playing";
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator = contents.GetComponentInChildren<Animator>(true);
            var ai = contents.GetComponent<ChickenBroAI>();
            string report = MatchFootSpeed(animator, ai);
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            AssetDatabase.SaveAssets();
            return report;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    public static string Pose()
    {
        if (Application.isPlaying) return "playing";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab) return "prefab missing";
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var animator = instance.GetComponentInChildren<Animator>();
            AnimationClip Find(string token) => animator.runtimeAnimatorController.animationClips
                .First(candidate => candidate.name.Contains(token));
            var peck = Find("Peck");
            var wing = Find("Wing");
            var run = Find("Run");
            Transform Bone(string name) => instance.GetComponentsInChildren<Transform>(true).First(transform => transform.name == name);
            var head = Bone("Head");
            var neck = Bone("Neck2");
            var wingBone = Bone("WingL2");
            var foot = Bone("FootL");
            AnimationMode.StartAnimationMode();
            Vector3 Sample(AnimationClip clip, float time, Transform bone)
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(animator.gameObject, clip, time);
                AnimationMode.EndSampling();
                return bone.position;
            }
            var headRest = Sample(peck, 0.02f, head);
            var headHit = Sample(peck, peck.length * 0.47f, head);
            var neckRest = Sample(peck, 0.02f, neck);
            var neckHit = Sample(peck, peck.length * 0.47f, neck);
            var wingRest = Sample(wing, 0.02f, wingBone);
            var wingHit = Sample(wing, wing.length * 0.53f, wingBone);
            var footA = Sample(run, 0.05f, foot);
            var footB = Sample(run, run.length * 0.45f, foot);
            AnimationMode.StopAnimationMode();
            return "peck=" + peck.length.ToString("0.00")
                + " head=" + (headHit - headRest)
                + " neck=" + (neckHit - neckRest)
                + " wing=" + wing.length.ToString("0.00") + " tip=" + (wingHit - wingRest)
                + " run=" + run.length.ToString("0.00") + " foot=" + (footB - footA);
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(instance);
        }
    }

    static string ConfigureModel()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.SaveAndReimport();
        var descriptions = importer.defaultClipAnimations;
        foreach (var description in descriptions)
        {
            string name = description.name;
            description.loopTime = name.Contains("Idle") || name.Contains("Walk") || name.Contains("Run");
            description.lockRootRotation = true;
            description.lockRootHeightY = true;
            description.lockRootPositionXZ = true;
            description.keepOriginalOrientation = true;
            description.keepOriginalPositionY = true;
            description.keepOriginalPositionXZ = true;
        }
        importer.clipAnimations = descriptions;
        importer.SaveAndReimport();
        return string.Join(",", descriptions.Select(description => description.name));
    }

    static AnimatorController BuildController()
    {
        // Keep the reviewed continuous gait and periodic clips on reinstall.
        var polished = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/Animations/Polish034/BullyGoosePolish034.controller");
        if (polished) return polished;
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Motion", AnimatorControllerParameterType.Int);
        var machine = controller.layers[0].stateMachine;
        var wanted = new (string clip, string state, int motion, float speed)[]
        {
            ("Idle", "Idle", 0, 1f),
            ("Walk", "Walk", 1, 1f),
            ("Run", "Run", 2, 1f),
            ("Peck", "Strike", 3, 1f),
            ("Lunge", "Lunge", 4, 1f),
            ("Flinch", "Flinch", 5, 1f),
            ("Knockdown", "Knockdown", 6, 1f),
            ("Death", "Death", 7, 1f),
            ("Alert", "Alert", 8, 1f),
            ("Wing", "Wing", 9, 1f),
        };
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(clip => clip && !clip.name.StartsWith("__preview")).ToList();
        AnimatorState idle = null;
        foreach (var item in wanted)
        {
            var clip = clips.FirstOrDefault(candidate => candidate.name.Contains(item.clip));
            if (!clip) throw new System.InvalidOperationException("Missing clip " + item.clip + " in " + string.Join(",", clips.Select(candidate => candidate.name)));
            var state = machine.AddState(item.state);
            state.motion = clip;
            state.speed = item.speed;
            if (item.motion == 0) idle = state;
            var transition = machine.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.Equals, item.motion, "Motion");
            transition.hasExitTime = false;
            transition.duration = item.motion >= 3 ? 0.08f : 0.12f;
            transition.canTransitionToSelf = false;
        }
        machine.defaultState = idle;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static GameObject BuildPrefab(RuntimeAnimatorController controller)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("BullyGoose");
        EditorSceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            root.tag = "Enemy";
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            visual.name = "BullyGooseModel";
            visual.transform.SetParent(root.transform, false);
            var animator = visual.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            renderer.sharedMaterial = GooseMaterial();
            renderer.updateWhenOffscreen = false;
            visual.transform.localScale = Vector3.one;
            visual.transform.localPosition = Vector3.zero;
            float raw = renderer.bounds.size.y;
            float scale = raw > 0.05f ? TargetHeight / raw : 1f;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = new Vector3(0f, -renderer.bounds.min.y, 0f);
            var capsule = root.AddComponent<CharacterController>();
            capsule.height = Mathf.Max(1.2f, renderer.bounds.size.y);
            capsule.radius = 0.72f * SizeFactor;
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
            capsule.stepOffset = 0.4f * SizeFactor;
            capsule.slopeLimit = 45f;
            capsule.skinWidth = 0.06f * SizeFactor;
            var health = root.AddComponent<Health>();
            health.maxHealth = 75f;
            health.currentHealth = 75f;
            var ai = root.AddComponent<ChickenBroAI>();
            ai.sightRange = 32f;
            ai.giveUpRange = 50f;
            ai.matchLocomotionToMovement = true;
            ai.meleeRange = 3.4f * SizeFactor;
            ai.lungeMin = 3.6f * SizeFactor;
            ai.lungeMax = 8.2f * SizeFactor;
            ai.walkSpeed = 2f;
            ai.runSpeed = 5f;
            ai.lungeSpeed = 11f;
            ai.turnSpeed = 460f;
            ai.surroundDistance = 5.5f * SizeFactor;
            ai.separationRadius = 2.1f * SizeFactor;
            ai.strikeDamage = 4.8f;
            ai.lungeDamage = 8f;
            ai.strikeDuration = ClipLength(animator, "Peck", 0.6f);
            ai.lungeDuration = ClipLength(animator, "Lunge", 1f);
            ai.attackCooldown = 1.25f;
            ai.strikeWindowStart = 0.38f;
            ai.strikeWindowEnd = 0.58f;
            ai.lungeWindowStart = 0.26f;
            ai.lungeWindowEnd = 0.7f;
            ai.attackRadius = 0.95f * SizeFactor;
            ai.lightSlide = 1.85f;
            ai.heavySlide = 3.7f;
            ai.launchSlide = 5.4f;
            ai.launchHop = 5.8f;
            ai.flinchSeconds = 0.32f;
            ai.knockdownSeconds = 0.9f;
            ai.courtesySeconds = 0.5f;
            ai.alertSeconds = 0.95f;
            ai.peckReach = 1.35f * SizeFactor;
            ai.wingDamage = 6f;
            ai.wingRange = 2.15f * SizeFactor;
            ai.wingDuration = ClipLength(animator, "Wing", 0.55f);
            ai.wingWindowStart = 0.42f;
            ai.wingWindowEnd = 0.66f;
            ai.wingRadius = 1.55f * SizeFactor;
            ai.wingRecovery = 0.45f;
            ai.lungePush = 7f;
            ai.wingPush = 4f;
            ai.attackPriority = true;
            ai.shoveSmallerEnemies = true;
            root.AddComponent<BullyGooseVoice>();
            var audio = root.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.playOnAwake = false;
            audio.loop = false;
            audio.maxDistance = 28f;
            audio.minDistance = 2f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            footReport = MatchFootSpeed(animator, ai);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing) AssetDatabase.DeleteAsset(PrefabPath);
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static float ClipLength(Animator animator, string token, float fallback)
    {
        var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(candidate => candidate && candidate.name.Contains(token));
        return clip ? clip.length : fallback;
    }

    static string MatchFootSpeed(Animator animator, ChickenBroAI ai)
    {
        var machine = ((AnimatorController)animator.runtimeAnimatorController).layers[0].stateMachine;
        string report = "";
        foreach (var child in machine.states)
        {
            var clip = child.state.motion as AnimationClip;
            if (!clip) continue;
            float move = child.state.name == "Run" ? ai.runSpeed : child.state.name == "Walk" ? ai.walkSpeed : 0f;
            if (move <= 0f) continue;
            float travel = FootTravel(animator, clip);
            float playback = travel > 0.15f ? move * clip.length / (2f * travel) : 1f;
            playback = Mathf.Clamp(playback, 0.6f, 2f);
            child.state.speed = playback;
            report += child.state.name + "=" + playback.ToString("0.00") + "@" + travel.ToString("0.00") + " ";
        }
        EditorUtility.SetDirty(animator.runtimeAnimatorController);
        return report;
    }

    static float FootTravel(Animator animator, AnimationClip clip)
    {
        Transform left = null, right = null;
        foreach (var bone in animator.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name == "FootL") left = bone;
            if (bone.name == "FootR") right = bone;
        }
        if (!left || !right) return 0f;
        AnimationMode.StartAnimationMode();
        float minL = 999f, maxL = -999f, minR = 999f, maxR = -999f;
        try
        {
            int steps = 8;
            for (int i = 0; i <= steps; i++)
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(animator.gameObject, clip, clip.length * i / steps);
                AnimationMode.EndSampling();
                minL = Mathf.Min(minL, left.position.z);
                maxL = Mathf.Max(maxL, left.position.z);
                minR = Mathf.Min(minR, right.position.z);
                maxR = Mathf.Max(maxR, right.position.z);
            }
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        }
        return Mathf.Max(maxL - minL, maxR - minR);
    }

    static void AssignTextures()
    {
        const string folder = Folder + "/Textures";
        var normalPath = folder + "/texture_pbr_20250901_normal.png";
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/texture_pbr_20250901.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var material = GooseMaterial();
        if (albedo) material.SetTexture("_BaseMap", albedo);
        if (normal)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 1f);
        }
        material.SetFloat("_Smoothness", 0.42f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var renderer = prefab ? prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
        if (renderer) renderer.sharedMaterial = material;
    }

    static Material GooseMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.42f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static string WireEncounter(GameObject prefab)
    {
        var scene = EditorSceneManager.GetSceneByPath(EncounterPath);
        if (!scene.IsValid() || !scene.isLoaded) return "encounter scene not loaded";
        ChickenBroEncounter encounter = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            encounter = root.GetComponentInChildren<ChickenBroEncounter>(true);
            if (encounter) break;
        }
        if (!encounter) return "encounter missing";
        encounter.goose = prefab.GetComponent<ChickenBroAI>();
        encounter.goosePerWave = 1;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "encounter goose=" + encounter.goose.name + " perWave=" + encounter.goosePerWave
            + " duck=" + (encounter.duck ? encounter.duck.name : "missing");
    }

    static string WireArena(GameObject prefab)
    {
        var scene = EditorSceneManager.GetSceneByPath(ArenaPath);
        bool opened = false;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Additive);
            opened = true;
        }
        CombatTestArena arena = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            arena = root.GetComponentInChildren<CombatTestArena>(true);
            if (arena) break;
        }
        if (!arena)
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            return "arena missing";
        }
        arena.goose = prefab;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        return "arena goose=" + arena.goose.name;
    }

    static string Verify(GameObject prefab)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var health = instance.GetComponent<Health>();
            var ai = instance.GetComponent<ChickenBroAI>();
            var animator = instance.GetComponentInChildren<Animator>();
            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            var capsule = instance.GetComponent<CharacterController>();
            bool avatar = animator && animator.avatar && animator.avatar.isValid;
            string bones = string.Join(",", new[] { "Neck1", "Neck4", "Head", "WingL", "WingL2", "ThighL", "FootL", "FootR" }.Select(name =>
            {
                var bone = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(transform => transform.name == name);
                return bone ? name : name + "?";
            }));
            int motions = animator && animator.runtimeAnimatorController ? animator.runtimeAnimatorController.animationClips.Length : 0;
            return "hp=" + health.maxHealth
                + " run=" + ai.runSpeed.ToString("0.00")
                + " walk=" + ai.walkSpeed.ToString("0.00")
                + " peck=" + ai.strikeDamage.ToString("0.00")
                + " charge=" + ai.lungeDamage.ToString("0.00")
                + " wing=" + ai.wingDamage.ToString("0.00")
                + " height=" + capsule.height.ToString("0.00")
                + " avatar=" + avatar
                + " human=" + (animator && animator.avatar && animator.avatar.isHuman)
                + " clips=" + motions
                + " bones=" + bones
                + " tex=" + (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_BaseMap") && renderer.sharedMaterial.GetTexture("_BaseMap")
                    ? renderer.sharedMaterial.GetTexture("_BaseMap").name : "none");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
