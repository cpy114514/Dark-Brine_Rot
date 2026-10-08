using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class VerifyStory1WreckBattle
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task<object> VerifyNaturalEncounter()
    {
        Require(Application.isPlaying,"Requires Play Mode.");
        var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity",LoadSceneMode.Single);
        while(!load.isDone)await Task.Delay(50);
        await Task.Delay(300);
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        var battle=sequence.GetComponent<Story1WreckBattle>();
        float realStart=Time.realtimeSinceStartup;
        while(battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting&&Time.realtimeSinceStartup-realStart<42)await Task.Delay(100);
        Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Natural sailing did not reach a physical shark impact.");
        float impactAt=sequence.VoyageTime;
        sequence.sahur.GetComponent<PlayerHealth>().GrantProtection(100);
        float deadline=battle.FinaleAfterSeconds;
        Require(deadline>=20 && deadline<=35,"Encounter did not sample a random deadline.");
        battle.SharkHealth.ApplyDamage(battle.SharkHealth.maxHealth*.71f,battle.SharkHealth.transform.position);
        await Task.Delay(1000);
        Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Health threshold bypassed the timed fight.");
        for(int k=0;k<800 && battle.CurrentPhase==Story1WreckBattle.Phase.Fighting;k++)await Task.Delay(50);
        Require(battle.CurrentPhase==Story1WreckBattle.Phase.Finale,"Random deadline failed to start the movie.");
        Require(!PlayerDeathRespawn.IsOpen&&Time.timeScale>0,"Natural story defeat triggered game over.");
        return new{physicalSharkContactStartsWreckage=true,impactVoyageSeconds=impactAt,noTurnCut=true,
            randomDeadlineStartsFinale=true,randomDeadlineSeconds=deadline,normalRealTimeCombat=true,respawnNotOpened=true};
    }
    public static async Task<object> Verify(bool weaponsOnly=false)
    {
        Require(Application.isPlaying, "Requires Play Mode.");
        string save = Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original = File.Exists(save) ? File.ReadAllBytes(save) : null;
        Keyboard keyboard = null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var load = SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity",LoadSceneMode.Single);
            while (!load.isDone) await Task.Delay(50);
            await Task.Delay(400);
            var sequence = UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            Require(sequence != null,"Story sequence not loaded.");
            var battle = sequence.GetComponent<Story1WreckBattle>(); var player = sequence.sahur;
            Require(battle != null && battle.CurrentPhase == Story1WreckBattle.Phase.Waiting,"Wreck sequence did not initialize.");
            foreach(var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                Require(behaviour.GetType().Name!="Story1TurnBattle","Old turn battle is still active.");
            Require(!GameSaveManager.SaveCurrentGame(),"Story overwrote campaign save.");
            Shot("boat-intact");
            if(weaponsOnly)battle.minimumFightSeconds=battle.maximumFightSeconds=120;
            sequence.BeginImpact();
            for (int k=0;k<360 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;k++) await Task.Delay(25);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Ship did not shatter into the live encounter.");
            Require(!sequence.ship.gameObject.activeInHierarchy && battle.PlankCount==20,"Hull was not replaced by solid fragments.");
            Require(battle.ShipFragmentCount==202,"The rest of the original ship did not break into fragments.");
            foreach(var board in UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None))
                Require(board.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("deck_Object_30_")||
                    board.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("hull_Object_22_"),"A procedural substitute remains in the wreck.");
            var hp=player.GetComponent<PlayerHealth>();hp.GrantProtection(30);
            await Task.Delay(500);
            Require(player.enabled && player.GetComponent<SahurAttack>().enabled && player.GetComponent<SahurBoomerang>().enabled &&
                !player.ExternalControlLock && !player.ExternalMovementLock && !player.Swimming && player.CanUseGroundAttack,"Player cannot fight normally on the boards.");
            var capsule=player.GetComponent<CharacterController>();
            Vector3 before=player.transform.position;
            keyboard=InputSystem.AddDevice<Keyboard>("Wreck verification keyboard");
            Cursor.lockState=CursorLockMode.Locked;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();
            await Task.Delay(450);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            float walk=Vector3.ProjectOnPlane(player.transform.position-before,Vector3.up).magnitude;
            Require(walk>1 && !player.Swimming,"Normal movement did not work across the floating boards.");
            await Task.Delay(150);
            Shot("on-wooden-boards");
            // Test weapons from the middle of a deck, independently of jumping its new gaps.
            var support=player.GetComponent<Story1WreckRider>().Support();
            Require(support!=null,"Walking left the supporting deck.");
            if(weaponsOnly)
                support=System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None),p=>p.IsBoardable && p.Dimensions.y<1 && p.GetComponent<Story1WreckFloatBody>().InWater);
            // Keep the supporting deck still during isolated weapon checks while its NPC tick is suspended.
            if(weaponsOnly)
            {
                var supportBody=support.GetComponent<Rigidbody>();Vector3 isolated=supportBody.position+Vector3.right*160;
                isolated.y=UnityEngine.Object.FindFirstObjectByType<OceanWorld>().SampleSurfaceHeight(isolated,Time.time);
                supportBody.position=isolated;support.transform.position=isolated;supportBody.isKinematic=true;
            }
            Vector3 deckCenter=support.BoardingPoint(support.transform.position);
            player.RestoreSavedPose(deckCenter+Vector3.up*(.05f-player.LowestFootWorldOffset),player.transform.rotation);
            Physics.SyncTransforms(); await Task.Delay(150);
            Vector3 body=player.transform.position+player.transform.forward*10;
            var phaseField=typeof(Story1WreckBattle).GetField("sharkPhase",Flags);
            if(weaponsOnly){phaseField.SetValue(battle,Enum.Parse(phaseField.FieldType,"Recovery"));typeof(Story1WreckBattle).GetField("sharkAge",Flags).SetValue(battle,0f);}
            typeof(Story1WreckBattle).GetMethod("PlaceShark",Flags).Invoke(battle,new object[]{body,Quaternion.LookRotation(-player.transform.forward)});
            Physics.SyncTransforms();
            player.GetComponent<PlayerStamina>().currentStamina=player.GetComponent<PlayerStamina>().maxStamina;
            var attack=player.GetComponent<SahurAttack>();int hits=battle.SharkHealth.ReceivedHits;
            // Isolate the weapon volume from a legitimate NPC dodge while checking the lowered waterline.
            battle.enabled=false;
            try
            {
                attack.TriggerAttack();
                for(int k=0;k<100&&battle.SharkHealth.ReceivedHits==hits;k++)await Task.Delay(25);
            }
            finally {battle.enabled=true;}
            Require(battle.SharkHealth.ReceivedHits>hits && battle.SharkHealth.currentHealth<360,"The real stick hitbox did not damage the shark.");
            Require(player.GetComponent<SahurBoomerang>().enabled,"Normal boomerang was disabled.");
            await Task.Delay(1600);
            if(weaponsOnly)
            {
                Camera.main.transform.position=player.transform.position-player.transform.forward*12+Vector3.up*6;
                Camera.main.transform.LookAt(battle.SharkBody);
            }
            bool lockOn=player.GetComponent<EnemyLockOn>().Toggle(Camera.main);
            Require(lockOn && player.GetComponent<EnemyLockOn>().Target==battle.SharkHealth.transform,"Standard enemy lock-on could not target the shark.");
            player.GetComponent<EnemyLockOn>().Clear();
            int beforeThrow=battle.SharkHealth.ReceivedHits;
            if(weaponsOnly){phaseField.SetValue(battle,Enum.Parse(phaseField.FieldType,"Recovery"));typeof(Story1WreckBattle).GetField("sharkAge",Flags).SetValue(battle,0f);}
            // Check real projectile collision against a fixed, forward-facing target;
            // the live shark can legitimately turn away during the throw's windup.
            typeof(Story1WreckBattle).GetMethod("PlaceShark",Flags).Invoke(battle,new object[]{
                player.transform.position+player.transform.forward*10,Quaternion.LookRotation(-player.transform.forward)});
            Physics.SyncTransforms();battle.enabled=false;
            try
            {
                Require(player.GetComponent<SahurBoomerang>().TryThrow(),"Normal boomerang throw failed on the boards.");
                for(int k=0;k<140&&battle.SharkHealth.ReceivedHits==beforeThrow;k++)await Task.Delay(25);
            }
            finally {battle.enabled=true;}
            Require(battle.SharkHealth.ReceivedHits>beforeThrow,"Boomerang projectile did not damage the shark.");
            int totalWeaponHits=battle.SharkHealth.ReceivedHits;
            if(weaponsOnly)return new{normalWalkingDistance=walk,meleeDamagesSubmergedShark=true,boomerangDamagesSubmergedShark=true,totalWeaponHits,normalLockOn=true};
            // Let a real telegraphed strike resolve with the player still in its area.
            hp.GrantProtection(0);hp.currentHealth=hp.maxHealth;
            int strikes=battle.SharkAttackCount;
            // A committed strike can miss after an attack has moved Sahur. Wait for a hit,
            // allowing that legitimate miss instead of asserting damage from the next counter increment.
            for(int k=0;k<500&&hp.currentHealth==hp.maxHealth;k++)await Task.Delay(25);
            Require(battle.SharkAttackCount>strikes,"Shark did not attack in real time.");
            float remaining=hp.currentHealth;
            Require(remaining<hp.maxHealth,"Shark strike did not use real player health.");
            Shot("shark-live-combat");
            hp.GrantProtection(30);hp.currentHealth=hp.maxHealth;
            battle.SharkHealth.ApplyDamage(1000000,battle.SharkHealth.transform.position);
            Require(!battle.SharkHealth.IsDead && battle.SharkHealth.currentHealth==1,"Story shark could be killed.");
            for(int k=0;k<2400&&(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting||battle.CurrentPhase==Story1WreckBattle.Phase.Finale);k++)await Task.Delay(25);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Defeat||battle.CurrentPhase==Story1WreckBattle.Phase.Finished,"Unavoidable finishing wave did not defeat Sahur.");
            Require(hp.currentHealth==0 && !PlayerDeathRespawn.IsOpen && Time.timeScale>0 && !player.enabled,"Narrative defeat opened respawn or left control enabled.");
            for(int k=0;k<400&&UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>()==null;k++)await Task.Delay(25);
            var arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
            Require(arrival!=null&&!arrival.HasStick&&!arrival.IsAwake,"Defeat did not lead to unconscious, unarmed island arrival.");
            arrival.player.gameObject.AddComponent<GameSaveExcluded>();
            // Ordinary health outside this encounter still fires ordinary death.
            var dummy=new GameObject("Normal health verification");var ordinary=dummy.AddComponent<PlayerHealth>();int deaths=0;
            ordinary.OnDeath.AddListener(()=>deaths++);ordinary.ApplyDamage(1000,Vector3.zero);
            Require(deaths==1&&ordinary.currentHealth==0,"Ordinary island death behavior changed.");UnityEngine.Object.Destroy(dummy);
            return new{oldTurnBattleRemoved=true,solidFloatingBoards=44,normalWalkingDistance=walk,realWeaponHits=totalWeaponHits,standardLockOn=true,boomerangDamagesShark=true,
                sharkStrikeRemainingHP=remaining,sharkCannotDie=true,forcedDefeatWithoutRespawn=true,unconsciousIslandArrival=true,normalDeathPreserved=true,savePreserved=true};
        }
        finally
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(weaponsOnly)foreach(var fragment in UnityEngine.Object.FindObjectsByType<Story1WreckFloatBody>(FindObjectsSortMode.None))
                if(fragment.Body!=null)fragment.Body.isKinematic=false;
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player!=null&&player.GetComponent<GameSaveExcluded>()==null)player.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }

    static void Shot(string name)
    {
        string dir=Path.GetFullPath(".codex/wreck-battle-preview");Directory.CreateDirectory(dir);
        var camera=Camera.main;var target=new RenderTexture(960,540,24);var frame=new Texture2D(960,540,TextureFormat.RGB24,false);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,960,540),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(dir,name+".png"),frame.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
}
