using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>Exercises the ridden board against the moving end platforms.</summary>
public static class VerifySkyboardMarshmallowCollision
{
    public static string Status="idle", Result="";
    static async Task Wait(float seconds)
    {
        float until=Time.time+seconds;
        var deadline=DateTime.UtcNow.AddSeconds(20);
        while(Time.time<until)
        {
            if(!EditorApplication.isPlaying || DateTime.UtcNow>deadline)throw new Exception("Play mode stopped or stalled.");
            await Task.Delay(20);
        }
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
            if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(.5f);
            var reward=UnityEngine.Object.FindFirstObjectByType<SkyboardReward>();
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(!reward || !player)throw new Exception("Board or player missing.");
            var body=player.GetComponent<CharacterController>();
            player.GetComponent<Mavis.PlayerHealth>().GrantProtection(120);
            if(!reward.CollectSpawnTestKit() || !reward.TryStartFlight())throw new Exception("Could not start the test flight.");
            await Wait(.3f);
            var row=reward.transform.Find("Row 22 - 2");
            if(!row)throw new Exception("Finish row not found.");
            var platform=row.GetChild(0).GetComponentInChildren<BoxCollider>();
            var marshmallow=row.GetChild(0).GetComponent<CapsuleCollider>();
            var board=GameObject.Find("Sahur skyboard");
            var fields=BindingFlags.NonPublic|BindingFlags.Instance;
            var probe=(BoxCollider)typeof(SkyboardReward).GetField("boardProbe",fields).GetValue(reward);
            var probeCenter=(Vector3)typeof(SkyboardReward).GetField("boardProbeCenter",fields).GetValue(reward);
            if(!platform || !marshmallow || !board || !probe)throw new Exception("A platform or board collision shape is missing.");
            Bounds BoardBounds()
            {
                var renderers=board.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds;
            }
            float Penetration(Collider c)
            {
                return Physics.ComputePenetration(probe,board.transform.TransformPoint(probeCenter),board.transform.rotation,
                    c,c.transform.position,c.transform.rotation,out _,out float distance)?distance:0;
            }
            float offset=-(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
            Vector3 at=platform.bounds.center;at.y=platform.bounds.max.y+offset+.04f;
            player.RestoreSavedPose(at,Quaternion.identity);
            await Wait(.4f);
            var bb=BoardBounds();var top=platform.bounds;
            bool above=bb.max.x>=top.min.x && bb.min.x<=top.max.x && bb.max.z>=top.min.z && bb.min.z<=top.max.z;
            if(above && bb.min.y<top.max.y-.05f)throw new Exception($"Board intersects the platform top by {top.max.y-bb.min.y:F3}.");
            if(Penetration(platform)>.05f || Penetration(marshmallow)>.05f)throw new Exception("Board remains inside the marshmallow after landing.");
            float maxPenetration=0;
            for(int i=0;i<30;i++)
            {
                await Wait(.1f);
                maxPenetration=Mathf.Max(maxPenetration,Penetration(platform),Penetration(marshmallow));
            }
            if(maxPenetration>.08f)throw new Exception($"Moving marshmallow penetrated the board by {maxPenetration:F3}.");
            if(!reward.Flying)throw new Exception("Collision incorrectly ended flight.");
            // Approach a side below its top: the board should be pushed clear,
            // without lifting the rider onto the platform from underneath.
            top=platform.bounds;
            at=new Vector3(top.min.x-.55f,top.max.y-1.25f+offset,top.center.z);
            player.RestoreSavedPose(at,Quaternion.identity);
            await Wait(.5f);
            if(Penetration(platform)>.08f || Penetration(marshmallow)>.08f)throw new Exception("Board remains inside the side of the marshmallow.");
            if(body.bounds.min.y>platform.bounds.max.y+.4f)throw new Exception("Side contact lifted the rider onto the platform from underneath.");
            Result=$"Top landing, moving platform and side contact: maximum board overlap {maxPenetration:F3}; flight remains active.";
            Status="passed";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        Debug.Log("Skyboard marshmallow collision test: "+Status+" "+Result);
    }
}
