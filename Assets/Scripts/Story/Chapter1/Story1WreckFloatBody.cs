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
            return Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,normal),surfaceTiltLimit)*Vector3.up;
        }
    }
    OceanWorld ocean;
    static Transform samplingFocus;
    Mesh pressureMesh;
    Vector3 pressureSize;
    Vector3 pressureCentre;
    Vector3 dimensions;
    float stiffness,verticalDamping,draft;
    Vector3 broadFaceNormal;
    Vector3 sampledAt;
    float surface,slopeX,slopeZ,sampleTime,surfaceVelocity,nextSample;
    Vector3 broadAxisA,broadAxisB;
    float thickness;
    float angularStiffness,angularDamping,surfaceTiltLimit;
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
        pressureCentre=source!=null ? Vector3.Scale(source.bounds.center,size) : Vector3.zero;
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
            pressureLocal[i]=pressureCentre+Vector3.ProjectOnPlane(extreme-pressureCentre,normal);
        }
    }
    public void Initialize(OceanWorld water,Vector3 size,bool floats,Vector3 velocity,Vector3 spin)
    {
        ocean=water;dimensions=size;Floats=floats;
        if(samplingFocus==null){var actor=Object.FindFirstObjectByType<ThirdPersonPlayerController>();samplingFocus=actor!=null ? actor.transform : null;}
        Body=GetComponent<Rigidbody>();Body.isKinematic=false;Body.useGravity=true;
        // Set the physics pose before enabling interpolation; newly added bodies start at the origin.
        Body.position=transform.position;Body.rotation=transform.rotation;
        var source=TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
        float volume=MaterialVolume(source,size);
        // Volume is in world cubic metres. Eight kg/m3 made a deck lighter
        // than foam while the shark's contact solver transfers 1500 kg of
        // momentum: it repeatedly lifted the passenger's platform from below.
        // Use timber density and retain the heavier non-floating proportion.
        Body.mass=Mathf.Clamp(volume*(floats ? 450 : 675),1,50000);
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
        PreparePressureGeometry(source,size);
        for(int i=0;i<4;i++)pressureWeights[i]=.25f;
        massSettlingSteps=0;weightedCentre=Vector3.positiveInfinity;
        float broadArea=size.x*size.y*size.z/Mathf.Max(.001f,thickness);
        thickness=Mathf.Clamp(volume/Mathf.Max(.001f,broadArea),.04f,thickness);
        // Broad timber follows swell with inertia rather than springing out of it.
        stiffness=Physics.gravity.magnitude/Mathf.Max(.2f,thickness*.4f);
        verticalDamping=2*Mathf.Sqrt(stiffness)*.95f;
        // Light timber displaces less than half its thickness. A positive
        // centre-plane draft submerged most of a thin deck before any swell.
        draft=-thickness*.15f;
        // A wide landing deck rolls slowly under a passing swell; a short
        // shard can turn with it. Keep the deck's existing boarding limit,
        // without imposing its heavy response on every small offcut.
        float span=Mathf.Max(broadAxisA.magnitude,broadAxisB.magnitude)/.35f;
        angularStiffness=36f*Mathf.Clamp(Mathf.Sqrt(5f/Mathf.Max(.5f,span)),.55f,1.4f);
        angularDamping=2f*Mathf.Sqrt(angularStiffness);
        surfaceTiltLimit=Mathf.Lerp(27f,16f,Mathf.InverseLerp(3f,9f,span));
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
    // Large cinematic landing decks need water-entry drag independently of
    // the capped hydrostatic restoring force; otherwise a falling deck can
    // retain enough speed to dive tens of metres before its spring recovers.
    public bool SeparateWaterEntryDrag { get; set; }
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
        bool distant=samplingFocus!=null && (samplingFocus.position-position).sqrMagnitude>6400 &&
            Mathf.Max(dimensions.x,dimensions.z)<10;
        // Keep the prediction horizon short enough for a changing crest.
        // Distant timber previously extrapolated 80ms, then held for 160ms;
        // sample twice as often instead of amplifying stale wave velocity.
        float sampleInterval=distant ? .12f : .06f;
        if(Body.linearVelocity.sqrMagnitude>36f || Body.angularVelocity.sqrMagnitude>2.25f)
            sampleInterval=Mathf.Min(sampleInterval,.04f);
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
            float velocityBlend=1-Mathf.Exp(-pointDelta/.07f);
            centreSurfaceVelocity=sampled ? Mathf.Lerp(centreSurfaceVelocity,Mathf.Clamp((centreHeight-predictedCentre)/pointDelta,-12,12),velocityBlend) : 0;
            centreSurface=centreHeight;
            centreSamplePoint=massPoint;
            for(int i=0;i<4;i++)
            {
                float predicted=pointSurface[i]+slopeX*(points[i].x-waterPoints[i].x)+slopeZ*(points[i].z-waterPoints[i].z);
                float pointHeight=i==0 ? nw : i==1 ? ne : i==2 ? sw : se;
                pointSurfaceVelocity[i]=sampled ? Mathf.Lerp(pointSurfaceVelocity[i],Mathf.Clamp((pointHeight-predicted)/pointDelta,-12,12),velocityBlend) : 0;
                pointSurface[i]=pointHeight;
                waterPoints[i]=points[i];
            }
            if(sampled)
            {
                float predicted=surface+slopeX*(position.x-sampledAt.x)+slopeZ*(position.z-sampledAt.z);
                float delta=Mathf.Max(Time.fixedDeltaTime,Time.time-sampleTime);
                surfaceVelocity=Mathf.Lerp(surfaceVelocity,Mathf.Clamp((height-predicted)/delta,-12,12),velocityBlend);
                surface=height;
                float blend=1-Mathf.Exp(-12*delta);
                slopeX=Mathf.Lerp(slopeX,newSlopeX,blend);slopeZ=Mathf.Lerp(slopeZ,newSlopeZ,blend);
            }
            else{surface=height;slopeX=newSlopeX;slopeZ=newSlopeZ;surfaceVelocity=0;}
            sampledAt=position;sampleTime=Time.time;nextSample=Time.time+sampleInterval;sampled=true;
        }
        int wet=0;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            // Both faces contribute to immersion, including a board entering edge first.
            Vector3 point=position+rotation*(pressureCentre+Vector3.Scale(new Vector3(x*.4f,y*.4f,z*.4f),dimensions));
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
            // Integrate vertical displacement over the same real-mesh probes.
            // Clipping each corner's spring separately made a passing crest
            // inject net upward energy and launch the whole thin fragment.
            // Heave belongs to its displaced volume; roll/pitch belongs to
            // the water-plane moment, with the native inertia left intact.
            Vector3 centrePoint=position+rotation*(pressureCentre+Vector3.ProjectOnPlane(nativeMassCentre-pressureCentre,broadFaceNormal));
            float extrapolation=Mathf.Min(.08f,Time.time-sampleTime);
            float centreWater=centreSurface+slopeX*(centrePoint.x-centreSamplePoint.x)+slopeZ*(centrePoint.z-centreSamplePoint.z)+centreSurfaceVelocity*extrapolation;
            float immersion=(centreWater-centrePoint.y)*.5f;
            float waterVelocity=centreSurfaceVelocity*.5f;
            for(int i=0;i<4;i++)
            {
                Vector3 point=position+rotation*pressureLocal[i];
                float height=pointSurface[i]+slopeX*(point.x-waterPoints[i].x)+slopeZ*(point.z-waterPoints[i].z)+pointSurfaceVelocity[i]*extrapolation;
                immersion+=(height-point.y)*pressureWeights[i]*.5f;
                waterVelocity+=pointSurfaceVelocity[i]*pressureWeights[i]*.5f;
            }
            float restoring=Physics.gravity.magnitude+(immersion-draft)*stiffness;
            float relativeSpeed=Body.linearVelocity.y-waterVelocity;
            float drag=-relativeSpeed*verticalDamping;
            // Broad decks entering quickly also displace water through their
            // face. Quadratic entry drag arrests the fall over roughly one
            // thickness; linear swell damping alone allows metres of overshoot.
            if(SeparateWaterEntryDrag&&relativeSpeed<0)
                drag=Mathf.Max(drag,relativeSpeed*relativeSpeed/(2*Mathf.Max(.25f,thickness)));
            float acceleration=SeparateWaterEntryDrag?
                Mathf.Clamp(restoring,-Physics.gravity.magnitude*1.5f,Physics.gravity.magnitude*2.4f)+
                Mathf.Clamp(drag,-Mathf.Abs(relativeSpeed)/Time.fixedDeltaTime,Mathf.Abs(relativeSpeed)/Time.fixedDeltaTime)*submerged:
                Mathf.Clamp(restoring+drag,-Physics.gravity.magnitude*1.5f,Physics.gravity.magnitude*2.4f);
            Body.AddForce(Vector3.up*acceleration,ForceMode.Acceleration);
            Vector3 normal=rotation*broadFaceNormal;
            Vector3 upright=normal.y>=0 ? SurfaceNormal : -SurfaceNormal;
            Vector3 tiltSpin=Vector3.ProjectOnPlane(Body.angularVelocity,Vector3.up);
            Body.AddTorque((Vector3.Cross(normal,upright)*angularStiffness-tiltSpin*angularDamping)*submerged,ForceMode.Acceleration);
        }
        Body.AddTorque(-Body.angularVelocity*(.65f*submerged),ForceMode.Acceleration);
        if(!Floats)Body.AddForce(-Vector3.up*Body.linearVelocity.y*(.65f*submerged),ForceMode.Acceleration);
    }
}
