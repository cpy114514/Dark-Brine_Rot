using System.Collections.Generic;
using UnityEngine;
namespace Mavis
{
    public static class BombardinoPayloadMesh
    {
        static Mesh missile,bomb;
        public static Mesh Get(bool guided)
        {
            if(guided && missile)return missile;if(!guided && bomb)return bomb;
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            float[] z=guided?new[]{-.8f,-.68f,.38f,.62f,.88f}:new[]{-.65f,-.45f,.22f,.5f,.65f};
            float[] radius=guided?new[]{.07f,.14f,.14f,.105f,.005f}:new[]{.10f,.24f,.24f,.16f,.005f};
            const int sides=16;
            for(int row=0;row<z.Length;row++)for(int j=0;j<sides;j++)
            {float a=j*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*radius[row],Mathf.Sin(a)*radius[row],z[row]));uv.Add(new Vector2(j/(float)sides,row/(float)(z.Length-1)));}
            for(int row=0;row<z.Length-1;row++)for(int j=0;j<sides;j++)
            {int a=row*sides+j,b=row*sides+(j+1)%sides,c=a+sides,d=b+sides;triangles.AddRange(new[]{a,b,c,b,d,c});}
            for(int fin=0;fin<4;fin++)
            {
                float a=fin*Mathf.PI/2;Vector3 outward=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                int k=vertices.Count;float rear=guided?-.65f:-.48f;
                vertices.AddRange(new[]{outward*.12f+Vector3.forward*rear,outward*.40f+Vector3.forward*(rear-.12f),outward*.36f+Vector3.forward*(rear+.25f),outward*.13f+Vector3.forward*(rear+.45f)});
                uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3,k+2,k+1,k,k+3,k+2,k});
            }
            var mesh=new Mesh{name=guided?"Finned guided missile":"Finned gravity bomb"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(guided)missile=mesh;else bomb=mesh;return mesh;
        }
    }
}
