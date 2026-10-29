using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Editor-evaluated play session: uses real Input System frames and the real island colliders.
public static class PlaySecondIslandMarshmallow
{
    static Keyboard keyboard, oldKeyboard;
    static Mouse mouse, oldMouse;
    static Key[] keys = Array.Empty<Key>();
    static ThirdPersonPlayerController player;
    static Task pending;
    static string phase;
    public static string Status = "idle";
    static readonly List<object> samples = new List<object>();
    static readonly List<object> flights = new List<object>();
    static float launchedAt, launchY, peakY, previousVertical;
    static bool inFlight;
    static int screenshot;
    static int observedLandings, landingScreenshots;
    static float landingCaptureAt = -1;
    static MarshmallowIslandExperience experience;
    public static object Summary;
    const string Folder = "Temp/MarshmallowExperience/Playthrough";
    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); mouse.MakeCurrent();
        Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(keys));
        InputState.Change(mouse, new MouseState());
    }
    static async Task Play(string label, float seconds, params Key[] held)
    {
        phase = label; Status = label; keys = held;
        float until = Time.time + seconds;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (Time.time < until)
        {
            if (!Application.isPlaying) throw new Exception("Play mode stopped");
            if (DateTime.UtcNow > deadline) throw new Exception("Play frames stalled");
            float v = player.VerticalSpeed;
            if (!inFlight && previousVertical <= 0 && v > 1f)
            { inFlight = true; launchedAt = Time.time; launchY = peakY = player.transform.position.y; }
            if (inFlight)
            {
                peakY = Mathf.Max(peakY, player.transform.position.y);
                if (previousVertical > 0 && v <= 0 && screenshot < 8)
                    ScreenCapture.CaptureScreenshot(Folder + "/apex-" + (++screenshot) + ".png");
                if (player.GetComponent<CharacterController>().isGrounded && v < 0)
                {
                    flights.Add(new { phase, duration = Time.time - launchedAt, rise = peakY - launchY });
                    inFlight = false;
                }
            }
            previousVertical = v;
            var p = player.transform.position; var velocity = player.WorldVelocity;
            var visual = player.transform.Find("Pbr Sahur Visual");
            if(experience.Landings != observedLandings)
            {
                observedLandings=experience.Landings;
                if(landingScreenshots<6) landingCaptureAt=Time.time+.10f;
            }
            if(landingCaptureAt>0 && Time.time>=landingCaptureAt)
            {
                ScreenCapture.CaptureScreenshot(Folder+"/landing-"+(++landingScreenshots)+".png");
                landingCaptureAt=-1;
            }
            samples.Add(new { phase, t = Time.time, position = new[] {p.x,p.y,p.z}, vertical = v,
                planar = new Vector2(velocity.x,velocity.z).magnitude,
                grounded = player.GetComponent<CharacterController>().isGrounded,
                visualScale = visual.localScale.y, swimming = player.Swimming,
                frameTime=Time.deltaTime,pressure=experience.Pressure,impressions=experience.ActiveImpressions,
                visualSink=experience.VisualSink,
                particles=experience.GetComponentsInChildren<ParticleSystem>().Sum(s=>s.particleCount),
                cameraOffset=experience.CameraOffset,cameraPullback=experience.CameraPullback,
                landings=experience.Landings,takeoffs=experience.Takeoffs,sounds=experience.SoundsPlayed });
            await Task.Delay(16);
        }
    }
    public static object Start()
    {
        if(pending!=null && !pending.IsCompleted) return "Playthrough already running";
        pending = Run(); return "Native island playthrough started";
    }
    static void Check(bool ok,string label) { if(!ok)throw new Exception(label); }
    static async Task Run()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            samples.Clear(); flights.Clear(); inFlight = false; previousVertical = 0f; screenshot = 0;
            observedLandings=landingScreenshots=0;landingCaptureAt=-1;Summary=null;
            player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if (!player.GetComponent<MarshmallowTraversal>().Active) throw new Exception("Soft traversal inactive");
            experience=player.GetComponent<MarshmallowIslandExperience>();
            Check(experience && experience.Ready,"Experience prewarmed");
            oldKeyboard = Keyboard.current; oldMouse = Mouse.current;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.onBeforeUpdate += Inject;
            await Play("settle", .6f);
            await Play("ground-roll-press", .1f, GameInputSettings.Get(GameInputSettings.Action.Dodge));
            await Play("ground-roll-recover", 1.1f);
            await Play("walk-forward", 5f, GameInputSettings.Get(GameInputSettings.Action.Forward));
            await Play("steer-right", 2f, GameInputSettings.Get(GameInputSettings.Action.Right));
            await Play("release-and-land", 2.5f);
            await Play("manual-jump-press", .1f, GameInputSettings.Get(GameInputSettings.Action.Jump));
            await Play("manual-jump-flight", 2.8f);
            await Play("sprint-back", 5f, GameInputSettings.Get(GameInputSettings.Action.Back), GameInputSettings.Get(GameInputSettings.Action.Sprint));
            await Play("stop-after-sprint", 2.5f);
            await Play("jump-for-flip", .1f, GameInputSettings.Get(GameInputSettings.Action.Jump));
            await Play("before-flip", .4f);
            await Play("air-flip-press", .1f, GameInputSettings.Get(GameInputSettings.Action.Dodge));
            await Play("air-flip-land", 3f);
            await Play("impressions-recover", 5f);
            Check(experience.Landings>=7 && experience.Takeoffs>=7,"Real jumps trigger linked feedback");
            Check(experience.PuffParticlesEmitted>=70 && experience.SoundsPlayed>=10,"Landings and lifts emit candy particles and sound");
            Check(experience.ActiveImpressions==0,"Footprints recover instead of accumulating");
            Check(experience.Pressure>.2f && experience.Pressure<.26f,"Standing gives a bounded soft depression");
            Check(experience.VisualSink>.18f && experience.VisualSink<.25f,"Visual soles follow the settled depression");
            Check(new Vector2(player.WorldVelocity.x,player.WorldVelocity.z).magnitude<.01f,"Release settles without sliding");
            float clock=Time.time,pressure=experience.Pressure;
            float previousTimeScale=Time.timeScale;
            try
            {
                Time.timeScale=0;await Task.Delay(250);
                Check(Mathf.Abs(Time.time-clock)<.001f && Mathf.Abs(experience.Pressure-pressure)<.001f,"Pause freezes surface recovery");
            }
            finally { Time.timeScale=previousTimeScale; }
            var profile=player.GetComponent<MarshmallowTraversal>();
            profile.enabled=false;
            Check(experience.Pressure==0 && experience.VisualSink==0 && experience.ActiveImpressions==0,"Disabling profile clears surface state");
            Check(!experience.GetComponentsInChildren<AudioSource>().Any(a=>a.name.StartsWith("Soft candy voice") && a.isPlaying),"Disabling profile stops its voices");
            profile.enabled=true;
            await Play("resume-profile", .4f);
            Check(experience.Pressure>.18f,"Reenabled profile resumes pressure");
            var shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(experience.surface.sharedMaterial.shader)
                .Concat(UnityEditor.ShaderUtil.GetShaderMessages(experience.fluffMaterial.shader))
                .Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray();
            Check(shaderErrors.Length==0,"Surface shader compiles");
            var usedClips=experience.GetComponentsInChildren<AudioSource>()
                .Where(s=>s.name.StartsWith("Soft candy voice") && s.clip).Select(s=>s.clip).Distinct().ToArray();
            Check(usedClips.Length>=2,"Compression, takeoff and landing use the prewarmed audio voices");
            float audioPeak=0;
            foreach(var clip in usedClips)
            {
                var audio=new float[clip.samples];clip.GetData(audio,0);
                audioPeak=Mathf.Max(audioPeak,audio.Max(v=>Mathf.Abs(v)));
            }
            Check(audioPeak>.01f && audioPeak<.5f,"Soft audio is present without clipping");
            Summary=new { passed=true,experience.Landings,experience.Takeoffs,experience.PuffParticlesEmitted,experience.SoundsPlayed,
                recovered=true,pauseAndResume=true,disableAndResume=true,audioPeak,shaderErrors };
            File.WriteAllText(Folder+"/verified-summary.json",Newtonsoft.Json.JsonConvert.SerializeObject(Summary,Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(Folder + "/playthrough.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { flights, samples }, Newtonsoft.Json.Formatting.Indented));
            Status = "completed";
        }
        catch(Exception e) { Status = "failed: " + e; File.WriteAllText(Folder + "/playthrough-error.txt", e.ToString()); }
        finally
        {
            InputSystem.onBeforeUpdate -= Inject;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            keyboard = null; mouse = null;
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
        }
    }
}


