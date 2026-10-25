using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed partial class Story1WreckBattle
{
    public bool FinaleRecoveryAvailable { get; private set; }
    public string FinaleFailure { get; private set; }
    const string FinaleRecoveryHint = "The ending could not start.\nR: retry ending  /  Enter: continue to shore";

    void RecoverFinale(System.Exception error)
    {
        FinaleFailure=error.Message;
        FinaleRecoveryAvailable=true;
        var failed=Finale;
        if(failed!=null)
        {
            failed.enabled=false;
            failed.ReleaseActors();
            player.Motion.Release(failed);
            Destroy(failed);
            Finale=null;
        }
        player.Motion.Release(this);
        // The film may have partially disabled colliders, animation or the rider.
        // Restore the last real gameplay pose, not a sampled future film pose.
        player.ExternalControlLock=player.ExternalMovementLock=false;
        player.ExternalMovementAnimation=null;player.ExternalCameraFocus=null;
        player.KeepCameraAboveWater=false;
        capsule.enabled=true;
        player.RestoreSavedPose(Handoff.position,Handoff.rotation);
        player.enabled=true;
        var attack=player.GetComponent<SahurAttack>();
        attack.CinematicHandoffPending=false;attack.enabled=true;
        player.GetComponent<SahurBoomerang>().enabled=true;
        var knockback=player.GetComponent<CombatKnockback>();
        if(knockback!=null)knockback.enabled=true;
        if(rider!=null)rider.Initialize(player,planks,ocean,Handoff.support,detailPieces);
        if(sharkAnimator!=null)sharkAnimator.ReleaseCinematicAnimation();
        playerHealth.NarrativeDefeatHandler=ProtectStoryFight;
        playerHealth.NarrativeHealthFloor=playerHealth.maxHealth*finalePlayerHealthRatio;
        playerHealth.GrantProtection(2);
        SharkHealth.IsProtected=true;
        RequestedFinaleReason=FinaleReason.None;
        CurrentPhase=Phase.Fighting;phaseAge=0;
        bossName.enabled=false;healthFill.transform.parent.gameObject.SetActive(false);
        controls.text=FinaleRecoveryHint;
        CinematicLetterbox.Set(this,false);
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        Debug.LogWarning("[Opening] Ending recovery: "+error,this);
    }

    void TickFinaleRecovery(float deltaTime)
    {
        MovePlanks(deltaTime);
        // The player can move while choosing; the shark must not punish a loading error.
        playerHealth.GrantProtection(2);
        var keys=Keyboard.current;
        if(keys==null || SahurLoadoutUI.BlocksInput || IslandMapUI.BlocksInput || PlayerDeathRespawn.IsOpen)return;
        if(keys.rKey.wasPressedThisFrame)RetryFinale();
        else if(keys.enterKey.wasPressedThisFrame)ContinueAfterFinaleFailure();
    }

    public bool RetryFinale()
    {
        if(!FinaleRecoveryAvailable || PauseSettingsMenu.IsOpen)return false;
        FinaleRecoveryAvailable=false;
        BeginFinale();
        return !FinaleRecoveryAvailable;
    }

    public bool ContinueAfterFinaleFailure()
    {
        if(!FinaleRecoveryAvailable || PauseSettingsMenu.IsOpen)return false;
        const string island="Assets/Scenes/First Island/Main.unity";
        if(!Application.CanStreamedLevelBeLoaded(island))
        {
            controls.text="The shore is unavailable.\nEnter: try again  /  Esc: pause";
            return false;
        }
        GameSaveManager.CancelPendingContinue();
        if(!SceneLoadingScreen.Load(island))return false;
        FinaleRecoveryAvailable=false;
        CurrentPhase=Phase.Finished;
        return true;
    }
}
