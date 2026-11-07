using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Plays the bounce garden with walking input and isolated save data.</summary>
public static class VerifyCandyBounceGarden
{
    public static string Status="idle", Result="";
    static Keyboard keyboard;
    static Key[] keys=Array.Empty<Key>();
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));
    }
    static async Task Wait(float seconds)
    {
        float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(30);
        while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");await Task.Delay(20);}
    }
    static async Task Until(Func<bool> condition,float seconds,string label)
    {
        float end=Time.time+seconds;
        while(!condition()){if(!EditorApplication.isPlaying || Time.time>end)throw new Exception("Timed out: "+label);await Task.Delay(20);}
    }
    static async Task Press(Key key)
    {keys=Array.Empty<Key>();await Wait(.2f);keys=new[]{key};await Wait(.15f);keys=Array.Empty<Key>();await Wait(.2f);}
    static void Place(ThirdPersonPlayerController player,CharacterController body,Vector3 ground,float yaw)
    {
        float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(ground+Vector3.up*(.1f-sole),Quaternion.Euler(0,yaw,0));
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/candy-bounce-test.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.5f);
            var garden=UnityEngine.Object.FindFirstObjectByType<CandyBounceGarden>();
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var body=player ? player.GetComponent<CharacterController>() : null;
            var traversal=player ? player.GetComponent<MarshmallowTraversal>() : null;
            var inventory=player ? player.GetComponent<EquipmentInventory>() : null;
            var board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            var scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
            keyboard=Keyboard.current;
            if(!garden || !player || !body || !traversal || !inventory || !scroll || !board || keyboard==null)throw new Exception("Garden, player, inventory or keyboard missing.");
            if(garden.Playing || garden.RewardClaimed)throw new Exception("Game should begin idle and unclaimed.");
            if(!board.CollectSpawnTestKit())throw new Exception("Could not set up Skyboard interaction check.");
            foreach(var renderer in garden.GetComponentsInChildren<Renderer>(true))
                if(!renderer.GetComponent<Collider>() && !renderer.GetComponentInParent<Collider>(true))throw new Exception("Visible garden object has no collider: "+renderer.name);
            var island=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var point=garden.Sign.position+new Vector3(0,0,-3);
            if(!island.Raycast(new Ray(point+Vector3.up*50,Vector3.down),out var ground,100))throw new Exception("No ground by sign.");
            Place(player,body,ground.point,0);InputSystem.onBeforeUpdate+=Inject;await Wait(.4f);await Press(Key.F);
            if(!garden.Playing || garden.Collected!=0 || board.Flying)throw new Exception("The sign did not start the game or accidentally launched the Skyboard.");
            int before=inventory.Count(scroll);
            for(int i=0;i<3;i++)
            {
                Status="walking onto spring "+(i+1);
                traversal.SetNormalGravity(i==1);
                var pad=garden.Pad(i);point=pad.position+new Vector3(0,0,-6);
                if(!island.Raycast(new Ray(point+Vector3.up*50,Vector3.down),out ground,100))throw new Exception("No ground by spring "+i);
                Place(player,body,ground.point,0);await Wait(.4f);
                keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
                var trace=new System.Text.StringBuilder();float end=Time.time+5, sample=0;
                while(!(player.WorldVelocity.y>9f && Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-pad.position,Vector3.up),Vector3.zero)<3.2f) && Time.time<end)
                {
                    if(Time.time>=sample){sample=Time.time+.25f;trace.Append(" [z="+(player.transform.position.z-pad.position.z).ToString("F1")+" feet="+(body.bounds.min.y-pad.position.y).ToString("F1")+" vy="+player.WorldVelocity.y.ToString("F1")+"]");}
                    await Task.Delay(20);
                }
                if(Time.time>=end)throw new Exception("Bounce "+i+" failed:"+trace);
                keys=Array.Empty<Key>();
                await Until(()=>garden.Collected>i,5,"collect star "+i);
                await Wait(1f);
            }
            if(garden.Playing || !garden.RewardClaimed || inventory.Count(scroll)!=before+1)throw new Exception("The first reward was not granted exactly once.");
            var save=player.GetComponent<SecondIslandSaveState>();var snapshot=save.Capture();
            if(!snapshot.bounceGardenReward || !GameSaveManager.SaveCurrentGame())throw new Exception("Bounce reward did not save.");
            garden.RestoreProgress(false);save.Restore(snapshot);
            if(!garden.RewardClaimed)throw new Exception("Bounce reward did not restore.");
            if(!GameSaveManager.QueueContinue())throw new Exception("Could not queue isolated save.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            await Until(()=>!GameSaveManager.IsContinuePending,15,"continue");await Wait(.5f);
            garden=UnityEngine.Object.FindFirstObjectByType<CandyBounceGarden>();player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            body=player.GetComponent<CharacterController>();inventory=player.GetComponent<EquipmentInventory>();
            if(!garden || !garden.RewardClaimed || inventory.Count(scroll)!=before+1)throw new Exception("Continue did not restore the reward and inventory.");
            Place(player,body,garden.Sign.position+new Vector3(0,0,-3),0);await Wait(.4f);await Press(Key.F);
            if(!garden.Playing || garden.Collected!=0)throw new Exception("Could not replay.");
            for(int i=0;i<3;i++)
            {
                var star=garden.Star(i);Place(player,body,star.position,0);await Until(()=>garden.Collected>i,3,"replay star "+i);
            }
            if(inventory.Count(scroll)!=before+1)throw new Exception("Replay duplicated the reward.");
            Place(player,body,garden.Sign.position+new Vector3(0,0,-3),0);await Wait(.4f);
            keys=new[]{Key.LeftShift};await Wait(.2f);keys=new[]{Key.LeftShift,Key.F};await Wait(.15f);keys=Array.Empty<Key>();await Wait(.2f);
            if(!garden.Playing || !garden.Advanced)throw new Exception("Sprint + interact did not start the moving-star mode.");
            var first=garden.Star(0).localPosition;await Wait(.8f);
            if(Vector3.Distance(first,garden.Star(0).localPosition)<.2f)throw new Exception("Advanced stars did not move.");
            for(int i=0;i<3;i++)
            {
                var pad=garden.Pad(i);point=pad.position+new Vector3(0,0,-6);
                island=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
                if(!island.Raycast(new Ray(point+Vector3.up*50,Vector3.down),out ground,100))throw new Exception("No ground by moving-star spring");
                Place(player,body,ground.point,0);await Wait(.4f);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
                await Until(()=>player.WorldVelocity.y>9f && Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-pad.position,Vector3.up),Vector3.zero)<3.2f,6,"advanced bounce "+i);
                keys=Array.Empty<Key>();await Until(()=>garden.Collected>i,6,"moving star "+i);await Wait(1f);
            }
            if(inventory.Count(scroll)!=before+1)throw new Exception("Advanced mode duplicated the reward.");
            Status="passed";Result="Walked onto three springs in both gravity modes; Skyboard input, first reward, regular and moving-star replay, no duplicate reward and isolated save/continue passed.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally{keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputState.Change(keyboard,new KeyboardState());}
        Debug.Log("Candy Bounce Garden test: "+Status+" "+Result);
    }
}
