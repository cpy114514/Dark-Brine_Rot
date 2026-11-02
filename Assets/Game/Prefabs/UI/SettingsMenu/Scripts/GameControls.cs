using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Single-player commands shared by gameplay, tutorials and button prompts.</summary>
public static class GameControls
{
    public enum Command { Sprint, Jump, Dodge, Heal, Guard, Attack, Charge, Special, Interact, Reload,
        LockOn, Gear, Map, Pause, Confirm, Cancel, NextWeapon, Glasses, Gravity, CameraView }
    static readonly string[] Paths={"leftStickPress","buttonSouth","buttonEast","dpad/up","leftShoulder",
        "rightTrigger","leftTrigger","rightShoulder","buttonWest","buttonWest","rightStickPress",
        "dpad/down","select","start","buttonSouth","buttonEast","buttonNorth","dpad/left","dpad/right","buttonNorth"};
    static readonly Key[] Keys={Key.LeftShift,Key.Space,Key.LeftCtrl,Key.H,Key.E,Key.None,Key.None,Key.E,Key.F,Key.R,
        Key.None,Key.I,Key.M,Key.Escape,Key.Enter,Key.Escape,Key.None,Key.G,Key.B,Key.V};
    static InputActionMap pad;
    static InputAction move,look;
    static InputAction[] buttons;
    static Vector2 previousMove,frameMove;
    static Gamepad activePad;
    static bool usingPad;
    static string family="Xbox";
    static int interruptedFrame=-1;
    static int menuFrame=-1;
    public static int Revision { get; private set; }
    public static void NotifyBindingsChanged(){Revision++;Changed?.Invoke();}
    public static void ConsumeMenuInput(){menuFrame=Time.frameCount;}
    static bool Suppressed(Command c)=>menuFrame==Time.frameCount&&c!=Command.Pause&&c!=Command.Cancel&&c!=Command.Confirm&&c!=Command.Gear&&c!=Command.Map;
    public static event Action Changed;
    public static bool UsingGamepad { get { Ensure(); return usingPad; } }
    public static string ControllerFamily { get { Ensure(); return family; } }
    public static bool InputInterrupted=>Time.frameCount<=interruptedFrame;
    public static bool HasDevice=>Keyboard.current!=null||Gamepad.current!=null;
    public static bool MenusOpen=>PauseSettingsMenu.IsOpen||Mavis.SahurLoadoutUI.BlocksInput||
        Mavis.IslandMapUI.BlocksInput||Mavis.PlayerDeathRespawn.IsOpen||Mavis.VisualStyles.VisualStyleUIController.BlocksParentInput;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        InputSystem.onAfterUpdate-=TrackInput;InputSystem.onDeviceChange-=DeviceChanged;
        pad?.Dispose();pad=null;buttons=null;activePad=null;usingPad=false;family="Xbox";
        previousMove=frameMove=Vector2.zero;interruptedFrame=menuFrame=-1;Revision=0;Changed=null;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        Ensure();var go=new GameObject("Shared game input");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<GameControlsRuntime>();
    }
    static void Ensure()
    {
        if(pad!=null)return;
        pad=new InputActionMap("Shared gamepad commands");
        move=pad.AddAction("Move",InputActionType.Value,"<Gamepad>/leftStick");move.expectedControlType="Vector2";
        look=pad.AddAction("Look",InputActionType.Value,"<Gamepad>/rightStick");look.expectedControlType="Vector2";
        buttons=new InputAction[Paths.Length];
        for(int i=0;i<buttons.Length;i++)buttons[i]=pad.AddAction(((Command)i).ToString(),InputActionType.Button,"<Gamepad>/"+Paths[i]);
        pad.Enable();InputSystem.onAfterUpdate+=TrackInput;InputSystem.onDeviceChange+=DeviceChanged;
        if(Keyboard.current==null&&Gamepad.current!=null)SetDevice(Gamepad.current);
    }
    static void SetDevice(Gamepad device)
    {
        string next=device==null?"Xbox":Family(device);
        bool changed=usingPad!=(device!=null)||family!=next||activePad!=device;
        activePad=device;usingPad=device!=null;family=next;
        if(changed){Revision++;Changed?.Invoke();}
    }
    static string Family(Gamepad device)
    {
        string name=(device.layout+" "+device.description.product).ToLowerInvariant();
        if(name.Contains("dualshock")||name.Contains("dualsense")||name.Contains("playstation"))return "PlayStation";
        return name.Contains("switch")||name.Contains("nintendo")?"Switch":"Xbox";
    }
    static void DeviceChanged(InputDevice device,InputDeviceChange change)
    {
        if(device!=activePad||change!=InputDeviceChange.Removed&&change!=InputDeviceChange.Disconnected&&change!=InputDeviceChange.Disabled)return;
        interruptedFrame=Time.frameCount+1;SetDevice(null);
    }
    static void TrackInput()
    {
        if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        previousMove=frameMove;frameMove=move.ReadValue<Vector2>();
        foreach(var device in Gamepad.all)
        {
            if(!device.enabled)continue;
            bool intent=device.leftStick.ReadValue().sqrMagnitude>.04f||device.rightStick.ReadValue().sqrMagnitude>.04f;
            foreach(var c in device.allControls)
                if(c is ButtonControl button&&button.wasPressedThisFrame){intent=true;break;}
            if(intent)SetDevice(device);
        }
        var k=Keyboard.current;var m=Mouse.current;
        if(k!=null&&k.anyKey.wasPressedThisFrame||m!=null&&(m.leftButton.wasPressedThisFrame||m.rightButton.wasPressedThisFrame||
            m.middleButton.wasPressedThisFrame||m.scroll.ReadValue().sqrMagnitude>1||m.delta.ReadValue().sqrMagnitude>16))SetDevice(null);
    }
    static GameInputSettings.Action? Legacy(Command command)
    {
        switch(command)
        {
            case Command.Sprint:return GameInputSettings.Action.Sprint;
            case Command.Jump:return GameInputSettings.Action.Jump;
            case Command.Dodge:return GameInputSettings.Action.Dodge;
            case Command.Heal:return GameInputSettings.Action.Heal;
            case Command.Guard:return GameInputSettings.Action.Guard;
            default:return null;
        }
    }
    static ButtonControl KeyboardButton(Command command)
    {
        var m=Mouse.current;
        if(command==Command.Attack)return m?.leftButton;
        if(command==Command.Charge)return m?.rightButton;
        if(command==Command.LockOn)return m?.middleButton;
        var k=Keyboard.current;var legacy=Legacy(command);
        Key key=legacy.HasValue?GameInputSettings.Get(legacy.Value):Keys[(int)command];
        return k!=null&&key!=Key.None?k[key]:null;
    }
    public static bool Held(Command command){Ensure();return !Suppressed(command)&&(buttons[(int)command].IsPressed()||(KeyboardButton(command)?.isPressed??false));}
    public static bool Down(Command command){Ensure();return !Suppressed(command)&&(buttons[(int)command].WasPressedThisFrame()||(KeyboardButton(command)?.wasPressedThisFrame??false));}
    public static bool Up(Command command){Ensure();return !Suppressed(command)&&(buttons[(int)command].WasReleasedThisFrame()||(KeyboardButton(command)?.wasReleasedThisFrame??false));}
    public static Vector2 Move
    {
        get
        {
            Ensure();
            if(menuFrame==Time.frameCount)return Vector2.zero;
            if(usingPad)return move.ReadValue<Vector2>();
            var k=Keyboard.current;if(k==null)return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2((k[GameInputSettings.Get(GameInputSettings.Action.Right)].isPressed?1:0)-(k[GameInputSettings.Get(GameInputSettings.Action.Left)].isPressed?1:0),
                (k[GameInputSettings.Get(GameInputSettings.Action.Forward)].isPressed?1:0)-(k[GameInputSettings.Get(GameInputSettings.Action.Back)].isPressed?1:0)),1);
        }
    }
    public static Vector2 LookDelta(float sensitivity,float deltaTime)
    {
        Ensure();if(menuFrame==Time.frameCount)return Vector2.zero;return usingPad?look.ReadValue<Vector2>()*
            (160f*deltaTime*Mathf.Clamp(sensitivity/.12f,.1f,5)):(Mouse.current?.delta.ReadValue()??Vector2.zero)*sensitivity;
    }
    public static bool LegacyHeld(GameInputSettings.Action action)
    {
        if((int)action<4){Vector2 v=Move;return action==GameInputSettings.Action.Forward?v.y>.15f:action==GameInputSettings.Action.Back?v.y<-.15f:action==GameInputSettings.Action.Left?v.x<-.15f:v.x>.15f;}
        return Held(action==GameInputSettings.Action.Sprint?Command.Sprint:action==GameInputSettings.Action.Jump?Command.Jump:
            action==GameInputSettings.Action.Dodge?Command.Dodge:action==GameInputSettings.Action.Heal?Command.Heal:Command.Guard);
    }
    public static bool LegacyDown(GameInputSettings.Action action)
    {
        if((int)action<4)
        {
            var k=Keyboard.current;bool keyboard=k!=null&&k[GameInputSettings.Get(action)].wasPressedThisFrame;
            bool pressed=action==GameInputSettings.Action.Forward?frameMove.y>.2f&&previousMove.y<=.2f:
                action==GameInputSettings.Action.Back?frameMove.y<-.2f&&previousMove.y>=-.2f:
                action==GameInputSettings.Action.Left?frameMove.x<-.2f&&previousMove.x>=-.2f:frameMove.x>.2f&&previousMove.x<=.2f;
            return keyboard||pressed;
        }
        return Down(action==GameInputSettings.Action.Sprint?Command.Sprint:action==GameInputSettings.Action.Jump?Command.Jump:
            action==GameInputSettings.Action.Dodge?Command.Dodge:action==GameInputSettings.Action.Heal?Command.Heal:Command.Guard);
    }
    public static string MovementPrompt=>UsingGamepad?"LS":string.Join(" / ",new[]{GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Left),GameInputSettings.Get(GameInputSettings.Action.Back),GameInputSettings.Get(GameInputSettings.Action.Right)});
    public static string Prompt(GameInputSettings.Action action)=>(int)action<4?(UsingGamepad?"LS":GameInputSettings.Get(action).ToString()):
        Prompt(action==GameInputSettings.Action.Sprint?Command.Sprint:action==GameInputSettings.Action.Jump?Command.Jump:action==GameInputSettings.Action.Dodge?Command.Dodge:action==GameInputSettings.Action.Heal?Command.Heal:Command.Guard);
    public static string Prompt(Command command)
    {
        if(!UsingGamepad)
        {
            if(command==Command.Attack)return "Left Mouse";if(command==Command.Charge)return "Right Mouse";if(command==Command.LockOn)return "Middle Mouse";
            if(command==Command.NextWeapon)return "1 / 2 / 3";
            var legacy=Legacy(command);return (legacy.HasValue?GameInputSettings.Get(legacy.Value):Keys[(int)command]).ToString();
        }
        string path=Paths[(int)command];
        switch(path)
        {
            case "buttonSouth":return family=="PlayStation"?"×":family=="Switch"?"B":"A";
            case "buttonEast":return family=="PlayStation"?"○":family=="Switch"?"A":"B";
            case "buttonWest":return family=="PlayStation"?"□":family=="Switch"?"Y":"X";
            case "buttonNorth":return family=="PlayStation"?"△":family=="Switch"?"X":"Y";
            case "leftTrigger":return family=="PlayStation"?"L2":family=="Switch"?"ZL":"LT";
            case "rightTrigger":return family=="PlayStation"?"R2":family=="Switch"?"ZR":"RT";
            case "leftShoulder":return family=="PlayStation"?"L1":family=="Switch"?"L":"LB";
            case "rightShoulder":return family=="PlayStation"?"R1":family=="Switch"?"R":"RB";
            case "leftStickPress":return family=="PlayStation"?"L3":"LS Press";
            case "rightStickPress":return family=="PlayStation"?"R3":"RS Press";
            case "select":return family=="PlayStation"?"Touchpad":family=="Switch"?"−":"View";
            case "start":return family=="PlayStation"?"Options":family=="Switch"?"+":"Menu";
            case "dpad/up":return "D-Pad ↑";case "dpad/down":return "D-Pad ↓";
            case "dpad/left":return "D-Pad ←";default:return "D-Pad →";
        }
    }
    public static string Token(Command command)=>"{command:"+command+"}";
    public static string Token(GameInputSettings.Action action)=>"{action:"+action+"}";
    static string ResolveTokens(string value)=>Regex.Replace(value,@"\{(command|action):([A-Za-z]+)\}",m=>
        m.Groups[1].Value=="command"&&Enum.TryParse<Command>(m.Groups[2].Value,out var c)?Prompt(c):
        m.Groups[1].Value=="action"&&Enum.TryParse<GameInputSettings.Action>(m.Groups[2].Value,out var a)?Prompt(a):m.Value);
    public static string AdaptPrompt(string value)
    {
        if(string.IsNullOrEmpty(value))return value;
        if(!UsingGamepad)return ResolveTokens(value);
        value=value.Replace("Mouse camera speed","Stick camera speed").Replace("Reverse vertical mouse look","Reverse vertical stick look");
        value=value.Replace("Move with your movement keys; look with the mouse.","Move with LS; look with RS.");
        value=value.Replace("SPACE / CLICK",Prompt(Command.Confirm)).Replace("Space / Click",Prompt(Command.Confirm));
        value=value.Replace("Jump +",Token(Command.Jump)+" +");
        value=Regex.Replace(value,@"W\s*/\s*A\s*/\s*S\s*/\s*D|W\s*/\s*S\s*/\s*A\s*/\s*D|WASD", "LS");
        value=Regex.Replace(value,@"Left Mouse|LMB|鼠标左键",Prompt(Command.Attack),RegexOptions.IgnoreCase);
        value=Regex.Replace(value,@"Right Mouse|RMB|鼠标右键",Prompt(Command.Charge),RegexOptions.IgnoreCase);
        value=Regex.Replace(value,@"Middle Mouse|Middle[ -]click|鼠标中键|中键",Prompt(Command.LockOn),RegexOptions.IgnoreCase);
        value=Regex.Replace(value,@"1\s*/\s*2\s*/\s*3",Prompt(Command.NextWeapon));
        value=Regex.Replace(value,@"1[–-]4 to select", Token(Command.Gear)+" to open");
        bool guard=value.IndexOf("block",StringComparison.OrdinalIgnoreCase)>=0||value.Contains("格挡");
        string[] tokens={"LeftShift","Left Shift","Shift","LeftCtrl","Left Ctrl","Ctrl","Space","SPACE","Esc","ESC","Escape","Enter","F","R","E","H","G","I","M","B","V"};
        Command[] commands={Command.Sprint,Command.Sprint,Command.Sprint,Command.Dodge,Command.Dodge,Command.Dodge,Command.Jump,Command.Jump,
            Command.Cancel,Command.Cancel,Command.Cancel,Command.Confirm,Command.Interact,Command.Reload,guard?Command.Guard:Command.Special,Command.Heal,Command.Glasses,Command.Gear,Command.Map,Command.Gravity,Command.CameraView};
        // One pass prevents replacing an Xbox B label again as the keyboard B binding.
        string pattern=@"(?<![\p{L}\p{N}_])(?:"+string.Join("|",Array.ConvertAll(tokens,Regex.Escape))+@")(?![\p{L}\p{N}_])";
        return ResolveTokens(Regex.Replace(value,pattern,m=>
        {
            // M is also the distance unit in settings and HUD readouts.
            int before=m.Index-1;while(before>=0&&char.IsWhiteSpace(value[before]))before--;
            if(m.Value=="M"&&before>=0&&char.IsDigit(value[before]))return m.Value;
            return Prompt(commands[Array.IndexOf(tokens,m.Value)]);
        }));
    }
}

