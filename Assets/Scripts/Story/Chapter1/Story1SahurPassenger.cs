using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Keeps the current island character on deck until the story releases control.</summary>
[DefaultExecutionOrder(14000), DisallowMultipleComponent]
public sealed class Story1SahurPassenger : MonoBehaviour
{
    public Transform ship;
    Vector3 deckPosition;
    Quaternion deckRotation;
    Animator visual;
    Collider[] deckColliders;
    Vector3 leftSole,rightSole;
    bool solesReady;
    PlayableGraph idleGraph;
    AnimationClipPlayable idlePose;
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
            visual.applyRootMotion=false;
            var clip=Story1EncounterClipCache.Find("CH1_SahurStickIdle_v221");
            if(clip!=null)
            {
                idleGraph=PlayableGraph.Create("Sahur ship stick fidget v221");
                idleGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                idlePose=AnimationClipPlayable.Create(idleGraph,clip);idlePose.SetSpeed(0);
                AnimationPlayableOutput.Create(idleGraph,"Passenger idle",visual).SetSourcePlayable(idlePose);idleGraph.Play();
            }
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
            if(idleGraph.IsValid())
            {
                StickIdleTime+=Time.deltaTime;
                idlePose.SetTime(Mathf.Repeat(StickIdleTime,idlePose.GetAnimationClip().length));idleGraph.Evaluate(0);
            }
            visual.SetFloat("Speed", 0f);
            if(!solesReady){SahurSupportPose.MeasureSoles(visual,ref leftSole,ref rightSole);solesReady=true;}
            AlignToDeck(transform,visual,deckColliders,leftSole,rightSole);
        }
    }
    void OnDisable(){if(idleGraph.IsValid())idleGraph.Destroy();}

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
