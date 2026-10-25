using System.Collections.Generic;
using UnityEngine;

public sealed partial class Story1WreckBattle
{
    // The cinematic owns transforms, so PhysX cannot keep its sampled poses
    // solid for us. Query the visible shark and original cut timber instead.
    sealed class RedSolidWood
    {
        public Mesh mesh;
        public Vector3[] vertices, query;
        public int[] triangles;
        public RedRiggingTree tree,lastSharkTree;
        public Matrix4x4 queryMatrix;
        public Vector3 queryOffset,contact;
        public int queryFrame=-1,poseFrame=-1,poseVersion=-1;
        public bool touched;
    }
    readonly Dictionary<Story1WreckMeshCollision,RedSolidWood> redSolidWood=new Dictionary<Story1WreckMeshCollision,RedSolidWood>();
    RedRiggingTree redSolidSharkTree;
    Vector3[] redSolidSkin;
    Mesh redSolidHeroMesh;
    readonly List<Vector3> redSolidHeroVertices=new List<Vector3>();
    SkinnedMeshRenderer[] redSolidHeroSkins;
    bool redAllowOpeningFracture;
    int redSolidFrame=-1,redSolidSkinFrame=-1,redSolidSkinVersion=-1;
    static readonly Unity.Profiling.ProfilerMarker RedSolidsMarker=new Unity.Profiling.ProfilerMarker("Opening.NativeSolids");
    public int RedesignSolidWoodContacts { get; private set; }
    public int RedesignSolidFractures { get; private set; }
    public int RedesignSolidUnresolved { get; private set; }
    public float RedesignSolidSupportLift { get; private set; }
    public double RedesignSolidMs { get; private set; }
    public float RedesignSolidHeroWoodDepth { get; private set; }

