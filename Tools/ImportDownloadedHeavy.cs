using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class ImportDownloadedHeavy
{
    public const string Path = "Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/DownloadedHeavy/UAL2_HeavySource.fbx";
    public static object Run()
    {
        AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(Path);
        var template = (ModelImporter)AssetImporter.GetAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx");
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.humanDescription = template.humanDescription;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        var source = importer.defaultClipAnimations.FirstOrDefault(c => c.name == "Armature|Sword_Regular_A");
        if (source == null) return importer.defaultClipAnimations.Select(c => new { c.name, c.takeName, c.firstFrame, c.lastFrame }).ToArray();
        source.name = "SahurDownloadedHeavy";
        source.loopTime = false; source.loopPose = false;
        source.lockRootRotation = true; source.lockRootHeightY = true; source.lockRootPositionXZ = true;
        var recovery = importer.defaultClipAnimations.Single(c => c.name == "Armature|Sword_Regular_A_Rec");
        recovery.name = "SahurDownloadedRecovery";
        recovery.loopTime = false; recovery.lockRootRotation = true; recovery.lockRootHeightY = true; recovery.lockRootPositionXZ = true;
        importer.clipAnimations = new[] { source, recovery };
        importer.SaveAndReimport();
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        return new { source.firstFrame, source.lastFrame, clips = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<AnimationClip>().Select(c => new { c.name, c.length, c.isHumanMotion }).ToArray(), layers = controller.layers.Select(l => new { l.name, l.iKPass, mask = l.avatarMask ? l.avatarMask.name : "", states = l.stateMachine.states.Select(s => new { s.state.name, motion = s.state.motion ? s.state.motion.name : "", s.state.speed, s.state.timeParameterActive }).ToArray() }).ToArray() };
    }
}
