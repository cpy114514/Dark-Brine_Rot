using System;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class CalibrateSlashWristV033
{
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    public static object Inspect()
    {
        if(!Application.isPlaying)throw new Exception("Native Play required");
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),Vector3.up*500,Quaternion.identity);
        if(actor.GetComponent<GameSaveExcluded>()==null)actor.AddComponent<GameSaveExcluded>();
        foreach(var b in actor.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
        actor.GetComponent<CharacterController>().enabled=false;
        var animator=actor.GetComponent<ThirdPersonPlayerController>().CharacterAnimator;
        animator.speed=0;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var graph=PlayableGraph.Create("Actual retained right-hand weapon orientation audit");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/AnimationStaging/CH1_SahurSlash_v032/CH1_SahurSlash_v032.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        var playable=AnimationClipPlayable.Create(graph,clip);playable.SetSpeed(0);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(playable);graph.Play();
        try
        {
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var weapon=actor.GetComponent<SahurAttack>().stickHitbox as CapsuleCollider;
            var rows=new System.Collections.Generic.List<object>();
            foreach(float time in new[]{.5f,.55f,.60f,.63f,.66f,.7f,.8f})
            {
                playable.SetTime(time);graph.Evaluate(0);
                Story1CapsuleContact.Segment(weapon,out var a,out var b,out _);
                Vector3 current=((a-hand.position).sqrMagnitude>(b-hand.position).sqrMagnitude ? a : b)-hand.position;
                Vector3 desired=new Vector3(0,-.45f,1).normalized;
                Quaternion correction=Quaternion.FromToRotation(current.normalized,desired),local=Quaternion.Inverse(hand.rotation)*correction*hand.rotation;
                rows.Add(new{time,hand=V(hand.position-actor.transform.position),shaftDirection=V(current.normalized),localCorrection=new[]{local.x,local.y,local.z,local.w},localCorrectionEuler=V(local.eulerAngles),requiredTurn=Quaternion.Angle(Quaternion.identity,correction)});
            }
            Directory.CreateDirectory(".codex/encounter-v033");File.WriteAllText(".codex/encounter-v033/native-wrist-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
        }
        finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(actor);}
    }
}
