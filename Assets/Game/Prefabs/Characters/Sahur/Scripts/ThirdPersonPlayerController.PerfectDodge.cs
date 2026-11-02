using Mavis;
using UnityEngine;
public sealed partial class ThirdPersonPlayerController
{
    [Header("Perfect dodge / returning sweep")]
    [Range(.08f,.4f)] public float perfectDodgeWindow=.28f;
    [Range(.18f,.4f)] public float perfectStepDuration=.28f;
    [Range(.8f,3f)] public float perfectStepDistance=2f;
    [Range(.2f,.7f)] public float perfectCounterWindow=.45f;
    public bool IsPerfectStepping=>perfectStep&&rollTimer>0;
    public int PerfectDodgesPerformed { get; private set; }
    public Transform PerfectDodgeAttacker=>perfectAttacker;
    bool perfectStep,perfectCounterBuffered;
    float perfectCounterUntil=-1;
    readonly long[] consumedPerfectStrikes=new long[32];
    int consumedPerfectIndex;
    Transform perfectAttacker;
    SahurWeaponLoadout perfectLoadout;
    bool perfectBlades;
    PerfectDodgeEcho perfectEcho;
    float ActiveDodgeDuration=>perfectStep?perfectStepDuration:rollDuration;
    bool TryStartPerfectDodge()
    {
        perfectStep=false;ClearPerfectCounter();var health=GetComponent<PlayerHealth>();
        if(!health||health.currentHealth<=0||health.NarrativeDamageOwner||!animator||
           !animator.HasState(0,Animator.StringToHash("Base Layer.Perfect Step")))return false;
        if(!PerfectDodgeThreats.Find(health,perfectDodgeWindow,consumedPerfectStrikes,out var threat))return false;
        perfectStep=true;perfectAttacker=threat.attacker;consumedPerfectStrikes[consumedPerfectIndex++%32]=threat.stamp;
        perfectCounterUntil=Time.time+perfectStepDuration+perfectCounterWindow;
        perfectLoadout=GetComponent<SahurWeaponLoadout>();perfectBlades=combat&&combat.UsesTwinBlades;PerfectDodgesPerformed++;
        if(!perfectEcho)perfectEcho=GetComponent<PerfectDodgeEcho>()??gameObject.AddComponent<PerfectDodgeEcho>();
        perfectEcho.Begin();return true;
    }
    public void RefreshPerfectCounter()
    {
        if(perfectCounterUntil<0)return;
        var hp=GetComponent<PlayerHealth>();
        if(Time.time>perfectCounterUntil||!isActiveAndEnabled||ExternalControlLock||ExternalMovementLock||Swimming||
           PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput||IslandMapUI.BlocksInput||GameControls.InputInterrupted||
           !hp||hp.currentHealth<=0||hp.NarrativeDamageOwner||!PerfectDodgeThreats.Alive(perfectAttacker)||
           combat&&combat.UsesTwinBlades!=perfectBlades||perfectLoadout&&perfectLoadout.UsesRifle)ClearPerfectCounter();
    }
    public bool PerfectCounterInput(SahurAttack attack)
    {
        if(GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump)){ClearPerfectCounter();return false;}
        RefreshPerfectCounter();if(!perfectAttacker||Time.time>perfectCounterUntil)return false;
        if(rollTimer>0){if(perfectStep&&rollTimer<=.10f)perfectCounterBuffered=true;return true;}
        if(!CanUseGroundAttack)return false;
        if(attack.BeginPerfectCounter(perfectAttacker)){ClearPerfectCounter();return true;}
        return false;
    }
    public void FlushPerfectCounter(SahurAttack attack)
    {
        RefreshPerfectCounter();if(perfectCounterBuffered&&rollTimer<=0&&CanUseGroundAttack)PerfectCounterInput(attack);
    }
    public void ClearPerfectCounter(){perfectAttacker=null;perfectCounterUntil=-1;perfectCounterBuffered=false;}
    public void SetPerfectCounterFacing(Quaternion rotation)
    {
        recoveringAttackFacing=false;hasAttackFacing=true;attackFacing=rotation;transform.rotation=rotation;
    }
}
