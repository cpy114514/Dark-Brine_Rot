using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;

/// <summary>Checks an already-placed visual becomes one responsive encounter.</summary>
public static class VerifyZhuBaJiePlacedBoss
{
    public static string Status="idle",Result="";
    static async Task Wait(float seconds)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(Time.realtimeSinceStartup<end){if(!EditorApplication.isPlaying)throw new Exception("Play mode stopped.");await Task.Delay(20);}
    }
    static async Task Until(Func<bool> done,float timeout,string label)
    {
        float end=Time.realtimeSinceStartup+timeout;
        while(!done()){if(!EditorApplication.isPlaying || Time.realtimeSinceStartup>end)throw new Exception("Timed out: "+label);await Task.Delay(20);}
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,Path.GetFullPath("Temp/zhu-bajie-placed-verification.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.5f);
            var bosses=UnityEngine.Object.FindObjectsByType<ZhuBaJieBoss>(FindObjectsSortMode.None);
            var old=UnityEngine.Object.FindObjectsByType<ZhuBaJieVisualOnly>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .FirstOrDefault(x=>x.gameObject.scene.name=="Second Island" && !x.GetComponentInParent<ZhuBaJieBoss>(true));
            if(bosses.Length!=1 || !old || old.gameObject.activeInHierarchy)throw new Exception("The placed visual was not replaced by exactly one boss.");
            var boss=bosses[0];var difference=Vector3.ProjectOnPlane(boss.transform.position-old.transform.position,Vector3.up);
            if(difference.magnitude>.2f)throw new Exception("Boss did not keep the placed model position: "+difference);
            float facing=Quaternion.Angle(boss.boarForm.transform.rotation,old.transform.rotation);
            if(facing>1f)throw new Exception("Boss did not keep the placed model's facing: "+facing);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();var start=player.GetComponent<SecondIslandStartProtection>();
            if(!start || !start.WaitingForMovement)throw new Exception("Expected unmoved-player start protection.");
            var motor=player.GetComponent<CharacterController>();var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            Vector3 axis=boss.transform.forward;Vector3 point=boss.transform.position-axis*12;
            if(!ground.Raycast(new Ray(point+Vector3.up*50,Vector3.down),out var hit,100))throw new Exception("No ground near placed boss.");
            float sole=(motor.center.y-motor.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
            var rotation=Quaternion.LookRotation(axis);player.RestoreSavedPose(hit.point+Vector3.up*(.1f-sole),rotation);
            typeof(ThirdPersonPlayerController).GetField("yaw",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,rotation.eulerAngles.y);
            Cursor.lockState=CursorLockMode.Locked;await Wait(.4f);
            motor.Move(axis*10);await Wait(.6f);
            if(!start.WaitingForMovement || boss.FightActive)throw new Exception("The boss initiated combat before the unmoved player attacked.");
            await Until(()=>player.CanUseGroundAttack,5,"player ground attack");
            float health=boss.Health.currentHealth;player.GetComponent<SahurAttack>().TriggerAttack();
            await Until(()=>boss.Health.currentHealth<health,4,"actual player hit");
            await Until(()=>boss.FightActive && boss.AttacksStarted>0,5,"boss response to being hit");
            if(!start.WaitingForMovement)throw new Exception("The test accidentally cleared start protection by movement.");
            Status="passed";Result="One combat boss replaces the placed visual at the same position and facing; an unmoved player's actual attack damages it and starts its response.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        Debug.Log("Placed Zhu Bajie test: "+Status+" "+Result);
    }
}
