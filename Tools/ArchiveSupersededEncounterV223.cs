using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class ArchiveSupersededEncounterV223
{
    static readonly string[] Names={"CH1_SahurGetUp_v214","CH1_SahurProneDodge_v214","CH1_SahurOverhead_v214"};
    public static object Run(bool apply=false)
    {
        if(Application.isPlaying||EditorApplication.isCompiling)throw new Exception("Use ready Edit Mode");
        var sources=Names.Select(n=>"Assets/Resources/Encounter/v214/"+n).ToArray();
        bool Under(string path)=>sources.Any(s=>path==s||path.StartsWith(s+"/",StringComparison.Ordinal));
        var roots=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)&&!Under(p)).ToArray();
        var references=AssetDatabase.GetDependencies(roots,true).Where(Under).ToArray();
        if(references.Length>0)throw new Exception("Superseded assets are still referenced: "+string.Join(",",references));
        foreach(var code in Directory.GetFiles("Assets","*.cs",SearchOption.AllDirectories))
            if(Names.Any(n=>File.ReadAllText(code).Contains(n)))throw new Exception("Runtime code still names an archived clip: "+code);
        var plan=sources.Select(s=>new{source=s,target=s.Replace("Assets/Resources/Encounter/","Assets/ArtArchive/Encounter/"),files=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(s+"/")&&!AssetDatabase.IsValidFolder(p)).Select(p=>new{path=p,guid=AssetDatabase.AssetPathToGUID(p),bytes=new FileInfo(p).Length}).ToArray()}).ToArray();
        if(apply)
        {
            foreach(var move in plan)
            {
                Ensure(Path.GetDirectoryName(move.target).Replace('\\','/'));
                string error=AssetDatabase.MoveAsset(move.source,move.target);if(error.Length>0)throw new Exception(error);
                foreach(var file in move.files)
                {
                    string target=file.path.Replace(move.source,move.target);
                    if(AssetDatabase.AssetPathToGUID(target)!=file.guid)throw new Exception("GUID changed: "+target);
                }
            }
            File.WriteAllText(".codex/project-cleanup-20261018/archived-resources.json",Newtonsoft.Json.JsonConvert.SerializeObject(plan,Newtonsoft.Json.Formatting.Indented));
        }
        return new{apply,plan,references,sourcePreserved=true,stillImportedForHistory=true};
    }
    static void Ensure(string folder)
    {
        if(AssetDatabase.IsValidFolder(folder))return;
        string parent=Path.GetDirectoryName(folder).Replace('\\','/');Ensure(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));
    }
}
