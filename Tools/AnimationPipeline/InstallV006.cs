using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallV006
{
    const string Folder="Assets/Resources/Encounter/v006";
    static AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(Folder+"/"+name+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
    public static object Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v006");
        foreach(string fbx in Directory.GetFiles("ArtSource/Encounter/v006","*.fbx"))
        {
            string name=Path.GetFileNameWithoutExtension(fbx);
            if(AssetDatabase.IsValidFolder(Folder+"/"+name))continue;
            string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,Folder+"/"+name);
            if(error!="")throw new Exception(error);
        }
        string controllerPath=Folder+"/SahurGameplay.controller";
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)!=null)throw new Exception("Gameplay controller already installed; revise explicitly.");
        AssetDatabase.CopyAsset("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller",controllerPath);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        void Param(string name){if(!controller.parameters.Any(p=>p.name==name))controller.AddParameter(name,AnimatorControllerParameterType.Float);}
        Param("BoardThrottle");Param("BoardSteer");Param("LocomotionRate");
        var rate=controller.parameters;foreach(var parameter in rate)if(parameter.name=="LocomotionRate")parameter.defaultFloat=1;controller.parameters=rate;
        var baseMachine=controller.layers[0].stateMachine;
        var locomotion=baseMachine.states.Single(s=>s.state.name=="Locomotion").state;
        var tree=locomotion.motion as BlendTree;
        if(tree!=null)
        {
            var children=tree.children;
            for(int i=0;i<children.Length;i++)
            {
                string name=children[i].motion!=null ? children[i].motion.name : "";
                if(name.Contains("Walk_Loop"))children[i].motion=Clip("PLAYER_Walk_v006");
                if(name.Contains("Jog_Fwd_Loop") || name.Contains("Sprint_Loop"))children[i].motion=Clip("PLAYER_Run_v006");
            }
            tree.children=children;EditorUtility.SetDirty(tree);
        }
        locomotion.speedParameter="LocomotionRate";locomotion.speedParameterActive=true;
        foreach(var child in baseMachine.states)
        {
            var state=child.state;
            string clip=state.name=="Jump Start" ? "PLAYER_JumpStart_v006" : state.name=="Jump Loop" ? "PLAYER_JumpAir_v006" : state.name=="Land" ? "PLAYER_JumpLand_v006" :
                state.name=="Wreck Climb" ? "PLAYER_Climb_v006" : state.name=="Finale Stagger" ? "PLAYER_Stagger_v006" : state.name=="Finale Knockdown" ? "PLAYER_Fall_v006" : null;
            if(clip!=null){state.motion=Clip(clip);state.speed=1;EditorUtility.SetDirty(state);}
        }
        var surf=baseMachine.states.Single(s=>s.state.name=="Wreck Surf").state;
        var surfTree=new BlendTree{name="Board balance response",blendType=BlendTreeType.FreeformDirectional2D,blendParameter="BoardSteer",blendParameterY="BoardThrottle"};
        AssetDatabase.AddObjectToAsset(surfTree,controller);
        surfTree.AddChild(Clip("PLAYER_BoardBalance_v006"),Vector2.zero);surfTree.AddChild(Clip("PLAYER_BoardDrive_v006"),Vector2.up);
        surfTree.AddChild(Clip("PLAYER_BoardBrake_v006"),Vector2.down);surfTree.AddChild(Clip("PLAYER_BoardLeft_v006"),Vector2.left);surfTree.AddChild(Clip("PLAYER_BoardRight_v006"),Vector2.right);
        surf.motion=surfTree;surf.speed=1;
        var upper=controller.layers[1].stateMachine;
        for(int i=1;i<=3;i++)
        {
            var state=upper.AddState("Combo "+i);state.motion=Clip("CH1_SahurLight"+i+"_v006");state.writeDefaultValues=false;
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var definition=ScriptableObject.CreateInstance<Story1EncounterDefinition>();definition.beats=Story1EncounterDefinition.ApprovedBeats();
        AssetDatabase.CreateAsset(definition,Folder+"/EncounterDefinition.asset");
        string sourcePrefab="Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab";
        AssetDatabase.CopyAsset(sourcePrefab,Folder+"/PreviousSahurPlayer.prefab");
        var actor=PrefabUtility.LoadPrefabContents(sourcePrefab);
        try
        {
            actor.GetComponent<Animator>().runtimeAnimatorController=controller;
            var movement=actor.GetComponent<ThirdPersonPlayerController>();movement.coyoteTime=.1f;movement.jumpBufferTime=.15f;
            var attack=actor.GetComponent<Mavis.SahurAttack>();
            attack.comboOneHitWindow=new Vector2(.49f,.66f);attack.comboTwoHitWindow=new Vector2(.46f,.66f);attack.comboThreeHitWindow=new Vector2(.42f,.61f);
            attack.weaponHitPadding=.05f;
            PrefabUtility.SaveAsPrefabAsset(actor,sourcePrefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(actor);}
        string sharkControllerPath=Folder+"/EncounterShark.controller";
        AssetDatabase.CopyAsset("Assets/Resources/Encounter/v003/EncounterShark.controller",sharkControllerPath);
        var sharkController=AssetDatabase.LoadAssetAtPath<AnimatorController>(sharkControllerPath);
        foreach(var child in sharkController.layers[0].stateMachine.states)
        {
            string name=child.state.name;string suffix=name=="Bite_Lunge" ? "Bite" : name=="Tail_Strike" ? "Tail" : name=="Ship_Smash" ? "ShipSmash" : name=="Hit_Recoil" ? "Recoil" : name=="Breach" ? "Breach" : null;
            if(suffix!=null){child.state.motion=Clip("CH1_Shark"+suffix+"_v006");EditorUtility.SetDirty(child.state);}
        }
        EditorUtility.SetDirty(sharkController);AssetDatabase.SaveAssetIfDirty(sharkController);
        const string setPath="Assets/Resources/SharkAnimation/TralaleroAnimations.asset";
        AssetDatabase.CopyAsset(setPath,Folder+"/PreviousAnimationSet.asset");
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(setPath);
        var instance=UnityEngine.Object.Instantiate(set.rigPrefab);var animator=instance.GetComponentInChildren<Animator>();animator.enabled=false;animator.runtimeAnimatorController=sharkController;
        var tail=instance.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");
        Clip("CH1_SharkTail_v006").SampleAnimation(animator.gameObject,.65f);set.tailStrikeContact=instance.transform.InverseTransformPoint(tail.position);
        Clip("CH1_SharkShipSmash_v006").SampleAnimation(animator.gameObject,.65f);set.shipSmashContact=instance.transform.InverseTransformPoint(tail.position);
        var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();Bounds bounds=skin.localBounds;
        foreach(var clip in sharkController.animationClips)for(int i=0;i<=30;i++)
        {clip.SampleAnimation(animator.gameObject,clip.length*i/30);skin.BakeMesh(mesh);foreach(var vertex in mesh.vertices)bounds.Encapsulate(vertex);}
        bounds.Expand(.1f);skin.localBounds=bounds;sharkController.animationClips.Single(c=>c.name=="Swim_Cruise").SampleAnimation(animator.gameObject,0);animator.enabled=true;
        set.rigPrefab=PrefabUtility.SaveAsPrefabAsset(instance,Folder+"/EncounterShark.prefab");EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(mesh);
        return new{installed=26,definition.duration,definition.fps,controller=controllerPath,preservedRigs=true,previousAssetsRetained=true};
    }
}