[DefaultExecutionOrder(-32000)]
public sealed class GameControlsRuntime:MonoBehaviour
{
    void Update()
    {
        if(!GameControls.UsingGamepad||GameControls.MenusOpen||Time.timeScale<=0||SceneManager.GetActiveScene().name=="MainMenu")return;
        if(GameControls.Move.sqrMagnitude>.01f||GameControls.Down(GameControls.Command.Attack)||GameControls.Down(GameControls.Command.Jump))
        {Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
    }
    void LateUpdate()
    {
        if(!GameControls.UsingGamepad||!GameControls.MenusOpen&&SceneManager.GetActiveScene().name!="MainMenu")return;
        var events=EventSystem.current;if(events==null)return;
        Selectable best=null;int order=int.MinValue;
        foreach(var selectable in Selectable.allSelectablesArray)
        {
            if(!selectable.IsActive()||!selectable.IsInteractable())continue;
            var canvas=selectable.GetComponentInParent<Canvas>();int priority=canvas?canvas.rootCanvas.sortingOrder:0;
            if(priority>order){best=selectable;order=priority;}
        }
        var current=events.currentSelectedGameObject?events.currentSelectedGameObject.GetComponent<Selectable>():null;
        var currentCanvas=current?current.GetComponentInParent<Canvas>():null;
        if(best&&(!current||!current.IsActive()||!current.IsInteractable()||currentCanvas&&currentCanvas.rootCanvas.sortingOrder<order))best.Select();
    }
}
