using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEditor;
using Mavis;
public static class ReviewCombatV245
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Existing Editor Play required");
        string folder=".codex/combat-recovery-20261023/"+label;
        if(Directory.Exists(folder))throw new Exception("Preserve prior take");Directory.CreateDirectory(folder);
        var go=new GameObject("Temporary combat review v245");var review=go.AddComponent<CombatReviewV245>();review.folder=folder;
        return new{folder};
    }
}
[DefaultExecutionOrder(950)]
public sealed class CombatReviewV245:MonoBehaviour
{
    public string folder;
    Camera view;GameObject player,boss;ThirdPersonPlayerController motor;PlayerHealth health;SahurAttack attack;SahurWeaponLoadout loadout;
    RenderTexture rt;Texture2D pixels;int frame;bool recording,left,right;Key[] keys=Array.Empty<Key>();
    Keyboard keyboard,oldKeyboard;Mouse mouse,oldMouse;
    int oldRate,oldSync;float oldDelta;string beat;
    readonly List<object> events=new List<object>();readonly List<object> rows=new List<object>();
    async Task Wait(float seconds){float end=Time.time+seconds;var limit=DateTime.UtcNow.AddSeconds(25);while(Time.time<end){if(DateTime.UtcNow>limit)throw new Exception("Native review stalled");await Task.Delay(5);}}
    async Task Click(){left=true;await Wait(.09f);left=false;}
    async void Start()
    {
        oldRate=Application.targetFrameRate;oldSync=QualitySettings.vSyncCount;oldDelta=Time.captureDeltaTime;
        try
        {
            CombatFeelPreview.Spawn();player=CombatFeelPreview.Player;boss=CombatFeelPreview.Target;view=CombatFeelPreview.Camera;
            motor=player.GetComponent<ThirdPersonPlayerController>();attack=player.GetComponent<SahurAttack>();loadout=player.GetComponent<SahurWeaponLoadout>();
            health=player.GetComponent<PlayerHealth>();if(!health)health=player.AddComponent<PlayerHealth>();
            health.currentHealth=health.maxHealth=100;health.NarrativeHealthFloor=0;
            oldKeyboard=Keyboard.current;oldMouse=Mouse.current;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
            var input=player.GetComponent<PlayerInput>();InputUser.PerformPairingWithDevice(keyboard,input.user);InputUser.PerformPairingWithDevice(mouse,input.user);input.ActivateInput();
            InputSystem.onBeforeUpdate+=Inject;CombatHitFeedback.HitConfirmed+=Hit;
            await Wait(.7f);Time.captureDeltaTime=1f/30;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            rt=new RenderTexture(960,540,24);rt.Create();pixels=new Texture2D(960,540,TextureFormat.RGB24,false);view.targetTexture=rt;
            RenderPipelineManager.endCameraRendering+=Rendered;recording=true;
            beat="Club: charged heavy";right=true;await Wait(2);right=false;await Wait(1.6f);
            beat="Club: real weapon contact";
            for(int i=0;i<16;i++){await Click();await Wait(.10f);}await Wait(1.0f);
            beat="Player: received boss impact";
            var point=player.GetComponent<CharacterController>().bounds.center;
            health.ApplyAttackDamage(30,point,boss.transform.position);await Wait(.9f);
            beat="Player: repeated volley protection";
            for(int i=0;i<4;i++){health.ApplyAttackDamage(30,point,boss.transform.position);await Wait(.14f);}await Wait(.7f);
            beat="Twin blades: normal combo";loadout.Restore(true,SahurWeaponLoadout.Weapon.CapriTwinBlades);await Wait(.5f);
            for(int i=0;i<18;i++){await Click();await Wait(.10f);}await Wait(1.3f);
            beat="Twin blades: received hit, then regain control";health.ApplyAttackDamage(20,point,boss.transform.position);await Wait(.8f);await Click();await Wait(1.6f);
            beat="Dodge after recovery";keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Dodge)};await Wait(.08f);keys=Array.Empty<Key>();await Wait(.9f);
            beat="Recovery complete";await Wait(.7f);
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,fps=30,health=health.currentHealth,events,rows,scope="Existing Editor native input, real weapon sweeps and actual received damage. Silent visual review; capture simulation Hz is not hardware FPS."},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(folder+"/error.txt",e.ToString());}
        finally{Finish();}
    }
    void Hit(CombatHitEvent e)=>events.Add(new{frame,beat,kind=e.kind.ToString(),result=e.result.ToString(),damage=e.damage,target=e.target?e.target.name:"missing",point=new[]{e.point.x,e.point.y,e.point.z}});
    void Inject()
    {
        if(keyboard==null||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();mouse.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        InputState.Change(keyboard,new KeyboardState(keys));InputState.Change(mouse,new MouseState().WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
    }
    void LateUpdate()
    {
        if(!recording||!player||!view)return;
        // Keep a readable side-on view while inputs/weapon sweeps remain real.
        // Keep the actual follow camera and its combat aiming convention.
        // A forced side camera changed attack facing in the first rejected take.
        var reaction=player.GetComponent<CombatReceivedReaction>();
        rows.Add(new{frame,beat,health=health.currentHealth,attack=attack.CurrentComboStage,phase=attack.CurrentAttackPhase,receivedWeight=reaction?reaction.Weight:0,playerStopped=CombatLocalHitStop.Active(player),bossStopped=CombatLocalHitStop.Active(boss),protection=health.RecoveryRemaining,scale=Time.timeScale});
    }
    void Rendered(ScriptableRenderContext context,Camera camera)
    {
        if(!recording||camera!=view)return;
        if(frame%2==0)
        {
            var active=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply(false);RenderTexture.active=active;
            File.WriteAllBytes(folder+"/frame-"+(frame/2).ToString("D4")+".png",pixels.EncodeToPNG());
        }
        frame++;
        if(frame%30==0)File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,beat}));
    }
    void Finish()
    {
        recording=false;RenderPipelineManager.endCameraRendering-=Rendered;InputSystem.onBeforeUpdate-=Inject;CombatHitFeedback.HitConfirmed-=Hit;
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
        oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();
        if(view)view.targetTexture=null;if(rt){rt.Release();DestroyImmediate(rt);}if(pixels)DestroyImmediate(pixels);
        Time.captureDeltaTime=oldDelta;Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldSync;
        CombatFeelPreview.Clear();Destroy(gameObject);
    }
}
