using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildCapriMotionPolish
{
    const string Folder="Assets/Resources/CapriPhase2/Polish";
    struct Beat
    {
        public float start,end,from,to;public bool mirror;public AnimationClip clip;
        public Beat(float s,float e,AnimationClip c,float f=0,float t=1,bool m=false){start=s;end=e;clip=c;from=f;to=t;mirror=m;}
    }
    public static object Run()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var sources=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/ThirdParty/CapriPolishKayKit","Assets/ThirdParty/BossPhaseAssets"})
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>().ToArray();
        Func<string,AnimationClip> find=n=>sources.First(c=>c.name==n||c.name=="Armature|"+n);
        var slice=find("Melee_Dualwield_Attack_Slice");var chop=find("Melee_Dualwield_Attack_Chop");var block=find("Melee_Blocking");
        var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/Animations/CapriCombat.controller");
        var report=new List<object>();
        Action<string,float,Beat[]> build=(name,length,beats)=>
        {
            var clip=Bake(name+"Smooth",length,beats,slice);string path=Folder+"/"+name+"Smooth.anim";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}else AssetDatabase.CreateAsset(clip,path);
            ctrl.layers[0].stateMachine.states.First(s=>s.state.name==name).state.motion=clip;
            report.Add(new{name,length,clip.humanMotion,path,source=beats.Select(b=>b.clip.name).Distinct().ToArray()});
        };
        build("CapriP2Idle",2.4f,new[]{new Beat(0,2.4f,find("Idle_B"))});
        build("CapriP2Transition",4.2f,new[]{new Beat(0,.75f,find("Hit_B"),0,.85f),new Beat(.75f,3.55f,find("Ranged_Magic_Summon"),.04f,.94f),new Beat(3.55f,4.2f,slice,0,0)});
        build("CapriP2Shadow",3.35f,new[]{new Beat(0,.7f,slice,0,.12f),new Beat(.7f,1.36f,slice,.12f,.88f),
            new Beat(1.36f,2.06f,slice,.12f,.88f,true),new Beat(2.06f,2.78f,slice,.12f,.88f),new Beat(2.78f,3.35f,slice,.88f,1)});
        build("CapriP2Cyclone",3.25f,new[]{new Beat(0,.85f,find("Melee_2H_Attack_Spin"),0,.27f),
            new Beat(.85f,2.25f,find("Melee_2H_Attack_Spin"),.27f,.8f),new Beat(2.25f,3.25f,find("Melee_2H_Attack_Spin"),.8f,1)});
        build("CapriP2Dive",3.45f,new[]{new Beat(0,.9f,find("NinjaJump_Start")),new Beat(.9f,1.5f,find("NinjaJump_Idle_Loop"),0,.3f),
            new Beat(1.5f,2.12f,chop,.12f,.75f),new Beat(2.12f,2.85f,find("NinjaJump_Land"),.08f,1),new Beat(2.85f,3.45f,slice,0,0)});
        build("CapriP2Tempest",5.15f,new[]{new Beat(0,.85f,slice,0,.1f),new Beat(.85f,1.63f,slice,.1f,.92f),
            new Beat(1.63f,2.39f,slice,.1f,.92f,true),new Beat(2.39f,3.05f,find("Melee_Dualwield_Attack_Stab"),.12f,.85f),
            new Beat(3.05f,3.65f,chop,0,.19f),new Beat(3.65f,4.3f,chop,.19f,.85f),new Beat(4.3f,5.15f,chop,.85f,1)});
        EditorUtility.SetDirty(ctrl);AssetDatabase.SaveAssets();
        File.WriteAllText("Documentation/CappuccinoPolishReview/motions.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
    static AnimationClip Bake(string name,float length,Beat[] beats,AnimationClip ready)
    {
        var all=beats.Select(b=>b.clip).Append(ready).Distinct().ToArray();
        var maps=all.ToDictionary(c=>c,c=>AnimationUtility.GetCurveBindings(c).Where(b=>b.type==typeof(Animator)).ToDictionary(b=>b.propertyName,b=>AnimationUtility.GetEditorCurve(c,b)));
        var properties=maps.Values.SelectMany(m=>m.Keys).Distinct().ToArray();
        var clip=UnityEngine.Object.Instantiate(beats[0].clip);clip.name=name;clip.frameRate=60;
        foreach(var binding in AnimationUtility.GetCurveBindings(clip))AnimationUtility.SetEditorCurve(clip,binding,null);
        AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
        foreach(string property in properties)
        {
            var keys=new List<Keyframe>();
            for(int i=0;i<=Mathf.RoundToInt(length*60);i++)
            {
                float time=i/60f;int index=0;while(index<beats.Length-1&&time>=beats[index].end)index++;
                var b=beats[index];float rate=(b.to-b.from)*b.clip.length/(b.end-b.start);
                float sample=Mathf.Lerp(b.from,b.to,Mathf.InverseLerp(b.start,b.end,time))*b.clip.length;
                float value=Value(maps[b.clip],property,sample,b.mirror);
                // Preserve source velocities through a 0.18s Hermite inertial correction.
                // Unlike resetting a pose, the correction is C1 at both boundaries.
                if(index>0&&time<b.start+.18f)
                {
                    var prev=beats[index-1];float oldTime=prev.to*prev.clip.length;
                    float oldRate=(prev.to-prev.from)*prev.clip.length/(prev.end-prev.start);
                    float a=Value(maps[prev.clip],property,oldTime,prev.mirror),vOld=Derivative(maps[prev.clip],property,oldTime,prev.mirror)*oldRate;
                    float newTime=b.from*b.clip.length,c=Value(maps[b.clip],property,newTime,b.mirror),vNew=Derivative(maps[b.clip],property,newTime,b.mirror)*rate;
                    float u=(time-b.start)/.18f;
                    float h0=2*u*u*u-3*u*u+1,h1=u*u*u-2*u*u+u;
                    value+=(a-c)*h0+(vOld-vNew)*.18f*h1;
                }
                // Shared entry/exit guard prevents the outgoing idle from snapping the wrists.
                bool arm=property.Contains("Arm ")||property.Contains("Forearm ")||property.Contains("Shoulder ")||property.Contains(" Hand ");
                float readyValue=Value(maps[ready],property,0,false);
                if(name.Contains("Idle")&&arm)value=Mathf.Lerp(readyValue,value,.08f);
                if(arm)
                {
                    float enter=Smooth(Mathf.Clamp01(time/.18f)),exit=Smooth(Mathf.Clamp01((length-time)/.24f));
                    value=Mathf.Lerp(readyValue,value,Mathf.Min(enter,exit));
                }
                if(property.StartsWith("RootT."))value=property=="RootT.y"?.96f:0;
                if(property.StartsWith("RootQ."))value=property=="RootQ.w"?1:0;
                if(HumanTrait.MuscleName.Contains(property))value=Mathf.Clamp(value,-.95f,.95f);
                keys.Add(new Keyframe(time,value));
            }
            bool constant=keys.All(k=>Mathf.Abs(k.value-keys[0].value)<.00005f);
            var curve=new AnimationCurve(constant?new[]{keys[0],keys[keys.Count-1]}:keys.ToArray());
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),property),curve);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=length;settings.loopTime=name.Contains("Idle");settings.loopBlendPositionXZ=true;settings.loopBlendPositionY=true;settings.loopBlendOrientation=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        clip.EnsureQuaternionContinuity();return clip;
    }
    static float Smooth(float u)=>u*u*u*(u*(u*6-15)+10);
    static float Value(Dictionary<string,AnimationCurve> map,string property,float time,bool mirrored)
    {
        string p=mirrored?property.Replace("Left","TEMP").Replace("Right","Left").Replace("TEMP","Right"):property;
        float value=map.TryGetValue(p,out var curve)?curve.Evaluate(time):property.EndsWith("Q.w")?1:0;
        if(mirrored&&(p.Contains("Twist Left-Right")||p.Contains("Tilt Left-Right")||p.Contains("Turn Left-Right")||p.Contains("Left-Right")||p.EndsWith("T.x")||p.EndsWith("Q.y")||p.EndsWith("Q.z")))value=-value;
        return value;
    }
    static float Derivative(Dictionary<string,AnimationCurve> map,string p,float t,bool mirror)=>(Value(map,p,t+.002f,mirror)-Value(map,p,t-.002f,mirror))/.004f;
}
