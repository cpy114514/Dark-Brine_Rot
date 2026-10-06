using UnityEngine;

/// <summary>A real hull fragment, driven by rigidbody physics and surf controls.</summary>
[RequireComponent(typeof(BoxCollider),typeof(Story1WreckFloatBody))]
public sealed class Story1WreckPlank : MonoBehaviour
{
    public BoxCollider Deck { get; private set; }
    public Vector3 Dimensions => size;
    public Vector3 Velocity => floating!=null ? floating.Body.linearVelocity : Vector3.zero;
    public float Speed => Vector3.ProjectOnPlane(Velocity,Vector3.up).magnitude;
    public Vector3 ShipSourcePosition { get; private set; }
    public float TopLocalY => transform.up.y>=0 ? .5f : -.5f;
    public bool IsBoardable => Deck!=null && Deck.enabled && Mathf.Abs(transform.up.y)>.65f;
    Story1WreckFloatBody floating;
    Vector3 size;
    float throttle,steer;
    Transform rider;
    Matrix4x4 previousPose;

    public void Initialize(Vector3 from,Quaternion rotation,Vector3 dimensions,OceanWorld ocean,Vector3 velocity,Vector3 spin)
    {
        ShipSourcePosition=from;size=dimensions;
        transform.localScale=dimensions;transform.SetPositionAndRotation(from,rotation);
        Deck=GetComponent<BoxCollider>();Deck.size=Vector3.one;Deck.center=Vector3.zero;Deck.enabled=true;
        floating=GetComponent<Story1WreckFloatBody>();floating.Initialize(ocean,dimensions,true,velocity,spin);
        previousPose=transform.localToWorldMatrix;
    }
    public void Drive(float forward,float turn,Transform occupant)
    {
        throttle=Mathf.Clamp(forward,-1,1);steer=Mathf.Clamp(turn,-1,1);rider=occupant;
    }
    void FixedUpdate()
    {
        if(rider==null || floating==null || !floating.InWater)return;
        var body=floating.Body;
        Vector3 forward=Vector3.ProjectOnPlane(body.rotation*Vector3.forward,Vector3.up).normalized;
        float wanted=throttle>=0 ? throttle*10 : throttle*4.5f;
        float current=Vector3.Dot(body.linearVelocity,forward);
        body.AddForce(forward*Mathf.Clamp((wanted-current)*3,-7,7),ForceMode.Acceleration);
        float turn=steer*65*Mathf.Deg2Rad;
        body.AddTorque(Vector3.up*((turn-body.angularVelocity.y)*4),ForceMode.Acceleration);
    }
    public Vector3 BoardingPoint(Vector3 from)
    {
        Vector3 local=LocalSurfacePoint(from);
        float insetX=Mathf.Min(.4f,.8f/size.x),insetZ=Mathf.Min(.4f,.8f/size.z);
        local.x=Mathf.Clamp(local.x,-.5f+insetX,.5f-insetX);
        local.z=Mathf.Clamp(local.z,-.5f+insetZ,.5f-insetZ);local.y=TopLocalY;
        return transform.TransformPoint(local);
    }
    public float EdgeDistance(Vector3 from)
    {
        Vector3 local=LocalSurfacePoint(from);
        local.x=Mathf.Clamp(local.x,-.5f,.5f);local.z=Mathf.Clamp(local.z,-.5f,.5f);local.y=TopLocalY;
        return Vector3.ProjectOnPlane(transform.TransformPoint(local)-from,Vector3.up).magnitude;
    }
    Vector3 LocalSurfacePoint(Vector3 from)
    {
        Vector3 top=transform.TransformPoint(new Vector3(0,TopLocalY,0));
        Vector3 normal=transform.up;
        if(Mathf.Abs(normal.y)>.2f)
            from.y=top.y-(normal.x*(from.x-top.x)+normal.z*(from.z-top.z))/normal.y;
        return transform.InverseTransformPoint(from);
    }
    public bool Supports(Vector3 feet)
    {
        if(!IsBoardable)return false;
        Vector3 local=transform.InverseTransformPoint(feet);
        return Mathf.Abs(local.x)<.49f && Mathf.Abs(local.z)<.49f && Mathf.Abs((local.y-TopLocalY)*size.y)<.55f;
    }
    public bool SupportedPreviousPose(Vector3 feet)
    {
        float up=previousPose.GetColumn(1).y/size.y;
        if(Deck==null || !Deck.enabled || Mathf.Abs(up)<.65f)return false;
        Vector3 local=previousPose.inverse.MultiplyPoint3x4(feet);
        float top=up>=0 ? .5f : -.5f;
        return Mathf.Abs(local.x)<.49f && Mathf.Abs(local.z)<.49f && Mathf.Abs((local.y-top)*size.y)<.55f;
    }
    public Vector3 CarryDisplacement(Vector3 feet) => transform.TransformPoint(previousPose.inverse.MultiplyPoint3x4(feet))-feet;
    public void CommitPose() => previousPose=transform.localToWorldMatrix;
}
