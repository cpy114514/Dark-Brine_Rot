using System.Collections.Generic;
using UnityEngine;

public sealed partial class Story1WreckBattle
{
    // Offsets belong to a shot, rather than the world. A moving authored camera
    // therefore retains the selected side of the subject for the whole cut.
    int redClearShot = int.MinValue;
    OpeningStage redClearStage;
    Vector3 redClearOffset = new Vector3(0, 0, 1);
    Vector3 redClearTarget = new Vector3(0, 0, 1);
    float redClearBlockedSeconds, redClearLastSearch;
    SphereCollider redLensQuery;
    public bool RedesignCameraLensInsideWood { get; private set; }
    readonly List<Story1WreckMeshCollision> redClearShapes = new List<Story1WreckMeshCollision>();
    static readonly float[] RedClearBearings = { 0, .25f, -.25f, .5f, -.5f, .75f, -.75f, 1, -1 };
    static readonly float[] RedClearPursuitLifts = { 0, .75f, 1.5f };
    static readonly float[] RedClearDuelLifts = { 0, 1.5f, 3, 5 };
    static readonly float[] RedClearClimbLifts = { 0 };
    static readonly float[] RedClearRanges = { 1, .86f, 1.12f };
    static readonly float[] RedClearClimbRanges = { 1 };
    static readonly float[] RedClearProneRanges = { 1, .85f, 1.2f };

    /// <summary>Weighted real-wood obstruction of the returned camera; zero is clear.</summary>
    public float RedesignCameraWoodObstruction { get; private set; }

    /// <summary>
    /// Keep the authored reveal, choose a nearby clear angle at the cut, and
    /// retain it. Moving wreckage can request a damped correction afterwards.
    /// This never moves, hides, or changes collision on a fragment.
    /// </summary>
    Vector3 RedClearCamera(int shot, Vector3 wanted, Vector3 focus)
    {
        if (RedesignStage != OpeningStage.Pursuit && RedesignStage != OpeningStage.Duel)
        {
            redClearShot = int.MinValue;
            RedesignCameraWoodObstruction = 0;
            return wanted;
        }

        redClearShapes.Clear();
        foreach (var plank in planks)
            if (plank != null && !plank.IsBroken && plank.Collision != null && plank.Collision.Active)
                redClearShapes.Add(plank.Collision);
        foreach (var piece in detailPieces)
            if (piece != null && !piece.IsBroken && piece.Collision != null && piece.Collision.Active)
                redClearShapes.Add(piece.Collision);

        bool cut = redClearShot != shot || redClearStage != RedesignStage;
        if (cut)
        {
            redClearShot = shot;
            redClearStage = RedesignStage;
            redClearOffset = redClearTarget = RedChooseClearCamera(shot, wanted, focus);
            redClearBlockedSeconds = 0;
            redClearLastSearch = Time.time;
        }

        Vector3 current = RedCameraCandidate(wanted, focus, redClearOffset);
        float obstruction = RedCameraObstruction(current, focus);
        float dt = Mathf.Max(0, Time.deltaTime);
        redClearBlockedSeconds = obstruction > .1f ? redClearBlockedSeconds + dt : 0;
        // Hysteresis prevents wave/debris motion from switching sides every
        // frame. An already clear shot never drifts back towards its old angle.
        if (!cut && redClearBlockedSeconds > .22f && Time.time - redClearLastSearch > .45f)
        {
            Vector3 alternative = RedChooseClearCamera(shot, wanted, focus);
            Vector3 point = RedCameraCandidate(wanted, focus, alternative);
            if (RedCameraObstruction(point, focus) + .35f < obstruction)
                redClearTarget = alternative;
            redClearLastSearch = Time.time;
        }

        if (!cut)
        {
            float blend = 1 - Mathf.Exp(-dt / .3f);
            redClearOffset = Vector3.Lerp(redClearOffset, redClearTarget, blend);
            current = RedCameraCandidate(wanted, focus, redClearOffset);
        }
        // Visibility can use hysteresis, but a lens/near plane inside a real
        // fragment must be resolved in this rendered frame.
        current=RedKeepLensOutOfWood(current,focus);
        RedesignCameraWoodObstruction = RedCameraObstruction(current, focus);
        return current;
    }

