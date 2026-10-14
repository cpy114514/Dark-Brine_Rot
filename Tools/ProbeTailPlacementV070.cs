using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(1100)]
public sealed class ProbeTailPlacementV070:MonoBehaviour
{
    public OpeningBenchmark benchmark;public string output;
    readonly List<object> rows=new List<object>();int sampled;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object F(object owner,string name)=>owner.GetType().GetField(name,Private)?.GetValue(owner);
    void LateUpdate()
    {
        var film=benchmark!=null ? benchmark.Battle?.Finale : null;if(film==null)return;
        float t=film.CinematicTime;if(t>18.35f){File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));Destroy(this);return;}
        if(!(t>=13.6f && t<=14.78f || t>=17.5f && t<=18.3f) || ++sampled%4!=0)return;
        var contact=benchmark.Battle.WreckContact;var shark=(TralaleroSwimAnimator)F(film,"shark");
        bool powerful=t>16;Vector3 marker=shark.TailWorldPoint,aim=(Vector3)F(film,"attackAim"),local=shark.transform.InverseTransformPoint(marker),expected=(Vector3)F(film,powerful?"filmShipTailLocal":"filmTailLocal");
        float stalePosition=0,staleAngle=0;
        foreach(var entry in (IEnumerable)F(contact,"planningParts"))
        {
            var type=entry.GetType();var collider=(MeshCollider)type.GetField("collider").GetValue(entry);if(collider==null)continue;
            stalePosition=Mathf.Max(stalePosition,Vector3.Distance((Vector3)type.GetField("position").GetValue(entry),collider.transform.position));
            staleAngle=Mathf.Max(staleAngle,Quaternion.Angle((Quaternion)type.GetField("rotation").GetValue(entry),collider.transform.rotation));
        }
        var anatomy=contact.CaptureAnatomy();float cached=contact.PoseObstruction(anatomy,marker,shark.transform.rotation,true);
        typeof(Story1SharkWreckContact).GetField("planningFrame",Private).SetValue(contact,-1);
        float fresh=contact.PoseObstruction(anatomy,marker,shark.transform.rotation,true);
        rows.Add(new{t,action=shark.CinematicAction,sample=shark.CinematicActionSeconds,marker=marker.ToString("F5"),aim=aim.ToString("F5"),intendedMarkerError=Vector3.Distance(marker,aim),localError=Vector3.Distance(local,expected),cached,fresh,stalePosition,staleAngle,endpoint=F(film,"attackEndpointCost"),route=F(film,"attackRouteCost"),priorCorrection=contact.LastCorrection.ToString("F5")});
    }
}
public static class ProbeTailPlacementV070Run
{
    public static object Start(string output,int seed=20261012)
    {
        if(!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<OpeningBenchmark>()!=null)throw new Exception("Fresh original Play Mode required");
        var go=new GameObject("Native tail placement probe v070");UnityEngine.Object.DontDestroyOnLoad(go);
        var b=go.AddComponent<OpeningBenchmark>();b.captureScreenshots=false;b.waterEntry=true;b.scatterSeed=seed;
        var p=go.AddComponent<ProbeTailPlacementV070>();p.benchmark=b;p.output=output;
        return new{started=true,output,diagnosticOnly=true,performanceEvidence=false};
    }
}
