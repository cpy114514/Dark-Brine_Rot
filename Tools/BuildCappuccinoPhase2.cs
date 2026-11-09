using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// All new motions are baked AnimationClips on the existing humanoid motion source.
// Originals, avatar, fitted bones, first-phase states and death are preserved.
public static class BuildCappuccinoPhase2
{
    const string Folder="Assets/Resources/CapriPhase2";
    const string Source="Assets/ThirdParty/BossPhaseAssets/UAL2_Standard.fbx";
    struct Beat
    {
        public float at,end,from,to; public AnimationClip clip;
        public Beat(float a,float b,AnimationClip c,float f=0,float t=1){at=a;end=b;clip=c;from=f;to=t;}
    }
    public static object Run()
    {
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var library=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().ToArray();
        Func<string,AnimationClip> c=n=>library.First(a=>a.name=="Armature|"+n);
        var block=c("Sword_Block");var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx").OfType<AnimationClip>().First(a=>a.name=="Rig|Sword_Idle");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Bosses/CappuccinoAssassino/Animations/CapriCombat.controller");
        var sm=controller.layers[0].stateMachine;
        if(!controller.parameters.Any(p=>p.name=="Ristretto"))controller.AddParameter("Ristretto",AnimatorControllerParameterType.Bool);
        var built=new List<object>();
        Action<string,float,Beat[]> bake=(name,duration,beats)=>
        {
            var clip=Bake(name,duration,beats);
            string path=Folder+"/"+name+".anim";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}
            else AssetDatabase.CreateAsset(clip,path);
            var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);
            state.motion=clip;state.writeDefaultValues=true;
            built.Add(new{name,duration,clip.humanMotion,curves=AnimationUtility.GetCurveBindings(clip).Length});
        };
        bake("CapriP2Transition",4.2f,new[]{
            new Beat(0,.8f,c("Hit_Knockback"),0,.7f),new Beat(.8f,1.5f,c("Idle_Shield_Break"),.3f,.78f),
            new Beat(1.5f,2.7f,block,0,.52f),new Beat(2.7f,3.6f,c("Sword_Regular_C"),.1f,.65f),new Beat(3.6f,4.2f,idle,0,.35f)});
        bake("CapriP2Shadow",3.35f,new[]{new Beat(0,.7f,block,0,.4f),
            new Beat(.7f,1.32f,c("Sword_Dash_RM"),.18f,.8f),new Beat(1.32f,2.02f,c("Sword_Regular_B")),
            new Beat(2.02f,2.72f,c("Sword_Regular_A")),new Beat(2.72f,3.35f,c("Sword_Regular_A_Rec"))});
        bake("CapriP2Cyclone",3.25f,new[]{new Beat(0,.85f,block,0,.52f),
            new Beat(.85f,1.55f,c("Sword_Regular_C"),.18f,.65f),new Beat(1.55f,2.25f,c("Sword_Regular_C"),.18f,.65f),
            new Beat(2.25f,3.25f,c("Sword_Regular_B_Rec"))});
        bake("CapriP2Dive",3.45f,new[]{new Beat(0,.9f,c("NinjaJump_Start")),new Beat(.9f,1.5f,c("NinjaJump_Idle_Loop"),0,.3f),
            new Beat(1.5f,1.95f,c("Sword_Regular_C"),.1f,.6f),new Beat(1.95f,2.8f,c("NinjaJump_Land")),new Beat(2.8f,3.45f,idle,0,.4f)});
        bake("CapriP2Tempest",5.15f,new[]{new Beat(0,.85f,block,0,.5f),new Beat(.85f,3.05f,c("Sword_Regular_Combo")),
            new Beat(3.05f,3.65f,block,.3f,.65f),new Beat(3.65f,4.25f,c("Sword_Regular_C"),.05f,.72f),new Beat(4.25f,5.15f,c("Sword_Regular_B_Rec"))});
        bake("CapriP2Idle",idle.length,new[]{new Beat(0,idle.length,idle)});
        var ready=sm.states.First(s=>s.state.name=="CapriP2Idle").state;
        var originalIdle=sm.states.First(s=>s.state.name=="Idle").state;
        if(!originalIdle.transitions.Any(t=>t.destinationState==ready))
        {
            var enter=originalIdle.AddTransition(ready);enter.duration=.18f;enter.AddCondition(AnimatorConditionMode.If,0,"Ristretto");
            var exit=ready.AddTransition(originalIdle);exit.duration=.18f;exit.AddCondition(AnimatorConditionMode.IfNot,0,"Ristretto");
            var walk=ready.AddTransition(sm.states.First(s=>s.state.name=="Walk").state);walk.duration=.16f;walk.AddCondition(AnimatorConditionMode.Greater,.05f,"Move");
            var retreat=ready.AddTransition(sm.states.First(s=>s.state.name=="Retreat").state);retreat.duration=.16f;retreat.AddCondition(AnimatorConditionMode.Less,-.05f,"Move");
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        var shader=Shader.Find("DarkBrine/Crema Mesh");
        if(!shader)throw new Exception("Crema shader is not imported");
        if(!AssetDatabase.LoadAssetAtPath<Material>(Folder+"/CremaMesh.mat"))AssetDatabase.CreateAsset(new Material(shader){name="CremaMesh"},Folder+"/CremaMesh.mat");
        AssetDatabase.SaveAssets();
        File.WriteAllText("Documentation/CappuccinoPhase2-assets.json",Newtonsoft.Json.JsonConvert.SerializeObject(built,Newtonsoft.Json.Formatting.Indented));
        return built;
    }
    static AnimationClip Bake(string name,float duration,Beat[] beats)
    {
        var sources=beats.Select(b=>b.clip).Distinct().ToArray();
        var curves=sources.ToDictionary(c=>c,c=>AnimationUtility.GetCurveBindings(c).Where(b=>b.type==typeof(Animator)).ToDictionary(b=>b.propertyName,b=>AnimationUtility.GetEditorCurve(c,b)));
        var properties=curves.Values.SelectMany(c=>c.Keys).Distinct().ToArray();
        var result=UnityEngine.Object.Instantiate(beats[0].clip);result.name=name;result.frameRate=60;
        foreach(var binding in AnimationUtility.GetCurveBindings(result))AnimationUtility.SetEditorCurve(result,binding,null);
        AnimationUtility.SetAnimationEvents(result,Array.Empty<AnimationEvent>());
        foreach(string property in properties)
        {
            var keys=new List<Keyframe>();
            for(int frame=0;frame<=Mathf.RoundToInt(duration*60);frame++)
            {
                float time=frame/60f;int index=0;while(index<beats.Length-1&&time>=beats[index].end)index++;
                Beat beat=beats[index];float phase=Mathf.InverseLerp(beat.at,beat.end,time);
                float value=Value(curves[beat.clip],property,Mathf.Lerp(beat.from,beat.to,phase)*beat.clip.length);
                // Blend complete full-body poses, including entry to every cut; no frame resets.
                if(index>0&&time<beat.at+.14f)
                {
                    Beat previous=beats[index-1];float from=Value(curves[previous.clip],property,previous.to*previous.clip.length);
                    value=Mathf.Lerp(from,value,Mathf.SmoothStep(0,1,(time-beat.at)/.14f));
                }
                if(property.StartsWith("RootT.")) value=property=="RootT.y"?Mathf.Clamp(value,.82f,1.05f):0;
                if(property.StartsWith("RootQ.")) value=property=="RootQ.w"?1:0;
                // Short arms carry long katanas: keep the unsupported shield hand engaged.
                if(name!="CapriP2Idle" && property.StartsWith("Left Arm") && !curves[beat.clip].ContainsKey(property))value*=.7f;
                keys.Add(new Keyframe(time,value));
            }
            var curve=new AnimationCurve(keys.ToArray());
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
            curve=Reduce(curve,duration);
            AnimationUtility.SetEditorCurve(result,EditorCurveBinding.FloatCurve("",typeof(Animator),property),curve);
        }
        AuthorDualBladeSilhouette(result,name,duration);
        var settings=AnimationUtility.GetAnimationClipSettings(result);settings.loopTime=name=="CapriP2Idle";
        settings.startTime=0;settings.stopTime=duration;settings.keepOriginalPositionXZ=true;settings.keepOriginalOrientation=true;
        settings.loopBlendPositionXZ=true;settings.loopBlendOrientation=true;settings.loopBlendPositionY=true;
        AnimationUtility.SetAnimationClipSettings(result,settings);result.EnsureQuaternionContinuity();return result;
    }
    static float Value(Dictionary<string,AnimationCurve> curves,string property,float time)=>curves.TryGetValue(property,out var curve)?curve.Evaluate(time):property=="RootQ.w"?1:0;

    struct Pose
    {
        public float time,twist;public Vector4 left,right;
        public Pose(float t,Vector4 l,Vector4 r,float w=0){time=t;left=l;right=r;twist=w;}
    }
    // Authored muscle keys for the cup's short arms and full-length twin katanas.
    // Imported motion supplies hips, legs, shoulders and secondary body motion.
    static void AuthorDualBladeSilhouette(AnimationClip clip,string name,float duration)
    {
        Vector4 guard=new Vector4(-.25f,.35f,-.25f,.08f), high=new Vector4(.65f,.4f,-.2f,-.1f),
            cross=new Vector4(.05f,.85f,-.65f,.2f), wide=new Vector4(.08f,-.3f,.45f,0),
            cut=new Vector4(-.55f,.65f,.4f,.25f), trail=new Vector4(-.15f,-.55f,.25f,-.1f);
        Pose[] poses;
        if(name=="CapriP2Transition")poses=new[]{new Pose(0,trail,guard),new Pose(.7f,cut,cut),new Pose(1.4f,cross,cross),
            new Pose(2.6f,high,high),new Pose(3.12f,wide,wide),new Pose(3.55f,wide,trail),new Pose(4.2f,guard,high)};
        else if(name=="CapriP2Shadow")poses=new[]{new Pose(0,guard,high),new Pose(.75f,trail,high,-.25f),new Pose(1.1f,high,cut,.3f),
            new Pose(1.45f,high,trail,.25f),new Pose(1.82f,cut,high,-.3f),new Pose(2.15f,trail,high,-.25f),new Pose(2.52f,high,cut,.3f),new Pose(3.35f,guard,high)};
        else if(name=="CapriP2Cyclone")poses=new[]{new Pose(0,cross,cross),new Pose(.65f,cross,high,-.3f),new Pose(1.03f,wide,wide),
            new Pose(2.23f,wide,wide),new Pose(2.65f,cut,trail,.3f),new Pose(3.25f,guard,high)};
        else if(name=="CapriP2Dive")poses=new[]{new Pose(0,guard,high),new Pose(.6f,trail,trail),new Pose(1.04f,high,high),
            new Pose(1.5f,high,high,-.1f),new Pose(2.01f,cut,cut,.2f),new Pose(2.5f,cut,cut),new Pose(3.45f,guard,high)};
        else if(name=="CapriP2Tempest")poses=new[]{new Pose(0,cross,cross),new Pose(.94f,trail,high,-.3f),new Pose(1.3f,high,cut,.3f),
            new Pose(1.7f,high,trail,.3f),new Pose(2.08f,cut,high,-.3f),new Pose(2.45f,wide,wide),new Pose(2.85f,cross,cut,.25f),
            new Pose(3.5f,high,high,-.2f),new Pose(4.1f,cut,cut,.35f),new Pose(4.6f,cut,trail,.15f),new Pose(5.15f,guard,high)};
        else poses=new[]{new Pose(0,guard,high),new Pose(duration*.5f,guard+new Vector4(.02f,0,0,0),high+new Vector4(-.03f,0,0,0)),new Pose(duration,guard,high)};
        string[] suffix={"Arm Down-Up","Arm Front-Back","Forearm Stretch","Hand Down-Up"};
        for(int side=0;side<2;side++)for(int channel=0;channel<suffix.Length;channel++)
        {
            var keys=poses.Select(p=>new Keyframe(p.time,(side==0?p.left:p.right)[channel])).ToArray();
            SetMuscle(clip,(side==0?"Left ":"Right ")+suffix[channel],keys);
        }
        SetMuscle(clip,"Chest Twist Left-Right",poses.Select(p=>new Keyframe(p.time,p.twist)).ToArray());
    }
    static void SetMuscle(AnimationClip clip,string muscle,Keyframe[] keys)
    {
        var curve=new AnimationCurve(keys);
        for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),muscle),curve);
    }
    static AnimationCurve Reduce(AnimationCurve original,float duration)
    {
        var source=original.keys;
        if(source.Length<3)return original;
        var keep=new bool[source.Length];keep[0]=keep[source.Length-1]=true;
        var pending=new Stack<Vector2Int>();pending.Push(new Vector2Int(0,source.Length-1));
        while(pending.Count>0)
        {
            var span=pending.Pop();float worst=.0006f;int split=-1;
            for(int i=span.x+1;i<span.y;i++)
            {
                float value=Mathf.Lerp(source[span.x].value,source[span.y].value,Mathf.InverseLerp(source[span.x].time,source[span.y].time,source[i].time));
                float error=Mathf.Abs(value-source[i].value);if(error>worst){worst=error;split=i;}
            }
            if(split>=0){keep[split]=true;pending.Push(new Vector2Int(span.x,split));pending.Push(new Vector2Int(split,span.y));}
        }
        var result=new AnimationCurve(source.Where((k,i)=>keep[i]).ToArray());
        for(int i=0;i<result.length;i++){AnimationUtility.SetKeyLeftTangentMode(result,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(result,i,AnimationUtility.TangentMode.Linear);}
        // Validate at twice the bake rate, including between the source samples.
        for(int frame=0;frame<=Mathf.RoundToInt(duration*120);frame++)
            if(Mathf.Abs(original.Evaluate(frame/120f)-result.Evaluate(frame/120f))>.0015f)return original;
        return result;
    }
}
