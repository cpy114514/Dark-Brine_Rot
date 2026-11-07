using System;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Exercises the friendly conversation, guide, portals, fountain and saved state with real input.</summary>
public static class VerifyFriendlyMarshmallowVillage
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
        float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(20);
        while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");await Task.Delay(20);}
    }
    static async Task Press(Key key)
    { keys=Array.Empty<Key>();await Wait(.15f);keys=new[]{key};await Wait(.12f);keys=Array.Empty<Key>();await Wait(.2f); }
    static void Place(ThirdPersonPlayerController p, Vector3 ground, float facing=0)
    {
        var b=p.GetComponent<CharacterController>();
        float sole=(b.center.y-b.height*.5f)*Mathf.Abs(p.transform.lossyScale.y);
        p.RestoreSavedPose(ground+Vector3.up*(.1f-sole),Quaternion.Euler(0,facing,0));
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/friendly-grove-test.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.4f);
            var v=UnityEngine.Object.FindFirstObjectByType<FriendlyMarshmallowVillage>();
            var p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var body=p.GetComponent<CharacterController>();keyboard=Keyboard.current;
            if(!v || !p || keyboard==null)throw new Exception("Village, player or keyboard missing.");
            InputSystem.onBeforeUpdate+=Inject;
            if(v.Unlocked || v.Entrance.gameObject.activeSelf || v.Grove.gameObject.activeSelf)throw new Exception("Secret is visible before conversation.");
            if(v.GetComponentsInChildren<MarshmallowEnemyHealth>(true).Length!=0 || v.GetComponentsInChildren<ChickenBroAI>(true).Length!=0)throw new Exception("Residents have enemy behaviour.");
            var ground=p.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var point=v.Guide.position+new Vector3(0,0,3f);
            if(!ground.Raycast(new Ray(point+Vector3.up*50,Vector3.down),out var hit,100))throw new Exception("No ground by guide.");
            Place(p,hit.point,180);await Wait(.5f);
            if(!body.isGrounded)throw new Exception("Player not grounded beside guide.");
            await Press(Key.F);
            if(!v.Talking || v.Unlocked)throw new Exception("Conversation did not open before unlocking.");
            var dialog=UnityEngine.Object.FindFirstObjectByType<SecondIslandDialogue>();
            if(!dialog || !dialog.Active || dialog.CurrentLine!=0)throw new Exception("First line missing.");
            for(int i=0;i<4;i++)await Press(Key.Space);
            await Wait(.3f);
            if(v.Talking || !v.Unlocked || !v.Entrance.gameObject.activeSelf || !v.Leading)throw new Exception("Dialogue did not unlock and start guide.");
            var before=v.Guide.position;await Wait(1.3f);
            if(Vector3.Distance(before,v.Guide.position)<1f)throw new Exception("Guide did not lead nearby player.");
            // Put the player beyond following range; the guide must wait rather than get lost.
            Place(p,hit.point+new Vector3(20,0,-15));await Wait(.3f);before=v.Guide.position;await Wait(.7f);
            if(Vector3.Distance(before,v.Guide.position)>.2f)throw new Exception("Guide did not wait for the player.");
            var ep=v.Entrance.position+new Vector3(0,0,-3);
            if(!ground.Raycast(new Ray(ep+Vector3.up*50,Vector3.down),out hit,100))throw new Exception("No ground by entrance.");
            Place(p,hit.point);await Wait(.5f);
            if(!v.Talking || v.InGrove || v.SmallLightOwned)throw new Exception("Approaching the door did not automatically start the marshmallow's warning.");
            var portraitStudio=(GameObject)typeof(FriendlyMarshmallowVillage).GetField("portraitStudio",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(v);
            var portraitLight=portraitStudio ? portraitStudio.GetComponentInChildren<Light>(true) : null;
            if(!portraitLight || portraitLight.type!=LightType.Point || Vector3.Distance(portraitLight.transform.position,p.transform.position)<=portraitLight.range)
                throw new Exception("Marshmallow portrait light can affect the island.");
            dialog=UnityEngine.Object.FindFirstObjectByType<SecondIslandDialogue>();
            var speaker=dialog ? Array.Find(dialog.GetComponentsInChildren<UnityEngine.UI.Text>(),t=>t.name=="Speaker") : null;
            if(!dialog || dialog.CurrentLine!=0 || !speaker || speaker.text!="MARSHMALLOW")throw new Exception("Marshmallow warning did not start on its first line.");
            for(int i=0;i<4;i++)await Press(Key.Space);
            await Wait(.3f);
            if(!v.DoorInspected || !v.Leading || !v.SmallLight.gameObject.activeSelf)throw new Exception("Door did not reveal the Small Light search and start the guide.");
            var guidePoint=v.Guide.position+new Vector3(0,0,3);
            if(!ground.Raycast(new Ray(guidePoint+Vector3.up*50,Vector3.down),out var guideGround,100))throw new Exception("No ground beside the guide.");
            Place(p,guideGround.point);await Wait(.3f);
            float beforeLight=Vector3.Distance(v.Guide.position,v.SmallLight.position);await Wait(.8f);
            if(Vector3.Distance(v.Guide.position,v.SmallLight.position)>=beforeLight-.5f)throw new Exception("Marshmallow did not lead toward the Small Light.");
            foreach(var renderer in v.SmallLight.GetComponentsInChildren<Renderer>())
                if(!renderer.GetComponent<Collider>() && !renderer.GetComponentInParent<Collider>(true))throw new Exception("Small Light part has no collider: "+renderer.name);
            var lampPoint=v.SmallLight.position+new Vector3(0,0,-3);
            if(!ground.Raycast(new Ray(lampPoint+Vector3.up*50,Vector3.down),out var lampGround,100))throw new Exception("No ground beside Small Light.");
            Place(p,lampGround.point);await Wait(.4f);await Press(Key.F);
            if(!v.SmallLightOwned || v.SmallLight.gameObject.activeSelf)throw new Exception("Could not collect the Small Light.");
            // The guide may be waiting by the door. Its talk prompt must not
            // steal the same F press used to enter with the light.
            v.Guide.position=hit.point+new Vector3(2,0,0);
            var normalScale=p.transform.localScale;Place(p,hit.point);await Wait(.5f);await Press(Key.F);
            await Wait(.9f);
            var performer=GameObject.Find("Small Light marshmallow performer");
            if(!performer || !performer.transform.Find("Bouncy visual/Right arm pivot/Handheld Small Light"))throw new Exception("The marshmallow did not hold the Small Light during the cutscene.");
            await Wait(1.1f);
            if(p.transform.localScale.x>=normalScale.x || p.transform.localScale.x<=normalScale.x*.38f)throw new Exception("Sahur did not shrink gradually during the beam.");
            await Wait(2f);
            if(!v.InGrove || !v.IsSmall || p.transform.localScale.x>=normalScale.x*.5f || Vector3.Distance(p.transform.position,v.Grove.position)>20 || !body.isGrounded)throw new Exception("Small Light entrance did not shrink and place the player safely in the grove: "+p.transform.position);
            foreach(var renderer in v.Grove.GetComponentsInChildren<Renderer>())
                if(!renderer.GetComponent<Collider>() && !renderer.GetComponentInParent<Collider>(true))throw new Exception("Visible grove object has no collider: "+renderer.name);
            var board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            if(!board || !board.CollectSpawnTestKit() || board.TryStartFlight())throw new Exception("Skyboard should not launch inside the tiny grove.");
            var walkStart=p.transform.position;
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Forward)};await Wait(.5f);keys=Array.Empty<Key>();await Wait(.7f);
            if(Vector3.Distance(walkStart,p.transform.position)<.2f)throw new Exception("Shrunken Sahur cannot walk around the grove.");
            Place(p,v.Grove.position+new Vector3(15,0,8));await Wait(.2f);
            body.Move(Vector3.right*4f);
            if(p.transform.position.x>v.Grove.position.x+17.2f)throw new Exception("The grove edge rail did not block the player.");
            var hp=p.GetComponent<PlayerHealth>();var stamina=p.GetComponent<PlayerStamina>();hp.currentHealth=40;stamina.currentStamina=10;
            Place(p,v.Grove.position+new Vector3(0,0,1));await Wait(1.7f);await Press(Key.F);
            if(!v.Rested || hp.currentHealth<=40 || stamina.currentStamina<stamina.maxStamina-.01f)throw new Exception("Fountain did not restore health/stamina.");
            var state=p.GetComponent<SecondIslandSaveState>();var snapshot=state.Capture();
            if(!snapshot.secretGroveUnlocked || !snapshot.secretGroveRested || !snapshot.secretDoorInspected || !snapshot.secretSmallLightOwned || !GameSaveManager.SaveCurrentGame())throw new Exception("Unlocked/rested/Small Light state was not saved.");
            v.RestoreProgress(false,false);if(v.Grove.gameObject.activeSelf)throw new Exception("Locked grove remained active.");
            state.Restore(snapshot);if(!v.Unlocked || !v.Rested || !v.Grove.gameObject.activeSelf)throw new Exception("Grove state did not restore.");
            if(!GameSaveManager.QueueContinue())throw new Exception("Could not queue the isolated save.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            float deadline=Time.realtimeSinceStartup+12f;
            while(GameSaveManager.IsContinuePending && Time.realtimeSinceStartup<deadline)await Task.Delay(30);
            await Wait(.5f);
            v=UnityEngine.Object.FindFirstObjectByType<FriendlyMarshmallowVillage>();
            p=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();body=p.GetComponent<CharacterController>();
            if(GameSaveManager.IsContinuePending || !v || !v.Unlocked || !v.Rested || !v.SmallLightOwned || !v.IsSmall || !v.InGrove || Vector3.Distance(p.transform.position,v.Grove.position)>20 || !body.isGrounded)
                throw new Exception("Continue did not restore the player safely inside the unlocked grove.");
            // Test the safety net independently of the portal, then use the actual return input.
            Place(p,v.Grove.position+new Vector3(37,-8,0));await Wait(.4f);
            if(!v.InGrove || Vector3.Distance(p.transform.position,v.Grove.position)>20)throw new Exception("Grove fall recovery failed.");
            // A death respawn or another system's long-distance teleport is not a fall.
            Place(p,v.Entrance.position+new Vector3(0,0,-3));await Wait(.4f);
            if(v.InGrove || v.IsSmall || Vector3.Distance(p.transform.position,v.Entrance.position)>10)throw new Exception("Island relocation was incorrectly treated as a grove fall.");
            await Wait(1.5f);
            if(!body.isGrounded || !v.ReservesInteraction)throw new Exception("Player was not safely grounded at the entrance before re-entry: pos="+p.transform.position+", feet="+body.bounds.min.y+", grounded="+body.isGrounded+", reserved="+v.ReservesInteraction);
            await Press(Key.F);await Wait(4f);
            if(!v.InGrove)throw new Exception("Could not re-enter the grove after relocation: pos="+p.transform.position+", grounded="+body.isGrounded+", talking="+v.Talking+", reserved="+v.ReservesInteraction+", board="+UnityEngine.Object.FindFirstObjectByType<SkyboardReward>().Flying);
            Place(p,v.Grove.position+new Vector3(0,0,-9));await Wait(1.7f);await Press(Key.F);await Wait(.4f);
            if(v.InGrove || v.IsSmall || Mathf.Abs(p.transform.localScale.x-normalScale.x)>.01f || Vector3.Distance(p.transform.position,v.Entrance.position)>10 || !body.isGrounded)throw new Exception("Return portal failed to restore full size: "+p.transform.position);
            board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            if(!board || !board.TryStartFlight())throw new Exception("Skyboard did not become available after leaving the grove.");
            if(UnityEngine.Object.FindObjectsByType<MarshmallowEnemyHealth>(FindObjectsSortMode.None).Length!=70)throw new Exception("Enemy count changed.");
            Status="passed";Result="Door requires Small Light; pickup, shrinking, movement and return, fountain, full save/continue, fall recovery and grove colliders passed. 70 enemies unchanged.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally{keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null)InputState.Change(keyboard,new KeyboardState());}
        Debug.Log("Friendly village test: "+Status+" "+Result);
    }
}
