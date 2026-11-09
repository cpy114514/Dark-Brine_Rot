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

/// <summary>Exercises both actual forms and damage windows in an isolated play session.</summary>
public static class VerifyZhuBaJieBoss
{
    public static string Status="idle",Result="";
    static Keyboard keyboard,previous;static Key[] keys=Array.Empty<Key>();
    static void Inject(){if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(keys));}
    static async Task Wait(float seconds)
    {
        float end=Time.time+seconds;var timeout=DateTime.UtcNow.AddSeconds(25);
        while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>timeout)throw new Exception("Play mode stopped or stalled.");await Task.Delay(20);}
    }
    static async Task Until(Func<bool> condition,float seconds,string label)
    {float end=Time.realtimeSinceStartup+seconds;while(!condition()){if(!EditorApplication.isPlaying || Time.realtimeSinceStartup>end)throw new Exception("Timed out: "+label);await Task.Delay(25);}}
    static void SetTimer(ZhuBaJieBoss b,float value)=>typeof(ZhuBaJieBoss).GetField("timer",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(b,value);
    static void Place(ThirdPersonPlayerController player,Vector3 at)
    {
        var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
        if(!ground.Raycast(new Ray(at+Vector3.up*50,Vector3.down),out var hit,100))throw new Exception("No ground at "+at);
        var body=player.GetComponent<CharacterController>();float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(hit.point+Vector3.up*(.1f-sole),Quaternion.identity);
    }
    static void ClearRecovery(PlayerHealth hp)
    {
        typeof(PlayerHealth).GetField("protectedUntil",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hp,0f);
        typeof(PlayerHealth).GetField("recoveryUntil",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hp,0f);
        hp.currentHealth=hp.maxHealth;
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,Path.GetFullPath("Temp/zhu-bajie-boss-verification.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.4f);
            var boss=UnityEngine.Object.FindFirstObjectByType<ZhuBaJieBoss>();var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(!boss || !player || boss.Health.maxHealth<1000 || !boss.boarForm.activeInHierarchy || boss.humanForm.activeSelf)throw new Exception("Encounter, health or initial form missing.");
            var hp=player.GetComponent<PlayerHealth>();var playerBody=player.GetComponent<CharacterController>();
            previous=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Inject;
            typeof(SecondIslandStartProtection).GetProperty("WaitingForMovement").SetValue(player.GetComponent<SecondIslandStartProtection>(),false);
            Place(player,boss.transform.position+new Vector3(0,0,-12));await Wait(.7f);
            await Until(()=>boss.FightActive,4,"engagement");
            // Leave the natural attack untouched and observe it enter the real state machine.
            await Until(()=>boss.AttacksStarted>0,5,"natural attack");
            Result+="encounter engaged; ";
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Chase,8,"first recovery");SetTimer(boss,20);
            var solid=boss.boarForm.transform.Find("BodySolid_Beast").GetComponent<Collider>();
            if(!solid.enabled || solid.isTrigger || !boss.boarForm.GetComponentsInChildren<Collider>().Any(c=>c.isTrigger))throw new Exception("Boar body or hittable collision missing.");
            // The player's controller must hit the extended body, not pass into the long mesh.
            boss.enabled=false;Vector3 axis=boss.transform.forward;
            Vector3 approach=boss.transform.position-axis*12;
            Place(player,approach);await Wait(.4f);Physics.SyncTransforms();
            Vector3 before=player.transform.position;var flags=playerBody.Move(axis*10);
            float travel=Vector3.ProjectOnPlane(player.transform.position-before,Vector3.up).magnitude;boss.enabled=true;
            if((flags&CollisionFlags.Sides)==0 || travel>8f)
                throw new Exception("Player controller crossed the boar's solid body: "+flags+", travel="+travel);
            Result+="body collision; ";
            // Use Sahur's actual attack from the front of the long boar body.
            boss.enabled=false;var face=Vector3.ProjectOnPlane(boss.transform.position-player.transform.position,Vector3.up);
            var facing=Quaternion.LookRotation(face);player.RestoreSavedPose(player.transform.position,facing);
            typeof(ThirdPersonPlayerController).GetField("yaw",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,facing.eulerAngles.y);
            await Wait(.6f);Cursor.lockState=CursorLockMode.Locked;
            await Until(()=>player.CanUseGroundAttack,5,"ground attack near boar");
            float bossHealth=boss.Health.currentHealth;var playerCombat=player.GetComponent<SahurAttack>();playerCombat.TriggerAttack();
            await Wait(1.5f);boss.enabled=true;
            if(boss.Health.currentHealth>=bossHealth)throw new Exception("Sahur's actual ground attack could not hit the boar from its solid front.");
            Result+="player melee damage; ";
            // Start outside contact, face the player, and let the authored charge move and strike.
            Place(player,boss.transform.position+new Vector3(0,0,-12));await Wait(.3f);
            boss.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(player.transform.position-boss.transform.position,Vector3.up));
            ClearRecovery(hp);int prior=boss.HitsLanded;
            if(!boss.TryBeginAttack(ZhuBaJieBoss.MoveKind.Charge))throw new Exception("Charge refused to start.");
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Recover,6,"charge recovery");
            if(boss.HitsLanded<=prior || hp.currentHealth>=hp.maxHealth)throw new Exception("Charge failed to hit an exposed player.");
            Result+="charge damage; ";
            // Retreat beyond the old nine-unit cutoff. The AI must pursue and
            // pick a sweep on its own, then turn its tail toward the target.
            int chargeCount=boss.AttacksStarted;
            Vector3 chargeEnd=boss.transform.position;
            Place(player,chargeEnd+boss.transform.forward*12);
            ClearRecovery(hp);prior=boss.HitsLanded;
            await Until(()=>boss.AttacksStarted>chargeCount,7,"automatic follow-up after charge");
            if(boss.CurrentMove!=ZhuBaJieBoss.MoveKind.TailSweep || Vector3.ProjectOnPlane(boss.transform.position-chargeEnd,Vector3.up).magnitude<2f)
                throw new Exception("The boss did not close the gap and choose a tail sweep after its charge.");
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Attack,3,"tail attack");
            Vector3 toPlayer=Vector3.ProjectOnPlane(player.transform.position-boss.transform.position,Vector3.up);
            if(Vector3.Angle(-boss.transform.forward,toPlayer)>90f)throw new Exception("The boss aimed its head at the player during the sweep.");
            await Until(()=>boss.HitsLanded>prior,2,"automatically aimed tail damage");
            if(hp.currentHealth>=hp.maxHealth)throw new Exception("Tail sweep failed to damage the player.");
            Result+="automatic tail sweep after retreat, turn and damage; ";
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Chase,5,"tail recovery");SetTimer(boss,20);
            float oldHealth=boss.Health.currentHealth;boss.Health.ApplyDamage(oldHealth-boss.Health.maxHealth*.49f,boss.transform.position);
            if(boss.CurrentState!=ZhuBaJieBoss.State.Transform || boss.HumanPhase)throw new Exception("Half health did not start the transformation.");
            await Until(()=>boss.HumanPhase,4,"human form");
            if(boss.boarForm.activeSelf || !boss.humanForm.activeInHierarchy || boss.Health.currentHealth>boss.Health.maxHealth*.5f)throw new Exception("Form or health was reset during transformation.");
            var bones=boss.humanForm.GetComponentsInChildren<Transform>();var hand=bones.First(t=>t.name=="hand_r");var grip=bones.First(t=>t.name=="weapon");
            if(Vector3.Distance(hand.position,grip.position)>.02f)throw new Exception("Rake grip is not attached to the hand.");
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Chase,5,"transformation recovery");SetTimer(boss,20);
            Place(player,boss.transform.position+new Vector3(0,0,-7));await Wait(.35f);
            boss.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(player.transform.position-boss.transform.position,Vector3.up));
            ClearRecovery(hp);prior=boss.HitsLanded;var arm=bones.First(t=>t.name=="upperarm_r");Quaternion rest=arm.localRotation;
            if(!boss.TryBeginAttack(ZhuBaJieBoss.MoveKind.RakeCombo))throw new Exception("Rake combo refused to start.");
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Attack,3,"rake attack");await Wait(.4f);
            if(Quaternion.Angle(rest,arm.localRotation)<3 || Vector3.Distance(hand.position,grip.position)>.02f)throw new Exception("The arm or held rake did not animate together.");
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Recover,3,"rake recovery");
            if(boss.HitsLanded<prior+2 || hp.currentHealth>hp.maxHealth-30)throw new Exception("The two rake strikes did not both hit an exposed player.");
            Result+="half-health transformation and held-rake damage; ";
            await Until(()=>boss.CurrentState==ZhuBaJieBoss.State.Chase,4,"rake finish");SetTimer(boss,20f);
            Place(player,boss.transform.position+boss.transform.forward*8);await Wait(.4f);
            var aim=Quaternion.LookRotation(Vector3.ProjectOnPlane(boss.transform.position-player.transform.position,Vector3.up));
            player.RestoreSavedPose(player.transform.position,aim);
            typeof(ThirdPersonPlayerController).GetField("yaw",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,aim.eulerAngles.y);
            Cursor.lockState=CursorLockMode.Locked;var attack=player.GetComponent<SahurAttack>();
            await Until(()=>player.CanUseGroundAttack,5,"player ready to attack");
            SetTimer(boss,.2f);int dodges=boss.DodgesPerformed;attack.TriggerAttack();
            await Until(()=>attack.IsGroundComboActive || attack.IsHeavyAttackActive,2,"player attack start");
            await Until(()=>boss.DodgesPerformed>dodges,4,"reactive dodge");
            if(boss.CurrentState!=ZhuBaJieBoss.State.Dodge)throw new Exception("Dodge did not enter its movement state.");
            Result+="reactive dodge; ";
            boss.Health.Kill();await Wait(.2f);
            if(boss.CurrentState!=ZhuBaJieBoss.State.Dead || !player.GetComponent<SecondIslandSaveState>().Capture().zhuBaJieDefeated)throw new Exception("Boss death was not captured in save progress.");
            if(!GameSaveManager.SaveCurrentGame() || !GameSaveManager.QueueContinue())throw new Exception("Could not write isolated boss save.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            await Until(()=>!GameSaveManager.IsContinuePending,15,"continue boss progress");await Wait(.5f);
            boss=UnityEngine.Object.FindFirstObjectByType<ZhuBaJieBoss>();
            if(!boss || !boss.Health.IsDead || boss.CurrentState!=ZhuBaJieBoss.State.Dead)throw new Exception("Continue resurrected the defeated boss.");
            Status="passed";Result+="defeat and Continue passed";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        finally{keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(previous!=null&&previous.added)previous.MakeCurrent();keyboard=previous=null;}
        Debug.Log("Zhu Bajie boss review: "+Status+" "+Result);
    }
}
