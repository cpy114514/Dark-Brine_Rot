using UnityEngine;
/// <summary>Bounded two-bone correction of an authored stance onto its actual deck.</summary>
public static class SahurSupportPose
{
    public static float Anchor(Animator animator,bool left,Vector3 sole,Vector3 target,float limit=.35f)
    {
        var hip=animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
        var knee=animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
        var foot=animator.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
        Vector3 delta=Vector3.ClampMagnitude(target-foot.TransformPoint(sole),limit);Quaternion rotation=foot.rotation;
        if(delta.sqrMagnitude<.000001f)return Vector3.Distance(foot.TransformPoint(sole),target);
        PlaceSole(hip,knee,foot,sole,foot.TransformPoint(sole)+delta,animator.transform.forward,rotation);
        return Vector3.Distance(foot.TransformPoint(sole),target);
    }
    public static void Hand(Animator animator,bool left,Vector3 grip,float weight)
    {
        var hip=animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
        var knee=animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        var end=animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        if(hip==null || knee==null || end==null)return;
        Vector3 target=Vector3.Lerp(end.position,grip,weight);
        Vector3 delta=target-end.position;if(delta.magnitude>.65f)target=end.position+delta.normalized*.65f;
        Solve(hip,knee,end,target,animator.transform.forward);
    }
    public static float Plant(Animator animator,Story1WreckPlank board,Vector3 leftSole,Vector3 rightSole,float limit=.35f)
    {
        float a=Foot(animator,board,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,leftSole,limit);
        float b=Foot(animator,board,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,rightSole,limit);
        // Walking and a stagger may deliberately unload one leg. Report the supporting sole.
        return Mathf.Min(a,b);
    }
    static float Foot(Animator animator,Story1WreckPlank board,HumanBodyBones thigh,HumanBodyBones shin,HumanBodyBones foot,Vector3 sole,float limit)
    {
        Transform hip=animator.GetBoneTransform(thigh),knee=animator.GetBoneTransform(shin),ankle=animator.GetBoneTransform(foot);
        Vector3 soleAt=ankle.TransformPoint(sole),normal=board.transform.up;
        if(normal.y<0)normal=-normal;
        float reach=Mathf.Max(2,board.Dimensions.y+1);
        if(!board.Collision.Raycast(new Ray(soleAt+normal*reach,-normal),out var hit,reach*2))
        {
            // A authored footstep may straddle a jagged timber edge. Move the
            // supporting foot onto that actual nearby surface within the same
            // bounded IK reach, rather than treating the empty ray as a deck.
            Vector3 edge=board.Collision.ClosestPoint(soleAt);
            if(Vector3.Distance(edge,soleAt)>limit ||
                !board.Collision.Raycast(new Ray(edge+normal*reach,-normal),out hit,reach*2) ||
                Vector3.Dot(hit.normal,normal)<.5f || Vector3.Distance(hit.point,soleAt)>limit)return float.PositiveInfinity;
        }
        Vector3 surface=hit.point;Quaternion footRotation=ankle.rotation;
        Vector3 delta=surface-soleAt;if(delta.magnitude>limit)delta=delta.normalized*limit;
        if(delta.sqrMagnitude<.000001f)return Vector3.Distance(soleAt,surface);
        PlaceSole(hip,knee,ankle,sole,soleAt+delta,animator.transform.forward,footRotation);
        return Vector3.Distance(ankle.TransformPoint(sole),surface);
    }
    static void PlaceSole(Transform hip,Transform knee,Transform ankle,Vector3 sole,Vector3 target,Vector3 forward,Quaternion rotation)
    {
        // Retargeted hierarchies can contain scaled parent transforms. Restoring
        // the authored foot rotation changes its sole offset after the first
        // joint solve. Iterate towards the SAME bounded world target, never
        // extend the original correction limit with each iteration.
        for(int iteration=0;iteration<4;iteration++)
        {
            Vector3 correction=target-ankle.TransformPoint(sole);
            if(correction.sqrMagnitude<.000001f)break;
            Solve(hip,knee,ankle,ankle.position+correction,forward);ankle.rotation=rotation;
        }
    }
    static void Solve(Transform hip,Transform knee,Transform ankle,Vector3 target,Vector3 forward)
    {
        Vector3 h=hip.position;
        float upper=Vector3.Distance(h,knee.position),lower=Vector3.Distance(knee.position,ankle.position);
        float distance=Mathf.Clamp(Vector3.Distance(h,target),Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
        Vector3 axis=(target-h).normalized;
        Vector3 bend=Vector3.ProjectOnPlane(knee.position-h,axis).normalized;
        if(bend.sqrMagnitude<.1f)bend=Vector3.ProjectOnPlane(forward,axis).normalized;
        float along=(upper*upper+distance*distance-lower*lower)/(2*distance);
        Vector3 desiredKnee=h+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        hip.rotation=Quaternion.FromToRotation(knee.position-h,desiredKnee-h)*hip.rotation;
        knee.rotation=Quaternion.FromToRotation(ankle.position-knee.position,target-knee.position)*knee.rotation;
    }
}
