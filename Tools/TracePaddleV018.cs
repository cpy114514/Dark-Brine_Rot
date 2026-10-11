using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(2700)]
public sealed class V018PaddleTrace:MonoBehaviour
{
    public string file;readonly List<object> rows=new List<object>();float next;
    void LateUpdate()
    {
        if(Time.time<next)return;next=Time.time+.06f;
        var hero=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();if(hero==null)return;
        var rider=hero.GetComponent<Story1WreckRider>();if(rider==null || !rider.IsSurfing)return;
        var board=rider.DrivenBoard;var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();var a=hero.CharacterAnimator;
        var lf=a.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=a.GetBoneTransform(HumanBodyBones.RightFoot);
        var left=lf.position;var right=rf.position;
        var pose=hero.GetComponent<SahurMovementPose>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var leftSole=lf.TransformPoint((Vector3)typeof(SahurMovementPose).GetField("leftSole",flags).GetValue(pose));
        var rightSole=rf.TransformPoint((Vector3)typeof(SahurMovementPose).GetField("rightSole",flags).GetValue(pose));
        Vector3 normal=board.transform.up;if(normal.y<0)normal=-normal;
        bool onLeft=board.Collision.Raycast(new Ray(leftSole+normal*2,-normal),out var leftHit,4);
        bool onRight=board.Collision.Raycast(new Ray(rightSole+normal*2,-normal),out var rightHit,4);
        rows.Add(new{time=Time.time,phase=rider.PaddlePhase,paddling=rider.IsPaddling,throttle=a.GetFloat("BoardThrottle"),waterEngagement=pose.PaddleWaterEngagement,
            board=board.name,dimensions=board.Dimensions.ToString(),speed=board.Speed,forward=Vector3.Dot(hero.transform.forward,board.SurfForward),
            root=hero.transform.position.ToString(),rightAxis=hero.transform.right.ToString(),
            leftSoleLocal=hero.transform.InverseTransformVector(leftSole-hero.transform.position).ToString(),rightSoleLocal=hero.transform.InverseTransformVector(rightSole-hero.transform.position).ToString(),
            left=left.ToString(),right=right.ToString(),leftAnkleToDeck=left.y-board.BoardingPoint(left).y,rightAnkleToWater=right.y-water.SampleSurfaceHeight(right,Time.time),
            rightEdgeDistance=board.EdgeDistance(right),leftSole=leftSole.ToString(),rightSole=rightSole.ToString(),
            leftSoleGap=onLeft ? Vector3.Distance(leftSole,leftHit.point) : -1,
            leftSoleEdgeDistance=board.EdgeDistance(leftSole),leftSoleClosestDistance=Vector3.Distance(leftSole,board.Collision.ClosestPoint(leftSole)),
            rightSoleToWater=rightSole.y-water.SampleSurfaceHeight(rightSole,Time.time),
            rightSoleEdgeDistance=board.EdgeDistance(rightSole),rightSoleOverDeck=onRight,
            state=a.GetCurrentAnimatorStateInfo(0).fullPathHash});
        File.WriteAllText(file,Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
    }
}
public static class TracePaddleV018
{
    public static object Start(string name)
    {
        if(!Application.isPlaying)throw new Exception("Play required");Stop();
        Directory.CreateDirectory(".codex/encounter-v018");var go=new GameObject("Read-only native paddle review");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<V018PaddleTrace>().file=Path.GetFullPath(".codex/encounter-v018/"+name+"-foot-trace.json");return "Paddle trace armed";
    }
    public static object Stop()
    {
        foreach(var trace in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(t=>t.GetType().Name=="V018PaddleTrace").ToArray())UnityEngine.Object.DestroyImmediate(trace.gameObject);
        return "Paddle trace stopped";
    }
}
