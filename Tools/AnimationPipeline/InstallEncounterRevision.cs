using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallEncounterRevision
{
    const string Folder="Assets/Resources/Encounter/v001";
    static AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(Folder+"/"+name+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
    public static object Install(bool ship=false)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Encounter"))AssetDatabase.CreateFolder("Assets/Resources","Encounter");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v001");
        var names=ship ? new[]{"CH1_ShipTailSmash_v001"} : new[]{"CH1_SharkBite_v001","CH1_SharkTail_v001","CH1_SharkRecoil_v001","CH1_SahurDiagonalSlash_v001"};
        foreach(var name in names)
        {
            string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,Folder+"/"+name);
            if(error!="")throw new Exception(error);
        }
        string controllerPath=Folder+"/EncounterShark.controller",prefabPath=Folder+"/EncounterShark.prefab";
        if(!ship)
        {
            if(!AssetDatabase.CopyAsset("Assets/Resources/SharkAnimation/TralaleroAnatomy.controller",controllerPath))throw new Exception("Controller copy failed");
            AssetDatabase.CopyAsset("Assets/Resources/SharkAnimation/TralaleroAnimations.asset",Folder+"/OriginalAnimationSet.asset");
        }
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        foreach(var state in controller.layers[0].stateMachine.states.Select(s=>s.state))
        {
            string name=ship ? state.name=="Ship_Smash" ? "CH1_ShipTailSmash_v001" : null :
                state.name=="Bite_Lunge" ? "CH1_SharkBite_v001" : state.name=="Tail_Strike" ? "CH1_SharkTail_v001" : state.name=="Hit_Recoil" ? "CH1_SharkRecoil_v001" : null;
            if(name!=null){state.motion=Clip(name);EditorUtility.SetDirty(state);}
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(ship ? prefabPath : "Assets/Resources/SharkAnimation/TralaleroRig.prefab");
        var instance=UnityEngine.Object.Instantiate(source);var animator=instance.GetComponentInChildren<Animator>();animator.enabled=false;animator.runtimeAnimatorController=controller;
        var tail=instance.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>("Assets/Resources/SharkAnimation/TralaleroAnimations.asset");
        Clip(ship ? "CH1_ShipTailSmash_v001" : "CH1_SharkTail_v001").SampleAnimation(animator.gameObject,.65f);
        Vector3 contact=instance.transform.InverseTransformPoint(tail.position);
        if(ship)set.shipSmashContact=contact;else set.tailStrikeContact=contact;
        var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();Bounds bounds=skin.localBounds;
        foreach(var name in names.Where(n=>n.Contains("Shark")||n.Contains("Ship")))for(int i=0;i<=30;i++)
        {
            var clip=Clip(name);clip.SampleAnimation(animator.gameObject,clip.length*i/30);skin.BakeMesh(baked);
            foreach(var vertex in baked.vertices)bounds.Encapsulate(vertex);
        }
        bounds.Expand(.1f);skin.localBounds=bounds;
        var cruise=controller.animationClips.Single(c=>c.name=="Swim_Cruise");cruise.SampleAnimation(animator.gameObject,0);animator.enabled=true;
        set.rigPrefab=PrefabUtility.SaveAsPrefabAsset(instance,prefabPath);
        EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(baked);
        return new{names,controllerPath,prefabPath,contact=contact.ToString("F5"),preservedOriginalRig=true};
    }
}
