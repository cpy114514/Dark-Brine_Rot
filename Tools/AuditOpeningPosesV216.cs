using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
public static class AuditOpeningPosesV216
{
    public static object Inspect()
    {
        if(Application.isPlaying)throw new Exception("Edit mode only");
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var actor=UnityEngine.Object.Instantiate(asset);actor.hideFlags=HideFlags.HideAndDontSave;
        var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;
        actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var graph=PlayableGraph.Create("Pose audit isolated clone");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var output=AnimationPlayableOutput.Create(graph,"Pose",animator);var rows=new List<object>();
        try
        {
            foreach(var name in new[]{"CH1_SahurGuard_v050","PLAYER_Climb_v010","CH1_SahurOverhead_v215","CH1_SahurProneDodge_v215","CH1_SahurGetUp_v215"})
            {
                var clip=Story1EncounterClipCache.Find(name);if(clip==null){rows.Add(new{name,missing=true});continue;}
                var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);output.SetSourcePlayable(pose);graph.Play();
                foreach(float phase in new[]{0f,.25f,.5f,.75f,1f})
                {
                    pose.SetTime(phase*(clip.length-.0001f));graph.Evaluate(0);
                    Transform Bone(HumanBodyBones bone)=>animator.GetBoneTransform(bone);
                    Vector3 P(HumanBodyBones bone)=>actor.transform.InverseTransformPoint(Bone(bone).position);
                    rows.Add(new{name,phase,clip.length,head=P(HumanBodyBones.Head).ToString("F3"),hips=P(HumanBodyBones.Hips).ToString("F3"),
                        hand=P(HumanBodyBones.LeftHand).ToString("F3"),foot=P(HumanBodyBones.LeftFoot).ToString("F3"),
                        headForward=actor.transform.InverseTransformDirection(Bone(HumanBodyBones.Head).forward).ToString("F3"),
                        root=actor.transform.position.ToString("F3")});
                }
                graph.DestroyPlayable(pose);
            }
            return rows;
        }
        finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(actor);}
    }
}
