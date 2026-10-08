using System.Collections.Generic;
using UnityEngine;

/// <summary>Solid anatomical contact for a shark whose animation owns its world movement.</summary>
[DefaultExecutionOrder(1200)]
public sealed class Story1SharkWreckContact : MonoBehaviour
{
    public int BlockedContactCount { get; private set; }
    public float MaximumResidualPenetration { get; private set; }
    public Vector3 LastCorrection { get; private set; }
    public int ConstraintHoldCount { get; private set; }
    public int LargeEscapeCount { get; private set; }
    public readonly List<string> LargeEscapes=new List<string>();
    Transform[] poseBones;
    Vector3[] retainedPositions;
    Quaternion[] retainedRotations;
    bool hasRetainedPose;
    public readonly List<string> LastObstacles=new List<string>();
    public CapsuleCollider HeadVolume=>volumes!=null ? volumes[0] : null;
    public Vector3 BulkBodyPoint=>body!=null ? body.position : shark.transform.position;
    Story1WreckBattle battle;
    TralaleroSwimAnimator shark;
    Transform head,body,tailBase,tailTip;
    CapsuleCollider[] volumes;
    CapsuleCollider attackVolume;
    GameObject root;
    Vector3 previousRoot,previousTip;
    Quaternion previousRotation;
    bool attackWasActive;
    readonly Vector3[] previousAnatomy=new Vector3[4],anatomyVelocity=new Vector3[4];
    readonly HashSet<Rigidbody> pushedBodies=new HashSet<Rigidbody>();
    float impulseTick=-1;
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
    bool ClearAt(Vector3 offset)
    {
        foreach(var volume in volumes)
        {
            foreach(var board in battle.Boards)
            {
                if(board.IsBroken || Time.time<board.SharkContactAfter)continue;
                if(OverlapShape(volume,board.Collision,offset,out var normal,out var depth) && depth>.003f)return false;
            }
            foreach(var piece in battle.WreckDetails)
            {
                if(OverlapShape(volume,piece.Collision,offset,out var normal,out var depth) && depth>.003f)return false;
            }
        }
        return true;
    }
    bool NearestClear(float maximumDistance,out Vector3 escape)
    {
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
        return false;
    }
    Vector3 EscapeWedge()=>NearestClear(float.PositiveInfinity,out var escape) ? escape : Vector3.down*20;

