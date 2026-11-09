using Mavis;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Two-phase encounter using the normal player damage and dodge rules.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Health),typeof(CharacterController))]
public sealed class ZhuBaJieBoss : MonoBehaviour, IPerfectDodgeThreatSource
{
    public enum State { Idle, Chase, Windup, Attack, Recover, Dodge, Transform, Dead }
    public enum MoveKind { Charge, TailSweep, RakeCombo }
    public GameObject boarForm, humanForm;
    public float sightRange=35f, leashRange=85f, chaseSpeed=6.1f;
    public float chargeDamage=52f, tailDamage=42f, rakeDamage=48f;
    public State CurrentState { get; private set; }
    public MoveKind CurrentMove { get; private set; }
    public float ActionProgress { get; private set; }
    public bool HumanPhase { get; private set; }
    public bool FightActive { get; private set; }
    public bool DamageWindowActive { get; private set; }
    public int AttacksStarted { get; private set; }
    public int HitsLanded { get; private set; }
    public int DodgesPerformed { get; private set; }
    public Health Health => hp;

    const float TailReach=8.3f, TailArc=220f;
    internal const float TailHitPhase=.46f;
    internal float TailAimYaw { get; private set; }
    const float ChargeRange=24f;
    Health hp; CharacterController motor; PlayerHealth target; SahurAttack playerAttack; SahurRifleCombat rifle;
    BossHudPanel hud; ZhuBaJieBossVisual visual; Vector3 home,committed,dodgeDirection;
    Vector3 sweepFacing;
    float timer,vertical,previousPhase,nextDodge,deathAt;
    int dodgeSideIndex,hitMask;
    bool dead,sweepNext;
    readonly RaycastHit[] hits=new RaycastHit[32];

