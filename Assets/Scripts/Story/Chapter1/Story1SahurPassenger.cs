using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Keeps the current island character on deck until the story releases control.</summary>
[DefaultExecutionOrder(14000), DisallowMultipleComponent]
public sealed class Story1SahurPassenger : MonoBehaviour
{
    // One brief opening action, including the voyage-to-warning handoff.
    public const float OpeningFidgetSeconds=3f;
    public const float OpeningLeadInSeconds=.3f;
    public Transform ship;
    Vector3 deckPosition;
    Quaternion deckRotation;
    Animator visual;
    Collider[] deckColliders;
    Vector3 leftSole,rightSole;
    bool solesReady,deckCalibrated;
    PlayableGraph idleGraph;
    AnimationClipPlayable idlePose, fidgetPose;
    AnimationMixerPlayable passengerMixer;
    internal const string DeckFidgetClip = "CH1_SahurDeckFidget_v279";
    bool idleInitialized,priorRootMotion;
    AnimationClip sailingIdle,stickFidget;
    Story1SharkCollisionSequence opening;
    float priorAnimatorSpeed;
    public float StickIdleTime { get; private set; }
    void Awake()
    {
        if (ship == null) return;
        deckPosition = ship.InverseTransformPoint(transform.position);
        deckRotation = Quaternion.Inverse(ship.rotation) * transform.rotation;
        deckColliders=ship.GetComponentsInChildren<Collider>();
        var movement = GetComponent<ThirdPersonPlayerController>();
        if (movement != null) movement.enabled = false;
        var combat = GetComponent<Mavis.SahurAttack>();
        if (combat != null) combat.enabled = false;
        GetComponent<CharacterController>().enabled = false;
    }
    void Start()
    {
        visual = transform.Find("Pbr Sahur Visual")?.GetComponent<Animator>();
        if (visual != null)
        {
            visual.Play("Locomotion", 0, 0f);
            visual.SetFloat("Speed", 0f);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void LateUpdate()
    {
        if (ship == null) return;
        transform.SetPositionAndRotation(ship.TransformPoint(deckPosition), ship.rotation * deckRotation);
        if (visual != null)
        {
            // Runtime equipment/controller initialization can rebind the Animator
            // during Start. Take ownership only after those initializers finish.
            if(!idleInitialized)
            {
                opening=FindFirstObjectByType<Story1SharkCollisionSequence>();
                foreach(var candidate in visual.runtimeAnimatorController.animationClips)
                    if(candidate.name=="Rig|Idle_Loop"){sailingIdle=candidate;break;}
                stickFidget = Story1EncounterClipCache.Find(DeckFidgetClip);
                priorRootMotion = visual.applyRootMotion;
                priorAnimatorSpeed = visual.speed;
                visual.applyRootMotion = false;
                visual.speed = 0;
                StickIdleTime = 0;
                if (sailingIdle != null || stickFidget != null)
                {
                    idleGraph = PlayableGraph.Create("Sahur on deck");
                    idleGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    passengerMixer = AnimationMixerPlayable.Create(idleGraph, 2);
                    if (sailingIdle != null)
                    {
                        idlePose = AnimationClipPlayable.Create(idleGraph, sailingIdle);
                        idlePose.SetSpeed(0);
                        passengerMixer.ConnectInput(0, idlePose, 0);
                    }
                    if (stickFidget != null)
                    {
                        fidgetPose = AnimationClipPlayable.Create(idleGraph, stickFidget);
                        fidgetPose.SetSpeed(0);
                        passengerMixer.ConnectInput(1, fidgetPose, 0);
                    }
                    AnimationPlayableOutput.Create(idleGraph, "Passenger", visual).SetSourcePlayable(passengerMixer);
                    idleGraph.Play();
                }
                idleInitialized = true;
            }
            if (idleGraph.IsValid())
            {
                bool fidget = opening == null || opening.VoyageTime >= opening.HelmControlSeconds - OpeningLeadInSeconds;
                if (fidget) StickIdleTime += Time.deltaTime;
                if (idlePose.IsValid()) idlePose.SetTime(Mathf.Repeat(Time.time, sailingIdle.length));
                if (fidgetPose.IsValid()) fidgetPose.SetTime(Mathf.Min(StickIdleTime, stickFidget.length));
                float weight = Mathf.SmoothStep(0, 1, StickIdleTime / .22f);
                if (sailingIdle == null) weight = 1;
                if (stickFidget == null) weight = 0;
                passengerMixer.SetInputWeight(0, 1 - weight);
                passengerMixer.SetInputWeight(1, weight);
                idleGraph.Evaluate(0);
            }
            visual.SetFloat("Speed", 0f);
            if(!solesReady){SahurSupportPose.MeasureSoles(visual,ref leftSole,ref rightSole);solesReady=true;}
            if(AlignToDeck(transform,visual,deckColliders,leftSole,rightSole)&&!deckCalibrated)
            {
                // Retain the fitted support height in ship space. Resetting to
                // the unfitted spawn every frame projects that large vertical
                // correction sideways across a rolling deck.
                deckPosition=ship.InverseTransformPoint(transform.position);
                deckCalibrated=true;
            }
        }
    }
    void OnDisable()
    {
        if(idleGraph.IsValid())idleGraph.Destroy();
        if(idleInitialized&&visual!=null)
        {
            if(visual.speed==0)visual.speed=priorAnimatorSpeed;
            if(!visual.applyRootMotion)visual.applyRootMotion=priorRootMotion;
        }
        idleInitialized=false;
    }

    // Query the native hull mesh near the actual soles, excluding sails and
    // the lower hull. Shared by the passenger and the cinematic takeover.
    public static bool TryDeckSurface(Collider[] colliders,Vector3 sole,out Vector3 point)
    {
        point=sole;float closest=float.PositiveInfinity;bool found=false;
        foreach(var collider in colliders)
        {
            if(collider==null||!collider.enabled||collider.isTrigger||!collider.gameObject.activeInHierarchy)continue;
            if(!collider.Raycast(new Ray(sole+Vector3.up*8,Vector3.down),out var hit,16)||hit.normal.y<.65f)continue;
            float error=Mathf.Abs(hit.point.y-sole.y);
            if(error<closest){closest=error;point=hit.point;found=true;}
        }
        return found;
    }
    public static bool AlignToDeck(Transform actor,Animator animator,Collider[] colliders,Vector3 leftSole,Vector3 rightSole)
    {
        if(animator==null||!animator.isHuman||colliders==null)return false;
        Physics.SyncTransforms();
        Vector3 left=animator.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(leftSole);
        Vector3 right=animator.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint(rightSole);
        bool a=TryDeckSurface(colliders,left,out var l),b=TryDeckSurface(colliders,right,out var r);
        if(!a&&!b)return false;
        float lift=Mathf.Max(a?l.y-left.y:float.NegativeInfinity,b?r.y-right.y:float.NegativeInfinity)+.035f;
        actor.position+=Vector3.up*lift;
        if(a)SahurSupportPose.Anchor(animator,true,leftSole,l+Vector3.up*.035f);
        if(b)SahurSupportPose.Anchor(animator,false,rightSole,r+Vector3.up*.035f);
        return true;
    }
}
