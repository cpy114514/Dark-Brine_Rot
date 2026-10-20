using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InstallDualBladePolishV002
{
    static AnimationClip Copy(string label)
    {
        string source="Assets/AnimationStaging/PLAYER_"+label+"_v002/PLAYER_"+label+"_v002.fbx";
        string target="Assets/Resources/Combat/Combinations/"+label+"V002.anim";
        if(AssetDatabase.LoadMainAssetAtPath(target))throw new Exception("Keep existing revision: "+target);
        var clip=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Single(x=>!x.name.StartsWith("__preview__"));
        if(!clip.humanMotion)throw new Exception("Humanoid retargeting missing");
        var copy=UnityEngine.Object.Instantiate(clip);copy.name=label+"V002";AssetDatabase.CreateAsset(copy,target);return copy;
    }
    public static object Run()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Use ready Edit Mode");
        var spin=Copy("BladeFinisher");var rush=Copy("BladeRush");
        var rows=new System.Collections.Generic.List<object>();
        foreach(var path in new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller"})
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            foreach(var name in new[]{"Capri Combo 3","Capri Heavy"})
            {
                var state=controller.layers[0].stateMachine.states.Single(x=>x.state.name==name).state;
                var previous=AssetDatabase.GetAssetPath(state.motion);state.motion=name=="Capri Combo 3"?spin:rush;EditorUtility.SetDirty(state);
                rows.Add(new{controller=path,state=name,previous,current=AssetDatabase.GetAssetPath(state.motion),state.speed});
            }
            EditorUtility.SetDirty(controller);
        }
        AssetDatabase.SaveAssets(); // Asset files only. Never save the user's open scene.
        var result=new{installed=true,rows};File.WriteAllText(".codex/dual-blade-polish-20261019/installed.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
