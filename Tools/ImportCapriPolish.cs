using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class ImportCapriPolish
{
    public static object Curves()
    {
        return new[]{"Assets/ThirdParty/CapriPolishKayKit/Rig_Medium_CombatMelee.fbx","Assets/Resources/CapriPhase2/Polish/CapriP2ShadowSmooth.anim"}.Select(p=>
        {
            var c=AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().First(a=>a.name.Contains("Dualwield_Attack_Slice")||a.name.Contains("ShadowSmooth"));
            return new{p,c.humanMotion,bindings=AnimationUtility.GetCurveBindings(c).Where(b=>b.propertyName.Contains("Arm")||b.type!=typeof(Animator)).Select(b=>new{b.path,b.propertyName,type=b.type.Name,values=new[]{0f,.25f,.5f,.75f,1f}.Select(t=>AnimationUtility.GetEditorCurve(c,b)?.Evaluate(t*c.length))})};
        }).ToArray();
    }
    public static object Run()
    {
        AssetDatabase.Refresh();
        var avatar=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/KayKitLunarBound/Mannequin_Medium.fbx").OfType<Avatar>().First(a=>a.isValid&&a.isHuman);
        string dir="Assets/ThirdParty/CapriPolishKayKit/";
        foreach(string file in new[]{"Rig_Medium_CombatMelee.fbx","Rig_Medium_CombatRanged.fbx","Rig_Medium_General.fbx"})
        {
            var i=(ModelImporter)AssetImporter.GetAtPath(dir+file);i.animationType=ModelImporterAnimationType.Human;i.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;i.sourceAvatar=null;
            // KayKit has wrist -> hand as two joints. The lunar avatar maps only
            // wrist, which leaves the animated weapon-hand rotation unretargeted.
            var desc=((ModelImporter)AssetImporter.GetAtPath("Assets/ThirdParty/KayKitLunarBound/Mannequin_Medium.fbx")).humanDescription;
            var bones=desc.human.ToArray();for(int b=0;b<bones.Length;b++)
            {if(bones[b].humanName=="LeftHand")bones[b].boneName="hand.l";if(bones[b].humanName=="RightHand")bones[b].boneName="hand.r";}
            desc.human=bones;i.humanDescription=desc;
            i.importAnimation=true;i.materialImportMode=ModelImporterMaterialImportMode.None;i.animationCompression=ModelImporterAnimationCompression.Off;i.SaveAndReimport();
        }
        var report=Directory.GetFiles(dir,"Rig*.fbx").Select(p=>new{path=p,clips=AssetDatabase.LoadAllAssetsAtPath(p.Replace('\\','/')).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).Select(c=>new{c.name,c.length,c.humanMotion})}).ToArray();
        Directory.CreateDirectory("Documentation/CappuccinoPolishReview");File.WriteAllText("Documentation/CappuccinoPolishReview/import.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
}
