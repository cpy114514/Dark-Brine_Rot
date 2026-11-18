using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed partial class HuangMeiSealEffects
{
    Transform haloSculpture,boltSculpture;
    Material bronze,dustInk;
    Mesh ringMesh;
    ParticleSystem dust;
    HuangMeiImpactPulse impactPulse;
    readonly List<Material> polishMaterials=new List<Material>();
    void SetupPolish()
    {
        bronze=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Huang Mei engraved ritual bronze"};
        bronze.SetColor("_BaseColor",new Color(.74f,.47f,.17f));bronze.SetFloat("_Metallic",.78f);bronze.SetFloat("_Smoothness",.62f);
        bronze.EnableKeyword("_EMISSION");bronze.SetColor("_EmissionColor",new Color(.68f,.34f,.075f));polishMaterials.Add(bronze);
        ringMesh=MakeTorus();
        haloSculpture=new GameObject("Sculpted eight-petal mandala").transform;haloSculpture.SetParent(root,false);
        var center=Ring(haloSculpture,1);center.name="Cast bronze outer mandala";
        Ring(haloSculpture,.79f).localRotation=Quaternion.Euler(0,0,22.5f);
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI*.25f;
            var petal=Ring(haloSculpture,.30f);petal.localPosition=new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),0)*.56f;
            petal.localScale=new Vector3(.16f,.38f,.75f);petal.localRotation=Quaternion.Euler(0,0,-i*45f);
        }
        boltSculpture=new GameObject("Three dimensional seal projectile").transform;boltSculpture.SetParent(root,false);
        Ring(boltSculpture,1);Ring(boltSculpture,.68f).localRotation=Quaternion.Euler(45,25,0);Ring(boltSculpture,.47f).localRotation=Quaternion.Euler(-35,70,0);
        var seed=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.Destroy(seed.GetComponent<Collider>());seed.name="Condensed seal core";
        seed.transform.SetParent(boltSculpture,false);seed.transform.localScale=new Vector3(.45f,.45f,.6f);seed.GetComponent<Renderer>().sharedMaterial=bronze;
        var pulse=new GameObject("Pooled ground resonance");pulse.transform.SetParent(root,false);impactPulse=pulse.AddComponent<HuangMeiImpactPulse>();
        var echo=Ring(pulse.transform,1);var echo2=Ring(pulse.transform,.76f);
        impactPulse.Initialize(new[]{echo,echo2},new[]{echo.GetComponent<Renderer>(),echo2.GetComponent<Renderer>()});
        var shader=Resources.Load<Shader>("SecondIslandHuangMei/GoldenSeal");dustInk=new Material(shader){name="Soft ritual ash"};
        dustInk.SetTexture("_MainTex",Resources.Load<Texture2D>("SecondIslandHuangMei/Textures/smoke_04"));dustInk.SetFloat("_Style",2);polishMaterials.Add(dustInk);
        var cloud=new GameObject("Low density grounded ash");cloud.transform.SetParent(root,false);cloud.SetActive(false);dust=cloud.AddComponent<ParticleSystem>();
        var main=dust.main;main.loop=false;main.playOnAwake=false;main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=-.025f;
        var emission=dust.emission;emission.enabled=false;var shape=dust.shape;shape.enabled=false;
        var fade=dust.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.38f,.32f,.22f),0),new GradientColorKey(new Color(.15f,.13f,.10f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.24f,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var renderer=cloud.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=dustInk;renderer.renderMode=ParticleSystemRenderMode.Billboard;cloud.SetActive(true);
        if(staff)
        {
            var ribbon=new Material(shader){name="White-gold staff filament"};ribbon.SetColor("_Color",new Color(1.5f,1.2f,.65f));polishMaterials.Add(ribbon);
            staff.sharedMaterial=ribbon;staff.startWidth=.42f;staff.endWidth=.012f;staff.time=.22f;staff.textureMode=LineTextureMode.Stretch;
        }
    }
    Transform Ring(Transform parent,float radius)
    {
        var go=new GameObject("Bronze arc",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*radius;
        go.GetComponent<MeshFilter>().sharedMesh=ringMesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=bronze;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return go.transform;
    }
    static Mesh MakeTorus()
    {
        const int around=64,tube=6;var vertices=new Vector3[around*tube];var normals=new Vector3[vertices.Length];var triangles=new int[around*tube*6];
        for(int i=0;i<around;i++)for(int j=0;j<tube;j++)
        {
            float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/tube;int v=i*tube+j;var outward=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            var n=outward*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b);vertices[v]=outward+n*.014f;normals[v]=n;
            int next=((i+1)%around)*tube+j,edge=i*tube+(j+1)%tube,nextEdge=((i+1)%around)*tube+(j+1)%tube,k=v*6;
            triangles[k]=v;triangles[k+1]=next;triangles[k+2]=edge;triangles[k+3]=edge;triangles[k+4]=next;triangles[k+5]=nextEdge;
        }
        return new Mesh{name="Huang Mei cast bronze arc",vertices=vertices,normals=normals,triangles=triangles};
    }
    void PoseHalo(Vector3 center,Vector3 right,float size,float phase)
    {
        haloSculpture.gameObject.SetActive(true);haloSculpture.position=center;haloSculpture.rotation=Quaternion.LookRotation(Vector3.Cross(right,Vector3.up),Vector3.up)*Quaternion.Euler(0,0,Mathf.Sin(Time.time*.35f)*4);
        haloSculpture.localScale=Vector3.one*size;
    }
    void PoseBolt(Vector3 at,Vector3 direction,float radius)
    {
        boltSculpture.gameObject.SetActive(true);boltSculpture.SetPositionAndRotation(at,Quaternion.LookRotation(direction)*Quaternion.Euler(0,0,Time.time*110));boltSculpture.localScale=Vector3.one*radius;
    }
    void PolishedImpact(Vector3 point,bool large,float inner,float outer)
    {
        impactPulse.Play(point+Vector3.up*.18f,Mathf.Max(large?4:2.3f,outer),inner);
        if(!dust.isPlaying)dust.Play();
        int count=large?14:7;
        for(int i=0;i<count;i++)
        {
            float angle=i*2.39996f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            dust.Emit(new ParticleSystem.EmitParams{position=point+direction*Mathf.Max(.4f,outer*.6f),velocity=direction*(large?2:1)+Vector3.up*.35f,startSize=large?2.1f:1.2f,startLifetime=1.2f,rotation=angle*Mathf.Rad2Deg},1);
        }
    }
    void DisposePolish(){if(ringMesh)Object.Destroy(ringMesh);foreach(var mat in polishMaterials)if(mat)Object.Destroy(mat);}
}
