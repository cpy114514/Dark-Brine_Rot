using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
[DefaultExecutionOrder(2500)]
public sealed class FinaleLateFrameCapture : MonoBehaviour
{
    Action pending;TaskCompletionSource<bool> completion;
    public Task Capture(Action action)
    {
        pending=action;completion=new TaskCompletionSource<bool>();return completion.Task;
    }
    void LateUpdate()
    {
        if(pending==null)return;
        var action=pending;var result=completion;pending=null;completion=null;
        try{action();result.SetResult(true);}catch(Exception error){result.SetException(error);}
    }
}
[DefaultExecutionOrder(2000)]
public sealed class FinaleWaterReview : MonoBehaviour
{
    public Story1WreckFinale film;
    public ThirdPersonPlayerController hero;
    public OceanWorld ocean;
    public float minimumHeadClearance=float.PositiveInfinity;
    public int samples;
    public float maximumSharkWoodPenetration;
    public float worstPenetrationTime;
    void LateUpdate()
    {
        if(film!=null)
        {
            var battle=film.GetComponent<Story1WreckBattle>();
            if(battle.WreckContact!=null)
            {
                float penetration=battle.WreckContact.CurrentPenetration();
                if(penetration>maximumSharkWoodPenetration){maximumSharkWoodPenetration=penetration;worstPenetrationTime=film.CinematicTime;}
            }
        }
        if(film==null || hero==null || film.CinematicTime<Story1WreckFinale.TimelineForBeat(13))return;
        var head=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Head).position;
        minimumHeadClearance=Mathf.Min(minimumHeadClearance,head.y-ocean.SampleSurfaceHeight(head,Time.time));samples++;
    }
}
public static class VerifyStory1WreckFinale
{
    static string previewRoot=".codex/shark-finale-preview",multiViewRoot=".codex/encounter-v001/multiview";
    static FinaleLateFrameCapture frameCapture;
    static Task ShotLate(string name)=>frameCapture.Capture(()=>Shot(name));
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
    public static async Task<object> Verify(bool fromWater=false, bool capture=true, bool quick=false, bool lowHealth=false,bool movingReview=false,string revision="")
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        previewRoot=string.IsNullOrEmpty(revision) ? ".codex/shark-finale-preview" : ".codex/encounter-"+revision+"/preview";
        multiViewRoot=string.IsNullOrEmpty(revision) ? ".codex/encounter-v001/multiview" : ".codex/encounter-"+revision+"/multiview";
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);await Task.Delay(400);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();
            frameCapture=battle.gameObject.AddComponent<FinaleLateFrameCapture>();
            var hero=sequence.sahur;var hp=hero.GetComponent<PlayerHealth>();
            if(quick){battle.minimumFightSeconds=6;battle.maximumFightSeconds=9;}
            if(lowHealth)battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.BeginImpact();
            if(capture)
            {
                Directory.CreateDirectory(Path.GetFullPath(previewRoot+"/impact-frames"));
                var impactTimes=new System.Collections.Generic.List<float>();float start=Time.time;
                while(battle.CurrentPhase==Story1WreckBattle.Phase.Breaking)
                {
                    await ShotLate(Path.Combine("impact-frames",impactTimes.Count.ToString("D4")));impactTimes.Add(Time.time-start);
                    if(battle.ShipFragmentCount>0 && impactTimes.Count<20)await ShotLate("impact");
                    await Task.Delay(33);
                }
                File.WriteAllText(Path.GetFullPath(previewRoot+"/impact-times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(impactTimes));
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
            var waterReview=battle.gameObject.AddComponent<FinaleWaterReview>();waterReview.film=film;waterReview.hero=hero;waterReview.ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            if(lowHealth)Require(battle.FightElapsed<deadline && Mathf.Approximately(hp.currentHealth,hp.maxHealth*battle.finalePlayerHealthRatio),"Low health did not start the finale ahead of the deadline; hp="+hp.currentHealth+", elapsed="+battle.FightElapsed+", deadline="+deadline);
            else Require(battle.FightElapsed>=deadline && battle.FightElapsed-deadline<.3f,"The cinematic missed the random deadline.");
            Require(!hero.enabled && hero.ExternalControlLock && !hero.GetComponent<Story1WreckRider>().IsSurfing,"Finale did not release input and surfing.");
            await Task.Delay(100);
            float pausedFilm=film.Elapsed;Quaternion pausedArm=hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm).localRotation;
            Time.timeScale=0;await Task.Delay(350);
            Require(film.Elapsed==pausedFilm && Quaternion.Angle(pausedArm,hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm).localRotation)<.05f,"Paused paired animation advanced or changed its arm pose; elapsed="+film.Elapsed+", before="+pausedFilm+", arm="+Quaternion.Angle(pausedArm,hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm).localRotation));
            Time.timeScale=1;
            string dir=Path.GetFullPath(".codex/shark-finale-preview/frames");Directory.CreateDirectory(dir);
            int frame=0;
            var times=new System.Collections.Generic.List<float>();
            float[] views={1.7f,4.2f,5.5f,6.8f,8,11.9f,12.8f,15.3f,17.95f,18.7f};int viewIndex=0;
            var reviewTimes=new System.Collections.Generic.List<float>();float lastReview=-1;
            float minimumFloatHeadClearance=float.PositiveInfinity;
            while(!film.IsComplete)
            {
                Require(hp.currentHealth>0,"Hero died before the finishing animation.");
                if(capture){await ShotLate(Path.Combine("frames",(frame++).ToString("D4")));times.Add(film.Elapsed);}
                if(capture && viewIndex<views.Length && film.Elapsed-film.LeadInSeconds+.6f>=views[viewIndex])
                {
                    MultiView(hero,sequence.swimmer.GetComponent<TralaleroSwimAnimator>(),views[viewIndex]);viewIndex++;
                }
                if(capture && movingReview && film.Elapsed-lastReview>=.12f)
                {
                    lastReview=film.Elapsed;reviewTimes.Add(film.Elapsed);
                    MultiView(hero,sequence.swimmer.GetComponent<TralaleroSwimAnimator>(),film.Elapsed,reviewTimes.Count-1);
                }
                await Task.Delay(capture ? 33 : 50);
            }
            if(capture)File.WriteAllText(Path.GetFullPath(previewRoot+"/frame-times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(times));
            if(movingReview)File.WriteAllText(Path.GetFullPath(multiViewRoot+"/times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(reviewTimes));
            Require(film.LargestContactGap<.3f && film.LargestContactCorrection==0,"The shark missed actual contact or snapped into the hit pose; gap="+film.LargestContactGap);
            minimumFloatHeadClearance=waterReview.minimumHeadClearance;
            Require(waterReview.samples>5,"No actual rendered floating frames were observed.");
            Require(film.HeroCounterCount==3 && film.SharkCounterCount==3 && film.HasLostStick,"Finale omitted an exchange or lost stick.");
            Require(film.BoardJumpCount>=2 && film.SharkBreachCount==2 && film.LargestLandingError<.05f && film.LargestBreachClearance>1,"Cross-board hops or visible breaches failed.");
            Require(minimumFloatHeadClearance>0,"Defeated hero's head sank below the waterline: "+minimumFloatHeadClearance);
            float maximumSharkWoodPenetration=waterReview.maximumSharkWoodPenetration;
            int brokenWood=battle.BrokenBoardCount;
            Require(maximumSharkWoodPenetration<.08f,"Cinematic shark still penetrates unbroken wood: "+maximumSharkWoodPenetration+" at "+waterReview.worstPenetrationTime);
            Require(hp.currentHealth==0 && !PlayerDeathRespawn.IsOpen && Time.timeScale==1,"Finishing blow failed or opened respawn.");
            int counters=film.HeroCounterCount, sharkCounters=film.SharkCounterCount;
            for(int i=0;i<260 && UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>()==null;i++)await Task.Delay(25);
            var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
            Require(arrival!=null && !arrival.HasStick && !arrival.IsAwake,"Finale did not hand off to unconscious, unarmed island arrival.");
            if(arrival.player.GetComponent<GameSaveExcluded>()==null)arrival.player.gameObject.AddComponent<GameSaveExcluded>();
            Require(AudioListener.volume==0,"Scene transition restored sound effects.");
            var result=new{fromWater,realHullFragments=202,contactVfx=true,randomDeadline=deadline,sharkHealthDoesNotEndFightEarly=true,
                lowPlayerHealthStartsFinale=lowHealth,timerStartsFinale=!lowHealth,heroCounters=counters,sharkCounters,lostStick=true,actualLethalFinish=true,soundMutedAfterSceneChange=true,
                noRespawn=true,unconsciousUnarmedIsland=true,previewFrames=frame,
                contactGap=film.LargestContactGap,contactCorrection=film.LargestContactCorrection,film.LeadInSeconds,pairedPausePreservesPose=true,minimumFloatHeadClearance,maximumSharkWoodPenetration,brokenWood,
                jumps=film.BoardJumpCount,breaches=film.SharkBreachCount,landingError=film.LargestLandingError,breachClearance=film.LargestBreachClearance,contactGaps=film.ContactGaps.ToArray()};
            Directory.CreateDirectory(Path.GetFullPath(".codex/wreck-water-audio-timer"));
            File.WriteAllText(Path.GetFullPath(".codex/wreck-water-audio-timer/finale-verification.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result));
            if(!string.IsNullOrEmpty(revision))
            {
                Directory.CreateDirectory(Path.GetFullPath(".codex/encounter-"+revision));
                File.WriteAllText(Path.GetFullPath(".codex/encounter-"+revision+"/verification.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            }
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
        string path=Path.GetFullPath(previewRoot+"/"+name+".png");Directory.CreateDirectory(Path.GetDirectoryName(path));
        var camera=Camera.main;var target=new RenderTexture(720,405,24);var frame=new Texture2D(720,405,TextureFormat.RGB24,false);
        var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,720,405),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
    static void MultiView(ThirdPersonPlayerController hero,TralaleroSwimAnimator shark,float t,int index=-1)
    {
        // Capture the actual evaluated skin and weapon, with no foreground waves hiding a foot/contact.
        var root=new GameObject("Isolated encounter pose review");var meshes=new System.Collections.Generic.List<Mesh>();
        void Clone(Renderer source)
        {
            Mesh mesh;
            // Compensate for inherited scale before copying the source world scale.
            // Otherwise Sahur is tripled while his static weapon remains correct.
            if(source is SkinnedMeshRenderer skin){mesh=new Mesh();skin.BakeMesh(mesh,true);meshes.Add(mesh);}
            else{var filter=source.GetComponent<MeshFilter>();if(filter==null)return;mesh=filter.sharedMesh;}
            var part=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));part.layer=31;part.transform.SetParent(root.transform);
            part.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);part.transform.localScale=source.transform.lossyScale;
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterials=source.sharedMaterials;
        }
        foreach(var r in hero.GetComponentsInChildren<Renderer>())if(r.enabled && r.gameObject.activeInHierarchy)Clone(r);
        var heroParts=root.GetComponentsInChildren<Renderer>();
        Bounds heroBounds=heroParts[0].bounds;foreach(var part in heroParts)heroBounds.Encapsulate(part.bounds);
        Clone(shark.VisualRenderer);
        var board=(Story1WreckPlank)typeof(Story1WreckFinale).GetField("board",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .GetValue(UnityEngine.Object.FindFirstObjectByType<Story1WreckFinale>());Clone(board.GetComponent<Renderer>());
        var lightGo=new GameObject("Review key light");lightGo.transform.SetParent(root.transform);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.cullingMask=1<<31;lightGo.transform.rotation=Quaternion.Euler(35,-35,0);
        var go=new GameObject("Encounter review camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.17f);camera.nearClipPlane=.1f;camera.farClipPlane=100;
        Vector3 center=heroBounds.center;
        float reviewDistance=Mathf.Max(16,heroBounds.extents.magnitude*1.2f/Mathf.Tan(26*Mathf.Deg2Rad));
        try
        {
            foreach(var view in new[]{("front",hero.transform.forward), ("side",hero.transform.right), ("three-quarter",(hero.transform.forward+hero.transform.right).normalized)})
            {
                camera.transform.position=center+(view.Item2+Vector3.up*.3f).normalized*reviewDistance;camera.transform.LookAt(center);camera.fieldOfView=52;
                string path=Path.GetFullPath(multiViewRoot+"/"+view.Item1+"-"+(index<0 ? t.ToString("F2",System.Globalization.CultureInfo.InvariantCulture) : index.ToString("D4"))+".png");Directory.CreateDirectory(Path.GetDirectoryName(path));
                var target=new RenderTexture(960,540,24);var frame=new Texture2D(960,540,TextureFormat.RGB24,false);var active=RenderTexture.active;
                try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,960,540),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(root);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);}

    }
}
