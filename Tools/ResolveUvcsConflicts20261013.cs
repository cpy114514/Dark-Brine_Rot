using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class ResolveUvcsConflicts20261013
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        string folder=".codex/uvcs-conflicts-20261013/";
        var validation=JObject.Parse(File.ReadAllText(folder+"native-validation.json"));
        if(!(bool)validation["passed"])throw new Exception("Validation required");
        var progress=JArray.Parse(File.ReadAllText(".plastic/plastic.incomingchangesprogress"));
        if(progress.Count!=1 || (int)progress[0]["SourceCsetId"]!=172 || (int)progress[0]["DestinationCsetId"]!=168)throw new Exception("Incoming merge changed; audit again");
        var conflicts=(JArray)progress[0]["FileConflicts"];
        if(conflicts.Count!=14)throw new Exception("Unexpected conflict count");
        var installed=JArray.Parse(File.ReadAllText(folder+"installed-manifest.json"));
        var paths=new List<string>();
        foreach(var item in conflicts)
        {
            string relative=((string)item["Path"]).TrimStart('/');
            string path=Path.GetFullPath(relative);
            string expected=(string)installed.First(i=>(string)i["path"]==relative&&i["sha256"]!=null)["sha256"];
            using(var hash=System.Security.Cryptography.SHA256.Create())
                if(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant()!=expected)throw new Exception("Validated merge changed: "+relative);
            // Merge selection uses repository paths (/Assets/...), whereas
            // file validation uses the absolute filesystem path above.
            paths.Add((string)item["Path"]);
        }
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var window=Resources.FindObjectsOfTypeAll<EditorWindow>().Single(w=>w.GetType().Name=="UVCSWindow");
        var switcher=window.GetType().GetField("mViewSwitcher",Flags).GetValue(window);
        var tab=switcher.GetType().GetProperty("IncomingChangesTab",Flags).GetValue(switcher);
        if(tab.GetType().FullName!="Unity.PlasticSCM.Editor.Views.Merge.Developer.MergeTab")throw new Exception("Unexpected merge view");
        var operation=tab.GetType().GetMethods(Flags).Single(m=>m.Name=="ProcessMerges"&&m.GetParameters().Length==4);
        var keepMergedWorkspace=Enum.Parse(operation.GetParameters()[3].ParameterType,"KeepDestination");
        // Same native operation as the Unity menu, after installing the reviewed
        // combined contents. No check-in, source overwrite or metadata editing.
        operation.Invoke(tab,new object[]{paths,paths,null,keepMergedWorkspace});
        var result=new{requested=true,files=paths.Count,operation="Unity UVCS ProcessMerges / KeepDestination",validatedMergedContents=true,checkin=false};
        File.WriteAllText(folder+"resolution-dispatch.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
