using System.Linq;
using Mavis;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Authored, real-time exchanges on the player's current wreck deck, then defeat.</summary>
[DefaultExecutionOrder(900), DisallowMultipleComponent]
public sealed class Story1WreckFinale : MonoBehaviour
{
    public float Elapsed { get; private set; }
    public string CurrentBeat { get; private set; }
    public int HeroCounterCount { get; private set; }
    public int SharkCounterCount { get; private set; }
    public bool HasLostStick { get; private set; }
    public bool IsComplete { get; private set; }
    public float LargestContactGap { get; private set; }
    public float LargestContactCorrection { get; private set; }
    public const float Duration = 14.4f;
    public float LeadInSeconds { get; private set; } = 1.1f;
    Story1WreckBattle battle;
    ThirdPersonPlayerController hero;
    TralaleroSwimAnimator shark;
    Camera shotCamera;
    Story1WreckPlank board;
    OceanWorld ocean;
    Animator animator;
    Transform stick, projectile;
    Renderer[] held;
    bool[] heldEnabled;
    Mesh stickMesh;
    Vector3 startFeet, initialShark, cameraStart, throwStart, lostStart;
    Quaternion cameraRotation, projectileRotation;
    float cameraFov;
    float swimSeconds;
    bool fromWater;
    int beat = -1, fired;
    string activeAnimation;
    Vector3 sharkBeatStart;
    Quaternion sharkBeatRotation;
    Vector3 heroFeet, strikeAim;
    bool pendingCounter, pendingTail, pendingHeavy;
    CapsuleCollider weapon;
    bool manualAnimation;
    Collider[] actorColliders;
    bool[] colliderEnabled;
    PlayableGraph poseGraph;
    AnimationMixerPlayable poseMixer;
    AnimationClipPlayable currentPose, previousPose;
    AnimationClip currentClip, previousClip;
    float poseStart, previousStart, poseSpeed, previousSpeed, blendStart, blendDuration;
    bool startingBeat;
    float beatStart;
    Vector3 deckAnchor, defeatFeet, defeatSide, defeatForward;
    bool pendingThrow, falling;
    float counterStart;
    Vector3 tailHitAnchor;
    bool tailContactSet;
    bool climbing;
    Vector3 climbStart;

