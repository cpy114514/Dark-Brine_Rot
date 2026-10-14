using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallPreviousLightV062
{
    public static object Install()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        const string path="Assets/Resources/Encounter/v006/SahurGameplay.controller";
        const string version="Assets/Resources/Encounter/v062";
        if(!AssetDatabase.IsValidFolder(version))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v062");
        string backup=version+"/PreviousSahurGameplay.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(backup)!=null)
            throw new Exception("v062 already installed; do not overwrite its backup");
        if(!AssetDatabase.CopyAsset(path,backup))throw new Exception("Controller backup failed");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        var original=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        var mask=new AvatarMask{name="Sahur previous light attack torso"};
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
        foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,
            AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})
            mask.SetHumanoidBodyPartActive(part,true);
        AssetDatabase.CreateAsset(mask,version+"/LightAttackTorso.mask");
        var sm=new AnimatorStateMachine{name="Light Attack Upper Body"};AssetDatabase.AddObjectToAsset(sm,controller);
        var layer=new AnimatorControllerLayer{name=sm.name,stateMachine=sm,avatarMask=mask,
            blendingMode=AnimatorLayerBlendingMode.Override,defaultWeight=0};
        controller.AddLayer(layer);
        var restored=new System.Collections.Generic.List<object>();
        foreach(string name in new[]{"Combo 1","Combo 2","Combo 3"})
        {
            var source=original.layers[0].stateMachine.states.Single(s=>s.state.name==name).state;
            var baseState=controller.layers[0].stateMachine.states.Single(s=>s.state.name==name).state;
            baseState.motion=source.motion;baseState.speed=source.speed;EditorUtility.SetDirty(baseState);
            var state=sm.AddState(name);state.motion=source.motion;state.speed=source.speed;
            state.writeDefaultValues=false;state.iKOnFeet=false;EditorUtility.SetDirty(state);
            restored.Add(new{name,clip=source.motion.name,speed=source.speed,path=AssetDatabase.GetAssetPath(source.motion)});
        }
        // Charge keeps its existing single right-arm mask and heavy states.
        EditorUtility.SetDirty(sm);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        return new{backup,restored,mobileLayer=sm.name,chargeMask=AssetDatabase.GetAssetPath(controller.layers.Single(l=>l.name=="Charge Upper Body").avatarMask)};
    }
}
