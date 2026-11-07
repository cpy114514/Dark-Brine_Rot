using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Exercises the optional friend story with real interaction and walking input.</summary>
public static class VerifyMarshmallowFriendsQuest
{
    public static string Status="idle",Result="";
    static Keyboard keyboard;static Key[] keys=Array.Empty<Key>();
    static ThirdPersonPlayerController player;static Transform destination;static bool walking;
    static readonly FieldInfo Yaw=typeof(ThirdPersonPlayerController).GetField("yaw",BindingFlags.NonPublic|BindingFlags.Instance);
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        if(walking && destination && player){var d=destination.position-player.transform.position;d.y=0;if(d.sqrMagnitude>16){Yaw.SetValue(player,Quaternion.LookRotation(d).eulerAngles.y);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};}else keys=Array.Empty<Key>();}
        InputState.Change(keyboard,new KeyboardState(keys));
    }
    static async Task Wait(float seconds){float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(30);while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Stopped or stalled");await Task.Delay(20);}}
    static async Task Until(Func<bool> condition,float seconds,string label){float end=Time.time+seconds;while(!condition()){if(!EditorApplication.isPlaying || Time.time>end)throw new Exception("Timed out: "+label);await Task.Delay(20);}}
    static async Task Press(Key k){walking=false;keys=Array.Empty<Key>();await Wait(.15f);keys=new[]{k};await Wait(.12f);keys=Array.Empty<Key>();await Wait(.2f);}
    static void Place(ThirdPersonPlayerController p,CharacterController body,Vector3 ground)
    {float sole=(body.center.y-body.height*.5f)*Mathf.Abs(p.transform.lossyScale.y);p.RestoreSavedPose(ground+Vector3.up*(.1f-sole),Quaternion.identity);}
    static async Task FinishDialogue(int lines){for(int i=0;i<lines;i++)await Press(Key.Space);}
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/friends-story-test.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);await Wait(.5f);
            var quest=UnityEngine.Object.FindFirstObjectByType<MarshmallowFriendsQuest>();player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var body=player.GetComponent<CharacterController>();var inv=player.GetComponent<EquipmentInventory>();var scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
            var board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();keyboard=Keyboard.current;
            if(!quest || !player || !body || !inv || !scroll || !board || keyboard==null)throw new Exception("Story or player components missing");
            if(!board.CollectSpawnTestKit())throw new Exception("Board test kit unavailable");
            int before=inv.Count(scroll);InputSystem.onBeforeUpdate+=Inject;
            Place(player,body,quest.Home.position+new Vector3(0,0,-3));await Wait(.8f);
            var activityGuide=UnityEngine.Object.FindFirstObjectByType<IslandActivityGuide>();
            if(!activityGuide || (activityGuide.DiscoveredMask & 4)==0 || !activityGuide.VisibleTip.Contains("Lily"))throw new Exception("Fairy did not introduce the friends story on first visit");
            await Press(Key.F);
            if(!quest.Talking || board.Flying)throw new Exception("Lily dialogue did not begin or launched the board");
            body.enabled=false;await Wait(.3f);
            if(quest.Talking || player.ExternalControlLock || player.ExternalMovementLock || UnityEngine.Object.FindFirstObjectByType<SecondIslandDialogue>())throw new Exception("Interrupted dialogue left controls or the conversation open");
            body.enabled=true;await Wait(.3f);await Press(Key.F);
            if(!quest.Talking)throw new Exception("Could not restart the interrupted conversation");
            await FinishDialogue(2);await Until(()=>quest.Stage==1 && !quest.Talking,4,"accept story");
            if(quest.MapAnchor!=quest.Lost)throw new Exception("Map does not point to lost friend");
            Place(player,body,quest.Lost.position+new Vector3(0,0,-3));await Wait(.4f);await Press(Key.F);await FinishDialogue(2);
            await Until(()=>quest.Stage==2 && !quest.Talking,4,"find friend");
            var beforeFollow=quest.Lost.position;destination=quest.Home;walking=true;Status="walking Pip home";
            await Until(()=>Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-quest.Home.position,Vector3.up),Vector3.zero)<4.5f,18,"walk home");
            walking=false;keys=Array.Empty<Key>();
            await Until(()=>quest.IsNearHome,12,"friend follows home");
            if(Vector3.Distance(quest.Lost.position,beforeFollow)<10)throw new Exception("Pip did not follow");
            await Press(Key.F);await FinishDialogue(2);await Until(()=>quest.Stage==3 && !quest.Talking,4,"reunion");
            if(inv.Count(scroll)!=before+1)throw new Exception("Reunion did not grant one scroll");
            var state=player.GetComponent<SecondIslandSaveState>();var snapshot=state.Capture();if(snapshot.marshmallowFriendsStage!=3 || !GameSaveManager.SaveCurrentGame())throw new Exception("Story progress did not save");
            if(!GameSaveManager.QueueContinue())throw new Exception("Could not queue isolated save");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            await Until(()=>!GameSaveManager.IsContinuePending,15,"continue");await Wait(.5f);
            quest=UnityEngine.Object.FindFirstObjectByType<MarshmallowFriendsQuest>();player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();body=player.GetComponent<CharacterController>();inv=player.GetComponent<EquipmentInventory>();
            activityGuide=UnityEngine.Object.FindFirstObjectByType<IslandActivityGuide>();
            if(!quest || quest.Stage!=3 || !quest.IsNearHome || inv.Count(scroll)!=before+1 || !activityGuide || (activityGuide.DiscoveredMask&4)==0)throw new Exception("Continue did not restore reunited friends, discovery and reward");
            Place(player,body,quest.Home.position+new Vector3(0,0,-3));await Wait(.4f);await Press(Key.F);await FinishDialogue(1);await Until(()=>!quest.Talking,4,"repeat conversation");
            if(inv.Count(scroll)!=before+1)throw new Exception("Repeat conversation duplicated reward");
            Status="passed";Result="Real dialogue and walking reunited Pip and Lily; Skyboard input, map target, single reward, save/continue and repeat conversation passed.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally{walking=false;keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputState.Change(keyboard,new KeyboardState());}
        Debug.Log("Friends story test: "+Status+" "+Result);
    }
}
