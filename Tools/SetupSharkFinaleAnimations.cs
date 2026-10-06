using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class SetupSharkFinaleAnimations
{
    public static object Setup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Requires Edit Mode.");
        const string sprayPath="Assets/Resources/Story1Wreck/WreckWaterSpray.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(sprayPath)==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Wreck Water Spray"},sprayPath);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx").OfType<AnimationClip>().ToArray();
        var machine=controller.layers[0].stateMachine;
        void State(string name,AnimationClip clip,float speed)
        {
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);
            state.motion=clip;state.speed=speed;state.writeDefaultValues=true;EditorUtility.SetDirty(state);
        }
        State("Finale Guard",clips.Single(c=>c.name=="Armature|Sword_Block"),1);
        State("Finale Stagger",clips.Single(c=>c.name=="Armature|Hit_Knockback"),1);
        var getUp=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurShoreGetUp.anim");
        string path="Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurWreckKnockdown.anim";
        var reversed=UnityEngine.Object.Instantiate(getUp);reversed.name="SahurWreckKnockdown";
        foreach(var binding in AnimationUtility.GetCurveBindings(getUp))
        {
            var curve=AnimationUtility.GetEditorCurve(getUp,binding);
            var keys=curve.keys.Reverse().Select(k=>new Keyframe(getUp.length-k.time,k.value,-k.outTangent,-k.inTangent,k.outWeight,k.inWeight){weightedMode=k.weightedMode}).ToArray();
            AnimationUtility.SetEditorCurve(reversed,binding,new AnimationCurve(keys));
        }
        var settings=AnimationUtility.GetAnimationClipSettings(reversed);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(reversed,settings);
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing==null)AssetDatabase.CreateAsset(reversed,path);
        else{EditorUtility.CopySerialized(reversed,existing);UnityEngine.Object.DestroyImmediate(reversed);reversed=existing;AssetDatabase.SaveAssetIfDirty(existing);}
        State("Finale Knockdown",reversed,reversed.length/2.1f);
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        return new{guard=true,stagger=true,authoredKnockdownClip=path,knockdownSeconds=reversed.length};
    }
}
