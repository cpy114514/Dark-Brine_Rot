using System.Collections.Generic;
using UnityEngine;
namespace Mavis
{
    public sealed partial class CappuccinoPhaseTwoVfx
    {
        sealed class Blade
        {
            public Transform hand,frame;public Vector3 tip;
            public Mesh mesh;public MeshRenderer renderer;public int count;
            public readonly Vector3[] a=new Vector3[32],b=new Vector3[32],v=new Vector3[64];
            public readonly float[] at=new float[32];public readonly Color[] colors=new Color[64];
            public readonly Vector2[] uv=new Vector2[64];public readonly int[] triangles=new int[186];
        }
        readonly List<Blade> blades=new List<Blade>();
        void InitializeBlades()
        {
            var rig=GetComponentInChildren<CappuccinoMotionRig>();if(!rig||rig.bladeGroundPoints==null)return;
            var tips=new Dictionary<Transform,Vector3>();
            foreach(var p in rig.bladeGroundPoints)if(p.bone&&(!tips.TryGetValue(p.bone,out var tip)||p.localPoint.sqrMagnitude>tip.sqrMagnitude))tips[p.bone]=p.localPoint;
            foreach(var p in tips)
            {
                var blade=new Blade{hand=p.Key,tip=p.Value,mesh=new Mesh{name="Capri crema blade ribbon"}};blade.mesh.MarkDynamic();
                blade.frame=MeshObject("Crema blade ribbon / "+p.Key.name,blade.mesh,out blade.renderer);
                for(int i=0;i<31;i++){int n=i*6,k=i*2;blade.triangles[n]=k;blade.triangles[n+1]=k+2;blade.triangles[n+2]=k+1;blade.triangles[n+3]=k+1;blade.triangles[n+4]=k+2;blade.triangles[n+5]=k+3;}
                blades.Add(blade);
            }
        }
        void TickBlades()
        {
            bool active=ai.IsPhaseTwoAttack&&ai.DamageWindowActive;
            foreach(var blade in blades)
            {
                while(blade.count>0&&Time.time-blade.at[0]>.19f)ShiftBlade(blade);
                if(active)
                {
                    Vector3 tip=blade.hand.TransformPoint(blade.tip),heel=Vector3.Lerp(blade.hand.position,tip,.18f);
                    if(blade.count==0||(tip-blade.b[blade.count-1]).sqrMagnitude>.0001f)
                    {
                        if(blade.count==32)ShiftBlade(blade);
                        int n=blade.count++;blade.a[n]=heel;blade.b[n]=tip;blade.at[n]=Time.time;
                    }
                }
                blade.renderer.enabled=blade.count>1;if(blade.count<2)continue;
                for(int i=0;i<blade.count;i++)
                {
                    float life=Mathf.Pow(Mathf.Clamp01(1-(Time.time-blade.at[i])/.19f),1.5f);
                    blade.v[i*2]=blade.frame.InverseTransformPoint(blade.a[i]);blade.v[i*2+1]=blade.frame.InverseTransformPoint(blade.b[i]);
                    blade.colors[i*2]=new Color(1,.5f,.18f,life*.5f);blade.colors[i*2+1]=new Color(2.1f,1.65f,.9f,life*.88f);
                    blade.uv[i*2]=new Vector2(i/(float)(blade.count-1),.05f);blade.uv[i*2+1]=new Vector2(i/(float)(blade.count-1),.95f);
                }
                blade.mesh.Clear(false);blade.mesh.SetVertices(blade.v,0,blade.count*2);blade.mesh.SetColors(blade.colors,0,blade.count*2);blade.mesh.SetUVs(0,blade.uv,0,blade.count*2);
                blade.mesh.SetIndices(blade.triangles,0,(blade.count-1)*6,MeshTopology.Triangles,0);blade.mesh.RecalculateBounds();
                block.Clear();block.SetColor(Tint,Color.white);block.SetFloat("_Mode",3);blade.renderer.SetPropertyBlock(block);
            }
        }
        static void ShiftBlade(Blade blade){for(int i=1;i<blade.count;i++){blade.a[i-1]=blade.a[i];blade.b[i-1]=blade.b[i];blade.at[i-1]=blade.at[i];}blade.count--;}
        void ClearBlades(){foreach(var b in blades){b.count=0;if(b.renderer)b.renderer.enabled=false;}}
        void DisposeBlades(){foreach(var b in blades)if(b.mesh)Destroy(b.mesh);blades.Clear();}
    }
}
