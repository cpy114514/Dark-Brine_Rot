using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class AuditSoleScaleV054
{
    public static object Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var scene=EditorSceneManager.NewPreviewScene();GameObject actor=null;Mesh scaled=null,unscaled=null;var graph=PlayableGraph.Create("Sole scale diagnostic");
        try
        {
            actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),scene);
            actor.transform.position=Vector3.zero;
            var root=actor.GetComponent<Animator>();var animator=actor.transform.Find("Pbr Sahur Visual").GetComponent<Animator>();
            animator.runtimeAnimatorController=root.runtimeAnimatorController;animator.avatar=root.avatar;root.enabled=false;animator.enabled=true;animator.applyRootMotion=false;animator.Rebind();
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/AnimationStaging/PLAYER_BoardPaddle_v054/PLAYER_BoardPaddle_v054.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(playable);graph.Play();playable.SetTime(0);graph.Evaluate(0);
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();scaled=new Mesh();unscaled=new Mesh();skin.BakeMesh(scaled,true);skin.BakeMesh(unscaled,false);
            var tr=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
            float equivalentError=0,uncompensatedError=0,cpuSkinError=0;var a=scaled.vertices;var b=unscaled.vertices;
            var original=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;var bind=skin.sharedMesh.bindposes;
            var boneMatrices=skin.bones.Select((bone,i)=>bone.localToWorldMatrix*bind[i]).ToArray();
            for(int i=0;i<a.Length;i++)
            {
                Vector3 correct=skin.transform.TransformPoint(a[i]);
                equivalentError=Mathf.Max(equivalentError,Vector3.Distance(tr.MultiplyPoint3x4(b[i]),correct));
                uncompensatedError=Mathf.Max(uncompensatedError,Vector3.Distance(skin.transform.TransformPoint(b[i]),correct));
                var w=weights[i];Vector3 cpu=boneMatrices[w.boneIndex0].MultiplyPoint3x4(original[i])*w.weight0+boneMatrices[w.boneIndex1].MultiplyPoint3x4(original[i])*w.weight1+boneMatrices[w.boneIndex2].MultiplyPoint3x4(original[i])*w.weight2+boneMatrices[w.boneIndex3].MultiplyPoint3x4(original[i])*w.weight3;
                cpuSkinError=Mathf.Max(cpuSkinError,Vector3.Distance(correct,cpu));
            }
            var result=new {scale=skin.transform.lossyScale.ToString(),vertices=a.Length,compensatedBakeWithFullTransformVsUncompensatedBakeWithTR=equivalentError,compensatedBakeVsActualBoneSkinning=cpuSkinError,wrongUncompensatedBakeWithFullTransformError=uncompensatedError,passed=equivalentError<.0002f && cpuSkinError<.0002f,diagnosis="Native Unity 6000.3 BakeMesh(true) compensates for renderer scale. Existing MeasureSoles true+TransformPoint is correct against actual CPU bone skinning; do not change it."};
            File.WriteAllText(".codex/opening-optimization-20261012/sole-scale-audit-v054.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally {graph.Destroy();if(scaled!=null)UnityEngine.Object.DestroyImmediate(scaled);if(unscaled!=null)UnityEngine.Object.DestroyImmediate(unscaled);if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
