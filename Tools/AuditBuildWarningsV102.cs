using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class AuditBuildWarningsV102
{
    public static object Check()
    {
        var method=typeof(BuildReport).GetMethod("GetLatestReport",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
        var report=(BuildReport)method.Invoke(null,null);
        var rows=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning || m.type==LogType.Error || m.type==LogType.Exception)
            .GroupBy(m=>m.content).Select(g=>new{count=g.Count(),type=g.First().type.ToString(),message=g.Key}).ToArray();
        File.WriteAllText(".codex/opening-optimization-20261012/build-messages-v102.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
        return new{unique=rows.Length,examples=rows.Take(8).ToArray()};
    }
}
