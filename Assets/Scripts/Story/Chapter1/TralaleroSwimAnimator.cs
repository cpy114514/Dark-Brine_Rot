using UnityEngine;

/// <summary>
/// Plays the anatomical rig and actions authored on the original shark in Blender.
/// A mesh fallback remains for older scenes without the rig resource.
/// </summary>
[DisallowMultipleComponent]
public sealed class TralaleroSwimAnimator : MonoBehaviour
{
    const float SlapWindupEnd = 0.25f;
    public const float TailContactTime = 0.65f;
    const float SlapRecoveryEnd = 1.05f;

    [Header("Swimming")]
    [Min(0f)] public float tailBeatFrequency = 7f;
    [Min(0f)] public float tailSwing = 0.012f;
    [Min(0f)] public float waveTravel = 30f;

    [Header("Tail slap")]
    [Min(0f)] public float slapSwing = 0.038f;

    MeshFilter meshFilter;
    Mesh sourceMesh;
    Mesh animatedMesh;
    Vector3[] restVertices;
    Vector3[] restNormals;
    Vector3[] movedVertices;
    Vector3[] movedNormals;
    float tailStart;
    float tailLength;
    float bodyStart;
    float bodyLength;
    float swimPhase;
    float slapStartTime;
    bool slapping;
    Vector3 noseTip;
    Vector3 tailTip;
    TralaleroAnimationSet animationSet;
    Animator rigAnimator;
    Transform rigInstance, noseMarker, tailMarker;
    Bounds rootMeshBounds;
    Renderer originalRenderer;
    float actionUntil;
    bool powerfulTail;
    public bool UsesBlenderRig => rigAnimator != null;
    public Renderer VisualRenderer { get; private set; }
    public Animator BoneAnimator => rigAnimator;

    public Vector3 NoseWorldPoint => noseMarker != null ? noseMarker.position : meshFilter != null ? meshFilter.transform.TransformPoint(noseTip) : transform.position;
    public Vector3 TailWorldPoint => tailMarker != null ? tailMarker.position : meshFilter != null ? meshFilter.transform.TransformPoint(tailTip) : transform.position;
    public Bounds RootMeshBounds => rootMeshBounds;
    public Vector3 ShipSmashContactLocalPoint => rigAnimator!=null ?
        transform.InverseTransformPoint(meshFilter.transform.TransformPoint(animationSet.shipSmashContact)) : TailContactLocalPoint;

    // The imported root is below the torso: sinking that root by a fixed amount leaves the body exposed.
    public float SurfaceRootY(float waterHeight, Quaternion rotation, float exposedHeightFraction = .15f)
    {
        Vector3 half=Vector3.Scale(rootMeshBounds.extents,transform.lossyScale);
        float height=Mathf.Abs((rotation*new Vector3(half.x,0,0)).y)+
            Mathf.Abs((rotation*new Vector3(0,half.y,0)).y)+Mathf.Abs((rotation*new Vector3(0,0,half.z)).y);
        float centerY=(rotation*Vector3.Scale(rootMeshBounds.center,transform.lossyScale)).y;
        return waterHeight-centerY-height*(1-2*Mathf.Clamp01(exposedHeightFraction));
    }

    public void SynchronizeShipTailContact()
    {
        if(rigAnimator==null)return;
        rigAnimator.Play("Ship_Smash",0,TailContactTime/1.15f);
        rigAnimator.Update(0);
    }

    public Vector3 TailContactLocalPoint
    {
        get
        {
            if (meshFilter == null) return Vector3.back;
            if (rigAnimator != null)
            {
                Vector3 contact=powerfulTail ? animationSet.shipSmashContact : animationSet.tailStrikeContact;
                return transform.InverseTransformPoint(meshFilter.transform.TransformPoint(contact));
            }
            Vector3 point = tailTip + Vector3.right * (slapSwing * EvaluateSlap(TailContactTime));
            return transform.InverseTransformPoint(meshFilter.transform.TransformPoint(point));
        }
    }

    void Awake()
    {
        meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null || !meshFilter.sharedMesh.isReadable)
        {
            Debug.LogError("Tralalero needs a readable mesh for its swim and tail slap.", this);
            enabled = false;
            return;
        }

