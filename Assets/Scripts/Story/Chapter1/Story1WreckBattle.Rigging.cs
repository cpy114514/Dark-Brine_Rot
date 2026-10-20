using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class Story1WreckBattle
{
    // Integration: tick after RedCrushShark (3 <= t < 6); final release calls
    // RedRiggingPrepareHullPiece before AddShipDetail, then RedRiggingJoinWreck.
    // No scene or source Mesh is changed. Original renderers are replaced only
    // after every mapped cut piece has its real geometry at the source pose.
    sealed class RedRiggingGroup
    {
        public MeshFilter filter;
        public Renderer renderer;
        public Collider[] colliders;
        public bool[] colliderEnabled;
        public bool rendererEnabled, cloth, replaced, hullFrozen;
        public Vector3[] original, visible, cutCoordinates;
        public Vector2[] uv;
        public Color[] colors;
        public int[] triangles;
        public RedRiggingTree tree;
        public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        public readonly List<RedRiggingPiece> pieces = new List<RedRiggingPiece>();
        public Vector3 windLocal;
        public float billow, flutter, frequency, phase, clock;
        public Matrix4x4 lastSourceMatrix;
        public float lastGeometryClock;
        public bool hasGeometry;
    }
    sealed class RedRiggingPiece
    {
        public Story1ShipWreckAsset.Fragment source;
        public GameObject go;
        public Vector3[] original, warped, world, nativeBary;
        public Vector2[] uv;
        public int[] nativeTriangle;
        public int[] triangles;
        public Matrix4x4 matrix;
        public Mesh runtimeMesh;
        public RedRiggingTree tree;
        public bool released;
    }
    readonly List<RedRiggingGroup> redRiggingGroups = new List<RedRiggingGroup>();
    readonly Dictionary<Story1ShipWreckAsset.Fragment,RedRiggingGroup> redRiggingSourceGroups = new Dictionary<Story1ShipWreckAsset.Fragment,RedRiggingGroup>();
    readonly HashSet<Story1ShipWreckAsset.Fragment> redRiggingReleased = new HashSet<Story1ShipWreckAsset.Fragment>();
    readonly HashSet<Story1ShipWreckAsset.Fragment> redRiggingFinalized = new HashSet<Story1ShipWreckAsset.Fragment>();
    RedRiggingTree redRiggingSkinTree;
    Vector3[] redRiggingPreviousSkin, redRiggingCurrentSkin, redRiggingQuerySkin;
    GameObject redRiggingRoot;
    bool redRiggingPrepared, redRiggingReflectSourceZ;
    IEnumerator<object> redRiggingPreparation;
    public int RedesignRiggingGroupCount { get; private set; }
    public int RedesignRiggingReleasedCount => redRiggingReleased.Count;
    public float RedesignRiggingMaximumSourceGap { get; private set; }
    public float RedesignRiggingMaximumClothWarpError { get; private set; }
    public double RedesignRiggingPrepareMs { get; private set; }
    public double RedesignRiggingMaximumTickMs { get; private set; }
    public double RedesignRiggingMaximumSkinReadMs { get; private set; }
    public double RedesignRiggingMaximumGeometryMs { get; private set; }
    public double RedesignRiggingMaximumSkinTreeBuildMs { get; private set; }
    public double RedesignRiggingMaximumSkinTreeRefitMs { get; private set; }
    public double RedesignRiggingMaximumNativeTreeBuildMs { get; private set; }
    public double RedesignRiggingMaximumQueryMs { get; private set; }
    public double RedesignRiggingMaximumReplaceMs { get; private set; }
    public double RedesignRiggingMaximumBodyInitializeMs { get; private set; }
    public int RedesignRiggingMaximumSweepSamples { get; private set; }
    public int RedesignRiggingSkinTriangleCount { get; private set; }
    public long RedesignRiggingMaximumTrianglePairTests { get; private set; }
    static double RedRiggingElapsed(long started)=>(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
    void RedWarmSharkTree()
    {
        if(redRiggingSkinTree==null)
        {
            var owner=GetComponent<Story1SharkCollisionSequence>();
            var skin=owner!=null&&owner.swimmer!=null?owner.swimmer.GetComponentInChildren<SkinnedMeshRenderer>():null;
            if(skin==null||!skin.sharedMesh.isReadable)return;
            redRiggingSkinTree=new RedRiggingTree(skin.sharedMesh.vertices,skin.sharedMesh.triangles,true);
        }
        redRiggingSkinTree.BuildSlice(2);
    }

    public static object DiagnosticRiggingMeshes(Transform ship)
    {
        if(ship==null)throw new ArgumentNullException(nameof(ship));
        var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        var sails=Resources.Load<ShipSailSet>("Sailing/v039/ShipSails");
        var readable=Resources.Load<Story1RiggingContactSet>("Sailing/v217/RiggingContacts");
        var model=Story1ShipWreckAsset.ModelToWorld(ship);var scale=ship.lossyScale*.01f;
        var reflection=model*Matrix4x4.Scale(new Vector3(1,1,-1))*model.inverse;
        object MeshInfo(Mesh mesh)=>mesh==null?null:new {mesh.name,id=mesh.GetEntityId().ToString(),mesh.vertexCount,mesh.isReadable,
            triangleIndices=mesh.isReadable?mesh.triangles.Length:-1,
            submeshes=Enumerable.Range(0,mesh.subMeshCount).Select(i=>new {i,topology=mesh.GetTopology(i).ToString(),indices=mesh.GetIndexCount(i)}).ToArray()};
        var filters=ship.GetComponentsInChildren<MeshFilter>(true);
        var alignments=new List<object>();
        foreach(var panel in sails.panels)
        {
            var filter=filters.FirstOrDefault(f=>f.name==panel.node);if(filter==null)continue;
            var mesh=panel.mesh;
            if(mesh==null||!mesh.isReadable||mesh.triangles.Length==0)
            {alignments.Add(new {node=panel.node,mesh=MeshInfo(mesh),alignment="native render mesh unavailable"});continue;}
            var tree=new RedRiggingTree(mesh.vertices.Select(v=>filter.transform.TransformPoint(v)).ToArray(),mesh.triangles);
            double normal=0,flipped=0;int count=0;float normalMax=0,flippedMax=0;
            foreach(var source in asset.details.Where(p=>p.sourceObject==panel.node))
            {
                var matrix=Matrix4x4.TRS(model.MultiplyPoint3x4(source.sourceCenter),Story1ShipWreckAsset.SourceRotation(ship,source),Vector3.Scale(source.sourceSize,scale));
                var points=source.mesh.vertices;int step=Mathf.Max(1,points.Length/256);
                for(int i=0;i<points.Length;i+=step)
                {
                    Vector3 p=matrix.MultiplyPoint3x4(points[i]),q=reflection.MultiplyPoint3x4(p);
                    tree.Closest(p,out _,out _,out var a);tree.Closest(q,out _,out _,out var b);
                    float d=(p-a).sqrMagnitude,e=(q-b).sqrMagnitude;normal+=d;flipped+=e;count++;
                    normalMax=Mathf.Max(normalMax,Mathf.Sqrt(d));flippedMax=Mathf.Max(flippedMax,Mathf.Sqrt(e));
                }
            }
            alignments.Add(new {node=panel.node,mesh=MeshInfo(mesh),samples=count,normalRms=Math.Sqrt(normal/Math.Max(1,count)),sourceZReflectedRms=Math.Sqrt(flipped/Math.Max(1,count)),normalMax,sourceZReflectedMax=flippedMax});
        }
        return new {playing=Application.isPlaying,ship=ship.name,
            runtime=filters.Select(f=>new {node=f.name,path=RedRiggingPath(f.transform,ship),mesh=MeshInfo(f.sharedMesh),renderer=f.GetComponent<Renderer>()?.enabled,
                material=f.GetComponent<Renderer>()?.sharedMaterial?.name,shader=f.GetComponent<Renderer>()?.sharedMaterial?.shader?.name}).ToArray(),
            nativeAsset=asset.originalShip.GetComponentsInChildren<MeshFilter>(true).Select(f=>new {node=f.name,mesh=MeshInfo(f.sharedMesh)}).ToArray(),
            readableSnapshot=readable?.nodes?.Select(n=>new {n.name,mesh=MeshInfo(n.mesh)}).ToArray(),
            windAssets=sails.panels.Select(p=>new {p.node,mesh=MeshInfo(p.mesh)}).ToArray(),alignments};
    }
    public object DiagnosticPrepareRiggingGeometry()
    {
        if(!Application.isPlaying || !RedesignActive)throw new InvalidOperationException("Inspect prepared native rigging during the active opening.");
        if(!RedRiggingPrepare())return new {ready=false};
        var groups=new List<object>();
        foreach(var g in redRiggingGroups)
        {
            RedRiggingGeometry(g);float maximum=0;
            if(g.cloth)foreach(var p in g.pieces)
            {
                foreach(var vertex in p.world)if(g.tree.Closest(vertex,out _,out _,out var near))maximum=Mathf.Max(maximum,Vector3.Distance(vertex,near));
                for(int k=0;k<p.triangles.Length;k+=3)
                {
                    Vector3 centre=(p.world[p.triangles[k]]+p.world[p.triangles[k+1]]+p.world[p.triangles[k+2]])/3;
                    if(g.tree.Closest(centre,out _,out _,out var near))maximum=Mathf.Max(maximum,Vector3.Distance(centre,near));
                }
            }
            groups.Add(new {source=g.filter.name,g.cloth,nativeTriangleIndices=g.triangles.Length,cutPieces=g.pieces.Count,
                cutTriangleIndices=g.pieces.Sum(p=>p.triangles.Length),nativeSurfaceMaximumError=maximum});
        }
        return new {ready=true,sourceZReflected=redRiggingReflectSourceZ,RedesignRiggingPrepareMs,groups};
    }
    static string RedRiggingPath(Transform node,Transform root)
    {
        string path=node.name;for(var p=node.parent;p!=null&&p!=root;p=p.parent)path=p.name+"/"+path;return path;
    }

    bool RedRiggingPrepare()
    {
        if(redRiggingPrepared)return true;
        if(shipWreck==null || sequence==null || sequence.ship==null)return false;
        // Update's normal warm-up owns geometry construction. Never wake the
        // whole inactive debrisRoot just to release one sail or mast segment.
        if(shipWreck.details.Any(p=>!preparedPieces.ContainsKey(p)))return false;
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            if(redRiggingPreparation==null)redRiggingPreparation=RedRiggingPrepareGeometry();
            do
            {
                if(!redRiggingPreparation.MoveNext())
                {redRiggingPreparation.Dispose();redRiggingPreparation=null;redRiggingPrepared=true;RedEvent("Rigging preparation complete");break;}
            }while(RedRiggingElapsed(started)<6);
            return redRiggingPrepared;
        }
        finally{RedesignRiggingPrepareMs+=RedRiggingElapsed(started);}
    }
    IEnumerator<object> RedRiggingPrepareGeometry()
    {
        var sails=Resources.Load<ShipSailSet>("Sailing/v039/ShipSails");
        var native=Resources.Load<Story1RiggingContactSet>("Sailing/v217/RiggingContacts");
        if(native==null || native.sourceSha256!=shipWreck.sourceSha256)
            throw new InvalidOperationException("Rigging contact needs the readable v217 snapshot of the current original ship.");
        var sailNames=new HashSet<string>(sails?.panels?.Select(p=>p.node) ?? Enumerable.Empty<string>());
        var filters=sequence.ship.GetComponentsInChildren<MeshFilter>(true);
        var model=Story1ShipWreckAsset.ModelToWorld(sequence.ship.transform);
        var scale=sequence.ship.transform.lossyScale*.01f;
        foreach(var step in RedRiggingFindSourceParity(model,scale,filters,sails))yield return step;
        foreach(var sourceGroup in shipWreck.details.GroupBy(p=>p.sourceObject))
        {
            var filter=filters.FirstOrDefault(f=>f.name==sourceGroup.Key);
            var renderer=filter!=null?filter.GetComponent<Renderer>():null;
            if(filter==null || filter.sharedMesh==null || renderer==null)continue;
            bool cloth=sailNames.Contains(sourceGroup.Key);
            // Limit early destruction to the upper rigging. Low hull, cabin,
            // rail and deck pieces keep the existing t6 body-crush release.
            if(!cloth && renderer.bounds.max.y<redCrushContact.y+20)continue;
            var mesh=filter.sharedMesh.isReadable?filter.sharedMesh:native.nodes.Single(n=>n.name==sourceGroup.Key).mesh;
            if(mesh==null || !mesh.isReadable || mesh.triangles.Length==0)
                throw new InvalidOperationException("Rigging native readable geometry is absent: "+sourceGroup.Key);
            var group=new RedRiggingGroup{filter=filter,renderer=renderer,rendererEnabled=renderer.enabled,
                colliders=filter.GetComponents<Collider>(),cloth=cloth,original=mesh.vertices,
                colors=mesh.colors,uv=mesh.uv,triangles=mesh.triangles};
            group.colliderEnabled=group.colliders.Select(c=>c.enabled).ToArray();
            group.visible=new Vector3[group.original.Length];
            if(cloth)
            {
                var toModel=model.inverse*filter.transform.localToWorldMatrix;
                group.cutCoordinates=group.original.Select(v=>
                {
                    Vector3 p=toModel.MultiplyPoint3x4(v);
                    return new Vector3(-p.x,redRiggingReflectSourceZ?-p.z:p.z,p.y);
                }).ToArray();
            }
            foreach(var source in sourceGroup)
            {
                var piece=new RedRiggingPiece{source=source,go=preparedPieces[source],original=source.mesh.vertices,triangles=source.mesh.triangles};
                var old=Matrix4x4.TRS(model.MultiplyPoint3x4(source.sourceCenter),Story1ShipWreckAsset.SourceRotation(sequence.ship.transform,source),Vector3.Scale(source.sourceSize,scale));
                piece.matrix=RedRiggingPieceMatrix(source,model,scale);
                if(cloth)RedRiggingCutNativeCloth(group,piece);
                else if(redRiggingReflectSourceZ)
                {
                    var correction=piece.matrix.inverse*model*Matrix4x4.Scale(new Vector3(1,1,-1))*model.inverse*old;
                    for(int i=0;i<piece.original.Length;i++)piece.original[i]=correction.MultiplyPoint3x4(piece.original[i]);
                    for(int i=0;i<piece.triangles.Length;i+=3){int swap=piece.triangles[i+1];piece.triangles[i+1]=piece.triangles[i+2];piece.triangles[i+2]=swap;}
                }
                piece.warped=new Vector3[piece.original.Length];piece.world=new Vector3[piece.original.Length];
                group.pieces.Add(piece);
                redRiggingSourceGroups.Add(source,group);
            }
            redRiggingGroups.Add(group);
            RedRiggingGeometry(group);
            yield return null;
        }
    }

    Matrix4x4 RedRiggingPieceMatrix(Story1ShipWreckAsset.Fragment source,Matrix4x4 model,Vector3 scale)
    {
        Vector3 centre=source.sourceCenter;if(redRiggingReflectSourceZ)centre.z=-centre.z;
        return Matrix4x4.TRS(model.MultiplyPoint3x4(centre),Story1ShipWreckAsset.SourceRotation(sequence.ship.transform,source),Vector3.Scale(source.sourceSize,scale));
    }
    IEnumerable<object> RedRiggingFindSourceParity(Matrix4x4 model,Vector3 scale,MeshFilter[] filters,ShipSailSet sails)
    {
        var reflection=model*Matrix4x4.Scale(new Vector3(1,1,-1))*model.inverse;
        double normal=0,mirrored=0;int count=0;
        foreach(var panel in sails.panels)
        {
            var filter=filters.Single(f=>f.name==panel.node);var mesh=panel.mesh;
            if(!mesh.isReadable || mesh.triangles.Length==0)throw new InvalidOperationException("Wind contact mesh is not readable: "+panel.node);
            var tree=new RedRiggingTree(mesh.vertices.Select(v=>filter.transform.TransformPoint(v)).ToArray(),mesh.triangles);
            yield return null;
            foreach(var source in shipWreck.details.Where(p=>p.sourceObject==panel.node))
            {
                var matrix=Matrix4x4.TRS(model.MultiplyPoint3x4(source.sourceCenter),Story1ShipWreckAsset.SourceRotation(sequence.ship.transform,source),Vector3.Scale(source.sourceSize,scale));
                var vertices=source.mesh.vertices;int step=Mathf.Max(1,vertices.Length/128);
                for(int i=0;i<vertices.Length;i+=step)
                {
                    Vector3 p=matrix.MultiplyPoint3x4(vertices[i]),q=reflection.MultiplyPoint3x4(p);
                    tree.Closest(p,out _,out _,out var a);tree.Closest(q,out _,out _,out var b);
                    normal+=(p-a).sqrMagnitude;mirrored+=(q-b).sqrMagnitude;count++;
                }
                yield return null;
            }
        }
        bool reflect=mirrored<normal;
        RedEvent("Native rigging source parity; source Z reflected="+reflect+"; original RMS="+Math.Sqrt(normal/Math.Max(1,count)).ToString("F4")+"; reflected RMS="+Math.Sqrt(mirrored/Math.Max(1,count)).ToString("F4"));
        redRiggingReflectSourceZ=reflect;
    }
    struct RedRiggingClipVertex
    {
        public Vector3 point,bary;
        public RedRiggingClipVertex(Vector3 p,Vector3 b){point=p;bary=b;}
    }
    static void RedRiggingClipPlane(List<RedRiggingClipVertex> input,List<RedRiggingClipVertex> output,int axis,float limit,bool above)
    {
        output.Clear();if(input.Count==0)return;
        var previous=input[input.Count-1];float d=above?previous.point[axis]-limit:limit-previous.point[axis];bool before=d>=-.000001f;
        foreach(var current in input)
        {
            float e=above?current.point[axis]-limit:limit-current.point[axis];bool after=e>=-.000001f;
            if(after!=before)
            {
                float u=d/(d-e);output.Add(new RedRiggingClipVertex(Vector3.LerpUnclamped(previous.point,current.point,u),Vector3.LerpUnclamped(previous.bary,current.bary,u)));
            }
            if(after)output.Add(current);previous=current;d=e;before=after;
        }
    }
    void RedRiggingCutNativeCloth(RedRiggingGroup group,RedRiggingPiece piece)
    {
        // v002 uses a regular <=14-source-metre grid for detail cells. Clip the
        // actual subdivided Wind triangles by the SAME named cell boundaries,
        // carrying barycentric coordinates and original UVs. Every generated
        // triangle remains inside a real GPU triangle even after deformation.
        var bounds=new Bounds(group.cutCoordinates[0],Vector3.zero);foreach(var p in group.cutCoordinates)bounds.Encapsulate(p);
        var parts=piece.source.name.Split('_');var cells=new[]{int.Parse(parts[parts.Length-3]),int.Parse(parts[parts.Length-2]),int.Parse(parts[parts.Length-1])};
        var counts=new[]{Mathf.Clamp(Mathf.CeilToInt(bounds.size.x/14),1,5),Mathf.Clamp(Mathf.CeilToInt(bounds.size.y/14),1,5),Mathf.Clamp(Mathf.CeilToInt(bounds.size.z/14),1,5)};
        var points=new List<Vector3>();var triangles=new List<int>();var nativeTriangles=new List<int>();var bary=new List<Vector3>();var uv=new List<Vector2>();
        var polygon=new List<RedRiggingClipVertex>(10);var temporary=new List<RedRiggingClipVertex>(10);
        var toPiece=piece.matrix.inverse*group.filter.transform.localToWorldMatrix;
        for(int k=0;k<group.triangles.Length;k+=3)
        {
            int a=group.triangles[k],b=group.triangles[k+1],c=group.triangles[k+2];
            polygon.Clear();polygon.Add(new RedRiggingClipVertex(group.cutCoordinates[a],Vector3.right));polygon.Add(new RedRiggingClipVertex(group.cutCoordinates[b],Vector3.up));polygon.Add(new RedRiggingClipVertex(group.cutCoordinates[c],Vector3.forward));
            for(int axis=0;axis<3 && polygon.Count>0;axis++)
            {
                float span=bounds.size[axis]/counts[axis];
                if(cells[axis]>0){RedRiggingClipPlane(polygon,temporary,axis,bounds.min[axis]+span*cells[axis],true);var swap=polygon;polygon=temporary;temporary=swap;}
                if(cells[axis]<counts[axis]-1){RedRiggingClipPlane(polygon,temporary,axis,bounds.min[axis]+span*(cells[axis]+1),false);var swap=polygon;polygon=temporary;temporary=swap;}
            }
            for(int i=1;i<polygon.Count-1;i++)
            {
                var first=polygon[0];var second=polygon[i];var third=polygon[i+1];
                if(Vector3.Cross(second.point-first.point,third.point-first.point).sqrMagnitude<.0000000001f)continue;
                foreach(var v in new[]{first,second,third})
                {
                    int id=points.Count;Vector3 native=group.original[a]*v.bary.x+group.original[b]*v.bary.y+group.original[c]*v.bary.z;
                    points.Add(toPiece.MultiplyPoint3x4(native));triangles.Add(id);nativeTriangles.Add(k);bary.Add(v.bary);
                    uv.Add(group.uv[a]*v.bary.x+group.uv[b]*v.bary.y+group.uv[c]*v.bary.z);
                }
            }
        }
        if(triangles.Count<3)throw new InvalidOperationException("Native cloth cell has no surface: "+piece.source.name);
        piece.original=points.ToArray();piece.triangles=triangles.ToArray();piece.nativeTriangle=nativeTriangles.ToArray();piece.nativeBary=bary.ToArray();piece.uv=uv.ToArray();
    }

    static float RedRiggingFloat(Material material,string name,float fallback)
        =>material!=null && material.HasProperty(name)?material.GetFloat(name):fallback;
    void RedRiggingClothState(RedRiggingGroup group)
    {
        var material=group.renderer.sharedMaterial;
        group.renderer.GetPropertyBlock(group.properties);
        group.windLocal=group.properties.GetVector("_WindLocal");
        if(group.windLocal.sqrMagnitude<.00000001f)
        {
            Vector2 direction=ocean!=null?ocean.wave1.direction.normalized:new Vector2(.82f,.57f).normalized;
            group.windLocal=group.filter.transform.InverseTransformVector(new Vector3(direction.x,0,direction.y));
        }
        // ShipSailWind writes precisely these two block properties at order320.
        group.clock=group.properties.GetFloat("_SailTime");
        group.billow=RedRiggingFloat(material,"_Billow",1.8f);group.flutter=RedRiggingFloat(material,"_Flutter",.25f);
        group.frequency=RedRiggingFloat(material,"_Frequency",6);group.phase=RedRiggingFloat(material,"_Phase",0);
    }
    static Vector3 RedRiggingClothPoint(RedRiggingGroup g,Vector3 p,Color color)
    {
        // Exact CPU counterpart of Assets/Shaders/ShipWindCloth.shader.
        float phase=color.a*6.283185f+g.phase;
        float gust=.72f+.18f*Mathf.Sin(g.clock*.83f+g.phase)+.10f*Mathf.Sin(g.clock*1.71f+phase);
        float ripple=Mathf.Sin(g.clock*g.frequency-phase*2)*.65f+Mathf.Sin(g.clock*(g.frequency*1.47f)-phase*3.1f)*.35f;
        return p+g.windLocal*(color.r*(g.billow*gust+g.flutter*color.g*ripple));
    }
    void RedRiggingGeometry(RedRiggingGroup group)
    {
        if(group.cloth)RedRiggingClothState(group);
        var sourceToWorld=group.filter.transform.localToWorldMatrix;
        if(group.hasGeometry&&sourceToWorld==group.lastSourceMatrix&&(!group.cloth||group.clock==group.lastGeometryClock))return;
        group.hasGeometry=true;group.lastSourceMatrix=sourceToWorld;group.lastGeometryClock=group.clock;
        for(int i=0;i<group.original.Length;i++)
            group.visible[i]=sourceToWorld.MultiplyPoint3x4(group.cloth?RedRiggingClothPoint(group,group.original[i],group.colors.Length==group.original.Length?group.colors[i]:new Color(0,0,0,0)):group.original[i]);
        if(group.tree==null)
        {
            long started=System.Diagnostics.Stopwatch.GetTimestamp();group.tree=new RedRiggingTree(group.visible,group.triangles);
            RedesignRiggingMaximumNativeTreeBuildMs=Math.Max(RedesignRiggingMaximumNativeTreeBuildMs,RedRiggingElapsed(started));
        }
        else group.tree.Refit(group.visible);
        var model=Story1ShipWreckAsset.ModelToWorld(sequence.ship.transform);var scale=sequence.ship.transform.lossyScale*.01f;
        foreach(var p in group.pieces)
        {
            if(p.released)continue;
            p.matrix=RedRiggingPieceMatrix(p.source,model,scale);
            Vector3 at=p.matrix.GetColumn(3);Quaternion rotation=p.matrix.rotation;
            Vector3 size=Vector3.Scale(p.source.sourceSize,scale);
            var inverse=p.matrix.inverse;
            for(int i=0;i<p.original.Length;i++)
            {
                Vector3 world;
                if(group.cloth)
                {
                    int k=p.nativeTriangle[i];var bary=p.nativeBary[i];
                    world=group.visible[group.triangles[k]]*bary.x+group.visible[group.triangles[k+1]]*bary.y+group.visible[group.triangles[k+2]]*bary.z;
                }
                else world=p.matrix.MultiplyPoint3x4(p.original[i]);
                p.world[i]=world;p.warped[i]=inverse.MultiplyPoint3x4(world);
            }
            if(p.tree!=null)p.tree.Refit(p.world);
            if(!group.replaced)continue;
            p.go.transform.SetPositionAndRotation(at,rotation);p.go.transform.localScale=size;
            var body=p.go.GetComponent<Rigidbody>();body.position=at;body.rotation=rotation;
            if(p.runtimeMesh!=null){p.runtimeMesh.vertices=p.warped;p.runtimeMesh.RecalculateNormals();p.runtimeMesh.RecalculateBounds();}
        }
    }
    void RedRiggingReplace(RedRiggingGroup group)
    {
        if(group.replaced)return;
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        try{RedRiggingReplaceGeometry(group);}
        finally{RedesignRiggingMaximumReplaceMs=Math.Max(RedesignRiggingMaximumReplaceMs,RedRiggingElapsed(started));}
    }
    void RedRiggingReplaceGeometry(RedRiggingGroup group)
    {
        if(redRiggingRoot==null)
        {
            redRiggingRoot=new GameObject("Original ship rigging - local physical fracture");
            SceneManager.MoveGameObjectToScene(redRiggingRoot,gameObject.scene);
        }
        foreach(var p in group.pieces)
        {
            p.go.SetActive(false);p.go.transform.SetParent(redRiggingRoot.transform,false);
            p.go.transform.SetPositionAndRotation(p.matrix.GetColumn(3),p.matrix.rotation);
            p.go.transform.localScale=Vector3.Scale(p.source.sourceSize,sequence.ship.transform.lossyScale*.01f);
            if(group.cloth || redRiggingReflectSourceZ)
            {
                if(group.cloth)
                {
                    p.runtimeMesh=new Mesh{name=p.source.name+" native wind fracture",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                    p.runtimeMesh.vertices=p.warped;p.runtimeMesh.triangles=p.triangles;p.runtimeMesh.uv=p.uv;
                }
                else
                {
                    p.runtimeMesh=Instantiate(p.source.mesh);p.runtimeMesh.name=p.source.name+" native parity fracture";
                    p.runtimeMesh.vertices=p.warped;p.runtimeMesh.triangles=p.triangles;
                }
                p.runtimeMesh.RecalculateNormals();p.runtimeMesh.RecalculateBounds();
                p.go.GetComponent<MeshFilter>().sharedMesh=p.runtimeMesh;
                p.go.AddComponent<Story1RiggingRuntimeGeometry>().mesh=p.runtimeMesh;
            }
            p.go.GetComponent<Story1WreckFloatBody>().enabled=false;
            p.go.GetComponent<Story1WreckMeshCollision>().SetEnabled(false);
            var body=p.go.GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
            body.position=p.go.transform.position;body.rotation=p.go.transform.rotation;
            p.go.SetActive(true);
        }
        float maximumWarpError=0;
        if(group.cloth&&RedesignNearPassDiagnostics)
        {
            foreach(var p in group.pieces)foreach(var world in p.world)
                if(group.tree.Closest(world,out _,out _,out Vector3 visible))maximumWarpError=Mathf.Max(maximumWarpError,Vector3.Distance(world,visible));
            RedesignRiggingMaximumClothWarpError=Mathf.Max(RedesignRiggingMaximumClothWarpError,maximumWarpError);
        }
        // Atomic geometry ownership transfer: every original cut is visible
        // before retiring its intact source renderer and source colliders.
        group.renderer.enabled=false;
        foreach(var collider in group.colliders)collider.enabled=false;
        group.replaced=true;RedesignRiggingGroupCount++;
        RedEvent("Rigging cut transfer; original="+group.filter.name+"; mapped pieces="+group.pieces.Count+"; maximum current cloth surface error="+maximumWarpError.ToString("F3"));
    }
    void RedRiggingRelease(RedRiggingGroup group,RedRiggingPiece p,Vector3 contact,Vector3 velocity)
    {
        RedRiggingReplace(group);
        var floating=p.go.GetComponent<Story1WreckFloatBody>();floating.enabled=true;
        var detail=p.go.GetComponent<Story1WreckDebris>();
        Vector3 size=Vector3.Scale(p.source.sourceSize,sequence.ship.transform.lossyScale*.01f);
        Vector3 outward=Vector3.ProjectOnPlane(p.go.transform.position-redCrushContact,Vector3.up).normalized;
        if(outward.sqrMagnitude<.01f)outward=redRight;
        // The actual contacting skin velocity drives the local failure; the
        // timber is released through the existing gravity/buoyancy body.
        Vector3 launch=Vector3.ClampMagnitude(velocity,55)*.45f+outward*10+Vector3.down*2;
        Vector3 lever=contact-p.go.transform.position;
        Vector3 spin=Vector3.ClampMagnitude(Vector3.Cross(lever,launch)*.25f,75);
        bool buoyant=p.source.material!=null && (p.source.material.name=="acmat_1"||p.source.material.name=="acmat_5"||p.source.material.name=="acmat_7"||p.source.material.name=="acmat_9");
        long bodyStarted=System.Diagnostics.Stopwatch.GetTimestamp();detail.Initialize(ocean,size,buoyant,launch,spin);
        RedesignRiggingMaximumBodyInitializeMs=Math.Max(RedesignRiggingMaximumBodyInitializeMs,RedRiggingElapsed(bodyStarted));
        if(!detailPieces.Contains(detail))detailPieces.Add(detail);
        rider?.RegisterCollision(detail.Collision);
        p.released=true;redRiggingReleased.Add(p.source);
        float gap=0;
        if(group.tree.Closest(contact,out _,out _,out Vector3 original))gap=Vector3.Distance(original,contact);
        RedesignRiggingMaximumSourceGap=Mathf.Max(RedesignRiggingMaximumSourceGap,gap);
        Effects?.Impact(contact,launch.normalized,.7f,splinters:true);
        RedEvent("Rigging contact; original="+p.source.sourceObject+"; fragment="+p.source.name+"; source surface gap="+gap.ToString("F3"));
    }
    void RedRiggingTick(float t,float dt)
    {
        if(!RedesignActive || t<3 || t>=6 || dt<=0)return;
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        try{RedRiggingTickGeometry(t,dt);}
        finally{RedesignRiggingMaximumTickMs=Math.Max(RedesignRiggingMaximumTickMs,(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency);}
    }
    void RedRiggingTickGeometry(float t,float dt)
    {
        if(!RedRiggingPrepare())return;
        long skinStarted=System.Diagnostics.Stopwatch.GetTimestamp();var skin=RedContactSkin();if(skin==null)return;
        RedesignRiggingMaximumSkinReadMs=Math.Max(RedesignRiggingMaximumSkinReadMs,RedRiggingElapsed(skinStarted));
        int count=redPosedVertices.Count;
        if(redRiggingCurrentSkin==null || redRiggingCurrentSkin.Length!=count)
        {
            redRiggingCurrentSkin=new Vector3[count];redRiggingQuerySkin=new Vector3[count];
            redRiggingPreviousSkin=null;
        }
        float travel=0;
        for(int i=0;i<count;i++)
        {
            redRiggingCurrentSkin[i]=skin.transform.TransformPoint(redPosedVertices[i]);
            if(redRiggingPreviousSkin!=null)travel=Mathf.Max(travel,Vector3.Distance(redRiggingPreviousSkin[i],redRiggingCurrentSkin[i]));
        }
        long geometryStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        foreach(var group in redRiggingGroups)RedRiggingGeometry(group);
        RedesignRiggingMaximumGeometryMs=Math.Max(RedesignRiggingMaximumGeometryMs,RedRiggingElapsed(geometryStarted));
        int samples=Mathf.Max(1,Mathf.CeilToInt(travel/1.5f));
        RedesignRiggingMaximumSweepSamples=Math.Max(RedesignRiggingMaximumSweepSamples,samples);
        double queryMs=0;long trianglePairs=0;bool sweepPrepared=false;
        // No silent capped sweep: a long frame still checks its full skin path.
        for(int sample=1;sample<=samples;sample++)
        {
            float u=(float)sample/samples;
            for(int i=0;i<count;i++)redRiggingQuerySkin[i]=redRiggingPreviousSkin!=null?Vector3.LerpUnclamped(redRiggingPreviousSkin[i],redRiggingCurrentSkin[i],u):redRiggingCurrentSkin[i];
            var sweepBounds=new Bounds(redRiggingQuerySkin[0],Vector3.zero);
            for(int i=1;i<count;i++)sweepBounds.Encapsulate(redRiggingQuerySkin[i]);
            sweepBounds.Expand(.002f);
            bool candidate=false;
            foreach(var group in redRiggingGroups)
                if(sweepBounds.Intersects(group.tree.Bounds)&&group.pieces.Exists(p=>!p.released)){candidate=true;break;}
            // Preserve every sweep sample; only skip the expensive tree refit
            // when its complete skin bounds cannot meet remaining rigging.
            if(!candidate)continue;
            redRiggingSkinTree?.BuildSlice(double.PositiveInfinity);
            if(redRiggingSkinTree==null)
            {
                long treeStarted=System.Diagnostics.Stopwatch.GetTimestamp();var triangles=skin.sharedMesh.triangles;
                RedesignRiggingSkinTriangleCount=triangles.Length/3;redRiggingSkinTree=new RedRiggingTree(redRiggingQuerySkin,triangles);
                RedesignRiggingMaximumSkinTreeBuildMs=Math.Max(RedesignRiggingMaximumSkinTreeBuildMs,RedRiggingElapsed(treeStarted));
            }
            else if(redRiggingPreviousSkin==null)
            {
                redRiggingSkinTree.BuildSlice(double.PositiveInfinity);
                redRiggingSkinTree.Refit(redRiggingQuerySkin);
                RedesignRiggingSkinTriangleCount=redRiggingSkinTree.Triangles.Length/3;
            }
            if(redRiggingPreviousSkin!=null)
            {
                long treeStarted=System.Diagnostics.Stopwatch.GetTimestamp();
                if(!sweepPrepared){redRiggingSkinTree.PrepareSweep(redRiggingPreviousSkin,redRiggingCurrentSkin);sweepPrepared=true;}
                redRiggingSkinTree.RefitSweep(redRiggingQuerySkin,u);
                RedesignRiggingMaximumSkinTreeRefitMs=Math.Max(RedesignRiggingMaximumSkinTreeRefitMs,RedRiggingElapsed(treeStarted));
            }
            redRiggingSkinTree.ResetQueryCounters();long queryStarted=System.Diagnostics.Stopwatch.GetTimestamp();double releaseMs=0;
            foreach(var group in redRiggingGroups)
            {
                if(!redRiggingSkinTree.Bounds.Intersects(group.tree.Bounds))continue;
                // The native mesh includes many triangles outside the local
                // cut cells. Query it only when an unreleased cell can touch.
                bool nativeChecked=group.replaced;
                foreach(var p in group.pieces)
                {
                    if(p.released)continue;
                    if(p.tree==null)p.tree=new RedRiggingTree(p.world,p.triangles);
                    if(!redRiggingSkinTree.Touch(p.tree,out Vector3 contact))continue;
                    if(!nativeChecked)
                    {
                        if(!redRiggingSkinTree.Touch(group.tree,out _))break;
                        nativeChecked=true;
                    }
                    Vector3 velocity=Vector3.zero;
                    if(redRiggingPreviousSkin!=null && redRiggingSkinTree.Closest(contact,out int tri,out Vector3 bary,out _))
                    {
                        var indices=redRiggingSkinTree.Triangles;
                        Vector3 Delta(int index)=>(redRiggingCurrentSkin[index]-redRiggingPreviousSkin[index])/dt;
                        velocity=Delta(indices[tri])*bary.x+Delta(indices[tri+1])*bary.y+Delta(indices[tri+2])*bary.z;
                    }
                    long releaseStarted=System.Diagnostics.Stopwatch.GetTimestamp();RedRiggingRelease(group,p,contact,velocity);releaseMs+=RedRiggingElapsed(releaseStarted);
                }
            }
            queryMs+=Math.Max(0,RedRiggingElapsed(queryStarted)-releaseMs);trianglePairs+=redRiggingSkinTree.TrianglePairTests;
        }
        RedesignRiggingMaximumQueryMs=Math.Max(RedesignRiggingMaximumQueryMs,queryMs);
        RedesignRiggingMaximumTrianglePairTests=Math.Max(RedesignRiggingMaximumTrianglePairTests,trianglePairs);
        if(redRiggingPreviousSkin==null)redRiggingPreviousSkin=new Vector3[count];
        Array.Copy(redRiggingCurrentSkin,redRiggingPreviousSkin,count);
    }
    bool RedRiggingPrepareHullPiece(Story1ShipWreckAsset.Fragment source)
    {
        if(!RedesignActive)return false;
        if(redRiggingReleased.Contains(source)||redRiggingFinalized.Contains(source))return true;
        // Unhit cloth must also retain the visible wind pose at the t6 hull
        // fracture. Freeze each entire group once before its first AddShipDetail;
        // later members must not re-pose already initialized dynamic siblings.
        RedRiggingPrepare();
        if(redRiggingSourceGroups.TryGetValue(source,out var group))
        {
            if(!group.hullFrozen){RedRiggingGeometry(group);RedRiggingReplace(group);group.hullFrozen=true;}
            var piece=group.pieces.Single(p=>p.source==source);var detail=piece.go.GetComponent<Story1WreckDebris>();
            piece.go.GetComponent<Story1WreckFloatBody>().enabled=true;
            Vector3 size=Vector3.Scale(source.sourceSize,sequence.ship.transform.lossyScale*.01f);
            bool buoyant=source.material!=null && (source.material.name=="acmat_1"||source.material.name=="acmat_5"||source.material.name=="acmat_7"||source.material.name=="acmat_9");
            // The legacy AddShipDetail would restore the known mirrored source
            // centre. Initialize corrected native geometry at its own pivot.
            long bodyStarted=System.Diagnostics.Stopwatch.GetTimestamp();detail.Initialize(ocean,size,buoyant,FragmentVelocity(piece.go.transform.position),FragmentSpin());
            RedesignRiggingMaximumBodyInitializeMs=Math.Max(RedesignRiggingMaximumBodyInitializeMs,RedRiggingElapsed(bodyStarted));
            if(!detailPieces.Contains(detail))detailPieces.Add(detail);rider?.RegisterCollision(detail.Collision);
            piece.released=true;redRiggingFinalized.Add(source);return true;
        }
        if(preparedPieces.TryGetValue(source,out var go) && go!=null)go.GetComponent<Story1WreckFloatBody>().enabled=true;
        return false;
    }
    void RedRiggingJoinWreck()
    {
        if(redRiggingRoot!=null && debrisRoot!=null)redRiggingRoot.transform.SetParent(debrisRoot.transform,true);
    }
    void RedRiggingDispose()
    {
        redRiggingPreparation?.Dispose();redRiggingPreparation=null;
        if(!shattered)
        {
            foreach(var group in redRiggingGroups)
            {
                if(!group.replaced)continue;
                if(group.renderer!=null)group.renderer.enabled=group.rendererEnabled;
                for(int i=0;i<group.colliders.Length;i++)if(group.colliders[i]!=null)group.colliders[i].enabled=group.colliderEnabled[i];
            }
            if(redRiggingRoot!=null)Destroy(redRiggingRoot);
        }
        else RedRiggingJoinWreck();
        redRiggingSkinTree=null;redRiggingPreviousSkin=redRiggingCurrentSkin=redRiggingQuerySkin=null;
    }

    // A refittable triangle tree queries posed render geometry. No invisible
    // giant box or guessed mast/cloth Collider decides the break.
    sealed class RedRiggingTree
    {
        sealed class Node { public Bounds bounds;public int start,count,left=-1,right=-1,sweepStamp,articulatedStamp; public Bounds articulatedQueryBounds; }
        readonly List<Node> nodes=new List<Node>();
        Bounds[] sweepTriangleStart,sweepTriangleEnd,sweepNodeStart,sweepNodeEnd;
        readonly int[] order;
        readonly Bounds[] triangleBounds;
        readonly Vector3[] triangleCentres;
        Vector3[] vertices;
        int sweepStamp;
        int[] sweepTriangleStamp;
        float sweepFraction;
        bool queryingSweep;
        Vector3[] conservativeReference;
        float conservativePadding;
        bool articulatedBounds,articulatedReferenceValid;
        int articulatedStamp;
        Bounds[] articulatedQueryTriangles;
        int[] articulatedQueryStamps;
        Vector3[] articulatedReference;
        float[] articulatedPointMotion,articulatedTriangleMotion,articulatedNodeMotion;
        IEnumerator<object> buildSteps;
        public bool BuildComplete=>buildSteps==null;
        public int[] Triangles { get; }
        public long TrianglePairTests { get; private set; }
        public void ResetQueryCounters(){TrianglePairTests=0;}
        public Bounds Bounds=>QueryNodeBounds(0);
        public RedRiggingTree(Vector3[] points,int[] triangles):this(points,triangles,false){}
        public RedRiggingTree(Vector3[] points,int[] triangles,bool deferred)
        {
            vertices=points;Triangles=triangles;order=Enumerable.Range(0,triangles.Length/3).ToArray();
            if(order.Length==0)throw new InvalidOperationException("Rigging contact requires real mesh triangles");
            triangleBounds=new Bounds[order.Length];triangleCentres=new Vector3[order.Length];
            if(deferred)buildSteps=BuildIncremental().GetEnumerator();
            else{CacheTriangleBounds();Build(0,order.Length);}
        }
        public void BuildSlice(double milliseconds)
        {
            if(buildSteps==null)return;
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            do
            {
                if(!buildSteps.MoveNext()){buildSteps.Dispose();buildSteps=null;break;}
            }while(RedRiggingElapsed(started)<milliseconds);
        }
        IEnumerable<object> BuildIncremental()
        {
            for(int i=0;i<order.Length;i++)
            {triangleBounds[i]=TriangleBounds(i);triangleCentres[i]=triangleBounds[i].center;if((i&1023)==1023)yield return null;}
            var pending=new Stack<(int start,int count,int parent,bool left)>();pending.Push((0,order.Length,-1,false));
            while(pending.Count>0)
            {
                var work=pending.Pop();int index=nodes.Count;var node=new Node{start=work.start,count=work.count};nodes.Add(node);
                if(work.parent>=0){if(work.left)nodes[work.parent].left=index;else nodes[work.parent].right=index;}
                node.bounds=triangleBounds[order[work.start]];
                for(int i=work.start+1;i<work.start+work.count;i++)node.bounds.Encapsulate(triangleBounds[order[i]]);
                if(work.count>8)
                {
                    Vector3 size=node.bounds.size;int axis=size.x>=size.y&&size.x>=size.z?0:size.y>=size.z?1:2;
                    float split=node.bounds.center[axis];int left=work.start,right=work.start+work.count-1;
                    while(left<=right)
                    {
                        if(triangleCentres[order[left]][axis]<split){left++;continue;}
                        int swap=order[left];order[left]=order[right];order[right--]=swap;
                    }
                    int half=left-work.start;if(half==0||half==work.count)half=work.count/2;
                    pending.Push((work.start+half,work.count-half,index,false));pending.Push((work.start,half,index,true));
                }
                yield return null;
            }
        }
        Bounds TriangleBounds(int tri)
        {
            int k=tri*3;var b=new Bounds(vertices[Triangles[k]],Vector3.zero);
            b.Encapsulate(vertices[Triangles[k+1]]);b.Encapsulate(vertices[Triangles[k+2]]);b.Expand(.002f);return b;
        }
        int Build(int start,int count)
        {
            int index=nodes.Count;var node=new Node{start=start,count=count};nodes.Add(node);
            node.bounds=triangleBounds[order[start]];for(int i=start+1;i<start+count;i++)node.bounds.Encapsulate(triangleBounds[order[i]]);
            if(count<=8)return index;
            Vector3 size=node.bounds.size;int axis=size.x>=size.y&&size.x>=size.z?0:size.y>=size.z?1:2;
            float split=node.bounds.center[axis];int left=start,right=start+count-1;
            while(left<=right)
            {
                if(triangleCentres[order[left]][axis]<split){left++;continue;}
                int swap=order[left];order[left]=order[right];order[right--]=swap;
            }
            int half=left-start;if(half==0||half==count)half=count/2;
            node.left=Build(start,half);node.right=Build(start+half,count-half);return index;
        }
        void CacheTriangleBounds(){for(int i=0;i<order.Length;i++){triangleBounds[i]=TriangleBounds(i);triangleCentres[i]=triangleBounds[i].center;}}
        public void Refit(Vector3[] points)
        {
            queryingSweep=false;articulatedBounds=false;articulatedReferenceValid=false;vertices=points;conservativeReference=null;conservativePadding=0;
            // Split centres are used only when building the topology.
            // Update exact geometry bottom-up. Avoid constructing/encapsulating
            // several Bounds values for every triangle and recursively copying them.
            for(int i=0;i<order.Length;i++)
            {
                int k=i*3;Vector3 a=points[Triangles[k]],b=points[Triangles[k+1]],c=points[Triangles[k+2]];
                Vector3 lo=Vector3.Min(a,Vector3.Min(b,c)),hi=Vector3.Max(a,Vector3.Max(b,c));
                triangleBounds[i]=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.002f);
            }
            for(int i=nodes.Count-1;i>=0;i--)
            {
                var node=nodes[i];Bounds first=node.left>=0?nodes[node.left].bounds:triangleBounds[order[node.start]];
                Vector3 lo=first.min,hi=first.max;
                if(node.left>=0){var right=nodes[node.right].bounds;lo=Vector3.Min(lo,right.min);hi=Vector3.Max(hi,right.max);}
                else for(int j=node.start+1;j<node.start+node.count;j++)
                {var box=triangleBounds[order[j]];lo=Vector3.Min(lo,box.min);hi=Vector3.Max(hi,box.max);}
                node.bounds=new Bounds((lo+hi)*.5f,hi-lo);
            }
        }
        public void RefitConservative(Vector3[] points,float maximumPadding)
        {
            float square=0;
            if(conservativeReference!=null&&conservativeReference.Length==points.Length)
                for(int i=0;i<points.Length;i++)square=Mathf.Max(square,(points[i]-conservativeReference[i]).sqrMagnitude);
            if(conservativeReference==null||conservativeReference.Length!=points.Length||square>maximumPadding*maximumPadding)
            {
                Refit(points);conservativeReference=(Vector3[])points.Clone();return;
            }
            // Every vertex lies within this distance of its last exact bounds.
            // Inflating those bounds is conservative; leaf tests still use the
            // current vertices. No stale triangle can disappear from a query.
            vertices=points;queryingSweep=false;articulatedBounds=false;articulatedReferenceValid=false;conservativePadding=Mathf.Sqrt(square)+.00001f;
        }
        public void RefitArticulated(Vector3[] points,float maximumPadding)
        {
            if(articulatedReference==null||articulatedReference.Length!=points.Length)
            {
                articulatedReference=new Vector3[points.Length];articulatedPointMotion=new float[points.Length];
                articulatedTriangleMotion=new float[triangleBounds.Length];articulatedNodeMotion=new float[nodes.Count];
                Refit(points);Array.Copy(points,articulatedReference,points.Length);articulatedReferenceValid=true;return;
            }
            if(!articulatedReferenceValid){Refit(points);Array.Copy(points,articulatedReference,points.Length);articulatedReferenceValid=true;return;}
            float maximum=0;
            for(int i=0;i<points.Length;i++)
            {float square=(points[i]-articulatedReference[i]).sqrMagnitude;articulatedPointMotion[i]=square;maximum=Mathf.Max(maximum,square);}
            if(maximum>maximumPadding*maximumPadding)
            {Refit(points);Array.Copy(points,articulatedReference,points.Length);articulatedReferenceValid=true;return;}
            // Each subtree is bounded by its greatest actual vertex motion.
            // A moving tail cannot inflate every head triangle. Reference
            // bounds stay fixed; leaf tests still use the current posed skin.
            for(int i=0;i<triangleBounds.Length;i++)
            {int k=i*3;articulatedTriangleMotion[i]=Mathf.Max(articulatedPointMotion[Triangles[k]],Mathf.Max(articulatedPointMotion[Triangles[k+1]],articulatedPointMotion[Triangles[k+2]]));}
            // Both builders insert parents before children.
            for(int i=nodes.Count-1;i>=0;i--)
            {
                var node=nodes[i];float square=0;
                if(node.left>=0)square=Mathf.Max(articulatedNodeMotion[node.left],articulatedNodeMotion[node.right]);
                else for(int j=node.start;j<node.start+node.count;j++)square=Mathf.Max(square,articulatedTriangleMotion[order[j]]);
                articulatedNodeMotion[i]=square;
            }
            vertices=points;queryingSweep=false;conservativePadding=0;articulatedBounds=true;
            if(unchecked(++articulatedStamp)==0)
            {
                articulatedStamp=1;foreach(var node in nodes)node.articulatedStamp=0;
                if(articulatedQueryStamps!=null)Array.Clear(articulatedQueryStamps,0,articulatedQueryStamps.Length);
            }
        }
        public void PrepareSweep(Vector3[] start,Vector3[] end)
        {
            if(sweepTriangleStart==null)
            {
                sweepTriangleStart=new Bounds[triangleBounds.Length];sweepTriangleEnd=new Bounds[triangleBounds.Length];
                sweepTriangleStamp=new int[triangleBounds.Length];
                sweepNodeStart=new Bounds[nodes.Count];sweepNodeEnd=new Bounds[nodes.Count];
            }
            Refit(start);Array.Copy(triangleBounds,sweepTriangleStart,triangleBounds.Length);
            for(int i=0;i<nodes.Count;i++)sweepNodeStart[i]=nodes[i].bounds;
            Refit(end);Array.Copy(triangleBounds,sweepTriangleEnd,triangleBounds.Length);
            for(int i=0;i<nodes.Count;i++)sweepNodeEnd[i]=nodes[i].bounds;
        }
        static Bounds SweepBounds(Bounds a,Bounds b,float u)
            =>new Bounds(Vector3.LerpUnclamped(a.center,b.center,u),Vector3.LerpUnclamped(a.size,b.size,u));
        public void RefitSweep(Vector3[] points,float u)
        {
            vertices=points;sweepFraction=u;queryingSweep=true;sweepStamp++;conservativePadding=0;articulatedBounds=false;articulatedReferenceValid=false;
            // Every tested vertex follows a linear segment during this sweep.
            // Interpolating endpoint minima/maxima encloses that segment at u,
            // even if a different vertex supplies the extrema at either end.
            // Exact triangle tests still use the actual intermediate vertices.
            // Only visited regions need interpolated bounds. Each queried node
            // or triangle is cached once per sample; all samples still run.
        }
        Bounds QueryNodeBounds(int index)
        {
            var node=nodes[index];
            if(queryingSweep&&node.sweepStamp!=sweepStamp)
            {node.bounds=SweepBounds(sweepNodeStart[index],sweepNodeEnd[index],sweepFraction);node.sweepStamp=sweepStamp;}
            var bounds=node.bounds;if(conservativePadding>0)bounds.Expand(conservativePadding*2);
            if(articulatedBounds)
            {
                if(node.articulatedStamp!=articulatedStamp)
                {
                    // Tighten visited small regions once per pose. Exact current
                    // leaves prevent padded boxes from multiplying triangle pairs.
                    if(node.count<=32)
                    {
                        int k=order[node.start]*3;Vector3 lo=vertices[Triangles[k]],hi=lo;
                        for(int j=node.start;j<node.start+node.count;j++)for(int c=0;c<3;c++)
                        {var p=vertices[Triangles[order[j]*3+c]];lo=Vector3.Min(lo,p);hi=Vector3.Max(hi,p);}
                        bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.002f);
                    }
                    else bounds.Expand(2*Mathf.Sqrt(articulatedNodeMotion[index])+.00002f);
                    node.articulatedQueryBounds=bounds;node.articulatedStamp=articulatedStamp;
                }
                return node.articulatedQueryBounds;
            }
            return bounds;
        }
        Bounds QueryTriangleBounds(int index)
        {
            if(queryingSweep&&sweepTriangleStamp[index]!=sweepStamp)
            {triangleBounds[index]=SweepBounds(sweepTriangleStart[index],sweepTriangleEnd[index],sweepFraction);sweepTriangleStamp[index]=sweepStamp;}
            var bounds=triangleBounds[index];if(conservativePadding>0)bounds.Expand(conservativePadding*2);
            if(articulatedBounds)
            {
                if(articulatedQueryTriangles==null)
                {articulatedQueryTriangles=new Bounds[triangleBounds.Length];articulatedQueryStamps=new int[triangleBounds.Length];}
                if(articulatedQueryStamps[index]!=articulatedStamp)
                {
                    int k=index*3;var a=vertices[Triangles[k]];var b=vertices[Triangles[k+1]];var c=vertices[Triangles[k+2]];
                    var lo=Vector3.Min(a,Vector3.Min(b,c));var hi=Vector3.Max(a,Vector3.Max(b,c));
                    bounds=new Bounds((lo+hi)*.5f,hi-lo+Vector3.one*.002f);
                    articulatedQueryTriangles[index]=bounds;articulatedQueryStamps[index]=articulatedStamp;
                }
                return articulatedQueryTriangles[index];
            }
            return bounds;
        }
        Bounds RefitNode(int index)
        {
            var n=nodes[index];
            if(n.left>=0){n.bounds=RefitNode(n.left);n.bounds.Encapsulate(RefitNode(n.right));}
            else {n.bounds=triangleBounds[order[n.start]];for(int i=n.start+1;i<n.start+n.count;i++)n.bounds.Encapsulate(triangleBounds[order[i]]);}
            return n.bounds;
        }
        public bool Touch(Vector3[] points,int[] triangles,out Vector3 contact)
        {
            contact=default;
            if(points.Length==0)return false;
            Bounds candidate=new Bounds(points[0],Vector3.zero);for(int i=1;i<points.Length;i++)candidate.Encapsulate(points[i]);
            if(!Bounds.Intersects(candidate))return false;
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 a=points[triangles[i]],b=points[triangles[i+1]],c=points[triangles[i+2]];
                Bounds triangle=new Bounds(a,Vector3.zero);triangle.Encapsulate(b);triangle.Encapsulate(c);triangle.Expand(.002f);
                if(TouchNode(0,a,b,c,triangle,out contact))return true;
            }
            // A tiny entire rigging segment can enter the closed shark between
            // frames without any of its own edges crossing the current skin.
            if(Bounds.Contains(points[0]) && (RayCount(0,new Ray(points[0],new Vector3(.9817f,.1721f,.0837f).normalized))&1)!=0)
            {Closest(points[0],out _,out _,out contact);return true;}
            return false;
        }
        public bool Touch(RedRiggingTree other,out Vector3 contact)
        {
            contact=default;
            if(!Bounds.Intersects(other.Bounds))return false;
            if(TouchPair(0,other,0,out contact))return true;
            Vector3 p=other.vertices[0];
            if(Bounds.Contains(p)&&(RayCount(0,new Ray(p,new Vector3(.9817f,.1721f,.0837f).normalized))&1)!=0)
            {Closest(p,out _,out _,out contact);return true;}
            return false;
        }
        bool TouchPair(int index,RedRiggingTree other,int otherIndex,out Vector3 contact)
        {
            contact=default;var a=nodes[index];var b=other.nodes[otherIndex];
            var boundsA=QueryNodeBounds(index);var boundsB=other.QueryNodeBounds(otherIndex);
            if(!boundsA.Intersects(boundsB))return false;
            if(a.left>=0&&(b.left<0||boundsA.size.sqrMagnitude>=boundsB.size.sqrMagnitude))
                return TouchPair(a.left,other,otherIndex,out contact)||TouchPair(a.right,other,otherIndex,out contact);
            if(b.left>=0)return TouchPair(index,other,b.left,out contact)||TouchPair(index,other,b.right,out contact);
            for(int i=a.start;i<a.start+a.count;i++)
            {
                int ti=order[i],k=ti*3;Vector3 x=vertices[Triangles[k]],y=vertices[Triangles[k+1]],z=vertices[Triangles[k+2]];
                for(int j=b.start;j<b.start+b.count;j++)
                {
                    int tj=other.order[j];
                    if(!QueryTriangleBounds(ti).Intersects(other.QueryTriangleBounds(tj)))continue;
                    TrianglePairTests++;int l=tj*3;
                    Vector3 p=other.vertices[other.Triangles[l]],q=other.vertices[other.Triangles[l+1]],r=other.vertices[other.Triangles[l+2]];
                    if(Segment(p,q,x,y,z,out contact)||Segment(q,r,x,y,z,out contact)||Segment(r,p,x,y,z,out contact)||
                       Segment(x,y,p,q,r,out contact)||Segment(y,z,p,q,r,out contact)||Segment(z,x,p,q,r,out contact))return true;
                    Vector3 near=ClosestTriangle(p,x,y,z,out _);
                    if((near-p).sqrMagnitude<=.000001f){contact=(near+p)*.5f;return true;}
                }
            }
            return false;
        }
        bool TouchNode(int index,Vector3 a,Vector3 b,Vector3 c,Bounds bounds,out Vector3 contact)
        {
            contact=default;var n=nodes[index];if(!QueryNodeBounds(index).Intersects(bounds))return false;
            if(n.left>=0)return TouchNode(n.left,a,b,c,bounds,out contact)||TouchNode(n.right,a,b,c,bounds,out contact);
            for(int i=n.start;i<n.start+n.count;i++)
            {
                TrianglePairTests++;
                int k=order[i]*3;Vector3 x=vertices[Triangles[k]],y=vertices[Triangles[k+1]],z=vertices[Triangles[k+2]];
                if(Segment(a,b,x,y,z,out contact)||Segment(b,c,x,y,z,out contact)||Segment(c,a,x,y,z,out contact)||
                   Segment(x,y,a,b,c,out contact)||Segment(y,z,a,b,c,out contact)||Segment(z,x,a,b,c,out contact))return true;
                Vector3 near=ClosestTriangle(a,x,y,z,out _);
                if((near-a).sqrMagnitude<=.000001f){contact=(near+a)*.5f;return true;}
            }
            return false;
        }
        int RayCount(int index,Ray ray)
        {
            var n=nodes[index];if(!QueryNodeBounds(index).IntersectRay(ray))return 0;
            if(n.left>=0)return RayCount(n.left,ray)+RayCount(n.right,ray);
            int count=0;
            for(int i=n.start;i<n.start+n.count;i++)
            {
                int k=order[i]*3;
                if(RayTriangle(ray.origin,ray.direction,vertices[Triangles[k]],vertices[Triangles[k+1]],vertices[Triangles[k+2]],out float t)&&t>.00001f)count++;
            }
            return count;
        }
        public bool Closest(Vector3 point,out int triangle,out Vector3 bary,out Vector3 nearest)
        {
            triangle=-1;bary=Vector3.zero;nearest=point;float distance=float.PositiveInfinity;
            ClosestNode(0,point,ref distance,ref triangle,ref bary,ref nearest);return triangle>=0;
        }
        public float CapsuleGap(Vector3 a,Vector3 b,float radius)
            =>CapsuleGapUpTo(a,b,radius,float.PositiveInfinity);
        public float CapsuleGapUpTo(Vector3 a,Vector3 b,float radius,float limit)
            =>CapsuleGapQuery(a,b,radius,limit,true);
        public float CapsuleSurfaceGapUpTo(Vector3 a,Vector3 b,float radius,float limit)
            =>CapsuleGapQuery(a,b,radius,limit,false);
        float CapsuleGapQuery(Vector3 a,Vector3 b,float radius,float limit,bool signedContainment)
        {
            float ceiling=float.IsPositiveInfinity(limit)?float.PositiveInfinity:(radius+limit)*(radius+limit);
            float square=ceiling;
            var axisBounds=new Bounds((a+b)*.5f,new Vector3(Mathf.Abs(a.x-b.x),Mathf.Abs(a.y-b.y),Mathf.Abs(a.z-b.z)));
            bool point=a.Equals(b);
            if(point)PointNearest(0,a,ref square);else CapsuleNearest(0,a,b,axisBounds,ref square);
            float gap=Mathf.Sqrt(square)-radius;
            var middle=(a+b)*.5f;
            if(signedContainment&&gap>0&&Bounds.Contains(middle)&&(RayCount(0,new Ray(middle,new Vector3(.9817f,.1721f,.0837f).normalized))&1)!=0)
            {
                // A fully contained capsule needs the exact interior depth,
                // even when no surface is within the exterior search limit.
                if(square>=ceiling&&!float.IsPositiveInfinity(ceiling)){square=float.PositiveInfinity;if(point)PointNearest(0,a,ref square);else CapsuleNearest(0,a,b,axisBounds,ref square);gap=Mathf.Sqrt(square)-radius;}
                return -gap-2*radius;
            }
            return square>=ceiling?limit:Mathf.Min(limit,gap);
        }
        void PointNearest(int index,Vector3 point,ref float square)
        {
            if(QueryNodeBounds(index).SqrDistance(point)>=square)return;
            var node=nodes[index];
            if(node.left>=0)
            {
                int a=node.left,b=node.right;
                if(QueryNodeBounds(b).SqrDistance(point)<QueryNodeBounds(a).SqrDistance(point)){a=node.right;b=node.left;}
                PointNearest(a,point,ref square);PointNearest(b,point,ref square);return;
            }
            for(int i=node.start;i<node.start+node.count;i++)
            {
                int k=order[i]*3;
                var near=RedNearTrianglePoint(point,vertices[Triangles[k]],vertices[Triangles[k+1]],vertices[Triangles[k+2]]);
                square=Mathf.Min(square,(near-point).sqrMagnitude);
            }
        }
        void CapsuleNearest(int index,Vector3 a,Vector3 b,Bounds axisBounds,ref float square)
        {
            var bounds=QueryNodeBounds(index);
            Vector3 lower=Vector3.Max(Vector3.zero,Vector3.Max(axisBounds.min-bounds.max,bounds.min-axisBounds.max));
            if(lower.sqrMagnitude>=square)return;
            var node=nodes[index];
            if(node.left>=0)
            {
                float Lower(int child){var box=QueryNodeBounds(child);return Vector3.Max(Vector3.zero,Vector3.Max(axisBounds.min-box.max,box.min-axisBounds.max)).sqrMagnitude;}
                int first=node.left,second=node.right;if(Lower(second)<Lower(first)){first=node.right;second=node.left;}
                CapsuleNearest(first,a,b,axisBounds,ref square);CapsuleNearest(second,a,b,axisBounds,ref square);return;
            }
            for(int i=node.start;i<node.start+node.count;i++)
            {
                int k=order[i]*3;
                square=Mathf.Min(square,RedNearSegmentTriangle(a,b,vertices[Triangles[k]],vertices[Triangles[k+1]],vertices[Triangles[k+2]],out _,out _));
            }
        }
        void ClosestNode(int index,Vector3 p,ref float best,ref int triangle,ref Vector3 bary,ref Vector3 nearest)
        {
            var n=nodes[index];if(QueryNodeBounds(index).SqrDistance(p)>best)return;
            if(n.left>=0)
            {
                int a=n.left,b=n.right;if(QueryNodeBounds(b).SqrDistance(p)<QueryNodeBounds(a).SqrDistance(p)){a=n.right;b=n.left;}
                ClosestNode(a,p,ref best,ref triangle,ref bary,ref nearest);ClosestNode(b,p,ref best,ref triangle,ref bary,ref nearest);return;
            }
            for(int i=n.start;i<n.start+n.count;i++)
            {
                int k=order[i]*3;Vector3 q=ClosestTriangle(p,vertices[Triangles[k]],vertices[Triangles[k+1]],vertices[Triangles[k+2]],out var weights);
                float d=(p-q).sqrMagnitude;if(d<best){best=d;triangle=k;bary=weights;nearest=q;}
            }
        }
        static bool Segment(Vector3 a,Vector3 b,Vector3 x,Vector3 y,Vector3 z,out Vector3 point)
        {
            point=default;if(!RayTriangle(a,b-a,x,y,z,out float t)||t<-.000001f||t>1.000001f)return false;
            point=Vector3.LerpUnclamped(a,b,t);return true;
        }
        static bool RayTriangle(Vector3 p,Vector3 d,Vector3 a,Vector3 b,Vector3 c,out float t)
        {
            t=0;Vector3 e=b-a,f=c-a,h=Vector3.Cross(d,f);float det=Vector3.Dot(e,h);
            if(Mathf.Abs(det)<.00000001f)return false;float inverse=1/det;Vector3 s=p-a;float u=inverse*Vector3.Dot(s,h);
            if(u<-.000001f||u>1.000001f)return false;Vector3 q=Vector3.Cross(s,e);float v=inverse*Vector3.Dot(d,q);
            if(v<-.000001f||u+v>1.000001f)return false;t=inverse*Vector3.Dot(f,q);return true;
        }
        static Vector3 ClosestTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c,out Vector3 bary)
        {
            Vector3 ab=b-a,ac=c-a,ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
            if(d1<=0&&d2<=0){bary=Vector3.right;return a;}
            Vector3 bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);
            if(d3>=0&&d4<=d3){bary=Vector3.up;return b;}
            float vc=d1*d4-d3*d2;
            if(vc<=0&&d1>=0&&d3<=0){float v=d1/(d1-d3);bary=new Vector3(1-v,v,0);return a+ab*v;}
            Vector3 cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);
            if(d6>=0&&d5<=d6){bary=Vector3.forward;return c;}
            float vb=d5*d2-d1*d6;
            if(vb<=0&&d2>=0&&d6<=0){float w=d2/(d2-d6);bary=new Vector3(1-w,0,w);return a+ac*w;}
            float va=d3*d6-d5*d4;
            if(va<=0&&d4-d3>=0&&d5-d6>=0){float w=(d4-d3)/((d4-d3)+(d5-d6));bary=new Vector3(0,1-w,w);return b+(c-b)*w;}
            float sum=va+vb+vc;
            if(Mathf.Abs(sum)<.0000000001f){bary=Vector3.right;return a;}
            float v2=vb/sum,w2=vc/sum;bary=new Vector3(1-v2-w2,v2,w2);return a+ab*v2+ac*w2;
        }
    }
}

// A frozen cloth fragment's cloned mesh lasts exactly as long as its physical
// object. Shared project Mesh assets are never edited or destroyed.
public sealed class Story1RiggingRuntimeGeometry : MonoBehaviour
{
    public Mesh mesh;
    void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
