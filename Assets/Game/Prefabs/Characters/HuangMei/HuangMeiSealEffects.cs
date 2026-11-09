using UnityEngine;

/// <summary>Pooled world-space seals, staff trail and impact sparks for one encounter.</summary>
internal sealed class HuangMeiSealEffects
{
    const int Segments=48;
    const int RadialRows=7;
    static readonly Color Amber=new Color(1.25f,.43f,.055f,1);
    static readonly Color Gold=new Color(2.2f,1.2f,.24f,1);
    static readonly Vector2[] PalmGlyph={
        new Vector2(-.22f,-.67f),new Vector2(-.30f,-.34f),new Vector2(-.58f,-.13f),new Vector2(-.61f,.02f),
        new Vector2(-.50f,.11f),new Vector2(-.30f,.04f),new Vector2(-.36f,.45f),new Vector2(-.27f,.53f),
        new Vector2(-.17f,.47f),new Vector2(-.10f,.23f),new Vector2(-.09f,.66f),new Vector2(.03f,.71f),
        new Vector2(.12f,.64f),new Vector2(.14f,.24f),new Vector2(.22f,.59f),new Vector2(.33f,.62f),
        new Vector2(.39f,.53f),new Vector2(.35f,.20f),new Vector2(.46f,.39f),new Vector2(.56f,.38f),
        new Vector2(.58f,.27f),new Vector2(.48f,-.08f),new Vector2(.36f,-.38f),new Vector2(.33f,-.67f)
    };
    readonly Transform root;
    readonly Collider terrain;
    readonly System.Func<Vector3,Vector3> fallbackGround;
    readonly Material ink;
    readonly MaterialPropertyBlock tint=new MaterialPropertyBlock();
    readonly Vector3[] circle=new Vector3[Segments];
    readonly Vector3[] vertices=new Vector3[Segments*RadialRows];
    readonly LineRenderer outer, inner, halo, haloTrim, bolt, boltRunes, palm;
    readonly Mesh surface;
    readonly MeshRenderer field;
    readonly ParticleSystem sparks;
    readonly TrailRenderer staff;
    readonly AudioSource voice;
    readonly AudioClip gong, thump;
    Vector3 shapeCenter;
    float shapeInner=-1,shapeOuter=-1;

    public HuangMeiSealEffects(Transform owner,Transform tip,System.Func<Vector3,Vector3> fallbackGround)
    {
        root=new GameObject("Golden Seal effects").transform;
        root.SetParent(owner,false);
        this.fallbackGround=fallbackGround;
        var island=Object.FindFirstObjectByType<MarshmallowIslandExperience>();
        if(island && island.gameObject.scene==owner.gameObject.scene && island.surface)terrain=island.surface.GetComponent<Collider>();
        var shader=Resources.Load<Shader>("SecondIslandHuangMei/GoldenSeal");
        ink=new Material(shader ? shader : Shader.Find("Sprites/Default")){name="Huang Mei gold ink"};
        outer=Line("Seal outer edge");inner=Line("Seal inner edge");
        halo=Line("Mandala halo");haloTrim=Line("Mandala rotating facets");
        bolt=Line("Palm seal projectile");boltRunes=Line("Palm seal facets");
        palm=Line("Palm seal emblem");palm.positionCount=PalmGlyph.Length;
        var plane=new GameObject("Seal danger area",typeof(MeshFilter),typeof(MeshRenderer));
        plane.transform.SetParent(root,false);
        surface=new Mesh{name="Huang Mei radial seal field"};surface.MarkDynamic();
        var triangles=new int[Segments*(RadialRows-1)*6];
        for(int i=0;i<Segments;i++)
        {
            float a=i*Mathf.PI*2/Segments;circle[i]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            for(int row=0;row<RadialRows-1;row++)
            {
                int a0=row*Segments+i,a1=row*Segments+(i+1)%Segments,k=a0*6;
                triangles[k]=a0;triangles[k+1]=a1;triangles[k+2]=a0+Segments;
                triangles[k+3]=a1;triangles[k+4]=a1+Segments;triangles[k+5]=a0+Segments;
            }
        }
        surface.vertices=vertices;surface.triangles=triangles;
        var colors=new Color[vertices.Length];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;
        surface.colors=colors;
        plane.GetComponent<MeshFilter>().sharedMesh=surface;
        field=plane.GetComponent<MeshRenderer>();field.sharedMaterial=ink;field.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        sparks=CreateSparks();
        if(tip)
        {
            var trail=new GameObject("Staff edge ribbon");trail.transform.SetParent(tip,false);
            staff=trail.AddComponent<TrailRenderer>();staff.sharedMaterial=ink;staff.time=.19f;staff.minVertexDistance=.12f;
            staff.startWidth=.74f;staff.endWidth=.035f;staff.startColor=new Color(2f,1.11f,.29f,.9f);
            staff.endColor=new Color(1f,.33f,.04f,0);staff.numCapVertices=3;staff.emitting=false;
        }
        var audio=new GameObject("Seal audio emitter");audio.transform.SetParent(root,false);
        voice=audio.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=1;
        voice.rolloffMode=AudioRolloffMode.Linear;voice.minDistance=7;voice.maxDistance=75;voice.dopplerLevel=0;
        gong=Sound("Bronze mandala bell",1.5f,true);thump=Sound("Seal impact",.48f,false);
        Hide();
    }

