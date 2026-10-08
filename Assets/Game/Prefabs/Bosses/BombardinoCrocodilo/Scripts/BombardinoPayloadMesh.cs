using UnityEngine;
namespace Mavis
{
    public static class BombardinoPayloadMesh
    {
        static Mesh bomb,missile;
        public static Mesh Get(bool rocket)
        {
            if(rocket && missile!=null)return missile;if(!rocket && bomb!=null)return bomb;
            // A reusable surface of revolution; payload travel is along local Z.
            const int sides=12;var vertices=new Vector3[sides*5];var triangles=new int[sides*4*6];
            float[] z=rocket ? new[]{-.75f,-.62f,.35f,.6f,.85f} : new[]{-.55f,-.4f,0,.4f,.55f};
            float[] radius=rocket ? new[]{.1f,.2f,.2f,.13f,0} : new[]{.03f,.24f,.34f,.24f,.03f};
            for(int ring=0;ring<5;ring++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices[ring*sides+i]=new Vector3(Mathf.Cos(a)*radius[ring],Mathf.Sin(a)*radius[ring],z[ring]);}
            int k=0;for(int ring=0;ring<4;ring++)for(int i=0;i<sides;i++){int a=ring*sides+i,b=ring*sides+(i+1)%sides,c=a+sides,d=b+sides;triangles[k++]=a;triangles[k++]=c;triangles[k++]=b;triangles[k++]=b;triangles[k++]=c;triangles[k++]=d;}
            var mesh=new Mesh{name=rocket ? "Reusable missile body" : "Reusable bomb body",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(rocket)missile=mesh;else bomb=mesh;return mesh;
        }
    }
}
