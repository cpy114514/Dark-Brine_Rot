using System.Linq;
using UnityEngine;
public static class AuditOpeningDeckV216
{
    public static object Inspect()
    {
        var seq=Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        var hero=seq.sahur;var animator=hero.GetComponentInChildren<Animator>();
        Vector3 left=Vector3.zero,right=Vector3.zero;
        if(animator!=null&&animator.isHuman)SahurSupportPose.MeasureSoles(animator,ref left,ref right);
        Vector3 sole=animator.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(left);
        Physics.SyncTransforms();
        return new{playing=Application.isPlaying,hero=hero.transform.position.ToString("F3"),sole=sole.ToString("F3"),footOffset=hero.LowestFootWorldOffset,
            sharkScale=seq.swimmer.localScale.ToString("F3"),sharkWorld=seq.swimmer.lossyScale.ToString("F3"),
            surfaces=seq.ship.GetComponentsInChildren<Collider>().Select(c=>{
                bool hit=c.Raycast(new Ray(sole+Vector3.up*30,Vector3.down),out var h,60);
                return new{name=c.name,hit,y=hit?h.point.y:0,normal=hit?h.normal.ToString():"",gap=hit?sole.y-h.point.y:999,bounds=c.bounds.ToString()};}).Where(x=>x.hit).ToArray()};
    }
}
