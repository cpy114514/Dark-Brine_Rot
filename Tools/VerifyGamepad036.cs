using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class VerifyGamepad036
{
    static string folder;
    static Gamepad pad;
    static Keyboard keyboard;
    static Mouse mouse;
    static Keyboard oldKeyboard;
    static Mouse oldMouse;
    static InputSettings oldSettings,temporarySettings;
    static readonly List<InputDevice> physical=new List<InputDevice>();
    static readonly List<object> checks=new List<object>();
    static readonly List<string> errors=new List<string>();
    static Vector2 stick,view;
    static GamepadButton[] pressed=Array.Empty<GamepadButton>();
    static Key[] keys=Array.Empty<Key>();
    static float rt,lt;
    static bool mouseDown,armed,running;
    static GameObject root,player,target;
    static GamepadContactProbe036 damageProbe;
    static float capture;
    static string testCampaign;
    static bool menuOnly;
    static object V(Vector3 v)=>new[]{v.x,v.y,v.z};
    public static object Start(string label,bool menus=false)
    {
        if(!Application.isPlaying||running)throw new Exception("Fresh sandbox Play required");
        folder=".codex/gamepad036/"+label;Directory.CreateDirectory(folder);
        menuOnly=menus;running=true;_ = Run();return new{started=folder};
    }
    static void Check(bool pass,string name)
    {
        checks.Add(new{name,pass,frame=Time.frameCount});WriteProgress(name);
        if(!pass)throw new Exception(name);
    }
    static void WriteProgress(string name)=>File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{name,checks=checks.Count,running}));
    static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
    static void Lifecycle(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();}
    static void Inject()
    {
        if(!running||InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        if(pad!=null&&pad.added)
        {
            var state=new GamepadState{leftStick=stick,rightStick=view,rightTrigger=rt,leftTrigger=lt};
            foreach(var b in pressed)state=state.WithButton(b);
            InputSystem.QueueStateEvent(pad,state);
        }
        if(keyboard!=null&&keyboard.added)InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        if(mouse!=null&&mouse.added)InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left,mouseDown));
    }
    static async Task Frames(int count)
    {
        int end=Time.frameCount+count;
        while(running&&Time.frameCount<end)await Task.Delay(5);
        if(!running)throw new OperationCanceledException();
    }
    static async Task Wait(float seconds)=>await Frames(Mathf.CeilToInt(seconds*60));
    static async Task Press(GamepadButton button,int frames=3)
    {pressed=new[]{button};await Frames(frames);pressed=Array.Empty<GamepadButton>();await Frames(2);}
    static void Neutral(){stick=view=Vector2.zero;rt=lt=0;pressed=Array.Empty<GamepadButton>();keys=Array.Empty<Key>();mouseDown=false;}
    static void InputStep(){InputSystem.Update();}
    static async Task Run()
    {
        string failure=null;
        try
        {
            checks.Clear();errors.Clear();Neutral();
            oldKeyboard=Keyboard.current;oldMouse=Mouse.current;oldSettings=InputSystem.settings;
            capture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;
            testCampaign=HealingPackSupply.Campaign;
            armed=true;EditorApplication.playModeStateChanged+=Lifecycle;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;EditorApplication.quitting+=Cleanup;
            temporarySettings=UnityEngine.Object.Instantiate(oldSettings);temporarySettings.hideFlags=HideFlags.DontSave;
            temporarySettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            temporarySettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings=temporarySettings;
            foreach(var d in InputSystem.devices.ToArray())if((d is Keyboard||d is Mouse||d is Gamepad)&&d.enabled){physical.Add(d);InputSystem.DisableDevice(d);}
            keyboard=InputSystem.AddDevice<Keyboard>("Controller verification keyboard");mouse=InputSystem.AddDevice<Mouse>("Controller verification mouse");
            pad=InputSystem.AddDevice<Gamepad>();InputSystem.onBeforeUpdate+=Inject;Application.logMessageReceived+=Log;
            InputStep();
            rt=1;InputStep();Check(GameControls.Down(GameControls.Command.Attack)&&GameControls.Held(GameControls.Command.Attack),"RT attack press/hold through Input Actions");
            rt=0;InputStep();Check(GameControls.Up(GameControls.Command.Attack),"RT release edge");
            lt=1;InputStep();Check(GameControls.Held(GameControls.Command.Charge),"LT charge/aim");lt=0;
            pressed=new[]{GamepadButton.LeftShoulder,GamepadButton.RightShoulder};InputStep();
            Check(GameInputSettings.Pressed(GameInputSettings.Action.Guard)&&GameControls.Down(GameControls.Command.Special),"Separate guard and special shoulders");
            Neutral();stick=new Vector2(.45f,0);InputStep();Check(GameControls.Move.x>0&&GameControls.Move.x<.8f,"Analog magnitude retained");
            Check(GameControls.UsingGamepad&&GameControls.Prompt(GameControls.Command.Jump)=="A","Xbox prompt family");
            Neutral();keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};InputStep();
            Check(!GameControls.UsingGamepad&&GameControls.Move.y==1,"Keyboard takeover and saved movement binding");
            Neutral();stick=new Vector2(.05f,.03f);InputStep();Check(!GameControls.UsingGamepad,"Idle stick drift does not switch hints");
            Neutral();view=Vector2.right;InputStep();
            Check(Mathf.Abs(GameControls.LookDelta(.12f,1f/30).x-GameControls.LookDelta(.12f,1f/60).x*2)<.001f,"Stick look scales with delta time");
            Neutral();pressed=new[]{GamepadButton.East};InputStep();
            Check(GameControls.AdaptPrompt(GameControls.Token(GameControls.Command.Dodge)+" Dodge")=="B Dodge","Typed prompt is resolved once");
            Check(GameControls.AdaptPrompt("LMB / RMB / F / I / Esc")=="RT / LT / X / D-Pad ↓ / B","Legacy help text changes with the device");
            Check(GameControls.AdaptPrompt("11.0 M")=="11.0 M"&&GameControls.AdaptPrompt("M / Esc to close")=="View / B to close","Distance units stay intact while actual map/back shortcuts change");
            Check(GameControls.AdaptPrompt("Left mouse: attack / Hold right mouse")=="RT: attack / Hold LT","Mixed-case mouse button hints change");
            Check(GameControls.AdaptPrompt("Move with your movement keys; look with the mouse.")=="Move with LS; look with RS.","Island exploration explains both sticks");
            Check(GameControls.AdaptPrompt("Skills / 1–4 to select")=="Skills / D-Pad ↓ to open","Skill hint opens Gear instead of claiming gameplay D-Pad selection");
            foreach(string layout in new[]{"DualShockGamepad_Test036","SwitchProGamepad_Test036"})
            {
                InputSystem.RemoveDevice(pad);
                InputSystem.RegisterLayout("{\"name\":\""+layout+"\",\"extend\":\"Gamepad\"}");
                pad=(Gamepad)InputSystem.AddDevice(layout);Neutral();pressed=new[]{GamepadButton.South};InputStep();
                Check(GameControls.Prompt(GameControls.Command.Jump)==(layout.StartsWith("Dual")?"×":"B"),layout+" positional button labels");
                Check(GameControls.AdaptPrompt(GameControls.Token(GameControls.Command.Special))==(layout.StartsWith("Dual")?"R1":"R"),layout+" typed shoulder label");
            }
            InputSystem.RemoveDevice(pad);pad=InputSystem.AddDevice<Gamepad>();Neutral();pressed=new[]{GamepadButton.South};InputStep();
            InputSystem.RemoveDevice(pad);pad=null;
            Check(!GameControls.UsingGamepad&&!GameControls.Held(GameControls.Command.Attack),"Unplug clears device hints and held commands");
            pad=InputSystem.AddDevice<Gamepad>();Neutral();pressed=new[]{GamepadButton.South};InputStep();
            Check(GameControls.UsingGamepad,"Hot-plug resumes controller input");Neutral();await Frames(3);
            if(menuOnly){await MenuScenario();return;}
            Spawn();await Wait(1);
            var motor=player.GetComponent<ThirdPersonPlayerController>();var attack=player.GetComponent<SahurAttack>();
            var stamina=player.GetComponent<PlayerStamina>();var health=player.GetComponent<PlayerHealth>();
            var loadout=player.GetComponent<SahurWeaponLoadout>();
            var origin=player.transform.position;
            stick=new Vector2(0,.45f);await Wait(.7f);float partial=Vector3.Distance(origin,player.transform.position);
            Neutral();await Wait(.5f);motor.RestoreSavedPose(origin,Quaternion.identity);await Frames(3);
            stick=Vector2.up;await Wait(.7f);float full=Vector3.Distance(origin,player.transform.position);
            Check(partial>.2f&&full>partial*1.25f,"Actual controller capsule has analog walking speed [partial="+partial.ToString("F2")+", full="+full.ToString("F2")+"]");
            pressed=new[]{GamepadButton.LeftStick};await Wait(.3f);Check(motor.IsSprinting,"L3 actual sprint");
            Neutral();await Wait(.6f);motor.RestoreSavedPose(origin,Quaternion.identity);await Wait(.2f);
            int dodges=motor.DodgesPerformed;await Press(GamepadButton.East);Check(motor.DodgesPerformed==dodges+1,"B actual dodge");await Wait(1);
            await Press(GamepadButton.South);Check(motor.VerticalSpeed>0||!motor.CanUseGroundAttack,"A actual jump");await Wait(1.5f);
            Neutral();motor.RestoreSavedPose(origin,Quaternion.identity);await Wait(.3f);
            var contact=new GameObject("Native melee contact probe",typeof(BoxCollider),typeof(GamepadContactProbe036));contact.transform.SetParent(root.transform);
            contact.tag=string.IsNullOrEmpty(attack.enemyTag)?target.tag:attack.enemyTag;
            contact.transform.position=new Vector3(origin.x,50+3,origin.z+3.5f);contact.GetComponent<BoxCollider>().size=new Vector3(7,8,2);
            damageProbe=contact.GetComponent<GamepadContactProbe036>();Physics.SyncTransforms();
            rt=1;await Frames(3);rt=0;await Wait(1.7f);
            Check(damageProbe.hits>0,"RT actual weapon contact damages a target");
            contact.GetComponent<BoxCollider>().enabled=false;
            lt=1;await Wait(.5f);Check(attack.IsCharging,"LT actual charge starts");lt=0;await Wait(1.8f);
            Check(!attack.IsCharging,"LT release completes charged strike");
            lt=1;await Wait(.4f);Check(attack.IsCharging,"Charge before unplug");
            InputSystem.RemoveDevice(pad);pad=null;Neutral();await Frames(2);
            Check(!attack.IsCharging&&!attack.IsCombatMotionActive,"Unplug cancels charge without firing it");
            pad=InputSystem.AddDevice<Gamepad>();await Frames(3);
            rt=lt=1;await Wait(.8f);Check(attack.IsSpinning,"RT+LT actual spin");Neutral();await Wait(1);
            target.transform.position=new Vector3(player.transform.position.x,50,player.transform.position.z+12);Physics.SyncTransforms();
            await Press(GamepadButton.RightStick);Check(player.GetComponent<EnemyLockOn>().IsLocked,"R3 acquires an actual target");
            await Press(GamepadButton.RightStick);Check(!player.GetComponent<EnemyLockOn>().IsLocked,"R3 releases target");
            health.currentHealth=40;int packs=HealingPackSupply.Remaining;await Press(GamepadButton.DpadUp);await Wait(.3f);
            Check(health.currentHealth>40&&HealingPackSupply.Remaining==packs-1,"D-Pad up consumes a real healing pack");
            loadout.Restore(true,SahurWeaponLoadout.Weapon.Stick);await Wait(.3f);
            await Press(GamepadButton.North);Check(loadout.UsesTwinBlades,"Y cycles to an owned weapon");
            pressed=new[]{GamepadButton.LeftShoulder};await Wait(.3f);Check(attack.IsGuarding,"LB actual twin-blade guard");Neutral();await Wait(.3f);
            loadout.Restore(true,SahurWeaponLoadout.Weapon.M4A1,true);await Wait(.6f);
            Check(loadout.UsesRifle,"Owned rifle can be selected");
            var rifle=player.GetComponent<SahurRifleCombat>();rifle.RestoreAmmo(15,30);
            lt=1;await Wait(.3f);Check(motor.RifleAimRequested,"LT actual rifle aiming");
            int rounds=rifle.Rounds;rt=1;await Wait(.18f);rt=0;Check(rifle.Rounds<rounds,"RT actual automatic rifle fire consumes ammunition");
            Neutral();await Wait(.2f);await Press(GamepadButton.West);Check(rifle.IsReloading,"X actual rifle reload");
            await Wait(rifle.ReloadDuration+.2f);Check(rifle.Rounds==30&&!rifle.IsReloading,"Reload finishes through normal ammunition rules");
            loadout.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades);await Wait(.4f);
            var gear=player.GetComponent<SahurLoadoutUI>();await Press(GamepadButton.DpadDown);await Frames(3);
            Check(gear&&SahurLoadoutUI.IsOpen&&Time.timeScale==0,"D-Pad down opens Gear and pauses gameplay");
            Check(EventSystem.current&&EventSystem.current.currentSelectedGameObject,"Controller gets an initial UI selection");
            ScreenCapture.CaptureScreenshot(folder+"/controller-gear.png");
            var selected=EventSystem.current.currentSelectedGameObject;stick=NavigationDirection(selected.GetComponent<Selectable>());await Frames(45);Neutral();await Frames(3);
            Check(EventSystem.current.currentSelectedGameObject!=selected,"Stick navigates real Gear buttons");
            dodges=motor.DodgesPerformed;await Press(GamepadButton.East);Check(!SahurLoadoutUI.IsOpen&&motor.DodgesPerformed==dodges,"B closes Gear without a gameplay dodge");
            await Press(GamepadButton.Start);Check(PauseSettingsMenu.IsOpen,"Menu button pauses");
            dodges=motor.DodgesPerformed;await Press(GamepadButton.East);Check(!PauseSettingsMenu.IsOpen&&motor.DodgesPerformed==dodges,"B resumes pause menu without a dodge");
            await Press(GamepadButton.Select);Check(IslandMapUI.IsOpen,"View button opens the map");await Press(GamepadButton.East);Check(!IslandMapUI.IsOpen,"B closes the map");
            var prompt=new GameObject("Prompt verification",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));prompt.transform.SetParent(root.transform);
            var label=prompt.GetComponent<Text>();label.text="Left Mouse attack / "+GameControls.Token(GameControls.Command.Dodge)+" dodge";LocalizedGameText.Bind(label);
            await Frames(3);Check(label.text=="RT attack / B dodge","Live Text label displays controller commands");
            keys=new[]{Key.Tab};await Frames(3);keys=Array.Empty<Key>();await Frames(3);
            Check(label.text.StartsWith("Left Mouse")&&label.text.Contains(GameInputSettings.Get(GameInputSettings.Action.Dodge).ToString()),"Live Text returns to keyboard hints");
            var keyboardOrigin=player.transform.position;keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};await Wait(.3f);keys=Array.Empty<Key>();await Frames(3);
            Check(Vector3.Distance(keyboardOrigin,player.transform.position)>.2f,"Actual keyboard movement regression");
            await Wait(.3f);mouseDown=true;await Frames(3);mouseDown=false;Check(attack.IsCombatMotionActive,"Actual mouse attack regression");await Wait(1.5f);
            pressed=new[]{GamepadButton.South};await Frames(3);Neutral();await Frames(3);Check(label.text=="RT attack / B dodge","Live Text switches back without stale labels");
            Check(errors.Count==0,"No runtime errors in the native controller scenario");
        }
        catch(Exception e){failure=e.ToString();}
        finally
        {
            var result=new{passed=failure==null,error=failure,checks=checks.ToArray(),errors=errors.ToArray()};
            Cleanup();
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            EditorApplication.isPaused=true;
        }
    }
    static Vector2 NavigationDirection(Selectable from)
    {
        if(from.FindSelectableOnDown())return Vector2.down;if(from.FindSelectableOnUp())return Vector2.up;
        if(from.FindSelectableOnRight())return Vector2.right;return Vector2.left;
    }
    static async Task MenuScenario()
    {
        var operation=UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
        while(running&&!operation.isDone)await Task.Delay(5);
        await Wait(1);
        var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        Check(menu&&EventSystem.current&&EventSystem.current.currentSelectedGameObject,"Actual main menu gets initial controller selection");
        var before=EventSystem.current.currentSelectedGameObject;
        stick=NavigationDirection(before.GetComponent<Selectable>());await Frames(40);Neutral();await Frames(3);
        Check(EventSystem.current.currentSelectedGameObject!=before,"Stick navigates actual main-menu buttons");
        var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.IsActive()&&b.IsInteractable()&&
            (b.name.IndexOf("settings",StringComparison.OrdinalIgnoreCase)>=0||b.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="SETTINGS")));
        Check(button,"Actual Settings button found");button.Select();await Frames(3);
        await Press(GamepadButton.South);await Wait(.5f);
        Check(PauseSettingsMenu.IsOpen,"A submits actual main-menu Settings button");
        Check(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t=>System.Text.RegularExpressions.Regex.IsMatch(t.text,@"^\d+\.\d+ M$")),"Actual settings retains the distance unit");
        ScreenCapture.CaptureScreenshot(folder+"/controller-settings.png");
        // Keep the modal open until the unfocused Game view has rendered the capture.
        await Task.Delay(1000);
        await Press(GamepadButton.East);await Wait(.2f);Check(!PauseSettingsMenu.IsOpen,"B returns from Settings to main menu");
        Check(errors.Count==0,"No native main-menu controller errors");
    }
    static void Spawn()
    {
        root=new GameObject("Controller verification sandbox");root.transform.position=new Vector3(2000,50,2000);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
        var cam=new GameObject("Controller verification camera",typeof(Camera));cam.tag="MainCamera";cam.transform.SetParent(root.transform,false);cam.transform.localPosition=new Vector3(0,8,-12);
        var sun=new GameObject("Controller verification light",typeof(Light));sun.transform.SetParent(root.transform,false);sun.transform.rotation=Quaternion.Euler(40,-30,0);sun.GetComponent<Light>().type=LightType.Directional;
        player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"),root.transform);
        player.AddComponent<GameSaveExcluded>();
        var capsule=player.GetComponent<CharacterController>();float sole=(capsule.center.y-capsule.height*.5f)*player.transform.lossyScale.y;
        player.GetComponent<ThirdPersonPlayerController>().RestoreSavedPose(root.transform.position+Vector3.up*(.03f-sole),Quaternion.identity);
        target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab"),root.transform.position+Vector3.forward*35,Quaternion.Euler(0,180,0),root.transform);
        target.AddComponent<GameSaveExcluded>();target.GetComponent<NailongAI>().enabled=false;
        var agent=target.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
        target.GetComponent<NailongHealth>().maxHealth=target.GetComponent<NailongHealth>().currentHealth=100000;
        testCampaign=HealingPackSupply.Campaign;
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Physics.SyncTransforms();
    }
    static void Cleanup()
    {
        if(!armed)return;armed=false;running=false;Neutral();
        InputSystem.onBeforeUpdate-=Inject;Application.logMessageReceived-=Log;
        EditorApplication.playModeStateChanged-=Lifecycle;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;EditorApplication.quitting-=Cleanup;
        if(oldSettings)InputSystem.settings=oldSettings;
        if(temporarySettings)UnityEngine.Object.DestroyImmediate(temporarySettings);temporarySettings=null;
        if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        pad=null;keyboard=null;mouse=null;
        foreach(var d in physical)if(d.added)InputSystem.EnableDevice(d);physical.Clear();
        if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldMouse!=null&&oldMouse.added)oldMouse.MakeCurrent();
        Time.captureDeltaTime=capture;
        if(!string.IsNullOrEmpty(testCampaign))PlayerPrefs.DeleteKey("DarkBrine.HealingPacks.Remaining."+testCampaign);
    }
}
public sealed class GamepadContactProbe036:MonoBehaviour,IDamageable
{
    public int hits;
    public void ApplyDamage(float amount,Vector3 point){hits++;}
}
