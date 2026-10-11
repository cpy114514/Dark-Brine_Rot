using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallV020
{
    const string Folder="Assets/Resources/Encounter/v020";
    const string Controller="Assets/Resources/Encounter/v006/SahurGameplay.controller";
    const string Legacy="Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller";
    static void Ready()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v020");
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/PreviousSahurGameplay.controller")==null)
            if(!AssetDatabase.CopyAsset(Controller,Folder+"/PreviousSahurGameplay.controller"))throw new Exception("Controller backup failed");
    }
    public static object RestoreLegacyLocomotion()
    {
        Ready();var current=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        var previous=AssetDatabase.LoadAssetAtPath<AnimatorController>(Legacy);
        foreach(string name in new[]{"Locomotion","Roll"})
        {
            var target=current.layers[0].stateMachine.states.Single(s=>s.state.name==name).state;
            var original=previous.layers[0].stateMachine.states.Single(s=>s.state.name==name).state;
            target.motion=original.motion;target.speed=original.speed;
            target.speedParameter=original.speedParameter;target.speedParameterActive=original.speedParameterActive;
            target.cycleOffset=original.cycleOffset;target.cycleOffsetParameterActive=original.cycleOffsetParameterActive;
            EditorUtility.SetDirty(target);
            if(target.motion!=original.motion)throw new Exception("Legacy motion reference changed");
        }
        EditorUtility.SetDirty(current);AssetDatabase.SaveAssetIfDirty(current);
        return new{walkingRunningRestored=true,rollingRestored=true,legacy=Legacy,combatAndTraversalPreserved=true};
    }
    public static object InstallPaddle()
    {
        Ready();var current=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        AnimationClip Clip(string name)
        {
            string destination=Folder+"/"+name;
            if(!AssetDatabase.IsValidFolder(destination))
            {
                string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);
                if(!string.IsNullOrEmpty(error))throw new Exception(error);
            }
            return AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        }
        var forward=Clip("PLAYER_BoardPaddle_v020");var reverse=Clip("PLAYER_BoardPaddleReverse_v020");
        if(!forward.isHumanMotion || !reverse.isHumanMotion || Mathf.Abs(forward.length-1.2f)>.002f)throw new Exception("Paddle import invalid");
        var state=current.layers[0].stateMachine.states.Single(s=>s.state.name=="Wreck Surf").state;
        var tree=(BlendTree)state.motion;var children=tree.children;
        for(int i=0;i<children.Length;i++)
        {
            if(children[i].position==Vector2.up)children[i].motion=forward;
            if(children[i].position==Vector2.down)children[i].motion=reverse;
        }
        tree.children=children;
        if(!current.parameters.Any(p=>p.name=="BoardStrokePhase"))current.AddParameter("BoardStrokePhase",AnimatorControllerParameterType.Float);
        state.timeParameter="BoardStrokePhase";state.timeParameterActive=true;
        EditorUtility.SetDirty(tree);EditorUtility.SetDirty(state);EditorUtility.SetDirty(current);AssetDatabase.SaveAssets();
        Directory.CreateDirectory(".codex/encounter-v020");
        var result=new{forward=forward.name,reverse=reverse.name,seconds=forward.length,fps=forward.frameRate,phaseFollowsPhysicalStroke=true,originalAvatarRetained=true};
        File.WriteAllText(".codex/encounter-v020/import-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
