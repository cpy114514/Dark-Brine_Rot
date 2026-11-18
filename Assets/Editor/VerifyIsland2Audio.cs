using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

[InitializeOnLoad]
public static class VerifyIsland2Audio
{
    const string Folder=".codex/island2-audio/playtest/",Flag="Island2Audio.Verify";
    static readonly List<object> checks=new List<object>();static readonly List<string> errors=new List<string>();
    static Keyboard keyboard,oldKeyboard;static bool walk;
    static VerifyIsland2Audio(){EditorApplication.playModeStateChanged+=Changed;}
    static void Write(string name,object value){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+name,Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));}
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit scene required");
        Write("saves-before.json",Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save*.json*").ToDictionary(p=>p,p=>Convert.ToBase64String(File.ReadAllBytes(p))));
        SessionState.SetFloat(Flag+".volume",AudioListener.volume);SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();return "Isolated Island 2 audio regression queued";
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,Path.GetFullPath(Folder+"isolated-save.json"));GameSaveManager.BeginEditorTestSession();EditorApplication.delayCall+=Run;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);AudioListener.volume=SessionState.GetFloat(Flag+".volume",1);var before=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Folder+"saves-before.json"));Write("preservation.json",new{formalSavesUnchanged=before.All(p=>File.Exists(p.Key)&&Convert.ToBase64String(File.ReadAllBytes(p.Key))==p.Value),scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,stopped=true});}
    }
    static async Task Wait(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until){if(!EditorApplication.isPlaying)throw new Exception("Play interrupted");await Task.Delay(20);}}
    static void Assert(bool value,string name){if(!value)throw new Exception(name);checks.Add(new{name,passed=true});}
    static void Log(string m,string t,LogType kind){if(kind==LogType.Exception||kind==LogType.Error||kind==LogType.Assert)errors.Add(m);}
    static void Inject(){if(keyboard!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(walk?new[]{Key.W}:Array.Empty<Key>()));}}
    static Vector3 Floor(Collider ground,Vector3 at){if(!ground.Raycast(new Ray(new Vector3(at.x,ground.bounds.max.y+10,at.z),Vector3.down),out var h,ground.bounds.size.y+20))throw new Exception("No test ground");return h.point;}
    static void Place(ThirdPersonPlayerController player,Collider ground,Vector3 at){player.ExternalControlLock=false;player.ExternalMovementLock=false;player.RestoreSavedPose(Floor(ground,at)+Vector3.up*(-player.LowestFootWorldOffset+.06f),Quaternion.identity);}
    static async Task Load()
    {
        var op=SceneManager.LoadSceneAsync(SecondIslandAudio.ScenePath);while(!op.isDone)await Wait(.05f);await Wait(1);
        var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.Destroy(opening.gameObject);await Wait(.15f);
    }
    static void Set(object obj,string property,object value)=>obj.GetType().GetProperty(property,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    static async void Run()
    {
        bool passed=false;string failure=null;checks.Clear();errors.Clear();Application.logMessageReceived+=Log;
        try{
            await Load();var audio=SecondIslandAudio.Current;Assert(audio,"Native soundscape loads");
            var sources=audio.GetComponentsInChildren<AudioSource>();Assert(sources.Length==10,"Bounded four beds plus six accent voices");
            Assert(sources.Where(s=>s.loop).All(s=>s.clip&&s.isPlaying&&s.clip.loadType==AudioClipLoadType.Streaming),"All four streaming beds play");
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();var experience=player.GetComponent<MarshmallowIslandExperience>();var ground=experience.surface.GetComponent<Collider>();
            Place(player,ground,new Vector3(-120,0,2050));await Wait(1.4f);float far=audio.RiverGain;
            var river=audio.riverPoints[audio.riverPoints.Length/2];Place(player,ground,river);await Wait(1.3f);float near=audio.RiverGain;
            Assert(near>far+.15f,"River bed gains rise with proximity");checks.Add(new{name="River attenuation",nearGain=near,farGain=far,distance=audio.DistanceFromRiver});
            var wet=SecondIslandAudio.Movement(0,river,null);var dry=SecondIslandAudio.Movement(0,Floor(ground,new Vector3(-120,0,2050)),null);
            Assert(wet&&wet.name.StartsWith("RiverStep")&&dry&&dry.name.StartsWith("CottonPress"),"Wet and dry foley select different recordings");
            int before=experience.SoundsPlayed;oldKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Inject;walk=true;await Wait(1.8f);walk=false;await Wait(.1f);
            Assert(experience.SoundsPlayed>before&&audio.MovementCuesResolved>=3,"Real W movement triggers revised foley");
            InputSystem.onBeforeUpdate-=Inject;InputSystem.RemoveDevice(keyboard);keyboard=null;if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
            Time.timeScale=0;await Wait(.2f);Assert(audio.BedsPaused&&sources.Where(s=>s.loop).All(s=>!s.isPlaying),"Paused world beds stop advancing");
            Time.timeScale=1;await Wait(.2f);Assert(!audio.BedsPaused&&sources.Where(s=>s.loop).All(s=>s.isPlaying),"Beds resume without recreation");
            AudioListener.volume=0;GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);Assert(AudioListener.volume==0,"Existing master mute governs cues");AudioListener.volume=SessionState.GetFloat(Flag+".volume",1);
            var profile=SecondIslandAudio.SoundProfile(CombatFeelProfile.Default);Assert(profile!=CombatFeelProfile.Default&&profile.contact.Length==3&&profile.flesh.Length==3,"Island-only confirmed-hit recording palette");
            var melody=UnityEngine.Object.FindFirstObjectByType<CandyMelodyGarden>();var tones=(AudioClip[])typeof(CandyMelodyGarden).GetField("tones",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(melody);
            Assert(tones.Select((c,i)=>c&&c.name=="Melody"+i).All(b=>b),"All four melody puzzle notes use authored recordings");
            var huang=UnityEngine.Object.FindFirstObjectByType<HuangMeiBoss>();var bajie=UnityEngine.Object.FindFirstObjectByType<ZhuBaJieBoss>();
            Assert(huang.GetComponent<SecondIslandBossAudio>()&&bajie.GetComponent<SecondIslandBossAudio>(),"Both boss presentation readers attached");
            huang.enabled=false;bajie.enabled=false;int cues=audio.WorldCuesPlayed;
            Set(huang,"FightActive",true);Set(huang,"CurrentState",HuangMeiBoss.State.Windup);Set(bajie,"CurrentState",ZhuBaJieBoss.State.Windup);await Wait(.2f);
            Assert(audio.BattleMixActive&&audio.WorldCuesPlayed>=cues+2,"Boss windups emit accents and duck ambience");
            Set(huang,"FightActive",false);Set(huang,"CurrentState",HuangMeiBoss.State.Idle);Set(bajie,"CurrentState",ZhuBaJieBoss.State.Idle);
            var originalScene=SceneManager.GetActiveScene();var probe=SceneManager.CreateScene("Audio fallback probe");SceneManager.SetActiveScene(probe);await Wait(.15f);
            var fallback=(AudioClip)typeof(GameplayAudio).GetMethod("LoadClip",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"Pickup"});
            Assert(!SecondIslandAudio.Clip("Pickup")&&AssetDatabase.GetAssetPath(fallback).StartsWith("Assets/Resources/GameplayAudio/"),"Leaving Island 2 restores original cue routing");
            Assert(audio.BedsPaused,"Additive non-island active scene suspends island ambience");SceneManager.SetActiveScene(originalScene);var unload=SceneManager.UnloadSceneAsync(probe);while(!unload.isDone)await Wait(.03f);await Wait(.15f);
            audio.gameObject.SetActive(false);await Wait(.05f);audio.gameObject.SetActive(true);await Wait(.15f);Assert(SecondIslandAudio.Current==audio&&audio.GetComponentsInChildren<AudioSource>().Length==10&&audio.GetComponentsInChildren<AudioSource>().Where(s=>s.loop).All(s=>s.isPlaying),"Disable and enable resume one bounded source set");
            var data=new float[1024];AudioListener.GetOutputData(data,0);await Wait(.3f);double rms=0;float peak=0;
            for(int i=0;i<8;i++){AudioListener.GetOutputData(data,0);rms=Math.Max(rms,Math.Sqrt(data.Average(x=>(double)x*x)));peak=Math.Max(peak,data.Max(x=>Math.Abs(x)));await Wait(.07f);}
            checks.Add(new{name="Warmed listener output samples",rms,peak,dspTime=AudioSettings.dspTime});Assert(peak>.0001f,"Listener receives non-silent mixed audio");
            await Load();Assert(SecondIslandAudio.Current&&SecondIslandAudio.Clip("MandalaBell")&&SecondIslandAudio.Clip("Melody0"),"Scene reload preserves shared bell and melody audio assets");
            passed=errors.Count==0;if(!passed)failure="Runtime error or exception captured";
        }catch(Exception e){failure=e.ToString();}
        finally{walk=false;Time.timeScale=1;AudioListener.volume=SessionState.GetFloat(Flag+".volume",1);InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();Application.logMessageReceived-=Log;Write("result.json",new{passed,failure,checks,errors});EditorApplication.ExitPlaymode();}
    }
}
