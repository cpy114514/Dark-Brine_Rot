using Mavis;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class MarshmallowHollowGuardian : Health
{
    public enum Phase { Dormant, Windup, Feint, Pulse, Rest, Exposed, Defeated }
    public Phase Current { get; private set; }
    public bool CanOpen => Current == Phase.Rest;
    public float PulseRadius { get; private set; }

    MarshmallowHollow hollow;
    PlayerHealth target;
    CharacterController targetBody;
    Transform leftArm, rightArm, head, core;
    LineRenderer ring;
    BossHudPanel hud;
    Collider hitbox;
    float age, duration;
    bool waveHit;
    int attacks;

    public void Build(MarshmallowHollow instance, MarshmallowHollowArt art)
    {
        hollow = instance;
        maxHealth = currentHealth = 720;
        OnDeath ??= new UnityEvent();
        OnDamaged ??= new UnityEvent<float>();
        OnHealthChanged ??= new UnityEvent<float,float>();
        OnDeath.AddListener(OnFell);
        var solid = gameObject.AddComponent<CapsuleCollider>();
        solid.center = new Vector3(0,3.25f,0); solid.radius = 2.25f; solid.height = 6.5f;
        hitbox = solid;
        CombatBodyCollision.Ensure(gameObject);
        var response = gameObject.AddComponent<CombatImpactResponse>();
        response.unflinching = true; response.knockbackMultiplier = 0; response.stopMultiplier = 0;
        art.Rounded(transform,"Kiln guardian body",new Vector3(0,3.22f,0),new Vector3(4.6f,4.45f,3.35f),art.Stone);
        art.Rounded(transform,"Left rooted foot",new Vector3(-1.27f,.49f,-.18f),new Vector3(1.85f,1.1f,2.55f),art.Copper);
        art.Rounded(transform,"Right rooted foot",new Vector3(1.27f,.49f,-.18f),new Vector3(1.85f,1.1f,2.55f),art.Copper);
        head = art.Rounded(transform,"Sugarstone mask",new Vector3(0,5.8f,-.15f),new Vector3(3.5f,2.25f,2.65f),art.Sugar);
        art.Rounded(head,"Left kiln eye",new Vector3(-.66f,.13f,-1.32f),new Vector3(.62f,.45f,.20f),art.Amber);
        art.Rounded(head,"Right kiln eye",new Vector3(.66f,.13f,-1.32f),new Vector3(.62f,.45f,.20f),art.Amber);
        core = art.Glyph(transform,MarshmallowHollowArt.Mark.Kiln,new Vector3(0,3.45f,-1.7f),1.07f,art.Mint);
        leftArm = Arm(art,-1);
        rightArm = Arm(art,1);
        var go = MarshmallowHollowArt.Node(transform,"Kiln ground shockwave",new Vector3(0,.11f,0));
        ring = go.gameObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 64;
        ring.sharedMaterial = art.Amber; ring.widthMultiplier = .28f;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.enabled = false;
        hud = new BossHudPanel(transform,"Hollow guardian boss bar");
        core.gameObject.SetActive(false);
    }

    Transform Arm(MarshmallowHollowArt art, int side)
    {
        var arm = MarshmallowHollowArt.Node(transform,side<0?"Left kiln arm":"Right kiln arm",new Vector3(side*2.0f,4.75f,0));
        art.Rounded(arm,"Glazed upper arm",new Vector3(side*.64f,-.50f,-.05f),new Vector3(1.65f,2.0f,1.65f),art.Copper);
        art.Rounded(arm,"Glazed kiln fist",new Vector3(side*.86f,-1.73f,-.40f),new Vector3(1.86f,1.6f,1.9f),art.Sugar);
        art.Crystal(arm,new Vector3(side*.9f,-1.65f,-1.25f),.67f,art.Amber);
        return arm;
    }

    void Update()
    {
        if (!hollow || IsDead || PauseSettingsMenu.IsOpen || Time.timeScale<=0) return;
        if (!hollow.InArena)
        {
            if (Current!=Phase.Dormant) Enter(Phase.Dormant,0);
            return;
        }
        if (!target)
        {
            target = FindFirstObjectByType<PlayerHealth>();
            if (target) targetBody=target.GetComponent<CharacterController>();
        }
        if (!target || !targetBody || target.currentHealth<=0 || !targetBody.enabled) return;
        age += Time.deltaTime;
        switch (Current)
        {
            case Phase.Dormant:
                BeginWindup();
                break;
            case Phase.Windup:
                if (age>=duration)
                {
                    if (Ratio<=.5f && attacks%2==1) Enter(Phase.Feint,.66f);
                    else Enter(Phase.Pulse,1.60f);
                }
                break;
            case Phase.Feint:
                if (age>=duration) Enter(Phase.Pulse,1.44f);
                break;
            case Phase.Pulse:
                TickPulse();
                if (age>=duration) Enter(Phase.Rest,3.5f);
                break;
            case Phase.Rest:
            case Phase.Exposed:
                if (age>=duration) BeginWindup();
                break;
        }
        Pose();
    }

    void BeginWindup()
    {
        attacks++;
        Vector3 toward = Vector3.ProjectOnPlane(transform.position-target.transform.position,Vector3.up);
        if (toward.sqrMagnitude>.05f) transform.rotation=Quaternion.LookRotation(toward);
        Enter(Phase.Windup,Ratio>.5f?1.45f:1.08f);
    }

    void TickPulse()
    {
        float previous = PulseRadius;
        PulseRadius = Mathf.Lerp(1.15f,20.8f,Mathf.Clamp01(age/duration));
        DrawRing(PulseRadius);
        Vector3 offset = targetBody.bounds.center - transform.position;
        float horizontal = Vector3.ProjectOnPlane(offset,Vector3.up).magnitude;
        float height = targetBody.bounds.min.y - transform.position.y;
        if (!waveHit && horizontal >= previous-1.2f && horizontal <= PulseRadius+1.2f && height<.95f && height>-.6f)
        {
            waveHit = true;
            target.ResolveCombatHit(gameObject,Ratio>.5f?27:35,target.transform.position,CombatHitKind.ComboOne);
        }
    }

    void Pose()
    {
        float t = duration>0 ? Mathf.Clamp01(age/duration) : 0;
        float rise = 0, slump = 0;
        switch (Current)
        {
            case Phase.Windup: rise=Mathf.SmoothStep(0,1,t); break;
            case Phase.Feint: rise=.52f+.48f*Mathf.Cos(t*Mathf.PI*2f); break;
            case Phase.Pulse: rise=1f-Mathf.Clamp01(t*7f); break;
            case Phase.Exposed: slump=Mathf.SmoothStep(0,1,t*7f); break;
        }
        leftArm.localRotation=Quaternion.Euler(-110f*rise+26f*slump,0,18f*rise-12f*slump);
        rightArm.localRotation=Quaternion.Euler(-110f*rise+26f*slump,0,-18f*rise+12f*slump);
        head.localRotation=Quaternion.Euler(-10f*rise+21f*slump,0,5f*slump);
    }

    void DrawRing(float radius)
    {
        for (int i=0;i<ring.positionCount;i++)
        {
            float angle=i*(Mathf.PI*2f/ring.positionCount);
            ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
        }
    }

    void Enter(Phase phase, float seconds)
    {
        Current=phase; age=0; duration=seconds;
        ring.enabled=phase==Phase.Pulse;
        core.gameObject.SetActive(phase==Phase.Exposed);
        if (phase==Phase.Pulse)
        {
            waveHit=false; PulseRadius=1.15f; DrawRing(PulseRadius);
            GameplayAudio.Play(GameplayAudio.Cue.Hurt,transform.position,true);
        }
    }

    public bool TryOpen()
    {
        if (!CanOpen || IsDead) return false;
        Enter(Phase.Exposed,hollow.Assisted?9.5f:6.5f);
        hollow.Notify("THE KILN CORE IS EXPOSED",2.5f);
        return true;
    }

    public override void ApplyDamage(float amount, Vector3 point)
    {
        if (Current==Phase.Exposed) base.ApplyDamage(amount,point);
    }

    void OnFell()
    {
        Enter(Phase.Defeated,0);
        hitbox.enabled=false;
        leftArm.localRotation=Quaternion.Euler(38,0,-22);
        rightArm.localRotation=Quaternion.Euler(38,0,22);
        head.localRotation=Quaternion.Euler(36,0,0);
        hollow.GuardianFell();
    }

    public void ResetFight()
    {
        if (IsDead) return;
        currentHealth=maxHealth;
        OnHealthChanged.Invoke(currentHealth,maxHealth);
        attacks=0;
        Enter(Phase.Dormant,0);
        Pose();
    }

    void LateUpdate()
    {
        if (hud==null || !hollow) return;
        string title=Current==Phase.Exposed?"KILN GUARDIAN   /   CORE EXPOSED":"KILN GUARDIAN";
        hud.Refresh(hollow.InArena && !IsDead,0,Ratio,currentHealth,maxHealth,title,Time.deltaTime);
    }
    void OnDestroy()
    {
        OnDeath?.RemoveListener(OnFell);
        hud?.Dispose();
    }
}

