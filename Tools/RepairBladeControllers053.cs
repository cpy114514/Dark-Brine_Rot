using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class RepairBladeControllers053
{
    static readonly string[] Moves={"Air Combo 1","Air Combo 2","Capri Air Combo 1","Capri Air Combo 2"};
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Author controllers outside Play");
        var source=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Encounter/v006/SahurGameplay.controller");
        var master=source.layers[0].stateMachine;
        var rows=new System.Collections.Generic.List<object>();
        foreach(string path in new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"})
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var graph=controller.layers[0].stateMachine;
            foreach(string name in Moves)
            {
                var existing=graph.states.FirstOrDefault(s=>s.state.name==name).state;
                if(existing){rows.Add(new{path,name,added=false});continue;}
                var template=master.states.First(s=>s.state.name==name).state;
                var state=graph.AddState(name,new Vector3(900,100+Array.IndexOf(Moves,name)*100,0));
                state.motion=template.motion;state.speed=template.speed;state.speedParameter=template.speedParameter;state.speedParameterActive=template.speedParameterActive;
                state.timeParameter=template.timeParameter;state.timeParameterActive=template.timeParameterActive;
                state.mirror=template.mirror;state.mirrorParameter=template.mirrorParameter;state.mirrorParameterActive=template.mirrorParameterActive;
                state.cycleOffset=template.cycleOffset;state.iKOnFeet=template.iKOnFeet;state.writeDefaultValues=template.writeDefaultValues;
                foreach(var transition in template.transitions)
                {
                    AnimatorStateTransition copy;
                    if(transition.isExit)copy=state.AddExitTransition();
                    else
                    {
                        var destination=graph.states.FirstOrDefault(s=>s.state.name==transition.destinationState.name).state;
                        if(!destination)throw new Exception("Missing destination: "+transition.destinationState.name);
                        copy=state.AddTransition(destination);
                    }
                    copy.hasExitTime=transition.hasExitTime;copy.exitTime=transition.exitTime;copy.hasFixedDuration=transition.hasFixedDuration;copy.duration=transition.duration;
                    copy.offset=transition.offset;copy.interruptionSource=transition.interruptionSource;copy.orderedInterruption=transition.orderedInterruption;copy.canTransitionToSelf=transition.canTransitionToSelf;
                    foreach(var condition in transition.conditions)copy.AddCondition(condition.mode,condition.threshold,condition.parameter);
                }
                rows.Add(new{path,name,added=true});
            }
            const string airLayer="Air Attack Legs";
            if(!controller.layers.Any(l=>l.name==airLayer))
            {
                var template=source.layers.First(l=>l.name==airLayer);
                controller.AddLayer(airLayer);var layers=controller.layers;var layer=layers[layers.Length-1];
                layer.avatarMask=template.avatarMask;layer.defaultWeight=template.defaultWeight;layer.blendingMode=template.blendingMode;layer.iKPass=template.iKPass;
                foreach(var child in template.stateMachine.states)
                {
                    if(child.state.transitions.Length!=0)throw new Exception("Review new air-layer transitions before copying");
                    var state=layer.stateMachine.AddState(child.state.name,child.position);
                    state.motion=child.state.motion;state.speed=child.state.speed;state.writeDefaultValues=child.state.writeDefaultValues;
                    state.iKOnFeet=child.state.iKOnFeet;
                    if(template.stateMachine.defaultState==child.state)layer.stateMachine.defaultState=state;
                }
                controller.layers=layers;rows.Add(new{path,name=airLayer,added=true});
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented);File.WriteAllText(".codex/blades053/controller-install.json",json);return json;
    }
}
