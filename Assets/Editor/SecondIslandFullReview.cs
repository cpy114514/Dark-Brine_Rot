using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// An editor-only, end-to-end review of the actual second island scene.
[InitializeOnLoad]
public static class SecondIslandFullReview
{
    const string SessionKey="SecondIslandFullReview.Run";
    const string Folder="Temp/SecondIslandFullReview";
    public static string Status="idle";
    static SecondIslandFullReview()=>EditorApplication.playModeStateChanged+=OnMode;

    [MenuItem("Tools/Second Island Review/Run Full Playthrough")]
    public static void StartReview()
    {
        if(Application.isPlaying)throw new Exception("Stop play before starting the review.");
        Directory.CreateDirectory(Folder);
        SessionState.SetBool(SessionKey,true);
        VerifySecondIslandAwakening.Arm();
        Status="armed";EditorApplication.isPlaying=true;
    }
    static void OnMode(PlayModeStateChange mode)
    {
        if(mode!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey,false))return;
        SessionState.SetBool(SessionKey,false);_ = Run();
    }
    static void Progress(string status)
    {
        Status=status;File.WriteAllText(Folder+"/status.txt",status);
    }
    static async Task WaitFor(Func<bool> done,float timeoutSeconds)
    {
        var deadline=DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while(!done())
        {
            if(!Application.isPlaying)throw new Exception("Play mode ended during the review.");
            if(DateTime.UtcNow>deadline)throw new Exception("Review phase timed out: "+Status);
            await Task.Delay(40);
        }
    }
    static object Terrain(ThirdPersonPlayerController player,MarshmallowIslandExperience experience)
    {
        var collider=experience.surface.GetComponent<Collider>();
        if(!collider)throw new Exception("Island surface has no collision collider.");
        int hits=0,walkable=0,steep=0,missing=0;float min=999,max=0;var samples=new List<object>();
        for(int x=-5;x<=5;x++)for(int z=-5;z<=5;z++)
        {
            Vector3 at=player.transform.position+new Vector3(x*10,100,z*10);
            if(collider.Raycast(new Ray(at,Vector3.down),out var hit,350))
            {
                float angle=Vector3.Angle(hit.normal,Vector3.up);hits++;
                if(angle<=player.GetComponent<CharacterController>().slopeLimit)walkable++;else steep++;
                min=Mathf.Min(min,angle);max=Mathf.Max(max,angle);
                samples.Add(new{x,z,angle,height=hit.point.y});
            }
            else missing++;
        }
        return new{hits,walkable,steep,missing,minSlope=min,maxSlope=max,bounds=collider.bounds.size.ToString("F1"),samples};
    }
    static async Task<object> Weather()
    {
        var cycle=UnityEngine.Object.FindObjectsByType<DayNightCycle>(FindObjectsSortMode.None).FirstOrDefault(x=>x.gameObject.scene.name=="Second Island");
        if(!cycle)throw new Exception("The island day/night controller is missing.");
        float originalHour=cycle.startHour;bool originalClock=cycle.clockRuns;
        var periods=new List<object>();
        try
        {
            cycle.clockRuns=false;
            foreach(float hour in new[]{8f,12f,17.6f,21f})
            {
                cycle.startHour=hour;await Task.Delay(350);
                ScreenCapture.CaptureScreenshot(Folder+"/weather-"+hour.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture)+".png");
                var light=cycle.GetComponent<Light>();
                periods.Add(new{hour=cycle.CurrentHour,sun=light.intensity,color=light.color.ToString(),fog=RenderSettings.fog,
                    fogColor=RenderSettings.fogColor.ToString(),ambient=RenderSettings.ambientIntensity,
                    clouds=cycle.CurrentCloudCoverage});
            }
            float[] coverage={cycle.EvaluateCloudCoverage(0),cycle.EvaluateCloudCoverage(111),cycle.EvaluateCloudCoverage(222),cycle.EvaluateCloudCoverage(333)};
            var clouds=UnityEngine.Object.FindObjectsByType<MarshmallowCloudMotion>(FindObjectsSortMode.None);
            return new{originalHour,originalClock,cycle.marshmallowPalette,cloudGroups=clouds.Length,
                cloudRange=new[]{cycle.minimumCloudCoverage,cycle.maximumCloudCoverage},coverage,periods};
        }
        finally{cycle.startHour=originalHour;cycle.clockRuns=originalClock;}
    }
    static async Task<object> Map(ThirdPersonPlayerController player)
    {
        var map=player.GetComponent<Mavis.IslandMapUI>();
        if(!map)throw new Exception("Island map component is missing.");
        var timer=System.Diagnostics.Stopwatch.StartNew();
        if(!map.SetOpen(true))throw new Exception("The map did not open.");
        timer.Stop();
        try
        {
            await Task.Delay(300);
            Canvas.ForceUpdateCanvases();
            var mesh=map.MapGraphic.canvasRenderer.GetMesh();
            {
                int vertices=mesh.vertexCount,triangles=mesh.triangles.Length/3;
                ScreenCapture.CaptureScreenshot(Folder+"/map.png");
                await Task.Delay(150);
                if(vertices<100 || triangles<50)throw new Exception("Second island map has no usable terrain outline.");
                return new{vertices,triangles,map.MapGraphic.WorldSpan,openMilliseconds=timer.Elapsed.TotalMilliseconds};
            }
        }
        finally{map.SetOpen(false);}
    }
    static async Task Run()
    {
        var results=new Dictionary<string,object>();
        try
        {
            Progress("opening");
            await WaitFor(()=>VerifySecondIslandAwakening.Status=="completed" || VerifySecondIslandAwakening.Status.StartsWith("failed"),40);
            results["openingStatus"]=VerifySecondIslandAwakening.Status;results["opening"]=VerifySecondIslandAwakening.Summary;
            if(VerifySecondIslandAwakening.Status!="completed")throw new Exception(VerifySecondIslandAwakening.Status);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            var soft=player.GetComponent<MarshmallowTraversal>();var experience=player.GetComponent<MarshmallowIslandExperience>();
            if(!soft || !experience || !soft.Active || !experience.Ready)throw new Exception("Soft island traversal or feedback is inactive.");
            results["profile"]=new{soft.gravity,soft.jumpHeight,soft.walkingBoundHeight,soft.sprintingBoundHeight,soft.airControl,
                soft.groundCompressionTime,controllerSlopeLimit=player.GetComponent<CharacterController>().slopeLimit,
                fairy=UnityEngine.Object.FindObjectsByType<NealFairyHover>(FindObjectsSortMode.None).Length};
            results["terrain"]=Terrain(player,experience);
            Progress("candy-collisions");results["candyCollisions"]=CandyColliderTools.Verify(player);
            Progress("fast-air-dodge");VerifyFastAirDodge.Start();
            await WaitFor(()=>VerifyFastAirDodge.Status=="completed" || VerifyFastAirDodge.Status.StartsWith("failed"),40);
            results["fastAirDodgeStatus"]=VerifyFastAirDodge.Status;results["fastAirDodge"]=VerifyFastAirDodge.Summary;
            if(VerifyFastAirDodge.Status!="completed")throw new Exception(VerifyFastAirDodge.Status);
            Progress("map");results["map"]=await Map(player);
            Progress("weather");results["weather"]=await Weather();
            Progress("fairy-flight");VerifyNealFairyHover.Start();
            await WaitFor(()=>VerifyNealFairyHover.Status=="completed" || VerifyNealFairyHover.Status.StartsWith("failed"),30);
            results["fairyStatus"]=VerifyNealFairyHover.Status;results["fairy"]=VerifyNealFairyHover.Summary;
            if(VerifyNealFairyHover.Status!="completed")throw new Exception(VerifyNealFairyHover.Status);
            Progress("walking-jumping-dodging");VerifyMarshmallowSpacewalk.Start();
            await WaitFor(()=>VerifyMarshmallowSpacewalk.Status=="completed" || VerifyMarshmallowSpacewalk.Status.StartsWith("failed"),120);
            results["spacewalkStatus"]=VerifyMarshmallowSpacewalk.Status;results["spacewalk"]=VerifyMarshmallowSpacewalk.Summary;
            results["traversalStatus"]=PlaySecondIslandMarshmallow.Status;results["traversal"]=PlaySecondIslandMarshmallow.Summary;
            if(VerifyMarshmallowSpacewalk.Status!="completed")throw new Exception(VerifyMarshmallowSpacewalk.Status);
            // This phase checks the combo's input timing in isolation. Enemies
            // can otherwise interrupt it while the preceding traversal puts the
            // player inside an outpost; restore the actual encounter immediately.
            var outposts=GameObject.Find("Marshmallow Friends - Second Island");
            bool outpostsWereActive=outposts && outposts.activeSelf;
            try
            {
                if(outpostsWereActive)outposts.SetActive(false);
                Progress("aerial-combo");VerifyAerialCombo.Start();
                await WaitFor(()=>VerifyAerialCombo.Status=="completed" || VerifyAerialCombo.Status.StartsWith("failed"),90);
                results["aerialStatus"]=VerifyAerialCombo.Status;results["aerial"]=VerifyAerialCombo.Summary;
                if(VerifyAerialCombo.Status!="completed")throw new Exception(VerifyAerialCombo.Status);
            }
            finally{if(outpostsWereActive && outposts)outposts.SetActive(true);}
            Progress("performance");var frames=new List<float>();float end=Time.realtimeSinceStartup+2;
            while(Time.realtimeSinceStartup<end){frames.Add(Time.unscaledDeltaTime);await Task.Delay(16);}
            frames.Sort();results["frameTime"]=new{count=frames.Count,medianMs=frames.Count>0?frames[frames.Count/2]*1000:0,
                p95Ms=frames.Count>0?frames[Mathf.Min(frames.Count-1,Mathf.FloorToInt(frames.Count*.95f))]*1000:0};
            File.WriteAllText(Folder+"/review.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
            Progress("completed");
        }
        catch(Exception e)
        {
            results["error"]=e.ToString();File.WriteAllText(Folder+"/review.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
            Progress("failed: "+e);
        }
    }
}