    void Awake()
    {
        hp=GetComponent<Health>();motor=GetComponent<CharacterController>();visual=GetComponent<ZhuBaJieBossVisual>();
        if(boarForm)
        {
            var ownSolid=boarForm.transform.Find("BodySolid_Beast");
            if(ownSolid && ownSolid.TryGetComponent<Collider>(out var collider))Physics.IgnoreCollision(motor,collider,true);
        }
        hp.OnDamaged ??=new UnityEvent<float>();hp.OnDeath ??=new UnityEvent();
        hp.OnDamaged.AddListener(OnDamaged);hp.OnDeath.AddListener(OnDeath);
        CombatBodyCollision.Ensure(gameObject);PerfectDodgeThreats.Register(this);
        var response=GetComponent<CombatImpactResponse>();if(!response)response=gameObject.AddComponent<CombatImpactResponse>();
        response.unflinching=true;response.knockbackMultiplier=0;response.stopMultiplier=0;
        home=transform.position;hud=new BossHudPanel(transform,"Zhu Bajie Boss HUD");
        if(boarForm)boarForm.SetActive(true);if(humanForm)humanForm.SetActive(false);
    }
    void Start()
    {
        if(FindGround(transform.position,out var point)){motor.enabled=false;transform.position=point+Vector3.up*.05f;motor.enabled=true;}
        home=transform.position;
        SecondIslandRoninCamp.Build(this);
        DriftingPathShortcut.Build(this);
    }
    void Update()
    {
        float dt=Time.deltaTime;if(dt<=0 || PauseSettingsMenu.IsOpen)return;
        if(hp.IsDead)
        {
            if(!dead)OnDeath();
            if(Time.time-deathAt>=4f){if(boarForm && boarForm.activeSelf)boarForm.SetActive(false);if(humanForm && humanForm.activeSelf)humanForm.SetActive(false);}
            return;
        }
        if(!target || target.currentHealth<=0)
        {target=FindFirstObjectByType<PlayerHealth>();playerAttack=target?target.GetComponent<SahurAttack>():null;rifle=target?target.GetComponent<SahurRifleCombat>():null;}
        // Start protection prevents an unsolicited encounter, but striking
        // the boss is an explicit engagement even if Sahur has not walked.
        // The editor test station remains a protected area at all times.
        bool protectedAtStart=target && SecondIslandStartProtection.Protects(target);
        bool protectedAtTestStation=false;
#if UNITY_EDITOR
        protectedAtTestStation=target && SecondIslandTestStation.Protects(target);
#endif
        if(!target || target.currentHealth<=0 || protectedAtTestStation || protectedAtStart && !FightActive)
        {FightActive=false;if(CurrentState!=State.Transform)ReturnHome(dt);return;}
        Vector3 delta=Vector3.ProjectOnPlane(target.transform.position-transform.position,Vector3.up);
        float distance=delta.magnitude,fromHome=Vector3.ProjectOnPlane(transform.position-home,Vector3.up).magnitude;
        if(!FightActive && distance<sightRange && fromHome<leashRange){FightActive=true;CurrentState=State.Chase;timer=.55f;}
        if(FightActive && (distance>leashRange || fromHome>leashRange)){FightActive=false;DamageWindowActive=false;CurrentState=State.Idle;}
        if(!FightActive){ReturnHome(dt);return;}
        timer=Mathf.Max(0,timer-dt);
        switch(CurrentState)
        {
            case State.Idle: case State.Chase:
                if(TryDodge(distance,delta))break;
                // After a charge, close the gap on foot so retreating beyond
                // the sweep radius cannot force an endless sequence of charges.
                if(!HumanPhase && timer<=0 && distance<=TailReach && (sweepNext || AttacksStarted==0) &&
                   TryBeginAttack(MoveKind.TailSweep))break;
                if(delta.sqrMagnitude>.01f)Face(delta,dt);
                float stop=HumanPhase?5f:6.5f;
                float speed=!HumanPhase && sweepNext?chaseSpeed+1.8f:chaseSpeed;
                Move(distance>stop?delta.normalized*speed:Vector3.zero,dt);
                CurrentState=State.Chase;
                if(timer<=0 && distance<ChargeRange)
                {
                    if(HumanPhase)TryBeginAttack(MoveKind.RakeCombo);
                    else if(!sweepNext)TryBeginAttack(MoveKind.Charge);
                }
                break;
            case State.Windup:
                if(CurrentMove==MoveKind.TailSweep)Face(sweepFacing,dt);
                else if(ActionProgress<.5f && delta.sqrMagnitude>.01f)Face(delta,dt);
                Move(Vector3.zero,dt);ActionProgress=1f-timer/Windup(CurrentMove);
                if(timer<=0){CurrentState=State.Attack;committed=transform.forward;timer=Duration(CurrentMove);previousPhase=0;hitMask=0;ActionProgress=0;}
                break;
            case State.Attack: TickAttack(dt);break;
            case State.Recover:
                Move(Vector3.zero,dt);ActionProgress=1f-timer/Recovery(CurrentMove);
                if(timer<=0){CurrentState=State.Chase;timer=.2f;}break;
            case State.Dodge:
                Move(GroundAhead(dodgeDirection)?dodgeDirection*9f:Vector3.zero,dt);
                ActionProgress=1f-timer/.48f;if(timer<=0){CurrentState=State.Chase;timer=.28f;}break;
            case State.Transform:
                Move(Vector3.zero,dt);ActionProgress=1f-timer/1.5f;
                if(timer<=.7f && !HumanPhase)
                {
                    HumanPhase=true;if(boarForm)boarForm.SetActive(false);if(humanForm)humanForm.SetActive(true);visual?.SetHuman();
                    BossPhaseVfx.For(gameObject).Burst(transform.position+Vector3.up*2,true);
                }
                if(timer<=0){CurrentState=State.Recover;CurrentMove=MoveKind.RakeCombo;timer=.75f;}break;
        }
    }
    bool TryDodge(float distance,Vector3 delta)
    {
        if(Time.time<nextDodge || distance>24f || timer>.3f || !target)return false;
        Vector3 facing=Vector3.ProjectOnPlane(target.transform.forward,Vector3.up).normalized;
        bool aimedAtBoss=delta.sqrMagnitude>.01f && Vector3.Dot(facing,-delta.normalized)>.45f;
        bool threatened=aimedAtBoss && distance<13f && playerAttack &&
            (playerAttack.IsGroundComboActive || playerAttack.IsHeavyAttackActive || playerAttack.IsJumpSlashActive || playerAttack.IsCharging && playerAttack.Charge01>.65f);
        threatened |= aimedAtBoss && rifle && (rifle.IsAiming || rifle.FirePoseActive);
        if(!threatened)return false;
        var side=Vector3.Cross(Vector3.up,delta.normalized)*(dodgeSideIndex++%2==0?1:-1);
        if(!GroundAhead(side))side=-side;if(!GroundAhead(side))return false;
        dodgeDirection=(side-delta.normalized*.2f).normalized;nextDodge=Time.time+(HumanPhase?3.2f:4.2f);
        CurrentState=State.Dodge;timer=.48f;ActionProgress=0;DodgesPerformed++;return true;
    }
    public bool TryBeginAttack(MoveKind move)
    {
        if(!FightActive || !target || target.currentHealth<=0 || hp.IsDead || CurrentState!=State.Chase && CurrentState!=State.Idle)return false;
        if(HumanPhase!=(move==MoveKind.RakeCombo))return false;
        if(!HumanPhase)sweepNext=move==MoveKind.Charge;
        if(move==MoveKind.TailSweep)
        {
            sweepFacing=transform.forward;
            var towardTarget=Vector3.ProjectOnPlane(target.transform.position-transform.position,Vector3.up);
            if(towardTarget.sqrMagnitude>.01f && Vector3.Angle(-sweepFacing,towardTarget)>TailArc*.5f-15f)
            {
                float side=Vector3.SignedAngle(towardTarget,sweepFacing,Vector3.up)<0?-1f:1f;
                sweepFacing=Quaternion.AngleAxis(120f*side,Vector3.up)*towardTarget.normalized;
            }
            TailAimYaw=Vector3.SignedAngle(-sweepFacing,towardTarget,Vector3.up);
        }
        CurrentMove=move;CurrentState=State.Windup;timer=Windup(move);ActionProgress=0;DamageWindowActive=false;hitMask=0;AttacksStarted++;
        visual?.BeginAttack(move);return true;
    }
    void TickAttack(float dt)
    {
        float phase=1f-Mathf.Clamp01(timer/Duration(CurrentMove));ActionProgress=phase;
        DamageWindowActive=CurrentMove==MoveKind.Charge?phase>=.08f&&phase<=.83f:CurrentMove==MoveKind.TailSweep?phase>=.35f&&phase<=.58f:phase>=.18f&&phase<=.86f;
        if(CurrentMove==MoveKind.Charge && phase<.8f && GroundAhead(committed))Move(committed*15f,dt);else Move(Vector3.zero,dt);
        if(CurrentMove==MoveKind.Charge && DamageWindowActive)TryDamage(0,7f,115f,chargeDamage,committed);
        if(CurrentMove==MoveKind.TailSweep && previousPhase<TailHitPhase && phase>=TailHitPhase)
            TryDamage(0,TailReach,TailArc,tailDamage,-committed);
        if(CurrentMove==MoveKind.RakeCombo)
        {
            if(previousPhase<.22f && phase>=.22f)TryDamage(0,8f,135f,rakeDamage,committed);
            if(previousPhase<.82f && phase>=.82f)TryDamage(1,8.5f,145f,rakeDamage+5f,committed);
        }
        previousPhase=phase;
        if(timer<=0){CurrentState=State.Recover;timer=Recovery(CurrentMove);DamageWindowActive=false;ActionProgress=0;}
    }
    void TryDamage(int index,float reach,float arc,float damage,Vector3 direction)
    {
        if((hitMask&(1<<index))!=0 || !target || target.currentHealth<=0)return;
        var body=target.GetComponent<CharacterController>();Vector3 d=Vector3.ProjectOnPlane(target.transform.position-transform.position,Vector3.up);
        float extra=body?body.radius*Mathf.Abs(body.transform.lossyScale.x):0;
        if(d.magnitude>reach+extra || d.sqrMagnitude>.001f && Vector3.Angle(direction,d)>arc*.5f)return;
        float feet=body?body.bounds.min.y:target.transform.position.y;
        if(Mathf.Abs(feet-transform.position.y)>4.5f || !PerfectDodgeThreats.ClearPath(target.transform,transform,transform.position+Vector3.up*2,body?body.bounds.center:target.transform.position+Vector3.up))return;
        hitMask|=1<<index;if(target.ApplyAttackDamage(damage,body?body.bounds.center:target.transform.position,transform.position))HitsLanded++;
    }
    void OnDamaged(float damage)
    {
        if(hp.IsDead)return;
        if(!FightActive){target=FindFirstObjectByType<PlayerHealth>();playerAttack=target?target.GetComponent<SahurAttack>():null;rifle=target?target.GetComponent<SahurRifleCombat>():null;FightActive=target && target.currentHealth>0;}
        if(!HumanPhase && CurrentState!=State.Transform && hp.Ratio<=.5f)
        {CurrentState=State.Transform;timer=1.5f;DamageWindowActive=false;ActionProgress=0;visual?.BeginTransform();BossPhaseVfx.For(gameObject).Burst(transform.position+Vector3.up*2);}
        else if(CurrentState==State.Idle){CurrentState=State.Chase;timer=.25f;}
    }
    void OnDeath()
    {
        if(dead)return;dead=true;FightActive=false;CurrentState=State.Dead;DamageWindowActive=false;deathAt=Time.time;
        foreach(var collider in GetComponentsInChildren<Collider>(true))collider.enabled=false;
        visual?.Die();
        if (FindFirstObjectByType<DriftingPathShortcut>()) FindFirstObjectByType<IslandActivityGuide>()?.ShowRidgeVictory();
        GameSaveManager.RequestAutosave();
    }
    public void RestoreDefeated()
    {
        if(!hp)hp=GetComponent<Health>();hp.currentHealth=0;dead=true;FightActive=false;CurrentState=State.Dead;DamageWindowActive=false;
        foreach(var collider in GetComponentsInChildren<Collider>(true))collider.enabled=false;
        if(boarForm)boarForm.SetActive(false);if(humanForm)humanForm.SetActive(false);hud?.Hide();
    }
    void LateUpdate()
    {
        if(hud==null || !hp)return;var cam=Camera.main;
        hud.Refresh(cam && (FightActive || dead && Time.time-deathAt<3f),cam?(transform.position-cam.transform.position).sqrMagnitude:float.PositiveInfinity,
            hp.Ratio,hp.currentHealth,hp.maxHealth,HumanPhase?"ZHU BAJIE — HUMAN FORM":"ZHU BAJIE — WILD BOAR",Time.deltaTime);
    }
    void OnDestroy(){if(hp){hp.OnDamaged?.RemoveListener(OnDamaged);hp.OnDeath?.RemoveListener(OnDeath);}hud?.Dispose();}
    void ReturnHome(float dt)
    {
        var d=Vector3.ProjectOnPlane(home-transform.position,Vector3.up);
        if(d.magnitude>.5f){Face(d,dt);Move(GroundAhead(d.normalized)?d.normalized*chaseSpeed:Vector3.zero,dt);}else Move(Vector3.zero,dt);
    }
    void Face(Vector3 d,float dt){if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),240f*dt);}
    void Move(Vector3 v,float dt){if(!motor || !motor.enabled)return;vertical=motor.isGrounded?-2f:Mathf.Max(-30,vertical-28*dt);motor.Move((v+Vector3.up*vertical)*dt);}
    bool GroundAhead(Vector3 d)=>FindGround(transform.position+d*2f,out var g) && g.y>=transform.position.y-2.5f;
    bool FindGround(Vector3 at,out Vector3 point)
    {
        point=default;float best=float.PositiveInfinity;int count=Physics.RaycastNonAlloc(at+Vector3.up*8,Vector3.down,hits,25,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++){var h=hits[i];if(!h.collider || h.transform.IsChildOf(transform) || CombatBodyCollision.IsActor(h.collider) || h.normal.y<.6f || h.distance>=best)continue;best=h.distance;point=h.point;}
        return best<float.PositiveInfinity;
    }
    static float Windup(MoveKind k)=>k==MoveKind.Charge?.9f:k==MoveKind.TailSweep?.85f:.78f;
    static float Duration(MoveKind k)=>k==MoveKind.Charge?1.25f:k==MoveKind.TailSweep?.8f:1.25f;
    static float Recovery(MoveKind k)=>k==MoveKind.Charge?1.0f:k==MoveKind.TailSweep?.85f:.9f;
    public bool ForecastDodge(PlayerHealth player,float horizon,out PerfectDodgeThreat threat)
    {
        threat=default;if(!FightActive || hp.IsDead || CurrentState!=State.Windup && CurrentState!=State.Attack)return false;
        bool wind=CurrentState==State.Windup;int index=CurrentMove==MoveKind.RakeCombo && (hitMask&1)!=0?1:0;if((hitMask&(1<<index))!=0)return false;
        float cue=CurrentMove==MoveKind.Charge?.08f:CurrentMove==MoveKind.TailSweep?TailHitPhase:index==0?.22f:.82f;
        float seconds=wind?timer+Duration(CurrentMove)*cue:Mathf.Max(0,timer-Duration(CurrentMove)*(1-cue));
        Vector3 direction=wind?transform.forward:committed;
        if(CurrentMove==MoveKind.TailSweep)direction=wind?-sweepFacing:-committed;
        float reach=CurrentMove==MoveKind.Charge?7f:CurrentMove==MoveKind.TailSweep?TailReach:8.5f;
        return PerfectDodgeThreat.Sector(this,transform,player,seconds,horizon,transform.position,direction,reach,CurrentMove==MoveKind.TailSweep?TailArc:135f,4.5f,((long)GetInstanceID()<<32)^(uint)(AttacksStarted*17+index),out threat);
    }
}
