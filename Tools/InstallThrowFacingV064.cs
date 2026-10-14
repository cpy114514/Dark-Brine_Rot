using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InstallThrowFacingV064
{
    const string Version="Assets/Resources/Encounter/v064";
    const string ClipName="PLAYER_Throw_v064";
    public static object Import()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        if(!AssetDatabase.IsValidFolder(Version))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v064");
        string folder=Version+"/"+ClipName;
        if(AssetDatabase.IsValidFolder(folder))throw new Exception("v064 already exists; inspect it instead of overwriting");
        AssetDatabase.CreateFolder(Version,ClipName);
        string destination=folder+"/"+ClipName+".fbx";
        if(!AssetDatabase.CopyAsset("Assets/Resources/Encounter/v006/PLAYER_Throw_v006/PLAYER_Throw_v006.fbx",destination))
            throw new Exception("Throw source copy failed");
        var importer=(ModelImporter)AssetImporter.GetAtPath(destination);
        var clips=importer.clipAnimations;
        if(clips.Length!=1)throw new Exception("Unexpected throw take count");
        clips[0].name=ClipName;clips[0].rotationOffset+=180;importer.clipAnimations=clips;
        importer.SaveAndReimport();
        var clip=AssetDatabase.LoadAllAssetsAtPath(destination).OfType<AnimationClip>().Single(c=>c.name==ClipName);
        return new{destination,clip.length,clip.frameRate,clips[0].rotationOffset,originalPreserved=true};
    }
    public static object Activate()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        if(Story1EncounterClipCache.Find(ClipName)==null)throw new Exception("Import and native facing verification required first");
        const string definitionPath="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        string backup=Version+"/PreviousEncounterDefinition.asset";
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(backup)!=null)throw new Exception("v064 definition backup already exists");
        if(!AssetDatabase.CopyAsset(definitionPath,backup))throw new Exception("Definition backup failed");
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(definitionPath);
        var serialized=new SerializedObject(definition);var beats=serialized.FindProperty("beats");int replaced=0;
        for(int i=0;i<beats.arraySize;i++)
        {
            var value=beats.GetArrayElementAtIndex(i).FindPropertyRelative("clip");
            if(value.stringValue=="PLAYER_Throw_v006"){value.stringValue=ClipName;replaced++;}
        }
        if(replaced!=1)throw new Exception("Expected one throw beat; definition not changed");
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        return new{backup,replaced,clip=ClipName};
    }
}