    public void Initialize(Story1WreckBattle owner,TralaleroSwimAnimator animator)
    {
        battle=owner;shark=animator;
        var bones=animator.BoneAnimator.GetComponentsInChildren<Transform>();
        Transform Bone(string name)=>System.Array.Find(bones,t=>t.name==name);
        head=Bone("Head");body=Bone("Body");tailBase=Bone("Tail_Base");tailTip=Bone("Tail_Tip");
        root=new GameObject("Shark anatomical wreck contact");root.transform.SetParent(transform,false);
        volumes=new CapsuleCollider[4];
        for(int i=0;i<5;i++)
        {
            var go=new GameObject(i==4 ? "Swept attack" : "Solid anatomy "+i);go.transform.SetParent(root.transform,false);
            var collider=go.AddComponent<CapsuleCollider>();collider.direction=2;collider.isTrigger=true;
            if(i<4)volumes[i]=collider;else attackVolume=collider;
        }
        previousRoot=shark.transform.position;previousRotation=shark.transform.rotation;
        poseBones=animator.BoneAnimator.GetComponentsInChildren<Transform>();
        retainedPositions=new Vector3[poseBones.Length];retainedRotations=new Quaternion[poseBones.Length];
        UpdateVolumes();
        for(int i=0;i<volumes.Length;i++)previousAnatomy[i]=volumes[i].transform.position;
        RetainPose();
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
    static void Segment(CapsuleCollider collider,Vector3 a,Vector3 b,float radius)
    {
        Vector3 along=b-a;
        collider.transform.SetPositionAndRotation((a+b)*.5f,along.sqrMagnitude>.00001f ? Quaternion.LookRotation(along) : Quaternion.identity);
        collider.radius=radius;collider.height=along.magnitude+2*radius;
    }
    void UpdateVolumes()
    {
        float radius=Mathf.Clamp(shark.RootMeshBounds.extents.x*Mathf.Abs(shark.transform.lossyScale.x)*.48f,1.2f,2);
        // NoseWorldPoint is the front skin surface, not a capsule centre.
        // Starting the round end there projected an invisible radius through
        // deck edges before the visible shark had actually touched them.
        float headRadius=radius*.45f;
        Vector3 nose=shark.NoseWorldPoint;
        Segment(volumes[0],nose+(head.position-nose).normalized*headRadius,head.position,headRadius);
        Segment(volumes[1],head.position,body.position,radius);
        Segment(volumes[2],body.position,tailBase.position,radius*.7f);
        Segment(volumes[3],tailBase.position,tailTip.position,radius*.32f);
    }
    public float PoseObstruction(Vector3 marker,Quaternion rotation,bool tail)
    {
        Vector3 at=shark.transform.position;Quaternion facing=shark.transform.rotation;
        Vector3 local=shark.transform.InverseTransformPoint(tail ? shark.TailWorldPoint : shark.NoseWorldPoint);
        shark.transform.SetPositionAndRotation(marker-rotation*Vector3.Scale(local,shark.transform.lossyScale),rotation);UpdateVolumes();
        float cost=0;
        foreach(var volume in volumes)
        {
            foreach(var board in battle.Boards)
                if(Overlap(volume,board,out var normal,out var depth))cost+=depth*depth;
            foreach(var piece in battle.WreckDetails)
                if(OverlapShape(volume,piece.Collision,Vector3.zero,out var normal,out var depth))cost+=depth*depth;
        }
        shark.transform.SetPositionAndRotation(at,facing);UpdateVolumes();return cost;
    }
    static bool Overlap(Collider anatomy,Story1WreckPlank board,out Vector3 direction,out float depth)
    {
        direction=Vector3.zero;depth=0;
        if(board==null || board.IsBroken || !board.gameObject.activeInHierarchy || board.Deck==null || !board.Deck.enabled || Time.time<board.SharkContactAfter)return false;
        return OverlapShape(anatomy,board.Collision,Vector3.zero,out direction,out depth);
    }
    static bool OverlapShape(Collider anatomy,Story1WreckMeshCollision shape,Vector3 offset,out Vector3 direction,out float depth)
    {
        direction=Vector3.zero;depth=0;
        if(shape==null || !shape.Active)return false;
        var capsule=anatomy as CapsuleCollider;
        var anatomyScale=anatomy.transform.lossyScale;
        float reach=capsule!=null ? capsule.height*.5f*Mathf.Max(anatomyScale.x,Mathf.Max(anatomyScale.y,anatomyScale.z)) : float.PositiveInfinity;
        Vector3 anatomyCenter=anatomy.transform.TransformPoint(capsule!=null ? capsule.center : Vector3.zero)+offset;
        float reachSquared=reach*reach;
        if(shape.WorldBounds.SqrDistance(anatomyCenter)>reachSquared)return false;
        for(int i=0;i<shape.Parts.Length;i++)
        {
            var part=shape.Parts[i];
            // Geometry follows the real moving rigidbody, cached once per
            // rendered frame. Hypothetical shark poses never sync physics.
            if(shape.PartBounds(i).SqrDistance(anatomyCenter)>reachSquared)continue;
            if(Physics.ComputePenetration(anatomy,anatomy.transform.position+offset,anatomy.transform.rotation,
                part,shape.PartPosition(i),shape.PartRotation(i),out var normal,out var penetration) && penetration>depth)
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
                battle.DamageBoard(board,battle.TailBoardAttack ? 70 : 50,point,force);
            }
            count=battle.WreckDetails.Count;
            for(int i=0;i<count;i++)
            {
                var piece=battle.WreckDetails[i];
                if(piece.FractureSource==null || damagedDetails.Contains(piece) || !OverlapShape(attackVolume,piece.Collision,Vector3.zero,out var normal,out var depth))continue;
                damagedDetails.Add(piece);
                battle.DamageWreckDetail(piece,battle.TailBoardAttack ? 70 : 50,piece.Collision.ClosestPoint(tip),shark.transform.forward*3+Vector3.down);
            }
        }
        attackWasActive=active;previousTip=tip;
    }
    void LateUpdate()=>ResolvePose();
    void PushFloatingWood(int anatomy,Rigidbody timber,Story1WreckMeshCollision shape,Vector3 normal,float depth,bool occupied=false)
    {
        if(timber==null || timber.isKinematic || !pushedBodies.Add(timber))return;
        Vector3 point=shape.ClosestPoint(volumes[anatomy].transform.position);
        float closing=Mathf.Max(0,Vector3.Dot(anatomyVelocity[anatomy]-timber.GetPointVelocity(point),-normal));
        float correction=Mathf.Min(2,depth*.2f/Mathf.Max(.02f,Time.deltaTime));
        // A large animal transfers momentum to freely floating timber. Position
        // correction still keeps its anatomy solid; the physics engine moves wood.
        const float animalMass=1500;
        float reducedMass=timber.mass*animalMass/(timber.mass+animalMass);
        float limit=occupied ? 1 : timber.mass>=30 ? 2.5f : 6;
        float separating=Mathf.Max(0,Vector3.Dot(timber.GetPointVelocity(point),-normal));
        float impulseSpeed=Mathf.Max(0,Mathf.Min(limit,closing+correction)-separating);
        timber.AddForceAtPosition(-normal*(impulseSpeed*reducedMass),point,ForceMode.Impulse);
    }
    public void ResolvePose()
    {
        if(shark==null || battle.CurrentPhase==Story1WreckBattle.Phase.Breaking || battle.CurrentPhase==Story1WreckBattle.Phase.Finished)return;
        if(PauseSettingsMenu.IsOpen || Time.deltaTime<=0)
        {
            // The cinematic re-samples poses while paused; keep the last solved
            // world placement too, rather than restoring its unblocked target.
            shark.transform.SetPositionAndRotation(previousRoot,previousRotation);UpdateVolumes();return;
        }
        Vector3 intended=shark.transform.position;Quaternion intendedRotation=shark.transform.rotation;
        float travelBudget=24*Mathf.Max(.01f,Time.deltaTime)+.15f;
        Vector3 desired=previousRoot+Vector3.ClampMagnitude(intended-previousRoot,travelBudget);
        Quaternion desiredRotation=Quaternion.RotateTowards(previousRotation,intendedRotation,300*Mathf.Max(.01f,Time.deltaTime)+3);
        LastObstacles.Clear();
        if(impulseTick!=Time.fixedTime){impulseTick=Time.fixedTime;pushedBodies.Clear();}
        UpdateVolumes();
        for(int i=0;i<volumes.Length;i++)anatomyVelocity[i]=Vector3.ClampMagnitude((volumes[i].transform.position-previousAnatomy[i])/Mathf.Max(.02f,Time.deltaTime),14);
        int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(previousRoot,desired)/.45f,
            Quaternion.Angle(previousRotation,desiredRotation)/7)),1,32);
        Vector3 resolved=previousRoot;
        for(int step=1;step<=steps;step++)
        {
            Quaternion rotation=Quaternion.Slerp(previousRotation,desiredRotation,(float)step/steps);
            resolved+=(desired-previousRoot)/steps;
            shark.transform.SetPositionAndRotation(resolved,rotation);UpdateVolumes();
            for(int pass=0;pass<24;pass++)
            {
                bool blocked=false;int count=battle.Boards.Count;
                for(int i=0;i<count;i++)
                {
                    var board=battle.Boards[i];
                    for(int n=0;n<volumes.Length;n++)
                    {
                        if(!Overlap(volumes[n],board,out var direction,out float depth) || depth<.001f)continue;
                        PushFloatingWood(n,board.GetComponent<Rigidbody>(),board.Collision,direction,depth,
                            battle.CurrentPhase==Story1WreckBattle.Phase.Finale ? battle.Finale.ProtectsBoard(board) : battle.BoardCarriesPlayer(board));
                        if(battle.CurrentPhase==Story1WreckBattle.Phase.Finale && battle.Finale.CanBreakBoards && !battle.Finale.ProtectsBoard(board) && board.FractureSource!=null)
                        {
                            if(battle.DamageBoard(board,100,board.Collision.ClosestPoint(volumes[n].transform.position),shark.transform.forward*5+Vector3.up*2))break;
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
                    for(int n=0;n<volumes.Length;n++)
                    {
                        if(!OverlapShape(volumes[n],piece.Collision,Vector3.zero,out var direction,out float depth) || depth<.001f)continue;
                        PushFloatingWood(n,piece.GetComponent<Rigidbody>(),piece.Collision,direction,depth);
                        if(piece.FractureSource!=null && battle.CurrentPhase==Story1WreckBattle.Phase.Finale && battle.Finale.CanBreakBoards)
                        {
                            if(battle.DamageWreckDetail(piece,100,piece.Collision.ClosestPoint(volumes[n].transform.position),shark.transform.forward*5+Vector3.up*2))break;
                        }
                        blocked=true;BlockedContactCount++;resolved+=direction*(depth+.025f);
                        if(LastObstacles.Count<12)LastObstacles.Add(piece.name+" anatomy="+n+" depth="+depth.ToString("F3"));
                        shark.transform.position=resolved;UpdateVolumes();
                    }
                }
                if(!blocked)break;
            }
        }
        shark.transform.SetPositionAndRotation(resolved,desiredRotation);UpdateVolumes();
        Vector3 escape=EscapeWedge();resolved+=escape;
        if(escape.sqrMagnitude>0){shark.transform.position=resolved;UpdateVolumes();}
        if(Vector3.Distance(resolved,previousRoot)>travelBudget+.15f && hasRetainedPose)
        {
            // A narrow seam can make iterative minimum translations oscillate
            // then choose a distant empty location. Preserve the previous
            // actual anatomical pose when it is still clear instead of
            // jumping the whole animal to that distant escape point.
            Vector3 rejected=resolved;Quaternion rejectedRotation=shark.transform.rotation;
            var currentPositions=new Vector3[poseBones.Length];var currentRotations=new Quaternion[poseBones.Length];
            for(int i=0;i<poseBones.Length;i++){currentPositions[i]=poseBones[i].localPosition;currentRotations[i]=poseBones[i].localRotation;}
            RestoreRetainedPose();shark.transform.SetPositionAndRotation(previousRoot,previousRotation);UpdateVolumes();
            if(NearestClear(travelBudget+.15f,out var retainedShift))
            {
                resolved=previousRoot+retainedShift;desiredRotation=previousRotation;shark.transform.position=resolved;UpdateVolumes();ConstraintHoldCount++;
            }
            else
            {
                for(int i=0;i<poseBones.Length;i++){poseBones[i].localPosition=currentPositions[i];poseBones[i].localRotation=currentRotations[i];}
                shark.transform.SetPositionAndRotation(rejected,rejectedRotation);resolved=rejected;UpdateVolumes();LargeEscapeCount++;
                if(LargeEscapes.Count<24)LargeEscapes.Add(battle.CurrentPhase+" film="+(battle.Finale!=null ? battle.Finale.CinematicTime : -1)+" delta="+Vector3.Distance(previousRoot,rejected)+" frame="+Time.deltaTime+" root="+rejected+" escape="+escape);
            }
        }
        // Damage uses the solved anatomical pose, so wood cannot conceal a
        // raw desired pose hitting Sahur or a board on the other side of an obstacle.
        DamageSweptAttack();battle.ApplySharkContactDamage();
        float residual=0;
        foreach(var board in battle.Boards)foreach(var volume in volumes)
            if(Overlap(volume,board,out var normal,out var depth))residual=Mathf.Max(residual,depth);
        foreach(var piece in battle.WreckDetails)foreach(var volume in volumes)
            if(OverlapShape(volume,piece.Collision,Vector3.zero,out var normal,out var depth))residual=Mathf.Max(residual,depth);
        MaximumResidualPenetration=Mathf.Max(MaximumResidualPenetration,residual);
        previousRoot=resolved;previousRotation=desiredRotation;
        for(int i=0;i<volumes.Length;i++)previousAnatomy[i]=volumes[i].transform.position;
        RetainPose();LastCorrection=resolved-intended;
    }
    public float CurrentPenetration()
    {
        UpdateVolumes();float result=0;
        foreach(var board in battle.Boards)foreach(var volume in volumes)
            if(Overlap(volume,board,out var normal,out var depth))result=Mathf.Max(result,depth);
        foreach(var piece in battle.WreckDetails)foreach(var volume in volumes)
            if(OverlapShape(volume,piece.Collision,Vector3.zero,out var normal,out var depth))result=Mathf.Max(result,depth);
        return result;
    }
    void OnDestroy(){if(root!=null)Destroy(root);}
}
