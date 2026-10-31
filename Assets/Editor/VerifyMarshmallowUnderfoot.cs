using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-200)]
public sealed class UnderfootTestPosition : MonoBehaviour
{
    public CharacterController player;
    public Vector3 offset;
    void Update()
    {
        if (!player) return;
        var b=player.bounds;
        transform.position=new Vector3(b.center.x,b.min.y,b.center.z)+player.transform.rotation*offset;
    }
}

// Real inputs and real enemy colliders; objects exist only in this play session.
[InitializeOnLoad]
public static class VerifyMarshmallowUnderfoot
{
    const string Key="MarshmallowUnderfoot.Review";
    const string Folder="Temp/MarshmallowUnderfootReview";
    static Mouse mouse;
    static Keyboard keyboard;
    static bool click, jump;
    static VerifyMarshmallowUnderfoot()=>EditorApplication.playModeStateChanged+=OnMode;
    [MenuItem("Tools/Second Island Review/Test Underfoot Attacks")]
    public static void Start()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play before starting this review.");
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/status.txt","starting");
        SessionState.SetBool(Key,true);VerifySecondIslandAwakening.Arm();EditorApplication.isPlaying=true;
    }
    static void OnMode(PlayModeStateChange mode)
    {
        if(mode!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);_=Run();
    }
    static void Inject()
    {
        if(mouse==null || keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        mouse.MakeCurrent();keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        var m=new MouseState();if(click)m=m.WithButton(MouseButton.Left);InputState.Change(mouse,m);
        InputState.Change(keyboard,jump ? new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Jump)) : new KeyboardState());
    }
    static async Task Wait(float seconds)
    {
        float end=Time.time+seconds;var timeout=DateTime.UtcNow.AddSeconds(15);
        while(Time.time<end)
        {
            if(!EditorApplication.isPlaying || DateTime.UtcNow>timeout)throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(20);
        }
    }
    static async Task WaitUntil(Func<bool> check,float timeout,string label)
    {
        var end=DateTime.UtcNow.AddSeconds(timeout);
        while(!check())
        {
            if(!EditorApplication.isPlaying || DateTime.UtcNow>end)throw new Exception("Timed out: "+label);
            await Task.Delay(30);
        }
    }
    static GameObject Spawn(CharacterController player,Vector3 offset)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Marshmallow/Strawberry Marshmallow.prefab");
        var instance=UnityEngine.Object.Instantiate(prefab);
        instance.GetComponent<ChickenBroAI>().enabled=false;
        var follow=instance.AddComponent<UnderfootTestPosition>();follow.player=player;follow.offset=offset;
        return instance;
    }
    static async Task Run()
    {
        GameObject near=null,far=null,group=null;
        try
        {
            File.WriteAllText(Folder+"/status.txt","opening");
            await WaitUntil(()=>VerifySecondIslandAwakening.Status=="completed" || VerifySecondIslandAwakening.Status.StartsWith("failed"),45,"opening");
            if(VerifySecondIslandAwakening.Status!="completed")throw new Exception(VerifySecondIslandAwakening.Status);
            group=GameObject.Find("Marshmallow Friends - Second Island");if(group)group.SetActive(false);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var body=player.GetComponent<CharacterController>();var attack=player.GetComponent<SahurAttack>();
            keyboard=Keyboard.current;mouse=Mouse.current;
            if(keyboard==null || mouse==null)throw new Exception("Input devices are unavailable.");
            InputSystem.onBeforeUpdate+=Inject;
            await WaitUntil(()=>player.CanUseGroundAttack && !attack.IsCombatMotionActive,8,"grounded player");
            near=Spawn(body,new Vector3(0,0,.9f));far=Spawn(body,new Vector3(0,0,-5f));
            await Wait(.25f);
            float nearBefore=near.GetComponent<MarshmallowEnemyHealth>().currentHealth;
            float farBefore=far.GetComponent<MarshmallowEnemyHealth>().currentHealth;
            click=true;await Wait(.09f);click=false;await Wait(1.3f);
            float nearAfter=near.GetComponent<MarshmallowEnemyHealth>().currentHealth;
            float farAfter=far.GetComponent<MarshmallowEnemyHealth>().currentHealth;
            if(nearAfter>=nearBefore || farAfter<farBefore)throw new Exception($"Ground attack: near {nearBefore}->{nearAfter}, far {farBefore}->{farAfter}");
            UnityEngine.Object.Destroy(near);near=null;UnityEngine.Object.Destroy(far);far=null;
            await WaitUntil(()=>player.CanUseGroundAttack && !attack.IsCombatMotionActive,8,"ground recovery");
            player.GetComponent<PlayerStamina>().currentStamina=100;
            near=Spawn(body,new Vector3(0,-1f,-.55f));far=Spawn(body,new Vector3(0,-1f,-5f));
            await Wait(.15f);nearBefore=near.GetComponent<MarshmallowEnemyHealth>().currentHealth;farBefore=far.GetComponent<MarshmallowEnemyHealth>().currentHealth;
            jump=true;await Wait(.09f);jump=false;await Wait(.15f);click=true;await Wait(.09f);click=false;await Wait(1.4f);
            nearAfter=near.GetComponent<MarshmallowEnemyHealth>().currentHealth;farAfter=far.GetComponent<MarshmallowEnemyHealth>().currentHealth;
            if(nearAfter>=nearBefore || farAfter<farBefore)throw new Exception($"Aerial attack: near {nearBefore}->{nearAfter}, far {farBefore}->{farAfter}");
            File.WriteAllText(Folder+"/result.txt",$"Ground and aerial input damaged nearby marshmallows; distant targets untouched. Aerial health {nearBefore}->{nearAfter}.");
            File.WriteAllText(Folder+"/status.txt","completed");
        }
        catch(Exception e){File.WriteAllText(Folder+"/status.txt","failed: "+e);}
        finally
        {
            click=jump=false;InputSystem.onBeforeUpdate-=Inject;
            if(near)UnityEngine.Object.Destroy(near);if(far)UnityEngine.Object.Destroy(far);if(group)group.SetActive(true);
        }
    }
}
