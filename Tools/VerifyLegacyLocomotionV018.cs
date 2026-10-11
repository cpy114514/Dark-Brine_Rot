using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class VerifyLegacyLocomotionV018
{
    public static object Verify()
    {
        var original=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        var current=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Encounter/v006/SahurGameplay.controller");
        var states=current.layers[0].stateMachine.states;
        var rows=new[]{"Locomotion","Roll"}.Select(name=>
        {
            var old=original.layers[0].stateMachine.states.Single(s=>s.state.name==name).state;
            var active=states.Single(s=>s.state.name==name).state;
            if(active.motion!=old.motion || active.speed!=old.speed || active.speedParameter!=old.speedParameter || active.speedParameterActive!=old.speedParameterActive)
                throw new Exception("Original animation or playback changed: "+name);
            return new{name,originalMotion=active.motion.name,path=AssetDatabase.GetAssetPath(active.motion),originalReference=true,speed=active.speed,speedParameter=active.speedParameter};
        }).ToArray();
        var hero=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        if(Application.isPlaying && hero!=null && hero.CharacterAnimator.runtimeAnimatorController!=current)
            throw new Exception("Live player is not using the verified controller");
        var result=new{originalWalkingRunningRolling=true,states=rows,liveController=hero?.CharacterAnimator.runtimeAnimatorController?.name,
            newCombatAndTraversalRetained=states.Any(s=>s.state.name=="Wreck Surf")&&states.Any(s=>s.state.name=="Wreck Climb")};
        Directory.CreateDirectory(".codex/encounter-v018");
        File.WriteAllText(".codex/encounter-v018/legacy-locomotion-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
