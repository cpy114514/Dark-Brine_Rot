using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class ConfigureRightArmHeavy
{
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Stop Play first.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        var layers=controller.layers;
        int index=Array.FindIndex(layers,l=>l.name=="Charge Upper Body");
        if(index<=0) throw new Exception("Missing charge layer.");
        var layer=layers[index]; var mask=layer.avatarMask;
        Undo.RecordObject(mask,"Right arm only charged attack");
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm,true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers,true);
        // Disable all transform overrides as well: this is a Humanoid arm-only layer.
        for(int i=0;i<mask.transformCount;i++) mask.SetTransformActive(i,false);
        var windup=layer.stateMachine.states.Single(s=>s.state.name=="Charge Windup").state;
        var heavy=layer.stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Heavy Attack")
            ?? layer.stateMachine.AddState("Heavy Attack");
        heavy.motion=windup.motion; heavy.speed=1.1f; heavy.speedParameterActive=false;
        heavy.timeParameterActive=false; heavy.writeDefaultValues=false;
        foreach(var transition in heavy.transitions) heavy.RemoveTransition(transition);
        layer.defaultWeight=0; layer.blendingMode=AnimatorLayerBlendingMode.Override;
        layers[index]=layer; controller.layers=layers;
        EditorUtility.SetDirty(mask); EditorUtility.SetDirty(heavy); EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(mask); AssetDatabase.SaveAssetIfDirty(controller);
        return new { rightArmOnly=true,leftArm=false,body=false,head=false,legs=false,rootMotion=false,clip=heavy.motion.name,layer=index };
    }
}
