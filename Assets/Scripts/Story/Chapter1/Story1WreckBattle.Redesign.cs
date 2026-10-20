using System;
using System.Collections.Generic;
using System.Linq;
using Mavis;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>Approved Oct17 opening: crush, gated tutorial, pursuit, counter, defeat.</summary>
public sealed partial class Story1WreckBattle
{
    public enum OpeningStage { Crush, Tutorial, Pursuit, Duel, Drift, Finished }
    public enum Lesson { Move, Attack, Dodge, ChargeHold, ChargeRelease, Jump, Swim, Complete }
    public bool RedesignActive { get; private set; }
    public OpeningStage RedesignStage { get; private set; }
    public Lesson CurrentLesson { get; private set; }
    public string RedesignBeat { get; private set; }
    public float RedesignClock { get; private set; }
    public bool TutorialWaiting { get; private set; }
    public float TutorialDamage { get; private set; }
    public float TutorialDamageBudget { get; private set; }
    public float TutorialHealthFloor { get; private set; }
    public int RedesignShot { get; private set; } = -1;
    public int RedesignContacts { get; private set; }
    public readonly List<string> RedesignEvents = new List<string>();
    float redesignTime, lessonTime, holdTime, storedTimeScale=1, savedAnimatorSpeed;
    bool ownsPause, lessonExecuting, lessonImpact, redesignManual, stowReady;
    bool redHidden, redReleased;
    int lastShot=-1;
    Vector3 redStart, redEnd, redForward, redRight, redBoardLocal, redThrowStart, redThrowTarget;
    Vector3 redLostVelocity, redFloatStart, redFloatForward, redSwimStart;
    Quaternion redStartRotation, redStickRotation;
    Transform redStick, redProjectile;
    Renderer[] redHeld;
    bool[] redHeldVisible;
    SahurSwimmingWeapon redStow;
    Animator redAnimator;
    PlayableGraph redGraph;
    AnimationClipPlayable redPose;
    AnimationClipPlayable redPreviousPose;
    float redBlendStarted,redBlendDuration;
    AnimationClipPlayable redSplashPose;
    Quaternion redSplashRotation;
    float redSplashPoseTime,redSplashHeadDepth;
    AnimationMixerPlayable redMixer;
    AnimationClip redClip;
    readonly Dictionary<string,AnimationClip> redClips=new Dictionary<string,AnimationClip>();
    Story1WreckPlank redCombatBoard;
    Story1OpeningDirector redDirector;
    float redHeroHeight=5, redScaleBefore=1;
    CapsuleCollider redWeapon;
    Vector3 redCameraPosition, redCameraLook;
    float redCameraFov=50;
    float redPreviousHealthFloor;
    Action redPreviousDefeatHandler;
    bool redOwnershipCaptured,redDisposing,redPriorMovement,redPriorCapsule,redPriorAttack,redPriorBoomerang;
    bool redPriorControlLock,redPriorMovementLock,redPriorRootMotion,redPriorCursorVisible;
    CursorLockMode redPriorCursor;
    float[] redPriorLayerWeights;
    bool redChargeInterrupted;
    Collider[] redShipDeckColliders;
    bool RedInputBlocked=>PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput;
    Vector3 redEdgeLocal,redStanceLocal,redLeftSole,redRightSole;
    bool redHasStance,redDiagnosticMovement;
    float redMoveDistance;
    float redShipIdleStart;
    float redPriorMaximumDelta,redCrushMaximumDelta;
    bool redOwnsMaximumDelta;
    Story1WreckPlank redMovementBoard;
    Vector3 redMovementLocal;
    public float RedesignContactGap { get; private set; } = -1;
    public float RedesignDeckClearance => redCombatBoard==null ? 0 : RedBoardPoint(redCombatBoard).y-RedWater(RedBoardPoint(redCombatBoard));

    // An incomplete imported animation set must not strand a player after control is locked.
    // Keep the existing playable wreck encounter until all of the paired film's assets exist.
    public static string[] MissingRedesignClips()
    {
        return new[] { "CH1_SahurStickIdle_v221", "CH1_SahurGuard_v050", "CH1_SahurSlash_v006", "CH1_SahurOverhead_v215", "CH1_SahurProneDodge_v215",
            "CH1_SahurGetUp_v215", "CH1_SahurStow_v214", "CH1_SahurRetrieve_v214", "CH1_SahurAirHit_v214",
            "PLAYER_Climb_v010", "PLAYER_Fall_v006", "PLAYER_JumpAir_v006", "PLAYER_PursuitSwim_v210", "PLAYER_Throw_v064",
            "CH1_SharkLeapCrush_v214", "CH1_SharkHeadShake_v214", "CH1_SharkFeintTail_v214" }
            .Where(name => Story1EncounterClipCache.Find(name) == null).ToArray();
    }

