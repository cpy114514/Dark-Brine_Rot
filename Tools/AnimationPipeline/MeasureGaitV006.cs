using System;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
public static class MeasureGaitV006
{
    public static object Measure()
    {
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),Vector3.up*500,Quaternion.identity);
        actor.AddComponent<GameSaveExcluded>();var animator=actor.GetComponent<ThirdPersonPlayerController>().CharacterAnimator;
        foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
        actor.GetComponent<CharacterController>().enabled=false;animator.applyRootMotion=false;
        var graph=PlayableGraph.Create("Measure retargeted foot speeds");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var mixer=AnimationMixerPlayable.Create(graph,1);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(mixer);graph.Play();
        var reports=new System.Collections.Generic.List<object>();
        try
        {
            foreach(string name in new[]{"PLAYER_Walk_v006","PLAYER_Run_v006"})
            {
                var clip=Resources.LoadAll<AnimationClip>("Encounter/v006/"+name+"/"+name).Single(c=>!c.name.StartsWith("__preview__"));
                var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);mixer.ConnectInput(0,pose,0);mixer.SetInputWeight(0,1);
                var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Vector3[] a=new Vector3[121],b=new Vector3[121];float floor=float.PositiveInfinity;
                for(int i=0;i<=120;i++){pose.SetTime(clip.length*i/120);graph.Evaluate(0);a[i]=actor.transform.InverseTransformPoint(left.position)*actor.transform.lossyScale.x;b[i]=actor.transform.InverseTransformPoint(right.position)*actor.transform.lossyScale.x;floor=Mathf.Min(floor,a[i].y,b[i].y);}
                var values=new System.Collections.Generic.List<float>();
                for(int i=1;i<120;i++)foreach(var samples in new[]{a,b})
                {float speed=-(samples[i].z-samples[i-1].z)/(clip.length/120);if(samples[i].y-floor<.18f && speed>.3f)values.Add(speed);}
                var travel=new System.Collections.Generic.List<float>();
                for(int i=1;i<120;i++)foreach(var samples in new[]{a,b})
                {float speed=-(samples[i].z-samples[i-1].z)/(clip.length/120);if(speed>.3f)travel.Add(speed);}
                values.Sort();travel.Sort();reports.Add(new{name,stanceSpeed=values.Count>0 ? values[values.Count/2] : 0,samples=values.Count,cycleSeconds=clip.length,
                    backwardMedian=travel[travel.Count/2],backwardP75=travel[(int)(travel.Count*.75)],stepRange=a.Max(p=>p.z)-a.Min(p=>p.z),heightRange=a.Max(p=>p.y)-floor});
                mixer.DisconnectInput(0);graph.DestroyPlayable(pose);
            }
            Directory.CreateDirectory(".codex/encounter-v006");File.WriteAllText(".codex/encounter-v006/gait-measurements.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));return reports;
        }
        finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(actor);}
    }
}
