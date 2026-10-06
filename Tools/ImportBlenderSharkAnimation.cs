using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ImportBlenderSharkAnimation
{
    const string Folder="Assets/Resources/SharkAnimation/";
    public static object Setup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Requires Edit Mode.");
        string modelPath=Folder+"TralaleroAnimated.fbx";
        AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceUpdate);
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.isReadable=true;importer.optimizeGameObjects=false;
        importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        var specs=importer.defaultClipAnimations;
        foreach(var spec in specs)
        {
            spec.name=spec.name.Split('|').Last();spec.loopTime=spec.name.StartsWith("Swim_");spec.loopPose=spec.loopTime;
            spec.lockRootRotation=true;spec.lockRootHeightY=true;spec.lockRootPositionXZ=true;
            spec.keepOriginalOrientation=true;spec.keepOriginalPositionY=true;spec.keepOriginalPositionXZ=true;
        }
        importer.clipAnimations=specs;importer.SaveAndReimport();
        var clips=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        if(clips.Length!=7)throw new Exception("Expected seven Blender actions.");
        AnimationClip Clip(string name)=>clips.Single(c=>c.name==name);
        string controllerPath=Folder+"TralaleroAnatomy.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=controller.layers[0].stateMachine;
        if(!controller.parameters.Any(p=>p.name=="SwimEffort"))controller.AddParameter("SwimEffort",AnimatorControllerParameterType.Float);
        AnimatorState State(string name)
        {
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);
            state.writeDefaultValues=true;return state;
        }
        var swim=State("Swim");var blend=swim.motion as BlendTree;
        if(blend==null)
        {
            blend=new BlendTree{name="Swimming effort",blendType=BlendTreeType.Simple1D,blendParameter="SwimEffort",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(blend,controller);
        }
        blend.children=new[]{new ChildMotion{motion=Clip("Swim_Cruise"),threshold=0,timeScale=1},new ChildMotion{motion=Clip("Swim_Fast"),threshold=1,timeScale=1}};
        swim.motion=blend;machine.defaultState=swim;EditorUtility.SetDirty(blend);
        foreach(var clip in clips.Where(c=>!c.name.StartsWith("Swim_")))
        {
            var state=State(clip.name);state.motion=clip;state.speed=1;
            foreach(var old in state.transitions)state.RemoveTransition(old);
            var exit=state.AddTransition(swim);exit.hasExitTime=true;exit.exitTime=1;exit.hasFixedDuration=true;exit.duration=.12f;
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));model.name="Animated shark skeleton";
        var animator=model.GetComponent<Animator>()??model.GetComponentInChildren<Animator>();animator.enabled=false;
        var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
        var baked=new Mesh();skin.BakeMesh(baked);
        Bounds rest=new Bounds();bool first=true;
        foreach(var vertex in baked.vertices)
        {
            var point=skin.transform.TransformPoint(vertex);
            if(first){rest=new Bounds(point,Vector3.zero);first=false;}else rest.Encapsulate(point);
        }
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/TralaleroTralala/Models/TralaleroTralala.fbx").GetComponentInChildren<MeshFilter>();
        Bounds target=original.sharedMesh.bounds;
        var guide=new GameObject("Tralalero animated visual");
        var frame=new GameObject("Original mesh coordinates");frame.transform.SetParent(guide.transform,false);
        float fit=target.size.z/rest.size.z;
        frame.transform.localScale=Vector3.one*fit;frame.transform.localPosition=target.center-rest.center*fit;
        model.transform.SetParent(frame.transform,false);
        var sourcePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/TralaleroTralala/TralaleroTralala.prefab");
        skin.sharedMaterials=sourcePrefab.GetComponentInChildren<Renderer>().sharedMaterials;
        skin.updateWhenOffscreen=true;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var tail=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");
        Clip("Tail_Strike").SampleAnimation(model,.65f);Vector3 tailContact=guide.transform.InverseTransformPoint(tail.position);
        Clip("Ship_Smash").SampleAnimation(model,.65f);Vector3 smashContact=guide.transform.InverseTransformPoint(tail.position);
        // Sample the authored range for culling, including the larger final tail sweep.
        Bounds animatedBounds=new Bounds();first=true;
        foreach(var clip in clips)for(int i=0;i<=10;i++)
        {
            clip.SampleAnimation(model,clip.length*i/10);skin.BakeMesh(baked);
            foreach(var p in baked.vertices)
            {
                if(first){animatedBounds=new Bounds(p,Vector3.zero);first=false;}else animatedBounds.Encapsulate(p);
            }
        }
        animatedBounds.Expand(.5f);skin.localBounds=animatedBounds;
        Clip("Swim_Cruise").SampleAnimation(model,0);animator.enabled=true;
        string prefabPath=Folder+"TralaleroRig.prefab";
        var prefab=PrefabUtility.SaveAsPrefabAsset(guide,prefabPath);
        string setPath=Folder+"TralaleroAnimations.asset";
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(setPath);
        if(set==null){set=ScriptableObject.CreateInstance<TralaleroAnimationSet>();AssetDatabase.CreateAsset(set,setPath);}
        set.rigPrefab=prefab;set.tailStrikeContact=tailContact;set.shipSmashContact=smashContact;
        EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        UnityEngine.Object.DestroyImmediate(guide);UnityEngine.Object.DestroyImmediate(baked);
        return new{clips=clips.Select(c=>new{c.name,c.length,c.isLooping}).ToArray(),bones=16,restBounds=rest.ToString("F5"),
            originalMeshBounds=target.ToString("F5"),fit,tailContact=tailContact.ToString("F5"),smashContact=smashContact.ToString("F5"),prefabPath};
    }
}
