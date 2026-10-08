using UnityEngine;
/// <summary>Closest real surface markers on two posed capsules, including disabled actor hurtboxes.</summary>
public static class Story1CapsuleContact
{
    public static void Segment(CapsuleCollider capsule,out Vector3 a,out Vector3 b,out float radius)
    {
        Vector3 axis=capsule.direction==0 ? Vector3.right : capsule.direction==1 ? Vector3.up : Vector3.forward;
        Vector3 scale=capsule.transform.lossyScale;
        radius=capsule.radius*(capsule.direction==0 ? Mathf.Max(Mathf.Abs(scale.y),Mathf.Abs(scale.z)) : capsule.direction==1 ? Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)) : Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));
        float axialScale=Mathf.Abs(capsule.direction==0 ? scale.x : capsule.direction==1 ? scale.y : scale.z);
        // Unity scales the rounded caps by the radial axes. Subtracting the
        // local radius before axial scaling stretches the straight segment
        // incorrectly on a nonuniformly scaled hurtbox.
        float half=Mathf.Max(0,capsule.height*axialScale*.5f-radius);
        Vector3 centre=capsule.transform.TransformPoint(capsule.center);
        Vector3 extent=capsule.transform.TransformDirection(axis).normalized*half;
        a=centre-extent;b=centre+extent;
    }
    public static float Gap(CapsuleCollider first,CapsuleCollider second,out Vector3 contact)
    {
        Segment(first,out var a,out var b,out float ra);Segment(second,out var c,out var d,out float rb);
        return Gap(a,b,ra,c,d,rb,out contact);
    }
    public static float PointGap(Vector3 point,CapsuleCollider body,out Vector3 contact)
    {
        Segment(body,out var c,out var d,out float radius);return Gap(point,point,0,c,d,radius,out contact);
    }
    static float Gap(Vector3 p,Vector3 q,float ra,Vector3 r,Vector3 u,float rb,out Vector3 contact)
    {
        Vector3 a=q-p,b=u-r,w=p-r;float aa=Vector3.Dot(a,a),bb=Vector3.Dot(b,b),ab=Vector3.Dot(a,b),aw=Vector3.Dot(a,w),bw=Vector3.Dot(b,w),s,t;
        if(aa<.000001f){s=0;t=bb>.000001f ? Mathf.Clamp01(bw/bb) : 0;}
        else if(bb<.000001f){t=0;s=Mathf.Clamp01(-aw/aa);}
        else
        {
            float denominator=aa*bb-ab*ab;s=denominator>.000001f ? Mathf.Clamp01((ab*bw-aw*bb)/denominator) : 0;
            t=(ab*s+bw)/bb;if(t<0){t=0;s=Mathf.Clamp01(-aw/aa);}else if(t>1){t=1;s=Mathf.Clamp01((ab-aw)/aa);}
        }
        Vector3 x=p+a*s,y=r+b*t,along=(y-x).normalized;contact=(x+along*ra+y-along*rb)*.5f;
        return Mathf.Max(0,Vector3.Distance(x,y)-ra-rb);
    }
}
