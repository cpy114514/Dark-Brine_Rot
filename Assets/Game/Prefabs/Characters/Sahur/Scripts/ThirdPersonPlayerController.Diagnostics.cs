using UnityEngine;using Mavis;
public sealed partial class ThirdPersonPlayerController
{
    [Header("Development diagnostics")]
    [Tooltip("Editor/development builds only. The panel records real input, ownership and rejection/buffering decisions.")]
    public bool showControlDiagnostics;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public struct ControlEvent { public int frame;public float time;public string action,decision; }
    ControlEvent[] controlEvents;
    int controlEventIndex;
    public ControlEvent LastControlEvent {get;private set;}
    public float LastControlInputTime {get;private set;}
    public string LastControlState {get;private set;}
    public bool DiagnosticMovementLocked {get;private set;}
    public ControlEvent[] ControlEvents=>controlEvents;
#endif
    [System.Diagnostics.Conditional("UNITY_EDITOR"),System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public void TraceControl(string action,string decision)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(!showControlDiagnostics)return;
        if(controlEvents==null)controlEvents=new ControlEvent[64];
        var item=new ControlEvent{frame=Time.frameCount,time=Time.time,action=action,decision=decision};
        controlEvents[controlEventIndex++%controlEvents.Length]=item;LastControlEvent=item;
#endif
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR"),System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    void SampleControlDiagnostics()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(!showControlDiagnostics)return;
        DiagnosticMovementLocked=ExternalControlLock||ExternalMovementLock||combat&&combat.IsCombatMotionActive&&!combat.CanTranslateDuringCombat;
        LastControlState=ExternalControlLock||ExternalMovementLock?"External owner":Swimming?"Swimming":IsDodging?"Dodge":combat&&combat.IsCombatMotionActive?"Combat":motionState==4?"Landing":IsGroundedOrOnSea()?"Locomotion":"Airborne";
        foreach(var command in new[]{GameControls.Command.Attack,GameControls.Command.Charge,GameControls.Command.Dodge,GameControls.Command.Jump})if(GameControls.Down(command))
        {
            LastControlInputTime=Time.time;
            string reason=PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput?"Menu blocks input":ExternalControlLock||ExternalMovementLock?"External owner blocks input":
                command==GameControls.Command.Dodge&&rollCooldownTimer>0&&!IsDodging?"Dodge cooldown":command==GameControls.Command.Dodge&&playerStamina&&!playerStamina.CanSpend(playerStamina.rollCost)?"Insufficient stamina":
                command==GameControls.Command.Attack&&Swimming?"Swimming blocks ground attacks":
                command==GameControls.Command.Jump&&combat&&combat.IsCombatMotionActive?"Attack commitment blocks jump":
                command==GameControls.Command.Jump&&playerStamina&&!playerStamina.CanSpend(playerStamina.jumpCost)?"Insufficient stamina":
                command==GameControls.Command.Attack&&IsDodging?(DodgeTimeRemaining<=ControlFeel.inputBuffer?"Attack buffered for dodge recovery":"Too early in dodge"):
                combat&&combat.IsCombatMotionActive&&command==GameControls.Command.Dodge?"Buffered / waiting for damage arc recovery":"Input sampled";
            TraceControl(command.ToString(),reason);
        }
#endif
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void OnGUI()
    {
        if(!showControlDiagnostics||!Application.isPlaying)return;
        float phase=combat?combat.CurrentAttackPhase:0;var e=LastControlEvent;
        GUI.Box(new Rect(12,140,440,155),"Sahur controls · "+ControlFeel.controlPreset);
        GUI.Label(new Rect(24,164,420,130),"State: "+LastControlState+" | move lock: "+DiagnosticMovementLocked+
            "\nSpeed: "+Vector3.ProjectOnPlane(WorldVelocity,Vector3.up).magnitude.ToString("F2")+" | attack phase: "+phase.ToString("F3")+
            "\nInput t="+LastControlInputTime.ToString("F3")+" | transition: "+(animator&&animator.IsInTransition(0))+
            "\nEvent f="+e.frame+" t="+e.time.ToString("F3")+" "+e.action+"\n"+e.decision);
    }
#endif
}
