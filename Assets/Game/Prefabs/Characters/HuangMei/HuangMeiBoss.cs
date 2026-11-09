using Mavis;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent, RequireComponent(typeof(Health),typeof(CharacterController))]
public sealed partial class HuangMeiBoss : MonoBehaviour, IPerfectDodgeThreatSource
{
    public enum State { Idle, Pursuit, Windup, Strike, Recover, Invocation, Dead }
    public enum Attack { StaffSweep, StaffSlam, PalmSeal, GoldenMandala }
    public State CurrentState { get; private set; }
    public Attack CurrentAttack { get; private set; }
    public float Progress { get; private set; }
    public float Moving { get; private set; }
    public bool FightActive { get; private set; }
    public bool SecondPhase { get; private set; }
    public int HitsLanded { get; private set; }
    public Health Health { get; private set; }
    public float ActionElapsed => duration-timer;

    [Min(12)] public float sightRange=32f, leashRange=53f;
    [Min(1)] public float moveSpeed=4.3f;
    CharacterController motor, playerBody;
    PlayerHealth player;
    BossHudPanel hud;
    HuangMeiRig rig;
    HuangMeiSealEffects effects;
    Vector3 home, committed, sealCenter;
    Quaternion homeRotation;
    float vertical, timer, duration, previousRadius, deadAt;
    int combo, hitMask, attackSerial;
    bool pulseHit, retry;
    Vector3 palmPosition;
    float palmDistance;
    bool palmFlying;
    readonly RaycastHit[] hits=new RaycastHit[24];

    void Awake()
    {
        Health=GetComponent<Health>();motor=GetComponent<CharacterController>();
        rig=GetComponent<HuangMeiRig>();
        home=transform.position;homeRotation=transform.rotation;
        Health.OnDeath ??= new UnityEvent();Health.OnDamaged ??= new UnityEvent<float>();
        Health.OnDeath.AddListener(Defeat);Health.OnDamaged.AddListener(WakeOnHit);
        hud=new BossHudPanel(transform,"Huang Mei Boss HUD");
        CombatBodyCollision.Ensure(gameObject);
        PerfectDodgeThreats.Register(this);
        var response=GetComponent<CombatImpactResponse>();
        if(!response)response=gameObject.AddComponent<CombatImpactResponse>();
        response.unflinching=true;response.knockbackMultiplier=0;response.stopMultiplier=0;
        effects=new HuangMeiSealEffects(transform,rig?rig.staffTip:null,GroundAt);
    }

    void Start()
    {
        if(TryGround(transform.position,out var floor))
        {
            motor.enabled=false;
            transform.position=floor+Vector3.up*.04f;
            motor.enabled=true;
            home=transform.position;
        }
    }

    void FindPlayer()
    {
        player=FindFirstObjectByType<PlayerHealth>();
        playerBody=player ? player.GetComponent<CharacterController>() : null;
    }

