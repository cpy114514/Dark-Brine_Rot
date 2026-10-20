using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mavis;

public static class InstallNailongAttacksV228
{
    const string Folder="Assets/Resources/BossCombat/Animations/v228";
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stop Play before asset installation.");
        Ensure("Assets/Resources/BossCombat/Animations"); Ensure(Folder);
        var shader=Shader.Find("DarkBrine/Nailong Water Droplet");
        if(!shader)throw new Exception("Water shader missing.");
        string matPath="Assets/Resources/BossCombat/NailongSaliva.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!material){material=new Material(shader){name="Nailong Saliva Water"};AssetDatabase.CreateAsset(material,matPath);}
        material.SetColor("_WaterTint",new Color(.84f,.94f,.97f,1));
        material.SetFloat("_RefractionPixels",2);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        var sonar=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSonar.mat");
        if(sonar)
        {
            sonar.SetColor("_Tint",new Color(.88f,.94f,1,1));sonar.SetFloat("_DistortionPixels",3);sonar.SetFloat("_RimOpacity",.045f);
            EditorUtility.SetDirty(sonar);AssetDatabase.SaveAssetIfDirty(sonar);
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var controller=prefab.GetComponentInChildren<Animator>().runtimeAnimatorController;
        var idle=controller.animationClips.First(c=>c.name.Contains("Idle"));
        Build(idle,"NailongSpit",1.6f,new Dictionary<string,float[]> {
            {"Spine Front-Back",new[]{0f,-.07f,-.12f,.16f,.09f,.025f,0f}},
            {"Chest Front-Back",new[]{0f,-.09f,-.15f,.20f,.11f,.03f,0f}},
            {"Head Nod Down-Up",new[]{0f,.06f,.12f,-.12f,-.04f,0f,0f}},
            {"Right Arm Down-Up",new[]{0f,.08f,.14f,.18f,.12f,.04f,0f}},
            {"Right Arm Front-Back",new[]{0f,.04f,.12f,-.23f,-.16f,-.04f,0f}},
            {"Right Forearm Stretch",new[]{0f,.03f,.06f,.24f,.14f,.04f,0f}},
            {"Left Arm Front-Back",new[]{0f,.06f,.10f,-.10f,-.06f,0f,0f}},
            {"Jaw Close",new[]{0f,-.10f,-.28f,-.12f,-.04f,0f,0f}}
        },new[]{0f,.4f,.78f,.9f,1.03f,1.23f,1.6f});
        Build(idle,"NailongRoar",1.7f,new Dictionary<string,float[]> {
            {"Spine Front-Back",new[]{0f,-.08f,-.14f,.22f,.16f,.05f,0f}},
            {"Chest Front-Back",new[]{0f,-.10f,-.18f,.24f,.18f,.04f,0f}},
            {"Head Nod Down-Up",new[]{0f,.06f,.15f,-.10f,-.06f,.02f,0f}},
            {"Left Arm Down-Up",new[]{0f,.12f,.23f,.32f,.25f,.09f,0f}},
            {"Right Arm Down-Up",new[]{0f,.12f,.23f,.32f,.25f,.09f,0f}},
            {"Left Forearm Stretch",new[]{0f,.02f,.05f,.18f,.12f,.03f,0f}},
            {"Right Forearm Stretch",new[]{0f,.02f,.05f,.18f,.12f,.03f,0f}},
            {"Jaw Close",new[]{0f,-.12f,-.35f,-.55f,-.3f,-.07f,0f}}
        },new[]{0f,.45f,.88f,1f,1.12f,1.45f,1.7f});
        return new {material=matPath,clips=new[]{Folder+"/NailongSpit.anim",Folder+"/NailongRoar.anim"},originalClipsPreserved=true};
    }
    static void Ensure(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        Ensure(Path.GetDirectoryName(path).Replace('\\','/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
    }
    static void Build(AnimationClip source,string name,float seconds,Dictionary<string,float[]> poses,float[] times)
    {
        string path=Folder+"/"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(!clip){clip=UnityEngine.Object.Instantiate(source);clip.name=name;AssetDatabase.CreateAsset(clip,path);}
        else EditorUtility.CopySerialized(source,clip);
        clip.name=name;clip.frameRate=60;clip.wrapMode=WrapMode.Once;
        float scale=seconds/source.length;
        foreach(var binding in AnimationUtility.GetCurveBindings(source))
        {
            var curve=AnimationUtility.GetEditorCurve(source,binding);
            var keys=curve.keys;
            for(int i=0;i<keys.Length;i++) {keys[i].time*=scale;keys[i].inTangent/=scale;keys[i].outTangent/=scale;}
            AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));
        }
        foreach(var pair in poses)
        {
            if(!HumanTrait.MuscleName.Contains(pair.Key))throw new Exception("Unknown humanoid muscle "+pair.Key);
            var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),pair.Key);
            var baseline=AnimationUtility.GetEditorCurve(source,binding);
            var delta=new AnimationCurve(times.Select((t,i)=>new Keyframe(t,pair.Value[i],0,0)).ToArray());
            var keys=new Keyframe[Mathf.RoundToInt(seconds*60)+1];
            for(int i=0;i<keys.Length;i++)
            {
                float t=i/60f;
                keys[i]=new Keyframe(t,Mathf.Clamp((baseline!=null?baseline.Evaluate(t/scale):0)+delta.Evaluate(t),-1,1));
            }
            var result=new AnimationCurve(keys);
            for(int i=0;i<keys.Length;i++)AnimationUtility.SetKeyBroken(result,i,false);
            for(int i=0;i<keys.Length;i++) {AnimationUtility.SetKeyLeftTangentMode(result,i,AnimationUtility.TangentMode.Auto);AnimationUtility.SetKeyRightTangentMode(result,i,AnimationUtility.TangentMode.Auto);}
            AnimationUtility.SetEditorCurve(clip,binding,result);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlend=false;
        settings.startTime=0;settings.stopTime=seconds;AnimationUtility.SetAnimationClipSettings(clip,settings);
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        if(!clip.humanMotion)throw new Exception("Humanoid clip generation failed: "+name);
    }
}
