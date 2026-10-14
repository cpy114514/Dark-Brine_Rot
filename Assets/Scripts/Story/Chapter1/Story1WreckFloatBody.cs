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
    static Transform samplingFocus;
    Mesh pressureMesh;
    Vector3 pressureSize;
    Vector3 dimensions;
    float stiffness,verticalDamping,draft;
    Vector3 broadFaceNormal;
    Vector3 sampledAt;
    float surface,slopeX,slopeZ,sampleTime,surfaceVelocity,nextSample;
    Vector3 broadAxisA,broadAxisB;
    float thickness;
    readonly Vector3[] waterPoints=new Vector3[4];
    readonly Vector3[] samplePoints=new Vector3[4];
    readonly Vector3[] pressureLocal=new Vector3[4];
    readonly float[] pressures=new float[4];
    readonly float[] pressureWeights=new float[4];
    int massSettlingSteps;
    Vector3 weightedCentre;
    readonly float[] pointSurface=new float[4],pointSurfaceVelocity=new float[4];
    Vector3 centreSamplePoint;
    float centreSurface,centreSurfaceVelocity;
    bool sampled;
    Vector3 pendingVelocity, pendingSpin;
    bool launchPending;
    static readonly Unity.Profiling.ProfilerMarker BuoyancyMarker=new Unity.Profiling.ProfilerMarker("Story.WreckBuoyancy");
    static readonly Dictionary<Mesh,float> meshVolumes=new Dictionary<Mesh,float>();
    static readonly Dictionary<Mesh,Vector3[]> meshVertices=new Dictionary<Mesh,Vector3[]>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearVolumes(){meshVolumes.Clear();meshVertices.Clear();samplingFocus=null;}
    static Vector3[] Vertices(Mesh mesh)
    {
        if(!meshVertices.TryGetValue(mesh,out var vertices))meshVertices.Add(mesh,vertices=mesh.vertices);
        return vertices;
    }
    public static void PrepareVolume(Mesh mesh){if(mesh!=null)MaterialVolume(mesh,Vector3.one);}
    static float MaterialVolume(Mesh mesh,Vector3 size)
    {
        if(mesh==null)return size.x*size.y*size.z;
        if(!meshVolumes.TryGetValue(mesh,out float volume))
        {
            var vertices=Vertices(mesh);var triangles=mesh.triangles;double signed=0;
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
    public static void PrewarmMaterialVolume(Mesh mesh)=>MaterialVolume(mesh,Vector3.one);
    public void PreparePressureGeometry(Mesh source,Vector3 size)
    {
        if(pressureMesh==source && pressureSize==size)return;
        pressureMesh=source;pressureSize=size;
        float thin=Mathf.Min(size.x,Mathf.Min(size.y,size.z));
        Vector3 normal=thin==size.x ? Vector3.right : thin==size.y ? Vector3.up : Vector3.forward;
        Vector3 axisA=thin==size.x ? Vector3.up*(size.y*.35f) : Vector3.right*(size.x*.35f);
        Vector3 axisB=thin==size.z ? Vector3.up*(size.y*.35f) : Vector3.forward*(size.z*.35f);
        var points=source!=null ? Vertices(source) : null;
        for(int i=0;i<4;i++)
        {
            Vector3 direction=axisA.normalized*(i%2==0 ? -1 : 1)+axisB.normalized*(i<2 ? 1 : -1);
            Vector3 extreme=axisA*(i%2==0 ? -1 : 1)+axisB*(i<2 ? 1 : -1);
            float best=float.NegativeInfinity;
            if(points!=null)foreach(var point in points)
            {
                float score=Vector3.Dot(point,direction);
                if(score>best){best=score;extreme=Vector3.Scale(point,size)*.88f;}
            }
            pressureLocal[i]=Vector3.ProjectOnPlane(extreme,normal);
        }
    }
    public void Initialize(OceanWorld water,Vector3 size,bool floats,Vector3 velocity,Vector3 spin)
    {
        ocean=water;dimensions=size;Floats=floats;
        if(samplingFocus==null){var actor=Object.FindFirstObjectByType<ThirdPersonPlayerController>();samplingFocus=actor!=null ? actor.transform : null;}
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
        // Warmed fragments are still inactive. Unity discards velocity writes
        // until the native body is enabled; retain the launch across release.
        pendingVelocity=velocity;pendingSpin=spin*Mathf.Deg2Rad;launchPending=true;
        if(gameObject.activeInHierarchy && enabled)ApplyPendingLaunch();
        // Use the thin axis of each real fragment, including rails/masts and curved hulls.
        thickness=Mathf.Min(size.x,Mathf.Min(size.y,size.z));
        broadFaceNormal=thickness==size.x ? Vector3.right : thickness==size.y ? Vector3.up : Vector3.forward;
        broadAxisA=thickness==size.x ? Vector3.up*(size.y*.35f) : Vector3.right*(size.x*.35f);
        broadAxisB=thickness==size.z ? Vector3.up*(size.y*.35f) : Vector3.forward*(size.z*.35f);
        // Irregular triangular fragments do not have four rectangular corners.
        // Put the pressure probes near the real timber perimeter, retaining
        // the same four surface samples and the body's actual mass/inertia.
        var source=GetComponent<MeshFilter>()?.sharedMesh;
        PreparePressureGeometry(source,size);
        for(int i=0;i<4;i++)pressureWeights[i]=.25f;
        massSettlingSteps=0;weightedCentre=Vector3.positiveInfinity;
        float broadArea=size.x*size.y*size.z/Mathf.Max(.001f,thickness);
        thickness=Mathf.Clamp(volume/Mathf.Max(.001f,broadArea),.04f,thickness);
        // Broad timber follows swell with inertia rather than springing out of it.
        stiffness=Physics.gravity.magnitude/Mathf.Max(.25f,thickness*.4f);
        verticalDamping=2*Mathf.Sqrt(stiffness)*1.05f;
        draft=thickness*.25f;
    }
    void OnEnable() { if(launchPending && Body!=null)ApplyPendingLaunch(); }
    void ApplyPendingLaunch()
    {
        Body.linearVelocity=pendingVelocity;Body.angularVelocity=pendingSpin;
        Body.WakeUp();launchPending=false;
    }
    void RebalancePressure(Vector3 centre)
    {
        weightedCentre=centre;
        Vector3 mean=(pressureLocal[0]+pressureLocal[1]+pressureLocal[2]+pressureLocal[3])*.25f;
        Vector3 axisA=broadAxisA.normalized,axisB=broadAxisB.normalized;
        float xx=0,zz=0,xz=0;
        for(int i=0;i<4;i++)
        {
            Vector3 delta=pressureLocal[i]-mean;float x=Vector3.Dot(delta,axisA),z=Vector3.Dot(delta,axisB);
            xx+=x*x;zz+=z*z;xz+=x*z;
        }
        float det=xx*zz-xz*xz,dx=Vector3.Dot(centre-mean,axisA),dz=Vector3.Dot(centre-mean,axisB);
        float coefficientA=Mathf.Abs(det)>.0001f ? (dx*zz-dz*xz)/det : xx>=zz ? dx/Mathf.Max(.001f,xx) : 0;
        float coefficientB=Mathf.Abs(det)>.0001f ? (dz*xx-dx*xz)/det : zz>xx ? dz/Mathf.Max(.001f,zz) : 0;
        float totalWeight=0;
        for(int i=0;i<4;i++)
        {
            Vector3 delta=pressureLocal[i]-mean;
            pressureWeights[i]=Mathf.Max(.001f,.25f+Vector3.Dot(delta,axisA)*coefficientA+Vector3.Dot(delta,axisB)*coefficientB);
            totalWeight+=pressureWeights[i];
        }
        for(int i=0;i<4;i++)pressureWeights[i]/=totalWeight;
    }
    void FixedUpdate()
    {
        if(Body==null || Body.isKinematic || ocean==null)return;
        using var measurement=BuoyancyMarker.Auto();
        // Freshly cooked compound colliders are registered by the first
        // physics steps. Their construction-frame COM may still be zero.
        // Fit the pressure weights to the registered native mass geometry.
        if(massSettlingSteps<3){Body.ResetCenterOfMass();Body.ResetInertiaTensor();massSettlingSteps++;}
        Vector3 nativeMassCentre=Quaternion.Inverse(Body.rotation)*(Body.worldCenterOfMass-Body.position);
        if((nativeMassCentre-weightedCentre).sqrMagnitude>.000001f)RebalancePressure(nativeMassCentre);
        Vector3 position=Body.position;Quaternion rotation=Body.rotation;
        // Small distant debris needs fewer expensive ocean queries. Rigid-body
        // contacts and buoyancy forces still run on every physics step.
        float sampleInterval=samplingFocus!=null && (samplingFocus.position-position).sqrMagnitude>6400 &&
            Mathf.Max(dimensions.x,dimensions.z)<10 ? .24f : .08f;
        if(sampled)nextSample=sampleTime+sampleInterval;
        if(!sampled || Time.time>=nextSample)
        {
            // All five pressure probes use the same ocean/camera/clock.
            // Prepare waves once, retaining every inverse-displacement sample.
            var water=OceanSurfaceSampler.Capture(ocean,Camera.main,Time.time);
            var points=samplePoints;for(int i=0;i<4;i++)points[i]=position+rotation*pressureLocal[i];
            float nw=water.Height(points[0]),ne=water.Height(points[1]);
            float sw=water.Height(points[2]),se=water.Height(points[3]);
            Vector3 massPoint=Body.worldCenterOfMass;
            float centreHeight=water.Height(massPoint);
            float height=centreHeight*.5f+(nw+ne+sw+se)*.125f;
            Vector3 centre=(points[0]+points[1]+points[2]+points[3])*.25f;float mean=(nw+ne+sw+se)*.25f;
            float xx=0,zz=0,xz=0,xh=0,zh=0;
            for(int i=0;i<4;i++)
            {
                Vector3 delta=points[i]-centre;float h=(i==0 ? nw : i==1 ? ne : i==2 ? sw : se)-mean;
                xx+=delta.x*delta.x;zz+=delta.z*delta.z;xz+=delta.x*delta.z;xh+=delta.x*h;zh+=delta.z*h;
            }
            float det=xx*zz-xz*xz;
            float newSlopeX=Mathf.Abs(det)>.01f ? (xh*zz-zh*xz)/det : slopeX;
            float newSlopeZ=Mathf.Abs(det)>.01f ? (zh*xx-xh*xz)/det : slopeZ;
            float pointDelta=Mathf.Max(Time.fixedDeltaTime,Time.time-sampleTime);
            float predictedCentre=centreSurface+slopeX*(massPoint.x-centreSamplePoint.x)+slopeZ*(massPoint.z-centreSamplePoint.z);
            centreSurface=sampled ? Mathf.SmoothDamp(predictedCentre,centreHeight,ref centreSurfaceVelocity,.18f,Mathf.Infinity,pointDelta) : centreHeight;
            centreSamplePoint=massPoint;
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
            sampledAt=position;sampleTime=Time.time;nextSample=Time.time+sampleInterval;sampled=true;
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
        Vector3 immersionCentre=Body.worldCenterOfMass;
        float interiorHeight=centreSurface+slopeX*(immersionCentre.x-centreSamplePoint.x)+slopeZ*(immersionCentre.z-centreSamplePoint.z);
        if(interiorHeight>immersionCentre.y-thickness*.5f)wet=Mathf.Max(wet,4);
        InWater=wet>0;
        if(wet==0)return;
        float submerged=wet*.125f;
        Vector3 planar=Vector3.ProjectOnPlane(Body.linearVelocity,Vector3.up);
        Vector3 resistance=-planar*1.15f;
        Body.AddForce(resistance*submerged,ForceMode.Acceleration);
        if(Floats)
        {
            float positivePressure=0;
            for(int i=0;i<4;i++)
            {
                pressures[i]=0;Vector3 point=position+rotation*pressureLocal[i];
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
                pressures[i]=Mathf.Clamp(lift-relativeHeave*drag,-Physics.gravity.magnitude*1.5f,Physics.gravity.magnitude*8);
                positivePressure+=Mathf.Max(0,pressures[i])*pressureWeights[i]*.5f;
            }
            // The interior displaces water too. A corner-only quadrature
            // ignored crests underneath a broad deck and could submerge its
            // entire fighting surface while all four corners looked buoyant.
            // Reuse the fifth existing surface sample at the native COM;
            // half the pressure represents the interior, half the perimeter.
            Vector3 centrePoint=Body.worldCenterOfMass;
            float centreWater=centreSurface+slopeX*(centrePoint.x-centreSamplePoint.x)+slopeZ*(centrePoint.z-centreSamplePoint.z)+centreSurfaceVelocity*Mathf.Min(.08f,Time.time-sampleTime);
            float centreImmersion=centreWater-centrePoint.y;
            float centrePressure=0;
            if(centreImmersion>=-thickness*.5f)
            {
                float lift=Mathf.Clamp(Physics.gravity.magnitude+(centreImmersion-draft)*stiffness,0,Physics.gravity.magnitude*8);
                float drag=verticalDamping*Mathf.Clamp01((centreImmersion+thickness*.5f)/Mathf.Max(.1f,thickness)*2);
                centrePressure=Mathf.Clamp(lift-(Body.linearVelocity.y-centreSurfaceVelocity)*drag,-Physics.gravity.magnitude*1.5f,Physics.gravity.magnitude*8);
                positivePressure+=Mathf.Max(0,centrePressure)*.5f;
            }
            // A single wet corner may support light timber; capping each
            // corner at a quarter of the available lift sank the ends of
            // broad decks. Keep the total upward acceleration bounded so
            // the stronger local response cannot launch the whole fragment.
            float pressureScale=Mathf.Min(1,Physics.gravity.magnitude*2.4f/Mathf.Max(.001f,positivePressure));
            for(int i=0;i<4;i++)
            {
                float pressure=pressures[i]>0 ? pressures[i]*pressureScale : pressures[i];
                Body.AddForceAtPosition(Vector3.up*(pressure*Body.mass*pressureWeights[i]*.5f),position+rotation*pressureLocal[i],ForceMode.Force);
            }
            Body.AddForce(Vector3.up*((centrePressure>0 ? centrePressure*pressureScale : centrePressure)*Body.mass*.5f),ForceMode.Force);
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