    void Update()
    {
        if(Health.IsDead){if(CurrentState!=State.Dead)Defeat();return;}
        if(FightActive && player && player.currentHealth<=0)retry=true;
        float dt=Time.deltaTime;
        if(dt<=0 || PauseSettingsMenu.IsOpen)return;
        if(!player)FindPlayer();
        if(!player || !playerBody)return;
        if(retry || FightActive && (Flat(player.transform.position-home).magnitude>leashRange || Flat(transform.position-home).magnitude>leashRange))
        {
            ResetEncounter();
            return;
        }
        var toward=Flat(playerBody.bounds.center-transform.position);
        float distance=toward.magnitude;
        float height=playerBody.bounds.min.y-transform.position.y;
        if(!FightActive && player.currentHealth>0 && playerBody.enabled && distance<sightRange && Mathf.Abs(height)<8f)
            Engage();
        if(!FightActive){Move(Vector3.zero,dt);return;}
        if(!SecondPhase && Health.Ratio<=.5f && (CurrentState==State.Pursuit || CurrentState==State.Recover))
        {
            SecondPhase=true;
            Enter(State.Invocation,.86f);
            effects.RingBell(transform.position+Vector3.up*4);
        }
        timer=Mathf.Max(0,timer-dt);
        Progress=duration>0?1-timer/duration:0;
        switch(CurrentState)
        {
            case State.Pursuit:
                Face(toward,dt);
                Move(distance>7.1f?toward.normalized*moveSpeed*(SecondPhase?1.23f:1):Vector3.zero,dt);
                if(timer<=0 && SecondPhase && Time.time>=nextMandala && distance<25f && Mathf.Abs(height)<7f)BeginAttack(Attack.GoldenMandala);
                else if(timer<=0 && distance<13.4f && Mathf.Abs(height)<6)BeginAttack((Attack)(combo++%3));
                break;
            case State.Windup:
                if(CurrentAttack!=Attack.GoldenMandala && Progress<.46f)Face(toward,dt);
                Move(Vector3.zero,dt);
                Telegraph();
                if(timer<=0)
                {
                    committed=transform.forward;
                    Enter(State.Strike,StrikeDuration(CurrentAttack));
                }
                break;
            case State.Strike:
                if(CurrentAttack==Attack.StaffSweep && (Progress<.34f || Progress>.63f && Progress<.85f))Move(committed*3.9f,dt);
                else Move(Vector3.zero,dt);
                Strike(dt);
                if(timer<=0)Enter(State.Recover,CurrentAttack==Attack.GoldenMandala?1.65f:SecondPhase?.58f:.86f);
                break;
            case State.Recover:
                Move(Vector3.zero,dt);
                if(timer<=0)Enter(State.Pursuit,SecondPhase?.24f:.45f);
                break;
            case State.Invocation:
                Move(Vector3.zero,dt);
                DrawMandala(Mathf.Lerp(2.7f,5.8f,Progress),Progress);
                if(timer<=0)BeginAttack(Attack.GoldenMandala);
                break;
        }
    }

    public const float FirstSweep=.33f, SecondSweep=.84f, PalmRelease=.36f;
    const float PalmSpeed=23f, PalmRadius=1.55f, PalmRange=16f;
    float SlamRadius=>SecondPhase?10.2f:8.6f;
    public static float ImpactPhase(Attack kind)=>kind==Attack.StaffSlam?.37f:kind==Attack.StaffSweep?FirstSweep:PalmRelease;
    static float StrikeDuration(Attack kind)=>kind==Attack.GoldenMandala?4.18f:kind==Attack.StaffSweep?1.48f:1.25f;
    void Telegraph()
    {
        if(CurrentAttack==Attack.GoldenMandala)DrawMandala(Mathf.Lerp(4.2f,6.25f,Progress),Progress);
        else if(CurrentAttack==Attack.StaffSlam)
        {
            sealCenter=GroundAt(transform.TransformPoint(new Vector3(1.1f,0,4.5f)));
            effects.Ground(sealCenter,0,SlamRadius,Progress*.62f,false);
        }
        else if(CurrentAttack==Attack.PalmSeal && Progress>.22f)
            effects.Bolt(PalmOrigin()+transform.forward*.3f,transform.forward,Mathf.Lerp(.24f,1.16f,Progress));
    }

    void Strike(float dt)
    {
        switch(CurrentAttack)
        {
            case Attack.StaffSweep:
                effects.Trail(Progress>.14f && Progress<.45f || Progress>.65f && Progress<.94f);
                Sweep(0,FirstSweep,9.7f,120f,23);
                Sweep(1,SecondSweep,10.2f,158f,27);
                break;
            case Attack.StaffSlam: Slam(); break;
            case Attack.PalmSeal: Palm(dt); break;
            case Attack.GoldenMandala: TickMandala(); break;
        }
    }

    void Sweep(int index,float beat,float range,float angle,float damage)
    {
        if(!Spend(index,beat))return;
        GameplayAudio.Play(GameplayAudio.Cue.Swing,rig && rig.staffTip ? rig.staffTip.position : transform.position);
        Arc(range,angle,damage);
    }

