using Mavis;
using UnityEngine;

// Small, bouncy procedural poses layered over the enemy's physics movement.
[DefaultExecutionOrder(-50)]
public sealed class MarshmallowEnemyVisual : MonoBehaviour
{
    public Transform visual, leftArm, rightArm, leftLeg, rightLeg;
    [Header("Proximity eye glow")]
    [Min(0f)] public float redEyeRadius = 25f;
    [Min(0f)] public float chaseStopRadius = 40f;
    [Min(1f)] public float alertSpeedMultiplier = 1.5f;
    [Min(.01f)] public float eyeTransitionSeconds = .3f;
    public Color alertEyeColor = new Color(1f, .025f, .025f, 1f);
    [Min(0f)] public float eyeGlowIntensity = 2f;
    ChickenBroAI brain;
    Health health;
    Vector3 rest;
    float phase;
    Renderer leftEye, rightEye;
    MaterialPropertyBlock eyeBlock;
    Color calmEyeColor;
    float eyeAlert;
    bool eyesBound;
    static Material glowingEyeMaterial;
    static int eyeUsers;
    static PlayerHealth player;
    static float nextPlayerSearch;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    public float EyeAlertLevel => eyeAlert;
    public bool IsAlert { get; private set; }

    void Awake()
    {
        brain = GetComponent<ChickenBroAI>();
        health = GetComponent<Health>();
        if (visual) rest = visual.localPosition;
        phase = Mathf.Abs(GetInstanceID() % 37) * .17f;
        if (!visual) return;
        var left = visual.Find("Left eye"); var right = visual.Find("Right eye");
        leftEye = left ? left.GetComponent<Renderer>() : null;
        rightEye = right ? right.GetComponent<Renderer>() : null;
        if (!leftEye || !rightEye || !leftEye.sharedMaterial) return;
        var original = leftEye.sharedMaterial;
        calmEyeColor = original.GetColor(BaseColor);
        if (!glowingEyeMaterial)
        {
            glowingEyeMaterial = new Material(original) { name = "Marshmallow eyes - runtime glow" };
            glowingEyeMaterial.EnableKeyword("_EMISSION");
        }
        leftEye.sharedMaterial = glowingEyeMaterial;
        rightEye.sharedMaterial = glowingEyeMaterial;
        eyeBlock = new MaterialPropertyBlock(); eyeUsers++; eyesBound = true;
    }
    void OnDestroy()
    {
        if (!eyesBound) return;
        eyeUsers = Mathf.Max(0, eyeUsers - 1);
        if (eyeUsers == 0 && glowingEyeMaterial) { Destroy(glowingEyeMaterial); glowingEyeMaterial = null; }
    }
    void Update()
    {
        if (!player && Time.time >= nextPlayerSearch)
        {
            player = FindFirstObjectByType<PlayerHealth>();
            nextPlayerSearch = Time.time + 1f;
        }
        if (!player || player.currentHealth <= 0f || !health || health.IsDead)
        {
            IsAlert = false;
            return;
        }
        if (Time.deltaTime <= 0f || PauseSettingsMenu.IsOpen) return;
        float radius = Mathf.Max(0f, IsAlert ? chaseStopRadius : redEyeRadius);
        IsAlert = (player.transform.position - transform.position).sqrMagnitude <= radius * radius;
    }
    void LateUpdate()
    {
        if (!brain || !visual || PauseSettingsMenu.IsOpen) return;
        var mode = brain.Mode;
        bool moving = mode == ChickenBroAI.Mood.Patrol || mode == ChickenBroAI.Mood.Chase;
        float clock = Time.time * (mode == ChickenBroAI.Mood.Chase ? 9f : 6f) + phase;
        float step = moving ? Mathf.Sin(clock) : 0f;
        float bounce = moving ? Mathf.Abs(Mathf.Sin(clock)) * .10f : Mathf.Sin(Time.time * 2.1f + phase) * .025f;
        float squash = moving ? Mathf.Abs(Mathf.Sin(clock)) * .035f : .012f * Mathf.Sin(Time.time * 2.1f + phase);
        float attack = brain.HoldsAttack ? Mathf.Sin(brain.ActionPhase * Mathf.PI) : 0f;
        float hurt = mode == ChickenBroAI.Mood.Flinch || mode == ChickenBroAI.Mood.Launch || mode == ChickenBroAI.Mood.Knockdown ? 1f : 0f;
        visual.localPosition = rest + Vector3.up * bounce;
        visual.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash);
        visual.localRotation = Quaternion.Euler(attack * 16f - hurt * 14f, 0, moving ? step * 5f : Mathf.Sin(Time.time * 1.8f + phase) * 2f);
        if (leftArm) leftArm.localRotation = Quaternion.Euler(step * 27f - attack * 65f, 0, -12f - attack * 20f);
        if (rightArm) rightArm.localRotation = Quaternion.Euler(-step * 27f - attack * 65f, 0, 12f + attack * 20f);
        if (leftLeg) leftLeg.localRotation = Quaternion.Euler(-step * 30f, 0, -4f);
        if (rightLeg) rightLeg.localRotation = Quaternion.Euler(step * 30f, 0, 4f);
        if (mode == ChickenBroAI.Mood.Dead)
        {
            visual.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 80, brain.ActionPhase));
            visual.localScale = Vector3.one * Mathf.Lerp(1f, .84f, brain.ActionPhase);
        }
        UpdateEyes(mode);
    }

    void UpdateEyes(ChickenBroAI.Mood mode)
    {
        if (!eyesBound) return;
        bool close = IsAlert && mode != ChickenBroAI.Mood.Dead;
        float previousAlert = eyeAlert;
        eyeAlert = Mathf.MoveTowards(eyeAlert, close ? 1f : 0f, Time.deltaTime / Mathf.Max(.01f, eyeTransitionSeconds));
        if (Mathf.Approximately(previousAlert, eyeAlert)) return;
        var tint = Color.Lerp(calmEyeColor, alertEyeColor, eyeAlert);
        var emission = alertEyeColor * (eyeAlert * Mathf.Max(0, eyeGlowIntensity));
        SetEye(leftEye, tint, emission); SetEye(rightEye, tint, emission);
    }
    void SetEye(Renderer eye, Color tint, Color emission)
    {
        if (!eye) return;
        // Preserve hit flashes and other existing per-renderer properties.
        eye.GetPropertyBlock(eyeBlock, 0);
        eyeBlock.SetColor(BaseColor, tint); eyeBlock.SetColor(EmissionColor, emission);
        eye.SetPropertyBlock(eyeBlock, 0);
    }
}

