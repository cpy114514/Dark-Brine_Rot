using UnityEngine;
using System.Collections.Generic;

/// <summary>Gravity, collisions and water forces determine a ship fragment's final pose.</summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class Story1WreckFloatBody : MonoBehaviour
{
    public Rigidbody Body { get; private set; }
    public bool InWater { get; private set; }
    public bool Floats { get; private set; }
    public Vector3 SurfaceNormal
    {
        get
        {
            Vector3 normal=new Vector3(-slopeX*.75f,1,-slopeZ*.75f).normalized;
            return Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,normal),16)*Vector3.up;
        }
    }
    OceanWorld ocean;
    Vector3 dimensions;
    float stiffness,verticalDamping,draft;
    Vector3 broadFaceNormal;
    Vector3 sampledAt;
    float surface,slopeX,slopeZ,sampleTime,surfaceVelocity,nextSample;
    Vector3 broadAxisA,broadAxisB;
    float thickness;
    readonly Vector3[] waterPoints=new Vector3[4];
    readonly Vector3[] samplePoints=new Vector3[4];
    readonly float[] pointSurface=new float[4],pointSurfaceVelocity=new float[4];
    bool sampled;
    static readonly Unity.Profiling.ProfilerMarker BuoyancyMarker=new Unity.Profiling.ProfilerMarker("Story.WreckBuoyancy");
    static readonly Dictionary<Mesh,float> meshVolumes=new Dictionary<Mesh,float>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearVolumes()=>meshVolumes.Clear();
    static float MaterialVolume(Mesh mesh,Vector3 size)
    {
        if(mesh==null)return size.x*size.y*size.z;
        if(!meshVolumes.TryGetValue(mesh,out float volume))
        {
            var vertices=mesh.vertices;var triangles=mesh.triangles;double signed=0;
            for(int i=0;i<triangles.Length;i+=3)signed+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]))/6.0;
            volume=(float)System.Math.Abs(signed);
            // Some original ropes/sails are open surfaces. Their collision
            // patches have finite thickness, but their empty bounding boxes
            // must not weigh as much as solid timber blocks.
            if(volume<.000001f)volume=mesh.bounds.size.x*mesh.bounds.size.y*mesh.bounds.size.z*.02f;
            meshVolumes.Add(mesh,volume);
        }
        return volume*size.x*size.y*size.z;
    }
    public void Initialize(OceanWorld water,Vector3 size,bool floats,Vector3 velocity,Vector3 spin)
    {
        ocean=water;dimensions=size;Floats=floats;
        Body=GetComponent<Rigidbody>();Body.isKinematic=false;Body.useGravity=true;
        // Set the physics pose before enabling interpolation; newly added bodies start at the origin.
        Body.position=transform.position;Body.rotation=transform.rotation;
        float volume=MaterialVolume(GetComponent<MeshFilter>()?.sharedMesh,size);
        Body.mass=Mathf.Clamp(volume*(floats ? 8 : 12),1,2000);
        Body.linearDamping=.03f;Body.angularDamping=.06f;Body.maxAngularVelocity=5;
        Body.maxDepenetrationVelocity=3;Body.interpolation=RigidbodyInterpolation.Interpolate;
        Body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        Body.solverIterations=10;Body.solverVelocityIterations=4;
        nextSample=Random.value*.08f;sampled=false;
        Body.linearVelocity=velocity;Body.angularVelocity=spin*Mathf.Deg2Rad;
        // Use the thin axis of each real fragment, including rails/masts and curved hulls.
        thickness=Mathf.Min(size.x,Mathf.Min(size.y,size.z));
        broadFaceNormal=thickness==size.x ? Vector3.right : thickness==size.y ? Vector3.up : Vector3.forward;
        broadAxisA=thickness==size.x ? Vector3.up*(size.y*.35f) : Vector3.right*(size.x*.35f);
        broadAxisB=thickness==size.z ? Vector3.up*(size.y*.35f) : Vector3.forward*(size.z*.35f);
        float broadArea=size.x*size.y*size.z/Mathf.Max(.001f,thickness);
        thickness=Mathf.Clamp(volume/Mathf.Max(.001f,broadArea),.04f,thickness);
        // Broad timber follows swell with inertia rather than springing out of it.
        stiffness=Physics.gravity.magnitude/Mathf.Max(.25f,thickness*.4f);
        verticalDamping=2*Mathf.Sqrt(stiffness)*1.05f;
        draft=thickness*.25f;
    }
    void FixedUpdate()
    {
        if(Body==null || Body.isKinematic || ocean==null)return;
        using var measurement=BuoyancyMarker.Auto();
        Vector3 position=Body.position;Quaternion rotation=Body.rotation;
        if(!sampled || Time.time>=nextSample)
        {
            Vector3 across=rotation*broadAxisA,along=rotation*broadAxisB;
            var points=samplePoints;points[0]=position-across+along;points[1]=position+across+along;points[2]=position-across-along;points[3]=position+across-along;
            float nw=ocean.SampleSurfaceHeight(points[0],Time.time),ne=ocean.SampleSurfaceHeight(points[1],Time.time);
            float sw=ocean.SampleSurfaceHeight(points[2],Time.time),se=ocean.SampleSurfaceHeight(points[3],Time.time);
            float height=ocean.SampleSurfaceHeight(position,Time.time)*.5f+(nw+ne+sw+se)*.125f;
            float alongA=(ne+se-nw-sw)*.25f,alongB=(nw+ne-sw-se)*.25f,det=across.x*along.z-across.z*along.x;
            float newSlopeX=Mathf.Abs(det)>.01f ? (alongA*along.z-alongB*across.z)/det : slopeX;
            float newSlopeZ=Mathf.Abs(det)>.01f ? (alongB*across.x-alongA*along.x)/det : slopeZ;
            float pointDelta=Mathf.Max(Time.fixedDeltaTime,Time.time-sampleTime);
            for(int i=0;i<4;i++)
            {
                float predicted=pointSurface[i]+slopeX*(points[i].x-waterPoints[i].x)+slopeZ*(points[i].z-waterPoints[i].z);
                float pointHeight=i==0 ? nw : i==1 ? ne : i==2 ? sw : se;
                pointSurface[i]=sampled ? Mathf.SmoothDamp(predicted,pointHeight,ref pointSurfaceVelocity[i],.18f,Mathf.Infinity,pointDelta) : pointHeight;
                waterPoints[i]=points[i];
            }
            if(sampled)
            {
                float predicted=surface+slopeX*(position.x-sampledAt.x)+slopeZ*(position.z-sampledAt.z);
                float delta=Mathf.Max(Time.fixedDeltaTime,Time.time-sampleTime);
                surface=Mathf.SmoothDamp(predicted,height,ref surfaceVelocity,.18f,Mathf.Infinity,delta);
                float blend=1-Mathf.Exp(-3*delta);
                slopeX=Mathf.Lerp(slopeX,newSlopeX,blend);slopeZ=Mathf.Lerp(slopeZ,newSlopeZ,blend);
            }
            else{surface=height;slopeX=newSlopeX;slopeZ=newSlopeZ;surfaceVelocity=0;}
            sampledAt=position;sampleTime=Time.time;nextSample=Time.time+.08f;sampled=true;
        }
        int wet=0;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            // Both faces contribute to immersion, including a board entering edge first.
            Vector3 point=position+rotation*Vector3.Scale(new Vector3(x*.4f,y*.4f,z*.4f),dimensions);
            float depth=surface+slopeX*(point.x-sampledAt.x)+slopeZ*(point.z-sampledAt.z)+
                surfaceVelocity*Mathf.Min(.08f,Time.time-sampleTime)-point.y;
            if(depth<=0)continue;
            wet++;
        }
        InWater=wet>0;
        if(wet==0)return;
        float submerged=wet*.125f;
        Body.AddForce(-Vector3.ProjectOnPlane(Body.linearVelocity,Vector3.up)*(1.15f*submerged),ForceMode.Acceleration);
        if(Floats)
        {
            Vector3 across=rotation*broadAxisA,along=rotation*broadAxisB;
            for(int i=0;i<4;i++)
            {
                Vector3 point=position+across*(i%2==0 ? -1 : 1)+along*(i<2 ? 1 : -1);
                float height=pointSurface[i]+slopeX*(point.x-waterPoints[i].x)+slopeZ*(point.z-waterPoints[i].z)+pointSurfaceVelocity[i]*Mathf.Min(.08f,Time.time-sampleTime);
                float immersion=height-point.y;
                if(immersion<-thickness*.5f)continue;
                float lift=Mathf.Clamp(Physics.gravity.magnitude+(immersion-draft)*stiffness,0,Physics.gravity.magnitude*8);
                float relativeHeave=Body.GetPointVelocity(point).y-pointSurfaceVelocity[i];
                float drag=verticalDamping*Mathf.Clamp01((immersion+thickness*.5f)/Mathf.Max(.1f,thickness)*2);
                // Four broad-face pressure points follow the actual crest at
                // each end. Real mass/inertia converts their lever arms into
                // torque; applying acceleration directly at distant corners
                // used to over-excite pitch, while centre-only lift submerged
                // long deck edges even when the centre floated correctly.
                float pressure=Mathf.Clamp(lift-relativeHeave*drag,-Physics.gravity.magnitude*1.5f,Physics.gravity.magnitude*2.4f);
                Body.AddForceAtPosition(Vector3.up*(pressure*Body.mass*.25f),point,ForceMode.Force);
            }
            Vector3 normal=Body.rotation*broadFaceNormal;
            Vector3 waterNormal=SurfaceNormal;
            Vector3 upright=normal.y>=0 ? waterNormal : -waterNormal;
            Vector3 tiltSpin=Vector3.ProjectOnPlane(Body.angularVelocity,Vector3.up);
            Body.AddTorque((Vector3.Cross(normal,upright)*2f-tiltSpin*1.5f)*submerged,ForceMode.Acceleration);
        }
        Body.AddTorque(-Body.angularVelocity*(.65f*submerged),ForceMode.Acceleration);
        if(!Floats)Body.AddForce(-Vector3.up*Body.linearVelocity.y*(.65f*submerged),ForceMode.Acceleration);
    }
}
