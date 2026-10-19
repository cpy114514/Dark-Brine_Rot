using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class DiagnoseCrushTreeV219
{
    public static object Start()
    {new GameObject("Native crush query diagnostic").AddComponent<CrushTreeProbeV219>();return new {started=true};}
}
[DefaultExecutionOrder(31000)]
public sealed class CrushTreeProbeV219:MonoBehaviour
{
    bool done;void Start(){Time.captureFramerate=15;}
    static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    void LateUpdate()
    {
        var b=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        if(done||b==null||b.RedesignClock<3.4f)return;done=true;
        var live=Get(b,"redRiggingSkinTree");var type=live.GetType();
        var ctor=type.GetConstructor(new[]{typeof(Vector3[]),typeof(int[])});
        var query=type.GetMethod("Touch",new[]{type,typeof(Vector3).MakeByRefType()});
        var points=(Vector3[])Get(b,"redRiggingQuerySkin");var triangles=(int[])type.GetProperty("Triangles").GetValue(live);
        var fresh=ctor.Invoke(new object[]{points,triangles});var rows=new List<object>();
        foreach(var g in (IEnumerable)Get(b,"redRiggingGroups"))
        foreach(var p in (IEnumerable)Get(g,"pieces"))
        {
            if((bool)Get(p,"released"))continue;
            var verts=(Vector3[])Get(p,"world");var indices=(int[])Get(p,"triangles");
            var tree=Get(p,"tree")??ctor.Invoke(new object[]{verts,indices});
            var watch=System.Diagnostics.Stopwatch.StartNew();bool a=(bool)query.Invoke(live,new object[]{tree,Vector3.zero});double liveMs=watch.Elapsed.TotalMilliseconds;
            watch.Restart();bool c=(bool)query.Invoke(fresh,new object[]{tree,Vector3.zero});double freshMs=watch.Elapsed.TotalMilliseconds;
            rows.Add(new {name=((GameObject)Get(p,"go")).name,triangles=indices.Length/3,liveMs,freshMs,a,c});
        }
        File.WriteAllText(".codex/encounter-redesign-20261017/revision-v219/query-diagnostic.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {b.RedesignClock,rows},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;EditorApplication.isPlaying=false;
    }
    void OnDestroy(){Time.captureFramerate=0;}
}
