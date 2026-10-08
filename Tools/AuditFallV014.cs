using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using Mavis;
public static class AuditFallV014
{
    public static async Task<object> Verify()
    {
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),Vector3.up*500,Quaternion.identity);
        actor.AddComponent<GameSaveExcluded>();
        foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
        actor.GetComponent<CharacterController>().enabled=false;
        var animator=actor.GetComponent<ThirdPersonPlayerController>().CharacterAnimator;animator.applyRootMotion=false;animator.speed=0;
        var graph=PlayableGraph.Create("Isolated actual fall audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        try
        {
            var clip=Resources.LoadAll<AnimationClip>("Encounter/v006/PLAYER_Fall_v006/PLAYER_Fall_v006").First(c=>!c.name.StartsWith("__preview__"));
            var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);AnimationPlayableOutput.Create(graph,"audit",animator).SetSourcePlayable(pose);graph.Play();
            var rows=new System.Collections.Generic.List<object>();
            foreach(float t in new[]{0f,.5f,1f,1.5f,2f,2.2f,2.24999f})
            {
                pose.SetTime(t);graph.Evaluate(0);await Task.Delay(25);graph.Evaluate(0);
                var hip=animator.GetBoneTransform(HumanBodyBones.Hips);var head=animator.GetBoneTransform(HumanBodyBones.Head);
                rows.Add(new {time=t,vertical=Vector3.Dot((head.position-hip.position).normalized,Vector3.up)});
            }
            return rows;
        }
        finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(actor);}
    }
}
