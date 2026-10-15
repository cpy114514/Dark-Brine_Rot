using System.Collections.Generic;
using UnityEngine;

/// <summary>Solid anatomical contact for a shark whose animation owns its world movement.</summary>
[DefaultExecutionOrder(1200)]
public sealed class Story1SharkWreckContact : MonoBehaviour
{
    public struct AnatomyPose
    {
        public Vector3 head,body,tailBase,tailTip,nose,tailMarker,finPosition;
        public Quaternion finRotation;
    }
    public AnatomyPose CaptureAnatomy()
    {
        Vector3 Local(Vector3 point)=>shark.transform.InverseTransformPoint(point);
        return new AnatomyPose{head=Local(head.position),body=Local(body.position),tailBase=Local(tailBase.position),
            tailTip=Local(tailTip.position),nose=Local(shark.NoseWorldPoint),tailMarker=Local(shark.TailWorldPoint),
            finPosition=Local(tailTip.position),finRotation=Quaternion.Inverse(shark.transform.rotation)*tailTip.rotation};
    }
    public int BlockedContactCount { get; private set; }
    public float MaximumResidualPenetration { get; private set; }
    public Vector3 LastCorrection { get; private set; }
    public int ConstraintHoldCount { get; private set; }
    public int LargeEscapeCount { get; private set; }
    public int PoseAdvanceCount { get; private set; }
    public int PartialPoseAdvanceCount { get; private set; }
    public int PartialRootAdvanceCount { get; private set; }
    public int CurrentPoseRecenterCount { get; private set; }
    public readonly List<string> LargeEscapes=new List<string>();
    Transform[] poseBones;
    Vector3[] retainedPositions;
    Quaternion[] retainedRotations;
    Vector3[] retryPositions;
    Quaternion[] retryRotations;
    Vector3[] intendedPositions;
    Quaternion[] intendedRotations;
    bool hasRetainedPose;
    public readonly List<string> LastObstacles=new List<string>();
    public CapsuleCollider HeadVolume=>volumes!=null ? volumes[0] as CapsuleCollider : null;
    public Vector3 BulkBodyPoint=>body!=null ? body.position : shark.transform.position;
    Story1WreckBattle battle;
    TralaleroSwimAnimator shark;
    Transform head,body,tailBase,tailTip;
    Collider[] volumes;
    Mesh finMesh;
    MeshCollider finVolume,planningFin;
    CapsuleCollider[] planningVolumes;
    readonly Dictionary<Vector2,CapsuleCollider>[] planningGeometry={
        new Dictionary<Vector2,CapsuleCollider>(),new Dictionary<Vector2,CapsuleCollider>(),
        new Dictionary<Vector2,CapsuleCollider>(),new Dictionary<Vector2,CapsuleCollider>()};
    public int PlanningGeometryReuses { get; private set; }
    public int PlanningGeometryCreated { get; private set; }
    CapsuleCollider attackVolume;
    CapsuleCollider planningReach;
    // A planning pass tests thousands of hypothetical attitudes against the
    // same rendered wreck. Snapshot it once, avoiding Transform/native bounds
    // queries for every capsule, without changing the final solid contact solve.
    struct PlanningShape
    {
        public Bounds bounds;
        public int first,count;
    }
    struct PlanningPart{public Bounds bounds;public MeshCollider collider;public Vector3 position;public Quaternion rotation;public int owner;}
    // A capsule can overlap a mesh only if its centre segment enters the
    // mesh AABB expanded by its radius. Unlike the capsule's enclosing AABB,
    // this rejects distant corners along a long diagonal body or tail.
    // This is rejection only; every remaining pair keeps its native geometry
    // test and its original penetration/contact thresholds.
    struct CapsuleBoundsQuery
    {
        readonly Vector3 start,delta;
        readonly float radius;
        readonly bool capsule;
        public CapsuleBoundsQuery(Collider shape,Vector3 position,Quaternion rotation)
        {
            start=delta=Vector3.zero;radius=0;capsule=shape is CapsuleCollider;
            if(!(shape is CapsuleCollider c))return;
            Vector3 scale=c.transform.lossyScale;
            float axial=Mathf.Abs(c.direction==0?scale.x:c.direction==1?scale.y:scale.z);
            radius=c.radius*(c.direction==0?Mathf.Max(Mathf.Abs(scale.y),Mathf.Abs(scale.z)):
                c.direction==1?Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)):Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));
            Vector3 axis=c.direction==0?Vector3.right:c.direction==1?Vector3.up:Vector3.forward;
            Vector3 extent=rotation*axis*Mathf.Max(0,c.height*axial*.5f-radius);
            Vector3 centre=position+rotation*Vector3.Scale(c.center,scale);
            start=centre-extent;delta=extent*2;
        }
        public bool MayTouch(Bounds bounds)
        {
            if(!capsule)return true;
            // The extra millimetre is conservative broadphase padding only;
            // it cannot accept penetration rejected by the native solver.
            Vector3 padding=Vector3.one*(radius+.001f),low=bounds.min-padding,high=bounds.max+padding;
            float enter=0,leave=1;
            bool Clip(float at,float along,float min,float max)
            {
                if(Mathf.Abs(along)<1e-8f)return at>=min && at<=max;
                float a=(min-at)/along,b=(max-at)/along;
                if(a>b){float swap=a;a=b;b=swap;}
                enter=Mathf.Max(enter,a);leave=Mathf.Min(leave,b);return enter<=leave;
            }
            return Clip(start.x,delta.x,low.x,high.x) && Clip(start.y,delta.y,low.y,high.y) && Clip(start.z,delta.z,low.z,high.z);
        }
    }
    readonly List<PlanningShape> planningShapes=new List<PlanningShape>(256);
    readonly List<PlanningPart> planningParts=new List<PlanningPart>(2048);
    readonly Dictionary<Story1WreckMeshCollision,int> planningShapeIndices=new Dictionary<Story1WreckMeshCollision,int>();
    readonly Dictionary<Collider,Bounds> anatomyBounds=new Dictionary<Collider,Bounds>();
    Bounds solidBounds;
    readonly Dictionary<Vector3Int,List<int>> planningCells=new Dictionary<Vector3Int,List<int>>();
    readonly List<int> largePlanningParts=new List<int>(),touchedPlanningShapes=new List<int>();
    int[] visitedPlanningParts=new int[2048];float[] planningDepths=new float[512];int planningQuery;
    const float PlanningCellSize=8;
    static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x/PlanningCellSize),Mathf.FloorToInt(p.y/PlanningCellSize),Mathf.FloorToInt(p.z/PlanningCellSize));
    int planningFrame=-1;
    readonly Dictionary<Collider,int> recentBlockingParts=new Dictionary<Collider,int>();
    public int RecentBlockerRejects { get; private set; }
    public int RetainedFailureReuses { get; private set; }
    public int BoundaryClearCount { get; private set; }
    public int PlanningQueries { get; private set; }
    static readonly Unity.Profiling.ProfilerMarker PlanningMarker=new Unity.Profiling.ProfilerMarker("Wreck.PoseObstruction");
    static readonly Unity.Profiling.ProfilerMarker SolidMarker=new Unity.Profiling.ProfilerMarker("Wreck.SolidContact");
    static readonly Unity.Profiling.ProfilerMarker SweepMarker=new Unity.Profiling.ProfilerMarker("Wreck.SolidSweep");
    static readonly Unity.Profiling.ProfilerMarker EscapeMarker=new Unity.Profiling.ProfilerMarker("Wreck.SolidEscape");
    static readonly Unity.Profiling.ProfilerMarker ClearSearchMarker=new Unity.Profiling.ProfilerMarker("Wreck.ClearSearch");
    static readonly Unity.Profiling.ProfilerMarker RetainedMarker=new Unity.Profiling.ProfilerMarker("Wreck.RetainedPlacement");
    static readonly Unity.Profiling.ProfilerMarker VolumeMarker=new Unity.Profiling.ProfilerMarker("Wreck.UpdateVolumes");
    static readonly Unity.Profiling.ProfilerMarker OverlapMarker=new Unity.Profiling.ProfilerMarker("Wreck.SolidOverlap");
    static readonly Unity.Profiling.ProfilerMarker DamageMarker=new Unity.Profiling.ProfilerMarker("Wreck.SolvedDamage");
    static readonly Unity.Profiling.ProfilerMarker ResidualMarker=new Unity.Profiling.ProfilerMarker("Wreck.ResidualPenetration");
    static readonly Unity.Profiling.ProfilerMarker SnapshotMarker=new Unity.Profiling.ProfilerMarker("Wreck.ShapeSnapshot");
    static readonly Unity.Profiling.ProfilerMarker AdaptivePoseMarker=new Unity.Profiling.ProfilerMarker("Wreck.AdaptivePose");
    void SnapshotPlanningShapes()
    {
        if(planningFrame==Time.frameCount)return;
        using var measurement=SnapshotMarker.Auto();
        planningFrame=Time.frameCount;planningShapes.Clear();planningParts.Clear();planningShapeIndices.Clear();recentBlockingParts.Clear();
        foreach(var cell in planningCells.Values)cell.Clear();largePlanningParts.Clear();
        void Add(Story1WreckMeshCollision shape)
        {
            if(shape==null || !shape.Active)return;
            shape.ReadWorldGeometry(out var aggregate,out var bounds,out var positions,out var rotations);
            var entry=new PlanningShape{bounds=aggregate,first=planningParts.Count,count=shape.Parts.Length};
            for(int i=0;i<entry.count;i++)
            {
                var part=new PlanningPart{bounds=bounds[i],collider=shape.Parts[i],position=positions[i],rotation=rotations[i],owner=planningShapes.Count};
                int partIndex=planningParts.Count;planningParts.Add(part);Vector3Int low=Cell(part.bounds.min),high=Cell(part.bounds.max);
                if((long)(high.x-low.x+1)*(high.y-low.y+1)*(high.z-low.z+1)>128){largePlanningParts.Add(partIndex);continue;}
                for(int x=low.x;x<=high.x;x++)for(int y=low.y;y<=high.y;y++)for(int z=low.z;z<=high.z;z++)
                {
                    var key=new Vector3Int(x,y,z);
                    if(!planningCells.TryGetValue(key,out var entries)){entries=new List<int>(8);planningCells.Add(key,entries);}
                    entries.Add(partIndex);
                }
            }
            planningShapeIndices.Add(shape,planningShapes.Count);planningShapes.Add(entry);
        }
        foreach(var board in battle.Boards)
        {
            if(board==null || board.IsBroken || Time.time<board.SharkContactAfter)continue;
            var shape=board.Collision;
            Add(shape);
        }
        foreach(var piece in battle.WreckDetails)
        {
            if(piece==null)continue;
            var shape=piece.Collision;
            Add(shape);
        }
        if(visitedPlanningParts.Length<planningParts.Count)System.Array.Resize(ref visitedPlanningParts,Mathf.NextPowerOfTwo(planningParts.Count));
        if(planningDepths.Length<planningShapes.Count)System.Array.Resize(ref planningDepths,Mathf.NextPowerOfTwo(planningShapes.Count));
    }
    float PlanningCost(Collider anatomy,Vector3 position,Quaternion rotation,Bounds bounds)
        =>PlanningCostBounded(anatomy,position,rotation,bounds,float.PositiveInfinity);
    float PlanningCostBounded(Collider anatomy,Vector3 position,Quaternion rotation,Bounds bounds,float stopAbove)
    {
        // Adjacent escape/attitude probes often fail on the same original
        // convex part. A native penetration against that part alone is a
        // proven lower bound; it may reject, never accept, a candidate.
        // Keep the original accumulation order whenever this proof fails.
        if(!float.IsPositiveInfinity(stopAbove) && RecentBlockingDepth(anatomy,position,rotation,bounds,out float depth) &&
            (double)depth*depth>stopAbove+1e-6*(1+Mathf.Abs(stopAbove)))
        {RecentBlockerRejects++;return depth*depth;}
        return PlanningCostBoundedOriginal(anatomy,position,rotation,bounds,stopAbove);
    }
    bool RecentBlockingDepth(Collider anatomy,Vector3 position,Quaternion rotation,Bounds bounds,out float depth)
    {
        depth=0;
        if(!recentBlockingParts.TryGetValue(anatomy,out int index))return false;
        var part=planningParts[index];
        return part.bounds.Intersects(bounds) && new CapsuleBoundsQuery(anatomy,position,rotation).MayTouch(part.bounds) &&
            Physics.ComputePenetration(anatomy,position,rotation,part.collider,part.position,part.rotation,out _,out depth);
    }
    float PlanningCostBoundedOriginal(Collider anatomy,Vector3 position,Quaternion rotation,Bounds bounds,float stopAbove)
    {
        var capsuleBounds=new CapsuleBoundsQuery(anatomy,position,rotation);
        planningQuery++;touchedPlanningShapes.Clear();double lowerBound=0;
        bool bounded=!float.IsPositiveInfinity(stopAbove);
        bool Probe(int index)
        {
            if(visitedPlanningParts[index]==planningQuery)return false;visitedPlanningParts[index]=planningQuery;
            var part=planningParts[index];if(!part.bounds.Intersects(bounds) || !capsuleBounds.MayTouch(part.bounds))return false;
            if(!Physics.ComputePenetration(anatomy,position,rotation,part.collider,part.position,part.rotation,out _,out float depth))return false;
            float previous=planningDepths[part.owner];
            if(previous==0)touchedPlanningShapes.Add(part.owner);
            if(depth>previous)
            {
                planningDepths[part.owner]=depth;
                if(bounded)lowerBound+=(double)depth*depth-(double)previous*previous;
            }
            // Costs only increase as more native parts are examined. A
            // losing route needs a proven lower bound, not its final score.
            // The small margin keeps float accumulation at the boundary exact.
            bool rejected=bounded && lowerBound>stopAbove+1e-6*(1+Mathf.Abs(stopAbove));
            if(rejected)recentBlockingParts[anatomy]=index;
            return rejected;
        }
        Vector3Int low=Cell(bounds.min),high=Cell(bounds.max);
        for(int x=low.x;x<=high.x;x++)for(int y=low.y;y<=high.y;y++)for(int z=low.z;z<=high.z;z++)
            if(planningCells.TryGetValue(new Vector3Int(x,y,z),out var parts))for(int i=0;i<parts.Count;i++)if(Probe(parts[i]))goto Complete;
        foreach(int index in largePlanningParts)if(Probe(index))break;
        Complete:
        float cost=0;foreach(int index in touchedPlanningShapes){float depth=planningDepths[index];cost+=depth*depth;planningDepths[index]=0;}
        return cost;
    }
    float PlanningMaximum(Collider anatomy,Vector3 position,Quaternion rotation,Bounds bounds,float stopAbove=float.PositiveInfinity)
    {
        if(!float.IsPositiveInfinity(stopAbove) && RecentBlockingDepth(anatomy,position,rotation,bounds,out float depth) && depth>stopAbove)
        {RecentBlockerRejects++;return depth;}
        return PlanningMaximumOriginal(anatomy,position,rotation,bounds,stopAbove);
    }
    float PlanningMaximumOriginal(Collider anatomy,Vector3 position,Quaternion rotation,Bounds bounds,float stopAbove=float.PositiveInfinity)
    {
        var capsuleBounds=new CapsuleBoundsQuery(anatomy,position,rotation);
        planningQuery++;float maximum=0;
        bool Probe(int index)
        {
            if(visitedPlanningParts[index]==planningQuery)return false;visitedPlanningParts[index]=planningQuery;
            var part=planningParts[index];if(!part.bounds.Intersects(bounds) || !capsuleBounds.MayTouch(part.bounds))return false;
            if(Physics.ComputePenetration(anatomy,position,rotation,part.collider,part.position,part.rotation,out _,out float depth))maximum=Mathf.Max(maximum,depth);
            bool rejected=maximum>stopAbove;
            if(rejected)recentBlockingParts[anatomy]=index;
            return rejected;
        }
        Vector3Int low=Cell(bounds.min),high=Cell(bounds.max);
        for(int x=low.x;x<=high.x;x++)for(int y=low.y;y<=high.y;y++)for(int z=low.z;z<=high.z;z++)
            if(planningCells.TryGetValue(new Vector3Int(x,y,z),out var parts))
                for(int i=0;i<parts.Count;i++)if(Probe(parts[i]))return maximum;
        foreach(int index in largePlanningParts)if(Probe(index))return maximum;
        return maximum;
    }
    GameObject root;
    Vector3 previousRoot,previousTip;
    Quaternion previousRotation;
    bool attackWasActive;
    readonly Vector3[] previousAnatomy=new Vector3[5],anatomyVelocity=new Vector3[5];
    readonly HashSet<Rigidbody> pushedBodies=new HashSet<Rigidbody>();
    readonly Dictionary<Rigidbody,float> woodContactTimes=new Dictionary<Rigidbody,float>();
    float impulseTick=-1;
    int movementBudgetFrame=-1;
    float frameTravelled,frameTurned;
    readonly HashSet<Story1WreckPlank> damaged=new HashSet<Story1WreckPlank>();
    readonly HashSet<Story1WreckDebris> damagedDetails=new HashSet<Story1WreckDebris>();
    static readonly Vector3[] escapeDirections=CreateEscapeDirections();
    static readonly float[] escapeDistances={.05f,.1f,.2f,.3f,.5f,.75f,1,1.5f,2,3,5,8,12};
    static Vector3[] CreateEscapeDirections()
    {
        var directions=new List<Vector3>{Vector3.down,Vector3.up};
        for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
            if(x!=0 || y!=0 || z!=0)directions.Add(new Vector3(x,y,z).normalized);
        return directions.ToArray();
    }
    int lastEscapeBlockingVolume;
    bool ClearAt(Vector3 offset)=>ClearAtTolerance(offset,.003f);
    bool ClearAtTolerance(Vector3 offset,float tolerance)
    {
        SnapshotPlanningShapes();
        int first=lastEscapeBlockingVolume;
        for(int i=0;i<volumes.Length;i++)
        {
            // Nearby escape probes usually fail on the same anatomical part.
            // Test it first; acceptance still requires ALL original volumes
            // and keeps the exact same escape-position order and 3mm limit.
            int index=(first+i)%volumes.Length;var volume=volumes[index];
            // The same exact native parts, selected spatially, avoid scanning
            // the entire wreck for every small hypothetical escape shift.
            // One penetration over the existing3 mm limit is sufficient;
            // no collider, collision margin or final contact is relaxed.
            var bounds=anatomyBounds[volume];bounds.center+=offset;
            if(PlanningMaximum(volume,volume.transform.position+offset,volume.transform.rotation,bounds,tolerance)>tolerance)
            {lastEscapeBlockingVolume=index;return false;}
        }
        return true;
    }
    bool NearestClear(float maximumDistance,out Vector3 escape)
    {
        using var measurement=ClearSearchMarker.Auto();
        escape=Vector3.zero;if(ClearAt(Vector3.zero))return true;
        // Opposing minimum translations can oscillate in a narrow board seam.
        // Find the nearest clear placement; test hypothetical poses without
        // teleporting every collider or moving the freely floating wreck.
        foreach(float distance in escapeDistances)
        {
            if(distance>maximumDistance)break;
            foreach(var direction in escapeDirections)
            {
                Vector3 offset=direction*distance;if(ClearAt(offset)){escape=offset;return true;}
            }
        }
        // A finite movement allowance usually lies between the discrete
        // search radii (for example0.55m between0.5 and0.75). Failing at0.5
        // does not establish that the whole allowed ball is obstructed.
        // Try its actual boundary before abandoning the retained pose for
        // an emergency distant escape. Every result still needs ALL native
        // anatomy checks, and stays strictly inside the same movement limit.
        if(!float.IsPositiveInfinity(maximumDistance) && maximumDistance>.0001f)
        {
            foreach(var direction in escapeDirections)
            {
                Vector3 offset=direction*(maximumDistance-.0001f);
                if(ClearAt(offset)){escape=offset;BoundaryClearCount++;return true;}
            }
        }
        return false;
    }
    Vector3 EscapeWedge()=>NearestClear(float.PositiveInfinity,out var escape) ? escape : Vector3.down*20;

    public void Initialize(Story1WreckBattle owner,TralaleroSwimAnimator animator)
    {
        using var measurement=new Unity.Profiling.ProfilerMarker("Wreck.InitializeAnatomy").Auto();
        battle=owner;shark=animator;
        var bones=animator.BoneAnimator.GetComponentsInChildren<Transform>();
        Transform Bone(string name)=>System.Array.Find(bones,t=>t.name==name);
        head=Bone("Head");body=Bone("Body");tailBase=Bone("Tail_Base");tailTip=Bone("Tail_Tip");
        root=new GameObject("Shark anatomical wreck contact");root.transform.SetParent(transform,false);
        volumes=new Collider[5];
        planningVolumes=new CapsuleCollider[4];
        for(int i=0;i<5;i++)
        {
            var go=new GameObject(i==4 ? "Swept attack" : "Solid anatomy "+i);go.transform.SetParent(root.transform,false);
            var collider=go.AddComponent<CapsuleCollider>();collider.direction=2;collider.isTrigger=true;
            if(i<4)volumes[i]=collider;else attackVolume=collider;
        }
        for(int i=0;i<planningVolumes.Length;i++)
        {
            var go=new GameObject("Planning anatomy "+i);go.transform.SetParent(root.transform,false);go.transform.localPosition=Vector3.down*10000;
            var collider=go.AddComponent<CapsuleCollider>();collider.direction=2;collider.isTrigger=true;
            planningVolumes[i]=collider;
        }
        var reachObject=new GameObject("Planning weapon reach");reachObject.transform.SetParent(root.transform,false);
        reachObject.transform.position=Vector3.down*10000;
        planningReach=reachObject.AddComponent<CapsuleCollider>();planningReach.direction=2;planningReach.isTrigger=true;
        PrepareTailFin();
        previousRoot=shark.transform.position;previousRotation=shark.transform.rotation;
        poseBones=animator.BoneAnimator.GetComponentsInChildren<Transform>();
        retainedPositions=new Vector3[poseBones.Length];retainedRotations=new Quaternion[poseBones.Length];
        retryPositions=new Vector3[poseBones.Length];retryRotations=new Quaternion[poseBones.Length];
        intendedPositions=new Vector3[poseBones.Length];intendedRotations=new Quaternion[poseBones.Length];
        UpdateVolumes();
        for(int i=0;i<volumes.Length;i++)previousAnatomy[i]=volumes[i].transform.position;
        RetainPose();
    }
    void PrepareTailFin()
    {
        using var measurement=new Unity.Profiling.ProfilerMarker("Wreck.BuildTailFin").Auto();
        // The visible caudal fin extends beyond Tail_Tip's bone origin.
        // Preserve its original rigidly skinned vertices and bind matrix;
        // the convex shell follows that bone without per-frame mesh cooking.
        var skin=shark.VisualRenderer as SkinnedMeshRenderer;
        var source=skin.sharedMesh;int tip=System.Array.IndexOf(skin.bones,tailTip);
        var sourceVertices=source.vertices;var weights=source.boneWeights;
        // Mesh.bindposes returns a new full array. Fetch the unchanged tail
        // matrix once, avoiding repeated full-array copies during handoff.
        var tailBind=source.bindposes[tip];
        var points=new List<Vector3>();var indices=new Dictionary<int,int>();
        for(int i=0;i<sourceVertices.Length;i++)
        {
            var w=weights[i];float amount=(w.boneIndex0==tip?w.weight0:0)+(w.boneIndex1==tip?w.weight1:0)+
                (w.boneIndex2==tip?w.weight2:0)+(w.boneIndex3==tip?w.weight3:0);
            if(amount<.9999f)continue;
            indices.Add(i,points.Count);points.Add(tailBind.MultiplyPoint3x4(sourceVertices[i]));
        }
        var triangles=new List<int>();var sourceTriangles=source.triangles;
        for(int i=0;i<sourceTriangles.Length;i+=3)
            if(indices.TryGetValue(sourceTriangles[i],out int a) && indices.TryGetValue(sourceTriangles[i+1],out int b) && indices.TryGetValue(sourceTriangles[i+2],out int c))
            {triangles.Add(a);triangles.Add(b);triangles.Add(c);}
        finMesh=new Mesh{name="Original caudal skin collision"};finMesh.SetVertices(points);finMesh.SetTriangles(triangles,0);finMesh.RecalculateBounds();
        MeshCollider Create(string name)
        {
            var go=new GameObject(name);go.transform.SetParent(root.transform,false);
            var c=go.AddComponent<MeshCollider>();c.sharedMesh=finMesh;c.convex=true;c.isTrigger=true;
            Vector3 scale=tailTip.lossyScale,parent=go.transform.parent.lossyScale;
            go.transform.localScale=new Vector3(scale.x/parent.x,scale.y/parent.y,scale.z/parent.z);
            return c;
        }
        finVolume=Create("Solid caudal skin");planningFin=Create("Planning caudal skin");
        planningFin.transform.position=Vector3.down*10000;volumes[4]=finVolume;
    }
    void RetainPose()
    {
        for(int i=0;i<poseBones.Length;i++){retainedPositions[i]=poseBones[i].localPosition;retainedRotations[i]=poseBones[i].localRotation;}
        hasRetainedPose=true;
    }
    void RestoreRetainedPose()
    {
        for(int i=0;i<poseBones.Length;i++){poseBones[i].localPosition=retainedPositions[i];poseBones[i].localRotation=retainedRotations[i];}
    }
    bool TryRetainedPlacement(float maximumShift,out Vector3 placement)
    {
        using var measurement=RetainedMarker.Auto();
        Vector3 at=shark.transform.position;Quaternion heading=shark.transform.rotation;
        for(int i=0;i<poseBones.Length;i++){retryPositions[i]=poseBones[i].localPosition;retryRotations[i]=poseBones[i].localRotation;}
        RestoreRetainedPose();shark.transform.SetPositionAndRotation(previousRoot,previousRotation);UpdateVolumes();
        if(NearestClear(maximumShift,out var shift))
        {
            placement=previousRoot+shift;shark.transform.position=placement;UpdateVolumes();return true;
        }
        for(int i=0;i<poseBones.Length;i++){poseBones[i].localPosition=retryPositions[i];poseBones[i].localRotation=retryRotations[i];}
        shark.transform.SetPositionAndRotation(at,heading);UpdateVolumes();placement=at;return false;
    }
    bool TryCurrentPlacement(float maximumShift,out Vector3 placement)
    {
        // Iterative minimum translations can drift outside this frame's
        // movement ball. A search centred there is not a search of the
        // allowed ball centred on the last actual placement. Test the NEW
        // articulated pose inside that ball too; retained-pose recovery
        // alone cannot find clearances opened by the current spine motion.
        Vector3 at=shark.transform.position;
        shark.transform.position=previousRoot;UpdateVolumes();
        if(NearestClear(maximumShift,out var shift))
        {
            placement=previousRoot+shift;shark.transform.position=placement;UpdateVolumes();return true;
        }
        shark.transform.position=at;UpdateVolumes();placement=at;return false;
    }
    bool TryPoseAdvance(Vector3 desired,Quaternion heading,int rootSteps)
    {
        using var measurement=AdaptivePoseMarker.Auto();
        if(!hasRetainedPose)return false;
        Vector3 rawRoot=shark.transform.position;Quaternion rawHeading=shark.transform.rotation;
        float angularTravel=0;
        for(int i=0;i<poseBones.Length;i++)
        {
            intendedPositions[i]=poseBones[i].localPosition;intendedRotations[i]=poseBones[i].localRotation;
            angularTravel+=Quaternion.Angle(retainedRotations[i],intendedRotations[i]);
        }
        int steps=Mathf.Clamp(Mathf.Max(rootSteps,Mathf.CeilToInt(angularTravel/7)),1,32);
        for(int attempt=0;attempt<7;attempt++)
        {
            // Preserve all four original full-distance attempts first. If
            // wood blocks that destination, reducing only the bone motion
            // cannot help. Advance a shorter portion of BOTH the root path
            // and the articulated pose before resorting to seam depenetration.
            // The live mesh, all anatomy and the tolerance remain unchanged.
            float rootFraction=attempt<4 ? 1 : 1-(attempt-3)*.25f;
            float fraction=attempt<4 ? 1-attempt*.25f : rootFraction;bool clear=true;
            int sampledSteps=Mathf.Max(1,Mathf.CeilToInt(steps*rootFraction));
            for(int step=1;step<=sampledSteps;step++)
            {
                float time=(float)step/sampledSteps,weight=time*fraction;
                for(int i=0;i<poseBones.Length;i++)
                {
                    poseBones[i].localPosition=Vector3.Lerp(retainedPositions[i],intendedPositions[i],weight);
                    poseBones[i].localRotation=Quaternion.Slerp(retainedRotations[i],intendedRotations[i],weight);
                }
                shark.transform.SetPositionAndRotation(Vector3.Lerp(previousRoot,desired,time*rootFraction),Quaternion.Slerp(previousRotation,heading,time*rootFraction));
                UpdateVolumes();
                // Stricter than the existing 1mm translation-pass trigger.
                // Every accepted sampled pose uses ALL native anatomy parts.
                if(!ClearAtTolerance(Vector3.zero,.00099f)){clear=false;break;}
            }
            if(clear)
            {
                PoseAdvanceCount++;if(attempt>0)PartialPoseAdvanceCount++;
                if(attempt>=4)PartialRootAdvanceCount++;
                return true;
            }
        }
        // No clear articulated advance: keep the original impulse/contact
        // solver, with precisely the requested pose restored before it runs.
        for(int i=0;i<poseBones.Length;i++)
        {poseBones[i].localPosition=intendedPositions[i];poseBones[i].localRotation=intendedRotations[i];}
        shark.transform.SetPositionAndRotation(rawRoot,rawHeading);UpdateVolumes();return false;
    }
    static void Segment(CapsuleCollider collider,Vector3 a,Vector3 b,float radius)
    {
        Vector3 along=b-a;
        collider.transform.SetPositionAndRotation((a+b)*.5f,along.sqrMagnitude>.00001f ? Quaternion.LookRotation(along) : Quaternion.identity);
        collider.radius=radius;collider.height=along.magnitude+2*radius;
    }
    void UpdateVolumes()
    {
        using var measurement=VolumeMarker.Auto();
        float radius=Mathf.Clamp(shark.RootMeshBounds.extents.x*Mathf.Abs(shark.transform.lossyScale.x)*.48f,1.2f,2);
        // NoseWorldPoint is the front skin surface, not a capsule centre.
        // Starting the round end there projected an invisible radius through
        // deck edges before the visible shark had actually touched them.
        float headRadius=radius*.45f;
        Vector3 nose=shark.NoseWorldPoint;
        Segment((CapsuleCollider)volumes[0],nose+(head.position-nose).normalized*headRadius,head.position,headRadius);
        Segment((CapsuleCollider)volumes[1],head.position,body.position,radius);
        Segment((CapsuleCollider)volumes[2],body.position,tailBase.position,radius*.7f);
        Segment((CapsuleCollider)volumes[3],tailBase.position,tailTip.position,radius*.32f);
        finVolume.transform.SetPositionAndRotation(tailTip.position,tailTip.rotation);
        foreach(var volume in volumes)
        {
            if(volume is CapsuleCollider capsule)
            {
                Story1CapsuleContact.Segment(capsule,out var a,out var b,out float r);
                Vector3 half=(b-a)*.5f;
                anatomyBounds[volume]=new Bounds((a+b)*.5f,2*(new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z))+Vector3.one*r));
            }
            else anatomyBounds[volume]=FinBounds(tailTip.position,tailTip.rotation,tailTip.lossyScale);
        }
        solidBounds=anatomyBounds[volumes[0]];
        for(int i=1;i<volumes.Length;i++)solidBounds.Encapsulate(anatomyBounds[volumes[i]]);
    }
    bool MayOverlapSolid(Story1WreckMeshCollision shape)
    {
        // Each individual overlap already rejects this exact shape AABB.
        // Their union can reject distant wood once instead of querying all
        // five anatomy volumes for it on every iterative solver pass. The
        // union is rebuilt after EVERY correction; order and native narrow
        // phase tests for nearby wood remain unchanged.
        if(shape==null)return false;
        SnapshotPlanningShapes();
        return planningShapeIndices.TryGetValue(shape,out int index) && planningShapes[index].bounds.Intersects(solidBounds);
    }
    public float PoseObstruction(Vector3 marker,Quaternion rotation,bool tail)
    {
        return PoseObstruction(CaptureAnatomy(),marker,rotation,tail);
    }
    public float PoseObstruction(AnatomyPose pose,Vector3 marker,Quaternion rotation,bool tail)
        =>PoseObstructionBounded(pose,marker,rotation,tail,float.PositiveInfinity);
    CapsuleCollider PlanningCapsule(int index,float radius,float height)
    {
        // Authored path poses are queried at many headings. Reusing one
        // mutable capsule recooks its PhysX geometry every time the search
        // alternates between those poses. Cache the EXACT dimensions, never
        // a rounded length or a smaller collision shell. Explicit transforms
        // still place every query against this frame's actual wreck snapshot.
        var key=new Vector2(radius,height);var cache=planningGeometry[index];
        if(cache.TryGetValue(key,out var found)){PlanningGeometryReuses++;return found;}
        if(cache.Count<32)
        {
            var go=new GameObject("Cached planning anatomy "+index+" / "+cache.Count);
            go.transform.SetParent(root.transform,false);go.transform.localPosition=Vector3.down*10000;
            var shape=go.AddComponent<CapsuleCollider>();shape.direction=2;shape.isTrigger=true;
            shape.radius=radius;shape.height=height;cache.Add(key,shape);PlanningGeometryCreated++;return shape;
        }
        // Live poses may have many distinct lengths. Keep memory bounded
        // and use the original mutable query geometry for uncached values.
        var fallback=planningVolumes[index];
        if(fallback.radius!=radius)fallback.radius=radius;
        if(fallback.height!=height)fallback.height=height;
        return fallback;
    }
    // Exact below the limit; otherwise returns a lower bound already above it.
    // Never use a rejected lower bound to rank or accept a different route.
    public float PoseObstructionBounded(AnatomyPose pose,Vector3 marker,Quaternion rotation,bool tail,float maximumCost)
    {
        using var measurement=PlanningMarker.Auto();
        PlanningQueries++;SnapshotPlanningShapes();
        // A pose is sampled once and can be tested at many approach attitudes.
        // Hypothetical geometry must not repeatedly evaluate the Animator or
        // move the rendered rig while searching a collision-free route.
        Vector3 scale=shark.transform.lossyScale;
        Vector3 at=marker-rotation*Vector3.Scale(tail ? pose.tailMarker : pose.nose,scale);
        Vector3 World(Vector3 local)=>at+rotation*Vector3.Scale(local,scale);
        float radius=Mathf.Clamp(shark.RootMeshBounds.extents.x*Mathf.Abs(scale.x)*.48f,1.2f,2),headRadius=radius*.45f;
        Vector3 nose=World(pose.nose),headPoint=World(pose.head),bodyPoint=World(pose.body),basePoint=World(pose.tailBase),tipPoint=World(pose.tailTip);
        float cost=0;
        void Grade(int index,Vector3 a,Vector3 b,float capsuleRadius,float exactLength)
        {
            // ComputePenetration accepts an explicit hypothetical transform.
            // Searching attitudes must not move/resize the live solid anatomy
            // thousands of times or invalidate its physics broadphase.
            Vector3 along=b-a,position=(a+b)*.5f;
            Quaternion facing=along.sqrMagnitude>.00001f ? Quaternion.LookRotation(along) : Quaternion.identity;
            // Rotation and a distant world origin must not perturb the
            // capsule's length and repeatedly rebuild its PhysX geometry.
            float height=exactLength+2*capsuleRadius;
            var volume=PlanningCapsule(index,capsuleRadius,height);
            Vector3 halfAxis=along*.5f;
            Bounds bounds=new Bounds(position,2*(new Vector3(Mathf.Abs(halfAxis.x),Mathf.Abs(halfAxis.y),Mathf.Abs(halfAxis.z))+Vector3.one*capsuleRadius));
            cost+=PlanningCostBounded(volume,position,facing,bounds,maximumCost-cost);
        }
        Grade(0,nose+(headPoint-nose).normalized*headRadius,headPoint,headRadius,Mathf.Abs(Vector3.Scale(pose.head-pose.nose,scale).magnitude-headRadius));
        if(cost>maximumCost)return cost;
        Grade(1,headPoint,bodyPoint,radius,Vector3.Scale(pose.body-pose.head,scale).magnitude);
        if(cost>maximumCost)return cost;
        Grade(2,bodyPoint,basePoint,radius*.7f,Vector3.Scale(pose.tailBase-pose.body,scale).magnitude);
        if(cost>maximumCost)return cost;
        Grade(3,basePoint,tipPoint,radius*.32f,Vector3.Scale(pose.tailTip-pose.tailBase,scale).magnitude);
        if(cost>maximumCost)return cost;
        Vector3 finAt=World(pose.finPosition);Quaternion finFacing=rotation*pose.finRotation;
        var finBounds=FinBounds(finAt,finFacing,planningFin.transform.lossyScale);
        cost+=PlanningCostBounded(planningFin,finAt,finFacing,finBounds,maximumCost-cost);
        return cost;
    }
    Bounds FinBounds(Vector3 position,Quaternion rotation,Vector3 scale)
    {
        Vector3 half=Vector3.Scale(finMesh.bounds.extents,scale);
        Vector3 x=rotation*new Vector3(half.x,0,0),y=rotation*new Vector3(0,half.y,0),z=rotation*new Vector3(0,0,half.z);
        var extents=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
        return new Bounds(position+rotation*Vector3.Scale(finMesh.bounds.center,scale),extents*2);
    }
    static float PlanningPenetration(CapsuleCollider capsule,Vector3 position,Quaternion rotation,Bounds bounds,Story1WreckMeshCollision shape)
    {
        if(shape==null || !shape.Active || !shape.WorldBounds.Intersects(bounds))return 0;
        float depth=0;
        for(int i=0;i<shape.Parts.Length;i++)
        {
            if(!shape.PartBounds(i).Intersects(bounds))continue;
            if(Physics.ComputePenetration(capsule,position,rotation,shape.Parts[i],shape.PartPosition(i),shape.PartRotation(i),out _,out var penetration))
                depth=Mathf.Max(depth,penetration);
        }
        return depth*depth;
    }
    bool Overlap(Collider anatomy,Story1WreckPlank board,out Vector3 direction,out float depth)
    {
        direction=Vector3.zero;depth=0;
        if(board==null || board.IsBroken || !board.gameObject.activeInHierarchy || board.Deck==null || !board.Deck.enabled || Time.time<board.SharkContactAfter)return false;
        return OverlapShape(anatomy,board.Collision,Vector3.zero,out direction,out depth);
    }
    bool OverlapShape(Collider anatomy,Story1WreckMeshCollision shape,Vector3 offset,out Vector3 direction,out float depth)
    {
        using var measurement=OverlapMarker.Auto();
        direction=Vector3.zero;depth=0;
        if(shape==null)return false;
        SnapshotPlanningShapes();
        if(!planningShapeIndices.TryGetValue(shape,out int shapeIndex))return false;
        Bounds bounds;
        if(!anatomyBounds.TryGetValue(anatomy,out bounds))
        {
            Story1CapsuleContact.Segment((CapsuleCollider)anatomy,out var a,out var b,out float r);
            Vector3 half=(b-a)*.5f;bounds=new Bounds((a+b)*.5f,2*(new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z))+Vector3.one*r));
        }
        bounds.center+=offset;var candidate=planningShapes[shapeIndex];
        if(!candidate.bounds.Intersects(bounds))return false;
        Vector3 position=anatomy.transform.position+offset;Quaternion rotation=anatomy.transform.rotation;
        var capsuleBounds=new CapsuleBoundsQuery(anatomy,position,rotation);
        for(int i=candidate.first;i<candidate.first+candidate.count;i++)
        {
            var part=planningParts[i];if(!part.bounds.Intersects(bounds) || !capsuleBounds.MayTouch(part.bounds))continue;
            if(Physics.ComputePenetration(anatomy,position,rotation,
                part.collider,part.position,part.rotation,out var normal,out var penetration) && penetration>depth)
            {direction=normal;depth=penetration;}
        }
        return depth>0;
    }
    void DamageSweptAttack()
    {
        bool active=battle.BoardAttackActive;
        Vector3 tip=battle.TailBoardAttack ? shark.TailWorldPoint : shark.NoseWorldPoint;
        if(active)
        {
            if(!attackWasActive){damaged.Clear();damagedDetails.Clear();previousTip=tip;}
            Segment(attackVolume,previousTip,tip,battle.TailBoardAttack ? 1.8f : 1.1f);
            int count=battle.Boards.Count;
            for(int i=0;i<count;i++)
            {
                var board=battle.Boards[i];
                if(damaged.Contains(board) || !Overlap(attackVolume,board,out var normal,out var depth))continue;
                damaged.Add(board);
                Vector3 point=board.Collision.ClosestPoint(tip);
                Vector3 force=(tip-previousTip).normalized*3+Vector3.down*1.2f+shark.transform.forward*2;
                if(battle.DamageBoard(board,battle.TailBoardAttack ? 70 : 50,point,force))planningFrame=-1;
            }
            count=battle.WreckDetails.Count;
            for(int i=0;i<count;i++)
            {
                var piece=battle.WreckDetails[i];
                if(piece.FractureSource==null || damagedDetails.Contains(piece) || !OverlapShape(attackVolume,piece.Collision,Vector3.zero,out var normal,out var depth))continue;
                damagedDetails.Add(piece);
                if(battle.DamageWreckDetail(piece,battle.TailBoardAttack ? 70 : 50,piece.Collision.ClosestPoint(tip),shark.transform.forward*3+Vector3.down))planningFrame=-1;
            }
        }
        attackWasActive=active;previousTip=tip;
    }
    void LateUpdate()=>ResolvePose();
    void PushFloatingWood(int anatomy,Rigidbody timber,Story1WreckMeshCollision shape,Vector3 normal,float depth)
    {
        if(timber==null || timber.isKinematic || !pushedBodies.Add(timber))return;
        Vector3 point=shape.ClosestPoint(volumes[anatomy].transform.position);
        float closing=Mathf.Max(0,Vector3.Dot(anatomyVelocity[anatomy]-timber.GetPointVelocity(point),-normal));
        float correction=Mathf.Min(2,depth*.2f/Mathf.Max(.02f,Time.deltaTime));
        // A large animal transfers momentum to freely floating timber. Position
        // correction still keeps its anatomy solid; the physics engine moves wood.
        const float animalMass=1500;
        float reducedMass=timber.mass*animalMass/(timber.mass+animalMass);
        // Support protection controls fracture, not the physical momentum of
        // the same free rigidbody. Its passenger follows that actual motion.
        float limit=timber.mass>=30 ? 2.5f : 6;
        float separating=Mathf.Max(0,Vector3.Dot(timber.GetPointVelocity(point),-normal));
        float impulseSpeed=Mathf.Max(0,Mathf.Min(limit,closing+correction)-separating);
        bool entering=!woodContactTimes.TryGetValue(timber,out float lastContact) || Time.time-lastContact>.25f;
        woodContactTimes[timber]=Time.time;
        // A held contact is pressure over time, not a fresh collision every
        // physics step. Repeating the full onset impulse drove occupied decks
        // metres underwater, then launched them when the shark moved away.
        float duration=entering ? 1 : Mathf.Clamp(Time.time-lastContact,0,.1f);
        timber.AddForceAtPosition(-normal*(impulseSpeed*reducedMass*duration),point,ForceMode.Impulse);
    }
    public void ResolvePose()
    {
        using var measurement=SolidMarker.Auto();
        if(shark==null || battle.CurrentPhase==Story1WreckBattle.Phase.Breaking || battle.CurrentPhase==Story1WreckBattle.Phase.Finished)return;
        if(PauseSettingsMenu.IsOpen || Time.deltaTime<=0)
        {
            // The cinematic re-samples poses while paused; keep the last solved
            // world placement too, rather than restoring its unblocked target.
            shark.transform.SetPositionAndRotation(previousRoot,previousRotation);UpdateVolumes();return;
        }
        Vector3 intended=shark.transform.position;Quaternion intendedRotation=shark.transform.rotation;
        // A crossed timeline contact samples this solver before LateUpdate.
        // Those two calls share ONE frame's movement allowance.
        if(movementBudgetFrame!=Time.frameCount)
        {movementBudgetFrame=Time.frameCount;frameTravelled=frameTurned=0;}
        float frameTranslation=24*Mathf.Max(.01f,Time.deltaTime)+.15f;
        float travelBudget=Mathf.Max(0,frameTranslation-frameTravelled);
        float correctionAllowance=Mathf.Min(.15f,Mathf.Max(0,frameTranslation+.15f-frameTravelled-travelBudget));
        Vector3 desired=previousRoot+Vector3.ClampMagnitude(intended-previousRoot,travelBudget);
        Quaternion desiredRotation=Quaternion.RotateTowards(previousRotation,intendedRotation,Mathf.Max(0,300*Mathf.Max(.01f,Time.deltaTime)+3-frameTurned));
        LastObstacles.Clear();
        if(impulseTick!=Time.fixedTime){impulseTick=Time.fixedTime;pushedBodies.Clear();}
        UpdateVolumes();
        for(int i=0;i<volumes.Length;i++)anatomyVelocity[i]=Vector3.ClampMagnitude((volumes[i].transform.position-previousAnatomy[i])/Mathf.Max(.02f,Time.deltaTime),14);
        int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(previousRoot,desired)/.45f,
            Quaternion.Angle(previousRotation,desiredRotation)/7)),1,32);
        Vector3 resolved=previousRoot;
        using(SweepMarker.Auto())
        {
        bool advanced=TryPoseAdvance(desired,desiredRotation,steps);
        if(advanced){resolved=shark.transform.position;desiredRotation=shark.transform.rotation;}
        else
        {
        for(int step=1;step<=steps;step++)
        {
            Quaternion rotation=Quaternion.Slerp(previousRotation,desiredRotation,(float)step/steps);
            resolved+=(desired-previousRoot)/steps;
            shark.transform.SetPositionAndRotation(resolved,rotation);UpdateVolumes();
            for(int pass=0;pass<8;pass++)
            {
                bool blocked=false;int count=battle.Boards.Count;
                for(int i=0;i<count;i++)
                {
                    var board=battle.Boards[i];
                    if(board==null || !MayOverlapSolid(board.Collision))continue;
                    for(int n=0;n<volumes.Length;n++)
                    {
                        if(!Overlap(volumes[n],board,out var direction,out float depth) || depth<.001f)continue;
                        PushFloatingWood(n,board.GetComponent<Rigidbody>(),board.Collision,direction,depth);
                        if(battle.CurrentPhase==Story1WreckBattle.Phase.Finale && battle.Finale.CanBreakBoards && !battle.Finale.ProtectsBoard(board) && board.FractureSource!=null)
                        {
                            if(battle.DamageBoard(board,100,board.Collision.ClosestPoint(volumes[n].transform.position),shark.transform.forward*5+Vector3.up*2)){planningFrame=-1;break;}
                        }
                        blocked=true;BlockedContactCount++;
                        if(LastObstacles.Count<12)LastObstacles.Add(board.name+" anatomy="+n+" depth="+depth.ToString("F3")+" size="+board.Dimensions);
                        resolved+=direction*(depth+.025f);
                        shark.transform.position=resolved;UpdateVolumes();
                    }
                }
                count=battle.WreckDetails.Count;
                for(int i=0;i<count;i++)
                {
                    var piece=battle.WreckDetails[i];
                    if(piece==null || !MayOverlapSolid(piece.Collision))continue;
                    for(int n=0;n<volumes.Length;n++)
                    {
                        if(!OverlapShape(volumes[n],piece.Collision,Vector3.zero,out var direction,out float depth) || depth<.001f)continue;
                        PushFloatingWood(n,piece.GetComponent<Rigidbody>(),piece.Collision,direction,depth);
                        if(piece.FractureSource!=null && battle.CurrentPhase==Story1WreckBattle.Phase.Finale && battle.Finale.CanBreakBoards)
                        {
                            if(battle.DamageWreckDetail(piece,100,piece.Collision.ClosestPoint(volumes[n].transform.position),shark.transform.forward*5+Vector3.up*2)){planningFrame=-1;break;}
                        }
                        blocked=true;BlockedContactCount++;resolved+=direction*(depth+.025f);
                        if(LastObstacles.Count<12)LastObstacles.Add(piece.name+" anatomy="+n+" depth="+depth.ToString("F3"));
                        shark.transform.position=resolved;UpdateVolumes();
                    }
                }
                if(!blocked)break;
            }
        }
        }
        }
        shark.transform.SetPositionAndRotation(resolved,desiredRotation);UpdateVolumes();
        using(EscapeMarker.Auto())
        {
        Vector3 escape=Vector3.zero;bool held=false;
        bool boundedRetainedFailed=false;
        bool TryBoundedRetained(out Vector3 placement)
        {
            // Neither physics nor the retained bones change inside this
            // solve. A failed search of the identical retained pose and
            // radius remains a failure after testing a distant new-pose
            // escape. Avoid doing that full native search a second time.
            if(boundedRetainedFailed){RetainedFailureReuses++;placement=default;return false;}
            bool found=TryRetainedPlacement(travelBudget+correctionAllowance,out placement);
            boundedRetainedFailed=!found;return found;
        }
        // Bound the new-pose escape search by the allowed root travel.
        // Prefer a clear previous actual pose before the exhaustive search;
        // opposing translations must not cause a distant whole-animal jump.
        float nearRadius=Vector3.Distance(resolved,previousRoot)+travelBudget+correctionAllowance+.01f;
        bool clear=ClearAt(Vector3.zero);
        // A clear previous actual pose is preferable to an exhaustive escape
        // after opposing translations have wedged the raw pose in a seam.
        if(!clear && hasRetainedPose && TryRetainedPlacement(0,out var previousPlacement))
        {resolved=previousPlacement;desiredRotation=previousRotation;held=true;ConstraintHoldCount++;}
        if(!clear && !held)clear=NearestClear(nearRadius,out escape);
        bool outsideBudget=Vector3.Distance(resolved+escape,previousRoot)>travelBudget+correctionAllowance;
        if((!clear || outsideBudget) && !held && hasRetainedPose && TryBoundedRetained(out var supported))
        {resolved=supported;desiredRotation=previousRotation;held=true;escape=Vector3.zero;ConstraintHoldCount++;}
        if((!clear || outsideBudget) && !held && TryCurrentPlacement(travelBudget+correctionAllowance,out var recentered))
        {resolved=recentered;clear=true;escape=Vector3.zero;CurrentPoseRecenterCount++;}
        if(!clear && !held)escape=EscapeWedge();
        resolved+=escape;
        if(escape.sqrMagnitude>0){shark.transform.position=resolved;UpdateVolumes();}
        if(Vector3.Distance(resolved,previousRoot)>travelBudget+correctionAllowance && hasRetainedPose)
        {
            // A narrow seam can make iterative minimum translations oscillate
            // then choose a distant empty location. Preserve the previous
            // actual anatomical pose when it is still clear instead of
            // jumping the whole animal to that distant escape point.
            Vector3 rejected=resolved;
            if(TryBoundedRetained(out var retainedPlacement))
            {
                resolved=retainedPlacement;desiredRotation=previousRotation;ConstraintHoldCount++;
            }
            else
            {
                resolved=rejected;LargeEscapeCount++;
                if(LargeEscapes.Count<24)LargeEscapes.Add(battle.CurrentPhase+" film="+(battle.Finale!=null ? battle.Finale.CinematicTime : -1)+" delta="+Vector3.Distance(previousRoot,rejected)+" frame="+Time.deltaTime+" root="+rejected+" escape="+escape);
            }
        }
        }
        // Damage uses the solved anatomical pose, so wood cannot conceal a
        // raw desired pose hitting Sahur or a board on the other side of an obstacle.
        using(DamageMarker.Auto()){DamageSweptAttack();battle.ApplySharkContactDamage();}
        float residual=CurrentPenetration();
        MaximumResidualPenetration=Mathf.Max(MaximumResidualPenetration,residual);
        frameTravelled+=Vector3.Distance(previousRoot,resolved);frameTurned+=Quaternion.Angle(previousRotation,desiredRotation);
        previousRoot=resolved;previousRotation=desiredRotation;
        for(int i=0;i<volumes.Length;i++)previousAnatomy[i]=volumes[i].transform.position;
        RetainPose();LastCorrection=resolved-intended;
    }
    public float CurrentPenetration()
    {
        using var measurement=ResidualMarker.Auto();
        UpdateVolumes();SnapshotPlanningShapes();float result=0;
        foreach(var volume in volumes)
            result=Mathf.Max(result,PlanningMaximum(volume,volume.transform.position,volume.transform.rotation,anatomyBounds[volume]));
        return result;
    }
    public bool CapsuleTranslationClear(CapsuleCollider shape,Vector3 from,Vector3 to)
    {
        SnapshotPlanningShapes();
        Story1CapsuleContact.Segment(shape,out var a,out var b,out float radius);
        Vector3 half=(b-a)*.5f;
        // Inflate ONLY the separate rejection probe, never the held weapon
        // or its damage collider. Half a sample interval contains every
        // intervening linear translation, including thin timber between taps.
        float probeRadius=radius+Vector3.Distance(from,to)/8+.003f;
        planningReach.radius=probeRadius;planningReach.height=(b-a).magnitude+2*probeRadius;
        Quaternion facing=(b-a).sqrMagnitude>.000001f ? Quaternion.LookRotation(b-a) : Quaternion.identity;
        Vector3 centre=(a+b)*.5f;
        var finalBounds=new Bounds(centre,2*(new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z))+Vector3.one*probeRadius));
        Vector3 finalPosition=shape.transform.position;
        // The wrist retains its authored world rotation; test the actual
        // translated capsule against the same native wreck geometry.
        for(int sample=0;sample<=4;sample++)
        {
            Vector3 position=Vector3.Lerp(from,to,sample*.25f);var bounds=finalBounds;bounds.center+=position-finalPosition;
            if(PlanningMaximum(planningReach,centre+position-finalPosition,facing,bounds,.003f)>.003f)return false;
        }
        return true;
    }
    public bool CameraClearance(Vector3 point,float clearance=2)
    {
        foreach(var volume in volumes)
        {
            if(volume is CapsuleCollider capsule)
            {
                Story1CapsuleContact.Segment(capsule,out var a,out var b,out float r);Vector3 axis=b-a;
                Vector3 nearest=a+axis*Mathf.Clamp01(Vector3.Dot(point-a,axis)/Mathf.Max(.000001f,axis.sqrMagnitude));
                if((point-nearest).sqrMagnitude<(r+clearance)*(r+clearance))return false;
            }
            else if(anatomyBounds[volume].SqrDistance(point)<clearance*clearance)return false;
        }
        return true;
    }
    void OnDestroy(){if(root!=null)Destroy(root);if(finMesh!=null)Destroy(finMesh);}
}
