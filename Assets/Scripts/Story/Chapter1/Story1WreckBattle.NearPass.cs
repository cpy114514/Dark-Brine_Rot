using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Story1WreckBattle
{
    sealed class RedNearHeroSkin
    {
        public SkinnedMeshRenderer skin;
        public byte[] region;
    }
    struct RedNearEnvelope
    {
        public Vector3 a,b;
        public float radius;
        public int vertices;
    }
    readonly List<RedNearHeroSkin> redNearHeroSkins=new List<RedNearHeroSkin>();
    readonly List<Vector3> redNearHeroVertices=new List<Vector3>();
    readonly List<Vector3> redNearSkullVertices=new List<Vector3>();
    readonly List<float> redNearRayHits=new List<float>();
    readonly RedNearEnvelope[] redNearEnvelopes=new RedNearEnvelope[3];
    Mesh redNearHeroMesh,redNearSharkSource;
    int[] redNearTriangles;
    Vector3[] redNearSharkWorld,redNearTriangleMin,redNearTriangleMax;
    Bounds redNearSharkBounds;
    float redNearNextSample;
    bool redNearTopologyClosed;
    static readonly Vector3[] RedNearInsideDirections={new Vector3(.913f,.267f,.307f).normalized,
        new Vector3(-.319f,.871f,.373f).normalized,new Vector3(.223f,-.397f,.890f).normalized};

    /// <summary>Opt-in diagnostic. The caller samples only after the final shark pose/root placement.</summary>
    public bool RedesignNearPassDiagnostics { get; set; }
    public bool RedesignNearPassValid { get; private set; }
    public float RedesignNearPassSampleClock { get; private set; } = -1;
    /// <summary>Positive envelope clearance; negative means conservative envelope overlap/containment, not proof of hero-mesh penetration. Never clamped.</summary>
    public float RedesignNearPassSignedGap { get; private set; } = float.NaN;
    /// <summary>Skull, chest, hips respectively. These are conservative native-mesh envelopes, not full-body proof.</summary>
    public Vector3 RedesignNearPassRegionGaps { get; private set; }
    public string RedesignNearPassClosestRegion { get; private set; }
    public bool RedesignNearPassEnvelopeMeasured { get; private set; }
    public Vector3 RedesignNearPassEnvelopeRadii { get; private set; }
    public Vector3Int RedesignNearPassEnvelopeVertices { get; private set; }
    public bool RedesignNearPassMeshClosed { get; private set; }
    public bool RedesignNearPassInside { get; private set; }
    /// <summary>True when ray votes disagree, or the welded source topology is not consistently closed.</summary>
    public bool RedesignNearPassInsideUncertain { get; private set; }
    public Vector3 RedesignNearPassHeroPoint { get; private set; }
    public Vector3 RedesignNearPassSharkPoint { get; private set; }

    /// <summary>Call after RedShark in Duel 10..13. At most one diagnostic sample per interval.</summary>
    void RedMeasureNearPass(float interval=.1f)
    {
        if(!RedesignNearPassDiagnostics||Time.time<redNearNextSample)return;
        redNearNextSample=Time.time+Mathf.Max(.033f,interval);
        RedesignNearPassValid=false;
        var skin=RedContactSkin(); // Reuses Contact's posed mesh and vertex buffer; one bake at this sample.
        if(skin==null||redPosedVertices.Count==0)return;
        if(redNearSharkSource!=skin.sharedMesh)
        {
            redNearSharkSource=skin.sharedMesh;redNearTriangles=skin.sharedMesh.triangles;
            redNearTopologyClosed=RedNearClosedTopology(skin.sharedMesh,redNearTriangles);
            redNearSharkWorld=new Vector3[redPosedVertices.Count];
            redNearTriangleMin=new Vector3[redNearTriangles.Length/3];redNearTriangleMax=new Vector3[redNearTriangleMin.Length];
        }
        for(int i=0;i<redPosedVertices.Count;i++)
        {
            Vector3 point=skin.transform.TransformPoint(redPosedVertices[i]);redNearSharkWorld[i]=point;
            if(i==0)redNearSharkBounds=new Bounds(point,Vector3.zero);else redNearSharkBounds.Encapsulate(point);
        }
        for(int i=0;i<redNearTriangles.Length;i+=3)
        {
            Vector3 a=redNearSharkWorld[redNearTriangles[i]],b=redNearSharkWorld[redNearTriangles[i+1]],c=redNearSharkWorld[redNearTriangles[i+2]];
            redNearTriangleMin[i/3]=Vector3.Min(a,Vector3.Min(b,c));redNearTriangleMax[i/3]=Vector3.Max(a,Vector3.Max(b,c));
        }
        RedNearReadHeroEnvelopes();
        float closest=float.PositiveInfinity;Vector3 gaps=Vector3.zero;bool anyInside=false,uncertain=!redNearTopologyClosed;
        for(int region=0;region<3;region++)
        {
            var envelope=redNearEnvelopes[region];float square=float.PositiveInfinity;
            Vector3 heroPoint=envelope.a,sharkPoint=envelope.a;
            Vector3 axisMin=Vector3.Min(envelope.a,envelope.b),axisMax=Vector3.Max(envelope.a,envelope.b);
            for(int i=0;i<redNearTriangles.Length;i+=3)
            {
                Vector3 lower=Vector3.Max(Vector3.zero,Vector3.Max(axisMin-redNearTriangleMax[i/3],redNearTriangleMin[i/3]-axisMax));
                if(lower.sqrMagnitude>=square)continue;
                Vector3 a=redNearSharkWorld[redNearTriangles[i]],b=redNearSharkWorld[redNearTriangles[i+1]],c=redNearSharkWorld[redNearTriangles[i+2]];
                float distance=RedNearSegmentTriangle(envelope.a,envelope.b,a,b,c,out var axis,out var surface);
                if(distance<square){square=distance;heroPoint=axis;sharkPoint=surface;}
            }
            float distanceToSurface=Mathf.Sqrt(square),gap=distanceToSurface-envelope.radius;
            // Surface intersection proves envelope overlap. Interior testing is
            // still needed when a capsule is entirely swallowed by the mesh.
            if(gap>=0)
            {
                Vector3 middle=(envelope.a+envelope.b)*.5f;
                if(redNearSharkBounds.Contains(middle))
                {
                    int votes=0;
                    foreach(var direction in RedNearInsideDirections)if(RedNearInsideRay(middle,direction))votes++;
                    if(votes>0&&votes<3)uncertain=true;
                    if(votes>=2){gap=-distanceToSurface-envelope.radius;anyInside=true;}
                }
            }
            gaps[region]=gap;
            if(gap<closest)
            {
                closest=gap;RedesignNearPassClosestRegion=region==0?"skull":region==1?"chest":"hips";
                RedesignNearPassHeroPoint=heroPoint;RedesignNearPassSharkPoint=sharkPoint;
            }
        }
        RedesignNearPassSignedGap=closest;RedesignNearPassRegionGaps=gaps;
        RedesignNearPassInside=anyInside;RedesignNearPassInsideUncertain=uncertain;
        RedesignNearPassMeshClosed=redNearTopologyClosed;RedesignNearPassSampleClock=RedesignClock;
        RedesignNearPassValid=!float.IsInfinity(closest)&&!float.IsNaN(closest);
    }

    void RedNearReadHeroEnvelopes()
    {
        Vector3 head=RedHead,chest=RedChest,hips=redAnimator.GetBoneTransform(HumanBodyBones.Hips).position;
        var neck=redAnimator.GetBoneTransform(HumanBodyBones.Neck);var spine=redAnimator.GetBoneTransform(HumanBodyBones.Spine);
        redNearEnvelopes[0]=new RedNearEnvelope{a=head,b=neck!=null?neck.position:head,radius=.55f*redHeroHeight/5};
        redNearEnvelopes[1]=new RedNearEnvelope{a=chest,b=spine!=null?spine.position:chest,radius=.55f*redHeroHeight/5};
        redNearEnvelopes[2]=new RedNearEnvelope{a=hips,b=hips,radius=.55f*redHeroHeight/5};
        redNearSkullVertices.Clear();
        if(redNearHeroMesh==null)
        {
            redNearHeroMesh=new Mesh{name="Opening near-pass hero envelope"};
            var groups=new Dictionary<Transform,byte>();
            RedNearAddBoneGroup(groups,HumanBodyBones.Head,1);RedNearAddBoneGroup(groups,HumanBodyBones.Neck,1);
            RedNearAddBoneGroup(groups,HumanBodyBones.Chest,2);RedNearAddBoneGroup(groups,HumanBodyBones.UpperChest,2);RedNearAddBoneGroup(groups,HumanBodyBones.Spine,2);
            RedNearAddBoneGroup(groups,HumanBodyBones.Hips,3);
            foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var weights=skin.sharedMesh.boneWeights;var regions=new byte[skin.sharedMesh.vertexCount];
                for(int i=0;i<weights.Length;i++)
                {
                    BoneWeight weight=weights[i];float one=0,two=0,three=0;
                    void Add(int index,float amount)
                    {
                        if(index<0||index>=skin.bones.Length||!groups.TryGetValue(skin.bones[index],out byte group))return;
                        if(group==1)one+=amount;else if(group==2)two+=amount;else three+=amount;
                    }
                    Add(weight.boneIndex0,weight.weight0);Add(weight.boneIndex1,weight.weight1);Add(weight.boneIndex2,weight.weight2);Add(weight.boneIndex3,weight.weight3);
                    float highest=Mathf.Max(one,Mathf.Max(two,three));
                    if(highest>=.5f)regions[i]=highest==one?(byte)1:highest==two?(byte)2:(byte)3;
                }
                redNearHeroSkins.Add(new RedNearHeroSkin{skin=skin,region=regions});
            }
        }
        foreach(var source in redNearHeroSkins)
        {
            if(source.skin==null)continue;source.skin.BakeMesh(redNearHeroMesh,true);redNearHeroMesh.GetVertices(redNearHeroVertices);
            for(int i=0;i<source.region.Length;i++)
            {
                int group=source.region[i]-1;if(group<0)continue;
                Vector3 point=source.skin.transform.TransformPoint(redNearHeroVertices[i]);var envelope=redNearEnvelopes[group];
                if(group==0)redNearSkullVertices.Add(point);
                envelope.radius=Mathf.Max(envelope.radius,Vector3.Distance(point,RedClosestSegment(point,envelope.a,envelope.b))+.025f);
                envelope.vertices++;redNearEnvelopes[group]=envelope;
            }
        }
        RedNearFitSkullAxis();
        RedesignNearPassEnvelopeMeasured=redNearEnvelopes[0].vertices>0&&redNearEnvelopes[1].vertices>0&&redNearEnvelopes[2].vertices>0;
        RedesignNearPassEnvelopeRadii=new Vector3(redNearEnvelopes[0].radius,redNearEnvelopes[1].radius,redNearEnvelopes[2].radius);
        RedesignNearPassEnvelopeVertices=new Vector3Int(redNearEnvelopes[0].vertices,redNearEnvelopes[1].vertices,redNearEnvelopes[2].vertices);
    }
    void RedNearFitSkullAxis()
    {
        if(redNearSkullVertices.Count<3)return;
        // The head pivot sits near the bottom of the long wooden head. Fit its
        // native weighted vertices along their principal axis rather than
        // enclosing that whole length in a large pivot-centred sphere.
        Vector3 origin=RedHead,mean=Vector3.zero;
        foreach(var point in redNearSkullVertices)mean+=point-origin;
        mean/=redNearSkullVertices.Count;Vector3 centre=origin+mean;
        float xx=0,yy=0,zz=0,xy=0,xz=0,yz=0;
        foreach(var point in redNearSkullVertices)
        {
            Vector3 p=point-centre;xx+=p.x*p.x;yy+=p.y*p.y;zz+=p.z*p.z;
            xy+=p.x*p.y;xz+=p.x*p.z;yz+=p.y*p.z;
        }
        Vector3 axis=xx>=yy&&xx>=zz?Vector3.right:yy>=zz?Vector3.up:Vector3.forward;
        for(int i=0;i<12;i++)
        {
            Vector3 next=new Vector3(xx*axis.x+xy*axis.y+xz*axis.z,xy*axis.x+yy*axis.y+yz*axis.z,xz*axis.x+yz*axis.y+zz*axis.z);
            if(next.sqrMagnitude<.0000001f)break;axis=next.normalized;
        }
        float lo=float.PositiveInfinity,hi=float.NegativeInfinity;
        foreach(var point in redNearSkullVertices){float projection=Vector3.Dot(point-centre,axis);lo=Mathf.Min(lo,projection);hi=Mathf.Max(hi,projection);}
        var envelope=redNearEnvelopes[0];envelope.a=centre+axis*lo;envelope.b=centre+axis*hi;envelope.radius=.025f;
        foreach(var point in redNearSkullVertices)envelope.radius=Mathf.Max(envelope.radius,Vector3.Distance(point,RedClosestSegment(point,envelope.a,envelope.b))+.025f);
        redNearEnvelopes[0]=envelope;
    }
    void RedNearAddBoneGroup(Dictionary<Transform,byte> groups,HumanBodyBones bone,byte group)
    {var transform=redAnimator.GetBoneTransform(bone);if(transform!=null)groups[transform]=group;}

    bool RedNearInsideRay(Vector3 origin,Vector3 direction)
    {
        redNearRayHits.Clear();
        for(int i=0;i<redNearTriangles.Length;i+=3)
            if(RedNearRayTriangle(origin,direction,redNearSharkWorld[redNearTriangles[i]],redNearSharkWorld[redNearTriangles[i+1]],redNearSharkWorld[redNearTriangles[i+2]],out float distance)&&distance>.0002f)
                redNearRayHits.Add(distance);
        redNearRayHits.Sort();int crossings=0;float previous=float.NegativeInfinity;
        foreach(float distance in redNearRayHits)
            if(distance-previous>.001f){crossings++;previous=distance;}
        return (crossings&1)==1;
    }
    static bool RedNearClosedTopology(Mesh source,int[] triangles)
    {
        // Duplicate UV/normal seam vertices are welded only for topology audit.
        // All clearance distances still use the original posed triangles.
        var vertices=source.vertices;float tolerance=Mathf.Max(.000001f,source.bounds.size.magnitude*.000001f);
        var weld=new Dictionary<Vector3Int,int>();var ids=new int[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 point=vertices[i]/tolerance;var key=new Vector3Int(Mathf.RoundToInt(point.x),Mathf.RoundToInt(point.y),Mathf.RoundToInt(point.z));
            if(!weld.TryGetValue(key,out int id)){id=weld.Count;weld.Add(key,id);}ids[i]=id;
        }
        var edges=new Dictionary<ulong,Vector2Int>();
        void Edge(int from,int to)
        {
            int lo=Mathf.Min(from,to),hi=Mathf.Max(from,to);ulong key=((ulong)(uint)lo<<32)|(uint)hi;
            edges.TryGetValue(key,out var count);count.x++;count.y+=from<to?1:-1;edges[key]=count;
        }
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=ids[triangles[i]],b=ids[triangles[i+1]],c=ids[triangles[i+2]];if(a==b||b==c||c==a)continue;
            Edge(a,b);Edge(b,c);Edge(c,a);
        }
        foreach(var pair in edges)if(pair.Value.x!=2||pair.Value.y!=0)return false;
        return edges.Count>0;
    }
    static bool RedNearRayTriangle(Vector3 origin,Vector3 direction,Vector3 a,Vector3 b,Vector3 c,out float distance)
    {
        distance=0;Vector3 ab=b-a,ac=c-a,cross=Vector3.Cross(direction,ac);float determinant=Vector3.Dot(ab,cross);
        if(Mathf.Abs(determinant)<.0000001f)return false;
        float inverse=1/determinant;Vector3 from=origin-a;float u=Vector3.Dot(from,cross)*inverse;
        if(u<-.000001f||u>1.000001f)return false;
        Vector3 other=Vector3.Cross(from,ab);float v=Vector3.Dot(direction,other)*inverse;
        if(v<-.000001f||u+v>1.000001f)return false;
        distance=Vector3.Dot(ac,other)*inverse;return distance>=0;
    }
    static Vector3 RedNearTrianglePoint(Vector3 point,Vector3 a,Vector3 b,Vector3 c)
    {
        Vector3 ab=b-a,ac=c-a;
        if(Vector3.Cross(ab,ac).sqrMagnitude<.000000000001f)
        {
            Vector3 edgeA=RedClosestSegment(point,a,b),edgeB=RedClosestSegment(point,b,c),edgeC=RedClosestSegment(point,c,a);
            float squareA=(point-edgeA).sqrMagnitude,squareB=(point-edgeB).sqrMagnitude,squareC=(point-edgeC).sqrMagnitude;
            return squareA<squareB?(squareA<squareC?edgeA:edgeC):(squareB<squareC?edgeB:edgeC);
        }
        Vector3 ap=point-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
        if(d1<=0&&d2<=0)return a;
        Vector3 bp=point-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
        float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
        Vector3 cp=point-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
        float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
        float va=d3*d6-d5*d4;if(va<=0&&d4-d3>=0&&d5-d6>=0)return b+(c-b)*((d4-d3)/(d4-d3+d5-d6));
        float sum=va+vb+vc;
        if(Mathf.Abs(sum)<.0000001f)
        {
            Vector3 x=RedClosestSegment(point,a,b),y=RedClosestSegment(point,b,c),z=RedClosestSegment(point,c,a);
            return (point-x).sqrMagnitude<(point-y).sqrMagnitude?((point-x).sqrMagnitude<(point-z).sqrMagnitude?x:z):((point-y).sqrMagnitude<(point-z).sqrMagnitude?y:z);
        }
        return a+ab*(vb/sum)+ac*(vc/sum);
    }
    static float RedNearSegmentSegment(Vector3 p,Vector3 q,Vector3 a,Vector3 b,out Vector3 first,out Vector3 second)
    {
        Vector3 d=q-p,e=b-a,r=p-a;float dd=Vector3.Dot(d,d),ee=Vector3.Dot(e,e),er=Vector3.Dot(e,r);float s,t;
        if(dd<.0000001f&&ee<.0000001f){first=p;second=a;return (first-second).sqrMagnitude;}
        if(dd<.0000001f){s=0;t=Mathf.Clamp01(er/ee);}
        else
        {
            float dr=Vector3.Dot(d,r);
            if(ee<.0000001f){t=0;s=Mathf.Clamp01(-dr/dd);}
            else
            {
                float de=Vector3.Dot(d,e),denominator=dd*ee-de*de;s=denominator>0?Mathf.Clamp01((de*er-dr*ee)/denominator):0;
                t=(de*s+er)/ee;
                if(t<0){t=0;s=Mathf.Clamp01(-dr/dd);}else if(t>1){t=1;s=Mathf.Clamp01((de-dr)/dd);}
            }
        }
        first=p+d*s;second=a+e*t;return (first-second).sqrMagnitude;
    }
    static float RedNearSegmentTriangle(Vector3 p,Vector3 q,Vector3 a,Vector3 b,Vector3 c,out Vector3 axis,out Vector3 surface)
    {
        Vector3 delta=q-p;float length=delta.magnitude;
        if(length>.000001f&&RedNearRayTriangle(p,delta/length,a,b,c,out float crossing)&&crossing<=length)
        {axis=surface=p+delta*(crossing/length);return 0;}
        axis=p;surface=RedNearTrianglePoint(p,a,b,c);float best=(axis-surface).sqrMagnitude;
        Vector3 end=RedNearTrianglePoint(q,a,b,c);float square=(q-end).sqrMagnitude;if(square<best){best=square;axis=q;surface=end;}
        square=RedNearSegmentSegment(p,q,a,b,out var onAxis,out var onEdge);if(square<best){best=square;axis=onAxis;surface=onEdge;}
        square=RedNearSegmentSegment(p,q,b,c,out onAxis,out onEdge);if(square<best){best=square;axis=onAxis;surface=onEdge;}
        square=RedNearSegmentSegment(p,q,c,a,out onAxis,out onEdge);if(square<best){best=square;axis=onAxis;surface=onEdge;}
        return best;
    }

    /// <summary>Optional read-only suggestion among the current shot and two nearby lifted bearings.</summary>
    bool RedNearPassCameraSuggestion(Vector3 cameraPosition,Vector3 focus,out Vector3 suggestion,out int blockedBodyRays)
    {
        suggestion=cameraPosition;blockedBodyRays=RedNearCameraBlocked(cameraPosition);
        if(!RedesignNearPassValid)return false;
        Vector3 delta=cameraPosition-focus;int original=blockedBodyRays;
        for(int i=0;i<2;i++)
        {
            Vector3 candidate=focus+Quaternion.AngleAxis(i==0?25:-25,Vector3.up)*delta+Vector3.up*3;
            int blocked=RedNearCameraBlocked(candidate);
            if(blocked<blockedBodyRays){blockedBodyRays=blocked;suggestion=candidate;}
        }
        return blockedBodyRays<original;
    }
    int RedNearCameraBlocked(Vector3 cameraPosition)
    {
        if(redNearTriangles==null)return 3;int blocked=0;
        foreach(var envelope in redNearEnvelopes)
        {
            Vector3 target=(envelope.a+envelope.b)*.5f,delta=target-cameraPosition;float length=delta.magnitude;
            if(length<.01f){blocked++;continue;}
            bool hit=RedCameraRayBlocked(cameraPosition,target);
            if(!hit)for(int i=0;i<redNearTriangles.Length;i+=3)
                if(RedNearRayTriangle(cameraPosition,delta/length,redNearSharkWorld[redNearTriangles[i]],redNearSharkWorld[redNearTriangles[i+1]],redNearSharkWorld[redNearTriangles[i+2]],out float distance)&&distance<length-.15f){hit=true;break;}
            if(hit)blocked++;
        }
        return blocked;
    }
    void RedNearPassDispose()
    {
        if(redNearHeroMesh!=null)Destroy(redNearHeroMesh);redNearHeroMesh=null;
        redNearHeroSkins.Clear();redNearHeroVertices.Clear();redNearSkullVertices.Clear();redNearRayHits.Clear();
    }
}
