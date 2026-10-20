using UnityEngine;
using UnityEngine.Rendering;
namespace Mavis
{
    // Three persistent bounded emitters and a 16-sample ribbon; no instantiate/destroy per hit.
    public sealed class CombatImpactVisuals : MonoBehaviour
    {
        ParticleSystem flash, dust, flecks;
        Material material;
        Material particleMaterial;
        Mesh ribbon;
        MeshRenderer ribbonRenderer;
        readonly Vector3[] endsA=new Vector3[16], endsB=new Vector3[16];
        readonly float[] times=new float[16];
        readonly Vector3[] vertices=new Vector3[32];
        readonly Color[] colors=new Color[32];
        readonly Vector3[] normals=new Vector3[32];
        readonly int[] indices=new int[90];
        int count;
        bool wasActive;
        void Awake()
        {
            material=NaturalParticleEffects.Material("Melee contact and tapered club trail",Color.white,false);
            particleMaterial=NaturalParticleEffects.Material("Soft melee contact",Color.white,true);
            flash=NaturalParticleEffects.Emitter(transform,"Melee contact flash (12)",particleMaterial,12,0);
            dust=NaturalParticleEffects.Emitter(transform,"Melee surface response (40)",particleMaterial,40,.25f);
            flecks=NaturalParticleEffects.Emitter(transform,"Melee contact flecks (48)",particleMaterial,48,1);
            var go=new GameObject("Animated club ribbon");go.transform.SetParent(transform,false);
            ribbon=new Mesh{name="Bounded club motion history"};ribbon.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh=ribbon;
            ribbonRenderer=go.AddComponent<MeshRenderer>();ribbonRenderer.sharedMaterial=material;
            ribbonRenderer.shadowCastingMode=ShadowCastingMode.Off;ribbonRenderer.receiveShadows=false;ribbonRenderer.enabled=false;
            for(int i=0;i<15;i++){int k=i*6,j=i*2;indices[k]=j;indices[k+1]=j+2;indices[k+2]=j+1;indices[k+3]=j+1;indices[k+4]=j+2;indices[k+5]=j+3;}
            for(int i=0;i<32;i++)normals[i]=Vector3.up;
        }
        public void Impact(CombatHitEvent hit, CombatFeelProfile.Impact settings, CombatMaterial surface, float brightness)
        {
            int budget=NaturalParticleEffects.BurstBudget(hit.point,Mathf.RoundToInt(6*settings.vfx));
            if(budget<=0)return;
            bool blocked=hit.result==CombatHitResult.Blocked;
            Color tint=surface==CombatMaterial.Metal?new Color(.92f,.77f,.46f):surface==CombatMaterial.Wood?new Color(.6f,.42f,.23f):new Color(.66f,.68f,.63f);
            var p=new ParticleSystem.EmitParams{position=hit.point,startLifetime=.045f,startSize=.23f*settings.vfx,startColor=new Color(1,.94f,.78f,brightness),velocity=Vector3.zero};
            if(brightness>0)flash.Emit(p,1);
            p.startColor=new Color(tint.r,tint.g,tint.b,.45f);p.startLifetime=.18f;p.startSize=.14f*settings.vfx;p.velocity=hit.direction*.3f;
            dust.Emit(p,blocked?2:3);
            for(int i=0;i<budget;i++)
            {
                float angle=i*2.39996f;Vector3 side=Vector3.Cross(hit.direction,Vector3.up).normalized;
                if(side.sqrMagnitude<.01f)side=Vector3.right;
                p.startColor=tint;p.startLifetime=.12f+(i%3)*.035f;p.startSize=surface==CombatMaterial.Metal?.025f:.045f;
                p.velocity=(-hit.direction*.4f+side*Mathf.Cos(angle)+Vector3.up*(.35f+Mathf.Sin(angle)*.6f))*(blocked?2.1f:1.3f)*settings.vfx;
                flecks.Emit(p,1);
            }
        }
        public void Sample(Vector3 a, Vector3 b, bool active, float lifetime)
        {
            if(active && !wasActive)count=0;
            wasActive=active;
            while(count>0 && Time.time-times[0]>lifetime)
            {for(int i=1;i<count;i++){endsA[i-1]=endsA[i];endsB[i-1]=endsB[i];times[i-1]=times[i];}count--;}
            if(active && (count==0 || (endsB[count-1]-b).sqrMagnitude>.0001f))
            {
                if(count>0 && (endsB[count-1]-b).sqrMagnitude>64)count=0;
                if(count==16){for(int i=1;i<16;i++){endsA[i-1]=endsA[i];endsB[i-1]=endsB[i];times[i-1]=times[i];}count--;}
                endsA[count]=Vector3.Lerp(a,b,.35f);endsB[count]=b;times[count++]=Time.time;
            }
            ribbonRenderer.enabled=count>1;
            if(count<2)return;
            for(int i=0;i<count;i++)
            {
                float life=Mathf.Clamp01(1-(Time.time-times[i])/Mathf.Max(.01f,lifetime));
                Vector3 middle=(endsA[i]+endsB[i])*.5f;
                vertices[i*2]=transform.InverseTransformPoint(Vector3.Lerp(middle,endsA[i],life));
                vertices[i*2+1]=transform.InverseTransformPoint(Vector3.Lerp(middle,endsB[i],life));
                colors[i*2]=new Color(.82f,.76f,.64f,life*life*.045f);
                colors[i*2+1]=new Color(.9f,.86f,.76f,life*life*.22f);
            }
            ribbon.Clear();ribbon.SetVertices(vertices,0,count*2);ribbon.SetColors(colors,0,count*2);
            ribbon.SetNormals(normals,0,count*2);
            ribbon.SetIndices(indices,0,(count-1)*6,MeshTopology.Triangles,0,true);
        }
        public void ClearTrail(){count=0;wasActive=false;if(ribbonRenderer)ribbonRenderer.enabled=false;}
        void OnDisable(){ClearTrail();if(flash)flash.Clear();if(dust)dust.Clear();if(flecks)flecks.Clear();}
        void OnDestroy(){if(material)Destroy(material);if(particleMaterial)Destroy(particleMaterial);if(ribbon)Destroy(ribbon);}
    }
}