    static readonly Unity.Profiling.ProfilerMarker RedHeroWoodMarker=new Unity.Profiling.ProfilerMarker("Opening.HeroWood");
    static readonly Unity.Profiling.ProfilerMarker RedSharkWoodMarker=new Unity.Profiling.ProfilerMarker("Opening.SharkWood");
    static readonly Unity.Profiling.ProfilerMarker RedSkinIndexMarker=new Unity.Profiling.ProfilerMarker("Opening.SkinIndex");
    RedSolidWood RedSolidGeometry(Story1WreckMeshCollision shape)
    {
        var filter=shape.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)return null;
        if(!redSolidWood.TryGetValue(shape,out var data)||data.mesh!=filter.sharedMesh)
        {
            data=new RedSolidWood{mesh=filter.sharedMesh,vertices=filter.sharedMesh.vertices,triangles=filter.sharedMesh.triangles};
            data.query=new Vector3[data.vertices.Length];redSolidWood[shape]=data;
        }
        return data;
    }
    static Bounds RedSolidBounds(Bounds bounds,Matrix4x4 matrix)
    {
        Vector3 x=matrix.MultiplyVector(Vector3.right*bounds.extents.x),y=matrix.MultiplyVector(Vector3.up*bounds.extents.y),z=matrix.MultiplyVector(Vector3.forward*bounds.extents.z);
        return new Bounds(matrix.MultiplyPoint3x4(bounds.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
    }
    // All post-crush local-space queries use one exact current-pose index.
    // Root fitting does not invalidate local vertices; bone sampling does.
    RedRiggingTree RedSolidIndex(SkinnedMeshRenderer skin)
    {
        if(redSolidSharkTree!=null&&redSolidSkinFrame==redContactBakedFrame&&redSolidSkinVersion==redContactBakedVersion)return redSolidSharkTree;
        using var measured=RedSkinIndexMarker.Auto();
        if(redSolidSkin==null||redSolidSkin.Length!=redPosedVertices.Count)redSolidSkin=new Vector3[redPosedVertices.Count];
        redPosedVertices.CopyTo(redSolidSkin);
        if(redSolidSharkTree==null)
        {
            // Crushing has finished; its prewarmed topology can now follow the
            // same local posed mesh rather than rebuilding a second hierarchy.
            redSolidSharkTree=redRiggingSkinTree;
            if(redSolidSharkTree==null)redSolidSharkTree=new RedRiggingTree(redSolidSkin,skin.sharedMesh.triangles);
            else redSolidSharkTree.BuildSlice(double.PositiveInfinity);
        }
        // Tight bounds cost less than repeatedly traversing a broadly padded skin.
        // Leaf tests retain the original current triangles and signed containment.
        // Tutorial pose changes can stay within the exact articulated hierarchy longer.
        // This changes conservative bounds only; leaf tests still use current triangles.
        redSolidSharkTree.RefitArticulated(redSolidSkin,RedesignStage==OpeningStage.Tutorial?3f:1f);
        redSolidSkinFrame=redContactBakedFrame;redSolidSkinVersion=redContactBakedVersion;
        return redSolidSharkTree;
    }
    bool RedSolidTouch(Story1WreckMeshCollision shape,SkinnedMeshRenderer skin,Vector3 woodOffset,out Vector3 contact)
    {
        using var measured=RedSharkWoodMarker.Auto();
        contact=default;if(shape==null||!shape.Active)return false;
        var inverse=skin.transform.worldToLocalMatrix;
        Bounds bounds=shape.WorldBounds;bounds.center+=woodOffset;
        Bounds localBounds=RedSolidBounds(bounds,inverse);
        if(!redSolidSharkTree.Bounds.Intersects(localBounds))return false;
        var data=RedSolidGeometry(shape);if(data==null)return false;
        Matrix4x4 matrix=inverse*shape.transform.localToWorldMatrix;Vector3 delta=inverse.MultiplyVector(woodOffset);
        // A held tutorial can render many frames without advancing physics or
        // either pose. Retain its exact query, with the same geometry/matrix keys.
        bool same=(data.queryFrame==Time.frameCount||RedesignStage==OpeningStage.Tutorial&&TutorialWaiting)&&data.poseFrame==redSolidSkinFrame&&data.poseVersion==redSolidSkinVersion&&
            ReferenceEquals(data.lastSharkTree,redSolidSharkTree)&&data.queryMatrix.Equals(matrix)&&data.queryOffset.Equals(delta);
        if(same){contact=skin.transform.TransformPoint(data.contact);return data.touched;}
        data.queryFrame=Time.frameCount;data.poseFrame=redSolidSkinFrame;data.poseVersion=redSolidSkinVersion;
        data.lastSharkTree=redSolidSharkTree;data.queryMatrix=matrix;data.queryOffset=delta;data.touched=false;data.contact=Vector3.zero;
        // Current posed triangles and signed containment still decide contact.
        if(redSolidSharkTree.CapsuleGapUpTo(localBounds.center,localBounds.center,localBounds.extents.magnitude,.001f)>=.001f)return false;
        for(int i=0;i<data.vertices.Length;i++)data.query[i]=matrix.MultiplyPoint3x4(data.vertices[i])+delta;
        if(data.tree==null)data.tree=new RedRiggingTree(data.query,data.triangles);
        else data.tree.Refit(data.query);
        if(!redSolidSharkTree.Touch(data.tree,out var local))return false;
        data.contact=local;data.touched=true;contact=skin.transform.TransformPoint(local);return true;
    }
    Vector3 RedSolidSeparation(Story1WreckMeshCollision shape,SkinnedMeshRenderer skin,Vector3 preferred)
    {
        var inverse=skin.transform.worldToLocalMatrix;
        Vector3 centre=inverse.MultiplyPoint3x4(shape.WorldBounds.center);
        if(!redSolidSharkTree.Closest(centre,out int triangle,out _,out var surface))return Vector3.zero;
        bool inside=redSolidSharkTree.CapsuleGapUpTo(centre,centre,0,.01f)<0;
        Vector3 direction=inside?surface-centre:centre-surface;
        if(direction.sqrMagnitude<.000001f)direction=inverse.MultiplyVector(preferred);
        direction.Normalize();
        var data=RedSolidGeometry(shape);float near=float.PositiveInfinity;
        // RedSolidTouch has already transformed the actual timber into this
        // skin's space. Project it once, then verify the resulting separation.
        foreach(var p in data.query)near=Mathf.Min(near,Vector3.Dot(p,direction));
        float distance=Mathf.Max(.02f,Vector3.Dot(surface,direction)-near+.02f);
        Vector3 offset=skin.transform.TransformVector(direction*distance);
        if(!RedSolidTouch(shape,skin,offset,out _))return offset;
        // Concave fins can invalidate the nearest tangent. A supporting plane
        // of the posed skin is a conservative, finite fallback, not another
        // series of hundreds of triangle-intersection searches per splinter.
        float far=float.NegativeInfinity;
        foreach(var p in redSolidSkin)far=Mathf.Max(far,Vector3.Dot(p,direction));
        return skin.transform.TransformVector(direction*Mathf.Max(distance,far-near+.02f));
    }
    bool RedSolidMayBreak(Story1WreckPlank board)
    {
        if(board==redCombatBoard)
            return RedesignStage==OpeningStage.Tutorial&&CurrentLesson==Lesson.Jump&&lessonExecuting&&lessonTime>.5f || RedesignStage==OpeningStage.Duel&&redTailHit;
        if(RedesignStage==OpeningStage.Tutorial)return CurrentLesson==Lesson.Jump&&lessonExecuting;
        return RedesignStage!=OpeningStage.Drift;
    }
    void RedResolveOpeningContacts()
    {
        if(!RedesignActive||!shattered||RedesignStage==OpeningStage.Crush||RedesignStage==OpeningStage.Finished||RedInputBlocked)return;
        if(redSolidFrame==Time.frameCount)return;redSolidFrame=Time.frameCount;
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        using var measurement=RedSolidsMarker.Auto();
        RedesignSolidUnresolved=0;
        try
        {
            RedSolidHeroTimber();
            bool tailContact=RedesignStage==OpeningStage.Duel&&redesignTime>=21.2f&&!redTailHit;
            if(!tailContact&&!redTailHit)
            {
                Vector3 away=RedesignStage==OpeningStage.Duel&&redesignTime>=10&&redesignTime<13?Vector3.up:
                    Vector3.ProjectOnPlane(sharkAnimator.NoseWorldPoint-RedChest,Vector3.up).normalized;
                RedSeparateTutorialShark(.18f,away);
            }
            var skin=RedContactSkin();if(skin==null)return;
            RedSolidIndex(skin);
            // Newly fractured shards are included in the same pass. They are
            // separated and receive momentum rather than being hidden/deleted.
            for(int pass=0;pass<3;pass++)
            {
                bool changed=false;
                for(int i=0;i<planks.Count;i++)
                {
                    var board=planks[i];if(board==null||board.IsBroken||!RedSolidTouch(board.Collision,skin,Vector3.zero,out var point))continue;
                    RedesignSolidWoodContacts++;
                    if(RedSolidMayBreak(board)&&board.FractureSource?.splinters?.Length>1)
                    {
                        redAllowOpeningFracture=true;bool broken;
                        try{broken=DamageBoard(board,1000,point,shark.forward*5+Vector3.up*2);}finally{redAllowOpeningFracture=false;}
                        if(broken){RedesignSolidFractures++;RedEvent("Shark fractured native plank on contact: "+board.name);changed=true;continue;}
                    }
                    changed|=RedSolidSeparateWood(board.Collision,skin,board==redCombatBoard,point);
                }
                for(int i=0;i<detailPieces.Count;i++)
                {
                    var piece=detailPieces[i];if(piece==null||piece.IsBroken||!RedSolidTouch(piece.Collision,skin,Vector3.zero,out var point))continue;
                    RedesignSolidWoodContacts++;
                    if(RedSolidMayBreak(null)&&piece.FractureSource?.splinters?.Length>1&&DamageWreckDetail(piece,1000,point,shark.forward*5+Vector3.up*2))
                    {RedesignSolidFractures++;changed=true;continue;}
                    changed|=RedSolidSeparateWood(piece.Collision,skin,false,point);
                }
                if(!changed)break;
            }
            // Fracture can create timber at the hero's feet after the first
            // body pass. Verify those new pieces in this very impact frame.
            RedSolidHeroTimber(skin);
        }
        finally{RedesignSolidMs=RedRiggingElapsed(started);}
    }
    internal void RedResolveOpeningSolids()=>RedResolveOpeningContacts();
    void RedSolidHeroTimber(SkinnedMeshRenderer skin=null)
    {
        using var measured=RedHeroWoodMarker.Auto();
        RedesignSolidHeroWoodDepth=0;
        if(!redesignManual||RedesignStage!=OpeningStage.Pursuit&&RedesignStage!=OpeningStage.Duel)return;
        RedWaterCollectShapes();RedTutorialBody();
        Bounds heroBounds=RedSolidBodyBounds();
        bool supported=RedesignStage==OpeningStage.Duel || RedesignStage==OpeningStage.Crush&&redesignTime>=10.2f ||
            RedesignStage==OpeningStage.Tutorial&&CurrentLesson!=Lesson.Jump&&CurrentLesson!=Lesson.Swim ||
            RedesignStage==OpeningStage.Pursuit&&redesignTime>=9;
        foreach(var shape in redWaterShapes)
        {
            if(supported&&shape.mesh==redCombatBoard?.Collision)continue;
            if(!shape.mesh.WorldBounds.Intersects(heroBounds))continue;
            for(int pass=0;pass<4;pass++)
            {
                Vector3 correction=Vector3.zero;float maximum=0;
                foreach(var capsule in redWaterCapsules)
                {
                    Bounds bounds=new Bounds((capsule.a+capsule.b)*.5f,Vector3.zero);bounds.Encapsulate(capsule.a);bounds.Encapsulate(capsule.b);bounds.Expand(capsule.radius*2);
                    if(!shape.mesh.WorldBounds.Intersects(bounds))continue;
                    redWaterQuery.radius=capsule.radius;redWaterQuery.height=Vector3.Distance(capsule.a,capsule.b)+capsule.radius*2;
                    Quaternion turn=Quaternion.FromToRotation(Vector3.up,capsule.b-capsule.a);
                    redWaterQuery.enabled=true;
                    try
                    {
                        for(int i=0;i<shape.mesh.Parts.Length;i++)
                        {
                            if(!shape.mesh.PartBounds(i).Intersects(bounds))continue;
                            if(Physics.ComputePenetration(redWaterQuery,(capsule.a+capsule.b)*.5f,turn,shape.mesh.Parts[i],shape.mesh.PartPosition(i),shape.mesh.PartRotation(i),out var direction,out float depth)&&depth>maximum)
                            {maximum=depth;correction=-direction*(depth+.025f);}
                        }
                    }
                    finally{redWaterQuery.enabled=false;}
                }
                if(maximum<=.001f)break;
                if(skin!=null)correction=RedSolidKeepWoodOffHero(shape.mesh,skin,correction);
                RedesignSolidHeroWoodDepth=Mathf.Max(RedesignSolidHeroWoodDepth,maximum);
                var body=shape.body;
                if(body!=null&&!body.isKinematic){body.position+=correction;shape.mesh.transform.position=body.position;}
                else shape.mesh.transform.position+=correction;
            }
            if(RedSolidWoodHeroOverlap(shape.mesh,Vector3.zero,out _))
            {
                Vector3 correction=RedSolidBoundsSeparation(shape.mesh.WorldBounds,RedSolidBodyBounds());
                if(skin!=null)correction=RedSolidKeepWoodOffHero(shape.mesh,skin,correction);
                if(shape.body!=null&&!shape.body.isKinematic){shape.body.position+=correction;shape.mesh.transform.position=shape.body.position;}
                else shape.mesh.transform.position+=correction;
            }
        }
    }
    bool RedSolidSeparateWood(Story1WreckMeshCollision shape,SkinnedMeshRenderer skin,bool support,Vector3 point)
    {
        Vector3 preferred=(shape.WorldBounds.center-skin.bounds.center).normalized;
        // Preserve the occupied platform. Other free timber yields to this
        // much heavier animal, including shards created inside its silhouette.
        Vector3 offset=RedSolidSeparation(shape,skin,preferred);
        if(offset.sqrMagnitude<.000001f){RedesignSolidUnresolved++;return false;}
        if(!support)offset=RedSolidKeepWoodOffHero(shape,skin,offset);
        if(support)shark.position-=offset;
        else
        {
            var body=shape.GetComponent<Rigidbody>();
            if(body!=null&&!body.isKinematic)
            {
                body.position+=offset;shape.transform.position=body.position;
                Vector3 direction=offset.normalized;Vector3 application=point+offset;
                float desired=Mathf.Min(body.mass>=30?2.5f:5,offset.magnitude/Mathf.Max(.02f,Time.deltaTime));
                float outgoing=Vector3.Dot(body.GetPointVelocity(application),direction);
                // Held pressure is not a new full impulse each render pass.
                // Apply at the translated contact, otherwise a deep initial
                // overlap creates a huge artificial torque about the old point.
                if(outgoing<desired)body.AddForceAtPosition(direction*(desired-outgoing)*body.mass,application,ForceMode.Impulse);
            }
            else shape.transform.position+=offset;
        }
        return true;
    }
    bool RedSolidWoodHeroOverlap(Story1WreckMeshCollision shape,Vector3 offset,out Vector3 correction)
    {
        correction=Vector3.zero;float deepest=0;
        if(redWaterQuery==null)return false;
        shape.ReadWorldGeometry(out var timber,out var partBounds,out var positions,out var rotations);timber.center+=offset;
        foreach(var capsule in redWaterCapsules)
        {
            Bounds bounds=new Bounds((capsule.a+capsule.b)*.5f,Vector3.zero);bounds.Encapsulate(capsule.a);bounds.Encapsulate(capsule.b);bounds.Expand(capsule.radius*2);
            if(!timber.Intersects(bounds))continue;
            redWaterQuery.radius=capsule.radius;redWaterQuery.height=Vector3.Distance(capsule.a,capsule.b)+capsule.radius*2;
            Quaternion turn=Quaternion.FromToRotation(Vector3.up,capsule.b-capsule.a);redWaterQuery.enabled=true;
            try
            {
                for(int i=0;i<shape.Parts.Length;i++)
                {
                    Bounds part=partBounds[i];part.center+=offset;if(!part.Intersects(bounds))continue;
                    if(Physics.ComputePenetration(redWaterQuery,(capsule.a+capsule.b)*.5f,turn,shape.Parts[i],positions[i]+offset,rotations[i],out var direction,out float depth)&&depth>deepest)
                    {deepest=depth;correction=-direction*(depth+.035f);}
                }
            }
            finally{redWaterQuery.enabled=false;}
        }
        return deepest>.0005f;
    }
    Vector3 RedSolidKeepWoodOffHero(Story1WreckMeshCollision shape,SkinnedMeshRenderer skin,Vector3 offset)
    {
        if(!RedSolidWoodHeroOverlap(shape,offset,out _)&&!RedSolidTouch(shape,skin,offset,out _))return offset;
        Vector3 normal=offset.normalized;float plane=float.NegativeInfinity;
        var matrix=skin.transform.localToWorldMatrix;
        foreach(var p in redSolidSkin)plane=Mathf.Max(plane,Vector3.Dot(matrix.MultiplyPoint3x4(p),normal));
        float nearest=float.PositiveInfinity;var data=RedSolidGeometry(shape);matrix=shape.transform.localToWorldMatrix;
        foreach(var p in data.vertices)nearest=Mathf.Min(nearest,Vector3.Dot(matrix.MultiplyPoint3x4(p),normal));
        plane-=nearest;plane+=.04f;
        for(int pass=0;pass<12;pass++)
        {
            if(!RedSolidWoodHeroOverlap(shape,offset,out var push)&&!RedSolidTouch(shape,skin,offset,out _))return offset;
            // Slide around the shark's supporting plane rather than pushing
            // the same timber back and forth between shark and protagonist.
            Vector3 slide=Vector3.ProjectOnPlane(push,normal);
            if(slide.magnitude<push.magnitude*.25f)
            {
                slide=Vector3.ProjectOnPlane(shape.WorldBounds.center+offset-RedChest,normal);
                if(slide.sqrMagnitude<.001f)slide=Vector3.Cross(normal,redForward);
                if(slide.sqrMagnitude<.001f)slide=Vector3.Cross(normal,Vector3.up);
                slide=slide.normalized*(push.magnitude+.1f);
            }
            offset+=slide+normal*Mathf.Max(0,Vector3.Dot(push,normal));
            offset+=normal*Mathf.Max(0,plane-Vector3.Dot(offset,normal));
        }
        // Finite fallback for an initially embedded long timber: put it just
        // outside the combined visible bounds, keeping the original object.
        Bounds occupied=skin.bounds;occupied.Encapsulate(RedSolidBodyBounds());
        return RedSolidBoundsSeparation(shape.WorldBounds,occupied);
    }
    Bounds RedSolidBodyBounds()
    {
        Bounds occupied=new Bounds(RedChest,Vector3.zero);
        foreach(var capsule in redWaterCapsules){occupied.Encapsulate(capsule.a+Vector3.one*capsule.radius);occupied.Encapsulate(capsule.a-Vector3.one*capsule.radius);occupied.Encapsulate(capsule.b+Vector3.one*capsule.radius);occupied.Encapsulate(capsule.b-Vector3.one*capsule.radius);}
        return occupied;
    }
    static Vector3 RedSolidBoundsSeparation(Bounds wood,Bounds occupied)
    {
        float right=occupied.max.x-wood.min.x+.04f,left=occupied.min.x-wood.max.x-.04f;
        float front=occupied.max.z-wood.min.z+.04f,back=occupied.min.z-wood.max.z-.04f;
        Vector3 result=Vector3.right*(Mathf.Abs(right)<Mathf.Abs(left)?right:left);
        float z=Mathf.Abs(front)<Mathf.Abs(back)?front:back;if(Mathf.Abs(z)<result.magnitude)result=Vector3.forward*z;
        return result;
    }

    static readonly Unity.Profiling.ProfilerMarker RedSupportMarker=new Unity.Profiling.ProfilerMarker("Opening.Support");
    void RedSolidSupport()
    {
        using var measured=RedSupportMarker.Auto();
        RedesignSolidSupportLift=0;
        if(redCombatBoard==null||redCombatBoard.IsBroken||!redCombatBoard.Collision.Active)return;
        // Foot offset is a controller constant, not the posed body's bottom.
        // A prone/get-up clip can put the chest below that constant. Measure
        // its real skin against the native deck before placing the shark.
        if(redSolidHeroMesh==null)redSolidHeroMesh=new Mesh{name="Opening supported body query"};
        if(redSolidHeroSkins==null)redSolidHeroSkins=player.GetComponentsInChildren<SkinnedMeshRenderer>();
        Vector3 normal=redCombatBoard.transform.up;if(normal.y<0)normal=-normal;
        Vector3 top=RedBoardPoint(redCombatBoard);float lift=0;
        var support=redCombatBoard.Collision.ReadRaycastSnapshot();
        Bounds supportBounds=support.Bounds;
        float highest=Vector3.Dot(supportBounds.center,normal)+Vector3.Dot(supportBounds.extents,new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z)));
        float reach=Mathf.Max(4,redCombatBoard.Dimensions.y+4);
        foreach(var skin in redSolidHeroSkins)
        {
            if(skin==null||!skin.enabled)continue;
            skin.BakeMesh(redSolidHeroMesh,true);redSolidHeroMesh.GetVertices(redSolidHeroVertices);var matrix=skin.transform.localToWorldMatrix;
            foreach(var vertex in redSolidHeroVertices)
            {
                Vector3 p=matrix.MultiplyPoint3x4(vertex);float signed=Vector3.Dot(p-top,normal);
                if(signed>=.025f+lift)continue;
                // No surface in the actual compound can demand a greater lift.
                if(highest-Vector3.Dot(p,normal)+.025f<=lift)continue;
                if(support.Raycast(new Ray(p+normal*(reach-signed),-normal),out var hit,reach+8))
                    lift=Mathf.Max(lift,Vector3.Dot(hit.point-p,normal)+.025f);
            }
        }
        if(lift>0){player.transform.position+=normal*lift;RedesignSolidSupportLift=lift;}
    }
    void RedSolidDispose()
    {
        if(redSolidHeroMesh!=null)Destroy(redSolidHeroMesh);
        redSolidSkinFrame=redSolidSkinVersion=-1;redSolidWood.Clear();redSolidSharkTree=null;redSolidSkin=null;redSolidHeroSkins=null;
    }
}
