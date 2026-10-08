using UnityEngine;
using System.Collections.Generic;

/// <summary>A real hull fragment, driven by rigidbody physics and surf controls.</summary>
[RequireComponent(typeof(Story1WreckMeshCollision),typeof(Story1WreckFloatBody))]
public sealed class Story1WreckPlank : MonoBehaviour
{
    public Story1WreckMeshCollision Collision { get; private set; }
    public MeshCollider Deck => Collision!=null ? Collision.Primary : null;
    public Story1ShipWreckAsset.Fragment FractureSource { get; private set; }
    public float Integrity { get; private set; } = 100;
    public bool IsBroken { get; private set; }
    public float SharkContactAfter { get; set; }
    public Vector3 Dimensions => size;
    public Vector3 Velocity => floating!=null ? floating.Body.linearVelocity : Vector3.zero;
    public float Speed => Vector3.ProjectOnPlane(Velocity,Vector3.up).magnitude;
    public Vector3 ShipSourcePosition { get; private set; }
    public float TopLocalY => transform.up.y>=0 ? .5f : -.5f;
    public bool IsBoardable => !IsBroken && gameObject.activeInHierarchy && Deck!=null && Deck.enabled && Mathf.Abs(transform.up.y)>.65f;
    Story1WreckFloatBody floating;
    Vector3 size;
    float throttle,steer;
    Transform rider;
    Matrix4x4 previousPose;
    Vector2[] footprint;

