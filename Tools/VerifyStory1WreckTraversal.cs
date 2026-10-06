using System;
using System.IO;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class VerifyStory1WreckTraversal
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static Keyboard keys;
    static async Task Hold(int milliseconds, params Key[] pressed)
    {
        InputSystem.QueueStateEvent(keys, new KeyboardState(pressed));
        await Task.Delay(milliseconds);
    }
    static async Task Tap(Key key) { await Hold(120, key); await Hold(140); }
    static Vector3 Feet(ThirdPersonPlayerController player) => player.transform.position + Vector3.up * player.LowestFootWorldOffset;

    public static async Task<object> Verify()
    {
        Require(Application.isPlaying, "Requires Play Mode.");
        string save = Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original = File.Exists(save) ? File.ReadAllBytes(save) : null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var load = SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity", LoadSceneMode.Single);
            while (!load.isDone) await Task.Delay(50);
            await Task.Delay(400);
            var sequence = UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle = sequence.GetComponent<Story1WreckBattle>();
            sequence.BeginImpact();
            for (int i=0;i<300 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++) await Task.Delay(30);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Battle did not begin.");
            battle.maximumFightSeconds = 120;
            var player = sequence.sahur; var health = player.GetComponent<PlayerHealth>(); health.GrantProtection(200);
            var rider = player.GetComponent<Story1WreckRider>(); var first = rider.Support();
            Require(first!=null && !player.ExternalMovementLock,"Normal board movement was replaced by a mount lock.");
            keys = InputSystem.AddDevice<Keyboard>("Wreck traversal verification keyboard"); Cursor.lockState = CursorLockMode.Locked;
            await Task.Delay(200);
            await Tap(Key.F);
            Require(rider.IsSurfing && rider.DrivenBoard==first,"F did not start surfing on the supporting board.");
            Vector3 boardStart = first.transform.position;
            await Hold(1700,GameInputSettings.Get(GameInputSettings.Action.Forward)); await Hold(150);
            float distance = Vector3.ProjectOnPlane(first.transform.position-boardStart, Vector3.up).magnitude;
            Require(distance>5 && rider.IsSurfing && !player.Swimming,"Controllable board did not move with its rider: "+distance);
            float yaw = first.transform.eulerAngles.y;
            await Hold(550,GameInputSettings.Get(GameInputSettings.Action.Right)); await Hold(120);
            float turn = Mathf.Abs(Mathf.DeltaAngle(yaw,first.transform.eulerAngles.y));
            Require(turn>15 && rider.IsSurfing,"Board did not steer with its rider: "+turn);
            Require(player.CanUseGroundAttack && player.GetComponent<SahurAttack>().enabled && player.GetComponent<SahurBoomerang>().enabled,"Mounting disabled normal combat.");
            var attack = player.GetComponent<SahurAttack>(); attack.TriggerAttack(); await Task.Delay(160);
            Require(attack.IsCombatMotionActive && rider.IsSurfing,"Attack animation was cancelled by mounted locomotion.");
            await Task.Delay(1800);
            Shot("surfing-on-hull-board");
            float takeoff = Feet(player).y, highest = takeoff;
            await Hold(70,GameInputSettings.Get(GameInputSettings.Action.Jump)); await Hold(20);
            for(int i=0;i<20;i++) { highest=Mathf.Max(highest,Feet(player).y); await Task.Delay(25); }
            Require(!rider.IsSurfing && !player.ExternalMovementLock && highest-takeoff>.7f,"Space did not launch a normal jump off the board: "+(highest-takeoff));
            await Task.Delay(1000);

            // Find a real crossing in the current randomly scattered, rotated wreck.
            var boards = UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None);
            Story1WreckPlank jumpFrom=null, jumpTo=null;
            Vector3 from=Vector3.zero, target=Vector3.zero;
            float shortest=float.PositiveInfinity;
            foreach(var a in boards) foreach(var b in boards)
            {
                if(a==b || a.Dimensions.x<3 || b.Dimensions.x<3 || a.Speed!=0 || b.Speed!=0)continue;
                Vector3 edgeTakeoff=a.BoardingPoint(b.transform.position),landing=b.BoardingPoint(edgeTakeoff);
                for(int n=0;n<3;n++){edgeTakeoff=a.BoardingPoint(landing);landing=b.BoardingPoint(edgeTakeoff);}
                float gap=Vector3.ProjectOnPlane(landing-edgeTakeoff,Vector3.up).magnitude;
                if(gap<shortest){shortest=gap;jumpFrom=a;jumpTo=b;from=edgeTakeoff;target=landing;}
            }
            Require(jumpFrom!=null && shortest<5,"No separate pair of jumpable boards found.");
            Vector3 direction=Vector3.ProjectOnPlane(target-from,Vector3.up).normalized;
            player.RestoreSavedPose(from+Vector3.up*(.05f-player.LowestFootWorldOffset),Quaternion.LookRotation(direction));
            Physics.SyncTransforms(); await Hold(160);
            player.GetComponent<PlayerStamina>().currentStamina=player.GetComponent<PlayerStamina>().maxStamina;
            var jumpTrace=new System.Collections.Generic.List<string>();
            void Trace(){jumpTrace.Add("feet="+Feet(player).ToString()+", source="+jumpFrom.transform.InverseTransformPoint(Feet(player)).ToString()+", target="+jumpTo.transform.InverseTransformPoint(Feet(player)).ToString()+", grounded="+player.GetComponent<CharacterController>().isGrounded+", swimming="+player.Swimming+", combat="+attack.IsCombatMotionActive);}
            Trace();
            await Hold(90,GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Jump));
            // Release above the destination, allowing ordinary braking to
            // finish the crossing. Wave height changes the landing time.
            InputSystem.QueueStateEvent(keys,new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Forward)));
            for(int i=0;i<100;i++)
            {
                await Task.Delay(16);Trace();
                if(Vector3.Dot(Vector3.ProjectOnPlane(Feet(player)-target,Vector3.up),direction)>=-.1f)break;
            }
            await Hold(650);
            var landed=rider.Support();
            Require(landed==jumpTo&&!player.Swimming,"Jump did not cross the gap and land on the adjacent deck; support="+(landed!=null?landed.name:"none")+"; "+string.Join("; ",jumpTrace));
            Shot("jumped-between-boards");

            // Deep water must still use the real swimming controller and fast stroke.
            var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            Vector3 waterAt=first.transform.position+first.transform.forward*45f;
            waterAt.y=ocean.SampleSurfaceHeight(waterAt,Time.time)-2-player.LowestFootWorldOffset;
            player.RestoreSavedPose(waterAt,Quaternion.LookRotation(first.transform.forward)); Physics.SyncTransforms(); await Hold(350);
            Require(player.Swimming,"Leaving a board did not enter normal swimming.");
            Vector3 swimStart=player.transform.position;
            await Hold(450,GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Sprint));
            bool fast=player.FastSwimming;
            float swam=Vector3.ProjectOnPlane(player.transform.position-swimStart,Vector3.up).magnitude; await Hold(150);
            Require(fast&&swam>2,"Ordinary fast swimming did not work around the wreck: fast="+fast+", distance="+swam);

            waterAt=jumpTo.transform.position+jumpTo.transform.right*(jumpTo.Dimensions.x*.5f+.85f);
            waterAt.y=ocean.SampleSurfaceHeight(waterAt,Time.time)-2-player.LowestFootWorldOffset;
            player.RestoreSavedPose(waterAt,Quaternion.LookRotation(-jumpTo.transform.right)); Physics.SyncTransforms(); await Hold(350);
            Require(player.Swimming&&rider.NearbyBoard!=null,"Swimming beside the deck did not show a boarding prompt.");
            await Hold(90,GameInputSettings.Get(GameInputSettings.Action.Jump)); await Hold(100);
            Require(rider.IsClimbing && player.CharacterAnimator.GetCurrentAnimatorStateInfo(0).IsName("Wreck Climb"),"Jump key did not start the authored climb animation.");
            Shot("climbing-out-of-water");
            await Task.Delay(1200);
            Require(!rider.IsClimbing&&!player.Swimming&&!player.ExternalMovementLock&&rider.Support()==jumpTo&&player.CanUseGroundAttack,"Climb did not restore a grounded, controllable fighter.");
            await Tap(Key.F); Require(rider.IsSurfing,"A climbed board could not be surfed.");
            await Tap(Key.F); Require(!rider.IsSurfing&&!player.ExternalMovementLock,"F did not restore ordinary walking.");
            await Tap(Key.F); Require(rider.IsSurfing,"Could not remount for defeat verification.");
            battle.SharkHealth.ApplyDamage(battle.SharkHealth.maxHealth*.71f,battle.SharkHealth.transform.position);
            for(int i=0;i<80&&battle.CurrentPhase==Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(25);
            Require(battle.CurrentPhase==Story1WreckBattle.Phase.Finale&&health.currentHealth>0&&!PlayerDeathRespawn.IsOpen&&!player.ExternalMovementLock&&!rider.IsSurfing,"Health-triggered finale failed to release the mounted rider.");
            return new{surfDistance=distance,steeringDegrees=turn,jumpHeight=highest-takeoff,jumpedBetweenBoards=true,
                fastSwimmingDistance=swam,authoredClimbWorks=true,climbRestoresCombat=true,surfPreservesCombat=true,
                FRestoresWalking=true,forcedDefeatReleasesBoard=true};
        }
        finally
        {
            if(keys!=null)InputSystem.RemoveDevice(keys); keys=null;
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null&&actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }

    static void Shot(string name)
    {
        string dir=Path.GetFullPath(".codex/wreck-traversal-preview");Directory.CreateDirectory(dir);
        var camera=Camera.main;var target=new RenderTexture(960,540,24);var frame=new Texture2D(960,540,TextureFormat.RGB24,false);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,960,540),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(dir,name+".png"),frame.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
}