    public void Initialize(Story1WreckBattle encounter, ThirdPersonPlayerController player,
        TralaleroSwimAnimator enemy, Camera shotCamera, Story1WreckPlank stage, OceanWorld water, bool swimming)
    {
        battle=encounter;hero=player;shark=enemy;this.shotCamera=shotCamera;board=stage;ocean=water;fromWater=swimming;
        animator=hero.CharacterAnimator;
        actorColliders=hero.GetComponentsInChildren<Collider>(true);
        colliderEnabled=actorColliders.Select(c=>c.enabled).ToArray();
        // Kinematic body hitboxes must not pin the moving deck beneath an authored pose.
        foreach(var collider in actorColliders)collider.enabled=false;
        // Clear charged-attack masks before authored full-body clips take over.
        for(int i=1;i<animator.layerCount;i++)animator.SetLayerWeight(i,0);
        animator.SetFloat("Speed",0);
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        // Both the choreography and the poses use the same clock, including pauses.
        animator.speed=0;manualAnimation=true;
        poseGraph=PlayableGraph.Create("Wreck fight poses");poseGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        poseMixer=AnimationMixerPlayable.Create(poseGraph,2);
        AnimationPlayableOutput.Create(poseGraph,"Sahur",animator).SetSourcePlayable(poseMixer);
        poseGraph.Play();
        weapon=hero.GetComponent<SahurAttack>().stickHitbox as CapsuleCollider;
        startFeet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        initialShark=battle.SharkBody;cameraStart=shotCamera.transform.position;cameraRotation=shotCamera.transform.rotation;cameraFov=shotCamera.fieldOfView;
        stick=hero.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Sahur Stick");
        if(stick!=null)
        {
            held=stick.GetComponentsInChildren<Renderer>(true);heldEnabled=held.Select(r=>r.enabled).ToArray();
            stickMesh=stick.GetComponent<MeshFilter>()?.sharedMesh;
        }
        if(fromWater)
        {
            swimSeconds=Mathf.Max(.2f,Vector3.ProjectOnPlane(startFeet-Deck(-4.3f),Vector3.up).magnitude/(hero.swimSpeed*hero.fastSwimMultiplier));
            LeadInSeconds=Mathf.Max(1.1f,swimSeconds+.95f);
        }
        SetBeat(0);
    }
    Vector3 Forward => Vector3.ProjectOnPlane(board.transform.forward,Vector3.up).normalized;
    Vector3 Side => Vector3.Cross(Vector3.up,Forward);
    Vector3 Deck(float z=0) => board.transform.TransformPoint(new Vector3(0,board.TopLocalY,
        Mathf.Clamp(z,-board.Dimensions.z*.4f,board.Dimensions.z*.4f)/board.Dimensions.z))+Vector3.up*.06f;
    Vector3 DeckAnchor => board.transform.TransformPoint(new Vector3(deckAnchor.x,board.TopLocalY,deckAnchor.z))+Vector3.up*.06f;
    float Water(Vector3 at) => ocean!=null ? ocean.SampleSurfaceHeight(at,Time.time) : hero.seaLevel;
    void Animate(string name, float fade=.08f)
    {
        if(activeAnimation==name)return;activeAnimation=name;
        if(previousPose.IsValid()) {poseMixer.DisconnectInput(0);poseGraph.DestroyPlayable(previousPose);}
        if(currentPose.IsValid())
        {
            poseMixer.DisconnectInput(1);previousPose=currentPose;previousClip=currentClip;
            previousStart=poseStart;previousSpeed=poseSpeed;
            poseMixer.ConnectInput(0,previousPose,0);
        }
        string clipName;
        switch(name)
        {
            case "Finale Guard":clipName="Rig|Sword_Idle";poseSpeed=1;break;
            case "Finale Block":clipName="Armature|Sword_Block";poseSpeed=1.5f;break;
            case "Finale Walk":clipName="Rig|Walk_Loop";poseSpeed=1.3f;break;
            case "Finale Strike":clipName="SahurSwordCombo1";poseSpeed=1.25f;break;
            case "Finale Dodge":clipName="Rig|Roll";poseSpeed=1.85f;break;
            case "Finale Throw":clipName="Armature|OverhandThrow";poseSpeed=1.65f;break;
            case "Finale Jump Strike":clipName="SahurJumpSlash";poseSpeed=1.15f;break;
            case "Finale Chop":clipName="SahurStandingMeleeDownward";poseSpeed=1.8f;break;
            case "Finale Stagger":clipName="Rig|Hit_Chest";poseSpeed=.8f;break;
            case "Finale Knockdown":clipName="Rig|Death01";poseSpeed=2.4f/2.1f;break;
            case "Swim Fast":clipName="SahurFastFreestyle";poseSpeed=1;break;
            case "Wreck Climb":clipName="Armature|ClimbUp_1m";poseSpeed=.7017544f;break;
            case "Jump Start":clipName="Rig|Jump_Start";poseSpeed=2.8f;break;
            default:clipName="Rig|Jump_Land";poseSpeed=2;break;
        }
        currentClip=animator.runtimeAnimatorController.animationClips.First(c=>c.name==clipName);
        currentPose=AnimationClipPlayable.Create(poseGraph,currentClip);currentPose.SetSpeed(0);
        poseMixer.ConnectInput(1,currentPose,0);poseStart=startingBeat ? beatStart : Elapsed;
        blendStart=Elapsed;blendDuration=fade;
    }
    static float ClipTime(AnimationClip clip,float seconds) => clip.isLooping ? Mathf.Repeat(Mathf.Max(0,seconds),clip.length) : Mathf.Clamp(seconds,0,clip.length-.00001f);
    void EvaluateHeroPose(float contactPhase=-1)
    {
        currentPose.SetTime(contactPhase>=0 ? currentClip.length*contactPhase : ClipTime(currentClip,(Elapsed-poseStart)*poseSpeed));
        float weight=previousPose.IsValid() ? Mathf.SmoothStep(0,1,Mathf.Clamp01((Elapsed-blendStart)/Mathf.Max(.01f,blendDuration))) : 1;
        if(previousPose.IsValid())previousPose.SetTime(ClipTime(previousClip,(Elapsed-previousStart)*previousSpeed));
        if(contactPhase>=0)weight=1;
        poseMixer.SetInputWeight(0,1-weight);poseMixer.SetInputWeight(1,weight);
        poseGraph.Evaluate(0);
    }
    void SetBeat(int value)
    {
        beat=value;fired=0;tailContactSet=false;sharkBeatStart=battle.SharkBody;sharkBeatRotation=shark.transform.rotation;
        deckAnchor=board.transform.InverseTransformPoint(hero.transform.position+Vector3.up*hero.LowestFootWorldOffset);
        startingBeat=true;
        beatStart=value==0 ? 0 : LeadInSeconds+(value==1 ? 0 : value==2 ? 1.7f : value==3 ? 3.3f : value==4 ? 5.2f : value==5 ? 7.2f : value==6 ? 8.9f : 10.4f);
        switch(value)
        {
            case 0:CurrentBeat="Regain footing";Animate(fromWater ? "Swim Fast" : Vector3.Distance(startFeet,Deck(-1))>1 ? "Finale Walk" : "Finale Guard");break;
            case 1:CurrentBeat="Stick counter";Animate("Finale Strike");shark.PlayBite();break;
            case 2:CurrentBeat="Dodge tail sweep";Animate("Finale Dodge");shark.PlayTailSlap();SharkCounterCount++;break;
            case 3:CurrentBeat="Boomerang counter";Animate("Finale Throw");break;
            case 4:CurrentBeat="Jumping counter";Animate("Jump Start");break;
            case 5:CurrentBeat="Block and stagger";Animate("Finale Block");shark.PlayTailSlap();SharkCounterCount++;break;
            case 6:CurrentBeat="Last stick counter";Animate("Finale Chop");shark.PlayBite();break;
            case 7:CurrentBeat="Overwhelming tail strike";Animate("Finale Block");shark.PlayTailSlap(powerful:true);SharkCounterCount++;break;
        }
        startingBeat=false;
        if(value==1 || value==4 || value==6)strikeAim=WeaponAim(value==6 ? "Finale Chop" : value==4 ? "Finale Jump Strike" : "Finale Strike",value==6 ? .67f : value==4 ? .55f : .68f);
    }
    void HeroAt(Vector3 feet, bool backward=false)
    {
        heroFeet=feet;
        float turn=1-Mathf.Exp(-16*Time.deltaTime);
        hero.transform.SetPositionAndRotation(feet-Vector3.up*hero.LowestFootWorldOffset,
            Quaternion.Slerp(hero.transform.rotation,Quaternion.LookRotation(backward ? -Forward : Forward),turn));
    }
    void SharkAt(Vector3 at, float lift=0, float bank=0)
    {
        battle.PoseCinematicShark(at,Quaternion.LookRotation(-Forward)*Quaternion.Euler(0,0,bank),lift);
    }
    void TailAt(Vector3 contact, float travel)
    {
        Quaternion turn=Quaternion.LookRotation(Side);
        Vector3 target=battle.TailContactBody(contact,turn);
        Vector3 approach=sharkBeatStart;
        float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(travel/.65f));
        float retreat=Mathf.SmoothStep(0,1,Mathf.Clamp01((travel-.72f)/.8f));
        Vector3 at=Vector3.Lerp(approach,target,t)-Side*(Mathf.Sin(t*Mathf.PI)*2.2f+retreat*4);
        // The tail carries past contact and the shark sinks back into its swim.
        float waterBody=shark.SurfaceRootY(Water(at),turn)+(turn*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale)).y;
        at.y=Mathf.Lerp(at.y,waterBody,retreat);
        battle.PoseCinematicShark(at,Quaternion.Slerp(sharkBeatRotation,turn,t),absoluteHeight:true);
    }
    void Counter(Vector3 at, bool heavy=false)
    {
        pendingCounter=true;pendingHeavy=heavy;
    }
    Vector3 WeaponTip
    {
        get
        {
            if(weapon==null)return GripCentre;
            Vector3 axis=weapon.direction==0 ? Vector3.right : weapon.direction==1 ? Vector3.up : Vector3.forward;
            float extent=Mathf.Max(0,weapon.height*.5f-weapon.radius);
            Vector3 a=weapon.transform.TransformPoint(weapon.center-axis*extent),b=weapon.transform.TransformPoint(weapon.center+axis*extent);
            Vector3 hand=animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            return (a-hand).sqrMagnitude>(b-hand).sqrMagnitude ? a : b;
        }
    }
    Vector3 WeaponAim(string state,float phase)
    {
        // Sample the incoming authored hit once; approach the weapon rather than an arbitrary radius.
        var clip=state=="Finale Jump Strike" ? animator.runtimeAnimatorController.animationClips.First(c=>c.name=="SahurJumpSlash") : currentClip;
        var sample=AnimationClipPlayable.Create(poseGraph,clip);sample.SetSpeed(0);sample.SetTime(clip.length*phase);
        poseMixer.DisconnectInput(1);poseMixer.ConnectInput(1,sample,0);poseMixer.SetInputWeight(0,0);poseMixer.SetInputWeight(1,1);poseGraph.Evaluate(0);
        Vector3 offset=hero.transform.InverseTransformVector(WeaponTip-(hero.transform.position+Vector3.up*hero.LowestFootWorldOffset));
        poseMixer.DisconnectInput(1);poseGraph.DestroyPlayable(sample);poseMixer.ConnectInput(1,currentPose,0);EvaluateHeroPose();
        return offset;
    }
    Vector3 SourceStep(int stage,float phase)
    {
        if(hero.comboSourceX==null || hero.comboSourceZ==null || hero.comboSourceX.Length<=stage || hero.comboSourceZ.Length<=stage)return Vector3.zero;
        var x=hero.comboSourceX[stage];var z=hero.comboSourceZ[stage];
        if(x==null || z==null)return Vector3.zero;
        Vector3 step=new Vector3(x.Evaluate(phase)-x.Evaluate(0),0,z.Evaluate(phase)-z.Evaluate(0));
        return animator.transform.rotation*Vector3.Scale(step,animator.transform.lossyScale)*(animator.humanScale*hero.comboTravelScale);
    }
    void ApproachWeapon(float age,float contact)
    {
        Quaternion facing=Quaternion.LookRotation(-Forward)*Quaternion.Euler(-15,0,0);
        Vector3 aim=pendingCounter ? WeaponTip : heroFeet+hero.transform.TransformVector(strikeAim);
        float arrival=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/contact));
        float recoil=pendingCounter ? 0 : Mathf.SmoothStep(0,1,Mathf.Clamp01((age-contact)/.7f));
        Quaternion rotation=Quaternion.Slerp(sharkBeatRotation,facing,arrival);
        Vector3 localNoseOffset=shark.transform.InverseTransformVector(shark.NoseWorldPoint-battle.SharkBody);
        Vector3 noseOffset=rotation*Vector3.Scale(localNoseOffset,shark.transform.lossyScale);
        Vector3 target=aim-noseOffset+Forward*recoil*4;
        Vector3 body=Vector3.Lerp(sharkBeatStart,target,arrival);
        battle.PoseCinematicShark(body,rotation,absoluteHeight:true);
        if(recoil>.1f)
        {
            Vector3 submerged=battle.SharkBody;
            float water=shark.SurfaceRootY(Water(submerged),rotation)+(rotation*Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale)).y;
            submerged.y=Mathf.Lerp(submerged.y,water,recoil);
            battle.PoseCinematicShark(submerged,rotation,absoluteHeight:true);
        }
    }
    void Update()
    {
        if(battle==null || IsComplete || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        Elapsed+=Time.deltaTime;
        float t=Elapsed-LeadInSeconds+1.1f;
        int next=Elapsed<LeadInSeconds ? 0 : t<2.8f ? 1 : t<4.4f ? 2 : t<6.3f ? 3 : t<8.3f ? 4 : t<10 ? 5 : t<11.5f ? 6 : 7;
        if(next!=beat)SetBeat(next);
        Vector3 deck=Deck(), f=Forward;
        switch(beat)
        {
            case 0:
                if(fromWater)
                {
                    Vector3 edge=Deck(-4.3f);edge.y=Water(edge)-2;
                    if(Elapsed<swimSeconds)
                    {
                        HeroAt(Vector3.Lerp(startFeet,edge,Elapsed/swimSeconds));
                        Vector3 toward=Vector3.ProjectOnPlane(edge-startFeet,Vector3.up);
                        if(toward.sqrMagnitude>.01f)hero.transform.rotation=Quaternion.LookRotation(toward);
                    }
                    else
                    {
                        if(!climbing){climbing=true;climbStart=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;}
                        Animate("Wreck Climb");
                        HeroAt(Vector3.Lerp(climbStart,Deck(-1),Mathf.SmoothStep(0,1,(Elapsed-swimSeconds)/.95f)));
                    }
                }
                else HeroAt(Vector3.Lerp(startFeet,Deck(-1),Mathf.SmoothStep(0,1,Elapsed/LeadInSeconds)));
                SharkAt(Vector3.Lerp(initialShark,deck+f*12,Mathf.SmoothStep(0,1,Elapsed/LeadInSeconds)));
                break;
            case 1:
                float a=t-1.1f;
                HeroAt(DeckAnchor+SourceStep(0,Mathf.Clamp01(a/.853f)));
                if(a>=.58f && fired==0){fired=1;Counter(shark.NoseWorldPoint);}
                if(a>1.0f)Animate("Finale Guard",.16f);
                break;
            case 2:
                float b=t-2.8f;
                HeroAt(Vector3.Lerp(DeckAnchor,Deck(-1.8f),Mathf.SmoothStep(0,1,b/.7f)),b<.85f);
                if(b>=.65f && fired==0){fired=1;pendingTail=true;}
                if(b>.85f)Animate("Finale Guard",.16f);
                break;
            case 3:
                float c=t-4.4f;HeroAt(DeckAnchor);
                SharkAt(deck+f*(c<.82f ? 12 : 12+Mathf.Min(1,(c-.82f)/.6f)*3));
                if(c>=.27f && fired==0){fired=1;pendingThrow=true;}
                if(projectile!=null)
                {
                    Vector3 target=shark.NoseWorldPoint;
                    Vector3 centre=c<.82f ? Vector3.Lerp(throwStart,target,Mathf.Clamp01((c-.27f)/.55f)) :
                        Vector3.Lerp(target,GripCentre,Mathf.Clamp01((c-.82f)/.55f));
                    SpinProjectile(centre,c-.27f);
                }
                if(c>=.82f && fired==1){fired=2;Counter(shark.NoseWorldPoint);}
                if(c>=.92f)Animate("Finale Guard",.15f);
                if(c>=1.37f && fired==2){fired=3;CatchStick();Animate("Finale Guard");}
                break;
            case 4:
                float d=t-6.3f;float jump=Mathf.Clamp01(d/1.35f);
                HeroAt(Vector3.Lerp(DeckAnchor,Deck(2.4f),Mathf.SmoothStep(0,1,jump))+Vector3.up*(4*jump*(1-jump)*2.7f));
                if(d>.2f && d<1.35f)Animate("Finale Jump Strike");
                if(d>=.9f && fired==0){fired=1;Counter(shark.NoseWorldPoint,true);}
                if(d>=1.35f)Animate(d<1.65f ? "Land" : "Finale Guard");
                break;
            case 5:
                float e=t-8.3f;
                HeroAt(DeckAnchor-f*(Mathf.Sin(Mathf.Clamp01((e-.65f)/.9f)*Mathf.PI*.5f)*.65f));
                if(e>=.65f && fired==0){fired=1;pendingTail=true;}
                if(e>1.25f)Animate("Finale Guard",.18f);
                break;
            case 6:
                float g=t-10;
                HeroAt(DeckAnchor);
                if(g>=.87f && fired==0){fired=1;Counter(shark.NoseWorldPoint,true);}
                if(g>1.36f)Animate("Finale Guard",.12f);
                break;
            case 7:
                float h=t-11.5f;
                if(h>=.65f && fired==0)
                {
                    fired=1;pendingTail=true;
                }
                float fall=Mathf.Clamp01((h-.65f)/2.1f);
                float fallAge=Mathf.Max(0,h-.65f);
                Vector3 feet=falling ? defeatFeet-defeatSide*(fallAge*(board.Dimensions.x*.5f+2.2f)/1.5f)-defeatForward*fallAge : DeckAnchor;
                if(falling)feet.y=Mathf.Max(Water(feet)-1.7f,defeatFeet.y+3.4f*fallAge-4.905f*fallAge*fallAge);
                HeroAt(feet);
                if(projectile!=null)
                {
                    Vector3 lost=lostStart+defeatSide*fallAge*6-defeatForward*fallAge*2+Vector3.up*(5.5f*fallAge-4.905f*fallAge*fallAge);
                    SpinProjectile(lost,fallAge);
                }
                if(fall>=.8f && fired==1){fired=2;battle.CinematicImpact(feet,false,true);}
                break;
        }
        if(t>=Duration){IsComplete=true;CurrentBeat="Defeated";animator.speed=1;shark.ReleaseCinematicAnimation();battle.CompleteFinale();}
    }
    Vector3 GripCentre => stick!=null && stickMesh!=null ? stick.TransformPoint(stickMesh.bounds.center) : hero.transform.position+Vector3.up*3;
    void CreateProjectile()
    {
        CatchStick();if(stick==null || stickMesh==null)return;
        var visual=new GameObject("Cinematic Sahur stick",typeof(MeshFilter),typeof(MeshRenderer));
        visual.transform.SetPositionAndRotation(stick.position,stick.rotation);visual.transform.localScale=stick.lossyScale;
        visual.GetComponent<MeshFilter>().sharedMesh=stickMesh;
        visual.GetComponent<MeshRenderer>().sharedMaterials=stick.GetComponent<Renderer>().sharedMaterials;
        projectile=visual.transform;projectileRotation=stick.rotation;
        foreach(var renderer in held)renderer.enabled=false;
    }
    void SpinProjectile(Vector3 centre,float age)
    {
        projectile.rotation=Quaternion.AngleAxis(age*680,Side)*projectileRotation;
        projectile.position=centre-projectile.TransformVector(stickMesh.bounds.center);
    }
    void CatchStick()
    {
        if(projectile!=null)Destroy(projectile.gameObject);projectile=null;
        if(held!=null && !HasLostStick)for(int i=0;i<held.Length;i++)if(held[i]!=null)held[i].enabled=heldEnabled[i];
    }
    void LateUpdate()
    {
        if(battle==null || IsComplete || PauseSettingsMenu.IsOpen)return;
        // Evaluate before testing contact; Update normally sees the previous rendered pose.
        EvaluateHeroPose(pendingCounter && beat!=3 ? beat==6 ? .67f : beat==4 ? .55f : .68f : -1);
        if(beat==0 && fromWater && !climbing)
        {
            Vector3 head=animator.GetBoneTransform(HumanBodyBones.Head).position;
            float correction=Water(head)+hero.swimHeadFreeboard-head.y;
            hero.transform.position+=Vector3.up*correction;heroFeet+=Vector3.up*correction;
        }
        float t=Elapsed-LeadInSeconds+1.1f;
        if(beat==1 || beat==4 || beat==6)shark.SampleCinematicAction(pendingCounter || fired==0 ? "Bite_Lunge" : "Hit_Recoil",
            pendingCounter ? .45f : fired==0 ? t-(beat==1 ? 1.1f : beat==4 ? 6.3f : 10) : Elapsed-counterStart);
        else if(beat==7 && t-11.5f>=1.65f)shark.SampleCinematicAction("Threat",t-13.15f);
        else if(beat==2 || beat==5 || beat==7)shark.SampleCinematicAction(beat==7 ? "Ship_Smash" : "Tail_Strike",pendingTail ? .65f : t-(beat==2 ? 2.8f : beat==5 ? 8.3f : 11.5f));
        else shark.ReleaseCinematicAnimation();
        if(beat==1 || beat==4 || beat==6)
            ApproachWeapon(t-(beat==1 ? 1.1f : beat==4 ? 6.3f : 10),beat==1 ? .58f : beat==4 ? .9f : .87f);
        else if(beat==2 || beat==5 || beat==7)
        {
            Vector3 target=tailContactSet ? board.transform.TransformPoint(tailHitAnchor) :
                beat==2 ? DeckAnchor+Vector3.up*2.4f : animator.GetBoneTransform(HumanBodyBones.Chest).position;
            TailAt(target,pendingTail ? .65f : t-(beat==2 ? 2.8f : beat==5 ? 8.3f : 11.5f));
        }
        if(pendingThrow){CreateProjectile();throwStart=GripCentre;pendingThrow=false;}
        if(pendingCounter)
        {
            Vector3 contact=beat==3 ? shark.NoseWorldPoint : WeaponTip;
            if(beat==3 && projectile!=null)SpinProjectile(contact,t-4.4f);
            else
            {
                LargestContactCorrection=Mathf.Max(LargestContactCorrection,Vector3.Distance(contact,shark.NoseWorldPoint));
                battle.PoseCinematicShark(battle.SharkBody+contact-shark.NoseWorldPoint,shark.transform.rotation,absoluteHeight:true);
            }
            LargestContactGap=Mathf.Max(LargestContactGap,Vector3.Distance(contact,shark.NoseWorldPoint));
            HeroCounterCount++;battle.CinematicImpact(contact,true,pendingHeavy);pendingCounter=false;counterStart=Elapsed;
        }
        if(pendingTail)
        {
            Vector3 contact=beat==2 ? DeckAnchor+Vector3.up*2.4f : animator.GetBoneTransform(HumanBodyBones.Chest).position;
            tailHitAnchor=board.transform.InverseTransformPoint(contact);tailContactSet=true;
            LargestContactCorrection=Mathf.Max(LargestContactCorrection,Vector3.Distance(contact,shark.TailWorldPoint));
            battle.PoseCinematicShark(battle.SharkBody+contact-shark.TailWorldPoint,shark.transform.rotation,absoluteHeight:true);
            LargestContactGap=Mathf.Max(LargestContactGap,Vector3.Distance(contact,shark.TailWorldPoint));
            battle.CinematicImpact(contact,false,beat==7);
            if(beat==5)Animate("Finale Stagger",.06f);
            if(beat==7)
            {
                Animate("Finale Knockdown",.04f);CreateProjectile();lostStart=GripCentre;HasLostStick=true;
                falling=true;defeatFeet=heroFeet;defeatSide=Side;defeatForward=Forward;
            }
            pendingTail=false;
        }
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        Vector3 enemyFocus=(beat==2 || beat==5 || beat==7) ? shark.TailWorldPoint : shark.NoseWorldPoint;
        Vector3 focus=Vector3.Lerp(feet+Vector3.up*2.5f,enemyFocus,.3f);
        Vector3 shot=focus+Side*(beat==7 ? 17 : 14)-Forward*9+Vector3.up*10;
        shot.y=Mathf.Max(shot.y,Water(shot)+7);
        float blend=1-Mathf.Exp(-4*Time.deltaTime);
        float opening=Mathf.SmoothStep(0,1,Elapsed/1.1f);
        shot=Vector3.Lerp(cameraStart,shot,opening);
        shotCamera.transform.position=Vector3.Lerp(shotCamera.transform.position,shot,blend);
        Vector3 clear=shotCamera.transform.position;clear.y=Mathf.Max(clear.y,Water(clear)+5);shotCamera.transform.position=clear;
        Quaternion look=Quaternion.LookRotation(focus-shotCamera.transform.position);
        shotCamera.transform.rotation=Quaternion.Slerp(shotCamera.transform.rotation,Quaternion.Slerp(cameraRotation,look,opening),blend);
        shotCamera.fieldOfView=Mathf.Lerp(cameraFov,beat==7 ? 50 : 44,opening);
    }
    void OnDestroy()
    {
        CatchStick();if(manualAnimation && animator!=null)animator.speed=1;
        if(poseGraph.IsValid())poseGraph.Destroy();
        if(shark!=null)shark.ReleaseCinematicAnimation();
        if(actorColliders!=null)for(int i=0;i<actorColliders.Length;i++)
            if(actorColliders[i]!=null)actorColliders[i].enabled=colliderEnabled[i];
    }
}
