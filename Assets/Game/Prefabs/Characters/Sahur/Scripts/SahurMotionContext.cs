using UnityEngine;

public enum SahurControlState { Grounded,Airborne,Swimming,BoardRiding,Climbing,Cinematic,Unconscious }

/// <summary>One owner of scripted movement; ordinary locomotion keeps the existing controller.</summary>
[DisallowMultipleComponent]
public sealed class SahurMotionContext : MonoBehaviour
{
    public SahurControlState State { get; private set; }
    public MonoBehaviour Owner { get; private set; }
    public bool Scripted => Owner!=null;
    public bool Claim(MonoBehaviour owner,SahurControlState state)
    {
        if(owner==null)return false;
        if(Owner!=null && Owner!=owner && state!=SahurControlState.Cinematic && state!=SahurControlState.Unconscious)return false;
        Owner=owner;State=state;return true;
    }
    public void Release(MonoBehaviour owner){if(Owner==owner){Owner=null;State=SahurControlState.Airborne;}}
    public void Locomotion(bool swimming,bool grounded)
    {
        if(Owner!=null && (!Owner.enabled || !Owner.gameObject.activeInHierarchy))Owner=null;
        if(Owner==null)State=swimming ? SahurControlState.Swimming : grounded ? SahurControlState.Grounded : SahurControlState.Airborne;
    }
    public SahurMotionSnapshot Capture(ThirdPersonPlayerController actor)
    {
        var rider=actor.GetComponent<Story1WreckRider>();
        var board=rider!=null ? rider.DrivenBoard ?? rider.Support() : null;
        return new SahurMotionSnapshot {position=actor.transform.position,rotation=actor.transform.rotation,
            velocity=board!=null ? board.Velocity : actor.WorldVelocity,state=State,support=board,
            supportLocalPoint=board!=null ? board.transform.InverseTransformPoint(actor.transform.position) : Vector3.zero};
    }
}
[System.Serializable]
public struct SahurMotionSnapshot
{
    public Vector3 position,velocity;
    public Quaternion rotation;
    public SahurControlState state;
    public Story1WreckPlank support;
    public Vector3 supportLocalPoint;
}