    bool Spend(int index,float beat)
    {
        int bit=1<<index;
        if(Progress<beat || (hitMask&bit)!=0)return false;
        hitMask|=bit;
        return true;
    }

    void Slam()
    {
        float impact=ImpactPhase(Attack.StaffSlam);
        effects.Trail(Progress>.16f && Progress<impact+.10f);
        if(Progress<impact)
        {
            effects.Ground(sealCenter,0,SlamRadius,.78f,false);
            return;
        }
        if(Spend(0,impact))effects.Impact(sealCenter,30);
        float phase=Mathf.InverseLerp(impact,1,Progress);
        float radius=Mathf.Lerp(.45f,SlamRadius,phase);
        effects.Ground(sealCenter,Mathf.Max(0,radius-.64f),radius,phase,true);
        if(!pulseHit && InGroundSeal(sealCenter,previousRadius,radius,1.30f))
        {
            pulseHit=true;
            Damage(34,sealCenter);
        }
        previousRadius=radius;
    }

    Vector3 PalmOrigin()=>rig && rig.LeftPalm ? rig.LeftPalm.position : transform.position+Vector3.up*4.6f;
    void Palm(float dt)
    {
        if(Spend(0,PalmRelease))
        {
            palmPosition=PalmOrigin()+committed*.64f;
            palmDistance=0;palmFlying=true;
            GameplayAudio.Play(GameplayAudio.Cue.Swing,palmPosition);
        }
        if(!palmFlying)return;
        float step=Mathf.Min(PalmRange-palmDistance,PalmSpeed*dt);
        bool playerContact=playerBody.bounds.SqrDistance(palmPosition)<=PalmRadius*PalmRadius;
        float nearest=playerContact?0:float.PositiveInfinity;
        int count=Physics.SphereCastNonAlloc(palmPosition,PalmRadius,committed,hits,step,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=hits[i];
            if(!hit.collider || hit.transform.IsChildOf(transform) || hit.distance>=nearest)continue;
            bool isPlayer=hit.collider==playerBody || hit.transform.IsChildOf(player.transform);
            if(!isPlayer && CombatBodyCollision.IsActor(hit.collider))continue;
            nearest=hit.distance;playerContact=isPlayer;
        }
        if(nearest<float.PositiveInfinity || step<=.001f)
        {
            if(playerContact)Damage(30,palmPosition+committed*nearest,false);
            effects.Impact(palmPosition+committed*Mathf.Min(step,nearest),13);
            effects.HideBolt();palmFlying=false;
            return;
        }
        palmPosition+=committed*step;palmDistance+=step;
        effects.Bolt(palmPosition,committed,PalmRadius);
    }

    void Arc(float range,float angle,float damage)
    {
        Vector3 direction=Flat(playerBody.bounds.center-transform.position);
        float radius=playerBody.radius*Mathf.Abs(player.transform.lossyScale.x);
        if(direction.magnitude>range+radius || Vector3.Angle(committed,direction)>angle*.5f ||
            Mathf.Abs(playerBody.bounds.min.y-transform.position.y)>3.7f)return;
        Damage(damage,transform.position);
    }
    void Damage(float amount,Vector3 origin,bool cover=true)
    {
        Vector3 from=origin+Vector3.up*2.5f,delta=playerBody.bounds.center-from;
        int count=cover?Physics.RaycastNonAlloc(from,delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore):0;
        for(int i=0;i<count;i++)if(hits[i].collider && !hits[i].transform.IsChildOf(transform) &&
            !hits[i].transform.IsChildOf(player.transform) && !CombatBodyCollision.IsActor(hits[i].collider))return;
        if(player.ApplyAttackDamage(amount*(SecondPhase?1.25f:1),playerBody.bounds.center,origin))HitsLanded++;
    }