    Vector3 RedKeepLensOutOfWood(Vector3 at,Vector3 focus)
    {
        Vector3 original=at;
        if(redLensQuery==null)
        {
            var go=new GameObject("Opening camera clearance query"){hideFlags=HideFlags.HideInHierarchy};
            go.transform.position=new Vector3(0,-100000,0);
            redLensQuery=go.AddComponent<SphereCollider>();redLensQuery.enabled=false;redLensQuery.isTrigger=true;
        }
        var camera=sequence.storyCamera;float tangent=Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
        float radius=Mathf.Max(.45f,camera.nearClipPlane*Mathf.Sqrt(1+tangent*tangent*(1+camera.aspect*camera.aspect))+.12f);
        redLensQuery.radius=radius;
        redLensQuery.enabled=true;
        try
        {
        for(int pass=0;pass<5;pass++)
        {
            bool moved=false;
            foreach(var shape in redClearShapes)
            {
                if(shape.WorldBounds.SqrDistance(at)>radius*radius)continue;
                RedCameraQueryMatrices(shape,out var toPhysics,out var toVisual);
                Quaternion poseDelta=toPhysics.rotation;
                for(int i=0;i<shape.Parts.Length;i++)
                {
                    var part=shape.Parts[i];if(part==null||!part.enabled||shape.PartBounds(i).SqrDistance(at)>radius*radius)continue;
                    if(!Physics.ComputePenetration(redLensQuery,toPhysics.MultiplyPoint3x4(at),Quaternion.identity,part,
                        toPhysics.MultiplyPoint3x4(part.transform.position),poseDelta*part.transform.rotation,out var direction,out float depth))continue;
                    at+=toVisual.MultiplyVector(direction*(depth+.025f));moved=true;
                }
            }
            if(!moved)break;
        }
        RedesignCameraLensInsideWood=RedLensIntersectsWood(at,radius-.01f);
        if(RedesignCameraLensInsideWood)
        {
            // Several moving compound fragments can push in opposing directions.
            // If local separation stalls, choose a measured clear nearby lens.
            Vector3 away=Vector3.ProjectOnPlane(original-focus,Vector3.up).normalized;
            if(away.sqrMagnitude<.01f)away=redRight;
            Vector3 right=Vector3.Cross(Vector3.up,away);float best=float.PositiveInfinity;Vector3 safe=at;
            foreach(float lift in RedLensLifts)foreach(float distance in RedLensDistances)foreach(float side in RedLensSides)
            {
                Vector3 offset=Vector3.up*lift+away*distance+right*side;
                if(offset.sqrMagnitude>=best)continue;
                Vector3 candidate=original+offset;
                if(RedLensIntersectsWood(candidate,radius+.02f))continue;
                float cost=offset.sqrMagnitude+RedCameraObstruction(candidate,focus)*2;
                if(cost<best){best=cost;safe=candidate;}
            }
            if(!float.IsInfinity(best))at=safe;
            RedesignCameraLensInsideWood=RedLensIntersectsWood(at,radius-.01f);
        }
        return at;
        }
        finally{redLensQuery.enabled=false;}
    }
    static readonly float[] RedLensLifts={0,1,2,4};
    static readonly float[] RedLensDistances={0,1,2,4,8};
    static readonly float[] RedLensSides={0,1,-1,2,-2,4,-4};
    bool RedLensIntersectsWood(Vector3 at,float radius)
    {
        foreach(var shape in redClearShapes)
            if(shape.WorldBounds.SqrDistance(at)<radius*radius&&RedCameraInsideWood(shape,at,radius))return true;
        return false;
    }
    void RedCameraDispose(){if(redLensQuery!=null)Destroy(redLensQuery.gameObject);redLensQuery=null;}

    Vector3 RedChooseClearCamera(int shot, Vector3 wanted, Vector3 focus)
    {
        bool climb = shot == 25;
        // Close surprise/reveal shots remain on their authored side. In
        // particular, the climb stays frontal and at board height.
        float bearing = climb ? 15 : shot == 20 || shot == 21 || shot == 33 || shot == 39 ? 7.5f : 32;
        var lifts = climb ? RedClearClimbLifts : RedesignStage == OpeningStage.Pursuit ? RedClearPursuitLifts : RedClearDuelLifts;
        // The prone subject is much lower than the standing focus. A closer
        // clear view can look past foreground wreckage without hiding it.
        var ranges = climb ? RedClearClimbRanges : shot == 34 ? RedClearProneRanges : RedClearRanges;
        Vector3 best = new Vector3(0, 0, 1);
        float bestCost = float.PositiveInfinity;
        foreach (float range in ranges)
        foreach (float lift in lifts)
        foreach (float fraction in RedClearBearings)
        {
            Vector3 offset = new Vector3(fraction * bearing, lift, range);
            float composition = Mathf.Abs(fraction) * .6f + lift * .18f + Mathf.Abs(range - 1) * 3;
            // Obstruction cost cannot be negative. Once a close clear option
            // exists, farther compositions need no more native mesh queries.
            if (composition >= bestCost) continue;
            Vector3 candidate = RedCameraCandidate(wanted, focus, offset);
            float blocked = RedCameraObstruction(candidate, focus);
            float cost = blocked * 20 + composition;
            if (cost < bestCost) { bestCost = cost; best = offset; }
            if (blocked == 0 && composition == 0) return best;
        }
        return best;
    }

