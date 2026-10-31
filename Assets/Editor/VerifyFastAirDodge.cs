using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Exercises real input against the active island motor, including its normal landing path.
public static class VerifyFastAirDodge
{
    public static string Status = "idle";
    public static object Summary;
    static Task pending;
    static Keyboard keyboard, oldKeyboard;
    static Mouse mouse, oldMouse;
    static Key[] held = Array.Empty<Key>();
    static ThirdPersonPlayerController player;
    static CharacterController body;
    const string Folder = "Temp/FastAirDodgeReview";
    static void Inject()
    {
        if (keyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent(); mouse.MakeCurrent(); Cursor.lockState = CursorLockMode.Locked;
        InputState.Change(keyboard, new KeyboardState(held));
        InputState.Change(mouse, new MouseState());
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Step(float seconds, params Key[] keys)
    {
        held = keys; float until = Time.time + seconds; DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        while (Time.time < until)
        {
            if (!Application.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Play stopped or stalled.");
            await Task.Delay(15);
        }
    }
    static async Task WaitFor(Func<bool> predicate, float seconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!predicate())
        {
            if (!Application.isPlaying || DateTime.UtcNow > deadline) throw new Exception("Timed out waiting for jump or landing.");
            await Task.Delay(15);
        }
    }
    public static void Start()
    {
        if (pending != null && !pending.IsCompleted) return;
        pending = Run();
    }
    static async Task<object> CheckFallingDodge(Key jump, Key dodge)
    {
        await Step(.4f);
        Check(body.isGrounded, "Player must be grounded before the falling-dodge check.");
        var stamina=player.GetComponent<Mavis.PlayerStamina>(); if(stamina)stamina.currentStamina=stamina.maxStamina;
        await Step(.1f,jump); held=Array.Empty<Key>();
        await WaitFor(()=>player.VerticalSpeed>1f && !body.isGrounded,3);
        await WaitFor(()=>player.VerticalSpeed < -2f && !body.isGrounded,6);
        float before=player.VerticalSpeed, height=player.transform.position.y;
        await Step(.1f,dodge); float after=player.VerticalSpeed;
        Check(body.isGrounded || after < before, "Falling dodge did not accelerate descent.");
        Check(player.transform.position.y <= height + .1f, "Falling dodge moved upward.");
        held=Array.Empty<Key>(); await WaitFor(()=>body.isGrounded && player.VerticalSpeed<=0,6);
        await Step(.4f);
        return new{beforeSpeed=before,afterSpeed=after,landed=body.isGrounded};
    }
    static async Task Run()
    {
        Directory.CreateDirectory(Folder); Summary = null;
        try
        {
            Status = "playing";
            player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            body = player ? player.GetComponent<CharacterController>() : null;
            Check(player && body && body.enabled, "Player controller unavailable.");
            oldKeyboard = Keyboard.current; oldMouse = Mouse.current;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.onBeforeUpdate += Inject;
            await Step(.6f); Check(body.isGrounded, "Player must start on the ground.");
            var visual=player.transform.Find("Pbr Sahur Visual");
            Quaternion restRotation=visual ? visual.localRotation : Quaternion.identity;
            var stamina=player.GetComponent<Mavis.PlayerStamina>(); if(stamina)stamina.currentStamina=stamina.maxStamina;
            Key jump=GameInputSettings.Get(GameInputSettings.Action.Jump), dodge=GameInputSettings.Get(GameInputSettings.Action.Dodge);
            await Step(.1f, jump); held=Array.Empty<Key>();
            await WaitFor(()=>player.VerticalSpeed>1f && !body.isGrounded, 3);
            await Step(.30f); float baselineStart=Time.time;
            await WaitFor(()=>body.isGrounded && player.VerticalSpeed<=0, 6);
            float baselineTime=Time.time-baselineStart;
            await Step(.9f);
            if(stamina)stamina.currentStamina=stamina.maxStamina;
            await Step(.1f, jump); held=Array.Empty<Key>();
            await WaitFor(()=>player.VerticalSpeed>1f && !body.isGrounded, 3);
            await Step(.30f); float beforeHeight=player.transform.position.y, beforeSpeed=player.VerticalSpeed;
            int groundRolls=player.DodgesPerformed; float fastStart=Time.time;
            await Step(.1f, dodge);
            float afterSpeed=player.VerticalSpeed, afterHeight=player.transform.position.y;
            ScreenCapture.CaptureScreenshot(Folder+"/fast-descent.png");
            Check(body.isGrounded || afterSpeed < -1f, "Air dodge did not start descending.");
            Check(afterHeight <= beforeHeight + .1f, "Air dodge added upward lift.");
            held=Array.Empty<Key>();
            await WaitFor(()=>body.isGrounded && player.VerticalSpeed<=0, 6);
            float fastTime=Time.time-fastStart;
            await Step(.35f);
            Check(player.DodgesPerformed==groundRolls, "Air dodge also triggered a ground roll.");
            float upright=visual ? Quaternion.Angle(visual.localRotation,restRotation) : 0f;
            Check(upright < 15f, "Visual did not recover upright after landing: "+upright);
            Check(fastTime < baselineTime*.75f, $"Fast descent was not substantially faster: {fastTime:F2} vs {baselineTime:F2} seconds.");
            await Step(.5f);
            int beforeRoll=player.DodgesPerformed;
            await Step(.1f,dodge); await Step(.1f);
            Check(player.DodgesPerformed==beforeRoll+1, "Ground dodge no longer starts normally.");
            await Step(1f);
            var falling=await CheckFallingDodge(jump,dodge);
            var soft=player.GetComponent<MarshmallowTraversal>();
            object normalGravity;
            try
            {
                if(soft)soft.enabled=false;
                normalGravity=await CheckFallingDodge(jump,dodge);
            }
            finally{if(soft)soft.enabled=true;}
            Summary=new{beforeSpeed,afterSpeed,baselineTime,fastTime,upright,groundRollsBefore=groundRolls,groundRollWorks=true,falling,normalGravity};
            Status="completed";
        }
        catch(Exception e){Status="failed: "+e;}
        finally
        {
            InputSystem.onBeforeUpdate-=Inject;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null)InputSystem.RemoveDevice(mouse);
            keyboard=null;mouse=null;held=Array.Empty<Key>();
            if(oldKeyboard!=null && oldKeyboard.added)oldKeyboard.MakeCurrent();
            if(oldMouse!=null && oldMouse.added)oldMouse.MakeCurrent();
            File.WriteAllText(Folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{Status,Summary},Newtonsoft.Json.Formatting.Indented));
        }
    }
}
