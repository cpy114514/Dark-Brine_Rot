using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallV007
{
    public static object Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        const string folder="Assets/Resources/Encounter/v007";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v007");
        foreach(var name in new[]{"CH1_SahurGuard_v007","CH1_SahurParry_v007","CH1_SahurBrace_v007","PLAYER_SwimIdle_v007","PLAYER_Swim_v007","PLAYER_SwimFast_v007"})
            if(!AssetDatabase.IsValidFolder(folder+"/"+name))
            {
                string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,folder+"/"+name);
                if(error!="")throw new Exception(error);
            }
        AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(folder+"/"+name+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        const string controllerPath="Assets/Resources/Encounter/v006/SahurGameplay.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/PreviousSahurGameplay.controller")==null)
            AssetDatabase.CopyAsset(controllerPath,folder+"/PreviousSahurGameplay.controller");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        foreach(string parameter in new[]{"SwimAmount","SwimFastBlend"})
            if(!controller.parameters.Any(p=>p.name==parameter))controller.AddParameter(parameter,AnimatorControllerParameterType.Float);
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Swimming")??machine.AddState("Swimming");
        var forward=new BlendTree{name="Normal to alternating freestyle",blendType=BlendTreeType.Simple1D,blendParameter="SwimFastBlend",useAutomaticThresholds=false};
        AssetDatabase.AddObjectToAsset(forward,controller);forward.AddChild(Clip("PLAYER_Swim_v007"),0);forward.AddChild(Clip("PLAYER_SwimFast_v007"),1);
        var tree=new BlendTree{name="Continuous swim speed",blendType=BlendTreeType.Simple1D,blendParameter="SwimAmount",useAutomaticThresholds=false};
        AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(Clip("PLAYER_SwimIdle_v007"),0);tree.AddChild(forward,1);
        state.motion=tree;state.writeDefaultValues=true;
        // Existing externally addressed states remain compatible.
        foreach(var s in machine.states)
        {
            if(s.state.name=="Swim Idle")s.state.motion=Clip("PLAYER_SwimIdle_v007");
            if(s.state.name=="Swim Forward")s.state.motion=Clip("PLAYER_Swim_v007");
            if(s.state.name=="Swim Fast")s.state.motion=Clip("PLAYER_SwimFast_v007");
        }
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>("Assets/Resources/Encounter/v006/EncounterDefinition.asset");
        if(AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(folder+"/PreviousEncounterDefinition.asset")==null)
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(definition),folder+"/PreviousEncounterDefinition.asset");
        for(int i=0;i<definition.beats.Length;i++)
            if(definition.beats[i].clip=="CH1_SahurGuard_v006"||definition.beats[i].clip=="CH1_SahurParry_v006"||definition.beats[i].clip=="CH1_SahurBrace_v006")
                definition.beats[i].clip=definition.beats[i].clip.Replace("v006","v007");
        EditorUtility.SetDirty(definition);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        return new {installed=6,continuousSwim=true,historicalResourcesPreserved=true};
    }
}
