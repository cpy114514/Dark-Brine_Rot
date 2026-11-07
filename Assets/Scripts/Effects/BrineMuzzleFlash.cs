using System;
using UnityEngine;

namespace Mavis
{
    // Replaces the old zig-zag muzzle line with one reusable, bore-aligned mesh.
    public sealed class BrineMuzzleFlash : IDisposable
    {
        readonly Mesh mesh;
        readonly MeshRenderer renderer;
        readonly Transform root;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        float age=1,variation;
        public bool Visible=>renderer&&renderer.enabled;
        public BrineMuzzleFlash(Transform parent,Material material)
        {
            var vertices=new Vector3[12];var uv=new Vector2[12];var colors=new Color[12];var triangles=new int[18];
            for(int plane=0;plane<3;plane++)
            {
                Vector3 across=Quaternion.AngleAxis(plane*60,Vector3.forward)*Vector3.right;
                int v=plane*4,t=plane*6;
                vertices[v]=-across*.09f;vertices[v+1]=across*.09f;
                vertices[v+2]=Vector3.forward*.65f-across*.17f;vertices[v+3]=Vector3.forward*.65f+across*.17f;
                uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(0,1);uv[v+2]=new Vector2(1,0);uv[v+3]=new Vector2(1,1);
                for(int i=0;i<4;i++)colors[v+i]=Color.white;
                triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            mesh=new Mesh{name="Three tapered muzzle sheets"};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            renderer=BrineVfxGeometry.Renderer("M4A1 brief muzzle combustion",parent,mesh,material,out var go);root=go.transform;
        }
        public void Show(Vector3 point,Vector3 direction,int shot)
        {age=0;variation=Mathf.Repeat(shot*.618034f,1);Pose(point,direction);Apply();}
        public void Tick(float dt,Vector3 point,Vector3 direction,bool allowed)
        {
            age+=dt;if(!allowed||age>=.14f){Clear();return;}
            Pose(point,direction);Apply();
        }
        void Pose(Vector3 point,Vector3 direction)
        {
            root.SetPositionAndRotation(point,Quaternion.LookRotation(direction)*Quaternion.AngleAxis(variation*60,Vector3.forward));
            root.localScale=Vector3.one*(.85f+variation*.25f);
        }
        void Apply()
        {
            block.SetFloat("_Mode",6);block.SetFloat("_Age",age/.14f);block.SetFloat("_Variation",variation);block.SetFloat("_Fade",1);
            renderer.SetPropertyBlock(block);renderer.enabled=true;
        }
        public void Clear(){age=1;if(renderer)renderer.enabled=false;}
        public void Dispose(){if(mesh)UnityEngine.Object.Destroy(mesh);if(root)UnityEngine.Object.Destroy(root.gameObject);}
    }
}
