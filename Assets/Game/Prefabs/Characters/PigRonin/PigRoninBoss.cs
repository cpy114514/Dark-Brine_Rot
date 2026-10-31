using Mavis;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Second-island ronin: readable sword attacks with movement and recovery windows.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Health),typeof(CharacterController))]
public sealed class PigRoninBoss : MonoBehaviour
{
    public enum State { Idle, Chase, Windup, Attack, Recover, Staggered, Return, Dead }
    public enum AttackKind { TwinSlash, Overhead, Dash, Spin }
    [Header("Encounter")]
    public float sightRange=30f, giveUpRange=52f, chaseSpeed=4.5f, turnSpeed=300f;
    public float poiseThreshold=140f, staggerDuration=1.1f;
    [Header("Attacks")]
    public float slashDamage=12f, overheadDamage=22f, dashDamage=18f, spinDamage=16f;
    public float slashReach=5f, overheadReach=5.4f, dashReach=3.4f, spinReach=5.2f;
    public float dashSpeed=13f;
    public State CurrentState { get; private set; }
    public AttackKind CurrentAttack { get; private set; }
    public float ActionProgress { get; private set; }
    public bool FightActive { get; private set; }
    public bool DamageWindowActive { get; private set; }
    public int HitsLanded { get; private set; }
    public int AttacksStarted { get; private set; }
    public bool HudVisible => hud!=null && hud.IsVisible;

    Health health;
    CharacterController motor;
    PlayerHealth target;
    BossHudPanel hud;
    Vector3 home, committed;
    float vertical, timer, previousPhase, poise, lastHit, victoryAt;
    int sequence, hitMask;
    bool dead;
    readonly RaycastHit[] casts=new RaycastHit[32];

