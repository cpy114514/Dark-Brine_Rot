using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallNativeChargedSlash
{
    const string Folder="Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/DownloadedHeavy/";
    const string Source=Folder+"UAL2_HeavySource.fbx";
    const string Output=Folder+"SahurNativeDiagonalSlash.anim";
    public const float Lead=.08f,StrikeSpeed=.35f,RecoverySpeed=2.4f,RecoveryBlend=.04f;
    static bool IsRight(string n)=>n.StartsWith("Right Shoulder")||n.StartsWith("Right Arm")||n.StartsWith("Right Forearm")||n.StartsWith("Right Hand")||new[]{"Right Thumb","Right Index","Right Middle","Right Ring","Right Little"}.Any(n.StartsWith);
    public static object Import(){
        if(Application.isPlaying)throw new Exception("Import outside Play Mode.");
        var importer=(ModelImporter)AssetImporter.GetAtPath(Source);
        var clips=importer.clipAnimations.Where(c=>c.name!="SahurNativeDiagonalStrike"&&c.name!="SahurNativeDiagonalRecovery").ToList();
        foreach(string take in new[]{"Armature|Sword_Regular_B","Armature|Sword_Regular_B_Rec"}){
            var c=importer.defaultClipAnimations.Single(x=>x.takeName==take);bool recovery=take.EndsWith("_Rec");
            c.name=recovery?"SahurNativeDiagonalRecovery":"SahurNativeDiagonalStrike";
            if(!recovery){c.firstFrame=7.5f;c.lastFrame=10f;} // The raised pose and damaging stroke; omit the held tail.
            c.loopTime=false;c.loopPose=false;c.lockRootRotation=c.lockRootHeightY=c.lockRootPositionXZ=true;
            clips.Add(c);
        }
        importer.clipAnimations=clips.ToArray();importer.SaveAndReimport();return Build();
    }
    public static object Build(){
        var all=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().ToArray();
        var strike=all.Single(c=>c.name=="SahurNativeDiagonalStrike");var recovery=all.Single(c=>c.name=="SahurNativeDiagonalRecovery");
        float recoveryAt=Lead+strike.length/StrikeSpeed+RecoveryBlend,duration=recoveryAt+recovery.length/RecoverySpeed;
        var output=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output);
        if(!output){output=new AnimationClip();AssetDatabase.CreateAsset(output,Output);}
        output.ClearCurves();output.name="SahurNativeDiagonalSlash";output.frameRate=60;
        float maxNativeDifference=0;
        foreach(var binding in AnimationUtility.GetCurveBindings(strike).Where(b=>b.type==typeof(Animator)&&IsRight(b.propertyName))){
            var first=AnimationUtility.GetEditorCurve(strike,binding);var second=AnimationUtility.GetEditorCurve(recovery,binding);
            var curve=new AnimationCurve();curve.AddKey(new Keyframe(0,first.Evaluate(0),0,0));
            foreach(var k in first.keys){var key=k;key.time=Lead+k.time/StrikeSpeed;key.inTangent=k.time<.00001f?0:k.inTangent*StrikeSpeed;key.outTangent=k.outTangent*StrikeSpeed;curve.AddKey(key);}
            // The imported slices share the same authored recovery pose. A small
            // retargeting seam can be blended without changing the damaging arc.
            if(second!=null){
                curve.AddKey(new Keyframe(recoveryAt,second.Evaluate(0),0,second.keys[0].outTangent*RecoverySpeed));
                foreach(var k in second.keys){if(k.time<.00001f)continue;var key=k;key.time=recoveryAt+k.time/RecoverySpeed;key.inTangent=k.inTangent*RecoverySpeed;key.outTangent=k.outTangent*RecoverySpeed;curve.AddKey(key);}
            }
            if(!binding.propertyName.Contains("Stretched"))for(float t=.001f;t<strike.length-.001f;t+=.003f)
                maxNativeDifference=Mathf.Max(maxNativeDifference,Mathf.Abs(first.Evaluate(t)-curve.Evaluate(Lead+t/StrikeSpeed)));
            else{
                float grip=binding.propertyName.StartsWith("Right Thumb")?-.25f:binding.propertyName.Contains(" 2 ")?-.65f:-.45f;
                curve=AnimationCurve.Linear(0,grip,duration,grip);
            }
            AnimationUtility.SetEditorCurve(output,binding,curve);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(output);settings.loopTime=settings.loopBlend=false;settings.startTime=0;settings.stopTime=duration;
        AnimationUtility.SetAnimationClipSettings(output,settings);AnimationUtility.SetAnimationEvents(output,Array.Empty<AnimationEvent>());
        EditorUtility.SetDirty(output);AssetDatabase.SaveAssetIfDirty(output);
        if(maxNativeDifference>.002f)throw new Exception("Native strike curves changed: "+maxNativeDifference);
        return new{strike=strike.name,recovery=recovery.name,strike.length,duration,maxNativeDifference,output=Output};
    }
    public static object Install(){
        if(Application.isPlaying)throw new Exception("Install outside Play Mode.");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        foreach(var layer in controller.layers){var state=layer.stateMachine.states.Single(s=>s.state.name=="Heavy Attack").state;
            Undo.RecordObject(state,"Use native charged strike");state.motion=clip;state.speed=1;state.timeParameterActive=false;EditorUtility.SetDirty(state);}
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        const string prefab="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
        var root=PrefabUtility.LoadPrefabContents(prefab);
        try{Configure(root.GetComponent<Mavis.SahurAttack>(),clip);PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
        const string demo="Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoCombatDemo.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(demo);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(demo,OpenSceneMode.Additive);
        try{foreach(var a in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Mavis.SahurAttack>(true)))Configure(a,clip);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        return new{clip=clip.name,windup="unchanged",hitWindow=".18–.36",rightArmOnly=true};
    }
    static void Configure(Mavis.SahurAttack attack,AnimationClip clip){attack.chargedStrikeClip=clip;attack.heavySwingWindowStart=.18f;attack.heavySwingWindowEnd=.36f;EditorUtility.SetDirty(attack);}
}
