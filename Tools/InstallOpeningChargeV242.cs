using System;
using UnityEditor;
using UnityEngine;
public static class InstallOpeningChargeV242
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required.");
        const string source="Assets/Game/Prefabs/Characters/Sahur/Animations/SahurChargeUpperBody.mask";
        const string folder="Assets/Resources/Encounter/v242";
        const string target=folder+"/SahurGameplayChargeRightArm.mask";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v242");
        if(!AssetDatabase.LoadAssetAtPath<AvatarMask>(target)&&!AssetDatabase.CopyAsset(source,target))throw new Exception("Mask copy failed");
        var a=AssetDatabase.LoadAssetAtPath<AvatarMask>(source);var b=AssetDatabase.LoadAssetAtPath<AvatarMask>(target);
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)if(a.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i)!=b.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i))throw new Exception("Humanoid mask differs");
        if(a.transformCount!=b.transformCount)throw new Exception("Transform mask differs");
        for(int i=0;i<a.transformCount;i++)if(a.GetTransformPath(i)!=b.GetTransformPath(i)||a.GetTransformActive(i)!=b.GetTransformActive(i))throw new Exception("Transform mask differs");
        return new{source,target,identicalMask=true};
    }
}
