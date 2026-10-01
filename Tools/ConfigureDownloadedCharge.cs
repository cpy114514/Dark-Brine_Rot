using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ConfigureDownloadedCharge
{
    public static object Run()
    {
        if(Application.isPlaying) throw new Exception("Stop Play Mode first.");
        const string folder="Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/DownloadedHeavy/";
        var clips=AssetDatabase.LoadAllAssetsAtPath(folder+"UAL2_HeavySource.fbx").OfType<AnimationClip>().ToArray();
        var strike=clips.Single(c=>c.name=="SahurDownloadedHeavy");
        var recovery=clips.Single(c=>c.name=="SahurDownloadedRecovery");
        var heavy=AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"SahurChargedSwing.anim");
        if(!heavy) { heavy=new AnimationClip(); AssetDatabase.CreateAsset(heavy,folder+"SahurChargedSwing.anim"); }
        EditorUtility.CopySerialized(strike,heavy); heavy.name="SahurChargedSwing";
        foreach(var binding in AnimationUtility.GetCurveBindings(strike).Union(AnimationUtility.GetCurveBindings(recovery)))
        {
            var a=AnimationUtility.GetEditorCurve(strike,binding); var b=AnimationUtility.GetEditorCurve(recovery,binding);
            var keys=(a!=null ? a.keys.Where(k=>k.time<strike.length-.0001f) : Array.Empty<Keyframe>()).ToList();
            if(b!=null) foreach(var original in b.keys) { var key=original; key.time+=strike.length; keys.Add(key); }
            else if(a!=null) { var key=a.keys.Last(); key.time=strike.length+recovery.length; keys.Add(key); }
            AnimationUtility.SetEditorCurve(heavy,binding,new AnimationCurve(keys.ToArray()));
        }
        var settings=AnimationUtility.GetAnimationClipSettings(heavy); settings.loopTime=false; settings.loopBlend=false;
        settings.startTime=0; settings.stopTime=strike.length+recovery.length; AnimationUtility.SetAnimationClipSettings(heavy,settings);
        AnimationUtility.SetAnimationEvents(heavy,Array.Empty<AnimationEvent>());
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        foreach(var layer in controller.layers) foreach(var child in layer.stateMachine.states)
        {
            var state=child.state;
            if(state.name!="Heavy Attack" && state.name!="Charge Windup") continue;
            state.motion=heavy; state.writeDefaultValues=false;
            state.timeParameterActive=state.name=="Charge Windup"; state.timeParameter="ChargePhase";
            state.speed=state.name=="Heavy Attack" ? 1.1f : 1;
            if(state.name=="Heavy Attack") foreach(var transition in state.transitions)
            { transition.exitTime=.94f; transition.duration=.12f; transition.hasFixedDuration=true; }
            EditorUtility.SetDirty(state);
        }
        var mask=controller.layers[1].avatarMask;
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,i==(int)AvatarMaskBodyPart.RightArm || i==(int)AvatarMaskBodyPart.RightFingers);
        EditorUtility.SetDirty(mask); EditorUtility.SetDirty(controller); EditorUtility.SetDirty(heavy);
        float peak=strike.length*.375f/heavy.length;
        const string prefabPath="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try { Configure(root.GetComponent<Mavis.SahurAttack>(),heavy,peak); PrefabUtility.SaveAsPrefabAsset(root,prefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        const string scenePath="Assets/Scenes/First Island/Gameplay.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath); bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
        foreach(var attack in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Mavis.SahurAttack>(true)))
        { Undo.RecordObject(attack,"Replace charged attack animation"); Configure(attack,heavy,peak); PrefabUtility.RecordPrefabInstancePropertyModifications(attack); }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); if(opened) EditorSceneManager.CloseScene(scene,true);
        AssetDatabase.SaveAssets();
        return new { heavy.name,heavy.length,peak,hitStart=.145f,hitEnd=.30f };
    }
    static void Configure(Mavis.SahurAttack attack,AnimationClip clip,float peak)
    {
        if(!attack) return;
        attack.chargeClip=clip; attack.fullChargeTime=1.8f; attack.maxChargePosePhase=peak;
        attack.heavySwingWindowStart=.145f; attack.heavySwingWindowEnd=.30f; EditorUtility.SetDirty(attack);
    }
}
