using System;
using Mavis;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

// Re-runnable, narrowly scoped setup for the First Island boss prefab.
// The encounter scene holds a prefab instance, so it inherits these changes.
public static class NailongBossSetup
{
    const string PrefabPath = "Assets/Models/Nailong/Nailong.prefab";
    const string ControllerPath = "Assets/Models/Nailong/NailongBoss.controller";
    const string AnimationLibraryPath =
        "Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx";

    [MenuItem("Tools/Boss/Finish Nailong Boss")]
    public static void Build()
    {
        AnimationClip idle = FindClip("Idle_Loop");
        AnimationClip walk = FindClip("Jog_Fwd_Loop");
        AnimationClip stumble = FindClip("Hit_Chest");
        AnimationClip death = FindClip("Death01");
        if (idle == null || walk == null || stumble == null || death == null)
        {
            Debug.LogError("[Nailong] Required CC0 clips are missing; prefab was not changed.");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states)
            machine.RemoveState(child.state);

        AnimatorState idleState = AddState(machine, "Idle", idle);
        AddState(machine, "Walk", walk);
        AddState(machine, "Stumble", stumble);
        AddState(machine, "Death", death);
        machine.defaultState = idleState;
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError("[Nailong] Boss prefab was not found: " + PrefabPath);
            return;
        }

        try
        {
            root.transform.localScale = Vector3.one * 1.6f;
            root.tag = "Enemy";

            Animator animator = root.GetComponent<Animator>();
            NailongHealth health = root.GetComponent<NailongHealth>();
            NailongAttack attack = root.GetComponent<NailongAttack>();
            NailongAI ai = root.GetComponent<NailongAI>();
            NailongAttackMotion motion = root.GetComponent<NailongAttackMotion>();
            if (animator == null || health == null || attack == null || ai == null)
                throw new InvalidOperationException("Nailong prefab is missing an existing boss component.");
            if (motion == null) root.AddComponent<NailongAttackMotion>();
            Transform armature = root.transform.Find("Armature");
            if (armature != null) armature.localPosition = new Vector3(0f, -0.275f, 0f);

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            health.maxHealth = 160f;
            health.currentHealth = 160f;

            attack.damage = 9f;
            attack.attackArc = 130f;
            attack.verticalTolerance = 2.5f;
            attack.targetTag = "Player";

            ai.targetTag = "Player";
            ai.sightRange = 12f;
            ai.attackRange = 2.8f;
            ai.attackInterval = 0.88f;
            ai.giveUpRange = 35f;
            ai.feignDuration = 1.1f;
            ai.chaseSpeed = 4.2f;
            ai.turnSpeed = 600f;
            ai.visualGroundDrop = 0.275f;
            ai.animator = animator;
            ai.health = health;
            ai.attack = attack;

            // Retained for prefab compatibility, but damage is now applied by
            // NailongAttack at an exact swipe frame rather than child triggers.
            Transform oldHitbox = root.transform.Find("NailongHitbox");
            if (oldHitbox != null && oldHitbox.TryGetComponent(out Collider hitbox))
                hitbox.enabled = false;

            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = ai.chaseSpeed;
                agent.stoppingDistance = ai.attackRange * 0.82f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            if (!success) throw new InvalidOperationException("Prefab save failed.");
            AssetDatabase.SaveAssets();
            Debug.Log("[Nailong] Boss ready: 1.6x scale, feigned injury, chase, custom claws and shove, 160 HP.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip)
    {
        AnimatorState state = machine.AddState(name);
        state.motion = clip;
        state.writeDefaultValues = true;
        return state;
    }

    static AnimationClip FindClip(string suffix)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(AnimationLibraryPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                (clip.name == suffix || clip.name.EndsWith("|" + suffix, StringComparison.Ordinal)))
                return clip;
        }
        return null;
    }
}
