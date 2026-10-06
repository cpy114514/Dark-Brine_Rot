using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyStory1WreckFinale
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<object> VerifyRandomDurations()
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        var durations=new System.Collections.Generic.List<float>();
        for(int run=0;run<3;run++)
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);await Task.Delay(200);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();sequence.BeginImpact();
            for(int i=0;i<300 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(30);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Break did not finish.");
            float duration=battle.FinaleAfterSeconds;
            Require(duration>=20 && duration<=35,"Random fight time outside its configured range.");
            durations.Add(duration);
        }
        Require(Mathf.Abs(durations[0]-durations[1])>.0001f || Mathf.Abs(durations[1]-durations[2])>.0001f,"Separate encounters reused the same duration.");
        return new{sampledOncePerEncounter=true,durations};
    }
    public static async Task<object> Verify(bool fromWater=false, bool capture=true, bool quick=false, bool lowHealth=false)
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);await Task.Delay(400);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();
            var hero=sequence.sahur;var hp=hero.GetComponent<PlayerHealth>();
            if(quick){battle.minimumFightSeconds=6;battle.maximumFightSeconds=9;}
            if(lowHealth)battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.BeginImpact();
            if(capture)
            {
                Directory.CreateDirectory(Path.GetFullPath(".codex/shark-finale-preview/impact-frames"));
                var impactTimes=new System.Collections.Generic.List<float>();float start=Time.time;
                while(battle.CurrentPhase==Story1WreckBattle.Phase.Breaking)
                {
                    impactTimes.Add(Time.time-start);Shot(Path.Combine("impact-frames",(impactTimes.Count-1).ToString("D4")));
                    if(battle.ShipFragmentCount>0 && impactTimes.Count<20)Shot("impact");
                    await Task.Delay(33);
                }
                File.WriteAllText(Path.GetFullPath(".codex/shark-finale-preview/impact-times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(impactTimes));
            }
            for(int i=0;i<300 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(30);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Break did not restore player.");
            Require(battle.ShipFragmentCount==202 && battle.Effects.ImpactCount>0,"Ship contact VFX or real fragments missing.");
            hp.GrantProtection(60);
            float deadline=battle.FinaleAfterSeconds;
            Require(lowHealth ? deadline==120 : deadline>=(quick ? 6 : 20) && deadline<=(quick ? 9 : 35),"Deadline is outside its configured range.");
            // Shark thresholds do not start the movie; changing configuration must not resample the deadline.
            battle.minimumFightSeconds=battle.maximumFightSeconds=1;hp.currentHealth=hp.maxHealth;
            battle.SharkHealth.ApplyDamage(360*.69f,battle.SharkHealth.transform.position);
            await Task.Delay(1000);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting && battle.FinaleAfterSeconds==deadline,"Deadline was resampled during the fight.");
            Require(battle.SharkRecoilCount>0,"Damaged shark did not recoil.");
            float pausedAt=battle.FightElapsed;
            Time.timeScale=0;await Task.Delay(600);
            Require(battle.FightElapsed==pausedAt,"Paused gameplay still consumed fight time.");Time.timeScale=1;
            if(fromWater)
            {
                var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
                Vector3 at=hero.transform.position+hero.transform.right*45;
                at.y=ocean.SampleSurfaceHeight(at,Time.time)-2-hero.LowestFootWorldOffset;
                hero.RestoreSavedPose(at,hero.transform.rotation);Physics.SyncTransforms();await Task.Delay(400);
                Require(hero.Swimming,"Water-entry test did not swim.");
            }
            battle.SharkHealth.ApplyDamage(360*.02f,battle.SharkHealth.transform.position);
            await Task.Delay(400);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"29% health ended the timed live fight early.");
            if(lowHealth)
            {
                float threshold=hp.maxHealth*battle.finalePlayerHealthRatio;
                hp.currentHealth=threshold+1;
                await Task.Delay(250);
                Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Player health above the threshold ended combat early.");
                hp.GrantProtection(0);hp.ApplyDamage(1,hero.transform.position);
            }
            for(int i=0;i<900 && battle.Finale==null;i++)await Task.Delay(50);
            var film=battle.Finale;
            Require(film!=null && battle.CurrentPhase==Story1WreckBattle.Phase.Finale,"Finale did not start.");
            if(lowHealth)Require(battle.FightElapsed<deadline && Mathf.Approximately(hp.currentHealth,hp.maxHealth*battle.finalePlayerHealthRatio),"Low health did not start the finale ahead of the deadline; hp="+hp.currentHealth+", elapsed="+battle.FightElapsed+", deadline="+deadline);
            else Require(battle.FightElapsed>=deadline && battle.FightElapsed-deadline<.3f,"The cinematic missed the random deadline.");
            Require(!hero.enabled && hero.ExternalControlLock && !hero.GetComponent<Story1WreckRider>().IsSurfing,"Finale did not release input and surfing.");
            string dir=Path.GetFullPath(".codex/shark-finale-preview/frames");Directory.CreateDirectory(dir);
            int frame=0;
            var times=new System.Collections.Generic.List<float>();
            while(!film.IsComplete)
            {
                Require(hp.currentHealth>0,"Hero died before the finishing animation.");
                if(capture){times.Add(film.Elapsed);Shot(Path.Combine("frames",(frame++).ToString("D4")));}
                await Task.Delay(capture ? 33 : 50);
            }
            if(capture)File.WriteAllText(Path.GetFullPath(".codex/shark-finale-preview/frame-times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(times));
            Require(film.HeroCounterCount==4 && film.SharkCounterCount==3 && film.HasLostStick,"Finale omitted an exchange or lost stick.");
            Require(hp.currentHealth==0 && !PlayerDeathRespawn.IsOpen && Time.timeScale==1,"Finishing blow failed or opened respawn.");
            int counters=film.HeroCounterCount, sharkCounters=film.SharkCounterCount;
            for(int i=0;i<160 && UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>()==null;i++)await Task.Delay(25);
            var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
            Require(arrival!=null && !arrival.HasStick && !arrival.IsAwake,"Finale did not hand off to unconscious, unarmed island arrival.");
            if(arrival.player.GetComponent<GameSaveExcluded>()==null)arrival.player.gameObject.AddComponent<GameSaveExcluded>();
            Require(AudioListener.volume==0,"Scene transition restored sound effects.");
            var result=new{fromWater,realHullFragments=202,contactVfx=true,randomDeadline=deadline,sharkHealthDoesNotEndFightEarly=true,
                lowPlayerHealthStartsFinale=lowHealth,timerStartsFinale=!lowHealth,heroCounters=counters,sharkCounters,lostStick=true,actualLethalFinish=true,soundMutedAfterSceneChange=true,
                noRespawn=true,unconsciousUnarmedIsland=true,previewFrames=frame};
            Directory.CreateDirectory(Path.GetFullPath(".codex/wreck-water-audio-timer"));
            File.WriteAllText(Path.GetFullPath(".codex/wreck-water-audio-timer/finale-verification.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        finally
        {
            Time.timeScale=1;
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null && actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
    static void Shot(string name)
    {
        string path=Path.GetFullPath(".codex/shark-finale-preview/"+name+".png");Directory.CreateDirectory(Path.GetDirectoryName(path));
        var camera=Camera.main;var target=new RenderTexture(720,405,24);var frame=new Texture2D(720,405,TextureFormat.RGB24,false);
        var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,720,405),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
}
