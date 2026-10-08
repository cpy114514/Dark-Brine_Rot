using System;
using UnityEditor;
using UnityEngine;
public static class InstallV008
{
    public static object Install()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        const string folder="Assets/Resources/Encounter/v008";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v008");
        if(!AssetDatabase.IsValidFolder(folder+"/CH1_SahurParry_v008"))
        {string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/CH1_SahurParry_v008",folder+"/CH1_SahurParry_v008");if(error!="")throw new Exception(error);}
        const string path="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        if(AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(folder+"/PreviousEncounterDefinition.asset")==null)AssetDatabase.CopyAsset(path,folder+"/PreviousEncounterDefinition.asset");
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(path);
        for(int i=0;i<definition.beats.Length;i++)if(definition.beats[i].action==Story1EncounterAction.Parry)definition.beats[i].clip="CH1_SahurParry_v008";
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();return new{parryInstalled=true};
    }
}
