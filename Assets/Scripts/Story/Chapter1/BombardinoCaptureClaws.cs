using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    // Two articulated crocodile grasping feet. Geometry belongs only to the
    // abduction presentation; the original boss damage/collision stays authoritative.
    public sealed class BombardinoCaptureClaws : IDisposable
    {
        readonly Transform frame;
        readonly GameObject visual;
        readonly Mesh mesh;
        readonly Material skin, horn;
        readonly List<Vector3> vertices = new List<Vector3>(1600);
        readonly List<int> body = new List<int>(6000), tips = new List<int>(1000);
        Vector3 localCentre, localRight;
        float span;
        public BombardinoCaptureClaws(Transform parent)
        {
            frame=parent;visual=new GameObject("Paired crocodile capture claws",typeof(MeshFilter),typeof(MeshRenderer));
            visual.transform.SetParent(parent,false);mesh=new Mesh{name="Two articulated capture feet"};mesh.MarkDynamic();
            visual.GetComponent<MeshFilter>().sharedMesh=mesh;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            skin=new Material(shader){name="Capture claw hide"};skin.SetColor("_BaseColor",new Color(.29f,.34f,.21f));skin.SetFloat("_Smoothness",.28f);
            horn=new Material(shader){name="Capture claw talons"};horn.SetColor("_BaseColor",new Color(.62f,.57f,.41f));horn.SetFloat("_Smoothness",.4f);
            visual.GetComponent<MeshRenderer>().sharedMaterials=new[]{skin,horn};
        }
        public void Hold(Vector3 centre,Vector3 right,float halfSpan,float closed)
        {
            localCentre=frame.InverseTransformPoint(centre);localRight=frame.InverseTransformDirection(right).normalized;span=halfSpan;
            Sample(closed);
        }
        public void Sample(float closed)
        {
            vertices.Clear();body.Clear();tips.Clear();
            Vector3 centre=frame.TransformPoint(localCentre),right=frame.TransformDirection(localRight).normalized;
            Vector3 up=frame.up,forward=Vector3.Cross(right,up).normalized;
            float size=Mathf.Max(.12f,span*.78f);
            for(int foot=0;foot<2;foot++)
            {
                float side=foot==0?-1:1;
                Vector3 grip=centre+right*(span*side);
                Vector3 ankle=grip+up*(size*2.6f)+right*(size*.4f*side);
                Tube(new[]{ankle+up*size,ankle,grip+up*size*.55f},size*.29f,size*.42f,body);
                for(int digit=0;digit<4;digit++)
                {
                    float angle=digit*Mathf.PI*.5f+Mathf.PI*.25f;
                    Vector3 radial=right*Mathf.Cos(angle)+forward*Mathf.Sin(angle);
                    float open=1-Mathf.SmoothStep(0,1,closed);
                    // Fingers surround the wrist; their inner surfaces retain
                    // a small clearance around the contact point.
                    Vector3 basePoint=grip+up*(size*.65f)+radial*(size*.32f);
                    Vector3 knuckle=grip+up*(size*.12f)+radial*(size*(.70f+open*.65f));
                    Vector3 end=grip-up*(size*.42f)+radial*(size*(.42f+open*1.15f));
                    Tube(new[]{basePoint,Vector3.Lerp(basePoint,knuckle,.6f),knuckle,end},size*.15f,size*.095f,body);
                    Tube(new[]{end,end-up*(size*.18f)-radial*(size*.1f),end-up*(size*.19f)-radial*(size*.3f)},size*.10f,.003f,tips);
                }
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.subMeshCount=2;mesh.SetTriangles(body,0);mesh.SetTriangles(tips,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        void Tube(Vector3[] points,float startRadius,float endRadius,List<int> triangles)
        {
            const int sides=8;int start=vertices.Count;
            for(int p=0;p<points.Length;p++)
            {
                Vector3 direction=(points[Mathf.Min(p+1,points.Length-1)]-points[Mathf.Max(0,p-1)]).normalized;
                Vector3 x=Vector3.Cross(direction,frame.forward);if(x.sqrMagnitude<.01f)x=Vector3.Cross(direction,frame.right);x.Normalize();
                Vector3 y=Vector3.Cross(direction,x).normalized;float radius=Mathf.Lerp(startRadius,endRadius,p/(float)(points.Length-1));
                for(int j=0;j<sides;j++)vertices.Add(frame.InverseTransformPoint(points[p]+radius*(x*Mathf.Cos(j*Mathf.PI*2/sides)+y*Mathf.Sin(j*Mathf.PI*2/sides))));
            }
            for(int p=0;p<points.Length-1;p++)for(int j=0;j<sides;j++)
            {
                int a=start+p*sides+j,b=start+p*sides+(j+1)%sides,c=a+sides,d=b+sides;
                triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(b);triangles.Add(d);triangles.Add(c);
            }
            for(int j=1;j<sides-1;j++){triangles.Add(start);triangles.Add(start+j+1);triangles.Add(start+j);int end=start+(points.Length-1)*sides;triangles.Add(end);triangles.Add(end+j);triangles.Add(end+j+1);}
        }
        public void Dispose(){if(visual)UnityEngine.Object.Destroy(visual);if(mesh)UnityEngine.Object.Destroy(mesh);if(skin)UnityEngine.Object.Destroy(skin);if(horn)UnityEngine.Object.Destroy(horn);}
    }
}
