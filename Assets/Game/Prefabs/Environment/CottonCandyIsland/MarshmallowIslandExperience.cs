using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One contact drives the surface, candy dust, soft audio and camera spring together.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(MarshmallowTraversal))]
public sealed class MarshmallowIslandExperience : MonoBehaviour
{
    public Renderer surface;
    public Material fluffMaterial;
    [Header("Soft surface")]
    [Range(.05f, .4f)] public float standingDepression = .24f;
    [Range(1f, 8f)] public float impressionLifetime = 4.5f;
    [Range(0f, 1f)] public float soundVolume = .38f;
    public bool cameraSpring = true;
    public bool ambientFloss = true;

    public int Landings { get; private set; }
    public int Takeoffs { get; private set; }
    public int PuffParticlesEmitted { get; private set; }
    public int SoundsPlayed { get; private set; }
    public int ActiveImpressions { get; private set; }
    public float Pressure => pressure;
    public float VisualSink => visualSink;
    public float CameraOffset => cameraSpring ? cameraHeight : 0f;
    public float CameraPullback => cameraSpring ? cameraDistance : 0f;
    public float LastImpact { get; private set; }
    public bool Ready => initialized && surface && fluff;

    const int Capacity = 12;
    readonly Vector4[] contacts = new Vector4[Capacity];
    readonly Vector4[] contactData = new Vector4[Capacity];
    readonly RaycastHit[] hits = new RaycastHit[16];
    readonly AudioSource[] voices = new AudioSource[4];
    readonly AudioClip[] clips = new AudioClip[3];
    MaterialPropertyBlock block;
    ThirdPersonPlayerController motor;
    MarshmallowTraversal traversal;
    CharacterController body;
    ParticleSystem fluff, strands, ambient;
    GameObject effects;
    Vector3 support, normal = Vector3.up, lastStep, previousPosition;
    float pressure, pressureVelocity, cameraHeight, cameraVelocity, cameraDistance;
    float visualSink, sinkVelocity;
    float ambientTimer, lastSound = -10, lastStepTime = -10;
    bool initialized, supported, wasGrounded, havePreviousPosition;
    int contactCursor, voiceCursor;
    static readonly int ContactsId = Shader.PropertyToID("_CandyContacts");
    static readonly int DataId = Shader.PropertyToID("_CandyContactData");
    static readonly int CountId = Shader.PropertyToID("_CandyContactCount");
    static readonly int PressureId = Shader.PropertyToID("_CandyPressure");
    static readonly int ClockId = Shader.PropertyToID("_CandyClock");

    void Start() => Initialize();
    void Initialize()
    {
        if (initialized || !Application.isPlaying) return;
        motor = GetComponent<ThirdPersonPlayerController>();
        traversal = GetComponent<MarshmallowTraversal>();
        body = GetComponent<CharacterController>();
        if (!traversal.Active || !surface || !fluffMaterial) return;
        initialized = true;
        block = new MaterialPropertyBlock();
        // All resources are prewarmed once; contacts reuse fixed rings, particles and voices.
        effects = new GameObject("Marshmallow contact feedback");
        effects.transform.SetParent(transform, false);
        fluff = MakeParticles("Soft sugar puffs", 160, false);
        strands = MakeParticles("Pulled candy fibres", 80, true);
        ambient = MakeParticles("Drifting sugar floss", 64, false);
        for (int i = 0; i < voices.Length; i++)
        {
            var go = new GameObject("Soft candy voice " + i);
            go.transform.SetParent(effects.transform, false);
            var voice = go.AddComponent<AudioSource>();
            voice.playOnAwake = false; voice.spatialBlend = .65f;
            voice.minDistance = 8; voice.maxDistance = 35;
            voice.rolloffMode = AudioRolloffMode.Linear; voice.dopplerLevel = 0;
            voices[i] = voice;
        }
        clips[0] = MakeClip("Cotton compression", .27f, 0);
        clips[1] = MakeClip("Cotton lift", .32f, 1);
        clips[2] = MakeClip("Cotton soft landing", .42f, 2);
        support = transform.position + Vector3.up * motor.LowestFootWorldOffset;
        lastStep = support;
    }

