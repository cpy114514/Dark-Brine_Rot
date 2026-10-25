using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Mavis;
public static class InstallCombatRecoveryV245
{
    public static object Apply()
    {
        if(Application.isPlaying)throw new Exception("Install in Edit Mode");
        var p=AssetDatabase.LoadAssetAtPath<CombatFeelProfile>("Assets/Resources/Combat/SahurCombatFeel.asset");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Combat/SahurReceivedHitV245.anim") ?? AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Encounter/v006/PLAYER_Stagger_v006/PLAYER_Stagger_v006.fbx").OfType<AnimationClip>().First(c=>c.name=="PLAYER_Stagger_v006");
        Undo.RecordObject(p,"Tune confirmed combat impacts and recovery");
        p.normal=new CombatFeelProfile.Impact(.055f,.065f,.13f,1.05f,1.25f,.16f);
        p.second=new CombatFeelProfile.Impact(.04f,.05f,.11f,.95f,1.1f,.13f);
        p.finisher=new CombatFeelProfile.Impact(.11f,.095f,.18f,1.3f,1.7f,.45f);
        p.heavy=new CombatFeelProfile.Impact(.095f,.1f,.19f,1.4f,1.8f,.7f);
        p.received=new CombatFeelProfile.Impact(.055f,.075f,.15f,.95f,1,0);
        p.receivedHeavy=new CombatFeelProfile.Impact(.085f,.10f,.19f,1.2f,1.25f,0);
        p.rifle=new CombatFeelProfile.Impact(.028f,.025f,.075f,.7f,.65f,0);
        p.incomingDamageMultiplier=.7f;p.maximumHitHealthFraction=.18f;p.damageRecoverySeconds=.6f;
        p.dodgeProtectionStart=.04f;p.dodgeProtectionEnd=.72f;
        p.receivedHitClip=clip;p.receivedReactionSeconds=.36f;p.receivedReactionWeight=.85f;
        EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);
        var data=new{p.incomingDamageMultiplier,p.maximumHitHealthFraction,p.damageRecoverySeconds,p.dodgeProtectionStart,p.dodgeProtectionEnd,received=clip.name,clip.length,p.normal,p.heavy,p.receivedHeavy};
        File.WriteAllText(".codex/combat-recovery-20261023/profile-installed.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;
    }
}
