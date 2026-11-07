using System;
using UnityEngine;

namespace Mavis
{
    // Cosmetic flight/collision only. Never adds rigidbodies, forces or damage.
    public sealed class BrineContactFragments : IDisposable
    {
        const int Count=10, VerticesPerPiece=12;
        readonly Vector3[] vertices=new Vector3[Count*VerticesPerPiece], normals=new Vector3[Count*VerticesPerPiece];
        readonly Color[] colors=new Color[Count*VerticesPerPiece];
        readonly int[] indices=new int[Count*VerticesPerPiece];
        readonly Vector3[] position=new Vector3[Count], velocity=new Vector3[Count];
        readonly Quaternion[] rotation=new Quaternion[Count];
        readonly float[] sizes=new float[Count]; readonly bool[] resting=new bool[Count];
        readonly RaycastHit[] hits=new RaycastHit[12];
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        readonly Mesh mesh; readonly MeshRenderer renderer; readonly Transform root;
        float age,duration,power; int count; bool metal,leaf; Color tint;
        public bool Active {get;private set;}
        public int Owner {get;private set;}
        public int CollisionCount {get;private set;}
        public BrineContactFragments(Transform parent,Material material,int index)
        {
            mesh=new Mesh{name="Bounded material fragments "+index};mesh.MarkDynamic();
            renderer=BrineVfxGeometry.Renderer("Pooled ballistic material contact "+index,parent,mesh,material,out var go);root=go.transform;
            for(int i=0;i<indices.Length;i++)indices[i]=i;
        }
        static float Seed(int i,float salt)=>Mathf.Repeat(Mathf.Sin(i*127.1f+salt)*43758.5453f,1);
        public void Begin(int owner,Vector3 point,Vector3 normal,float radius,float strength,CombatMaterial material,bool blast)
        {
            Owner=owner;age=0;power=Mathf.Clamp(strength,.2f,1.5f);metal=material==CombatMaterial.Metal&&!blast;
            leaf=material==CombatMaterial.Vegetation;duration=metal?.48f:leaf?.65f:1.0f;count=Mathf.Max(2,Mathf.CeilToInt((blast?10:metal?8:6)*CombatVfxSettings.DetailScale));CollisionCount=0;
            tint=metal?new Color(.65f,.33f,.09f):CombatSurfaceResponse.Body(material);
            root.SetPositionAndRotation(point,Quaternion.identity);
            var basis=Quaternion.FromToRotation(Vector3.forward,normal.sqrMagnitude>.001f?normal.normalized:Vector3.up);
            for(int i=0;i<count;i++)
            {
                float a=i*2.399963f+Seed(owner,3)*Mathf.PI*2;
                Vector3 outward=basis*new Vector3(Mathf.Cos(a),Mathf.Sin(a),.5f+Seed(i,2));
                position[i]=point+normal*.055f;resting[i]=false;
                velocity[i]=outward.normalized*(metal?3+Seed(i,7)*4:1.0f+Seed(i,7)*2.2f)*Mathf.Sqrt(power)*(blast?1.6f:1);
                rotation[i]=Quaternion.Euler(i*71,i*137,23+i*43);
                sizes[i]=(metal?.014f:.025f+Seed(i,4)*.025f)*Mathf.Clamp(radius,.6f,2)*(material==CombatMaterial.Wood?1.35f:1);
            }
            mesh.Clear();mesh.vertices=vertices;mesh.normals=normals;mesh.colors=colors;mesh.triangles=indices;
            Active=true;Draw();
        }
        public void Tick(float dt)
        {
            age+=dt;if(age>=duration){Clear();return;}
            // Substep for thin walls and frame spikes, with a fixed ceiling.
            int steps=Mathf.Clamp(Mathf.CeilToInt(dt/.025f),1,4);float step=Mathf.Min(dt,.1f)/steps;
            for(int s=0;s<steps;s++)for(int i=0;i<count;i++)
            {
                if(resting[i])continue;
                velocity[i]+=Physics.gravity*(leaf?.3f:1)*step;velocity[i]*=Mathf.Exp(-(metal?1.1f:1.8f)*step);
                Vector3 delta=velocity[i]*step;float length=delta.magnitude;
                int n=length>.00001f?Physics.RaycastNonAlloc(position[i],delta/length,hits,length+sizes[i],Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore):0;
                float nearest=float.MaxValue;RaycastHit best=default;
                for(int h=0;h<n;h++)if(hits[h].distance<nearest){nearest=hits[h].distance;best=hits[h];}
                if(nearest<float.MaxValue)
                {
                    position[i]=best.point+best.normal*sizes[i];
                    velocity[i]=Vector3.Reflect(velocity[i],best.normal)*.24f;
                    if(velocity[i].sqrMagnitude<.4f||metal)resting[i]=true;
                    CollisionCount++;
                }
                else position[i]+=delta;
                rotation[i]*=Quaternion.Euler(step*(leaf?260+i*31:130+i*19),step*90,step*73);
            }
            Draw();
        }
        void Draw()
        {
            for(int i=0;i<Count;i++)
            {
                int cursor=i*VerticesPerPiece;
                if(i>=count){for(int j=0;j<VerticesPerPiece;j++){vertices[cursor+j]=Vector3.zero;colors[cursor+j]=Color.clear;}continue;}
                float cool=1-Mathf.SmoothStep(.02f,duration,age);
                Color color=metal?Color.Lerp(new Color(.12f,.07f,.025f),new Color(1.6f,.72f,.17f),cool*cool):tint*(.72f+Seed(i,9)*.45f);
                Vector3 p=position[i]-root.position;
                Quaternion q=metal?Quaternion.LookRotation(velocity[i].sqrMagnitude>.01f?velocity[i].normalized:Vector3.down):rotation[i];
                float r=sizes[i],length=metal?Mathf.Clamp(velocity[i].magnitude*.012f,.035f,.13f):r*2.5f;
                Vector3 a=p+q*new Vector3(-r,0,0),b=p+q*new Vector3(r,0,0),c=p+q*new Vector3(0,r*(leaf?.08f:.75f),0),d=p-q*Vector3.forward*length;
                Triangle(ref cursor,a,b,c,color);Triangle(ref cursor,a,d,b,color);Triangle(ref cursor,b,d,c,color);Triangle(ref cursor,c,d,a,color);
            }
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.RecalculateBounds();
            block.SetFloat("_Life",Mathf.SmoothStep(.55f,1,age/duration));block.SetFloat("_Incandescence",metal?1:0);
            renderer.SetPropertyBlock(block);renderer.enabled=true;
        }
        void Triangle(ref int k,Vector3 a,Vector3 b,Vector3 c,Color color)
        {
            var n=Vector3.Cross(b-a,c-a).normalized;
            vertices[k]=a;normals[k]=n;colors[k++]=color;
            vertices[k]=b;normals[k]=n;colors[k++]=color;
            vertices[k]=c;normals[k]=n;colors[k++]=color;
        }
        public void Clear(){Active=false;if(renderer)renderer.enabled=false;}
        public void Dispose()=>Dispose(false);
        public void Dispose(bool immediate){BrineVfxGeometry.Release(mesh,immediate);if(root)BrineVfxGeometry.Release(root.gameObject,immediate);}
    }
}
