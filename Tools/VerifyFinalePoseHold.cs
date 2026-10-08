using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

// Observe after the finale's LateUpdate, so measurements match the rendered pose.
[DefaultExecutionOrder(2000)]
public sealed class FinalePoseObserver : MonoBehaviour
{
    public Story1WreckFinale film;
    public Story1WreckBattle battle;
    public Animator animator;
    public string label;
    public int frames, uprightFrames, pausedFrames;
    public float maxVerticality, endClock, maxClockDrift;
    public bool defeat, finished;
    public bool waveCheck;
    public OceanWorld ocean;
    public float minimumHeadClearance=float.PositiveInfinity,minimumChestY=float.PositiveInfinity,maximumChestY=float.NegativeInfinity;
    public float maxRockAngle,maxPausedMotion;
    Quaternion firstRotation,lastRotation;
    Vector3 lastChest;
    bool wasPaused;
    float start=-1;
    void LateUpdate()
    {
        if(film==null || animator==null || !film.IsComplete)return;
        if(start<0){start=Time.unscaledTime;endClock=film.Elapsed;firstRotation=film.transform.rotation;}
        var axis=animator.GetBoneTransform(HumanBodyBones.Head).position-animator.GetBoneTransform(HumanBodyBones.Hips).position;
        float verticality=Mathf.Abs(Vector3.Dot(axis.normalized,Vector3.up));
        maxVerticality=Mathf.Max(maxVerticality,verticality);frames++;
        if(verticality>.55f)uprightFrames++;
        if(Time.timeScale==0)pausedFrames++;
        maxClockDrift=Mathf.Max(maxClockDrift,Mathf.Abs(film.Elapsed-endClock));
        defeat|=battle.CurrentPhase==Story1WreckBattle.Phase.Defeat;
        finished|=battle.CurrentPhase==Story1WreckBattle.Phase.Finished;
        if(frames==5)Capture(label+"-after-completion.png");
        if(waveCheck)
        {
            Vector3 chest=animator.GetBoneTransform(HumanBodyBones.Chest).position;
            Vector3 head=animator.GetBoneTransform(HumanBodyBones.Head).position;
            var rotation=animator.transform.root.rotation;
            if(frames==1)firstRotation=rotation;
            minimumChestY=Mathf.Min(minimumChestY,chest.y);maximumChestY=Mathf.Max(maximumChestY,chest.y);
            minimumHeadClearance=Mathf.Min(minimumHeadClearance,head.y-ocean.SampleSurfaceHeight(head,Time.time));
            maxRockAngle=Mathf.Max(maxRockAngle,Quaternion.Angle(firstRotation,rotation));
            if(Time.timeScale==0 && wasPaused)maxPausedMotion=Mathf.Max(maxPausedMotion,
                Mathf.Max(Vector3.Distance(chest,lastChest),Quaternion.Angle(rotation,lastRotation)));
            wasPaused=Time.timeScale==0;lastChest=chest;lastRotation=rotation;
            if(frames%4==0)Capture(label+"-"+(frames/4).ToString("D4")+".png");
        }
    }
    void Capture(string name)
    {
        var camera=Camera.main;var target=new RenderTexture(960,540,24);var texture=new Texture2D(960,540,TextureFormat.RGB24,false);
        var old=camera.targetTexture;var active=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,960,540),0,0);texture.Apply();
            File.WriteAllBytes(Path.GetFullPath(".codex/finale-pose-hold-20261007/"+name),texture.EncodeToPNG());
        }
        finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();DestroyImmediate(target);DestroyImmediate(texture);}
    }
}
public static class VerifyFinalePoseHold
{
    public static async Task<object> Verify(string label="after",bool enforce=true,bool waves=false)
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        GameObject probe=null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);await Task.Delay(350);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var hero=sequence.sahur;hero.gameObject.AddComponent<GameSaveExcluded>();
            var battle=sequence.GetComponent<Story1WreckBattle>();battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.BeginImpact();
            for(int i=0;i<350 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(30);
            if(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting)throw new Exception("Ship break did not finish.");
            var hp=hero.GetComponent<PlayerHealth>();hp.GrantProtection(100);hp.currentHealth=hp.maxHealth*battle.finalePlayerHealthRatio;
            for(int i=0;i<100 && battle.Finale==null;i++)await Task.Delay(30);
            if(battle.Finale==null)throw new Exception("Low health did not start finale.");
            probe=new GameObject("Finale completion pose verification");UnityEngine.Object.DontDestroyOnLoad(probe);
            var observer=probe.AddComponent<FinalePoseObserver>();observer.film=battle.Finale;observer.battle=battle;observer.animator=hero.CharacterAnimator;observer.label=label;
            observer.waveCheck=waves;observer.ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            for(int i=0;i<1400 && !battle.Finale.IsComplete;i++)await Task.Delay(25);
            if(!battle.Finale.IsComplete)throw new Exception("Finale never completed.");
            await Task.Delay(250);Time.timeScale=0;await Task.Delay(350);Time.timeScale=1;
            int heroCounters=battle.Finale.HeroCounterCount,sharkCounters=battle.Finale.SharkCounterCount;
            int jumps=battle.Finale.BoardJumpCount,breaches=battle.Finale.SharkBreachCount;
            float landingError=battle.Finale.LargestLandingError,breachClearance=battle.Finale.LargestBreachClearance;
            var contactGaps=battle.Finale.ContactGaps.ToArray();
            float contactGap=battle.Finale.LargestContactGap;
            bool lostStick=battle.Finale.HasLostStick,noRespawn=!PlayerDeathRespawn.IsOpen,lethal=hp.currentHealth==0;
            for(int i=0;i<260 && UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>()==null;i++)await Task.Delay(25);
            var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
            bool handoff=arrival!=null && !arrival.HasStick && !arrival.IsAwake;
            if(arrival!=null && arrival.player.GetComponent<GameSaveExcluded>()==null)arrival.player.gameObject.AddComponent<GameSaveExcluded>();
            var result=new{label,observer.frames,observer.uprightFrames,observer.maxVerticality,observer.pausedFrames,
                observer.maxClockDrift,observer.defeat,observer.finished,heroCounters,sharkCounters,lostStick,noRespawn,lethal,
                unconsciousUnarmedIsland=handoff,soundMuted=AudioListener.volume==0,waves,jumps,breaches,landingError,breachClearance,contactGaps,contactGap,
                chestHeightRange=waves ? observer.maximumChestY-observer.minimumChestY : 0,
                observer.maxRockAngle,minimumHeadClearance=waves ? observer.minimumHeadClearance : 0,observer.maxPausedMotion};
            File.WriteAllText(Path.GetFullPath(".codex/finale-pose-hold-20261007/"+label+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(enforce && (observer.frames<60 || observer.uprightFrames!=0 || observer.pausedFrames<5 || observer.maxClockDrift!=0 || !observer.defeat || !observer.finished || !handoff || !noRespawn || !lethal || !lostStick || heroCounters!=3 || sharkCounters!=3))
                throw new Exception("Finale end regression failed: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
            if(waves && (result.chestHeightRange<.015f || observer.maxRockAngle<.02f || observer.minimumHeadClearance<.025f || observer.maxPausedMotion>.001f))
                throw new Exception("Wave drift regression failed: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
            if(enforce && (jumps<2 || breaches!=2 || landingError>.05f || breachClearance<1 || contactGap>.3f))
                throw new Exception("Traversal regression failed: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        finally
        {
            Time.timeScale=1;if(probe!=null)UnityEngine.Object.Destroy(probe);
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null && actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
}
