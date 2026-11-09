using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Walk-up pickup, unlimited use and isolated-save checks for the editor test area.</summary>
public static class VerifySecondIslandTestStation
{
    public static string Status="idle",Result="";
    static Keyboard keyboard,previousKeyboard;static Key[] keys=Array.Empty<Key>();
    static void Inject(){if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));}
    static async Task Wait(float seconds)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(Time.realtimeSinceStartup<end){if(!EditorApplication.isPlaying)throw new Exception("Play mode stopped.");await Task.Delay(20);}
    }
    static async Task Press(Key key){keys=Array.Empty<Key>();await Wait(.18f);keys=new[]{key};await Wait(.22f);keys=Array.Empty<Key>();await Wait(.25f);}
    static void Place(ThirdPersonPlayerController player,CharacterController body,Collider island,Vector3 target)
    {
        var bounds=island.bounds;var origin=new Vector3(target.x,bounds.max.y+10,target.z);
        if(!island.Raycast(new Ray(origin,Vector3.down),out var hit,bounds.size.y+20))throw new Exception("No ground near "+target);
        float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(hit.point+Vector3.up*(.1f-sole),Quaternion.identity);
    }
    static Button FindButton(string prefix)
    {
        foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {var t=button.GetComponentInChildren<Text>();if(button.gameObject.activeInHierarchy && t && t.text.StartsWith(prefix,StringComparison.Ordinal))return button;}
        return null;
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            string isolated=Path.GetFullPath("Temp/test-station-verification-save.json");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,isolated);
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.7f);
            if(UnityEngine.Object.FindFirstObjectByType<SecondIslandTestStation>())throw new Exception("Test gear spawned without a verification run.");
            var station=new GameObject("Test station verification fixture").AddComponent<SecondIslandTestStation>();
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var body=player.GetComponent<CharacterController>();var hp=player.GetComponent<PlayerHealth>();var inventory=player.GetComponent<EquipmentInventory>();
            var island=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();var board=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            if(!station || !station.enabled || station.PedestalCount!=7 || !player || !body || !hp || !inventory || !island || !board)throw new Exception("Test area or player components missing.");
            if(GameSaveManager.EditorTestSessionActive || SecondIslandTestStation.UnlimitedSkyboard || SecondIslandTestStation.UnlimitedScrolls)throw new Exception("Test mode should be opt-in.");
            foreach(var renderer in station.GetComponentsInChildren<Renderer>(true))
                if(!renderer.GetComponent<Collider>() && !renderer.GetComponentInParent<Collider>(true))throw new Exception("Test model lacks a collider: "+renderer.name);
            var originalCampaign=HealingPackSupply.Campaign;var originalCount=HealingPackSupply.Remaining;
            SessionState.SetString("SecondIslandTestOriginalCampaign",originalCampaign);SessionState.SetInt("SecondIslandTestOriginalHealing",originalCount);
            previousKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Inject;
            Place(player,body,island,station.PedestalAt(3).position+new Vector3(0,0,-2.2f));await Wait(.5f);
            keys=new[]{GameInputSettings.Get(GameInputSettings.Action.Left)};await Wait(.25f);keys=Array.Empty<Key>();await Wait(.25f);
            var startProtection=player.GetComponent<SecondIslandStartProtection>();
            if(!startProtection || startProtection.WaitingForMovement || !SecondIslandTestStation.Protects(hp))throw new Exception("Moving at the pickup pads did not retain the local safety zone.");
            var hostile=UnityEngine.Object.FindObjectsByType<MarshmallowEnemyVisual>(FindObjectsSortMode.None).Where(x=>Vector3.Distance(x.transform.position,player.transform.position)<25).ToArray();
            if(hostile.Any(x=>x.IsAlert))throw new Exception("Marshmallows remain aggressive inside the test area.");
            async Task Take(int index)
            {
                var stationPoint=station.PedestalAt(index).position+new Vector3(0,0,-2.2f);Place(player,body,island,stationPoint);await Wait(.45f);
                if(!SecondIslandTestStation.Protects(hp))throw new Exception("Spawn test area did not protect player.");
                await Press(Key.F);
                if(string.IsNullOrEmpty(station.LastMessage))throw new Exception("Pickup did not respond: "+index);
            }
            Status="collecting unlimited skyboard";await Take(0);
            if(!GameSaveManager.EditorTestSessionActive || !SecondIslandTestStation.UnlimitedSkyboard || !board.Owned || board.Flying || board.CooldownRemaining>0)throw new Exception("Skyboard pickup or interaction reservation failed.");
            var stamina=player.GetComponent<PlayerStamina>();stamina.currentStamina=0;
            if(!stamina.TrySpend(1000) || !stamina.TickSprint(1) || !stamina.CanSpend(1000))throw new Exception("Test mode still restricts stamina.");
            await Wait(.2f);if(stamina.currentStamina<stamina.maxStamina-.01f)throw new Exception("Test stamina was not restored.");
            if((string)typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)!=isolated)throw new Exception("Test session changed the isolated save path.");
            if(HealingPackSupply.Campaign==originalCampaign)throw new Exception("Test healing ledger was not isolated.");
            Place(player,body,island,station.transform.position+new Vector3(-30,0,0));await Wait(.25f);
            if(SecondIslandTestStation.Protects(hp))throw new Exception("The test safety zone extends beyond the spawn area.");
            var away=station.transform.position+new Vector3(0,0,-8);Place(player,body,island,away);await Wait(.4f);board.flightSeconds=1f;
            await Press(Key.F);if(!board.Flying || !float.IsPositiveInfinity(board.CeilingHeight))throw new Exception("Unlimited board did not start or height remained limited.");
            await Wait(1.5f);if(!board.Flying || board.FlightRemaining<.9f)throw new Exception("Test flight expired.");
            await Press(Key.F);if(board.Flying || board.CooldownRemaining>0)throw new Exception("Test board did not put away without cooldown.");
            await Press(Key.F);if(!board.Flying)throw new Exception("Test board could not immediately fly again.");await Wait(.35f);await Press(Key.F);
            if(board.Flying || board.CooldownRemaining>0)throw new Exception("Second test flight did not end freely.");
            Status="checking scroll selection";await Take(1);
            var scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);var timer=player.GetComponent<SecondIslandTestTimer>();int scrolls=inventory.Count(scroll);
            if(!SecondIslandTestStation.UnlimitedScrolls || scrolls<10 || !timer)throw new Exception("Unlimited test scrolls or practice target missing.");
            var ui=player.GetComponent<SahurLoadoutUI>();if(!ui)throw new Exception("Equipment UI missing.");ui.SetOpen(true);if(!ui.PanelVisible)throw new Exception("Could not open equipment.");
            ui.SelectEquipment(SahurLoadoutUI.EquipmentSlot.Tools);var card=FindButton("Speed Scroll");if(!card)throw new Exception("Scroll is missing from Tools.");
            card.onClick.Invoke();await Press(Key.Digit4);var target=FindButton("TEST TIMER");if(!target)throw new Exception("Practice target is missing from scroll selection.");
            target.onClick.Invoke();await Press(Key.Digit4);
            if(timer.Uses!=1 || inventory.Count(scroll)!=scrolls || board.CooldownRemaining>0)throw new Exception("Scroll selection was not unlimited or imposed a real cooldown.");
            ui.SetOpen(false);await Wait(.25f);
            Status="checking gravity and weapons";await Take(2);
            var gravity=player.GetComponent<GravityWristbandAbility>();if(!gravity || !gravity.Owned)throw new Exception("Wristband not granted.");
            bool wasNormal=gravity.NormalGravity;await Press(Key.B);if(gravity.NormalGravity==wasNormal)throw new Exception("Wristband did not switch gravity.");
            await Take(3);var weapons=player.GetComponent<SahurWeaponLoadout>();await Press(Key.Digit2);if(!weapons || !weapons.HasTwinBlades || !weapons.UsesTwinBlades)throw new Exception("Twin blades could not be equipped.");
            await Take(4);await Press(Key.Digit3);if(!weapons.HasM4A1 || !weapons.UsesRifle)throw new Exception("Rifle could not be equipped.");
            var rifle=player.GetComponent<SahurRifleCombat>();rifle.RestoreAmmo(1,0);await Wait(.3f);
            if(!SecondIslandTestStation.UnlimitedRifle || !rifle.TryFire())throw new Exception("Unlimited test rifle could not fire.");
            await Wait(.3f);if(rifle.Rounds<=0 || rifle.ReserveRounds!=SahurRifleCombat.ReserveCapacity || rifle.IsReloading)throw new Exception("Test rifle ammunition ran out or forced a reload.");
            Status="checking glasses and healing";await Take(5);
            var glasses=player.GetComponent<WeaknessGlasses>();if(!glasses || !glasses.Owned || !glasses.Equip())throw new Exception("Glasses could not be equipped.");
            await Press(Key.G);if(!glasses.IsActive || glasses.CooldownRemaining>0)throw new Exception("Test glasses did not activate without cooldown.");
            typeof(WeaknessGlasses).GetField("activeUntil",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(glasses,Time.time+.2f);await Wait(.6f);
            if(!glasses.IsActive || glasses.CooldownRemaining>0 || !glasses.Activate())throw new Exception("Test glasses expired or could not be reused.");
            await Take(6);int packs=HealingPackSupply.Remaining;hp.currentHealth=30;await Press(Key.H);
            if(hp.currentHealth<=30 || HealingPackSupply.Remaining!=packs)throw new Exception("Test healing did not work without consuming a pack.");
            hp.currentHealth=30;await Press(Key.H);if(hp.currentHealth<=30 || HealingPackSupply.Remaining!=packs)throw new Exception("Test healing could not be repeated.");
            if(!GameSaveManager.SaveCurrentGame() || !File.Exists(isolated))throw new Exception("Separate test save was not written.");
            var contents=File.ReadAllText(isolated);if(!contents.Contains("skyboard") || !contents.Contains("speed_scroll"))throw new Exception("Test save did not contain the collected test gear.");
            Status="passed";Result="Seven physical pickups; safe start, separate save, unlimited flight/height and instant reuse, scroll menu practice, gravity, both weapons, glasses and repeatable healing passed.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally{keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);if(previousKeyboard!=null && previousKeyboard.added)previousKeyboard.MakeCurrent();keyboard=previousKeyboard=null;}
        Debug.Log("Second island test station: "+Status+" "+Result);
    }
}
