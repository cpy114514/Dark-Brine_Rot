using System;using System.Collections.Generic;using UnityEngine;
namespace Mavis
{
    // Cinematic-only hard-surface manipulator. Both load paths start inside the
    // aircraft's actual underside mesh; damage and player control remain elsewhere.
    public sealed class BombardinoCaptureClaws : IDisposable
    {
        readonly Transform frame;
        readonly GameObject visual;
        readonly Mesh mesh;
        readonly Material[] materials;
        readonly List<Vector3> vertices=new List<Vector3>(12000);
        readonly List<Vector3> normals=new List<Vector3>(12000);
        readonly List<int>[] triangles={new List<int>(14000),new List<int>(10000),new List<int>(5000),new List<int>(1000)};
        readonly Vector3[] mounts=new Vector3[2];
        Vector3 localCentre,localRight=Vector3.right;
        float span=.65f;
        public Vector3 GripLocal {get;}
        public Vector3 MountLocal(int side)=>mounts[side];
        public int VertexCount=>mesh?mesh.vertexCount:0;
        public BombardinoCaptureClaws(Transform parent)
        {
            frame=parent;
            var hull=parent.GetComponent<MeshFilter>();
            Bounds bounds=hull&&hull.sharedMesh?hull.sharedMesh.bounds:new Bounds(Vector3.zero,new Vector3(14,2,10));
            float separation=Mathf.Clamp(bounds.size.x*.06f,.65f,1.05f);
            for(int i=0;i<2;i++)
            {
                var p=new Vector3(bounds.center.x+(i==0?-separation:separation),bounds.min.y,bounds.center.z);
                mounts[i]=Underside(hull,p,bounds);
            }
            GripLocal=(mounts[0]+mounts[1])*.5f-Vector3.up*Mathf.Clamp(bounds.size.y*.8f,1.3f,2.1f);
            localCentre=GripLocal;
            visual=new GameObject("Paired mechanical capture arms",typeof(MeshFilter),typeof(MeshRenderer));
            visual.transform.SetParent(parent,false);
            mesh=new Mesh{name="Aircraft-mounted hydraulic capture rig"};mesh.MarkDynamic();
            visual.GetComponent<MeshFilter>().sharedMesh=mesh;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            materials=new[]{Metal(shader,"Grab arm graphite alloy",new Color(.115f,.145f,.16f),.78f,.43f),
                Metal(shader,"Grab arm brushed steel",new Color(.49f,.55f,.58f),.88f,.63f),
                Metal(shader,"Grab arm rubber pads",new Color(.025f,.031f,.035f),.05f,.26f),
                Metal(shader,"Grab arm amber markings",new Color(.68f,.28f,.045f),.48f,.4f)};
            visual.GetComponent<MeshRenderer>().sharedMaterials=materials;
            Sample(0);
        }
        static Material Metal(Shader shader,string name,Color color,float metallic,float smooth)
        {
            var m=new Material(shader){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);return m;
        }
        static Vector3 Underside(MeshFilter hull,Vector3 point,Bounds bounds)
        {
            // Once, at cinematic creation. The current aircraft mesh is readable.
            // Ray/triangle intersection avoids attaching to the wing-tip AABB floor.
            if(!hull||!hull.sharedMesh||!hull.sharedMesh.isReadable)return point+Vector3.up*.08f;
            var v=hull.sharedMesh.vertices;var t=hull.sharedMesh.triangles;
            var origin=new Vector3(point.x,bounds.min.y-2,point.z);float nearest=float.MaxValue;
            for(int j=0;j<t.Length;j+=3)
            {
                var a=v[t[j]];var e1=v[t[j+1]]-a;var e2=v[t[j+2]]-a;
                var p=Vector3.Cross(Vector3.up,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-7f)continue;
                var s=origin-a;float u=Vector3.Dot(s,p)/det;if(u<0||u>1)continue;
                var q=Vector3.Cross(s,e1);float w=Vector3.Dot(Vector3.up,q)/det;if(w<0||u+w>1)continue;
                float distance=Vector3.Dot(e2,q)/det;if(distance>=0&&distance<nearest)nearest=distance;
            }
            return nearest<float.MaxValue?origin+Vector3.up*(nearest+.07f):point+Vector3.up*.08f;
        }
        public void Hold(Vector3 centre,Vector3 right,float halfSpan,float closed)
        {
            localCentre=frame.InverseTransformPoint(centre);
            localRight=frame.InverseTransformDirection(right).normalized;span=halfSpan;Sample(closed);
        }
        public void Sample(float closed)
        {
            vertices.Clear();normals.Clear();foreach(var t in triangles)t.Clear();
            var up=Vector3.up;var right=localRight;var forward=Vector3.Cross(right,up).normalized;
            float size=Mathf.Clamp(span*.68f,.30f,.52f),open=1-Mathf.SmoothStep(0,1,closed);
            for(int arm=0;arm<2;arm++)
            {
                float sign=arm==0?-1:1;
                // Keep mount ordering anatomical even when the imported visual
                // is rotated 180 degrees relative to the aircraft flight root.
                var shoulder=mounts[Vector3.Dot(right,Vector3.right)>0?arm:1-arm];
                var grip=localCentre+right*(span*sign);
                var palm=grip+up*(size*.62f);
                var elbow=Vector3.Lerp(shoulder,palm,.48f)+right*(sign*size*1.22f)-forward*size*.25f;
                Box(shoulder+up*.025f,right,up,new Vector3(size*1.65f,.22f,size*1.9f),0);
                // Four recessed fasteners secure each flange to the fuselage.
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                {
                    var bolt=shoulder+right*(x*size*.57f)+forward*(z*size*.68f)-up*.11f;
                    Cylinder(bolt-up*.035f,bolt+up*.025f,.045f,1,6);
                }
                Joint(shoulder-up*.13f,forward,size*.44f,size*.74f);
                Beam(shoulder-up*.13f,elbow,size*.48f,size*.66f,0);
                Joint(elbow,forward,size*.40f,size*.87f);
                Beam(elbow,palm,size*.40f,size*.55f,0);
                // Visible piston barrel and chrome shaft form a continuous brace.
                var pistonTop=Vector3.Lerp(shoulder,elbow,.24f)+right*(sign*size*.30f)-forward*size*.48f;
                var pistonEnd=Vector3.Lerp(elbow,palm,.68f)-forward*size*.48f;
                var sleeve=Vector3.Lerp(pistonTop,pistonEnd,.56f);
                Cylinder(pistonTop,sleeve,size*.16f,0,10);
                Cylinder(sleeve,pistonEnd,size*.075f,1,10);
                Cylinder(sleeve- (pistonEnd-pistonTop).normalized*.04f,sleeve+(pistonEnd-pistonTop).normalized*.04f,size*.19f,1,10);
                Joint(palm,forward,size*.31f,size*.70f);
                Box(palm-up*size*.12f,right,up,new Vector3(size*.95f,size*.40f,size*1.02f),0);
                Box(palm+forward*size*.52f,right,up,new Vector3(size*.65f,size*.18f,.035f),3);
                // Three articulated jaws close around the wrist from distinct sides.
                // Flat padded contacts and pin joints read as machinery, never talons.
                for(int finger=0;finger<3;finger++)
                {
                    float angle=finger*Mathf.PI*2/3+Mathf.PI/6;
                    var radial=right*Mathf.Cos(angle)+forward*Mathf.Sin(angle);
                    var tangent=Vector3.Cross(up,radial).normalized;
                    var a=palm+radial*size*.38f;
                    var b=grip+radial*size*(.69f+open*.68f)-up*size*.10f;
                    var c=grip+radial*size*(.39f+open*1.15f)-up*size*.61f;
                    Beam(a,b,size*.25f,size*.25f,0);
                    Cylinder(a-tangent*size*.18f,a+tangent*size*.18f,size*.16f,1,10);
                    Cylinder(b-tangent*size*.18f,b+tangent*size*.18f,size*.14f,1,10);
                    Beam(b,c,size*.22f,size*.23f,1);
                    Box(c+up*size*.12f-radial*size*.06f,tangent,up,new Vector3(size*.32f,size*.43f,size*.14f),2);
                }
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.subMeshCount=materials.Length;
            for(int i=0;i<triangles.Length;i++)mesh.SetTriangles(triangles[i],i,false);
            mesh.RecalculateBounds();
        }
        void Joint(Vector3 p,Vector3 axis,float radius,float width)
        {
            Cylinder(p-axis*width*.5f,p+axis*width*.5f,radius,0,12);
            Cylinder(p-axis*(width*.5f+.025f),p-axis*(width*.5f-.025f),radius*.71f,1,12);
            Cylinder(p+axis*(width*.5f-.025f),p+axis*(width*.5f+.025f),radius*.71f,1,12);
        }
        void Beam(Vector3 a,Vector3 b,float width,float depth,int material)
        {
            var y=(b-a).normalized;var x=Vector3.Cross(y,Vector3.forward);if(x.sqrMagnitude<.01f)x=Vector3.Cross(y,Vector3.right);x.Normalize();
            Box((a+b)*.5f,x,y,new Vector3(width,Vector3.Distance(a,b)+.04f,depth),material);
        }
        void Box(Vector3 centre,Vector3 x,Vector3 y,Vector3 size,int material)
        {
            var z=Vector3.Cross(x,y).normalized;x=x.normalized*size.x*.5f;y=y.normalized*size.y*.5f;z*=size.z*.5f;
            Quad(centre-x-y-z,centre-x+y-z,centre+x+y-z,centre+x-y-z,material);
            Quad(centre+x-y+z,centre+x+y+z,centre-x+y+z,centre-x-y+z,material);
            Quad(centre-x-y+z,centre-x+y+z,centre-x+y-z,centre-x-y-z,material);
            Quad(centre+x-y-z,centre+x+y-z,centre+x+y+z,centre+x-y+z,material);
            Quad(centre-x+y-z,centre-x+y+z,centre+x+y+z,centre+x+y-z,material);
            Quad(centre-x-y+z,centre-x-y-z,centre+x-y-z,centre+x-y+z,material);
        }
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material)
        {
            int start=vertices.Count;var n=Vector3.Cross(b-a,c-a).normalized;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            normals.Add(n);normals.Add(n);normals.Add(n);normals.Add(n);
            var t=triangles[material];t.Add(start);t.Add(start+1);t.Add(start+2);t.Add(start);t.Add(start+2);t.Add(start+3);
        }
        void Cylinder(Vector3 a,Vector3 b,float radius,int material,int sides)
        {
            var axis=(b-a).normalized;var x=Vector3.Cross(axis,Vector3.forward);if(x.sqrMagnitude<.01f)x=Vector3.Cross(axis,Vector3.right);x.Normalize();var y=Vector3.Cross(axis,x).normalized;
            for(int j=0;j<sides;j++)
            {
                float p=j*Mathf.PI*2/sides,q=(j+1)*Mathf.PI*2/sides;
                var r=radius*(x*Mathf.Cos(p)+y*Mathf.Sin(p));var s=radius*(x*Mathf.Cos(q)+y*Mathf.Sin(q));
                Quad(a+r,a+s,b+s,b+r,material);
                Triangle(a,a+s,a+r,material);Triangle(b,b+r,b+s,material);
            }
        }
        void Triangle(Vector3 a,Vector3 b,Vector3 c,int material)
        {
            int start=vertices.Count;var n=Vector3.Cross(b-a,c-a).normalized;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);normals.Add(n);normals.Add(n);normals.Add(n);
            var t=triangles[material];t.Add(start);t.Add(start+1);t.Add(start+2);
        }
        public void Dispose()
        {
            if(visual)UnityEngine.Object.Destroy(visual);if(mesh)UnityEngine.Object.Destroy(mesh);
            foreach(var m in materials)if(m)UnityEngine.Object.Destroy(m);
        }
    }
}
