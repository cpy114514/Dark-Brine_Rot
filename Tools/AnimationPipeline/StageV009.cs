using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Run through Unity MCP run_script; never replaces gameplay assets/controllers.</summary>
public static class UnityAnimationImport
{
    public static object StageEncounter(string source)
    {
        var result = new System.Collections.Generic.List<object>();
        foreach(var name in new[]{"CH1_SharkBite_v001","CH1_SharkTail_v001","CH1_SharkRecoil_v001","CH1_SahurDiagonalSlash_v001"})
            result.Add(Stage(Path.Combine(source,name+".fbx"),Path.Combine(source,name+".animation.json"),
                "Assets/AnimationStaging/"+name,name.Contains("Sahur") ? "Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx" : ""));
        return result;
    }
    [Serializable] public sealed class Manifest
    {
        public string action, unity_clip, revision, rig_type, root_motion, root_bone, stage;
        public string design_status, design_reference, approval_reference, review_status, review_reference;
        public string release_approval_reference, fbx;
        public int frame_start, frame_end;
        public float fps, duration_seconds;
        public bool loop;
    }

    static void RequireEditMode()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new InvalidOperationException("Requires a ready Editor in Edit Mode.");
    }
    static void ValidateFolder(string folder)
    {
        if (!Regex.IsMatch(folder, @"^Assets/AnimationStaging/[A-Za-z0-9_-]+$"))
            throw new ArgumentException("Use a new Assets/AnimationStaging/<revision> folder without path traversal.");
    }
    static Manifest ReadManifest(string path)
    {
        var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        if (m == null || string.IsNullOrWhiteSpace(m.action) || string.IsNullOrWhiteSpace(m.revision) ||
            !Regex.IsMatch(m.unity_clip ?? "", @"^[A-Za-z][A-Za-z0-9_]+$") || m.frame_end <= m.frame_start ||
            m.fps <= 0 || float.IsNaN(m.fps) || float.IsInfinity(m.fps) ||
            (m.rig_type != "Humanoid" && m.rig_type != "Generic") ||
            (m.root_motion != "in_place" && m.root_motion != "root_motion") ||
            string.IsNullOrWhiteSpace(m.root_bone) ||
            (m.design_status != "approved" && m.design_status != "approval_waived") ||
            string.IsNullOrWhiteSpace(m.design_reference) || string.IsNullOrWhiteSpace(m.approval_reference) ||
            m.review_status != "accepted_for_stage" || string.IsNullOrWhiteSpace(m.review_reference) ||
            !new[]{"BLOCKING","FIRST PASS","SECOND PASS","POLISH","FINAL","APPROVED"}.Contains(m.stage) ||
            (m.stage == "APPROVED" && string.IsNullOrWhiteSpace(m.release_approval_reference)))
            throw new ArgumentException("Incomplete, unapproved or inconsistent export manifest.");
        if (Mathf.Abs(m.duration_seconds - (m.frame_end-m.frame_start)/m.fps) > 0.001f)
            throw new ArgumentException("Export duration disagrees with source frame range/FPS.");
        return m;
    }

    public static object Stage(string fbx, string metadata, string folder, string sourceAvatarPath = "")
    {
        RequireEditMode(); ValidateFolder(folder);
        var m = ReadManifest(metadata);
        if (!File.Exists(fbx) || !string.Equals(Path.GetExtension(fbx), ".fbx", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Missing exported FBX.", fbx);
        if (!string.Equals(Path.GetFileName(fbx), m.fbx, StringComparison.Ordinal))
            throw new ArgumentException("FBX does not match exported manifest.");
        if (AssetDatabase.IsValidFolder(folder) || Directory.Exists(folder))
            throw new IOException("Staging revision already exists; existing assets will not be overwritten.");
        Avatar sourceAvatar = null;
        if (!string.IsNullOrEmpty(sourceAvatarPath))
        {
            sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(sourceAvatarPath) ??
                AssetDatabase.LoadAllAssetsAtPath(sourceAvatarPath).OfType<Avatar>().FirstOrDefault();
            if (sourceAvatar == null || !sourceAvatar.isValid || !sourceAvatar.isHuman || m.rig_type != "Humanoid")
                throw new ArgumentException("The explicitly chosen source Avatar must be valid and Humanoid.");
        }
        if (!AssetDatabase.IsValidFolder("Assets/AnimationStaging"))
            AssetDatabase.CreateFolder("Assets", "AnimationStaging");
        AssetDatabase.CreateFolder("Assets/AnimationStaging", Path.GetFileName(folder));
        string asset = folder + "/" + Path.GetFileName(fbx);
        try
        {
            File.WriteAllText(folder + "/.animation-pipeline-owned", "animation-pipeline-v1\n" + folder);
            File.Copy(fbx, asset, false);
            File.Copy(metadata, folder + "/" + Path.GetFileName(metadata), false);
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(asset) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("No ModelImporter for the FBX.");
            var takes = importer.defaultClipAnimations;
            if (takes.Length != 1) throw new InvalidOperationException("Expected exactly one exported Action/take.");
            var take = takes[0];
            if(sourceAvatar!=null)
            {
                var initialModel=AssetDatabase.LoadAssetAtPath<GameObject>(asset);
                var names=initialModel.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray();
                var missing=sourceAvatar.humanDescription.human.Select(b=>b.boneName).Where(n=>!string.IsNullOrEmpty(n)&&!names.Contains(n)).Distinct().ToArray();
                if(missing.Length>0)throw new ArgumentException("Copied Avatar requires the same source skeleton. Missing bones: "+string.Join(",",missing)+". Map/create the source Avatar instead of copying a different target character rig.");
            }
            float exportedSamples = take.lastFrame - take.firstFrame;
            if (Mathf.Abs(exportedSamples - (m.frame_end-m.frame_start)) > 0.1f)
                throw new InvalidOperationException("FBX take range differs from source range; inspect before retiming.");
            importer.animationType = m.rig_type == "Humanoid" ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
            importer.avatarSetup = sourceAvatar != null ? ModelImporterAvatarSetup.CopyFromOther : ModelImporterAvatarSetup.CreateFromThisModel;
            if (sourceAvatar != null) importer.sourceAvatar = sourceAvatar;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = true;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = false;
            importer.optimizeGameObjects = false;
            importer.isReadable = true; // staging measurement; set false after gameplay verification if unnecessary
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            take.name = m.unity_clip;
            take.loopTime = m.loop; take.loopPose = m.loop;
            take.keepOriginalOrientation = true; take.keepOriginalPositionY = true; take.keepOriginalPositionXZ = true;
            bool inPlace = m.root_motion == "in_place";
            take.lockRootRotation = inPlace; take.lockRootHeightY = inPlace; take.lockRootPositionXZ = inPlace;
            take.events = new AnimationEvent[0]; // timing metadata needs an explicitly verified receiver before event wiring
            importer.clipAnimations = new[]{take};
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(asset);
            if (model == null) throw new InvalidOperationException("Imported model is missing.");
            var roots = model.GetComponentsInChildren<Transform>(true).Where(t => t.name == m.root_bone).ToArray();
            if (roots.Length != 1) throw new InvalidOperationException("Root bone is missing or ambiguous in the imported hierarchy.");
            if (m.rig_type == "Generic" && !inPlace)
            {
                importer.motionNodeName = AnimationUtility.CalculateTransformPath(roots[0], model.transform);
                importer.SaveAndReimport();
                model = AssetDatabase.LoadAssetAtPath<GameObject>(asset);
            }
            var clips = AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != 1 || clips[0].name != m.unity_clip) throw new InvalidOperationException("Unexpected extra or renamed animation clips: "+string.Join(",",clips.Select(c=>c.name))+"; expected "+m.unity_clip);
            var clip = clips[0];
            if (Mathf.Abs(clip.frameRate-m.fps) > 0.01f || Mathf.Abs(clip.length-m.duration_seconds) > 0.01f || clip.isLooping != m.loop)
                throw new InvalidOperationException("Imported clip FPS, duration or looping disagrees with export metadata.");
            var avatar = sourceAvatar ?? AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || (m.rig_type == "Humanoid" && !avatar.isHuman))
                throw new InvalidOperationException("Unity Avatar is invalid. Map the intended rig before production integration.");
            return new {asset, m.action, m.unity_clip, clip.frameRate, clip.length, clip.isLooping,
                firstFrame=take.firstFrame,lastFrame=take.lastFrame,sourceFrameOffset=m.frame_start-take.firstFrame,
                rigType=importer.animationType.ToString(),avatarValid=avatar.isValid,avatarHuman=avatar.isHuman,
                importer.globalScale,importer.fileScale,importer.bakeAxisConversion, importer.motionNodeName,
                rootPath=AnimationUtility.CalculateTransformPath(roots[0],model.transform),
                curveCount=AnimationUtility.GetCurveBindings(clip).Length,
                animatedPaths=AnimationUtility.GetCurveBindings(clip).Select(b=>b.path).Distinct().ToArray(),
                warning="Staged only. Gameplay camera, contacts, runtime motion ownership and Humanoid retargeting need action-specific review."};
        }
        catch
        {
            Cleanup(folder);
            throw;
        }
    }

    public static bool Cleanup(string folder)
    {
        RequireEditMode(); ValidateFolder(folder);
        string marker=folder + "/.animation-pipeline-owned";
        if (!File.Exists(marker) || File.ReadAllText(marker) != "animation-pipeline-v1\n" + folder)
            throw new InvalidOperationException("Cleanup refuses folders not owned by this staging helper.");
        return AssetDatabase.DeleteAsset(folder);
    }

    public static object VerifyFixture(string fbx,string metadata,string folder)
    {
        var import=Stage(fbx,metadata,folder);
        var scene=EditorSceneManager.NewPreviewScene();
        GameObject instance=null; Mesh mesh=null;
        try
        {
            string asset=folder+"/"+Path.GetFileName(fbx);
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(asset);
            var clip=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            instance=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);
            var animator=instance.GetComponent<Animator>(); if(animator!=null)animator.enabled=false;
            var tip=instance.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="tip");
            var root=instance.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="root");
            clip.SampleAnimation(instance,0); Quaternion start=tip.localRotation;
            float boneLength=Vector3.Distance(tip.position,root.position);
            float up=Vector3.Dot((tip.position-root.position).normalized,Vector3.up);
            clip.SampleAnimation(instance,23f/24f); float angle=Quaternion.Angle(start,tip.localRotation);
            var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();
            mesh=new Mesh();skin.BakeMesh(mesh);
            int uniqueVertices=mesh.vertices.Distinct().Count(); // FBX splits hard normal/UV seams
            if(Mathf.Abs(boneLength-1)>0.01f || up<0.99f || angle<15 || angle>25 || uniqueVertices!=8)
                throw new InvalidOperationException($"Fixture measurements: boneLength={boneLength}, up={up}, poseAngle={angle}, vertices={mesh.vertexCount}");
            return new{passed=true,import,boneLength,up,poseAngleDegrees=angle,skinnedVertices=mesh.vertexCount,uniqueVertices,
                cleanup="Temporary staging revision deleted by AssetDatabase in finally"};
        }
        finally
        {
            if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);
            if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);
            EditorSceneManager.ClosePreviewScene(scene);
            Cleanup(folder);
        }
    }

    public static object VerifyExistingHumanoid(string sourceModelAsset,string reportManifest,string folder,string sourceAvatarPath)
    {
        RequireEditMode();ValidateFolder(folder);
        var importer=AssetImporter.GetAtPath(sourceModelAsset) as ModelImporter;
        if(importer==null || importer.animationType!=ModelImporterAnimationType.Human || importer.defaultClipAnimations.Length!=1)
            throw new ArgumentException("Choose an existing single-take Humanoid source to inspect and stage a copy.");
        var sourceClip=AssetDatabase.LoadAllAssetsAtPath(sourceModelAsset).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        var take=importer.defaultClipAnimations[0];
        var manifest=new Manifest{action=sourceClip.name,unity_clip="TEST_Humanoid_Existing_001",revision=Path.GetFileName(folder),
            frame_start=Mathf.RoundToInt(take.firstFrame),frame_end=Mathf.RoundToInt(take.lastFrame),fps=sourceClip.frameRate,
            loop=false,root_motion="in_place",root_bone="mixamorig:Hips",rig_type="Humanoid",stage="BLOCKING",
            design_status="approval_waived",design_reference="Isolated import test of an existing source; no new animation",
            approval_reference="User requested non-destructive workflow connectivity/import verification",
            review_status="accepted_for_stage",review_reference="Technical import fixture only; no artistic approval",
            fbx=Path.GetFileName(sourceModelAsset)};
        var sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>(sourceModelAsset);
        var hips=sourceModel.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
        if(hips==null)throw new InvalidOperationException("Existing source has no mapped Humanoid hips.");
        manifest.root_bone=hips.name;
        manifest.duration_seconds=(manifest.frame_end-manifest.frame_start)/manifest.fps;
        using(var stream=new StreamWriter(new FileStream(reportManifest,FileMode.CreateNew)))stream.Write(JsonUtility.ToJson(manifest,true));
        bool created=false;
        try
        {
            var result=Stage(sourceModelAsset,reportManifest,folder,sourceAvatarPath);created=true;
            return new{passed=true,import=result,scope="Existing Humanoid source copy with an explicit compatible source Avatar; retargeted gameplay quality remains action-specific."};
        }
        finally{if(created)Cleanup(folder);}
    }
}

public static class StageV009
{
    [Serializable] public class IndexEntry {public string action,avatar;}
    public static object StageAll()
    {
        var reports=new System.Collections.Generic.List<object>();
        const string root="ArtSource/Encounter/v009";
        foreach(string path in Directory.GetFiles(root,"*-index.json"))
        {
            // JsonUtility does not accept a top-level array.
            var index=Newtonsoft.Json.JsonConvert.DeserializeObject<IndexEntry[]>(File.ReadAllText(path));
            foreach(var m in index)
            {
                string folder="Assets/AnimationStaging/"+m.action;
                if(AssetDatabase.IsValidFolder(folder))continue;
                reports.Add(UnityAnimationImport.Stage(root+"/"+m.action+".fbx",root+"/"+m.action+".animation.json",folder,m.avatar));
            }
        }
        Directory.CreateDirectory(".codex/encounter-v009");
        File.WriteAllText(".codex/encounter-v009/import-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
        return new {staged=reports.Count,isolated=true};
    }
    [Serializable] public class Index {public IndexEntry[] entries;}
}