    // Invoked by the motor before its visual and camera. There is no second movement owner.
    public void Tick(bool allowed)
    {
        Initialize();
        if (!initialized || !isActiveAndEnabled) return;
        float dt = Time.deltaTime;
        if (dt <= 0 || PauseSettingsMenu.IsOpen) return;
        bool active = traversal.Active && motor.enabled && body.enabled && allowed;
        // Loading/respawning must not create a long streak or a false landing.
        if (havePreviousPosition && (transform.position - previousPosition).sqrMagnitude > 400)
        {
            ClearContacts(); pressure = pressureVelocity = visualSink = sinkVelocity = 0; wasGrounded = false;
            fluff.Clear(); strands.Clear(); ambient.Clear();
        }
        previousPosition = transform.position; havePreviousPosition = true;
        bool grounded = active && body.isGrounded && motor.VerticalSpeed <= 0;
        RaycastHit hit = default;
        supported = active && ProbeSupport(out hit);
        if (supported && grounded) { support = hit.point; normal = hit.normal; }
        float targetPressure = grounded && supported ? standingDepression : 0f;
        pressure = Mathf.SmoothDamp(pressure, targetPressure, ref pressureVelocity, .11f, Mathf.Infinity, dt);
        // Match the rendered depression at the current support, then unstick
        // promptly on takeoff. The collision capsule never sinks into the mesh.
        float targetSink=grounded && supported ? DepressionAtSupport()*.9f : 0f;
        visualSink=Mathf.SmoothDamp(visualSink,targetSink,ref sinkVelocity,.065f,Mathf.Infinity,dt);
        if (wasGrounded && !grounded && active && motor.VerticalSpeed > 1f && supported)
        {
            Takeoffs++;
            EmitFluff(support, normal, .28f, 5);
            EmitStrands(support, .4f);
            PlaySound(1, support, .55f);
        }
        if (grounded && supported && motor.WorldVelocity.sqrMagnitude > 2 &&
            Vector3.Distance(lastStep, support) > 2.1f && Time.time - lastStepTime > .22f)
        {
            Stamp(support, 1.25f, .32f);
            EmitFluff(support, normal, .18f, 3);
            PlaySound(0, support, .5f);
            lastStep = support; lastStepTime = Time.time;
        }
        wasGrounded = grounded;
        if (ambientFloss && active)
        {
            ambientTimer -= dt;
            if (ambientTimer <= 0)
            {
                ambientTimer = .35f;
                var offset = new Vector3(Random.Range(-11f,11f), Random.Range(1.5f,7f), Random.Range(-11f,11f));
                var emit = new ParticleSystem.EmitParams
                {
                    position = transform.position + offset,
                    velocity = new Vector3(.12f,.10f,.04f),
                    startSize = Random.Range(.055f,.16f), startLifetime = Random.Range(4f,7f),
                    startColor = CandyColor(.24f), rotation = Random.Range(0f,360f)
                };
                ambient.Emit(emit, 1);
            }
        }
        // A small damped dip/rebound, never a repeated random camera shake.
        cameraVelocity += (-cameraHeight * 65f - cameraVelocity * 14f) * dt;
        cameraHeight = Mathf.Clamp(cameraHeight + cameraVelocity * dt, -.18f,.12f);
        cameraDistance = Mathf.Lerp(cameraDistance, active && motor.IsSprinting ? .75f : 0f, 1-Mathf.Exp(-3.5f*dt));
        UploadSurface();
    }