    void Awake()
    {
        health=GetComponent<Health>();motor=GetComponent<CharacterController>();
        health.OnDamaged ??= new UnityEvent<float>();health.OnDeath ??= new UnityEvent();
        health.OnDamaged.AddListener(OnDamaged);health.OnDeath.AddListener(OnDeath);
        CombatBodyCollision.Ensure(gameObject);
        var impact=GetComponent<CombatImpactResponse>();if(!impact)impact=gameObject.AddComponent<CombatImpactResponse>();
        impact.unflinching=true;impact.knockbackMultiplier=0;
        // The model/Animator is authored on the prefab; no external visual driver is required here.
        hud=new BossHudPanel(transform,"Pig Ronin Boss HUD");home=transform.position;
    }
    void Start()
    {
        // The authored position may sit slightly above the uneven island.
        if(FindGround(transform.position,out var ground))
        {
            motor.enabled=false;transform.position=new Vector3(transform.position.x,ground.y+.04f,transform.position.z);motor.enabled=true;
        }
        home=transform.position;
    }
    void Update()
    {
        float dt=Time.deltaTime;if(dt<=0 || PauseSettingsMenu.IsOpen)return;
        if(!target || target.currentHealth<=0)target=FindFirstObjectByType<PlayerHealth>();
        if(health.IsDead){if(!dead)OnDeath();return;}
        if(!target || target.currentHealth<=0)
        {FightActive=false;CurrentState=State.Return;MoveHome(dt);return;}
        Vector3 toward=Vector3.ProjectOnPlane(target.transform.position-transform.position,Vector3.up);
        float distance=toward.magnitude,homeDistance=Vector3.ProjectOnPlane(transform.position-home,Vector3.up).magnitude;
        if(!FightActive && distance<=sightRange && homeDistance<giveUpRange)
        {FightActive=true;CurrentState=State.Chase;timer=.4f;}
        if(FightActive && (distance>giveUpRange || homeDistance>giveUpRange))
        {FightActive=false;CurrentState=State.Return;DamageWindowActive=false;}
        if(!FightActive){if(CurrentState==State.Return)MoveHome(dt);else Move(Vector3.zero,dt);return;}
        timer=Mathf.Max(0,timer-dt);
        switch(CurrentState)
        {
            case State.Idle:
            case State.Chase:
                if(toward.sqrMagnitude>.01f)Face(toward,dt);
                if(distance>4.2f)Move(toward.normalized*chaseSpeed,dt);else Move(Vector3.zero,dt);
                CurrentState=State.Chase;
                if(timer<=0 && distance<17f)
                {
                    var attack=distance>7f ? AttackKind.Dash : (AttackKind)(sequence++%4);
                    TryBeginAttack(attack);
                }
                break;
            case State.Windup:
                if(toward.sqrMagnitude>.01f && ActionProgress<.55f)Face(toward,dt);
                Move(Vector3.zero,dt);ActionProgress=1f-timer/WindupDuration(CurrentAttack);
                if(timer<=0){CurrentState=State.Attack;committed=transform.forward;timer=AttackDuration(CurrentAttack);previousPhase=0;ActionProgress=0;hitMask=0;}
                break;
            case State.Attack:
                TickAttack(dt);break;
            case State.Recover:
            case State.Staggered:
                Move(Vector3.zero,dt);ActionProgress=1f-Mathf.Clamp01(timer/(CurrentState==State.Staggered?staggerDuration:RecoveryDuration(CurrentAttack)));
                if(timer<=0){CurrentState=State.Chase;timer=.15f;}
                break;
        }
    }
    public bool TryBeginAttack(AttackKind kind)
    {
        if(!FightActive || !target || target.currentHealth<=0 || health.IsDead ||
            CurrentState==State.Windup || CurrentState==State.Attack || CurrentState==State.Dead)return false;
        CurrentAttack=kind;CurrentState=State.Windup;timer=WindupDuration(kind);ActionProgress=0;
        DamageWindowActive=false;hitMask=0;AttacksStarted++;return true;
    }
    void TickAttack(float dt)
    {
        float phase=1f-Mathf.Clamp01(timer/AttackDuration(CurrentAttack));
        DamageWindowActive=CurrentAttack==AttackKind.TwinSlash ? phase>=.18f && phase<=.73f :
            CurrentAttack==AttackKind.Overhead ? phase>=.48f && phase<=.62f :
            CurrentAttack==AttackKind.Dash ? phase>=.08f && phase<=.8f : phase>=.22f && phase<=.74f;
        if(CurrentAttack==AttackKind.Dash && phase<=.80f && GroundAhead(committed))Move(committed*dashSpeed,dt);
        else Move(Vector3.zero,dt);
        if(CurrentAttack==AttackKind.TwinSlash)
        {
            if(previousPhase<.27f && phase>=.27f)TryDamage(0,slashReach,120f,slashDamage);
            if(previousPhase<.65f && phase>=.65f)TryDamage(1,slashReach,120f,slashDamage+3f);
        }
        else if(CurrentAttack==AttackKind.Overhead && previousPhase<.53f && phase>=.53f)TryDamage(0,overheadReach,80f,overheadDamage);
        else if(CurrentAttack==AttackKind.Dash && DamageWindowActive)TryDamage(0,dashReach,105f,dashDamage);
        else if(CurrentAttack==AttackKind.Spin && previousPhase<.48f && phase>=.48f)TryDamage(0,spinReach,360f,spinDamage);
        previousPhase=phase;ActionProgress=phase;
        if(timer<=0){CurrentState=State.Recover;timer=RecoveryDuration(CurrentAttack);ActionProgress=0;DamageWindowActive=false;}
    }
    void TryDamage(int index,float reach,float arc,float damage)
    {
        if((hitMask&(1<<index))!=0 || !target || target.currentHealth<=0)return;
        Vector3 delta=Vector3.ProjectOnPlane(target.transform.position-transform.position,Vector3.up);
        var targetBody=target.GetComponent<CharacterController>();
        float extra=targetBody ? targetBody.radius*Mathf.Abs(targetBody.transform.lossyScale.x) : 0;
        if(delta.magnitude>reach+extra || arc<359 && delta.sqrMagnitude>.001f && Vector3.Angle(committed,delta)>arc*.5f)return;
        float targetFeet=targetBody ? targetBody.bounds.min.y : target.transform.position.y;
        if(Mathf.Abs(targetFeet-transform.position.y)>4f || Blocked(targetBody))return;
        hitMask|=1<<index;
        Vector3 point=targetBody ? targetBody.bounds.center : target.transform.position;
        if(target.ApplyAttackDamage(damage,point,transform.position))HitsLanded++;
    }
    bool Blocked(Collider targetBody)
    {
        Vector3 from=motor.bounds.center,to=targetBody ? targetBody.bounds.center : target.transform.position;
        Vector3 delta=to-from;if(delta.sqrMagnitude<.001f)return false;
        int count=Physics.RaycastNonAlloc(from,delta.normalized,casts,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var c=casts[i].collider;
            if(!c || c.transform.IsChildOf(transform) || c.transform.IsChildOf(target.transform) || CombatBodyCollision.IsActor(c))continue;
            return true;
        }
        return false;
    }
    void Move(Vector3 planar,float dt)
    {
        if(!motor || !motor.enabled)return;
        vertical=motor.isGrounded ? -2f : Mathf.Max(-30f,vertical-28f*dt);
        motor.Move((planar+Vector3.up*vertical)*dt);
    }
    void MoveHome(float dt)
    {
        var direction=Vector3.ProjectOnPlane(home-transform.position,Vector3.up);
        if(direction.magnitude<.5f){CurrentState=State.Idle;Move(Vector3.zero,dt);return;}
        Face(direction,dt);Move(direction.normalized*chaseSpeed,dt);
    }
    void Face(Vector3 direction,float dt)
    {if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction,Vector3.up),turnSpeed*dt);}
    bool GroundAhead(Vector3 direction)
    {
        Vector3 at=transform.position+direction*1.5f;
        return FindGround(at,out var ground) && ground.y>=transform.position.y-2.5f;
    }
    bool FindGround(Vector3 at,out Vector3 ground)
    {
        ground=default;float closest=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(at+Vector3.up*5,Vector3.down,casts,15,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            var h=casts[i];if(!h.collider || h.collider.transform.IsChildOf(transform) || CombatBodyCollision.IsActor(h.collider) || h.normal.y<.6f || h.distance>=closest)continue;
            ground=h.point;closest=h.distance;
        }
        return closest<float.PositiveInfinity;
    }
    void OnDamaged(float damage)
    {
        if(health.IsDead)return;
        if(!FightActive){target=FindFirstObjectByType<PlayerHealth>();if(target && target.currentHealth>0){FightActive=true;CurrentState=State.Chase;timer=.2f;}}
        if(Time.time-lastHit>4f)poise=0;lastHit=Time.time;poise+=damage;
        if(poise>=poiseThreshold){poise=0;CurrentState=State.Staggered;timer=staggerDuration;DamageWindowActive=false;}
    }
    void OnDeath()
    {
        if(dead)return;dead=true;FightActive=false;DamageWindowActive=false;CurrentState=State.Dead;victoryAt=Time.time;
        if(motor)motor.enabled=false;
    }
    void LateUpdate()
    {
        if(hud==null || !health)return;
        var camera=Camera.main;bool visible=camera && (FightActive || dead && Time.time-victoryAt<3f);
        hud.Refresh(visible,camera?(transform.position-camera.transform.position).sqrMagnitude:float.PositiveInfinity,
            health.Ratio,health.currentHealth,health.maxHealth,"PIG RONIN",Time.deltaTime);
    }
    public static float WindupDuration(AttackKind kind)=>kind==AttackKind.TwinSlash?.65f:kind==AttackKind.Overhead?.9f:kind==AttackKind.Dash?.85f:1f;
    public static float AttackDuration(AttackKind kind)=>kind==AttackKind.TwinSlash?1.2f:kind==AttackKind.Overhead?.8f:kind==AttackKind.Dash?.85f:1.05f;
    public static float RecoveryDuration(AttackKind kind)=>kind==AttackKind.TwinSlash?.7f:kind==AttackKind.Overhead?1.05f:kind==AttackKind.Dash?.85f:1f;
    void OnDisable(){hud?.Hide();}
    void OnDestroy(){if(health){health.OnDamaged.RemoveListener(OnDamaged);health.OnDeath.RemoveListener(OnDeath);}hud?.Dispose();}
}
