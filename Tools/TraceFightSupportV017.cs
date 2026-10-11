using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(2700)]
public sealed class V017SupportTrace : MonoBehaviour
{
    public string file;
    readonly List<object> rows=new List<object>();
    readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    float next;
    object Read(object instance,string name)=>instance.GetType().GetField(name,flags)?.GetValue(instance);
    void LateUpdate()
    {
        if(Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;
        var film=UnityEngine.Object.FindFirstObjectByType<Story1WreckFinale>();
        if(film==null)return;
        var plank=(Story1WreckPlank)Read(film,"board");if(plank==null)return;
        var body=plank.GetComponent<Rigidbody>();var floating=plank.GetComponent<Story1WreckFloatBody>();
        var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var pressure=(float[])Read(floating,"pressures");var weights=(float[])Read(floating,"pressureWeights");
        Vector3 centre=body.worldCenterOfMass;
        var neighbours=UnityEngine.Object.FindObjectsByType<Story1WreckFloatBody>(FindObjectsSortMode.None)
            .Where(b=>b!=floating && b.Body!=null && Vector3.ProjectOnPlane(b.Body.worldCenterOfMass-centre,Vector3.up).magnitude<6)
            .Select(b=>new{name=b.name,mass=b.Body.mass,position=b.Body.worldCenterOfMass.ToString(),velocity=b.Body.linearVelocity.ToString()}).ToArray();
        rows.Add(new {time=film.CinematicTime,board=plank.name,mass=body.mass,
            stanceCandidates=Read(film,"stanceCandidateCount"),stanceCost=Read(film,"chosenStancePoseCost"),
            stanceWoodPenetration=Read(film,"chosenStanceWoodPenetration"),footprintRadius=Read(film,"supportFootprintRadius"),
            centre=centre.ToString(),surface=water.SampleSurfaceHeight(centre,Time.time),velocity=body.linearVelocity.ToString(),
            tilt=Vector3.Angle(plank.transform.up,Vector3.up),floating.InWater,pressure=pressure.ToArray(),weights=weights.ToArray(),
            centreSurface=Read(floating,"centreSurface"),centreVelocity=Read(floating,"centreSurfaceVelocity"),
            stiffness=Read(floating,"stiffness"),draft=Read(floating,"draft"),neighbours});
        // Persist during the take: the verifier may change scenes after its assertions.
        System.IO.File.WriteAllText(file,Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
    }
}
public static class TraceFightSupportV017
{
    public static object Start(string revision)
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Support tracing requires native Play mode.");
        Stop();
        string directory=System.IO.Path.GetFullPath(".codex/encounter-"+revision);
        System.IO.Directory.CreateDirectory(directory);
        var go=new GameObject("Read-only fighting support trace");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<V017SupportTrace>().file=System.IO.Path.Combine(directory,"support-pressure-trace.json");
        return "Native read-only support trace armed.";
    }
    public static object Stop()
    {
        foreach(var trace in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b=>b.GetType().Name=="V017SupportTrace"))UnityEngine.Object.DestroyImmediate(trace.gameObject);
        return "Support trace stopped.";
    }
}
