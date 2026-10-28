using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Post-retarget polish on versioned humanoid clips. Source FBXs/actions stay intact.
public static class PolishJumpClipsV276
{
    public static object Install()
    {
        var paths=new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
        Directory.CreateDirectory(".codex/jump-transitions-v276/controllers-before");
        foreach(var path in paths)
        {
            string backup=".codex/jump-transitions-v276/controllers-before/"+Path.GetFileNameWithoutExtension(path)+".controller";
            if(!File.Exists(backup))File.Copy(path,backup);
            var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
            foreach(var pair in new[]{("Jump Slash","Club"),("Capri Jump Slash","Blade")})
            {
                var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==pair.Item1).state;
                state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Combat/Combinations/"+pair.Item2+"JumpChopV276.anim");EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        return new{installed=true,paths};
    }
    public static object Author()
    {
        foreach(string mode in new[]{"Club","Blade"})
        {
            string source="Assets/Resources/Combat/Combinations/"+(mode=="Club"?"ClubJumpChopV269":"BladeJumpChopV270")+".anim";
            string path="Assets/Resources/Combat/Combinations/"+mode+"JumpChopV276.anim";
            var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(source);var clip=UnityEngine.Object.Instantiate(original);clip.name=mode+"JumpChopV276";
            foreach(var binding in AnimationUtility.GetCurveBindings(original))
            {
                string name=binding.propertyName;
                bool arm=new[]{"Left Shoulder ","Right Shoulder ","Left Arm ","Right Arm ","Left Forearm ","Right Forearm ","Left Hand ","Right Hand "}.Any(name.StartsWith);
                if(!arm)continue;
                float limit=name.Contains("Twist")?.85f:name.Contains("Hand In-Out")?.7f:name.Contains("Hand Down-Up")?.85f:1f;
                var sourceCurve=AnimationUtility.GetEditorCurve(original,binding);var curve=new AnimationCurve();
                float[] weights={1,4,6,4,1};
                for(int frame=0;frame<=51;frame++)
                {
                    float sum=0;
                    for(int j=-2;j<=2;j++)sum+=Mathf.Clamp(sourceCurve.Evaluate(Mathf.Clamp(frame+j,0,51)/60f),-limit,limit)*weights[j+2];
                    curve.AddKey(frame/60f,sum/16f);
                }
                for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
                AnimationUtility.SetEditorCurve(clip,binding,curve);
            }
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}
            else AssetDatabase.CreateAsset(clip,path);
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        }
        return new{authored=true};
    }
}
