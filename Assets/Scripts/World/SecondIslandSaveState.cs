using System;
using System.Collections.Generic;
using Mavis;
using UnityEngine;

/// <summary>Captures second-island milestones alongside the shared inventory.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandSaveState : MonoBehaviour
{
    [Serializable] public sealed class Progress
    {
        public List<string> defeatedEnemies = new List<string>();
        public bool roninDefeated, finishedCourse, collectedScrolls, normalGravity, courseArmed, flying;
        public float boardCooldown, flightRemaining;
        public int courseRow;
    }
    int restoredRow;
    bool restoredArmed;
    float restoredFlightRemaining;

    public Progress Capture()
    {
        var wristband=FindFirstObjectByType<GravityWristbandReward>();
        var board=FindFirstObjectByType<SkyboardReward>();
        var objective=GetComponent<SecondIslandObjectives>();
        var traversal=GetComponent<MarshmallowTraversal>();
        var boss=FindFirstObjectByType<PigRoninBoss>();
        return new Progress {
            defeatedEnemies=wristband ? wristband.CaptureDefeated() : new List<string>(),
            roninDefeated=(objective && objective.RoninDefeated) || (boss && boss.GetComponent<Health>().IsDead),
            finishedCourse=objective && objective.FinishedCourse,
            collectedScrolls=board && board.CollectedScrolls>0,
            boardCooldown=board ? board.CooldownRemaining : 0,
            flying=board && board.Flying,
            flightRemaining=board && board.Flying ? board.FlightRemaining : 0,
            courseRow=board && board.GetComponent<MarshmallowHopRecovery>() ? board.GetComponent<MarshmallowHopRecovery>().SaveRow : 0,
            courseArmed=board && board.GetComponent<MarshmallowHopRecovery>() && board.GetComponent<MarshmallowHopRecovery>().RecoveryArmed,
            normalGravity=traversal && traversal.NormalGravity
        };
    }
    public void Restore(Progress progress)
    {
        if(progress==null)return;
        var wristband=FindFirstObjectByType<GravityWristbandReward>();
        var board=FindFirstObjectByType<SkyboardReward>();
        wristband?.RestoreDefeated(progress.defeatedEnemies);
        board?.RestoreProgress(progress.collectedScrolls,progress.boardCooldown);
        GetComponent<SecondIslandObjectives>()?.RestoreProgress(progress.finishedCourse,progress.roninDefeated);
        var gravity=GetComponent<GravityWristbandAbility>();
        if(gravity && gravity.Owned)gravity.SetNormalGravity(progress.normalGravity);
        else GetComponent<MarshmallowTraversal>()?.SetNormalGravity(progress.normalGravity);
        if(progress.roninDefeated)
        {
            var boss=FindFirstObjectByType<PigRoninBoss>();
            if(boss){var health=boss.GetComponent<Health>();if(health)health.currentHealth=0;boss.gameObject.SetActive(false);}
        }
        restoredRow=Mathf.Clamp(progress.courseRow,0,22);
        restoredArmed=progress.courseArmed;
        restoredFlightRemaining=progress.flying ? progress.flightRemaining : 0;
    }
    public void RestoreCoursePosition()
    {
        if(restoredRow<=0)return;
        var board=FindFirstObjectByType<SkyboardReward>();
        board?.GetComponent<MarshmallowHopRecovery>()?.RestoreRow(restoredRow,restoredArmed);
        restoredRow=0;
    }
    public void RestoreFlight()
    {
        if(restoredFlightRemaining<=0)return;
        FindFirstObjectByType<SkyboardReward>()?.RestoreFlight(restoredFlightRemaining);
        restoredFlightRemaining=0;
    }
}
