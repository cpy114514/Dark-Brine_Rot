using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class InstallPerfectDodge043
{
    const string Folder="Assets/Resources/Combat/PerfectDodge";
    static readonly string[] Controllers={"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
    public static object ReviseClubSweep()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stopped Editor required");
        var original=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controllers[0]).animationClips.First(c=>c.name=="ClubSpinOneHandV003");
        var clip=Retime(original,"ClubReturnSweepV003",.6f,.27f);
        foreach(string path in Controllers)
        {
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            c.layers[0].stateMachine.states.First(s=>s.state.name=="Perfect Counter").state.motion=clip;
            EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
        }
        return new{revised=clip.name,clip.length};
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stopped Editor required");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources/Combat","PerfectDodge");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controllers[0]);
        var jog=controller.animationClips.First(c=>c.name.Contains("Jog_Fwd"));
        var idle=controller.animationClips.First(c=>c.name=="Rig|Idle_Loop");
        var sweep=controller.animationClips.First(c=>c.name=="SahurSwordCombo2");
        var blade=controller.animationClips.First(c=>c.name=="BladeSlashV273");
        var step=Step(jog,idle);
        var club=Retime(controller.animationClips.First(c=>c.name=="ClubSpinOneHandV003"),"ClubReturnSweepV003",.6f,.27f);
        var twin=Retime(blade,"BladeReturnSweepV001",.6f);
        foreach(string path in Controllers)
        {
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);var sm=c.layers[0].stateMachine;
            var locomotion=sm.states.First(s=>s.state.name=="Locomotion").state;
            State(sm,"Perfect Step",step,locomotion);
            State(sm,"Perfect Counter",club,locomotion);
            State(sm,"Capri Perfect Counter",twin,locomotion);
            EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
        }
        return new{installed=true,clips=new[]{step,club,twin}.Select(c=>new{c.name,c.length,c.humanMotion}).ToArray(),controllers=Controllers};
    }
    static void State(AnimatorStateMachine sm,string name,AnimationClip clip,AnimatorState exit)
    {
        var state=sm.states.FirstOrDefault(s=>s.state.name==name).state;
        if(state==null)state=sm.AddState(name);
        state.motion=clip;state.speed=1;state.writeDefaultValues=true;
        if(state.transitions.Length==0)
        {
            var transition=state.AddTransition(exit);transition.hasExitTime=true;transition.exitTime=1;
            transition.hasFixedDuration=true;transition.duration=.065f;
        }
    }
    static AnimationClip Step(AnimationClip jog,AnimationClip idle)
    {
        string path=Folder+"/SahurStepV001.anim";
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing)return existing;
        var clip=UnityEngine.Object.Instantiate(jog);clip.name="SahurStepV001";clip.frameRate=60;
        var idleBindings=AnimationUtility.GetCurveBindings(idle);
        foreach(var b in AnimationUtility.GetCurveBindings(clip))
        {
            var source=AnimationUtility.GetEditorCurve(jog,b);
            var rest=idleBindings.Any(x=>x.path==b.path&&x.propertyName==b.propertyName)?AnimationUtility.GetEditorCurve(idle,b):null;
            var keys=new Keyframe[18];
            for(int i=0;i<keys.Length;i++)
            {
                float t=.28f*i/(keys.Length-1),u=t/.28f;
                float weight=Mathf.Sin(Mathf.PI*u);
                float at=Mathf.Lerp(.02f,.43f,Mathf.SmoothStep(0,1,u));
                float idleValue=rest!=null?rest.Evaluate(0):source.Evaluate(0);
                float value=Mathf.Lerp(idleValue,source.Evaluate(at),weight);
                if(b.propertyName.StartsWith("RootQ.")||b.propertyName=="RootT.x"||b.propertyName=="RootT.z")value=idleValue;
                if(b.propertyName=="RootT.y")value=idleValue-.075f*Mathf.Sin(Mathf.PI*u);
                if(b.propertyName=="Spine Front-Back")value+=.16f*weight;
                if(b.propertyName=="Chest Front-Back")value+=.08f*weight;
                keys[i]=new Keyframe(t,value);
            }
            AnimationUtility.SetEditorCurve(clip,b,new AnimationCurve(keys));
        }
        Finish(clip,path);return clip;
    }
    static AnimationClip Retime(AnimationClip source,string name,float duration,float phaseOffset=0)
    {
        string path=Folder+"/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing)return existing;
        var clip=UnityEngine.Object.Instantiate(source);clip.name=name;clip.frameRate=60;
        foreach(var b in AnimationUtility.GetCurveBindings(source))
        {
            var curve=AnimationUtility.GetEditorCurve(source,b);var keys=new Keyframe[37];
            for(int i=0;i<keys.Length;i++)
            {
                float t=duration*i/(keys.Length-1);
                float phase=phaseOffset==0?t/duration:Mathf.Repeat(t/duration+phaseOffset,1);
                float value=curve.Evaluate(phase*source.length);
                if(b.propertyName=="RootT.x"||b.propertyName=="RootT.z"||b.propertyName=="RootT.y")value=curve.Evaluate(0);
                keys[i]=new Keyframe(t,value);
            }
            AnimationUtility.SetEditorCurve(clip,b,new AnimationCurve(keys));
        }
        Finish(clip,path);return clip;
    }
    static void Finish(AnimationClip clip,string path)
    {
        AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlend=false;
        settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;settings.keepOriginalOrientation=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);clip.EnsureQuaternionContinuity();
        AssetDatabase.CreateAsset(clip,path);AssetDatabase.SaveAssetIfDirty(clip);
    }
}
