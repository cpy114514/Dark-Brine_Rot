using UnityEngine;

namespace Mavis
{
    // Secondary simulated smoke, layered over the true 3D thermal volume.
    // A shallow cambered mesh avoids a perfectly planar light response.
    public static class BrineSmokeDetail
    {
        public static Mesh CreateMesh()
        {
            const int X=8,Y=8;var v=new Vector3[(X+1)*(Y+1)];var uv=new Vector2[v.Length];var ix=new int[X*Y*6];
            for(int y=0;y<=Y;y++)for(int x=0;x<=X;x++)
            {
                float u=x/(float)X,w=y/(float)Y;int i=y*(X+1)+x;
                v[i]=new Vector3((u-.5f)*3.0f,w*3.0f-.06f,Mathf.Sin(u*Mathf.PI)*Mathf.Sin(w*Mathf.PI)*.18f);
                uv[i]=new Vector2(u,w);
            }
            int k=0;for(int y=0;y<Y;y++)for(int x=0;x<X;x++)
            {
                int a=y*(X+1)+x,b=a+X+1;ix[k++]=a;ix[k++]=a+1;ix[k++]=b;ix[k++]=a+1;ix[k++]=b+1;ix[k++]=b;
            }
            var mesh=new Mesh{name="Cambered secondary simulated smoke"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=ix;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(true);return mesh;
        }
    }
}
