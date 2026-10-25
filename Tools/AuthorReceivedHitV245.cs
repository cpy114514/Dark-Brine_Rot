using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class AuthorReceivedHitV245
{
    public static object Apply()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode authoring required");
        const string path="Assets/Resources/Combat/SahurReceivedHitV245.anim";
        if(AssetDatabase.LoadAssetAtPath<AnimationClip>(path))throw new Exception("Retain existing authored revision");
        var original=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Encounter/v006/PLAYER_Stagger_v006/PLAYER_Stagger_v006.fbx").OfType<AnimationClip>().First(c=>c.name=="PLAYER_Stagger_v006");
        // Calibrated on the live controller layer and rendered Sahur rig.
        int sign=1;
        var clip=UnityEngine.Object.Instantiate(original);clip.name="Sahur received hit — supported torso v245";clip.frameRate=60;
        // The original stagger carries most lean in RootQ and UpperChest.
        // RootQ must stay masked for locomotion, and this avatar has no mapped
        // UpperChest. Author the impulse into its supported spine/chest muscles.
        foreach(var entry in new[]{("Spine Front-Back",.44f),("Chest Front-Back",.36f)})
        {
            var curve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.035f,sign*entry.Item2*.35f),new Keyframe(.10f,sign*entry.Item2),new Keyframe(.19f,sign*entry.Item2*.7f),new Keyframe(.36f,sign*entry.Item2*.12f),new Keyframe(.55f,0),new Keyframe(.75f,0));
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),entry.Item1),curve);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,path);var profile=CombatFeelProfile.Default;Undo.RecordObject(profile,"Use supported authored torso reaction");profile.receivedHitClip=clip;
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(clip);AssetDatabase.SaveAssetIfDirty(profile);
        var data=new{path,source=AssetDatabase.GetAssetPath(original),clip.isHumanMotion,clip.length,sign,calibration="Native rendered-pose comparison in rendered-hurt-verification.json",note="Only supported humanoid spine/chest curves are revised; root motion remains excluded by the runtime upper-body mask."};
        File.WriteAllText(".codex/combat-recovery-20261023/authored-received-hit.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;
    }
}