    LineRenderer Line(string name)
    {
        var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(root,false);
        line.useWorldSpace=true;line.loop=true;line.positionCount=Segments;line.sharedMaterial=ink;
        line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
        return line;
    }

    ParticleSystem CreateSparks()
    {
        var go=new GameObject("Bronze sparks");go.transform.SetParent(root,false);
        go.SetActive(false);
        var particles=go.AddComponent<ParticleSystem>();
        var main=particles.main;main.loop=false;main.playOnAwake=false;main.maxParticles=220;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=.8f;
        var emission=particles.emission;emission.enabled=false;
        var shape=particles.shape;shape.enabled=false;
        var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=ink;
        renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=1.5f;renderer.velocityScale=.12f;
        go.SetActive(true);
        return particles;
    }

    public void Ground(Vector3 center,float inside,float outside,float charge,bool flash)
    {
        Color edge=flash ? Gold : Color.Lerp(new Color(.75f,.22f,.04f,.75f),Amber,charge);
        outer.enabled=true;outer.widthMultiplier=flash?.23f:.11f;SetColor(outer,edge);
        inner.enabled=inside>.08f;
        if(inner.enabled){inner.widthMultiplier=flash?.18f:.10f;SetColor(inner,edge);}
        // Thin expanding waves only need the two outlines. Static hazards reuse
        // their sampled mesh for the entire tell and impact instead of recasting each frame.
        field.enabled=outside-inside>1.1f;
        if(field.enabled)SetColor(field,flash ? new Color(1.68f,.72f,.11f,.26f*(1-charge)) : new Color(.58f,.18f,.035f,.17f+charge*.16f));
        if(inner.enabled==(shapeInner>.08f) && (shapeCenter-center).sqrMagnitude<.04f && Mathf.Abs(shapeInner-inside)<.24f && Mathf.Abs(shapeOuter-outside)<.24f)return;
        shapeCenter=center;shapeInner=inside;shapeOuter=outside;
        for(int i=0;i<Segments;i++)
        {
            var start=inside>.001f?GroundPoint(center+circle[i]*inside):center+Vector3.up*.31f;
            var end=GroundPoint(center+circle[i]*outside);
            outer.SetPosition(i,end);
            if(inner.enabled)inner.SetPosition(i,start);
            if(!field.enabled)continue;
            vertices[i]=root.InverseTransformPoint(start);
            vertices[i+(RadialRows-1)*Segments]=root.InverseTransformPoint(end);
            for(int row=1;row<RadialRows-1;row++)
            {
                float radius=Mathf.Lerp(inside,outside,row/(float)(RadialRows-1));
                vertices[i+row*Segments]=root.InverseTransformPoint(GroundPoint(center+circle[i]*radius));
            }
        }
        if(field.enabled){surface.vertices=vertices;surface.RecalculateBounds();}
    }

    Vector3 GroundPoint(Vector3 at)
    {
        if(terrain && terrain.Raycast(new Ray(at+Vector3.up*18,Vector3.down),out var hit,36))return hit.point+Vector3.up*.31f;
        return fallbackGround(at)+Vector3.up*.31f;
    }

    public void Halo(Vector3 center,Vector3 right,float size,float phase)
    {
        var spin=Quaternion.AngleAxis(Time.time*16,Vector3.Cross(right,Vector3.up));
        Draw(halo,center,right,Vector3.up,size,.11f,Gold);
        Draw(haloTrim,center,spin*right,spin*Vector3.up,size*.86f,.10f,Color.Lerp(Amber,Gold,phase),true);
    }

