using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Visible pursuit during admission; the fight begins after a real swim and climb.</summary>
public sealed class Story1WreckChaseEntry:MonoBehaviour
{
    public float Age { get; private set; }
    public bool DepartedDeck { get; private set; }
    public float SwimDistance { get; private set; }
    public bool StartsOnDeck=>startsOnDeck;
    public Vector3 Direction { get; private set; }
    Story1WreckBattle battle;ThirdPersonPlayerController hero;TralaleroSwimAnimator shark;OceanWorld ocean;Camera camera;
    PlayableGraph graph;AnimationClipPlayable swimPose,jumpPose;AnimationMixerPlayable mixer;
    RuntimeAnimatorController sharkController;AnimatorOverrideController chaseController;
    AnimationClip chaseClip;Vector3 startRoot,startDirection,velocity,cameraVelocity;Quaternion startRotation;
    float jumpLength,swimLength;bool startsOnDeck,heroReleased,finished;int lastFrame=-1;
    Vector3 renderedRoot;Quaternion renderedHeading;Story1WreckPlank support;
    int lastSharkFrame=-1;
    float pursuitSpeed,pursuitPhase,pursuitHeave,swimHeave;
    public float SwimDepth { get; private set; }
    readonly RaycastHit[] hits=new RaycastHit[64];
    float Water(Vector3 at)=>ocean!=null?ocean.SampleSurfaceHeight(at,Time.time):hero.seaLevel;
    static float Ease(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
    public void Initialize(Story1WreckBattle encounter,ThirdPersonPlayerController actor,TralaleroSwimAnimator enemy,Camera view,OceanWorld water,bool onDeck,AnimationClip fallbackSwim,AnimationClip departure)
    {
        battle=encounter;hero=actor;shark=enemy;camera=view;ocean=water;startsOnDeck=onDeck;
        SwimDepth=Mathf.Clamp(shark.SurfaceRootY(Water(shark.transform.position),shark.transform.rotation)-shark.transform.position.y,0,8);
        startRoot=actor.transform.position;startRotation=actor.transform.rotation;
        renderedRoot=startRoot;renderedHeading=startRotation;support=battle.EntryDeck;
        var target=battle.EntryDeck;Vector3 destination=target!=null?target.BoardingPoint(startRoot):startRoot+actor.transform.forward*8;
        startDirection=Vector3.ProjectOnPlane(destination-startRoot,Vector3.up).normalized;
        if(startDirection.sqrMagnitude<.01f)startDirection=actor.transform.forward;
        if(onDeck)startDirection=Vector3.ProjectOnPlane(actor.transform.forward,Vector3.up).normalized;
        Direction=startDirection;
        var swim=Story1EncounterClipCache.Find("PLAYER_PursuitSwim_v210")??fallbackSwim;
        graph=PlayableGraph.Create("Sahur pursuit admission");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        mixer=AnimationMixerPlayable.Create(graph,2);swimPose=AnimationClipPlayable.Create(graph,swim);swimPose.SetSpeed(0);swimLength=swim.length;
        jumpPose=AnimationClipPlayable.Create(graph,departure);jumpPose.SetSpeed(0);jumpLength=departure.length;
        mixer.ConnectInput(0,swimPose,0);mixer.ConnectInput(1,jumpPose,0);
        var output=AnimationPlayableOutput.Create(graph,"Pursuit visible pose",hero.CharacterAnimator);output.SetSourcePlayable(mixer);graph.Play();
        chaseClip=Story1EncounterClipCache.Find("CH1_SharkPursuit_v210");
        if(chaseClip!=null && shark.BoneAnimator!=null)
        {
            sharkController=shark.BoneAnimator.runtimeAnimatorController;
            chaseController=new AnimatorOverrideController(sharkController);
            var sources=sharkController.animationClips.Where(c=>c.name=="Swim_Cruise" || c.name=="Swim_Fast" || c.name=="Swim" || c.name.StartsWith("CH1_SharkSwim_")).ToArray();
            foreach(var source in sources)chaseController[source.name]=chaseClip;
            if(sources.Length>0)shark.BoneAnimator.runtimeAnimatorController=chaseController;
        }
    }
    public void RenderSearching()
    {
        if(finished || heroReleased)return;
        // Hypothetical support/contact samples must never become visible
        // displacement on the next chase frame.
        hero.transform.SetPositionAndRotation(renderedRoot,renderedHeading);
        bool advance=lastFrame!=Time.frameCount && !PauseSettingsMenu.IsOpen && Time.deltaTime>0;
        if(advance){lastFrame=Time.frameCount;Age+=Time.deltaTime;}
        float jumpWeight=startsOnDeck?1-Ease(Mathf.InverseLerp(.45f,1.1f,Age)):0;
        swimPose.SetTime(Mathf.Repeat(Age,swimLength));jumpPose.SetTime(Mathf.Min(jumpLength-.0001f,Age*jumpLength/1.1f));
        mixer.SetInputWeight(0,1-jumpWeight);mixer.SetInputWeight(1,jumpWeight);graph.Evaluate(0);
        if(advance)
        {
            Vector3 previous=hero.transform.position,position=previous;
            if(startsOnDeck && Age<1.1f)
            {
                float t=Mathf.Clamp01(Age/1.1f);
                position=startRoot+startDirection*(4.5f*Ease(t))+Vector3.up*(3.2f*Age-4.9f*Age*Age);
                hero.transform.rotation=Quaternion.Slerp(startRotation,Quaternion.LookRotation(startDirection),Ease(t));
                var head=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head);
                position+=Vector3.up*Mathf.Max(0,Water(head.position)+hero.swimHeadFreeboard-(head.position.y+position.y-previous.y))*Ease(Mathf.InverseLerp(.5f,1.1f,Age));
                DepartedDeck=Age>.8f;
            }
            else
            {
                var target=support;
                Vector3 destination=target!=null?target.BoardingPoint(previous):previous+Direction*8;
                if(target!=null)destination+=target.OpenWaterDirection(destination)*3;
                Vector3 desired=Vector3.ProjectOnPlane(destination-previous,Vector3.up);
                if(desired.magnitude>1)Direction=Steer(desired.normalized);
                float speed=Mathf.Min(hero.swimSpeed*hero.fastSwimMultiplier,Mathf.Max(1,desired.magnitude));
                velocity=Vector3.MoveTowards(velocity,Direction*speed,4*Time.deltaTime);position+=velocity*Time.deltaTime;
                hero.transform.rotation=Quaternion.Slerp(hero.transform.rotation,Quaternion.LookRotation(Direction),1-Mathf.Exp(-5*Time.deltaTime));
                var head=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head);
                float targetY=previous.y+Water(head.position)+hero.swimHeadFreeboard-head.position.y;
                position.y=Mathf.SmoothDamp(previous.y,targetY,ref swimHeave,.12f,Mathf.Infinity,Time.deltaTime);
                position.y=Mathf.Max(position.y,targetY-.05f);
                DepartedDeck=true;
            }
            Vector3 horizontal=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(position-previous,Vector3.up),10*Time.deltaTime);
            hero.transform.position=previous+horizontal+Vector3.up*(position.y-previous.y);
            renderedRoot=hero.transform.position;renderedHeading=hero.transform.rotation;
            SwimDistance+=Vector3.ProjectOnPlane(hero.transform.position-previous,Vector3.up).magnitude;
        }
        hero.GetComponent<SahurSwimmingWeapon>()?.RefreshPose();
        Pursue(Age);
        if(advance)
        {
            Vector3 focus=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Chest).position;
            Vector3 desired=focus-Direction*13+Vector3.Cross(Vector3.up,Direction)*7+Vector3.up*6;
            desired.y=Mathf.Max(desired.y,Water(desired)+3);
            camera.transform.position=Vector3.SmoothDamp(camera.transform.position,desired,ref cameraVelocity,.65f);
            camera.transform.rotation=Quaternion.Slerp(camera.transform.rotation,Quaternion.LookRotation(focus-camera.transform.position),1-Mathf.Exp(-4*Time.deltaTime));
        }
    }
    public void SetSupport(Story1WreckPlank target){support=target;}
    Vector3 Steer(Vector3 desired)
    {
        Vector3 at=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head).position;
        int count=Physics.SphereCastNonAlloc(at,.45f,desired,hits,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=hits[i];if(hit.collider==null || hit.collider.transform.IsChildOf(hero.transform) || hit.collider.transform.IsChildOf(shark.transform))continue;
            Vector3 normal=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
            Vector3 side=Vector3.Cross(Vector3.up,desired);
            if(normal.sqrMagnitude>.1f && Vector3.Dot(side,normal)<0)side=-side;
            return (desired*.25f+side).normalized;
        }
        return desired;
    }
    public void ReleaseHeroPose(){if(heroReleased)return;heroReleased=true;if(graph.IsValid())graph.Destroy();}
    public void Pursue(float time)
    {
        if(finished || shark==null || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        shark.SampleCinematicAction("Swim",chaseClip!=null?pursuitPhase*1.6f/chaseClip.length:time);
        if(lastSharkFrame==Time.frameCount){shark.CommitCinematicPose(time);return;}
        lastSharkFrame=Time.frameCount;
        Vector3 forward=Vector3.ProjectOnPlane(hero.transform.forward,Vector3.up).normalized;
        Vector3 target=hero.transform.position-forward*12+Vector3.Cross(Vector3.up,forward)*2;
        target.y=Water(target);
        Vector3 centre=Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
        target.y=shark.SurfaceRootY(target.y,shark.transform.rotation)+(shark.transform.rotation*centre).y;
        AdvanceSwimming(target,shark.transform.rotation,false,time);
    }
    public void SwimToRoot(Vector3 root,Quaternion destinationHeading,float time,bool finalApproach)
    {
        if(finished || shark==null || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        shark.SampleCinematicAction("Swim",chaseClip!=null?pursuitPhase*1.6f/chaseClip.length:time);
        if(lastSharkFrame==Time.frameCount){shark.CommitCinematicPose(time);return;}
        lastSharkFrame=Time.frameCount;
        Vector3 centre=Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
        AdvanceSwimming(root+destinationHeading*centre,destinationHeading,finalApproach,time);
    }
    public void SwimInFilmToRoot(Vector3 root,Quaternion heading,float clock)
    {
        if(shark==null || PauseSettingsMenu.IsOpen || Time.deltaTime<=0)return;
        if(shark.BoneAnimator!=null)shark.BoneAnimator.SetFloat("SwimEffort",Mathf.Clamp01(pursuitSpeed/15),.2f,Time.deltaTime);
        shark.SampleCinematicAction("Swim",pursuitPhase);
        if(lastSharkFrame==Time.frameCount){shark.CommitCinematicPose(clock);return;}
        lastSharkFrame=Time.frameCount;
        Vector3 centre=Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
        AdvanceSwimming(root+heading*centre,heading,true,clock);
    }
    void AdvanceSwimming(Vector3 targetBody,Quaternion destinationHeading,bool finalApproach,float time)
    {
        float dt=Time.deltaTime;
        Vector3 centre=Vector3.Scale(shark.RootMeshBounds.center,shark.transform.lossyScale);
        Vector3 body=shark.transform.position+shark.transform.rotation*centre,previousBody=body;
        Vector3 travel=Vector3.ProjectOnPlane(targetBody-body,Vector3.up);
        Quaternion desired=travel.sqrMagnitude>.01f?Quaternion.LookRotation(travel):shark.transform.rotation;
        bool settling=finalApproach && travel.magnitude<.4f;
        if(settling)desired=Quaternion.LookRotation(Vector3.ProjectOnPlane(destinationHeading*Vector3.forward,Vector3.up));
        Quaternion heading=Quaternion.RotateTowards(shark.transform.rotation,desired,90*dt);
        Vector3 forward=Vector3.ProjectOnPlane(heading*Vector3.forward,Vector3.up).normalized;
        float alignment=Mathf.Max(0,Vector3.Dot(forward,travel.normalized));
        float desiredSpeed=settling?0:Mathf.Min(18,travel.magnitude*1.2f)*Mathf.Lerp(.2f,1,alignment);
        pursuitSpeed=Mathf.MoveTowards(pursuitSpeed,desiredSpeed,8*dt);
        body+=forward*Mathf.Min(travel.magnitude,pursuitSpeed*dt);
        body.y=Mathf.SmoothDamp(body.y,targetBody.y-SwimDepth,ref pursuitHeave,.22f,Mathf.Infinity,dt);
        pursuitPhase+=dt*Mathf.Lerp(.55f,1.6f,Mathf.Clamp01(pursuitSpeed/18));
        // Turn about the body, not the offset imported root or a pinned nose.
        // This makes the visible torso travel forward through a real arc.
        shark.transform.SetPositionAndRotation(body-heading*centre,heading);
        shark.CommitCinematicPose(time);battle.WreckContact.ResolvePose();shark.RecordResolvedCinematicPose();
        Vector3 actualBody=shark.transform.position+shark.transform.rotation*centre;
        if(!settling && travel.magnitude>3 && pursuitSpeed>2 &&
            Vector3.ProjectOnPlane(actualBody-previousBody,Vector3.up).magnitude<pursuitSpeed*dt*.25f)
            SwimDepth=Mathf.Min(8,SwimDepth+2*dt);
    }
    public void Finish()
    {
        if(finished)return;finished=true;ReleaseHeroPose();
        if(sharkController!=null && shark.BoneAnimator!=null)shark.BoneAnimator.runtimeAnimatorController=sharkController;
        if(chaseController!=null)Destroy(chaseController);
    }
    void OnDestroy(){Finish();}
}
