using UnityEngine;

/// <summary>Gravity, collisions and water forces determine a ship fragment's final pose.</summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class Story1WreckFloatBody : MonoBehaviour
{
    public Rigidbody Body { get; private set; }
    public bool InWater { get; private set; }
    public bool Floats { get; private set; }
    OceanWorld ocean;
    Vector3 dimensions;
    float stiffness;
    Vector3 broadFaceNormal;
    public void Initialize(OceanWorld water,Vector3 size,bool floats,Vector3 velocity,Vector3 spin)
    {
        ocean=water;dimensions=size;Floats=floats;
        Body=GetComponent<Rigidbody>();Body.isKinematic=false;Body.useGravity=true;
        // Set the physics pose before enabling interpolation; newly added bodies start at the origin.
        Body.position=transform.position;Body.rotation=transform.rotation;
        Body.mass=Mathf.Clamp(size.x*size.y*size.z*(floats ? 8 : 12),1,300);
        Body.linearDamping=.03f;Body.angularDamping=.06f;Body.maxAngularVelocity=5;
        Body.maxDepenetrationVelocity=3;Body.interpolation=RigidbodyInterpolation.Interpolate;
        Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        Body.linearVelocity=velocity;Body.angularVelocity=spin*Mathf.Deg2Rad;
        // Use the thin axis of each real fragment, including rails/masts and curved hulls.
        float thickness=Mathf.Min(size.x,Mathf.Min(size.y,size.z));
        broadFaceNormal=thickness==size.x ? Vector3.right : thickness==size.y ? Vector3.up : Vector3.forward;
        stiffness=Physics.gravity.magnitude/Mathf.Max(.25f,thickness*.55f);
    }
    void FixedUpdate()
    {
        if(Body==null || Body.isKinematic || ocean==null)return;
        int wet=0;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            // Both faces contribute: an edge entering water must also generate tipping force.
            Vector3 point=Body.position+Body.rotation*Vector3.Scale(new Vector3(x*.4f,y*.4f,z*.4f),dimensions);
            float depth=ocean.SampleSurfaceHeight(point,Time.time)-point.y;
            if(depth<=0)continue;
            wet++;
            if(Floats)
            {
                float lift=Mathf.Clamp(depth*stiffness-Body.GetPointVelocity(point).y*3f,0,Physics.gravity.magnitude*3);
                Body.AddForceAtPosition(Vector3.up*(lift*.125f),point,ForceMode.Acceleration);
            }
        }
        InWater=wet>0;
        if(wet==0)return;
        float submerged=wet*.125f;
        Body.AddForce(-Vector3.ProjectOnPlane(Body.linearVelocity,Vector3.up)*(1.15f*submerged),ForceMode.Acceleration);
        if(Floats)
        {
            // Water rights the broad face through torque, preserving free yaw and physical collisions.
            // Either face may finish upwards; no fragment is rotated or moved to a landing pose.
            Vector3 normal=Body.rotation*broadFaceNormal;
            Vector3 upright=normal.y>=0 ? Vector3.up : Vector3.down;
            Vector3 tiltSpin=Vector3.ProjectOnPlane(Body.angularVelocity,Vector3.up);
            Body.AddTorque((Vector3.Cross(normal,upright)*10f-tiltSpin*4.5f)*submerged,ForceMode.Acceleration);
        }
        Body.AddTorque(-Body.angularVelocity*(.65f*submerged),ForceMode.Acceleration);
        if(!Floats)Body.AddForce(-Vector3.up*Body.linearVelocity.y*(.65f*submerged),ForceMode.Acceleration);
    }
}