    public void Bolt(Vector3 at,Vector3 direction,float radius)
    {
        Vector3 right=Vector3.Cross(Vector3.up,direction).normalized;
        Draw(bolt,at,right,Vector3.up,radius,.16f,Gold);
        Draw(boltRunes,at+direction*.06f,right,Vector3.up,radius*.72f,.11f,Amber,true);
        palm.enabled=true;palm.widthMultiplier=Mathf.Max(.05f,radius*.065f);SetColor(palm,Gold);
        for(int i=0;i<PalmGlyph.Length;i++)
            palm.SetPosition(i,at+direction*.09f+(right*PalmGlyph[i].x+Vector3.up*PalmGlyph[i].y)*(radius*.94f));
    }

    void Draw(LineRenderer line,Vector3 at,Vector3 right,Vector3 up,float radius,float width,Color color,bool faceted=false)
    {
        line.enabled=true;line.widthMultiplier=width;SetColor(line,color);
        for(int i=0;i<Segments;i++)
        {
            float size=radius*(faceted?.89f+.11f*Mathf.Cos(i*8f*Mathf.PI*2/Segments):1f);
            line.SetPosition(i,at+(right*circle[i].x+up*circle[i].z)*size);
        }
    }
    void SetColor(Renderer renderer,Color color){tint.SetColor("_Color",color);renderer.SetPropertyBlock(tint);}

    public void Trail(bool on)
    {
        if(staff && staff.emitting!=on){if(on)staff.Clear();staff.emitting=on;}
    }
    public void Impact(Vector3 point,int count,bool large=false,float inner=0,float outer=0)
    {
        if(GameAudioPolicy.SoundEnabled)
        {
            voice.transform.position=point;
            voice.PlayOneShot(large?gong:thump,large?.68f:.64f);
        }
        if(!sparks.isPlaying)sparks.Play();
        for(int i=0;i<count;i++)
        {
            float a=i*2.399963f;
            Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            Vector3 origin=outer>0 && i%4!=0
                ? GroundPoint(point+dir*Random.Range(Mathf.Max(.2f,inner),outer))
                : point+dir*Random.Range(.15f,large?1.7f:.7f)+Vector3.up*.23f;
            var emit=new ParticleSystem.EmitParams{
                position=origin,
                velocity=dir*Random.Range(2f,large?12f:6f)+Vector3.up*Random.Range(2f,7f),
                startColor=Color.Lerp(new Color(1f,.40f,.075f),new Color(1f,.83f,.38f),Random.value),
                startSize=Random.Range(.07f,.17f),startLifetime=Random.Range(.27f,.64f)
            };
            sparks.Emit(emit,1);
        }
    }
    public void RingBell(Vector3 at)
    {if(GameAudioPolicy.SoundEnabled){voice.transform.position=at;voice.PlayOneShot(gong,.48f);}}

    static AudioClip Sound(string name,float length,bool bell)
    {
        const int rate=22050;
        var data=new float[Mathf.RoundToInt(length*rate)];
        for(int i=0;i<data.Length;i++)
        {
            float t=i/(float)rate;
            float primary=Mathf.Sin((bell?116:49)*t*2*Mathf.PI)*Mathf.Exp(-t*(bell?3.1f:16));
            float metal=Mathf.Sin((bell?308:173)*t*2*Mathf.PI)*Mathf.Exp(-t*(bell?5.2f:32));
            float high=Mathf.Sin(783*t*2*Mathf.PI)*Mathf.Exp(-t*20);
            data[i]=(primary*.55f+metal*.26f+high*.11f)*Mathf.Clamp01(t*180)*Mathf.Clamp01((length-t)*15);
        }
        var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
    }

    public void HideGround(){outer.enabled=inner.enabled=field.enabled=false;shapeInner=shapeOuter=-1;}
    public void HideBolt(){bolt.enabled=boltRunes.enabled=palm.enabled=false;}
    public void Hide(){HideGround();HideBolt();halo.enabled=haloTrim.enabled=false;Trail(false);}
    public void Clear(){Hide();if(staff)staff.Clear();sparks.Clear();voice.Stop();}
    public void Dispose(){Object.Destroy(surface);Object.Destroy(ink);Object.Destroy(gong);Object.Destroy(thump);}
}