    public void Landing(float impactSpeed)
    {
        Initialize();
        if (!initialized || !isActiveAndEnabled || !traversal.Active || !ProbeSupport(out var hit)) return;
        Landings++; LastImpact = Mathf.Clamp01((impactSpeed - 2) / 13f);
        support = hit.point; normal = hit.normal;
        float energy = Mathf.Lerp(.42f,1.25f,LastImpact);
        Stamp(support, Mathf.Lerp(2.3f,3.6f,LastImpact), energy);
        // Two slightly separated sole impressions give the depression a footfall shape.
        Stamp(support + transform.right*.4f, .78f, energy*.32f);
        Stamp(support - transform.right*.4f, .78f, energy*.32f);
        EmitFluff(support, normal, energy, Mathf.RoundToInt(Mathf.Lerp(9,24,LastImpact)));
        if (LastImpact > .35f) EmitStrands(support, energy*.55f);
        PlaySound(2, support, Mathf.Lerp(.6f,1f,LastImpact));
        cameraVelocity = Mathf.Max(-1.4f, cameraVelocity - Mathf.Lerp(.35f,.9f,LastImpact));
        lastStep = support; lastStepTime = Time.time;
    }

    bool ProbeSupport(out RaycastHit closest)
    {
        closest = default;
        if (!body || !surface) return false;
        var b = body.bounds;
        var origin = new Vector3(b.center.x,b.min.y+.8f,b.center.z);
        int count = Physics.RaycastNonAlloc(origin,Vector3.down,hits,2.5f,~0,QueryTriggerInteraction.Ignore);
        float distance = float.PositiveInfinity;
        for (int i=0;i<count;i++)
        {
            var h=hits[i];
            if (h.collider.gameObject != surface.gameObject || h.distance >= distance) continue;
            closest=h; distance=h.distance;
        }
        return distance < float.PositiveInfinity;
    }

