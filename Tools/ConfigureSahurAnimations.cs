using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureSahurAnimations
{
    const string ControllerPath = "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";
    const string PrefabPath = "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";

    public static string Run()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) return "Sahur Animator controller not found.";
        var machine = controller.layers[0].stateMachine;
        var locomotion = machine.states.FirstOrDefault(x => x.state.name == "Locomotion").state;
        var lightAttack = machine.states.FirstOrDefault(x => x.state.name == "Attack").state;
        if (locomotion == null || lightAttack == null || lightAttack.motion == null)
            return "Sahur locomotion or attack animation is missing.";

        var report = new StringBuilder();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var movement = prefab != null ? prefab.GetComponent<ThirdPersonPlayerController>() : null;
        var sceneMovement = Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None)
            .FirstOrDefault(x => x.name == "Sahur Player");
        var tuning = sceneMovement != null ? sceneMovement : movement;
        float walkSpeed = tuning != null ? tuning.moveSpeed : 7f;
        float runSpeed = walkSpeed * (tuning != null ? tuning.sprintMultiplier : 1.65f);
        report.AppendLine($"Movement source {(sceneMovement != null ? "scene" : "prefab")}: walk={walkSpeed:F2}, sprint={runSpeed:F2}");

        if (locomotion.motion is BlendTree tree)
        {
            var children = tree.children;
            for (int i = 0; i < children.Length; i++)
            {
                string name = children[i].motion != null ? children[i].motion.name : "";
                if (name.Contains("Idle")) children[i].threshold = 0f;
                else if (name.Contains("Walk")) children[i].threshold = walkSpeed;
                else if (name.Contains("Jog")) children[i].threshold = runSpeed;
                report.AppendLine($"Blend {name}: {children[i].threshold:F2}");
            }
            tree.children = children;
            EditorUtility.SetDirty(tree);
        }

        var charge = FindOrAddState(machine, "Charge Windup", new Vector3(580f, 180f));
        charge.motion = lightAttack.motion;
        charge.speed = 1f;
        charge.speedParameterActive = false;
        charge.timeParameterActive = true;
        charge.timeParameter = "ChargePhase";
        if (!controller.parameters.Any(parameter => parameter.name == "ChargePhase"))
            controller.AddParameter("ChargePhase", AnimatorControllerParameterType.Float);
        var heavy = FindOrAddState(machine, "Heavy Attack", new Vector3(820f, 180f));
        heavy.motion = lightAttack.motion;
        heavy.speed = 0.9f;

        ConfigureExit(lightAttack, locomotion, 0.89f, 0.16f);
        ConfigureExit(heavy, locomotion, 0.89f, 0.18f);
        foreach (var transition in machine.anyStateTransitions)
        {
            if (transition.destinationState != lightAttack) continue;
            transition.hasFixedDuration = true;
            transition.duration = 0.11f;
            transition.canTransitionToSelf = false;
            EditorUtility.SetDirty(transition);
        }

        EditorUtility.SetDirty(charge);
        EditorUtility.SetDirty(heavy);
        EditorUtility.SetDirty(lightAttack);
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(controller);

        if (prefab != null)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var player = root.GetComponent<ThirdPersonPlayerController>();
                var attack = root.GetComponent<Mavis.SahurAttack>();
                if (attack != null)
                {
                    attack.chargeClip = lightAttack.motion as AnimationClip;
                    attack.chargeTimeParameter = "ChargePhase";
                    attack.chargePoseTime = SahurAttack.SafeChargePoseTime;
                    EditorUtility.SetDirty(attack);
                }
                if (player != null)
                {
                    player.rollExitBlend = 0.11f;
                    EditorUtility.SetDirty(player);
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        if (sceneMovement != null)
        {
            sceneMovement.rollExitBlend = 0.11f;
            EditorUtility.SetDirty(sceneMovement);
            EditorSceneManager.MarkSceneDirty(sceneMovement.gameObject.scene);
            EditorSceneManager.SaveScene(sceneMovement.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        report.AppendLine("Added charge wind-up and heavy attack states; softened attack exits.");
        return report.ToString();
    }

    static AnimatorState FindOrAddState(AnimatorStateMachine machine, string name, Vector3 position)
    {
        foreach (var child in machine.states)
            if (child.state.name == name) return child.state;
        return machine.AddState(name, position);
    }

    static void ConfigureExit(AnimatorState from, AnimatorState to, float exitTime, float seconds)
    {
        var transition = from.transitions.FirstOrDefault(x => x.destinationState == to);
        if (transition == null) transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.hasFixedDuration = true;
        transition.duration = seconds;
        transition.offset = 0f;
        EditorUtility.SetDirty(transition);
    }
}
