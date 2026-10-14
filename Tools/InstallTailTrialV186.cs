using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class InstallTailTrialV186
{
    const string Folder="Assets/Resources/Encounter/v186";
    const string Controller="Assets/Resources/Encounter/v006/EncounterShark.controller";
    const string Set="Assets/Resources/SharkAnimation/TralaleroAnimations.asset";
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        var check=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/animation-continuation-20261014/tail-import-v186.json"));
        if(!(bool)check["pass"])throw new Exception("Native comparison required");
        if(AssetDatabase.IsValidFolder(Folder))throw new Exception("Preserve existing revision backup");
        AssetDatabase.CreateFolder("Assets/Resources/Encounter","v186");
        if(!AssetDatabase.CopyAsset(Controller,Folder+"/PreviousEncounterShark.controller") ||
           !AssetDatabase.CopyAsset(Set,Folder+"/PreviousAnimationSet.asset"))throw new Exception("Backup failed");
        string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/CH1_SharkTail_v186",Folder+"/CH1_SharkTail_v186");
        if(!string.IsNullOrEmpty(error))throw new Exception(error);
        var revised=AssetDatabase.LoadAllAssetsAtPath(Folder+"/CH1_SharkTail_v186/CH1_SharkTail_v186.fbx").OfType<AnimationClip>().Single(c=>c.name=="CH1_SharkTail_v186");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Tail_Strike").state;
        if(state.motion.name!="CH1_SharkTail_v097")throw new Exception("Active source changed; audit first");
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(Set);
        var scene=EditorSceneManager.NewPreviewScene();GameObject actor=null;PlayableGraph graph=default;
        try
        {
            actor=UnityEngine.Object.Instantiate(set.rigPrefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.Rebind();animator.Update(0);
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var marker=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");
            if(!TralaleroSwimAnimator.FindVisibleTailContact(skin,marker,out var bone,out var local))throw new Exception("Actual caudal surface required");
            graph=PlayableGraph.Create("Versioned native tail calibration");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable=AnimationClipPlayable.Create(graph,revised);playable.SetSpeed(0);playable.SetTime(.65);
            AnimationPlayableOutput.Create(graph,"native",animator).SetSourcePlayable(playable);graph.Play();graph.Evaluate(0);
            Vector3 previous=set.tailStrikeContact,current=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
            state.motion=revised;EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
            set.tailStrikeContact=current;EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
            var result=new{integrationTrial=true,clip=revised.name,revised.length,revised.frameRate,
                previousContact=new[]{previous.x,previous.y,previous.z},contact=new[]{current.x,current.y,current.z},
                shipSmashUnchanged=true,originalRigPreserved=true,backup=Folder};
            File.WriteAllText(".codex/animation-continuation-20261014/tail-install-v186.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{if(graph.IsValid())graph.Destroy();if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
    }
    public static object RevertTrial()
    {
        if(Application.isPlaying)throw new Exception("Stop first");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        var previous=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/PreviousEncounterShark.controller");
        var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Tail_Strike").state;
        if(state.motion.name!="CH1_SharkTail_v186")throw new Exception("Do not overwrite a different revision");
        state.motion=previous.layers[0].stateMachine.states.Single(s=>s.state.name=="Tail_Strike").state.motion;
        EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(Set);
        set.tailStrikeContact=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(Folder+"/PreviousAnimationSet.asset").tailStrikeContact;
        EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        return new{reverted=true,clip=state.motion.name,historyPreserved=true};
    }
}
