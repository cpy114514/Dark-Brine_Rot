using UnityEngine;

/// <summary>Gentle idle flight for the fairy and her two independent wing hinges.</summary>
[DisallowMultipleComponent]
public sealed class NealFairyHover : MonoBehaviour
{
    [Header("Rig")]
    public Transform visual;
    public Transform leftWing;
    public Transform rightWing;
    [Header("Flight")]
    [Min(0f)] public float hoverHeight = .18f;
    [Min(0f)] public float hoverAmplitude = .055f;
    [Min(.1f)] public float hoverCyclesPerSecond = .65f;
    [Range(0f, 8f)] public float bodySwayDegrees = 1.5f;
    [Header("Wings")]
    [Min(.1f)] public float wingBeatsPerSecond = 1.45f;
    [Range(0f, 30f)] public float openAngle = 5f;
    [Range(0f, 75f)] public float foldedAngle = 40f;
    [Range(0f, 12f)] public float wingLiftDegrees = 4f;
    public float WingPhase { get; private set; }
    public float HoverOffset { get; private set; }
    Vector3 restPosition;
    Quaternion restVisualRotation, restLeft, restRight;
    float elapsed;
    bool bound;

    void OnEnable()
    {
        if(!visual)visual=transform.Find("Neal Fairy Model");
        if(visual && (!leftWing || !rightWing))
        {
            var chest=visual.Find("Hips/Spine/Chest");
            if(chest){if(!leftWing)leftWing=chest.Find("Wing.L");if(!rightWing)rightWing=chest.Find("Wing.R");}
        }
        bound=visual && leftWing && rightWing;
        if(!bound)return;
        restPosition=visual.localPosition;restVisualRotation=visual.localRotation;
        restLeft=leftWing.localRotation;restRight=rightWing.localRotation;
        elapsed=0;
        ApplyPose();
    }
    void Update()
    {
        if(!bound || Time.deltaTime<=0f || PauseSettingsMenu.IsOpen)return;
        elapsed+=Time.deltaTime;
        ApplyPose();
    }
    void ApplyPose()
    {
        // Fold backwards from the open rest pose, so the wings never swing
        // forward through the fairy's shoulders or face.
        WingPhase=Mathf.Repeat(elapsed*wingBeatsPerSecond,1f);
        float wave=.5f-.5f*Mathf.Cos(WingPhase*Mathf.PI*2f);
        float sweep=Mathf.Lerp(openAngle,Mathf.Max(openAngle,foldedAngle),wave);
        float lift=Mathf.Sin(WingPhase*Mathf.PI*2f)*wingLiftDegrees;
        leftWing.localRotation=restLeft*Quaternion.Euler(0,-sweep,-lift);
        rightWing.localRotation=restRight*Quaternion.Euler(0,sweep,lift);
        float floatWave=Mathf.Sin(elapsed*hoverCyclesPerSecond*Mathf.PI*2f);
        HoverOffset=hoverHeight+hoverAmplitude*floatWave;
        visual.localPosition=restPosition+Vector3.up*HoverOffset;
        visual.localRotation=restVisualRotation*Quaternion.Euler(floatWave*bodySwayDegrees,0,0);
    }
    void OnDisable()
    {
        if(!bound)return;
        if(visual){visual.localPosition=restPosition;visual.localRotation=restVisualRotation;}
        if(leftWing)leftWing.localRotation=restLeft;
        if(rightWing)rightWing.localRotation=restRight;
        bound=false;
    }
}
