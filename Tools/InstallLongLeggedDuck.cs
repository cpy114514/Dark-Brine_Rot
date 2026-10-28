using System.Collections.Generic;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallLongLeggedDuck
{
    const string Folder = "Assets/Game/Prefabs/Enemies/LongLeggedDuck";
    const string ModelPath = Folder + "/LongLeggedDuck.fbx";
    const string ControllerPath = Folder + "/LongLeggedDuck.controller";
    const string MaterialPath = Folder + "/LongLeggedDuck.mat";
    const string PrefabPath = Folder + "/LongLeggedDuck.prefab";
    const string EncounterPath = "Assets/Scenes/First Island/Enemies.unity";
    const string ArenaPath = "Assets/Scenes/Combat Test/CombatTest.unity";

    public static string Textures()
    {
        if (Application.isPlaying) return "playing";
        const string folder = Folder + "/Textures";
        AssetDatabase.Refresh();
        var normalPath = folder + "/texture_pbr_20250901_normal.png";
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/texture_pbr_20250901.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material || !albedo) return "material or albedo missing";
        material.SetTexture("_BaseMap", albedo);
        if (normal)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 1f);
        }
        material.SetFloat("_Smoothness", 0.42f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var renderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
        return "albedo=" + albedo.name + " normal=" + (normal ? normal.name : "none")
            + " prefabMat=" + (renderer.sharedMaterial == material);
    }

    public static string Arena()
    {
        if (Application.isPlaying) return "playing";
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
        if (!arena.duck)
        {
            arena.duck = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        string result = "duck=" + (arena.duck ? arena.duck.name : "null");
        if (opened) EditorSceneManager.CloseScene(scene, true);
        return result;
    }

    public static string Pose()
    {
        if (Application.isPlaying) return "playing";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var animator = instance.GetComponentInChildren<Animator>();
            var clip = animator.runtimeAnimatorController.animationClips.First(candidate => candidate.name.Contains("Run"));
            Transform Foot() => instance.GetComponentsInChildren<Transform>(true).First(transform => transform.name == "FootL");
            var rest = Foot().position;
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(animator.gameObject, clip, 0.12f);
            AnimationMode.EndSampling();
            var posed = Foot().position;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            AnimationMode.StopAnimationMode();
            return "clip=" + clip.name + " length=" + clip.length.ToString("0.00") + " curves=" + bindings.Length
                + " first=" + (bindings.Length > 0 ? bindings[0].path : "none")
                + " footDelta=" + (posed - rest);
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(instance);
        }
    }

    public static string Grow()
    {
        if (Application.isPlaying) return "playing";
        const float targetHeight = 3.4f;
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var renderer = contents.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Transform visual = renderer != null ? renderer.transform.root == contents.transform
                ? (renderer.transform == contents.transform ? renderer.transform : contents.transform.GetChild(0))
                : contents.transform.GetChild(0) : null;
            if (renderer == null || renderer.sharedMesh == null)
            {
                if (contents.transform.childCount > 0)
                    Object.DestroyImmediate(contents.transform.GetChild(0).gameObject);
                var spawned = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
                spawned.transform.SetParent(contents.transform, false);
                visual = spawned.transform;
                renderer = spawned.GetComponentInChildren<SkinnedMeshRenderer>(true);
            }
            else
            {
                visual = contents.transform.GetChild(0);
                renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            }
            var animator = visual.GetComponentInChildren<Animator>(true);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material) renderer.sharedMaterial = material;
            visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            float raw = renderer.bounds.size.y;
            float scale = raw > 0.05f ? targetHeight / raw : 1f;
            visual.localScale = Vector3.one * scale;
            visual.localPosition = new Vector3(0f, -renderer.bounds.min.y, 0f);
            var capsule = contents.GetComponent<CharacterController>();
            capsule.height = renderer.bounds.size.y;
            capsule.radius = 0.55f;
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
            capsule.stepOffset = 0.35f;
            var ai = contents.GetComponent<ChickenBroAI>();
            ai.meleeRange = 2.45f;
            ai.lungeMin = 3.1f;
            ai.lungeMax = 6.8f;
            ai.attackRadius = 1.35f;
            ai.separationRadius = 1.85f;
            ai.surroundDistance = 5f;
            string speeds = MatchFootSpeed(animator, ai);
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            AssetDatabase.SaveAssets();
            return "height=" + capsule.height.ToString("0.00")
                + " radius=" + capsule.radius.ToString("0.00")
                + " scale=" + scale.ToString("0.00")
                + " mesh=" + renderer.bounds.size
                + " " + speeds;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    static string MatchFootSpeed(Animator animator, ChickenBroAI ai)
    {
        var machine = ((AnimatorController)animator.runtimeAnimatorController).layers[0].stateMachine;
        string report = "";
        foreach (var child in machine.states)
        {
            var clip = child.state.motion as AnimationClip;
            if (clip == null) continue;
            float move = child.state.name == "Run" ? ai.runSpeed : child.state.name == "Walk" ? ai.walkSpeed : 0f;
            if (move <= 0f) continue;
            float travel = FootTravel(animator, clip);
            float playback = travel > 0.2f ? move * clip.length / travel : child.state.speed;
            playback = Mathf.Clamp(playback, 0.75f, 1.8f);
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
        if (left == null || right == null) return 0f;
        AnimationMode.StartAnimationMode();
        float minL = 999f, maxL = -999f, minR = 999f, maxR = -999f;
        try
        {
            AnimationMode.BeginSampling();
            int steps = 8;
            for (int i = 0; i <= steps; i++)
            {
                AnimationMode.SampleAnimationClip(animator.gameObject, clip, clip.length * i / steps);
                minL = Mathf.Min(minL, left.position.z);
                maxL = Mathf.Max(maxL, left.position.z);
                minR = Mathf.Min(minR, right.position.z);
                maxR = Mathf.Max(maxR, right.position.z);
            }
            AnimationMode.EndSampling();
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        }
        return Mathf.Max(maxL - minL, maxR - minR);
    }

    public static string Apply()
    {
        if (Application.isPlaying) return "playing";
        var model = ConfigureModel();
        var controller = BuildController();
        var prefab = BuildPrefab(controller);
        string encounter = WireEncounter(prefab);
        string arena = WireArena(prefab);
        return Verify(prefab) + " | " + encounter + " | " + arena + " | clips=" + model;
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
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath))
            AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Motion", AnimatorControllerParameterType.Int);
        var machine = controller.layers[0].stateMachine;
        var wanted = new (string clip, string state, int motion, float speed)[]
        {
            ("Idle", "Idle", 0, 1f),
            ("Walk", "Walk", 1, 1.7f),
            ("Run", "Run", 2, 2.2f),
            ("Peck", "Strike", 3, 1f),
            ("Lunge", "Lunge", 4, 1f),
            ("Flinch", "Flinch", 5, 1f),
            ("Knockdown", "Knockdown", 6, 1f),
            ("Death", "Death", 7, 1f),
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
            transition.duration = item.motion >= 5 ? 0.06f : 0.1f;
            transition.canTransitionToSelf = false;
        }
        machine.defaultState = idle;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    static GameObject BuildPrefab(RuntimeAnimatorController controller)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var root = new GameObject("LongLeggedDuck");
        try
        {
            root.tag = "Enemy";
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "LongLeggedDuckModel";
            visual.transform.SetParent(root.transform, false);
            var animator = visual.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            renderer.sharedMaterial = RubberMaterial(renderer.sharedMaterial);
            float height = renderer.bounds.size.y;
            float scale = height > 0.05f ? 1.5f / height : 1f;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = new Vector3(0f, -renderer.bounds.min.y, 0f);
            var capsule = root.AddComponent<CharacterController>();
            float body = renderer.bounds.size.y;
            capsule.height = Mathf.Max(0.8f, body);
            capsule.radius = 0.28f;
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
            capsule.stepOffset = 0.22f;
            capsule.slopeLimit = 45f;
            capsule.skinWidth = 0.05f;
            var health = root.AddComponent<Health>();
            health.maxHealth = 20f;
            health.currentHealth = 20f;
            var ai = root.AddComponent<ChickenBroAI>();
            ai.sightRange = 18f;
            ai.giveUpRange = 28f;
            ai.meleeRange = 1.45f;
            ai.lungeMin = 2.3f;
            ai.lungeMax = 5.2f;
            ai.walkSpeed = 3.36f;
            ai.runSpeed = 7.68f;
            ai.lungeSpeed = 14.4f;
            ai.turnSpeed = 720f;
            ai.surroundDistance = 3.8f;
            ai.separationRadius = 1.2f;
            ai.strikeDamage = 2.25f;
            ai.lungeDamage = 3.75f;
            ai.strikeDuration = 0.53f;
            ai.lungeDuration = 0.8f;
            ai.attackCooldown = 0.82f;
            ai.strikeWindowStart = 0.34f;
            ai.strikeWindowEnd = 0.55f;
            ai.lungeWindowStart = 0.32f;
            ai.lungeWindowEnd = 0.62f;
            ai.attackRadius = 0.72f;
            ai.lightSlide = 5.2f;
            ai.heavySlide = 10f;
            ai.launchSlide = 14f;
            ai.launchHop = 11f;
            ai.flinchSeconds = 0.22f;
            ai.knockdownSeconds = 0.7f;
            ai.courtesySeconds = 0.3f;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing) AssetDatabase.DeleteAsset(PrefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static Material RubberMaterial(Material imported)
    {
        Texture albedo = imported ? imported.mainTexture : null;
        if (!albedo && imported && imported.HasProperty("_BaseMap")) albedo = imported.GetTexture("_BaseMap");
        if (!albedo && imported && imported.HasProperty("_MainTex")) albedo = imported.GetTexture("_MainTex");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        if (albedo) material.SetTexture("_BaseMap", albedo);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.42f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static string WireEncounter(GameObject prefab)
    {
        var scene = EditorSceneManager.GetSceneByPath(EncounterPath);
        if (!scene.IsValid() || !scene.isLoaded)
            return "encounter scene not loaded";
        ChickenBroEncounter encounter = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            encounter = root.GetComponentInChildren<ChickenBroEncounter>(true);
            if (encounter) break;
        }
        if (!encounter) return "encounter missing";
        encounter.duck = prefab.GetComponent<ChickenBroAI>();
        encounter.duckRatio = 0.3f;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "encounter duck=" + (encounter.duck ? encounter.duck.name : "missing") + " ratio=" + encounter.duckRatio.ToString("0.00");
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
        arena.duck = prefab;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        return "arena duck=" + (arena.duck ? arena.duck.name : "missing");
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
            string bones = string.Join(",", new[] { "ThighL", "ShinL", "FootL", "Head", "WingL" }.Select(name =>
            {
                var bone = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(transform => transform.name == name);
                return bone ? name : name + "?";
            }));
            return "hp=" + health.maxHealth
                + " run=" + ai.runSpeed.ToString("0.00")
                + " walk=" + ai.walkSpeed.ToString("0.00")
                + " strike=" + ai.strikeDamage.ToString("0.00")
                + " lungeDmg=" + ai.lungeDamage.ToString("0.00")
                + " height=" + capsule.height.ToString("0.00")
                + " radius=" + capsule.radius.ToString("0.00")
                + " mesh=" + renderer.bounds.size
                + " avatar=" + avatar
                + " human=" + (animator && animator.avatar && animator.avatar.isHuman)
                + " bones=" + bones
                + " mat=" + (renderer.sharedMaterial ? renderer.sharedMaterial.shader.name : "none")
                + " tex=" + (renderer.sharedMaterial && renderer.sharedMaterial.mainTexture ? renderer.sharedMaterial.mainTexture.name : "none");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
