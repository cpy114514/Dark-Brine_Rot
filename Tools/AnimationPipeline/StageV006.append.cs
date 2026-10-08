public static class StageV006
{
    [Serializable] public class IndexEntry {public string action,avatar;}
    public static object StageAll()
    {
        var reports=new System.Collections.Generic.List<object>();
        const string root="ArtSource/Encounter/v006";
        foreach(string path in Directory.GetFiles(root,"*-index.json"))
        {
            // JsonUtility does not accept a top-level array.
            var index=Newtonsoft.Json.JsonConvert.DeserializeObject<IndexEntry[]>(File.ReadAllText(path));
            foreach(var m in index)
            {
                string folder="Assets/AnimationStaging/"+m.action;
                if(AssetDatabase.IsValidFolder(folder))continue;
                reports.Add(UnityAnimationImport.Stage(root+"/"+m.action+".fbx",root+"/"+m.action+".animation.json",folder,m.avatar));
            }
        }
        Directory.CreateDirectory(".codex/encounter-v006");
        File.WriteAllText(".codex/encounter-v006/import-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
        return new {staged=reports.Count,isolated=true};
    }
    [Serializable] public class Index {public IndexEntry[] entries;}
}