    void BeginRedesign()
    {
        RedesignActive=true;RedesignStage=OpeningStage.Crush;RedesignBeat="A shadow below";
        // Bound the catch-up work after a heavy fracture frame. All 50Hz
        // physics steps still run for the admitted simulation time; an overloaded
        // cinematic slows instead of chaining seventeen steps into the next frame.
        // Restore the caller's policy before playable tutorial control returns.
        redPriorMaximumDelta=Time.maximumDeltaTime;
        redCrushMaximumDelta=Mathf.Min(redPriorMaximumDelta,Mathf.Max(Time.fixedDeltaTime,.08f));
        Time.maximumDeltaTime=redCrushMaximumDelta;redOwnsMaximumDelta=true;
        redForward=heading.normalized;redRight=side.normalized;
        redStart=player.transform.position;redStartRotation=player.transform.rotation;
        redShipIdleStart=player.GetComponent<Story1SahurPassenger>()?.StickIdleTime??0;
        // Strike the hull ahead of Sahur: the boat's impulse launches him.
        // Centring this original-size animal on his feet would hit his body
        // before the ship breaks, contradicting that beat and clipping him.
        redCrushContact=redStart+redForward*24+Vector3.up*player.LowestFootWorldOffset;
        redShipDeckColliders=sequence.ship.GetComponentsInChildren<Collider>();
        Physics.SyncTransforms();
        if(!Story1SahurPassenger.TryDeckSurface(redShipDeckColliders,redCrushContact,out redCrushContact))
            throw new InvalidOperationException("Body crush requires a native hull surface ahead of Sahur");
        redAnimator=player.CharacterAnimator;savedAnimatorSpeed=redAnimator.speed;
        SahurSupportPose.MeasureSoles(redAnimator,ref redLeftSole,ref redRightSole);
        var body=player.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault();
        if(body!=null)redHeroHeight=Mathf.Clamp(body.bounds.size.y,3,8);
        redStick=player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Sahur Stick");
        if(redStick!=null){redHeld=redStick.GetComponentsInChildren<Renderer>(true);redHeldVisible=redHeld.Select(r=>r.enabled).ToArray();}
        redWeapon=player.GetComponent<SahurAttack>().stickHitbox as CapsuleCollider;
        redStow=player.GetComponent<SahurSwimmingWeapon>();if(redStow!=null)redStow.CinematicStowWeight=0;
        TutorialHealthFloor=Mathf.Max(.001f,playerHealth.currentHealth*.35f);
        TutorialHealthFloor=Mathf.Min(playerHealth.currentHealth,TutorialHealthFloor);
        TutorialDamageBudget=Mathf.Max(0,playerHealth.currentHealth-TutorialHealthFloor);
        playerHealth.NarrativeHealthFloor=TutorialHealthFloor;
        redScaleBefore=Time.timeScale>0?Time.timeScale:1;
        redDirector=gameObject.AddComponent<Story1OpeningDirector>();redDirector.owner=this;
        foreach(var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        RedManual(true);
        RedEvent("Opening started; tutorial entry health="+playerHealth.currentHealth);
    }

    AnimationClip RedClip(string name)
    {
        if(redClips.TryGetValue(name,out var found))return found;
        found=Story1EncounterClipCache.Find(name);
        if(found==null)found=redAnimator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name==name);
        if(found==null)throw new InvalidOperationException("Redesign clip missing: "+name);
        redClips.Add(name,found);return found;
    }
    void CaptureRedesignOwnership()
    {
        if(redOwnershipCaptured||player==null)return;
        redOwnershipCaptured=true;
        redPriorMovement=player.enabled;redPriorCapsule=capsule!=null&&capsule.enabled;
        redPriorControlLock=player.ExternalControlLock;redPriorMovementLock=player.ExternalMovementLock;
        redPriorAttack=player.GetComponent<SahurAttack>()?.enabled??false;
        redPriorBoomerang=player.GetComponent<SahurBoomerang>()?.enabled??false;
        redPriorCursor=Cursor.lockState;redPriorCursorVisible=Cursor.visible;
        if(playerHealth!=null){redPreviousHealthFloor=playerHealth.NarrativeHealthFloor;redPreviousDefeatHandler=playerHealth.NarrativeDefeatHandler;}
        var animator=player.CharacterAnimator;
        if(animator!=null)
        {
            redPriorRootMotion=animator.applyRootMotion;
            redPriorLayerWeights=new float[animator.layerCount];
            for(int i=0;i<animator.layerCount;i++)redPriorLayerWeights[i]=animator.GetLayerWeight(i);
        }
    }
    void RestoreRedesignAnimator()
    {
        if(redAnimator==null)return;
        redAnimator.speed=savedAnimatorSpeed;redAnimator.applyRootMotion=redPriorRootMotion;
        if(redPriorLayerWeights!=null)
            for(int i=0;i<Mathf.Min(redAnimator.layerCount,redPriorLayerWeights.Length);i++)redAnimator.SetLayerWeight(i,redPriorLayerWeights[i]);
    }
    void RedManual(bool value)
    {
        if(value==redesignManual)return;
        redesignManual=value;
        if(value)
        {
            // A scripted takeover must not leave an equipment panel without its enabled controller.
            player.GetComponent<SahurLoadoutUI>()?.SetOpen(false);
            player.GetComponent<IslandMapUI>()?.SetOpen(false);
            player.GetComponent<SahurAttack>().SuspendForSwimming();
            player.GetComponent<SahurAttack>().enabled=false;player.GetComponent<SahurBoomerang>().enabled=false;
            player.ExternalControlLock=true;player.enabled=false;capsule.enabled=false;
            if(rider!=null)rider.enabled=false;
            player.Motion.Claim(this,SahurControlState.Cinematic);
            redAnimator.speed=0;redAnimator.applyRootMotion=false;
            for(int i=1;i<redAnimator.layerCount;i++)redAnimator.SetLayerWeight(i,0);
            redGraph=PlayableGraph.Create("Opening redesign v214");redGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            redPose=AnimationClipPlayable.Create(redGraph,RedClip("CH1_SahurGuard_v050"));redPose.SetSpeed(0);
            redMixer=AnimationMixerPlayable.Create(redGraph,3);redMixer.ConnectInput(0,redPose,0);redMixer.SetInputWeight(0,1);
            AnimationPlayableOutput.Create(redGraph,"Sahur",redAnimator).SetSourcePlayable(redMixer);redGraph.Play();
            redClip=null;
        }
        else
        {
            if(redStow!=null)redStow.CinematicStowWeight=-1;
            if(redGraph.IsValid())redGraph.Destroy();redClip=null;RestoreRedesignAnimator();
            capsule.enabled=true;player.ExternalControlLock=player.ExternalMovementLock=false;player.enabled=true;
            player.Motion.Release(this);player.RestoreSavedPose(player.transform.position,player.transform.rotation);
            player.ContinueCameraFromCurrentView(sequence.storyCamera);
            if(rider!=null)rider.enabled=true;
            // Tutorial attacks are deliberately gated; locomotion remains the real controller.
            player.GetComponent<SahurAttack>().enabled=false;player.GetComponent<SahurBoomerang>().enabled=false;
            // Disabling the real controller releases its cursor; enabling it has no OnEnable hook.
            if(!RedInputBlocked){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }
    }
    void RedClearPoseBlend()
    {
        if(redPreviousPose.IsValid())
        {
            redMixer.DisconnectInput(2);redGraph.DestroyPlayable(redPreviousPose);
        }
        redMixer.SetInputWeight(2,0);
        redBlendDuration=0;
    }
    void RedPose(string name,float seconds,bool blend=true)
    {
        if(!redesignManual)RedManual(true);
        var clip=RedClip(name);
        if(redClip!=clip)
        {
            RedClearPoseBlend();
            // Only soften supported stance changes. Impacts, traversal and the
            // separately authored splash blend retain their exact contact poses.
            float duration=!blend||redClip==null?0:
                name=="CH1_SahurGuard_v050"?(redClip.name=="CH1_SahurStickIdle_v221"?.45f:.16f):
                name=="CH1_SahurRetrieve_v214"?.18f:
                name=="CH1_SahurOverhead_v215"?.12f:
                name=="CH1_SahurGetUp_v215"?.10f:
                name=="PLAYER_Throw_v064"?.12f:0;
            var old=redPose;redMixer.DisconnectInput(0);
            redPose=AnimationClipPlayable.Create(redGraph,clip);redPose.SetSpeed(0);
            redMixer.ConnectInput(0,redPose,0);redMixer.SetInputWeight(0,1);
            if(old.IsValid()&&duration>0)
            {
                redPreviousPose=old;redMixer.ConnectInput(2,old,0);
                redBlendStarted=Time.time;redBlendDuration=duration;
            }
            else if(old.IsValid())redGraph.DestroyPlayable(old);
            redClip=clip;
        }
        redPose.SetTime(clip.isLooping?Mathf.Repeat(Mathf.Max(0,seconds),clip.length):Mathf.Clamp(seconds,0,clip.length-.00001f));
        if(redBlendDuration>0)
        {
            float weight=Mathf.SmoothStep(0,1,(Time.time-redBlendStarted)/redBlendDuration);
            redMixer.SetInputWeight(0,weight);redMixer.SetInputWeight(2,1-weight);
            if(weight>=1)RedClearPoseBlend();
        }
        redGraph.Evaluate(0);
    }
    Vector3 RedChest=>redAnimator.GetBoneTransform(HumanBodyBones.Chest).position;
    Vector3 RedHead=>redAnimator.GetBoneTransform(HumanBodyBones.Head).position;
    float RedWater(Vector3 p)=>ocean!=null?ocean.SampleSurfaceHeight(p,Time.time):center.y;
    static float R01(float t,float lo,float hi)=>Mathf.Clamp01((t-lo)/Mathf.Max(.0001f,hi-lo));
    static float REase(float t,float lo,float hi)=>Mathf.SmoothStep(0,1,R01(t,lo,hi));
    void RedEvent(string label){RedesignEvents.Add(RedesignStage+" "+redesignTime.ToString("F3")+" "+label);}
    void RedFeet(Vector3 at,Quaternion rotation)
    {
        player.transform.SetPositionAndRotation(at,rotation);redGraph.Evaluate(0);
        player.transform.position+=Vector3.up*(at.y-player.transform.position.y-player.LowestFootWorldOffset);
    }
    Vector3 RedBoardPoint(Story1WreckPlank plank)
    {
        if(plank==null)return redEnd;
        if(redHasStance && plank==redCombatBoard)return plank.BoardingPoint(plank.transform.TransformPoint(redStanceLocal))+Vector3.up*.08f;
        return plank.BoardingPoint(plank.transform.position)+Vector3.up*.08f;
    }
    bool RedUsableDeck(Story1WreckPlank p)=>p.IsBoardable && p.FractureSource!=null && p.Dimensions.y<1.2f && Mathf.Min(p.Dimensions.x,p.Dimensions.z)>7 && Mathf.Abs(p.transform.up.y)>.88f;
    bool RedFindEntry(Story1WreckPlank p,Vector3 swimmer,out Vector3 edge,out Vector3 stance,out Vector3 outward)
    {
        edge=p.BoardingPoint(swimmer);outward=p.OpenWaterDirection(edge);stance=edge;
        if(!RedUsableDeck(p)||p.Speed>5 || Mathf.Abs(p.Velocity.y)>2)return false;
        Vector3 normal=p.transform.up;if(normal.y<0)normal=-normal;
        // BoardingPoint is already inset. Walk to the last supported point on
        // the real cut mesh, then keep the palms just inside that boundary.
        for(int i=1;i<=48;i++)
        {
            Vector3 probe=edge+outward*.25f;
            if(!p.Collision.Raycast(new Ray(probe+normal*3,-normal),out var hit,6))break;
            edge=hit.point;
        }
        // Leave room for both feet while keeping the counter's weapon arc
        // over open water. The old three-metre inset pulled the huge head
        // through the support deck just to reach the descending club.
        edge-=outward*.15f;stance=p.BoardingPoint(edge-outward*1.25f);
        float edgeHeight=edge.y-RedWater(edge),standHeight=stance.y-RedWater(stance);
        if(edgeHeight<-.1f||edgeHeight>2||standHeight<-.1f||standHeight>2)return false;
        Vector3 across=Vector3.Cross(Vector3.up,-outward);
        return p.ContainsFootprint(stance-across*.7f)&&p.ContainsFootprint(stance+across*.7f)&&p.ContainsFootprint(stance-outward*.8f);
    }
    void RedOnBoard(string pose,float seconds,Quaternion? rotation=null)
    {
        Quaternion facing=rotation??Quaternion.LookRotation(redForward);
        float grounded=pose.Contains("ProneDodge")?REase(seconds,.05f,.42f):pose.Contains("GetUp")?1-REase(seconds,.65f,2.2f):0;
        Vector3 normal=redCombatBoard.transform.up;if(normal.y<0)normal=-normal;
        Quaternion deckFacing=Quaternion.LookRotation(Vector3.ProjectOnPlane(facing*Vector3.forward,normal),normal);
        facing=Quaternion.Slerp(facing,deckFacing,grounded);
        RedPose(pose,seconds);RedFeet(RedBoardPoint(redCombatBoard),facing);
        player.transform.position+=(facing*Vector3.up-Vector3.up)*(-player.LowestFootWorldOffset);
        // v215 already authors the root descent and palm support. Planting
        // its airborne feet or lowering its torso twice destroys that arc.
        if(!pose.Contains("ProneDodge")&&!pose.Contains("GetUp"))
            SahurSupportPose.Plant(redAnimator,redCombatBoard,redLeftSole,redRightSole);
        if(pose.Contains("ProneDodge")||pose.Contains("GetUp"))RedSolidSupport();
    }
    void RedSwim(Vector3 at,float seconds,float dip=0)
    {
        RedPose("PLAYER_PursuitSwim_v210",seconds);
        player.transform.SetPositionAndRotation(at,Quaternion.LookRotation(redForward));redGraph.Evaluate(0);
        player.transform.position+=Vector3.up*(RedWater(at)+.22f-dip-RedHead.y);
        if(redStow!=null){redStow.CinematicTraversal=true;redStow.CinematicStowWeight=1;redStow.RefreshPose();}
    }
    void RedShark(string clip,float seconds,Vector3 nose,Vector3 forward)
    {
        redContactPoseVersion++;
        Quaternion q=Quaternion.LookRotation(forward.sqrMagnitude>.01f?forward:redForward);
        shark.rotation=q;
        sharkAnimator.SampleCinematicAction("Swim",seconds);
        bool crush=clip=="CH1_SharkLeapCrush_v214";
        if(crush)RedReadCrushStroke();
        if(clip.StartsWith("CH1_"))RedClip(clip).SampleAnimation(sharkAnimator.BoneAnimator.gameObject,seconds);
        else sharkAnimator.SampleCinematicAction(clip,seconds);
        if(crush)RedApplyCrushStroke(seconds);
        shark.position+=nose-sharkAnimator.NoseWorldPoint;
    }
    void RedShot(int shot,Vector3 position,Vector3 focus,float fov=50)
    {
        var camera=sequence.storyCamera;camera.fieldOfView=fov;
        position=RedClearCamera(shot,position,focus);
        RedesignShot=shot;redCameraPosition=position;redCameraLook=focus;redCameraFov=fov;
        if(lastShot!=shot){RedEvent("Shot "+shot);lastShot=shot;}
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(focus-position,Vector3.up));
    }
    void RedHeroShot(int shot,Vector3 offset,float fov=48)
    {
        var chest=RedChest;
        RedShot(shot,chest+redRight*offset.x+Vector3.up*offset.y+redForward*offset.z,chest,fov);
    }
    void RedSetRate(float rate)
    {
        if(!ownsPause && !RedInputBlocked)Time.timeScale=redScaleBefore*rate;
    }
    internal void RedesignTick()
    {
        if(!RedesignActive || RedesignStage==OpeningStage.Finished || RedInputBlocked)return;
        if(TutorialWaiting){RedReadLesson();return;}
        if(Time.deltaTime<=0)return;
        float dt=Time.deltaTime;
        RedesignClock+=dt;redesignTime+=dt;
        switch(RedesignStage)
        {
            case OpeningStage.Crush:RedCrush(dt);break;
            case OpeningStage.Tutorial:RedTutorial(dt);break;
            case OpeningStage.Pursuit:RedPursuit(dt);break;
            case OpeningStage.Duel:RedDuel(dt);break;
            case OpeningStage.Drift:RedDrift(dt);break;
        }
        if(redStow!=null && redesignManual)redStow.RefreshPose();
        if(headHitbox!=null)headHitbox.transform.SetPositionAndRotation(sharkAnimator.NoseWorldPoint,shark.rotation);
    }
    void RedCrush(float dt)
    {
        float t=redesignTime;float height=redHeroHeight;
        // Allow the physically falling deck to finish entering the water before
        // the prone landing. The longer flight also suits the authored 18m arc.
        const float landingTime=10.2f;
        RedSetRate(t>=4.35f&&t<4.7f?.25f:1);
        if(t<3)RedRiggingPrepare();
        RedPrepareTutorialBody();
        if(!shattered){RedCrushShark(Mathf.Min(t,6));RedRiggingTick(t,dt);}
        if(t<6)
        {
            RedesignBeat=t<3?"Sahur spots the approaching shadow":"The shark breaches and crushes the ship";
            RedPose(t<1.3f?"CH1_SahurStickIdle_v221":"CH1_SahurGuard_v050",t<1.3f?redShipIdleStart+t:t-1.3f);
            player.transform.SetPositionAndRotation(redStart-redRight*REase(t,2.2f,3.3f)*1.4f,Quaternion.Slerp(redStartRotation,Quaternion.LookRotation(redRight),REase(t,1.4f,2.4f)));
            Story1SahurPassenger.AlignToDeck(player.transform,redAnimator,redShipDeckColliders,redLeftSole,redRightSole);
            Vector3 nose=sharkAnimator.NoseWorldPoint;
            if(t<1.5f)RedShot(0,redStart+redRight*75-redForward*65+Vector3.up*115,redStart+redRight*20+redForward*18,55);
            else if(t<3)RedHeroShot(1,new Vector3(6,2,5));
            else if(t<4.7f)RedShot(2,redStart+redRight*85+redForward*140+Vector3.up*50,Vector3.Lerp(redStart,sharkAnimator.VisualRenderer.bounds.center,.5f)+Vector3.up*12,64);
            else RedShot(3,redCrushContact-redForward*60+redRight*110+Vector3.up*115,redCrushContact+Vector3.up*Mathf.Lerp(40,18,REase(t,4.7f,6)),64);
            return;
        }
        if(!shattered)
        {
            if(RedesignContactGap>.18f)throw new InvalidOperationException("Body crush missed the hull: "+RedesignContactGap);
            redCrushLaunch=player.transform.position;
            hullContactStarted=true;MaximumHullKickDegrees=12;
            sequence.ship.transform.rotation=Quaternion.AngleAxis(12,redForward)*breakShipRotation;
            Effects.ShipImpact(redCrushContact,-Vector3.up);
            CreateWreckage();shattered=true;sequence.ship.gameObject.SetActive(false);
            if(sequence.awakening.looseShipDecoration!=null)sequence.awakening.looseShipDecoration.SetActive(false);
            // Old tail-specific scatter replay cannot describe the new downward body crush.
            if(scatterPlayback!=null)scatterPlayback.enabled=false;
            landingBoard=planks.Where(RedUsableDeck).OrderBy(p=>Vector3.Distance(p.transform.position,redStart)).FirstOrDefault();
            if(landingBoard==null)throw new InvalidOperationException("Crush has no physical landing fragment");
            redCombatBoard=landingBoard;redEnd=RedBoardPoint(landingBoard);
            RedEvent("Body crush; visible underside/hull gap="+RedesignContactGap.ToString("F3")+"; original hull fractured; landing="+landingBoard.name);
        }
        if(t<landingTime)
        {
            float u=R01(t,6,landingTime);redEnd=RedBoardPoint(landingBoard);
            RedPose("CH1_SahurAirHit_v214",u*1.1f);
            player.transform.SetPositionAndRotation(Vector3.Lerp(redCrushLaunch,redEnd,u)+Vector3.up*(18*4*u*(1-u)),Quaternion.LookRotation(redForward)*Quaternion.Euler(0,0,-25*Mathf.Sin(u*Mathf.PI)));
            RedHeroShot(4,new Vector3(15,7,-18),58);
        }
        else
        {
            RedOnBoard("CH1_SahurGetUp_v215",t-landingTime);
            RedHeroShot(5,new Vector3(12,5,-13),54);
        }
        RedCrushRecovery(t);
        if(t>=landingTime+2.6f)
        {
            RedReleaseCatchupBudget();
            if(!sharkPrepared){sharkPrepared=true;PrepareShark();}
            if(WreckContact!=null)WreckContact.enabled=false;
            SharkHealth.IsProtected=true;bossName.enabled=false;healthFill.transform.parent.gameObject.SetActive(false);
            CurrentPhase=Phase.Fighting;RedesignStage=OpeningStage.Tutorial;redesignTime=0;
            rider=player.GetComponent<Story1WreckRider>();if(rider==null)rider=player.gameObject.AddComponent<Story1WreckRider>();
            rider.Initialize(player,planks,ocean,landingBoard,detailPieces);rider.enabled=false;
            CinematicLetterbox.Set(this,false);RedGate(Lesson.Move);
        }
    }
    string RedKeys()=>string.Join(" / ",new[]{GameInputSettings.Action.Forward,GameInputSettings.Action.Left,GameInputSettings.Action.Back,GameInputSettings.Action.Right}.Select(a=>GameInputSettings.Get(a).ToString()));
    string RedLessonMessage(Lesson lesson)=>lesson==Lesson.Move?RedKeys()+"  Move across the wreckage":
        lesson==Lesson.Attack?"Left Mouse  Attack the approaching shark":
        lesson==Lesson.Dodge?GameInputSettings.Get(GameInputSettings.Action.Dodge)+"  Dodge the bite":
        lesson==Lesson.ChargeHold?"Hold Right Mouse  Charge":lesson==Lesson.ChargeRelease?"Release Right Mouse  Heavy attack":
        lesson==Lesson.Jump?GameInputSettings.Get(GameInputSettings.Action.Jump)+"  Jump into the water before the wreck breaks":RedKeys()+"  Swim forward to escape the shark";
    void RedGate(Lesson lesson)
    {
        CurrentLesson=lesson;TutorialWaiting=true;lessonExecuting=false;lessonTime=holdTime=0;
        if(!ownsPause)storedTimeScale=Time.timeScale>0?Time.timeScale:redScaleBefore;
        ownsPause=true;Time.timeScale=0;redChargeInterrupted=false;
        string message=RedLessonMessage(lesson);
        controls.text=message;controls.fontSize=30;controls.enabled=true;
        controls.rectTransform.anchorMin=controls.rectTransform.anchorMax=new Vector2(.5f,.2f);
        controls.rectTransform.anchoredPosition=Vector2.zero;controls.rectTransform.sizeDelta=new Vector2(1400,100);
        RedesignBeat="Tutorial: "+message;RedEvent("Gate "+lesson);
    }
    Vector2 RedMoveIntent=>new Vector2((GameInputSettings.Pressed(GameInputSettings.Action.Right)?1:0)-(GameInputSettings.Pressed(GameInputSettings.Action.Left)?1:0),
        (GameInputSettings.Pressed(GameInputSettings.Action.Forward)?1:0)-(GameInputSettings.Pressed(GameInputSettings.Action.Back)?1:0));
    bool RedMoveInput()=>RedMoveIntent.sqrMagnitude>.01f;
    bool RedMeasureMovement()
    {
        Vector3 at=player.transform.position,travel=at-redSwimStart;
        var support=CurrentLesson==Lesson.Move&&rider!=null?rider.Support():null;
        if(support!=null)
        {
            Vector3 local=support.transform.InverseTransformPoint(at);
            travel=support==redMovementBoard?support.transform.TransformVector(local-redMovementLocal):Vector3.zero;
            redMovementLocal=local;
        }
        else if(CurrentLesson==Lesson.Move)travel=Vector3.zero;
        redMovementBoard=support;redSwimStart=at;
        bool correctMode=CurrentLesson==Lesson.Swim?player.Swimming:support!=null&&!player.Swimming;
        if(correctMode&&!RedInputBlocked&&Cursor.lockState==CursorLockMode.Locked&&RedMoveInput())
            redMoveDistance+=Vector3.ProjectOnPlane(travel,Vector3.up).magnitude;
        return redMoveDistance>2;
    }
    void RedReadLesson()
    {
        // Map/equipment/death overlays own their input and pause even during a tutorial gate.
        if(RedInputBlocked)
        {
            if(CurrentLesson==Lesson.ChargeHold||CurrentLesson==Lesson.ChargeRelease)redChargeInterrupted=true;
            return;
        }
        if(redChargeInterrupted)
        {
            holdTime=0;CurrentLesson=Lesson.ChargeHold;redChargeInterrupted=false;
        }
        // The director itself can be suspended by an overlay. A release consumed there
        // requires a fresh charge, rather than an unexplained automatic strike on return.
        if(CurrentLesson==Lesson.ChargeRelease&&Mouse.current!=null&&!Mouse.current.rightButton.isPressed&&!Mouse.current.rightButton.wasReleasedThisFrame)
        {holdTime=0;CurrentLesson=Lesson.ChargeHold;}
        controls.text=RedLessonMessage(CurrentLesson); // Bindings may have changed in the settings overlay.
        bool accepted=CurrentLesson==Lesson.Move||CurrentLesson==Lesson.Swim?RedMoveInput():
            CurrentLesson==Lesson.Attack?Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame:
            CurrentLesson==Lesson.Dodge?GameInputSettings.PressedThisFrame(GameInputSettings.Action.Dodge):
            CurrentLesson==Lesson.Jump?GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump):
            CurrentLesson==Lesson.ChargeHold?Mouse.current!=null&&Mouse.current.rightButton.isPressed:
            CurrentLesson==Lesson.ChargeRelease&&Mouse.current!=null&&Mouse.current.rightButton.wasReleasedThisFrame;
        if(CurrentLesson==Lesson.ChargeHold)
        {
            holdTime=accepted?holdTime+Time.unscaledDeltaTime:0;
            RedOnBoard("CH1_SahurOverhead_v215",Mathf.Lerp(0,1.05f,Mathf.Clamp01(holdTime/1.2f)));
            RedKeepTutorialSharkClear();
            controls.text="Hold Right Mouse  Charge  "+Mathf.RoundToInt(Mathf.Clamp01(holdTime/1.2f)*100)+"%";
            if(holdTime>=1.2f){CurrentLesson=Lesson.ChargeRelease;controls.text="Release Right Mouse  Heavy attack";RedEvent("Charge ready");}
            return;
        }
        if(accepted)RedAcceptLesson();
    }
    void RedAcceptLesson()
    {
        if(!TutorialWaiting||RedInputBlocked)return;
        ReleaseRedesignPause();TutorialWaiting=false;lessonExecuting=true;lessonTime=0;lessonImpact=false;
        RedEvent("Accepted "+CurrentLesson);controls.text="";
        if(CurrentLesson==Lesson.Move||CurrentLesson==Lesson.Swim)
        {
            redSwimStart=player.transform.position;redMoveDistance=0;redDiagnosticMovement=false;
            redMovementBoard=null;RedManual(false);
            if(CurrentLesson==Lesson.Swim)redSwimControlSeconds=0;
            if(CurrentLesson==Lesson.Move&&rider!=null)
            {
                rider.ResumeCinematicSupport(redCombatBoard);
                redMovementBoard=rider.Support();
                if(redMovementBoard!=null)redMovementLocal=redMovementBoard.transform.InverseTransformPoint(player.transform.position);
            }
        }
        if(CurrentLesson==Lesson.Dodge)RedPlanTutorialDodge();
        if(CurrentLesson==Lesson.Jump){redStart=player.transform.position;redEnd=redStart+redForward*13;redEnd.y=RedWater(redEnd);}
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DiagnosticAcceptLesson()
    {
        if(!TutorialWaiting)throw new InvalidOperationException("No tutorial input gate");
        if(CurrentLesson==Lesson.ChargeHold){holdTime=1.2f;CurrentLesson=Lesson.ChargeRelease;}
        RedAcceptLesson();
    }
    public void DiagnosticCompleteMovement()
    {
        if(RedesignStage==OpeningStage.Tutorial&&(CurrentLesson==Lesson.Move||CurrentLesson==Lesson.Swim)&&lessonExecuting)
            redDiagnosticMovement=true;
    }
#endif
    void ReleaseRedesignPause()
    {
        if(!redDisposing&&!isActiveAndEnabled&&RedesignActive){DisposeRedesign();return;}
        if(!ownsPause)return;ownsPause=false;
        if(!RedInputBlocked)Time.timeScale=storedTimeScale;
        else Story1OpeningPauseRelease.Request(storedTimeScale);
    }
    void RedTutorialDamage(float amount,Vector3 contact)
    {
        float permitted=Mathf.Min(amount,Mathf.Max(0,TutorialDamageBudget-TutorialDamage),Mathf.Max(0,playerHealth.currentHealth-TutorialHealthFloor));
        if(permitted<=0)return;
        // Respect protection owned by another system; only actually applied damage spends the budget.
        float before=playerHealth.currentHealth;playerHealth.ApplyDamage(permitted,contact);
        float applied=Mathf.Max(0,before-playerHealth.currentHealth);
        TutorialDamage=Mathf.Min(TutorialDamageBudget,TutorialDamage+applied);RedEvent("Tutorial damage="+applied);
    }
    void RedTutorial(float dt)
    {
        RedSetRate(1);
        Vector3 feet=RedBoardPoint(redCombatBoard),aim=feet+Vector3.up*(redHeroHeight*.65f);
        if(!lessonExecuting)return;
        lessonTime+=dt;
        if(CurrentLesson==Lesson.Move||CurrentLesson==Lesson.Swim)
        {
            Vector3 nose=player.transform.position-redForward*22;nose.y=RedWater(nose)-1;
            RedShark("Swim",redesignTime,nose,redForward);
            bool moved=RedMeasureMovement();
            if(CurrentLesson==Lesson.Swim&&player.Swimming&&RedMoveInput())redSwimControlSeconds+=dt;
            bool complete=CurrentLesson==Lesson.Swim?moved&&redMoveDistance>=8&&redSwimControlSeconds>=6:moved;
            if(redDiagnosticMovement || complete)
            {
                if(CurrentLesson==Lesson.Move){if(rider!=null&&rider.Support()!=null)redCombatBoard=rider.Support();RedStoreTutorialStance(player.transform.position+Vector3.up*player.LowestFootWorldOffset);RedManual(true);RedOnBoard("CH1_SahurGuard_v050",0);lessonExecuting=false;lessonTime=0;CurrentLesson=Lesson.Attack;}
                else {controls.text="";RedBeginPursuit();}
            }
            return;
        }
        if(CurrentLesson==Lesson.Attack)
        {
            RedOnBoard("CH1_SahurSlash_v006",Mathf.Min(lessonTime,1.4f));
            Vector3 nose=RedTutorialNose(feet,Mathf.Lerp(5,16,REase(lessonTime,.65f,1.6f)));
            RedShark(lessonTime<.65f?"Bite_Lunge":"Hit_Recoil",lessonTime<.65f?.45f:lessonTime-.65f,nose,-redForward+Vector3.up*.12f);
            if(!lessonImpact)
            {
                RedFitTutorialFrontToWeapon(REase(lessonTime,.15f,.6f));
            }
            RedKeepTutorialSharkClear(.2f);
            if(!lessonImpact&&lessonTime>=.6f)lessonImpact=RedRegisterHeadHit("Attack success",-redForward,.8f);
            if(lessonTime>=2){RedOnBoard("CH1_SahurGuard_v050",0);RedGate(Lesson.Dodge);}
        }
        else if(CurrentLesson==Lesson.Dodge)
        {
            RedTutorialDodge(lessonTime);
        }
        else if(CurrentLesson==Lesson.ChargeRelease)
        {
            RedOnBoard("CH1_SahurOverhead_v215",1.05f+lessonTime);
            RedShark("Hit_Recoil",Mathf.Max(0,lessonTime-.25f),aim+redForward*Mathf.Lerp(2,15,REase(lessonTime,.25f,1.7f)),-redForward);
            if(!lessonImpact)
            {
                RedFitTutorialFrontToWeapon(REase(lessonTime,0,.25f));
            }
            RedKeepTutorialSharkClear(.2f);
            if(!lessonImpact&&lessonTime>=.25f)lessonImpact=RedRegisterHeadHit("Charge success",Vector3.down,1.5f);
            if(lessonTime>=2.1f){lessonExecuting=false;lessonTime=0;stowReady=false;CurrentLesson=Lesson.Jump;}
        }
        else if(CurrentLesson==Lesson.Jump)
        {
            float u=R01(lessonTime,0,1.5f);
            RedPose("PLAYER_JumpAir_v006",lessonTime);player.transform.SetPositionAndRotation(Vector3.Lerp(redStart,redEnd,u)+Vector3.up*(4*u*(1-u)*5),Quaternion.LookRotation(redForward));
            if(redStow!=null)redStow.CinematicTraversal=true;
            Vector3 bite=redCombatBoard.transform.position;
            RedShark("Bite_Lunge",lessonTime,bite-redForward*5-Vector3.up*Mathf.Lerp(6,1,REase(u,0,.8f)),redForward);
            if(!redCombatBoard.IsBroken)
            {
                Vector3 target=redCombatBoard.Collision.ClosestPoint(bite)-Vector3.up*.025f;
                shark.position+=(target-RedHeadSurface(target))*REase(u,.25f,.75f);
            }
            if(u>=1){RedSwim(redEnd,0);RedGate(Lesson.Swim);}
        }
        RedHeroShot(10,new Vector3(9,5,-13),55);
    }
    // Approaches run before their input gates so the paused image explains the requested action.
    internal void RedesignPrepareLesson(float dt)
    {
        if(!RedesignActive||RedesignStage!=OpeningStage.Tutorial||TutorialWaiting||lessonExecuting||RedInputBlocked||dt<=0)return;
        lessonTime+=dt;Vector3 feet=RedBoardPoint(redCombatBoard);Vector3 aim=feet+Vector3.up*(redHeroHeight*.65f);
        if(CurrentLesson==Lesson.Attack)
        {
            RedOnBoard("CH1_SahurGuard_v050",lessonTime);
            RedShark("Swim",lessonTime,RedTutorialNose(feet,Mathf.Lerp(18,6,REase(lessonTime,0,1.8f))),-redForward+Vector3.up*.12f);
            RedKeepTutorialSharkClear(.45f);
            RedHeroShot(10,new Vector3(9,5,-13),55);
            if(lessonTime>=1.8f)RedGate(Lesson.Attack);
        }
        else if(CurrentLesson==Lesson.Jump)
        {
            RedOnBoard("CH1_SahurStow_v214",lessonTime);
            if(redStow!=null){redStow.CinematicStowWeight=REase(lessonTime,.45f,.95f);redStow.CinematicTraversal=lessonTime>=.9f;}
            Vector3 nose=feet-redForward*5;nose.y=RedWater(nose)-Mathf.Lerp(5,.5f,REase(lessonTime,1,2.3f));
            RedShark("Swim",lessonTime,nose,redForward);
            if(lessonTime>=1.4f&&!stowReady)
            {
                stowReady=true;var body=redCombatBoard.GetComponent<Rigidbody>();
                if(body!=null&&!body.isKinematic)body.AddForceAtPosition(Vector3.up*body.mass*2,feet-redForward*2,ForceMode.Impulse);
                // A visible upheaval throws the hero off balance and spends a bounded damage amount.
                Effects.Impact(feet-redForward*2,Vector3.up,1);RedTutorialDamage(12,feet);
            }
            RedHeroShot(11,new Vector3(11,5,-12),55);
            if(lessonTime>=2.5f)RedGate(Lesson.Jump);
        }
    }
    void RedBeginPursuit()
    {
        var candidates=planks.Where(p=>p!=landingBoard&&RedUsableDeck(p)).OrderBy(p=>Vector3.Distance(p.transform.position,player.transform.position+redForward*32));
        Story1WreckPlank chosen=null;Vector3 edge=default,stance=default,outward=default;
        foreach(var candidate in candidates)if(RedFindEntry(candidate,player.transform.position,out edge,out stance,out outward)){chosen=candidate;break;}
        if(chosen==null){controls.text="Keep swimming. Find wreckage to climb onto.";return;}
        CurrentLesson=Lesson.Complete;RedesignStage=OpeningStage.Pursuit;CurrentPhase=Phase.Finale;redesignTime=0;
        RedManual(true);CinematicLetterbox.Set(this,true);
        redStart=player.transform.position;
        redCombatBoard=chosen;
        if(redCombatBoard==null)throw new InvalidOperationException("Pursuit needs a surviving real board");
        redEdgeLocal=redCombatBoard.transform.InverseTransformPoint(edge);
        redStanceLocal=redCombatBoard.transform.InverseTransformPoint(stance);redHasStance=true;
        redEnd=RedBoardPoint(redCombatBoard);redForward=-outward;redRight=Vector3.Cross(Vector3.up,redForward);
        if(redStow!=null)redStow.CinematicTraversal=true;
        RedEvent("Player swimming handed to pursuit; target="+redCombatBoard.name);
    }
    void RedPursuit(float dt)
    {
        float t=redesignTime;RedSetRate(1);redEnd=RedBoardPoint(redCombatBoard);
        Vector3 edge=redCombatBoard.transform.TransformPoint(redEdgeLocal);
        Vector3 approach=edge-redForward*2.5f;Vector3 at=Vector3.Lerp(redStart,approach,REase(t,0,9));
        Vector3 chaseLine=at;
        if(t<4)at-=redRight*(3.2f*Mathf.Sin(Mathf.PI*R01(t,1.7f,3.5f)));
        else if(t<7.5f)at+=redForward*(3*Mathf.Sin(Mathf.PI*R01(t,4.7f,6.5f)));
        float dip=t<4?Mathf.Sin(Mathf.PI*R01(t,2,3.5f))*1.7f:t<7?Mathf.Sin(Mathf.PI*R01(t,5.1f,6.7f))*1.4f:0;
        RedSwim(at,t,dip);
        if(t>=1.6f&&t<3.6f)
        {
            var head=redAnimator.GetBoneTransform(HumanBodyBones.Head);
            head.rotation=Quaternion.AngleAxis(-100*REase(t,1.6f,2.1f)*(1-REase(t,3.1f,3.6f)),Vector3.up)*head.rotation;
        }
        Vector3 nose=at-redForward*18; nose.y=RedWater(nose)-1;
        string sharkPose="Swim";float poseTime=t;Vector3 facing=redForward;
        if(t<4)
        {
            RedesignBeat="First bite: a desperate look back";
            float u=R01(t,1.7f,3.5f);nose=chaseLine+redForward*Mathf.Lerp(-18,5,u)+redRight*3;nose.y=RedWater(nose)+Mathf.Sin(u*Mathf.PI)*6;
            sharkPose="Bite_Lunge";poseTime=u*1.3f;
            if(t<1.8f)RedHeroShot(20,new Vector3(4,1,5),48);
            else if(t<2.5f)RedHeroShot(21,new Vector3(2,1,4),65);
            else RedHeroShot(22,new Vector3(13,2,0),54);
        }
        else if(t<7.5f)
        {
            RedesignBeat="Second bite: ambush from the side";
            float u=R01(t,4.7f,6.5f);nose=chaseLine+redRight*Mathf.Lerp(18,-7,u)-redForward*3;nose.y=RedWater(nose)+Mathf.Sin(u*Mathf.PI)*5;facing=-redRight;
            sharkPose="Bite_Lunge";poseTime=u*1.3f;
            if(t<5.2f)RedShot(23,at-redRight*3+redForward*5+Vector3.up*(RedWater(at)-at.y+2.2f),RedHead,48);
            else RedShot(24,at-redRight*10-redForward*8+Vector3.up*(RedWater(at)-at.y+4),RedHead,58);
        }
        else
        {
            RedesignBeat="Third bite: scramble onto the plank";
            float climb=R01(t,9,12);
            if(t>=9)
            {
                RedPose("PLAYER_Climb_v010",climb*RedClip("PLAYER_Climb_v010").length);
                Vector3 support=Vector3.Lerp(approach,redEnd,REase(climb,.42f,1));
                support.y=Mathf.Lerp(RedWater(approach)-redHeroHeight*.65f,redEnd.y,REase(climb,0,.65f));
                // This older imported clip faces opposite to the native
                // guard rig; compensate here so the face looks onto the deck.
                RedFeet(support,Quaternion.LookRotation(-redForward));
                float grip=REase(climb,0,.12f)*(1-REase(climb,.62f,.84f));
                SahurSupportPose.Hand(redAnimator,true,edge-redRight*.45f,grip);
                SahurSupportPose.Hand(redAnimator,false,edge+redRight*.45f,grip);
                if(climb>.68f)RedSolidSupport();
            }
            float u=R01(t,9,11.5f);nose=approach-redForward*Mathf.Lerp(16,1,u);nose.y=RedWater(nose)+Mathf.Sin(u*Mathf.PI)*3;
            sharkPose="Bite_Lunge";poseTime=u*1.3f;
            Vector3 focus=Vector3.Lerp(RedChest,RedHead,.65f);
            RedShot(25,edge+redForward*11+Vector3.up*2.2f,focus+Vector3.up*.8f,59);
        }
        RedShark(sharkPose,poseTime,nose,facing);
        if(t>=12.4f)
        {
            RedesignStage=OpeningStage.Duel;redesignTime=0;redForward=-redForward;redRight=Vector3.Cross(Vector3.up,redForward);
            redBoardLocal=redCombatBoard.transform.InverseTransformPoint(RedBoardPoint(redCombatBoard));
            RedEvent("On board; breathe, retrieve stick");
        }
    }
    Vector3 RedWeaponPoint
    {
        get
        {
            if(redWeapon==null)return redStick!=null?redStick.position:RedChest;
            Story1CapsuleContact.Segment(redWeapon,out var a,out var b,out _);
            return Vector3.Distance(a,RedChest)>Vector3.Distance(b,RedChest)?a:b;
        }
    }
    void RedDuel(float dt)
    {
        float t=redesignTime;Vector3 feet=RedBoardPoint(redCombatBoard);Vector3 nose=feet+redForward*15; nose.y=RedWater(nose)-1;
        Vector3 face=-redForward;string sharkPose="Swim";float sharkTime=t;
        RedSetRate(redOverheadHit&&t<5.45f?.22f:redTailHit&&t>=redTailHitTime+.08f&&t<redTailHitTime+.68f?.22f:1);
        if(t<3)
        {
            RedesignBeat="Catch a breath. Draw the stick.";RedOnBoard(t<1.7f?"CH1_SahurGuard_v050":"CH1_SahurRetrieve_v214",t<1.7f?t:t-1.7f);
            player.transform.rotation=Quaternion.Slerp(Quaternion.LookRotation(-redForward),Quaternion.LookRotation(redForward),REase(t,0,.7f));
            if(redStow!=null){redStow.CinematicStowWeight=1-REase(t,2.3f,2.8f);redStow.CinematicTraversal=t<2.3f;}
            RedHeroShot(30,new Vector3(7,1,5),48);
        }
        else if(t<6.8f)
        {
            RedesignBeat="Two-handed overhead counter";RedOnBoard("CH1_SahurOverhead_v215",Mathf.Max(0,t-4));
            float u=R01(t,3.4f,5.3f);Vector3 target=RedWeaponPoint;
            face=Vector3.Slerp((-redForward+Vector3.up*.55f).normalized,-redForward,REase(t,5.4f,6.8f));
            nose=Vector3.Lerp(feet+redForward*17-Vector3.up*3,target,u*u);sharkPose="Bite_Lunge";sharkTime=u*.52f;
            if(t>=5.3f){float fall=R01(t,5.3f,6.8f);nose=target+redForward*(fall*8)-Vector3.up*(fall*7);sharkPose="Hit_Recoil";sharkTime=t-5.3f;}
            RedShot(31,feet+redRight*16+redForward*6+Vector3.up*6,feet+redForward*4+Vector3.up*3,53);
        }
        else if(t<10)
        {
            RedesignBeat="The shark disappears. Sahur searches.";RedOnBoard("CH1_SahurGuard_v050",t);
            player.transform.rotation=Quaternion.LookRotation(redForward)*Quaternion.Euler(0,Mathf.Sin((t-6.8f)*2)*35,0);
            float u=R01(t,6.8f,10);nose=feet+redForward*(Mathf.Cos(u*Mathf.PI)*16)+redRight*(Mathf.Sin(u*Mathf.PI)*20);nose.y=RedWater(nose)-8;
            face=Vector3.Lerp(-redForward,redForward,u);RedHeroShot(32,new Vector3(5,1,7),48);
        }
        else if(t<13)
        {
            RedesignBeat="Rear ambush: dive flat";
            Quaternion turn=Quaternion.Slerp(Quaternion.LookRotation(redForward),Quaternion.LookRotation(-redForward),REase(t,10,10.35f));
            RedOnBoard(t<10.35f?"CH1_SahurGuard_v050":"CH1_SahurProneDodge_v215",t<10.35f?t:t-10.35f,turn);
            float u=R01(t,10.25f,12.65f);
            // The jaws skim over the ducking hero, then the original broad
            // animal banks beside the board instead of driving its bulk
            // through the same plank/body footprint.
            float bank=REase(u,.05f,.65f);
            nose=feet+redForward*Mathf.Lerp(-14,13,u)+redRight*Mathf.Lerp(3,40,bank)+Vector3.up*(redHeroHeight*.9f+3+Mathf.Sin(u*Mathf.PI)*3);
            // Move the full-width body outboard before yawing; turning early
            // swings the belly/fin back across the board behind the jaws.
            face=Vector3.Lerp(redForward,(redForward+redRight).normalized,REase(u,.72f,1));sharkPose="Bite_Lunge";sharkTime=u*1.3f;
            if(t<10.4f)RedHeroShot(33,new Vector3(-3,2,5),65);else RedShot(34,feet-redRight*21+redForward*8+Vector3.up*7,feet+Vector3.up*2.5f,60);
        }
        else if(t<15.5f)
        {
            RedesignBeat="Get up and wind up the throw";
            RedOnBoard("CH1_SahurGetUp_v215",t-13,Quaternion.Slerp(Quaternion.LookRotation(-redForward),Quaternion.LookRotation(redForward),REase(t,14.9f,15.5f)));
            RedHeroShot(35,new Vector3(10,3,-10),52);
            float retreat=REase(t,13,15.5f);
            nose=feet+redForward*Mathf.Lerp(13,18,retreat)+redRight*Mathf.Lerp(40,3,retreat);
            nose.y=Mathf.Lerp(feet.y+redHeroHeight*.9f+3,RedWater(nose)+1,retreat);
            face=Vector3.Slerp((redForward+redRight).normalized,-redForward,REase(t,13,14));
        }
        else if(t<19.5f)
        {
            RedesignBeat="Head-side hit and returning catch";RedOnBoard("PLAYER_Throw_v064",Mathf.Min(2.0f,t-15.5f));
            nose=feet+redForward*18+redRight*3;nose.y=RedWater(nose)+1.8f;face=redRight;
            RedShark("Swim",t,nose,face);
            if(t>=16.05f&&!redReleased){RedThrow();redThrowStart=redStick.position;redThrowTarget=RedHeadSurface(nose-redForward*2);redReleased=true;RedEvent("Stick released");}
            if(redReleased&&redProjectile!=null)
            {
                float fly=R01(t,16.05f,16.8f),back=R01(t,16.8f,17.8f);
                redProjectile.rotation=redStickRotation*Quaternion.AngleAxis((t-16.05f)*1100,Vector3.up);
                if(t>=17.55f)redProjectile.rotation=Quaternion.Slerp(redProjectile.rotation,redStick.rotation,REase(t,17.55f,17.8f));
                Vector3 shaftCentre=(redProjectileA+redProjectileB)*.5f;
                // The animal moves with the board/waves during flight. Track
                // its posed surface, not the stale world point at release.
                if(!redThrowHit)redThrowTarget=RedHeadSurface(nose-redForward*2);
                Vector3 destination=redThrowTarget-redProjectile.TransformVector(shaftCentre)+Vector3.ProjectOnPlane(feet-redThrowTarget,Vector3.up).normalized*redProjectileRadius;
                // Retain the actual endpoint on the first crossing frame at
                // any update rate, before starting the return arc.
                redProjectile.position=!redThrowHit&&t>=16.8f?destination:
                    t<16.8f?Vector3.Lerp(redThrowStart,destination,fly)+Vector3.up*Mathf.Sin(fly*Mathf.PI):Vector3.Lerp(destination,redStick.position,back)+redRight*Mathf.Sin(back*Mathf.PI)*4;
                if(t<16.8f)RedShot(36,redProjectile.position-redForward*3+redRight*6+Vector3.up*2,redProjectile.position+redForward*4,60);
                else RedHeroShot(37,new Vector3(7,2,-6),49);
                if(t>=16.75f&&!redThrowHit)
                {
                    RedesignContactGap=RedThrownHeadGap(out var impact);
                    if(RedesignContactGap<=.035f){redThrowHit=true;RedesignContacts++;Effects.Impact(impact,redRight,.7f);RedEvent("Thrown stick head-side contact; shaft gap="+RedesignContactGap.ToString("F3"));}
                }
                if(t>=17.8f){redProjectile.GetComponent<Renderer>().enabled=false;Destroy(redProjectile.gameObject);redProjectile=null;RedShowStick(true);RedEvent("Stick caught");}
            }
            else if(t<18)RedHeroShot(37,new Vector3(7,2,-6),49);
            if(t>=18){sharkPose="CH1_SharkHeadShake_v214";sharkTime=t-18;RedShot(38,nose-redForward*8+redRight*5+Vector3.up*3,nose-redRight*2,45);}
        }
        else if(!redTailHit)
        {
            RedesignBeat="A false bite. Sahur readies a counter.";RedOnBoard("CH1_SahurOverhead_v215",Mathf.Min(1.05f,(t-19.5f)*.7f));
            nose=feet+redForward*Mathf.Lerp(14,4,REase(t,19.5f,21.2f))+Vector3.up*3;
            sharkPose="CH1_SharkFeintTail_v214";sharkTime=t<21.2f?Mathf.Lerp(0,1.25f,R01(t,19.5f,21.2f)):Mathf.Lerp(1.25f,1.58f,R01(t,21.2f,21.65f));
            if(t<20.65f)RedShot(39,nose-redForward*6+Vector3.up*2,nose+redForward*2,43);else RedHeroShot(40,new Vector3(6,2,6),48);
        }
        else
        {
            float flight=t-redTailHitTime;
            RedesignBeat=flight<.68f?"Tail strike: airborne in slow motion":"An uncontrolled fall";
            RedPose("CH1_SahurAirHit_v214",flight);
            player.transform.SetPositionAndRotation(RedFlightPosition(flight),Quaternion.LookRotation(redForward)*Quaternion.Euler(0,0,-flight*35));
            RedContinueLostStick(dt);
            sharkPose="CH1_SharkFeintTail_v214";sharkTime=Mathf.Min(2.79f,1.58f+flight);
            if(flight<.68f){Vector3 focus=Vector3.Lerp(RedChest,RedHead,.6f);RedShot(41,focus+redRight*9-redForward*4+Vector3.up*1.5f,focus,48);}
            else RedHeroShot(42,new Vector3(17,9,-15),58);
            if(flight>1&&RedChest.y<=RedWater(RedChest)+.3f)
            {
                Effects.Impact(RedChest,Vector3.up,2.5f);redFloatStart=player.transform.position;
                redSplashRotation=player.transform.rotation;redSplashPoseTime=flight;redSplashHeadDepth=RedHead.y-RedWater(RedHead);
                redFloatForward=redForward;RedesignStage=OpeningStage.Drift;redesignTime=0;RedSetRate(1);RedEvent("Unconscious splash");
            }
        }
        RedShark(sharkPose,sharkTime,nose,face);
        if(t>=10&&t<13)RedMeasureNearPass();
        if(t>=3&&t<6.8f&&!redOverheadHit)
        {
            RedFitTutorialFrontToWeapon(REase(t,4.9f,5.3f));
        }
        // Sample the real tail pose and fit the whole shark before contact, without moving the marker independently.
        if(t>=21.2f&&!redTailHit)
        {
            float turn=REase(t,21.2f,21.65f);Quaternion q=Quaternion.LookRotation(-redForward)*Quaternion.Euler(0,turn*105,0);
            shark.rotation=q;
            Vector3 target=Vector3.Lerp(redAnimator.GetBoneTransform(HumanBodyBones.Hips).position,RedChest,.5f);
            target-=redRight*.55f;
            shark.position+= (target-sharkAnimator.TailWorldPoint)*turn;
            // Fit the visible fin to the outside of the whole posed body.
            // A marker placed at the hip centre buried the broad native fin
            // through his chest before the previous contact check succeeded.
            RedSeparateTutorialShark(0,-redRight);
            RedesignContactGap=RedTailSkinBodyGap(out var contact);
            if(t>=21.6f&&RedesignContactGap<=.035f)
            {
                redTailHit=true;redTailHitTime=t;redFloatStart=player.transform.position;
                RedPlanWaterExit();
                redTailExitRoot=shark.position;redTailExitRotation=shark.rotation;
                RedesignContacts++;Effects.Impact(contact,redRight,1.8f);RedTutorialDamage(18,contact);
                RedThrow();redThrowStart=redStick.position;redLostVelocity=redWaterExitVelocity*1.08f+Vector3.up*8;RedBeginLostStick();
                RedEvent("Final tail contact; visible tail/body gap="+RedesignContactGap.ToString("F3"));
            }
        }
        else if(redTailHit){float age=t-redTailHitTime;shark.SetPositionAndRotation(redTailExitRoot-redRight*(age*12)-Vector3.up*(age*3),redTailExitRotation);}
        RedResolveOpeningContacts();
        if(t>=5.3f&&t<6.8f&&!redOverheadHit&&RedRegisterHeadHit("Overhead contact",Vector3.down,1.5f)){redOverheadHit=true;RedesignContacts++;}
    }
    void RedShowStick(bool visible)
    {
        if(redHeld==null)return;for(int i=0;i<redHeld.Length;i++)if(redHeld[i]!=null)redHeld[i].enabled=visible&&redHeldVisible[i];redHidden=!visible;
    }
    void RedThrow()
    {
        if(redStick==null||redProjectile!=null)return;
        var go=new GameObject("Opening thrown stick v214",typeof(MeshFilter),typeof(MeshRenderer));redProjectile=go.transform;
        var mesh=redStick.GetComponent<MeshFilter>();var render=redStick.GetComponent<MeshRenderer>();
        go.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;go.GetComponent<MeshRenderer>().sharedMaterials=render.sharedMaterials;
        redProjectile.SetPositionAndRotation(redStick.position,redStick.rotation);redProjectile.localScale=redStick.lossyScale;redStickRotation=redStick.rotation;RedShowStick(false);
        Story1CapsuleContact.Segment(redWeapon,out var a,out var b,out redProjectileRadius);
        redProjectileA=redStick.InverseTransformPoint(a);redProjectileB=redStick.InverseTransformPoint(b);
    }
    void RedDrift(float dt)
    {
        RedesignBeat="Unconscious, drifting face up";RedSetRate(1);
        float t=redesignTime;RedPose("PLAYER_Fall_v006",2.2f,false);
        if(!redSplashPose.IsValid())
        {
            redSplashPose=AnimationClipPlayable.Create(redGraph,RedClip("CH1_SahurAirHit_v214"));redSplashPose.SetSpeed(0);
            redSplashPose.SetTime(Mathf.Min(redSplashPoseTime,RedClip("CH1_SahurAirHit_v214").length-.0001f));
            redMixer.ConnectInput(1,redSplashPose,0);
        }
        float rise=REase(t,.4f,2.8f);
        redMixer.SetInputWeight(0,rise);redMixer.SetInputWeight(1,1-rise);
        Vector3 p=redFloatStart+redFloatForward*(t*.35f);
        player.transform.SetPositionAndRotation(p,Quaternion.Slerp(redSplashRotation,Quaternion.LookRotation(redFloatForward),rise));redGraph.Evaluate(0);
        float depth=t<.5f?Mathf.Lerp(redSplashHeadDepth,-.65f,REase(t,0,.5f)):Mathf.Lerp(-.65f,.32f,REase(t,.5f,2.8f));
        float desired=RedWater(RedHead)+depth;
        player.transform.position+=Vector3.up*(desired-RedHead.y);
        RedAvoidDrift(dt);RedContinueLostStick(dt);
        RedShot(43,RedChest+redRight*7+Vector3.up*9-redForward*5,Vector3.Lerp(RedChest,RedHead,.3f),50);
        if(t>=5)
        {
            blackout.enabled=true;blackout.color=new Color(0,0,0,R01(t,5,6.2f));
            if(t>=6.3f){CurrentPhase=Phase.Finished;RedesignStage=OpeningStage.Finished;RedEvent("Shore handoff");sequence.awakening.BeginBlackout(blackout,center,heading);CinematicLetterbox.Set(this,false);}
        }
    }
    void DisposeRedesign()
    {
        RedReleaseCatchupBudget();
        redTutorialEnvelopeWork?.Dispose();redTutorialEnvelopeWork=null;
        RedWaterDispose();
        RedSolidDispose();
        RedCameraDispose();
        RedNearPassDispose();
        RedRiggingDispose();
        if(redDisposing||!RedesignActive)return;
        redDisposing=true;
        ReleaseRedesignPause();
        if(!RedInputBlocked)Time.timeScale=redScaleBefore;
        else Story1OpeningPauseRelease.Request(redScaleBefore);
        if(redGraph.IsValid())redGraph.Destroy();
        bool ownsActor=player!=null&&player.Motion!=null&&player.Motion.Owner==this;
        if(ownsActor)
        {
            RestoreRedesignAnimator();
            if(redStow!=null){redStow.CinematicTraversal=false;redStow.CinematicStowWeight=-1;redStow.RefreshPose();}
            player.Motion.Release(this);
            // The finished encounter has handed disabled control to the awakening coroutine.
            if(RedesignStage!=OpeningStage.Finished)
            {
                player.ExternalControlLock=redPriorControlLock;player.ExternalMovementLock=redPriorMovementLock;
                if(capsule!=null)capsule.enabled=redPriorCapsule;
                player.enabled=redPriorMovement;
                var attack=player.GetComponent<SahurAttack>();if(attack!=null)attack.enabled=redPriorAttack;
                var boomerang=player.GetComponent<SahurBoomerang>();if(boomerang!=null)boomerang.enabled=redPriorBoomerang;
                if(!RedInputBlocked){Cursor.lockState=redPriorCursor;Cursor.visible=redPriorCursorVisible;}
            }
        }
        if(redProjectile!=null)Destroy(redProjectile.gameObject);
        if(redContactMesh!=null)Destroy(redContactMesh);
        if(redShadow!=null)Destroy(redShadow);if(redShadowMesh!=null)Destroy(redShadowMesh);if(redShadowMaterial!=null)Destroy(redShadowMaterial);
        RedShowStick(true);
        if(playerHealth!=null)
        {
            if(Mathf.Approximately(playerHealth.NarrativeHealthFloor,TutorialHealthFloor))playerHealth.NarrativeHealthFloor=redPreviousHealthFloor;
            if(playerHealth.NarrativeDefeatHandler==ProtectStoryFight||playerHealth.NarrativeDefeatHandler==BeginDefeat)
                playerHealth.NarrativeDefeatHandler=redPreviousDefeatHandler;
        }
        if(redDirector!=null){redDirector.owner=null;Destroy(redDirector);}
        if(controls!=null)controls.enabled=false;
        redesignManual=false;RedesignActive=false;CurrentPhase=Phase.Finished;RedesignStage=OpeningStage.Finished;
        redDisposing=false;
    }
    void RedReleaseCatchupBudget()
    {
        if(!redOwnsMaximumDelta)return;
        if(Mathf.Approximately(Time.maximumDeltaTime,redCrushMaximumDelta))Time.maximumDeltaTime=redPriorMaximumDelta;
        redOwnsMaximumDelta=false;
    }
}

/// <summary>One late owner keeps pose, weapon, contact and camera on the same cinematic clock.</summary>
[DefaultExecutionOrder(15000)]
public sealed class Story1OpeningDirector : MonoBehaviour
{
    public Story1WreckBattle owner;
    void LateUpdate()
    {
        if(owner==null||!owner.enabled)return;
        owner.RedesignTick();owner.RedesignPrepareLesson(Time.deltaTime);owner.RedResolveOpeningSolids();
    }
}

/// <summary>Releases a removed tutorial's pause after a later overlay has returned its saved zero scale.</summary>
[DefaultExecutionOrder(16000)]
public sealed class Story1OpeningPauseRelease : MonoBehaviour
{
    float resumeScale=1;
    public static void Request(float rate)
    {
        var go=new GameObject("Opening pause release");go.hideFlags=HideFlags.HideInHierarchy;
        var release=go.AddComponent<Story1OpeningPauseRelease>();release.resumeScale=Mathf.Max(.0001f,rate);
        if(Application.isPlaying)DontDestroyOnLoad(go);
    }
    void LateUpdate()
    {
        if(PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput)return;
        if(Time.timeScale<=0)Time.timeScale=resumeScale;
        Destroy(gameObject);
    }
}
