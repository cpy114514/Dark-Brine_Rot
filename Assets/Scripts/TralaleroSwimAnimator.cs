using UnityEngine;

/// <summary>
/// Animates the single-mesh Tralalero model without requiring a skeleton.
/// Its negative local Z end is the tail; the nose and body stay in place.
/// </summary>
[DisallowMultipleComponent]
public sealed class TralaleroSwimAnimator : MonoBehaviour
{
    const float SlapWindupEnd = 0.16f;
    public const float TailContactTime = 0.38f;
    const float SlapRecoveryEnd = 0.75f;

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
    float slapStartTime;
    bool slapping;

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
        restVertices = sourceMesh.vertices;
        restNormals = sourceMesh.normals;
        movedVertices = new Vector3[restVertices.Length];
        movedNormals = new Vector3[restVertices.Length];

        // The imported shark faces +Z. Only the narrow rear third bends.
        tailStart = Mathf.Lerp(sourceMesh.bounds.min.z, sourceMesh.bounds.max.z, 0.34f);
        tailLength = tailStart - sourceMesh.bounds.min.z;
        animatedMesh = Instantiate(sourceMesh);
        animatedMesh.name = sourceMesh.name + " (swimming)";
        animatedMesh.MarkDynamic();
        Bounds bounds = sourceMesh.bounds;
        bounds.Expand(new Vector3((tailSwing + slapSwing * 2f) * 2f, 0f, 0f));
        animatedMesh.bounds = bounds;
        meshFilter.sharedMesh = animatedMesh;
    }

    public void PlayTailSlap(float elapsedSeconds = 0f)
    {
        slapping = true;
        slapStartTime = Time.time - Mathf.Max(0f, elapsedSeconds);
    }

    void LateUpdate()
    {
        if (animatedMesh == null)
            return;

        float slapTime = Time.time - slapStartTime;
        float swimFade = slapping ? 1f - Mathf.Clamp01(slapTime / TailContactTime) : 1f;
        float slap = slapping ? EvaluateSlap(slapTime) : 0f;
        float waveTime = Time.time * tailBeatFrequency;
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
            float offset = wave * weight * sine + slapSwing * weight * slap;
            float slope = wave * (weightSlope * sine - weight * waveTravel * Mathf.Cos(phase))
                        + slapSwing * weightSlope * slap;

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
        if (meshFilter != null && meshFilter.sharedMesh == animatedMesh)
            meshFilter.sharedMesh = sourceMesh;
        if (animatedMesh != null)
            Destroy(animatedMesh);
    }
}
