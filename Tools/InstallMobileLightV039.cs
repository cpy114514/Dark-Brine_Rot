using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallMobileLightV039
{
    public static object Install()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        const string path="Assets/Resources/Encounter/v006/SahurGameplay.controller";
        string backup=".codex/opening-optimization-20261012/backups/SahurGameplay.controller";
        if(!File.Exists(backup))File.Copy(path,backup);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        var upper=controller.layers.Single(l=>l.name=="Charge Upper Body").stateMachine;
        var original=controller.layers[0].stateMachine;
        var untouched=original.states.Where(s=>s.state.name=="Locomotion" || s.state.name.Contains("Roll")).Select(s=>new{s.state.name,motion=AssetDatabase.GetAssetPath(s.state.motion)}).ToArray();
        int added=0;
        foreach(string name in new[]{"Combo 1","Combo 2","Combo 3"})
        {
            if(upper.states.Any(s=>s.state.name==name))continue;
            var source=original.states.Single(s=>s.state.name==name).state;
            var state=upper.AddState(name);state.motion=source.motion;state.speed=source.speed;
            state.writeDefaultValues=false;state.iKOnFeet=false;
            EditorUtility.SetDirty(state);added++;
        }
        EditorUtility.SetDirty(upper);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        return new{added,unchangedBaseStates=untouched,rightArmMask=AssetDatabase.GetAssetPath(controller.layers.Single(l=>l.name=="Charge Upper Body").avatarMask)};
    }
}