    Vector3 RedCameraCandidate(Vector3 wanted, Vector3 focus, Vector3 offset)
    {
        Vector3 delta = wanted - focus;
        Vector3 planar = Quaternion.AngleAxis(offset.x, Vector3.up) * Vector3.ProjectOnPlane(delta, Vector3.up) * offset.z;
        Vector3 at = focus + planar + Vector3.up * (delta.y + offset.y);
        if (RedesignStage == OpeningStage.Pursuit || RedesignStage == OpeningStage.Duel && redClearShot == 34)
        {
            // Sample the current displaced ocean at the actual lens and its
            // near surroundings, not at Sahur or at the board's centre.
            float water = RedWater(at);
            water = Mathf.Max(water, RedWater(at + Vector3.right * .5f));
            water = Mathf.Max(water, RedWater(at - Vector3.right * .5f));
            water = Mathf.Max(water, RedWater(at + Vector3.forward * .5f));
            water = Mathf.Max(water, RedWater(at - Vector3.forward * .5f));
            at.y = Mathf.Max(at.y, water + 1.2f);
        }
        return at;
    }

    float RedCameraObstruction(Vector3 at, Vector3 focus)
    {
        Vector3 direction = focus - at;
        if (direction.sqrMagnitude < .01f) return 10;
        Vector3 right = Vector3.Cross(direction.normalized, Vector3.up).normalized;
        if (right.sqrMagnitude < .1f) right = Vector3.right;
        float blocked = RedCameraRayBlocked(at, focus) ? 3 : 0;
        blocked += RedCameraRayBlocked(at + right * .18f, focus + right * .45f) ? 1 : 0;
        blocked += RedCameraRayBlocked(at - right * .18f, focus - right * .45f) ? 1 : 0;
        blocked += RedCameraRayBlocked(at + Vector3.up * .12f, focus + Vector3.up * .6f) ? 1 : 0;
        // A ray starts inside a convex collider without reporting its exit.
        // Explicit endpoint clearance catches a lens/near plane inside timber.
        foreach (var shape in redClearShapes)
        {
            if (shape.WorldBounds.SqrDistance(at) > .16f) continue;
            if (RedCameraInsideWood(shape, at, .35f)) { blocked += 6; break; }
        }
        return blocked;
    }

    bool RedCameraRayBlocked(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < .05f) return false;
        var ray = new Ray(from, delta / length);
        foreach (var shape in redClearShapes)
        {
            Bounds bounds = shape.WorldBounds;
            if (!bounds.IntersectRay(ray, out float enter) || enter > length) continue;
            RedCameraQueryMatrices(shape, out var toPhysics, out _);
            var query = new Ray(toPhysics.MultiplyPoint3x4(from), toPhysics.MultiplyVector(ray.direction));
            for (int i = 0; i < shape.Parts.Length; i++)
            {
                var part = shape.Parts[i];
                if (part == null || !part.enabled || part.isTrigger || !part.gameObject.activeInHierarchy) continue;
                if (!shape.PartBounds(i).IntersectRay(ray, out enter) || enter > length) continue;
                if (part.Raycast(query, out var hit, Mathf.Max(0, length - .04f))) return true;
            }
        }
        return false;
    }

    static bool RedCameraInsideWood(Story1WreckMeshCollision shape, Vector3 at, float radius)
    {
        RedCameraQueryMatrices(shape, out var toPhysics, out var toVisual);
        Vector3 query = toPhysics.MultiplyPoint3x4(at);
        for (int i = 0; i < shape.Parts.Length; i++)
        {
            var part = shape.Parts[i];
            if (part == null || !part.enabled || part.isTrigger || !part.gameObject.activeInHierarchy) continue;
            if (shape.PartBounds(i).SqrDistance(at) > radius * radius) continue;
            Vector3 point = toVisual.MultiplyPoint3x4(part.ClosestPoint(query));
            if ((point - at).sqrMagnitude < radius * radius) return true;
        }
        return false;
    }

    static void RedCameraQueryMatrices(Story1WreckMeshCollision shape, out Matrix4x4 toPhysics, out Matrix4x4 toVisual)
    {
        // Match the fragment's interpolated rendered pose to Unity's fixed-step
        // collider pose, as Story1WreckMeshCollision does for support/contact.
        var body = shape.Parts.Length > 0 ? shape.Parts[0].attachedRigidbody : null;
        if (body == null || body.interpolation == RigidbodyInterpolation.None)
        { toPhysics = toVisual = Matrix4x4.identity; return; }
        var physics = Matrix4x4.TRS(body.position, body.rotation, shape.transform.lossyScale);
        toPhysics = physics * shape.transform.worldToLocalMatrix;
        toVisual = shape.transform.localToWorldMatrix * physics.inverse;
    }
}
