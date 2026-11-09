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
    static Keyboard keyboard;static Mouse mouse;static bool pressB,pressJump,pressAttack,pressForward,pressSprint;
    static readonly System.Reflection.FieldInfo Yaw=typeof(ThirdPersonPlayerController).GetField("yaw",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
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
            pressJump ? new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Jump)) :
            pressForward ? (pressSprint
                ? new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Forward),GameInputSettings.Get(GameInputSettings.Action.Sprint))
                : new KeyboardState(GameInputSettings.Get(GameInputSettings.Action.Forward))) : new KeyboardState();
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
    static Vector3 Flat(Vector3 value)=>Vector3.ProjectOnPlane(value,Vector3.up);
    static async Task WalkNear(ThirdPersonPlayerController player, Vector3 target, float radius)
    {
        pressForward=true;
        try
        {
            await WaitUntil(()=>
            {
                var toward=Flat(target-player.transform.position);
                if(toward.magnitude<=radius)return true;
                Yaw.SetValue(player,Quaternion.LookRotation(toward).eulerAngles.y);
                return false;
            },15,"walk to "+target);
        }
        finally{pressForward=false;}
        await Wait(.15f);
    }
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
            Check(!reward.MainCampsComplete && reward.Visible == !reward.Collected && inventory.Count(reward.item)<=1,"One wristband is available before any camps are cleared.");
            Check(Flat(reward.transform.position-new Vector3(-147.47f,0,1610.31f)).magnitude<7,"The wristband is beside the spawn point.");
            Check(reward.MainCampCount==3 && reward.NextMainCamp==1,"Three trail camps begin at Mint Gate.");
            var counter=reward.GetComponentsInChildren<Text>(true).First(x=>x.name=="Count");
            Check(counter.gameObject.activeInHierarchy && counter.text.Contains("Mint Gate") && counter.text.Contains("10") && reward.ActiveEnemyMarkers==0,"Initial camp counter.");
            var arrival=UnityEngine.Object.FindFirstObjectByType<SecondIslandArrivalTrail>();
            Check(arrival && !arrival.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Practice Marshmallow") || t.name=="PRACTICE MARSHMALLOWS"),
                "The spawn practice pads and sign are absent.");
            // Disable only AI motion during this deterministic encounter test.
            foreach(var enemy in enemies)enemy.GetComponent<ChickenBroAI>().enabled=false;
            keyboard=Keyboard.current;mouse=Mouse.current;Check(keyboard!=null && mouse!=null,"Input devices available");InputSystem.onBeforeUpdate+=Inject;
            if(!reward.Collected)await WalkNear(player,reward.transform.position,1.7f);
            await WaitUntil(()=>inventory.Count(reward.item)==1,5,"collect the spawn wristband");
            var ability=player.GetComponent<GravityWristbandAbility>();
            var objectives=player.GetComponent<SecondIslandObjectives>();
            await WaitUntil(()=>objectives.Current==SecondIslandObjectives.Objective.Marshmallows,3,"pickup advances to Mint Gate");
            Check(ability && ability.Owned && !ability.NormalGravity && traversal.LowGravityActive && reward.Defeated==0,"Pickup works before combat and preserves low gravity.");
            var stamina=player.GetComponent<PlayerStamina>();
            stamina.currentStamina=stamina.maxStamina;
            await WalkNear(player,reward.CampMapAnchor(1).position,12f);
            Check(Mathf.Abs(stamina.currentStamina-stamina.maxStamina)<.01f,"Walking in low gravity does not cost SP.");
            var first=reward.enemyRoot.GetChild(1).GetComponentsInChildren<MarshmallowEnemyHealth>(true);
            foreach(var enemy in first.Take(4))enemy.Kill();
            await Wait(.15f);Check(reward.CampEnemiesRemaining(1)==6 && reward.ActiveEnemyMarkers==0 && counter.text.Contains("6"),"Six remaining in Mint Gate: count updates without markers.");
            first[4].Kill();await Wait(.15f);
            Check(reward.CampEnemiesRemaining(1)==5 && reward.ActiveEnemyMarkers==5 && counter.text.Contains("5"),"Five remaining in Mint Gate: all five get markers.");
            var markers=reward.GetComponentsInChildren<Text>(true).Where(x=>x.name.StartsWith("Remaining marshmallow marker") && x.gameObject.activeInHierarchy).ToArray();
            Check(markers.Length==5 && markers.All(x=>x.color.r>.9f && x.color.g>.7f && x.color.b<.2f),"Last five markers are yellow.");
            for(int i=0;i<markers.Length;i++)for(int j=i+1;j<markers.Length;j++)
            {
                var a=((RectTransform)markers[i].transform).anchorMin;var b=((RectTransform)markers[j].transform).anchorMin;
                Check(Mathf.Abs(a.x-b.x)>=.075f || Mathf.Abs(a.y-b.y)>=.075f,"Remaining enemy markers must not overlap.");
            }
            ScreenCapture.CaptureScreenshot(Folder+"/last-five.png");await Wait(.15f);
            foreach(var enemy in first.Skip(5))enemy.Kill();
            ability.SetNormalGravity(true);
            stamina.currentStamina=stamina.maxStamina;
            await WalkNear(player,reward.CampMapAnchor(0).position,12f);
            Check(Mathf.Abs(stamina.currentStamina-stamina.maxStamina)<.01f,"Walking in normal gravity does not cost SP.");
            var toward=Flat(reward.CampMapAnchor(0).position-player.transform.position);
            Yaw.SetValue(player,Quaternion.LookRotation(toward).eulerAngles.y);
            pressSprint=pressForward=true;
            await Wait(.55f);
            pressSprint=pressForward=false;
            Check(stamina.currentStamina<stamina.maxStamina-.1f,"Sprinting still costs SP.");
            ability.SetNormalGravity(false);
            Check(reward.MainCampsCleared==1 && reward.NextMainCamp==0 && !reward.Visible,"Mint Gate points the objective to Swirl Bend.");
            foreach(var enemy in reward.enemyRoot.GetChild(0).GetComponentsInChildren<MarshmallowEnemyHealth>(true))enemy.Kill();
            await WalkNear(player,reward.CampMapAnchor(4).position,12f);
            var last=reward.enemyRoot.GetChild(4).GetComponentsInChildren<MarshmallowEnemyHealth>(true);
            foreach(var enemy in last.Take(last.Length-1))enemy.Kill();
            await Wait(.15f);
            Check(reward.Defeated==29 && reward.MainCampsCleared==2 && !reward.MainCampsComplete && !reward.Visible,"The last enemy keeps the camp objective open.");
            Check(reward.ActiveEnemyMarkers==1 && counter.text.Contains("1"),"Final trail enemy stays marked.");
            last.Last().Kill();await Wait(.2f);
            Check(reward.Defeated==30 && reward.Remaining==40 && reward.MainCampsComplete && !reward.Visible && reward.ActiveEnemyMarkers==0,"All three camps are complete and the pickup stays collected.");
            await WaitUntil(()=>player.CharacterAnimator && player.CharacterAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm).Find("Gravity wristband - worn"),5,"worn model");
            var arm=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var hand=player.CharacterAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            var wrist=arm.Find("Gravity wristband - worn");
            Check(Vector3.Dot(wrist.forward,(hand.position-arm.position).normalized)>.995f,"Bracelet opening aligns with forearm.");
            Check(Vector3.Distance(wrist.position,Vector3.Lerp(hand.position,arm.position,.4f))<.02f,"Bracelet sits above the wrist joint.");
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
            var saved=player.GetComponent<SecondIslandSaveState>().Capture();
            Check(saved.defeatedEnemies.Count==30,"Saves retain the existing individual enemy keys.");
            File.WriteAllText(Folder+"/status.txt","reloading saved route");
            Check(GameSaveManager.SaveCurrentGame() && GameSaveManager.QueueContinue(),"Could not queue the isolated wristband save.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/Scenes/Second Island/Second Island.unity");
            await WaitUntil(()=>!GameSaveManager.IsContinuePending,20,"wristband Continue");
            await Wait(.45f);
            reward=UnityEngine.Object.FindFirstObjectByType<GravityWristbandReward>();
            player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            Check(reward && reward.MainCampsComplete && reward.Collected && !reward.Visible && reward.MainCampsCleared==3 && reward.Remaining==40,
                "Continue preserves the claimed wristband and four optional camps.");
            Check(player && player.GetComponent<SecondIslandObjectives>().Current==SecondIslandObjectives.Objective.ZhuBaJie,
                "Continue points to the boss after the wristband is collected.");
            File.WriteAllText(Folder+"/result.txt",$"enemies={reward.Total}; spawn pickup before combat; no walking SP cost in either gravity; sprint SP cost verified; 3 main camps; optionalEnemies={reward.Remaining}; normalGravity={normal}; lowGravity={low}; pickup=1; normalJumpAndAttack=true; savedEnemyKeys={saved.defeatedEnemies.Count}; continue=passed");
            File.WriteAllText(Folder+"/status.txt","completed");
        }
        catch(Exception e){File.WriteAllText(Folder+"/status.txt","failed: "+e);}
        finally{pressB=pressJump=pressAttack=pressForward=pressSprint=false;InputSystem.onBeforeUpdate-=Inject;}
    }
}
