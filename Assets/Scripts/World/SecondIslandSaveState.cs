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
        public bool roninDefeated, finishedCourse, collectedScrolls, normalGravity, courseArmed, flying, courseAssist, candyHuntStarted, secretGroveUnlocked, secretGroveRested, secretDoorInspected, secretSmallLightOwned, skyboardRallyReward, melodyGardenReward, candyHuntRewardClaimed;
        public bool bounceGardenReward, zhuBaJieDefeated, huangMeiDefeated;
        public int hollowChapter, hollowDefeats, hollowPacksTaken;
        public bool hollowAssisted, hollowWaymarkCopied;
        public int marshmallowFriendsStage;
        public int activityDiscoveryMask;
        public float boardCooldown, flightRemaining, skyboardRallyBest;
        public int courseRow, courseBestRow, courseFalls, courseStars, courseAssistUntil, candyHuntMask, cottageHealingPacks, candyHuntSelectedStyle;
    }
    int restoredRow;
    bool restoredArmed;
    float restoredFlightRemaining;
    bool huangMeiDefeated;

    public Progress Capture()
    {
        var wristband=FindFirstObjectByType<GravityWristbandReward>();
        var board=FindFirstObjectByType<SkyboardReward>();
        var objective=GetComponent<SecondIslandObjectives>();
        var traversal=GetComponent<MarshmallowTraversal>();
        var boss=FindFirstObjectByType<PigRoninBoss>();
        var zhuBaJie=FindFirstObjectByType<ZhuBaJieBoss>();
        var huangMei=FindFirstObjectByType<HuangMeiBoss>();
        var course=board ? board.GetComponent<MarshmallowHopRecovery>() : null;
        var candyHunt=FindFirstObjectByType<CandyHuntQuest>();
        var cottage=FindFirstObjectByType<CandyHouseMarker>();
        var residents=FindFirstObjectByType<FriendlyMarshmallowVillage>();
        var rally=FindFirstObjectByType<SkyboardRingRally>();
        var melody=FindFirstObjectByType<CandyMelodyGarden>();
        var bounce=FindFirstObjectByType<CandyBounceGarden>();
        var friends=FindFirstObjectByType<MarshmallowFriendsQuest>();
        var guide=FindFirstObjectByType<IslandActivityGuide>();
        var hollow=FindFirstObjectByType<MarshmallowHollowEntrance>();
        return new Progress {
            defeatedEnemies=wristband ? wristband.CaptureDefeated() : new List<string>(),
            roninDefeated=(objective && objective.LegacyRoninDefeated) || (boss && boss.GetComponent<Health>().IsDead),
            zhuBaJieDefeated=zhuBaJie && zhuBaJie.GetComponent<Health>().IsDead,
            huangMeiDefeated=huangMeiDefeated || huangMei && huangMei.Health && huangMei.Health.IsDead,
            finishedCourse=objective && objective.FinishedCourse,
            collectedScrolls=board && board.CollectedScrolls>0,
            boardCooldown=board ? board.CooldownRemaining : 0,
            flying=board && board.Flying,
            flightRemaining=board && board.Flying ? board.FlightRemaining : 0,
            courseRow=course ? course.SaveRow : 0,
            courseArmed=course && course.RecoveryArmed,
            courseBestRow=course ? course.HighestRow : 0,
            courseFalls=course ? course.ConsecutiveFalls : 0,
            courseStars=course ? course.StarMask : 0,
            courseAssist=course && course.AssistanceActive,
            courseAssistUntil=course ? course.AssistUntilRow : 0,
            candyHuntStarted=candyHunt && candyHunt.Started,
            candyHuntMask=candyHunt ? candyHunt.CollectedMask : 0,
            candyHuntRewardClaimed=candyHunt && candyHunt.RewardClaimed,
            candyHuntSelectedStyle=candyHunt ? candyHunt.SelectedStyle : 0,
            cottageHealingPacks=cottage ? cottage.CollectedHealingPacks : 0,
            secretGroveUnlocked=residents && residents.Unlocked,
            secretGroveRested=residents && residents.Rested,
            secretDoorInspected=residents && residents.DoorInspected,
            secretSmallLightOwned=residents && residents.SmallLightOwned,
            skyboardRallyReward=rally && rally.RewardClaimed,
            skyboardRallyBest=rally ? rally.BestTime : 0,
            melodyGardenReward=melody && melody.RewardClaimed,
            bounceGardenReward=bounce && bounce.RewardClaimed,
            marshmallowFriendsStage=friends ? friends.Stage : 0,
            activityDiscoveryMask=guide ? guide.DiscoveredMask : 0,
            normalGravity=MarshmallowHollowEntrance.SaveGravity(GetComponent<ThirdPersonPlayerController>(),traversal && traversal.NormalGravity),
            hollowChapter=hollow ? hollow.Chapter : 0,
            hollowDefeats=hollow ? hollow.GuardianDefeats : 0,
            hollowAssisted=hollow && hollow.Assisted,
            hollowWaymarkCopied=hollow && hollow.WaymarkCopied,
            hollowPacksTaken=hollow ? hollow.PacksTaken : 0
        };
    }
    public void Restore(Progress progress)
    {
        if(progress==null)return;
        var wristband=FindFirstObjectByType<GravityWristbandReward>();
        var board=FindFirstObjectByType<SkyboardReward>();
        wristband?.RestoreDefeated(progress.defeatedEnemies);
        board?.RestoreProgress(progress.collectedScrolls,progress.boardCooldown);
        board?.GetComponent<MarshmallowHopRecovery>()?.RestoreChallengeProgress(progress.courseBestRow,progress.courseFalls,progress.courseStars,progress.courseAssist,progress.courseAssistUntil);
        FindFirstObjectByType<CandyHuntQuest>()?.RestoreProgress(progress.candyHuntStarted, progress.candyHuntMask, progress.candyHuntRewardClaimed, progress.candyHuntSelectedStyle);
        FindFirstObjectByType<CandyHouseMarker>()?.RestoreHealingPacks(progress.cottageHealingPacks);
        FindFirstObjectByType<FriendlyMarshmallowVillage>()?.RestoreProgress(progress.secretGroveUnlocked, progress.secretGroveRested, progress.secretDoorInspected, progress.secretSmallLightOwned);
        FindFirstObjectByType<SkyboardRingRally>()?.RestoreProgress(progress.skyboardRallyReward, progress.skyboardRallyBest);
        FindFirstObjectByType<CandyMelodyGarden>()?.RestoreProgress(progress.melodyGardenReward);
        FindFirstObjectByType<CandyBounceGarden>()?.RestoreProgress(progress.bounceGardenReward);
        FindFirstObjectByType<MarshmallowFriendsQuest>()?.RestoreProgress(progress.marshmallowFriendsStage);
        FindFirstObjectByType<IslandActivityGuide>()?.RestoreProgress(progress.activityDiscoveryMask);
        FindFirstObjectByType<MarshmallowHollowEntrance>()?.RestoreProgress(progress.hollowChapter,progress.hollowDefeats,
            progress.hollowAssisted,progress.hollowWaymarkCopied,progress.hollowPacksTaken);
        GetComponent<SecondIslandObjectives>()?.RestoreProgress(progress.finishedCourse,progress.roninDefeated);
        var gravity=GetComponent<GravityWristbandAbility>();
        if(gravity && gravity.Owned)gravity.SetNormalGravity(progress.normalGravity);
        else GetComponent<MarshmallowTraversal>()?.SetNormalGravity(progress.normalGravity);
        if(progress.roninDefeated)
        {
            var boss=FindFirstObjectByType<PigRoninBoss>();
            if(boss){var health=boss.GetComponent<Health>();if(health)health.currentHealth=0;boss.gameObject.SetActive(false);}
        }
        if(progress.zhuBaJieDefeated)FindFirstObjectByType<ZhuBaJieBoss>()?.RestoreDefeated();
        huangMeiDefeated=progress.huangMeiDefeated;
        if(huangMeiDefeated)FindFirstObjectByType<HuangMeiBoss>()?.RestoreDefeated();
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
