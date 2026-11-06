using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Flies the real board with keyboard input through the authored ring course.</summary>
public static class VerifySkyboardRingRally
{
    public static string Status="idle",Result="";
    static Keyboard keyboard;
    static ThirdPersonPlayerController player;
    static CharacterController body;
    static SkyboardRingRally rally;
    static SkyboardReward board;
    static bool steer;
    static Key[] keys=Array.Empty<Key>();
    static readonly FieldInfo Yaw=typeof(ThirdPersonPlayerController).GetField("yaw",BindingFlags.Instance|BindingFlags.NonPublic);
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        if(steer && player && body && rally && board && board.Flying)
        {
            int index=rally.Running?rally.NextRing:0;
            var ring=rally.RingTransform(index);
            if(ring)
            {
                Vector3 delta=ring.position+ring.forward*9f-body.bounds.center;
                Vector3 flat=Vector3.ProjectOnPlane(delta,Vector3.up);
                if(flat.sqrMagnitude>.01f)Yaw.SetValue(player,Quaternion.LookRotation(flat).eulerAngles.y);
                float vertical=ring.position.y-body.bounds.center.y;
                if(vertical>1f)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Jump)};
                else if(vertical< -1f)keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Dodge)};
                else keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};
            }
        }
        InputState.Change(keyboard,new KeyboardState(keys));
    }
    static async Task Wait(float seconds)
    {
        float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(18);
        while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");await Task.Delay(20);}
    }
    static void Place(Vector3 desiredCenter,Quaternion facing)
    {
        Vector3 offset=body.bounds.center-player.transform.position;
        player.RestoreSavedPose(desiredCenter-offset,facing);
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/skyboard-rally-test.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.4f);
            player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();rally=UnityEngine.Object.FindFirstObjectByType<SkyboardRingRally>();
            board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();body=player.GetComponent<CharacterController>();keyboard=Keyboard.current;
            if(!player || !rally || !board || keyboard==null || rally.TotalRings!=10)throw new Exception("Missing player, board or ten-ring course.");
            player.GetComponent<PlayerHealth>().GrantProtection(120);
            if(!board.CollectSpawnTestKit())throw new Exception("Could not equip test board.");
            for(int i=0;i<rally.TotalRings;i++)
            {
                var r=rally.RingTransform(i);var mesh=r.GetComponent<MeshCollider>();var trigger=r.GetComponent<BoxCollider>();
                if(!mesh || !trigger || !trigger.isTrigger)throw new Exception("Ring collider is missing.");
                if(r.gameObject.activeSelf!=(i==0))throw new Exception("Only the first ring should be visible before the race.");
                if(i==0 && !mesh.Raycast(new Ray(r.position+r.right*6.5f+r.forward*4,-r.forward),out _,8))throw new Exception("Ring rim did not block a ray.");
                if(i==0 && mesh.Raycast(new Ray(r.position+r.forward*4,-r.forward),out _,8))throw new Exception("Ring opening is blocked.");
            }
            var start=rally.RingTransform(0);
            // Start on the actual island, not in mid-air, and climb into ring 1.
            var takeoff=start.position-start.forward*18+start.right*8;
            var island=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            if(!island.Raycast(new Ray(takeoff+Vector3.up*100,Vector3.down),out var groundHit,200))throw new Exception("No island ground at rally takeoff.");
            takeoff.y=groundHit.point.y+body.bounds.extents.y+.12f;Place(takeoff,start.rotation);await Wait(.35f);
            if(!body.isGrounded)throw new Exception("Player did not reach the rally takeoff ground.");
            if(!board.TryStartFlight())throw new Exception("Unable to start flight.");
            InputSystem.onBeforeUpdate+=Inject;await Wait(.3f);
            steer=true;float takeoffDeadline=Time.time+6f;
            while(!rally.Running && Time.time<takeoffDeadline)await Task.Delay(25);
            steer=false;keys=Array.Empty<Key>();await Wait(.15f);
            if(!rally.Running || rally.NextRing!=1)throw new Exception("Could not ascend from the ground and cross the starting ring: "+body.bounds.center);
            for(int i=0;i<rally.TotalRings;i++)if(rally.RingTransform(i).gameObject.activeSelf!=(i==1))throw new Exception("Passing ring 1 did not reveal only ring 2.");
            var movingRing=rally.RingTransform(1);var beforeMove=movingRing.position;
            float remaining=rally.TimeUntilMove;
            if(remaining>1)await Wait(remaining-.5f);
            if(rally.Relocating || Vector3.Distance(movingRing.position,beforeMove)>.05f)throw new Exception("Ring moved before 15 seconds elapsed.");
            float moveDeadline=Time.time+3;
            while(!rally.Relocating && Time.time<moveDeadline)await Task.Delay(25);
            if(!rally.Relocating)throw new Exception("Ring did not start moving after 15 seconds.");
            await Wait(1.1f);
            float shift=Vector3.Distance(beforeMove,movingRing.position);
            if(rally.Relocating || shift<1f || shift>100f || rally.NextRing!=1)throw new Exception("Ring relocation was incorrect: shift="+shift+", next="+rally.NextRing);
            for(int i=0;i<rally.TotalRings;i++)if(rally.RingTransform(i).gameObject.activeSelf!=(i==1))throw new Exception("Relocation exposed more than the active ring.");
            // Use one of the temporary test scrolls to obtain a fresh full
            // flight after the 15-second waiting check.
            typeof(SkyboardReward).GetMethod("EndFlight",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board,null);await Wait(.3f);
            if(!board.TryUseScrollOn(board) || !board.TryStartFlight())throw new Exception("Could not restart test flight after relocation check.");
            await Wait(.3f);
            // A later ring cannot start a race. Move through it in the correct direction.
            var second=rally.RingTransform(1);Place(second.position-second.forward*7,second.rotation);await Wait(.2f);
            Yaw.SetValue(player,second.rotation.eulerAngles.y);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};await Wait(1.1f);keys=Array.Empty<Key>();await Wait(.2f);
            if(rally.Running)throw new Exception("Wrong ring incorrectly started the race.");
            // Start ring must be crossed in the forward direction.
            Place(start.position+start.forward*7,Quaternion.LookRotation(-start.forward));await Wait(.2f);
            Yaw.SetValue(player,Quaternion.LookRotation(-start.forward).eulerAngles.y);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};await Wait(1.1f);keys=Array.Empty<Key>();await Wait(.2f);
            if(rally.Running)throw new Exception("Reverse crossing incorrectly started the race.");
            Place(start.position-start.forward*9,start.rotation);await Wait(.2f);
            // Start once and exercise the in-flight restart without changing board cooldown.
            Yaw.SetValue(player,start.rotation.eulerAngles.y);keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};await Wait(1f);keys=Array.Empty<Key>();await Wait(.2f);
            if(!rally.Running || rally.NextRing!=1)throw new Exception("Forward crossing did not start ring 1.");
            keys=new[]{Key.R};await Wait(.12f);keys=Array.Empty<Key>();await Wait(.3f);
            if(rally.Running || !board.Flying)throw new Exception("In-flight reset failed or stopped the board.");
            Place(start.position-start.forward*9,start.rotation);await Wait(.2f);
            float startingSpeed=board.horizontalSpeed;
            int beforeScrolls=board.Scrolls;
            int prior=-1;float changed=Time.time;bool sawBoost=false;steer=true;
            while(!rally.RewardClaimed && Time.time-changed<9f && board.Flying)
            {
                if(board.horizontalSpeed>startingSpeed*1.1f)sawBoost=true;
                int progress=rally.Running?rally.NextRing:0;
                if(progress!=prior)
                {
                    prior=progress;changed=Time.time;Status="flying ring "+(progress+1)+"/10";
                    for(int i=0;i<rally.TotalRings;i++)if(rally.RingTransform(i).gameObject.activeSelf!=(i==progress))throw new Exception("Unexpected visible ring at progress "+progress);
                }
                await Task.Delay(30);
            }
            steer=false;keys=Array.Empty<Key>();await Wait(.25f);
            if(!rally.RewardClaimed)throw new Exception("Could not fly the full course. progress="+rally.NextRing+", running="+rally.Running+", player="+body.bounds.center+", board remaining="+board.FlightRemaining);
            var inventory=player.GetComponent<EquipmentInventory>();var scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
            int scrolls=inventory.Count(scroll);float best=rally.BestTime;
            if(scrolls!=beforeScrolls+1 || best<=0 || best>55 || !sawBoost || Mathf.Abs(board.horizontalSpeed-startingSpeed)>.01f)throw new Exception("Finish, reward or boost was incorrect: scrolls="+scrolls+", best="+best+", sawBoost="+sawBoost+", speed="+board.horizontalSpeed);
            var snapshot=player.GetComponent<SecondIslandSaveState>().Capture();
            if(!snapshot.skyboardRallyReward || snapshot.skyboardRallyBest<=0 || !GameSaveManager.SaveCurrentGame())throw new Exception("Rally result was not saved.");
            if(!GameSaveManager.QueueContinue())throw new Exception("Could not queue isolated save.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            float deadline=Time.realtimeSinceStartup+15;
            while(GameSaveManager.IsContinuePending && Time.realtimeSinceStartup<deadline)await Task.Delay(30);
            await Wait(.5f);
            rally=UnityEngine.Object.FindFirstObjectByType<SkyboardRingRally>();player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();body=player.GetComponent<CharacterController>();
            if(GameSaveManager.IsContinuePending || !rally || !rally.RewardClaimed || Mathf.Abs(rally.BestTime-best)>.1f || player.GetComponent<EquipmentInventory>().Count(scroll)!=scrolls)
                throw new Exception("Continue did not restore the reward and best time.");
            // Calling the finish path a second time must not duplicate the prize.
            typeof(SkyboardRingRally).GetMethod("Finish",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rally,null);
            if(player.GetComponent<EquipmentInventory>().Count(scroll)!=scrolls)throw new Exception("Repeated finish duplicated the speed scroll.");
            Status="passed";Result=$"Only one ring visible, moved {shift:F1} units after 15 seconds; flew all 10 in {best:F1}s. Restart, boost, single reward and save/continue passed.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally{steer=false;keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputState.Change(keyboard,new KeyboardState());}
        Debug.Log("Skyboard rally test: "+Status+" "+Result);
    }
}
