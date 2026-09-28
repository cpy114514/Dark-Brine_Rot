using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Idempotently gives Sahur a masked charge layer above walking/running.</summary>
public static class ConfigureSahurMovingCharge
{
    const string Root = "Assets/Game/Prefabs/Characters/Sahur/Animations/";
    const string ControllerPath = Root + "SahurGrounded.controller";
    const string MaskPath = Root + "SahurChargeUpperBody.mask";
    const string LayerName = "Charge Upper Body";
    const string StateName = "Charge Windup";
    const string PhaseName = "ChargePhase";

    public static string Apply()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) throw new InvalidOperationException("Sahur controller is missing.");
        if (!controller.parameters.Any(parameter => parameter.name == PhaseName &&
            parameter.type == AnimatorControllerParameterType.Float))
            throw new InvalidOperationException("Sahur ChargePhase float parameter is missing.");

        var heavy = controller.layers[0].stateMachine.states
            .Select(child => child.state).FirstOrDefault(state => state.name == "Heavy Attack");
        if (heavy?.motion is not AnimationClip heavyClip)
            throw new InvalidOperationException("Sahur Heavy Attack clip is missing.");

        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        if (mask == null)
        {
            mask = new AvatarMask { name = "Sahur Charge Upper Body" };
            AssetDatabase.CreateAsset(mask, MaskPath);
        }
        // Body includes the hips in Humanoid rigs. Keep it on the locomotion
        // layer so the pelvis and both legs follow the walk/run stride.
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

        int layerIndex = Array.FindIndex(controller.layers, layer => layer.name == LayerName);
        if (layerIndex < 0)
        {
            controller.AddLayer(LayerName);
            layerIndex = Array.FindIndex(controller.layers, layer => layer.name == LayerName);
        }
        if (layerIndex <= 0) throw new InvalidOperationException("Charge layer was not created.");

        // AnimatorController.layers returns a copy; assign it back after edits.
        var layers = controller.layers;
        var layer = layers[layerIndex];
        layer.avatarMask = mask;
        layer.blendingMode = AnimatorLayerBlendingMode.Override;
        layer.defaultWeight = 0f;
        layer.syncedLayerIndex = -1;
        layer.iKPass = false;
        var machine = layer.stateMachine;
        if (machine == null) throw new InvalidOperationException("Charge layer state machine is missing.");
        var charge = machine.states.Select(child => child.state)
            .FirstOrDefault(state => state.name == StateName) ?? machine.AddState(StateName);
        charge.motion = heavyClip;
        charge.speed = 1f;
        charge.speedParameterActive = false;
        charge.timeParameterActive = true;
        charge.timeParameter = PhaseName;
        charge.writeDefaultValues = false;
        machine.defaultState = charge;
        layers[layerIndex] = layer;
        controller.layers = layers;

        EditorUtility.SetDirty(mask);
        EditorUtility.SetDirty(charge);
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(mask);
        AssetDatabase.SaveAssetIfDirty(controller);
        return Inspect();
    }

    public static string Inspect()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        if (controller == null || mask == null)
            throw new InvalidOperationException("Charge controller or mask is missing.");
        int layerIndex = Array.FindIndex(controller.layers, layer => layer.name == LayerName);
        if (layerIndex <= 0) throw new InvalidOperationException("Charge layer is missing.");
        var layer = controller.layers[layerIndex];
        var charge = layer.stateMachine.defaultState;
        if (layer.avatarMask != mask || layer.blendingMode != AnimatorLayerBlendingMode.Override ||
            charge == null || charge.name != StateName || !charge.timeParameterActive ||
            charge.timeParameter != PhaseName ||
            mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root) ||
            mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body) ||
            mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg) ||
            mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg) ||
            !mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm) ||
            !mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm))
            throw new InvalidOperationException("Sahur moving-charge mask or layer is misconfigured.");
        return $"Sahur moving charge ready: layer={layerIndex}, clip={charge.motion.name}, " +
               "pelvis/legs from locomotion, arms from charge windup.";
    }
}
