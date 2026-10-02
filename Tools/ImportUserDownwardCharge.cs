using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ImportUserDownwardCharge
{
    public const string Source="Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx";
    public const string ClipName="SahurStandingMeleeDownward";
    public const string Output="Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/SahurStandingDownwardCharge.anim";
    static bool IsRight(string n)=>n.StartsWith("Right Shoulder")||n.StartsWith("Right Arm")||n.StartsWith("Right Forearm")||n.StartsWith("Right Hand")||n.StartsWith("RightHand.");
    public static object Import(){
        if(Application.isPlaying)throw new Exception("Import outside Play Mode.");
        AssetDatabase.ImportAsset(Source,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(Source);
        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=true;
        importer.animationCompression=ModelImporterAnimationCompression.Off;
        var clip=importer.defaultClipAnimations.First();clip.name=ClipName;clip.loopTime=clip.loopPose=false;
        clip.lockRootRotation=clip.lockRootHeightY=clip.lockRootPositionXZ=true;clip.events=Array.Empty<AnimationEvent>();
        importer.clipAnimations=new[]{clip};importer.SaveAndReimport();
        return new{clip.firstFrame,clip.lastFrame,assets=AssetDatabase.LoadAllAssetsAtPath(Source).Where(a=>a is Avatar||a is AnimationClip).Select(a=>new{name=a.name,type=a.GetType().Name,valid=a is Avatar avatar?avatar.isValid&&avatar.isHuman:((AnimationClip)a).isHumanMotion,length=a is AnimationClip c?c.length:0}).ToArray()};
    }
    public static object Build(){
        var source=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().Single(c=>c.name==ClipName);
        var output=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output);
        if(!output){output=new AnimationClip();AssetDatabase.CreateAsset(output,Output);}
        output.ClearCurves();output.name=ClipName;output.frameRate=source.frameRate;
        foreach(var binding in AnimationUtility.GetCurveBindings(source).Where(b=>b.type==typeof(Animator)&&IsRight(b.propertyName))){
            var curve=AnimationUtility.GetEditorCurve(source,binding);
            if(binding.propertyName.Contains("Stretched")){
                float grip=binding.propertyName.StartsWith("RightHand.Thumb.")?-.25f:binding.propertyName.Contains(".2 Stretched")?-.65f:-.45f;
                curve=AnimationCurve.Linear(0,grip,source.length,grip);
            }
            AnimationUtility.SetEditorCurve(output,binding,curve);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(output);settings.startTime=0;settings.stopTime=source.length;settings.loopTime=settings.loopBlend=false;
        AnimationUtility.SetAnimationClipSettings(output,settings);AnimationUtility.SetAnimationEvents(output,Array.Empty<AnimationEvent>());
        EditorUtility.SetDirty(output);AssetDatabase.SaveAssetIfDirty(output);
        return new{source=Source,output=Output,output.length,output.humanMotion,originalArmAndWristCurves=true};
    }
    public static object Install(float holdPhase=.28f,float hitStart=.35f,float hitEnd=.43f,float windupSeconds=.48f,float speed=1f){
        if(Application.isPlaying)throw new Exception("Install outside Play Mode.");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output);
        if(!clip.isHumanMotion)throw new Exception("The animation must be humanoid.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        if(!controller.parameters.Any(p=>p.name=="HeavyPlaybackSpeed"))controller.AddParameter("HeavyPlaybackSpeed",AnimatorControllerParameterType.Float);
        var parameters=controller.parameters;parameters.Single(p=>p.name=="HeavyPlaybackSpeed").defaultFloat=1;controller.parameters=parameters;
        foreach(var layer in controller.layers)foreach(var child in layer.stateMachine.states){var state=child.state;
            if(state.name!="Charge Windup"&&state.name!="Heavy Attack")continue;
            Undo.RecordObject(state,"Use supplied downward attack");state.motion=clip;state.speed=speed;state.speedParameterActive=false;
            state.timeParameterActive=state.name=="Charge Windup";state.timeParameter="ChargePhase";state.writeDefaultValues=false;EditorUtility.SetDirty(state);}
        var heavy=controller.layers.Single(l=>l.name=="Charge Upper Body").stateMachine.states.Single(s=>s.state.name=="Heavy Attack").state;
        heavy.speed=1;heavy.speedParameter="HeavyPlaybackSpeed";heavy.speedParameterActive=true;EditorUtility.SetDirty(heavy);
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        const string prefab="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
        var root=PrefabUtility.LoadPrefabContents(prefab);
        try{Configure(root.GetComponent<Mavis.SahurAttack>(),clip,holdPhase,hitStart,hitEnd,windupSeconds);PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
        const string demo="Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoCombatDemo.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(demo);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(demo,OpenSceneMode.Additive);
        try{foreach(var a in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Mavis.SahurAttack>(true)))Configure(a,clip,holdPhase,hitStart,hitEnd,windupSeconds);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        return new{source=Source,clip=clip.name,clip.length,holdPhase,hitStart,hitEnd,windupSeconds,speed,rightArmOnly=true};
    }
    static void Configure(Mavis.SahurAttack a,AnimationClip c,float hold,float start,float end,float windup){a.chargeClip=c;a.chargedStrikeClip=null;a.maxChargePosePhase=hold;a.chargeWindupTime=windup;a.heavySwingWindowStart=start;a.heavySwingWindowEnd=end;EditorUtility.SetDirty(a);}
}
