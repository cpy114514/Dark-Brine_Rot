using UnityEngine;
using System.Collections.Generic;

/// <summary>Solid mesh parts follow the original cut geometry on a single dynamic body.</summary>
public sealed class Story1WreckMeshCollision : MonoBehaviour
{
    public MeshCollider[] Parts { get; private set; }
    public MeshCollider Primary => Parts!=null && Parts.Length>0 ? Parts[0] : null;
    public bool Active => Primary!=null && Primary.enabled && gameObject.activeInHierarchy;
    int boundsFrame=-1;
    Bounds worldBounds;
    Vector3 boundsPosition,boundsScale;
    Quaternion boundsRotation;
    Bounds[] partBounds;
    Vector3[] partPositions;
    Quaternion[] partRotations;
    Rigidbody body;
    void QueryMatrices(out Matrix4x4 visualToPhysics,out Matrix4x4 physicsToVisual)
    {
        if(body==null || body.interpolation==RigidbodyInterpolation.None)
        {visualToPhysics=physicsToVisual=Matrix4x4.identity;return;}
        var physics=Matrix4x4.TRS(body.position,body.rotation,transform.lossyScale);
        visualToPhysics=physics*transform.worldToLocalMatrix;
        physicsToVisual=transform.localToWorldMatrix*physics.inverse;
    }

    public Bounds WorldBounds { get { CacheWorldGeometry();return worldBounds; } }
    public Bounds PartBounds(int index){CacheWorldGeometry();return partBounds[index];}
    public Vector3 PartPosition(int index){CacheWorldGeometry();return partPositions[index];}
    public Quaternion PartRotation(int index){CacheWorldGeometry();return partRotations[index];}
    void CacheWorldGeometry()
    {
        Vector3 position=transform.position,scale=transform.lossyScale;Quaternion rotation=transform.rotation;
        // Interpolation can update a body between Update and LateUpdate in
        // the same rendered frame. Its geometry cache must follow that pose.
        if(boundsFrame==Time.frameCount && boundsPosition==position && boundsRotation==rotation && boundsScale==scale)return;
        boundsFrame=Time.frameCount;
        boundsPosition=position;boundsRotation=rotation;boundsScale=scale;
        for(int i=0;i<Parts.Length;i++)
        {
            var at=Parts[i].transform;var local=Parts[i].sharedMesh.bounds;
            Vector3 x=at.TransformVector(Vector3.right*local.extents.x),y=at.TransformVector(Vector3.up*local.extents.y),z=at.TransformVector(Vector3.forward*local.extents.z);
            Vector3 extents=new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
            partBounds[i]=new Bounds(at.TransformPoint(local.center),extents*2);
            partPositions[i]=at.position;partRotations[i]=at.rotation;
            if(i==0)worldBounds=partBounds[i];else worldBounds.Encapsulate(partBounds[i]);
        }
    }

    public void Initialize(Mesh mesh,Story1ShipWreckAsset.Shard[] shards=null)
    {
        if(Parts!=null)
            foreach(var part in Parts)
            {
                part.enabled=false;
                if(part.gameObject==gameObject)Destroy(part);else Destroy(part.gameObject);
            }
        // Dynamic bodies require convex pieces. Separate authored cut meshes
        // preserve hull recesses instead of filling the entire fragment with a box.
        bool compound=shards!=null && shards.Length>1;
        var parts=new List<MeshCollider>();
        for(int i=0;i<(compound ? shards.Length : 1);i++)
        {
            Mesh source=compound ? shards[i].mesh : mesh;
            foreach(var geometry in Story1ConvexMeshParts.Get(source))
            {
                // Empty children avoid MeshFilter auto-assignment cooking an
                // oversized whole-mesh hull before the bounded piece is set.
                var at=new GameObject("Solid mesh "+parts.Count);at.layer=gameObject.layer;
                at.transform.SetParent(transform,false);
                if(compound)
                {
                    at.transform.localPosition=shards[i].localCenter;
                    at.transform.localScale=shards[i].localSize;
                }
                var collider=at.AddComponent<MeshCollider>();
                collider.convex=true;collider.isTrigger=false;collider.contactOffset=.008f;
                collider.sharedMesh=geometry;parts.Add(collider);
            }
        }
        Parts=parts.ToArray();
        body=GetComponent<Rigidbody>();
        partBounds=new Bounds[Parts.Length];partPositions=new Vector3[Parts.Length];partRotations=new Quaternion[Parts.Length];boundsFrame=-1;
    }
    public void SetEnabled(bool value){foreach(var part in Parts)if(part!=null)part.enabled=value;}
    public void Ignore(Collider other,bool value)
    {
        if(other==null || Parts==null)return;
        foreach(var part in Parts)if(part!=null)Physics.IgnoreCollision(other,part,value);
    }
    public bool Raycast(Ray ray,out RaycastHit nearest,float reach)
    {
        // Native scene queries see the authoritative fixed-step body pose.
        // Feet and cinematic markers follow its interpolated render pose.
        // Query the same mesh in physics space and return the rendered point,
        // avoiding centimetres of false support error on a moving board.
        QueryMatrices(out var toPhysics,out var toVisual);
        var query=new Ray(toPhysics.MultiplyPoint3x4(ray.origin),toPhysics.MultiplyVector(ray.direction));
        nearest=default;bool found=false;
        for(int i=0;i<Parts.Length;i++)
        {
            var part=Parts[i];var bounds=PartBounds(i);bounds.Expand(.04f);
            if(!bounds.IntersectRay(ray,out var entry) || entry>reach)continue;
            if(part.enabled && part.Raycast(query,out var hit,reach))
            {
                hit.point=toVisual.MultiplyPoint3x4(hit.point);hit.normal=toVisual.MultiplyVector(hit.normal).normalized;
                nearest=hit;reach=hit.distance;found=true;
            }
        }
        return found;
    }
    public Vector3 ClosestPoint(Vector3 world)
    {
        Vector3 result=world;float best=float.PositiveInfinity;
        QueryMatrices(out var toPhysics,out var toVisual);
        Vector3 query=toPhysics.MultiplyPoint3x4(world);
        for(int i=0;i<Parts.Length;i++)
        {
            var part=Parts[i];
            if(PartBounds(i).SqrDistance(world)>=best)continue;
            Vector3 point=toVisual.MultiplyPoint3x4(part.ClosestPoint(query));float distance=(point-world).sqrMagnitude;
            if(distance<best){best=distance;result=point;}
        }
        return result;
    }
}
