using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class TuneLocomotionV006
{
    public static object Tune()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        const string path="Assets/Resources/Encounter/v006/SahurGameplay.controller";
        const string backup="Assets/Resources/Encounter/v008/PreviousLocomotion.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(backup)==null)AssetDatabase.CopyAsset(path,backup);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Locomotion").state;
        var tree=(BlendTree)state.motion;var children=tree.children;
        tree.useAutomaticThresholds=false;
        foreach(int i in Enumerable.Range(0,children.Length))
        {
            if(children[i].motion.name=="PLAYER_Walk_v006")children[i].threshold=3.59f;
            if(children[i].motion.name=="PLAYER_Run_v006")children[i].threshold=7.44f;
        }
        tree.children=children;EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        return new{walkThreshold=3.59f,runThreshold=7.44f,source="actual retargeted backward foot median"};
    }
}
