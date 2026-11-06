using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;

/// <summary>Checks repaired collision volumes and confirms proximity pickups still work.</summary>
public static class VerifySecondIslandCollisionRepair
{
    public static string Status="idle",Result="";
    static async Task Wait(float seconds)
    {
        float end=Time.time+seconds;var deadline=DateTime.UtcNow.AddSeconds(20);
        while(Time.time<end){if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");await Task.Delay(20);}
    }
    static void Place(ThirdPersonPlayerController player,Vector3 feet)
    {
        var body=player.GetComponent<CharacterController>();float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        player.RestoreSavedPose(feet+Vector3.up*(.1f-sole),Quaternion.identity);
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/second-island-collision-repair.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.6f);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var quest=UnityEngine.Object.FindFirstObjectByType<CandyHuntQuest>();
            var house=UnityEngine.Object.FindFirstObjectByType<CandyHouseMarker>();
            var reward=UnityEngine.Object.FindFirstObjectByType<GravityWristbandReward>();
            var hop=UnityEngine.Object.FindFirstObjectByType<MarshmallowHopRecovery>();
            if(!player || !quest || !house || !reward || !hop)throw new Exception("Island systems missing.");
            var blackboard=GameObject.Find("Marshmallow Challenge Blackboard");
            foreach(string n in new[]{"Wooden frame","Left post","Right post"})
            {
                var t=blackboard.transform.Find(n);var box=t?t.GetComponent<BoxCollider>():null;
                if(!box || !box.enabled || box.isTrigger)throw new Exception("Blackboard collider missing: "+n);
            }
            var finish=hop.transform.Find("Finish sign");
            if(!finish || !finish.GetComponent<BoxCollider>() || !finish.Find("Sign board").GetComponent<BoxCollider>())throw new Exception("Course finish sign collision missing.");
            foreach(var fairy in UnityEngine.Object.FindObjectsByType<NealFairyHover>(FindObjectsSortMode.None))
            {
                if(fairy.name.Contains("portrait"))continue;
                var col=fairy.GetComponentInChildren<CapsuleCollider>();
                if(!col || !col.enabled || !col.isTrigger)throw new Exception("Fairy's non-blocking collider missing: "+fairy.name);
            }
            foreach(var candy in quest.candies)
            {
                var box=candy.GetComponent<BoxCollider>();if(!box || !box.enabled || !box.isTrigger)throw new Exception("Quest candy trigger missing: "+candy.name);
            }
            var packs=house.GetComponentsInChildren<CandyHouseHealingPickup>(true);
            if(packs.Length!=3)throw new Exception("Expected three healing packs.");
            foreach(var pack in packs)
            {
                var box=pack.GetComponent<BoxCollider>();if(!box || !box.enabled || !box.isTrigger)throw new Exception("Healing pack trigger missing.");
            }
            var display=reward.transform.Find("Unlocked gravity wristband");
            if(!display || !display.GetComponent<SphereCollider>() || !display.GetComponent<SphereCollider>().isTrigger)throw new Exception("Wristband trigger missing.");
            var board=GameObject.Find("Skyboard pickup");var scrolls=GameObject.Find("Ten speed scrolls");
            if(!board || !board.GetComponent<BoxCollider>() || !board.GetComponent<BoxCollider>().isTrigger || !scrolls || !scrolls.GetComponent<BoxCollider>() || !scrolls.GetComponent<BoxCollider>().isTrigger)
                throw new Exception("Skyboard or scroll trigger missing.");
            var stars=hop.transform.Find("Challenge star markers");if(!stars || stars.childCount!=5)throw new Exception("Five star markers not present.");
            foreach(Transform star in stars){var c=star.GetComponent<SphereCollider>();if(!c || !c.isTrigger)throw new Exception("Course star trigger missing.");}
            quest.RestoreProgress(true,0);
            // Close the initial clue, then verify that a real pickup opens it
            // with a local light rather than a scene-wide directional light.
            typeof(CandyHuntQuest).GetField("popupUntil",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(quest,-1f);
            await Wait(.2f);
            var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var candyPos=quest.candies[0].position;
            if(!ground.Raycast(new Ray(candyPos+Vector3.up*50,Vector3.down),out var candyGround,100))throw new Exception("No ground at test candy.");
            Place(player,candyGround.point);await Wait(.6f);
            if((quest.CollectedMask&1)==0 || quest.candies[0].gameObject.activeSelf)throw new Exception("Quest candy could not be picked up with its new trigger.");
            var studio=(GameObject)typeof(CandyHuntQuest).GetField("portraitRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(quest);
            var portraitLight=studio ? studio.GetComponentInChildren<Light>(true) : null;
            if(!quest.PopupVisible || !studio || !studio.activeInHierarchy || !portraitLight || portraitLight.type!=LightType.Point ||
                Vector3.Distance(portraitLight.transform.position,player.transform.position)<=portraitLight.range)
                throw new Exception("Candy clue is missing or its portrait light can affect the island.");
            HealingPackSupply.RestoreCampaign("collision-audit-"+Guid.NewGuid().ToString("N"),2);
            var packTarget=packs[0];Place(player,packTarget.transform.position+new Vector3(-.8f,0,0));await Wait(.8f);
            if(HealingPackSupply.Remaining!=3 || (house.CollectedHealingPacks&1)==0 || packTarget.gameObject.activeSelf)throw new Exception("Healing pack could not be picked up with its new trigger.");
            var progress=player.GetComponent<SecondIslandSaveState>().Capture();
            if((progress.candyHuntMask&1)==0 || (progress.cottageHealingPacks&1)==0)throw new Exception("Pickup state not captured.");
            Status="passed";Result="Collision and pickup checks passed; the candy clue uses an isolated, local portrait light.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        Debug.Log("Second island collision repair test: "+Status+" "+Result);
    }
}
