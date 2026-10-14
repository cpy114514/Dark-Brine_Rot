using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class AuditThrowFacingV064
{
    public static object Inspect(string clipName="PLAYER_Throw_v006")
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required for isolated rig audit");
        GameObject holder=null;PlayableGraph graph=default;
        try
        {
            holder=new GameObject("Isolated throw facing audit");holder.hideFlags=HideFlags.HideAndDontSave;
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),holder.transform);
            player.transform.SetPositionAndRotation(new Vector3(2000,500,2000),Quaternion.identity);
            var animator=player.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=null;
            graph=PlayableGraph.Create("Isolated native throw pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output=AnimationPlayableOutput.Create(graph,"Pose",animator);
            var guard=Story1EncounterClipCache.Find("CH1_SahurGuard_v050");var clip=Story1EncounterClipCache.Find(clipName);
            if(guard==null || clip==null)throw new Exception("Native reference clip missing");
            var reference=AnimationClipPlayable.Create(graph,guard);reference.SetSpeed(0);output.SetSourcePlayable(reference);graph.Play();reference.SetTime(.2f);graph.Evaluate(0);
            var head=animator.GetBoneTransform(HumanBodyBones.Head);var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
            Vector3 headAxis=Quaternion.Inverse(head.rotation)*player.transform.forward,hipsAxis=Quaternion.Inverse(hips.rotation)*player.transform.forward;
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);output.SetSourcePlayable(pose);
            var rows=new System.Collections.Generic.List<object>();
            for(int i=0;i<=12;i++)
            {
                float seconds=clip.length*i/12;pose.SetTime(Mathf.Min(seconds,clip.length-.0001f));graph.Evaluate(0);
                Vector3 face=Vector3.ProjectOnPlane(head.rotation*headAxis,Vector3.up).normalized;
                Vector3 body=Vector3.ProjectOnPlane(hips.rotation*hipsAxis,Vector3.up).normalized;
                rows.Add(new{seconds,headYaw=Vector3.SignedAngle(player.transform.forward,face,Vector3.up),
                    hipsYaw=Vector3.SignedAngle(player.transform.forward,body,Vector3.up),
                    animatorEuler=animator.transform.localEulerAngles.ToString(),head=head.position.ToString()});
            }
            string path=AssetDatabase.GetAssetPath(clip);var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            var result=new{clipName,path,clip.length,clip.frameRate,avatar=animator.avatar.name,
                importClips=importer==null ? null : importer.clipAnimations.Select(c=>new{c.name,c.lockRootRotation,c.keepOriginalOrientation,c.rotationOffset}).ToArray(),rows};
            File.WriteAllText("D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/throw-facing-"+clipName+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally{if(graph.IsValid())graph.Destroy();if(holder!=null)UnityEngine.Object.DestroyImmediate(holder);}
    }
}
