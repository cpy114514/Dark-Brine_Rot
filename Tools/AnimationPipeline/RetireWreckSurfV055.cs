using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class RetireWreckSurfV055
{
    public static object Run()
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string path="Assets/Resources/Encounter/v006/SahurGameplay.controller",folder="Assets/Resources/Encounter/v055";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var sm=controller.layers[0].stateMachine;
        var surf=sm.states.Select(x=>x.state).SingleOrDefault(s=>s.name=="Wreck Surf");
        if(surf==null)return new{alreadyRemoved=true};
        var other=controller.layers.SelectMany(l=>l.stateMachine.states).Select(s=>s.state).Where(s=>s!=surf).ToArray();
        var preserved=other.Select(s=>new{s,s.motion,s.speed}).ToArray();
        string[] retired={"BoardThrottle","BoardSteer","BoardStrokePhase"};
        bool Uses(Motion motion)
        {
            var tree=motion as BlendTree;if(tree==null)return false;
            return retired.Contains(tree.blendParameter) || retired.Contains(tree.blendParameterY) || tree.children.Any(c=>retired.Contains(c.directBlendParameter)||Uses(c.motion));
        }
        if(other.Any(s=>Uses(s.motion) || s.transitions.Any(t=>t.destinationState==surf || t.conditions.Any(c=>retired.Contains(c.parameter))) || (s.timeParameterActive && retired.Contains(s.timeParameter)) || (s.speedParameterActive && retired.Contains(s.speedParameter))) || controller.layers.Any(l=>l.stateMachine.anyStateTransitions.Any(t=>t.destinationState==surf || t.conditions.Any(c=>retired.Contains(c.parameter)))))throw new Exception("Unexpected external reference to retired vehicle state");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v055");
        if(!AssetDatabase.CopyAsset(path,folder+"/PreviousSahurGameplay.controller"))throw new Exception("Versioned backup failed");
        sm.RemoveState(surf);
        foreach(string name in retired){int i=Array.FindIndex(controller.parameters,p=>p.name==name);if(i>=0)controller.RemoveParameter(i);}
        EditorUtility.SetDirty(sm);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        if(preserved.Any(x=>x.s.motion!=x.motion || x.s.speed!=x.speed))throw new Exception("Unrelated walking/combat state changed");
        var result=new{surfStateRemoved=true,paddleParametersRemoved=true,walkingRunningRollingAndCombatRetained=true,historicalAssetsRetained=true,backup=folder+"/PreviousSahurGameplay.controller",remainingStates=sm.states.Length};
        Directory.CreateDirectory(".codex/opening-optimization-20261012");File.WriteAllText(".codex/opening-optimization-20261012/retire-wreck-surf-v055.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