    void Stamp(Vector3 point, float radius, float strength)
    {
        int index=contactCursor++ % Capacity;
        contacts[index]=new Vector4(point.x,point.y,point.z,radius);
        contactData[index]=new Vector4(Time.time,strength,impressionLifetime,0);
    }
    float DepressionAtSupport()
    {
        float depth=pressure;
        for(int i=0;i<Capacity;i++)
        {
            float age=Time.time-contactData[i].x,lifetime=contactData[i].z;
            if(lifetime<=0 || age<0 || age>lifetime)continue;
            Vector2 delta=new Vector2(support.x-contacts[i].x,support.z-contacts[i].z);
            float layer=Mathf.Clamp01(1-Mathf.Abs(support.y-contacts[i].y)/4);
            float bowl=Mathf.Pow(2,-delta.sqrMagnitude/(contacts[i].w*contacts[i].w)*3);
            float recovery=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,lifetime,age));
            depth+=.30f*bowl*recovery*contactData[i].y*layer;
        }
        return Mathf.Clamp(depth,0,.48f);
    }
    void UploadSurface()
    {
        if (!surface) return;
        ActiveImpressions=0;
        for(int i=0;i<Capacity;i++)
            if(contactData[i].z>0 && Time.time-contactData[i].x < contactData[i].z) ActiveImpressions++;
        surface.GetPropertyBlock(block);
        block.SetVectorArray(ContactsId,contacts); block.SetVectorArray(DataId,contactData);
        block.SetInt(CountId,ActiveImpressions > 0 ? Mathf.Min(contactCursor,Capacity) : 0);
        block.SetVector(PressureId,new Vector4(support.x,support.y,support.z,pressure));
        block.SetFloat(ClockId,Time.time);
        surface.SetPropertyBlock(block);
    }
    ParticleSystem MakeParticles(string label,int max,bool fibre)
    {
        var go=new GameObject(label);go.transform.SetParent(effects.transform,false);
        var ps=go.AddComponent<ParticleSystem>();
        var main=ps.main; main.playOnAwake=false;main.loop=false;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.maxParticles=max;main.startSpeed=0;main.startLifetime=1.4f;
        main.gravityModifier=fibre ? -.006f : .018f;
        var emission=ps.emission;emission.enabled=false;
        var shape=ps.shape;shape.enabled=false;
        var size=ps.sizeOverLifetime;size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(.25f,1),new Keyframe(1,.15f)));
        var colors=ps.colorOverLifetime;colors.enabled=true;
        var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.85f,.12f),new GradientAlphaKey(.5f,.55f),new GradientAlphaKey(0,1)});
        colors.color=gradient;
        var renderer=go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial=fluffMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;renderer.renderMode=fibre ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale=fibre ? 6f : 1;renderer.velocityScale=fibre ? .3f : 0;
        ps.Play();return ps;
    }
    static Color CandyColor(float alpha)
    {
        var color=Color.Lerp(new Color(1,.88f,.76f),new Color(1,.64f,.80f),Random.value);
        color.a=alpha;return color;
    }
    void EmitFluff(Vector3 point,Vector3 up,float energy,int count)
    {
        for(int i=0;i<count;i++)
        {
            Vector2 ring=Random.insideUnitCircle;
            Vector3 radial=new Vector3(ring.x,0,ring.y);
            var emit=new ParticleSystem.EmitParams
            {
                position=point+radial*1.1f+up*.12f,
                velocity=radial*Mathf.Lerp(.35f,1.4f,energy)+up*Random.Range(.4f,1.5f)*Mathf.Max(.4f,energy),
                startSize=Random.Range(.18f,.50f)*Mathf.Lerp(.65f,1.15f,energy),
                startLifetime=Random.Range(.8f,1.8f),startColor=CandyColor(.7f),rotation=Random.Range(0f,360f)
            };
            fluff.Emit(emit,1);
        }
        PuffParticlesEmitted+=count;
    }
    void EmitStrands(Vector3 point,float energy)
    {
        for(int i=0;i<4;i++)
        {
            var emit=new ParticleSystem.EmitParams { position=point+Random.insideUnitSphere*.55f+Vector3.up*.15f,
                velocity=Vector3.up*Random.Range(.8f,1.8f)*energy,startSize=Random.Range(.04f,.08f),
                startLifetime=.55f,startColor=CandyColor(.45f) };
            strands.Emit(emit,1);
        }
    }
    void PlaySound(int kind,Vector3 point,float intensity)
    {
        if(!GameAudioPolicy.SoundEnabled || Time.time-lastSound < .09f || soundVolume<=0) return;
        lastSound=Time.time;
        var voice=voices[voiceCursor++ % voices.Length];
        voice.transform.position=point;voice.clip=clips[kind];voice.volume=soundVolume*intensity;
        voice.pitch=Random.Range(.94f,1.06f);voice.Play();SoundsPlayed++;
    }
    static AudioClip MakeClip(string label,float duration,int kind)
    {
        const int rate=22050;int length=Mathf.RoundToInt(rate*duration);
        var samples=new float[length];var random=new System.Random(9041+kind);
        float phase=0,filtered=0;
        for(int i=0;i<length;i++)
        {
            float t=i/(float)rate,u=i/(float)(length-1);
            float frequency=kind==1 ? Mathf.Lerp(110,205,u) : Mathf.Lerp(kind==2 ? 145:180,65,u);
            phase+=2*Mathf.PI*frequency/rate;
            filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.08f);
            float envelope=Mathf.Sin(Mathf.PI*u)*Mathf.Exp(-u*(kind==1 ? 3:5));
            samples[i]=(Mathf.Sin(phase)*.38f+Mathf.Sin(phase*.51f)*.18f+filtered*.65f)*envelope*.5f;
        }
        var clip=AudioClip.Create(label,length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    void ClearContacts()
    {
        System.Array.Clear(contacts,0,contacts.Length);System.Array.Clear(contactData,0,contactData.Length);
        ActiveImpressions=0;contactCursor=0;
    }
    public void ResetFeedback()
    {
        if(surface && block!=null)
        {
            surface.GetPropertyBlock(block);block.SetInt(CountId,0);block.SetVector(PressureId,Vector4.zero);
            surface.SetPropertyBlock(block);
        }
        pressure=pressureVelocity=visualSink=sinkVelocity=cameraHeight=cameraVelocity=cameraDistance=0;
        ClearContacts(); wasGrounded=false;
        if(fluff)fluff.Clear();if(strands)strands.Clear();if(ambient)ambient.Clear();
        foreach(var voice in voices)if(voice)voice.Stop();
    }
    void OnDisable() => ResetFeedback();
    void OnDestroy()
    {
        foreach(var clip in clips)if(clip)Destroy(clip);
        if(effects)Destroy(effects);
    }
}