    public void Initialize(Vector3 from,Quaternion rotation,Vector3 dimensions,OceanWorld ocean,Vector3 velocity,Vector3 spin)
    {
        ShipSourcePosition=from;size=dimensions;
        transform.localScale=dimensions;transform.SetPositionAndRotation(from,rotation);
        var mesh=GetComponent<MeshFilter>().sharedMesh;
        Collision=GetComponent<Story1WreckMeshCollision>();Collision.Initialize(mesh,FractureSource?.splinters);
        BuildFootprint(mesh);
        floating=GetComponent<Story1WreckFloatBody>();floating.Initialize(ocean,dimensions,true,velocity,spin);
        previousPose=transform.localToWorldMatrix;
    }
    public void SetFractureSource(Story1ShipWreckAsset.Fragment source)
    {
        FractureSource=source;
        if(Collision!=null)Collision.Initialize(source.mesh,source.splinters);
    }
    public bool Damage(float amount)
    {
        if(IsBroken || FractureSource==null || FractureSource.splinters==null || FractureSource.splinters.Length<2)return false;
        Integrity=Mathf.Max(0,Integrity-amount);return Integrity<=0;
    }
    public void MarkBroken()
    {
        IsBroken=true;Drive(0,0,null);Collision.SetEnabled(false);floating.enabled=false;
        floating.Body.isKinematic=true;gameObject.SetActive(false);
    }
    void BuildFootprint(Mesh mesh)
    {
        var points=new List<Vector2>();foreach(var vertex in mesh.vertices)points.Add(new Vector2(vertex.x*size.x,vertex.z*size.z));
        points.Sort((a,b)=>a.x!=b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        var unique=new List<Vector2>();foreach(var point in points)if(unique.Count==0 || (point-unique[unique.Count-1]).sqrMagnitude>.000001f)unique.Add(point);
        var hull=new List<Vector2>();
        for(int pass=0;pass<2;pass++)
        {
            int start=hull.Count;
            for(int n=0;n<unique.Count;n++)
            {
                var point=unique[pass==0 ? n : unique.Count-1-n];
                while(hull.Count>=start+2 && Cross(hull[hull.Count-1]-hull[hull.Count-2],point-hull[hull.Count-1])<=.00001f)hull.RemoveAt(hull.Count-1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count-1);
        }
        footprint=hull.ToArray();
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    bool Contains(Vector3 local,float margin=0)
    {
        var point=new Vector2(local.x*size.x,local.z*size.z);
        for(int i=0;i<footprint.Length;i++)
        {
            var edge=footprint[(i+1)%footprint.Length]-footprint[i];
            if(Cross(edge,point-footprint[i])<-margin*edge.magnitude)return false;
        }
        return true;
    }
    public bool ContainsFootprint(Vector3 world)
    {
        if(!Contains(transform.InverseTransformPoint(world),.12f))return false;
        Vector3 local=LocalSurfacePoint(world);local.y=TopLocalY;
        Vector3 normal=transform.up;if(normal.y<0)normal=-normal;
        float reach=Mathf.Max(2,size.y+1);
        return Collision.Active && Collision.Raycast(new Ray(transform.TransformPoint(local)+normal*reach,-normal),out var hit,reach*2);
    }
    public Vector3 OpenWaterDirection(Vector3 world)
    {
        Vector3 local=transform.InverseTransformPoint(world);
        Vector2 point=new Vector2(local.x*size.x,local.z*size.z),outward=Vector2.up;float best=float.PositiveInfinity;
        for(int i=0;i<footprint.Length;i++)
        {
            Vector2 edge=footprint[(i+1)%footprint.Length]-footprint[i];
            Vector2 at=footprint[i]+edge*Mathf.Clamp01(Vector2.Dot(point-footprint[i],edge)/Mathf.Max(.000001f,edge.sqrMagnitude));
            float gap=(point-at).sqrMagnitude;if(gap<best){best=gap;outward=new Vector2(edge.y,-edge.x).normalized;}
        }
        return Vector3.ProjectOnPlane(transform.TransformDirection(new Vector3(outward.x,0,outward.y)),Vector3.up).normalized;
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
        float turn=steer*90*Mathf.Deg2Rad;
        body.AddTorque(Vector3.up*((turn-body.angularVelocity.y)*4),ForceMode.Acceleration);
    }
    public Vector3 BoardingPoint(Vector3 from)
    {
        Vector3 local=LocalSurfacePoint(from);
        float insetX=Mathf.Min(.4f,.8f/size.x),insetZ=Mathf.Min(.4f,.8f/size.z);
        local.x=Mathf.Clamp(local.x,-.5f+insetX,.5f-insetX);
        local.z=Mathf.Clamp(local.z,-.5f+insetZ,.5f-insetZ);local.y=TopLocalY;
        Vector2 point=new Vector2(local.x*size.x,local.z*size.z);
        float inset=Mathf.Min(.65f,Mathf.Min(size.x,size.z)*.12f);
        for(int pass=0;pass<12;pass++)for(int i=0;i<footprint.Length;i++)
        {
            Vector2 edge=footprint[(i+1)%footprint.Length]-footprint[i];
            Vector2 inside=new Vector2(-edge.y,edge.x).normalized;
            float depth=Vector2.Dot(point-footprint[i],inside);
            if(depth<inset)point+=inside*(inset-depth);
        }
        local.x=point.x/size.x;local.z=point.y/size.z;
        Vector3 world=transform.TransformPoint(local),normal=transform.up;
        if(normal.y<0)normal=-normal;
        float reach=Mathf.Max(2,size.y+1);
        if(Collision.Active)
        {
            if(Collision.Raycast(new Ray(world+normal*reach,-normal),out var hit,reach*2))world=hit.point;
            else
            {
                // The footprint hull may contain a gap in the actual geometry.
                world=Collision.ClosestPoint(world);
                if(Collision.Raycast(new Ray(world+normal*reach,-normal),out hit,reach*2))world=hit.point;
            }
        }
        return world;
    }
    public float EdgeDistance(Vector3 from)
    {
        Vector3 local=LocalSurfacePoint(from);
        if(Contains(local))return 0;
        Vector2 point=new Vector2(local.x*size.x,local.z*size.z),closest=point;float best=float.PositiveInfinity;
        for(int i=0;i<footprint.Length;i++)
        {
            Vector2 edge=footprint[(i+1)%footprint.Length]-footprint[i];
            Vector2 at=footprint[i]+edge*Mathf.Clamp01(Vector2.Dot(point-footprint[i],edge)/Mathf.Max(.000001f,edge.sqrMagnitude));
            float squared=(at-point).sqrMagnitude;if(squared<best){best=squared;closest=at;}
        }
        local.x=closest.x/size.x;local.z=closest.y/size.z;local.y=TopLocalY;
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
        float top=transform.InverseTransformPoint(BoardingPoint(feet)).y;
        return ContainsFootprint(feet) && Mathf.Abs((local.y-top)*size.y)<.65f;
    }
    public bool SupportedPreviousPose(Vector3 feet)
    {
        float up=previousPose.GetColumn(1).y/size.y;
        if(IsBroken || Deck==null || !Deck.enabled || Mathf.Abs(up)<.65f)return false;
        Vector3 local=previousPose.inverse.MultiplyPoint3x4(feet);
        float top=transform.InverseTransformPoint(BoardingPoint(transform.TransformPoint(local))).y;
        return ContainsFootprint(transform.TransformPoint(local)) && Mathf.Abs((local.y-top)*size.y)<.55f;
    }
    public Vector3 CarryDisplacement(Vector3 feet) => transform.TransformPoint(previousPose.inverse.MultiplyPoint3x4(feet))-feet;
    public void CommitPose() => previousPose=transform.localToWorldMatrix;
}
