using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerifyGravityWristband
{
    const string Key="GravityWristband.Review", Folder="Temp/GravityWristbandReview";
    static Keyboard keyboard;static Mouse mouse;static bool pressB,pressJump,pressAttack;
    static VerifyGravityWristband()=>EditorApplication.playModeStateChanged+=OnMode;
    [MenuItem("Tools/Second Island Review/Test Gravity Wristband")]
    public static void Start()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play before running the wristband review.");
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/status.txt","starting");
        SessionState.SetBool(Key,true);VerifySecondIslandAwakening.Arm();EditorApplication.isPlaying=true;
    }
    static void OnMode(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);_=Run();}}
    static void Inject()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;
        var keys=pressB ? new KeyboardState(UnityEngine.InputSystem.Key.B) :
            pressJump ? new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Jump)) : new KeyboardState();
        InputState.Change(keyboard,keys);
        if(mouse!=null){mouse.MakeCurrent();var state=new MouseState();if(pressAttack)state=state.WithButton(MouseButton.Left);InputState.Change(mouse,state);}
    }
    static async Task WaitUntil(Func<bool> predicate,float seconds,string what)
    {
        var until=DateTime.UtcNow.AddSeconds(seconds);
        while(!predicate())
        {
            if(!EditorApplication.isPlaying || DateTime.UtcNow>until)throw new Exception("Timed out: "+what);
            await Task.Delay(30);
        }
    }
    static async Task Wait(float seconds)
    {
        float until=Time.time+seconds;await WaitUntil(()=>Time.time>=until,12,"frames");
    }
    static void Check(bool value,string label){if(!value)throw new Exception(label);}
    static async Task Run()
    {
        try
        {
            File.WriteAllText(Folder+"/status.txt","opening");
            await WaitUntil(()=>VerifySecondIslandAwakening.Status=="completed"||VerifySecondIslandAwakening.Status.StartsWith("failed"),45,"opening");
            Check(VerifySecondIslandAwakening.Status=="completed",VerifySecondIslandAwakening.Status);
            var reward=UnityEngine.Object.FindFirstObjectByType<GravityWristbandReward>();
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var traversal=player.GetComponent<MarshmallowTraversal>();var inventory=player.GetComponent<EquipmentInventory>();
            var enemies=reward.enemyRoot.GetComponentsInChildren<MarshmallowEnemyHealth>(true);
            await Wait(.2f);Check(reward.Total==70 && enemies.Length==70,"Seventy scene enemies are tracked.");
            Check(!reward.Unlocked && !reward.Visible && inventory.Count(reward.item)==0,"Reward remains hidden before victory.");
            var counter=reward.GetComponentsInChildren<Text>(true).First(x=>x.name=="Count");
            Check(counter.gameObject.activeInHierarchy && counter.text.Contains("70") && reward.ActiveEnemyMarkers==0,"Initial enemy counter.");
            // Disable only AI motion during this deterministic encounter test.
            foreach(var enemy in enemies)enemy.GetComponent<ChickenBroAI>().enabled=false;
            foreach(var enemy in enemies.Take(64))enemy.Kill();
            await Wait(.15f);Check(reward.Remaining==6 && reward.ActiveEnemyMarkers==0 && counter.text.Contains("6"),"Six remaining: count updates without markers.");
            enemies[64].Kill();await Wait(.15f);
            Check(reward.Remaining==5 && reward.ActiveEnemyMarkers==5 && counter.text.Contains("5"),"Five remaining: all five get markers.");
            var markers=reward.GetComponentsInChildren<Text>(true).Where(x=>x.name.StartsWith("Remaining marshmallow marker") && x.gameObject.activeInHierarchy).ToArray();
            Check(markers.Length==5 && markers.All(x=>x.color.r>.9f && x.color.g>.7f && x.color.b<.2f),"Last five markers are yellow.");
            for(int i=0;i<markers.Length;i++)for(int j=i+1;j<markers.Length;j++)
            {
                var a=((RectTransform)markers[i].transform).anchorMin;var b=((RectTransform)markers[j].transform).anchorMin;
                Check(Mathf.Abs(a.x-b.x)>=.075f || Mathf.Abs(a.y-b.y)>=.075f,"Remaining enemy markers must not overlap.");
            }
            ScreenCapture.CaptureScreenshot(Folder+"/last-five.png");await Wait(.15f);
            enemies[65].Kill();await Wait(.15f);Check(reward.Remaining==4 && reward.ActiveEnemyMarkers==4 && counter.text.Contains("4"),"Defeated enemy marker disappears.");
            foreach(var enemy in enemies.Skip(66).Take(3))enemy.Kill();
            await Wait(.1f);Check(reward.Defeated==69 && !reward.Unlocked && !reward.Visible,"The last surviving enemy keeps the reward locked.");
            Check(reward.Remaining==1 && reward.ActiveEnemyMarkers==1 && counter.text.Contains("1"),"Final enemy stays marked.");
            enemies.Last().Kill();await Wait(.2f);
            Check(reward.Defeated==70 && reward.Unlocked && reward.Visible && reward.ActiveEnemyMarkers==0,"Final defeat reveals reward and clears markers.");
            ScreenCapture.CaptureScreenshot(Folder+"/revealed.png");
            var body=player.GetComponent<CharacterController>();
            float feetOffset=body.bounds.min.y-player.transform.position.y;
            player.RestoreSavedPose(reward.transform.position+Vector3.up*(.1f-feetOffset),player.transform.rotation);
            Physics.SyncTransforms();
            await WaitUntil(()=>inventory.Count(reward.item)==1,5,"collect wristband");
            var ability=player.GetComponent<GravityWristbandAbility>();
            Check(ability && ability.Owned && !ability.NormalGravity && traversal.LowGravityActive,"Pickup preserves low gravity until activated.");
            await WaitUntil(()=>player.CharacterAnimator && player.CharacterAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).Find("Gravity wristband - worn"),5,"worn model");
            var arm=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var hand=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            var wrist=arm.Find("Gravity wristband - worn");
            Check(Vector3.Dot(wrist.forward,(hand.position-arm.position).normalized)>.995f,"Bracelet opening aligns with forearm.");
            Check(Vector3.Distance(wrist.position,Vector3.Lerp(hand.position,arm.position,.4f))<.02f,"Bracelet sits above the wrist joint.");
            keyboard=Keyboard.current;mouse=Mouse.current;Check(keyboard!=null && mouse!=null,"Input devices available");InputSystem.onBeforeUpdate+=Inject;
            await Wait(.15f);pressB=true;await Wait(.1f);pressB=false;await Wait(.2f);
            Check(ability.NormalGravity && !traversal.LowGravityActive,"B restores normal gravity.");
            var gravityProperty=typeof(ThirdPersonPlayerController).GetProperty("TraversalGravity",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            float normal=(float)gravityProperty.GetValue(player);
            Check(Mathf.Abs(normal-player.gravity)<.001f,"Normal gravity uses player settings.");
            ScreenCapture.CaptureScreenshot(Folder+"/normal-gravity.png");
            await WaitUntil(()=>player.CanUseGroundAttack,6,"ready to jump");
            var attack=player.GetComponent<SahurAttack>();
            pressJump=true;await Wait(.09f);pressJump=false;await Wait(.12f);
            Check(!player.CanUseGroundAttack,"Jump works with normal gravity.");
            pressAttack=true;await Wait(.09f);pressAttack=false;
            await WaitUntil(()=>attack.IsJumpSlashActive,3,"aerial attack with normal gravity");
            await WaitUntil(()=>player.CanUseGroundAttack && !attack.IsCombatMotionActive,7,"landing after aerial attack");
            pressB=true;await Wait(.1f);pressB=false;await Wait(.2f);
            Check(!ability.NormalGravity && traversal.LowGravityActive,"B switches back to island gravity.");
            float low=(float)gravityProperty.GetValue(player);
            Check(Mathf.Abs(low-traversal.gravity)<.001f && low<normal,"Low gravity is restored.");
            Check(inventory.Count(reward.item)==1 && !reward.Visible,"The collected item is not duplicated.");
            File.WriteAllText(Folder+"/result.txt",$"enemies={reward.Total}; count=70,6,5,4,1,0; yellowMarkers=0,5,4,1,0; normalGravity={normal}; lowGravity={low}; owned={ability.Owned}; wristAligned=true; pickup=1; normalJumpAndAttack=true");
            File.WriteAllText(Folder+"/status.txt","completed");
        }
        catch(Exception e){File.WriteAllText(Folder+"/status.txt","failed: "+e);}
        finally{pressB=pressJump=pressAttack=false;InputSystem.onBeforeUpdate-=Inject;}
    }
}
