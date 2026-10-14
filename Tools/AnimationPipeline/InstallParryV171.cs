using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InstallParryV171
{
    public static object Start()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready original Edit Mode required");
        const string parent="Assets/Resources/Encounter/v171";
        const string source="Assets/AnimationStaging/CH1_SahurParry_v171";
        const string path="Assets/Resources/Encounter/v006/EncounterDefinition.asset";
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(path);
        if(definition==null || definition.duration!=20.8f || definition.fps!=60)throw new Exception("Inspect actual approved definition before installation");
        var indices=definition.beats.Select((b,i)=>(b,i)).Where(x=>x.b.action==Story1EncounterAction.Parry).Select(x=>x.i).ToArray();
        if(indices.Length!=1 || definition.beats[indices[0]].clip!="CH1_SahurParry_v090")throw new Exception("Unexpected existing parry; protect current changes");
        var clip=AssetDatabase.LoadAllAssetsAtPath(source+"/CH1_SahurParry_v171.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        if(Mathf.Abs(clip.length-1.1f)>.001f || clip.frameRate!=60 || clip.isLooping)throw new Exception("Native clip timing mismatch");
        if(AssetDatabase.IsValidFolder(parent))throw new Exception("Never overwrite a versioned resource or definition backup");
        AssetDatabase.CreateFolder("Assets/Resources/Encounter","v171");
        if(!AssetDatabase.CopyAsset(path,parent+"/PreviousEncounterDefinition.asset"))throw new Exception("Versioned definition backup failed");
        if(!AssetDatabase.CopyAsset(source,parent+"/CH1_SahurParry_v171"))throw new Exception("Versioned native clip copy failed");
        var previous=definition.beats[indices[0]];var replacement=previous;replacement.clip="CH1_SahurParry_v171";definition.beats[indices[0]]=replacement;
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        var result=new{installedForNativeIntegrationTrial=true,finalApproval=false,oldClip=previous.clip,newClip=replacement.clip,
            originalAvatarRetained=true,contactSeconds=.484f,sourceFPS=60,duration=1.1f,
            definitionBackup=parent+"/PreviousEncounterDefinition.asset",ordinaryAttackAndMovementUnchanged=true,
            rootMotionLocksChanged=false,sceneSaved=false};
        File.WriteAllText(".codex/opening-optimization-20261012/install-parry-v171.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