    void BeginAttack(Attack attack)
    {
        CurrentAttack=attack;
        attackSerial++;
        if(attack==Attack.GoldenMandala)
        {
            PrepareMandala();
            Enter(State.Windup,1.72f);
        }
        else
        {
            sealCenter=GroundAt(transform.TransformPoint(new Vector3(1.1f,0,4.5f)));
            float windup=attack==Attack.StaffSlam?1.22f:attack==Attack.StaffSweep?.83f:.92f;
            Enter(State.Windup,windup*(SecondPhase?.86f:1));
        }
    }

    void Enter(State state,float seconds)
    {
        CurrentState=state;duration=timer=seconds;Progress=0;
        hitMask=0;pulseHit=palmFlying=false;previousRadius=0;
        effects.Hide();
    }
    void Engage(){FightActive=true;Enter(State.Pursuit,.38f);}
    void WakeOnHit(float damage)
    {
        if(Health.IsDead || FightActive)return;
        if(!player)FindPlayer();
        if(player && player.currentHealth>0 && Flat(player.transform.position-home).magnitude<leashRange)Engage();
    }

    void ResetEncounter()
    {
        retry=FightActive=SecondPhase=false;
        vertical=Moving=0;combo=0;
        nextMandala=0;effects.Clear();
        motor.enabled=false;transform.SetPositionAndRotation(home,homeRotation);motor.enabled=true;
        Health.currentHealth=Health.maxHealth;Health.OnHealthChanged?.Invoke(Health.currentHealth,Health.maxHealth);
        Enter(State.Idle,0);
    }

    void Defeat()
    {
        if(CurrentState==State.Dead)return;
        FightActive=false;Moving=0;deadAt=Time.time;
        effects.Clear();
        Enter(State.Dead,0);motor.enabled=false;
        if(gameObject.scene.path==SecondIslandHuangMeiEncounter.ScenePath)GameSaveManager.RequestAutosave();
    }

    public void RestoreDefeated()
    {
        Health.currentHealth=0;
        Defeat();
        gameObject.SetActive(false);
    }

    void Move(Vector3 velocity,float dt)
    {
        var before=transform.position;
        vertical=motor.isGrounded?-2f:Mathf.Max(-30f,vertical-26f*dt);
        motor.Move((velocity+Vector3.up*vertical)*dt);
        float speed=Flat(transform.position-before).magnitude/dt;
        Moving=Mathf.MoveTowards(Moving,Mathf.Clamp01(speed/moveSpeed),dt*7f);
    }
    void Face(Vector3 delta,float dt)
    {if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),250f*dt);}
    static Vector3 Flat(Vector3 direction)=>Vector3.ProjectOnPlane(direction,Vector3.up);
    Vector3 GroundAt(Vector3 at)=>TryGround(at,out var floor)?floor:at;
    bool TryGround(Vector3 at,out Vector3 floor)
    {
        floor=default;float nearest=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(at+Vector3.up*5,Vector3.down,hits,15,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var hit=hits[i];
            if(!hit.collider || hit.transform.IsChildOf(transform) || CombatBodyCollision.IsActor(hit.collider) || hit.normal.y<.6f || hit.distance>=nearest)continue;
            nearest=hit.distance;floor=hit.point;
        }
        return nearest<float.PositiveInfinity;
    }

    void LateUpdate()
    {
        var cam=Camera.main;
        if(hud!=null && cam)hud.Refresh(FightActive || CurrentState==State.Dead && Time.time-deadAt<3f,
            (transform.position-cam.transform.position).sqrMagnitude,Health.Ratio,Health.currentHealth,Health.maxHealth,
            CurrentAttack==Attack.GoldenMandala && (CurrentState==State.Windup || CurrentState==State.Strike) ? "HUANG MEI  ·  GOLDEN MANDALA" :
            SecondPhase?"HUANG MEI  ·  GOLDEN SEAL":"HUANG MEI",Time.deltaTime);
    }
    void OnDisable(){hud?.Hide();effects?.Clear();}
    void OnDestroy()
    {
        if(Health){Health.OnDeath.RemoveListener(Defeat);Health.OnDamaged.RemoveListener(WakeOnHit);}
        hud?.Dispose();effects?.Dispose();
    }
}
