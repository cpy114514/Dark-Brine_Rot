using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class AuditBuildMessagesV090
{
    public static object Check()
    {
        var method=typeof(BuildReport).GetMethod("GetLatestReport",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        if(method==null)throw new Exception("Latest build-report retrieval unavailable");
        var report=method.Invoke(null,null) as BuildReport;
        if(report==null)throw new Exception("No latest build report");
        var messages=report.steps.SelectMany(step=>step.messages.Select(message=>new{step=step.name,type=message.type.ToString(),message=message.content}))
            .Where(message=>message.type=="Error" || message.type=="Exception" || message.type=="Assert").ToArray();
        var result=new{status=report.summary.result.ToString(),report.summary.totalErrors,report.summary.totalWarnings,messages};
        File.WriteAllText(".codex/opening-optimization-20261012/build-messages-v090.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