        sourceMesh = meshFilter.sharedMesh;
        bool firstCorner=true;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            Vector3 point=transform.InverseTransformPoint(meshFilter.transform.TransformPoint(sourceMesh.bounds.center+
                Vector3.Scale(sourceMesh.bounds.extents,new Vector3(x,y,z))));
            if(firstCorner){rootMeshBounds=new Bounds(point,Vector3.zero);firstCorner=false;}else rootMeshBounds.Encapsulate(point);
        }
        originalRenderer=meshFilter.GetComponent<Renderer>();VisualRenderer=originalRenderer;
        animationSet=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations");
        if(animationSet!=null && animationSet.rigPrefab!=null)
        {
            rigInstance=Instantiate(animationSet.rigPrefab,meshFilter.transform,false).transform;
            rigInstance.name="Blender shark anatomy";
            rigAnimator=rigInstance.GetComponentInChildren<Animator>();
            foreach(var skin in rigInstance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.sharedMaterials=originalRenderer.sharedMaterials;skin.updateWhenOffscreen=true;
                VisualRenderer=skin;
            }
            noseMarker=System.Array.Find(rigInstance.GetComponentsInChildren<Transform>(),t=>t.name=="Nose_Marker");
            tailMarker=System.Array.Find(rigInstance.GetComponentsInChildren<Transform>(),t=>t.name=="Tail_Marker");
            originalRenderer.enabled=false;
            rigAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            rigAnimator.Rebind();rigAnimator.Update(0);return;
        }
        restVertices = sourceMesh.vertices;
        restNormals = sourceMesh.normals;
        movedVertices = new Vector3[restVertices.Length];
        movedNormals = new Vector3[restVertices.Length];

        // The imported shark faces +Z. Only the narrow rear third bends.
        tailStart = Mathf.Lerp(sourceMesh.bounds.min.z, sourceMesh.bounds.max.z, 0.34f);
        tailLength = tailStart - sourceMesh.bounds.min.z;
        bodyStart = Mathf.Lerp(sourceMesh.bounds.min.z, sourceMesh.bounds.max.z, .62f);
        bodyLength = Mathf.Max(.0001f, bodyStart - sourceMesh.bounds.min.z);
        float tipBand = sourceMesh.bounds.size.z * .02f;
        int noseCount = 0, tailCount = 0;
        foreach (Vector3 vertex in restVertices)
        {
            if (vertex.z >= sourceMesh.bounds.max.z - tipBand) { noseTip += vertex; noseCount++; }
            if (vertex.z <= sourceMesh.bounds.min.z + tipBand) { tailTip += vertex; tailCount++; }
        }
        noseTip /= Mathf.Max(1, noseCount);
        tailTip /= Mathf.Max(1, tailCount);
        noseTip.z = sourceMesh.bounds.max.z;
        tailTip.z = sourceMesh.bounds.min.z;
        animatedMesh = Instantiate(sourceMesh);
        animatedMesh.name = sourceMesh.name + " (swimming)";
        animatedMesh.MarkDynamic();
        Bounds bounds = sourceMesh.bounds;
        bounds.Expand(new Vector3((tailSwing * 1.25f + slapSwing * 2f) * 2f, 0f, 0f));
        animatedMesh.bounds = bounds;
        meshFilter.sharedMesh = animatedMesh;
    }

    public void PlayTailSlap(float elapsedSeconds = 0f, bool powerful = false)
    {
        powerfulTail=powerful;
        if(rigAnimator!=null){PlayRigAction(powerful ? "Ship_Smash" : "Tail_Strike",1.15f,elapsedSeconds);return;}
        slapping = true;
        slapStartTime = Time.time - Mathf.Max(0f, elapsedSeconds);
    }

    void PlayRigAction(string state,float duration,float offset=0)
    {
        rigAnimator.speed=1;actionUntil=Time.time+Mathf.Max(0,duration-offset);
        rigAnimator.CrossFadeInFixedTime(state,.055f,0,offset);
    }
    public void PlayBite(){if(rigAnimator!=null)PlayRigAction("Bite_Lunge",1.1f);else PlayTailSlap();}
    public void PlayRecoil(){if(rigAnimator!=null)PlayRigAction("Hit_Recoil",.8f);}
    public void PlayThreat(){if(rigAnimator!=null)PlayRigAction("Threat",1.8f);}

    bool cinematicAnimation;
    public void SampleCinematicAction(string state,float seconds)
    {
        if(rigAnimator==null)return;
        float duration=state=="Threat" ? 1.8f : state=="Hit_Recoil" ? .8f : state=="Bite_Lunge" ? 1.1f : 1.15f;
        cinematicAnimation=true;rigAnimator.speed=0;
        if(seconds<=duration+.12f)
        {
            rigAnimator.Play(state,0,Mathf.Clamp(seconds,0,duration-.00001f)/duration);rigAnimator.Update(0);
            // Let the controller's authored recovery blend into swimming at the end.
            if(seconds>duration){rigAnimator.speed=1;rigAnimator.Update(seconds-duration);rigAnimator.speed=0;}
        }
        else{rigAnimator.Play("Swim",0,(seconds-duration)/1.6f);rigAnimator.Update(0);}
    }
    public void ReleaseCinematicAnimation(){cinematicAnimation=false;if(rigAnimator!=null)rigAnimator.speed=1;}

    void LateUpdate()
    {
        if(rigAnimator!=null)
        {
            if(cinematicAnimation)return;
            float effort=Mathf.Clamp01((tailBeatFrequency-7)/5);
            rigAnimator.SetFloat("SwimEffort",effort,.2f,Time.deltaTime);
            rigAnimator.speed=Time.time<actionUntil ? 1 : Mathf.Clamp(tailBeatFrequency/7,.7f,1.6f);
            return;
        }
        if (animatedMesh == null)
            return;

        float slapTime = Time.time - slapStartTime;
        float swimFade = slapping ? 1f - Mathf.Clamp01(slapTime / TailContactTime) : 1f;
        float slap = slapping ? EvaluateSlap(slapTime) : 0f;
        // Integrate frequency: changing swimming speed must not jump the tail's phase.
        swimPhase = Mathf.Repeat(swimPhase + Time.deltaTime * tailBeatFrequency, Mathf.PI * 2f);
        float waveTime = swimPhase;
        float inverseLength = 1f / tailLength;

        for (int i = 0; i < restVertices.Length; i++)
        {
            Vector3 vertex = restVertices[i];
            float u = Mathf.Clamp01((tailStart - vertex.z) * inverseLength);
            float weight = u * u * (3f - 2f * u);
            float weightSlope = u > 0f && u < 1f ? -6f * u * (1f - u) * inverseLength : 0f;
            float phase = waveTime + (tailStart - vertex.z) * waveTravel;
            float sine = Mathf.Sin(phase);
            float wave = tailSwing * swimFade;
            float bodyU = Mathf.Clamp01((bodyStart - vertex.z) / bodyLength);
            float bodyWeight = bodyU * bodyU * (3f - 2f * bodyU);
            float bodySlope = bodyU > 0f && bodyU < 1f ? -6f * bodyU * (1f - bodyU) / bodyLength : 0f;
            float bodyPhase = waveTime + (bodyStart - vertex.z) * waveTravel * .6f;
            float bodyWave = wave * .22f;
            float offset = wave * weight * sine + slapSwing * weight * slap +
                bodyWave * bodyWeight * Mathf.Sin(bodyPhase);
            float slope = wave * (weightSlope * sine - weight * waveTravel * Mathf.Cos(phase))
                        + slapSwing * weightSlope * slap + bodyWave *
                (bodySlope * Mathf.Sin(bodyPhase) - bodyWeight * waveTravel * .6f * Mathf.Cos(bodyPhase));

            vertex.x += offset;
            movedVertices[i] = vertex;

            // Transform the imported normal with the bend so the original
            // shading and the single-mesh material stay intact.
            Vector3 normal = restNormals[i];
            normal.z -= slope * normal.x;
            movedNormals[i] = normal.normalized;
        }

        animatedMesh.vertices = movedVertices;
        animatedMesh.normals = movedNormals;
    }

    static float EvaluateSlap(float time)
    {
        if (time < SlapWindupEnd)
            return Mathf.Lerp(0f, -0.8f, Smooth(time / SlapWindupEnd));
        if (time < TailContactTime)
            return Mathf.Lerp(-0.8f, 1.8f,
                Smooth((time - SlapWindupEnd) / (TailContactTime - SlapWindupEnd)));
        if (time < SlapRecoveryEnd)
            return Mathf.Lerp(1.8f, 0f,
                Smooth((time - TailContactTime) / (SlapRecoveryEnd - TailContactTime)));
        return 0f;
    }

    static float Smooth(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    void OnDestroy()
    {
        if(rigInstance!=null)Destroy(rigInstance.gameObject);
        if(originalRenderer!=null && rigAnimator!=null)originalRenderer.enabled=true;
        if (meshFilter != null && meshFilter.sharedMesh == animatedMesh)
            meshFilter.sharedMesh = sourceMesh;
        if (animatedMesh != null)
        {
            if (Application.isPlaying) Destroy(animatedMesh);
            else DestroyImmediate(animatedMesh);
        }
    }
}
