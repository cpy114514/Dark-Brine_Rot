if(Application.isPlaying)throw new System.Exception("Run reference check outside Play mode");
var enabled=UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
var paths=UnityEditor.AssetDatabase.GetDependencies(enabled,true).Where(p=>p.StartsWith("Assets/")).Distinct().ToArray();
var issues=new System.Collections.Generic.List<object>();
var legacyRemovalOverrides=new System.Collections.Generic.List<object>();
var checkedGuids=new System.Collections.Generic.Dictionary<string,bool>();
int files=0,references=0,missingScripts=0,objectCount=0;
foreach(var path in paths)
{
    if(!System.IO.File.Exists(path)){issues.Add(new{path,problem="Dependency file missing"});continue;}
    string ext=System.IO.Path.GetExtension(path).ToLowerInvariant();
    if(!new[]{".unity",".prefab",".asset",".mat",".controller",".overridecontroller"}.Contains(ext))continue;
    string data=System.IO.File.ReadAllText(path);if(!data.StartsWith("%YAML"))continue;files++;
    foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(data,@"guid: ([a-fA-F0-9]{32})"))
    {
        string guid=match.Groups[1].Value;
        if(guid.StartsWith("0000000000000000"))continue;
        references++;
        if(!checkedGuids.TryGetValue(guid,out bool exists))
        {exists=!string.IsNullOrEmpty(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));checkedGuids[guid]=exists;}
        if(!exists)
        {
            int start=data.LastIndexOf("    m_RemovedGameObjects:",match.Index,System.StringComparison.Ordinal);
            int end=start<0?-1:data.IndexOf("\n    m_",start+25,System.StringComparison.Ordinal);
            if(start>=0&&(end<0||match.Index<end))legacyRemovalOverrides.Add(new{path,guid});
            else issues.Add(new{path,problem="Unresolved GUID",guid});
        }
    }
}
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
{
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
    {
        objectCount++;int n=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);missingScripts+=n;
        if(n>0)issues.Add(new{path=scene.path,problem="Missing component script",name=t.name,count=n});
    }
}
foreach(string path in new[]{"Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab","Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab","Assets/Game/Prefabs/Bosses/CappuccinoAssassino/CappuccinoAssassino.prefab","Assets/Game/Prefabs/Bosses/BombardinoCrocodilo/BombardinoCrocodilo.prefab"})
{
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if(!prefab){issues.Add(new{path,problem="Critical prefab missing"});continue;}
    foreach(var t in prefab.GetComponentsInChildren<Transform>(true))
    {
        int n=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);missingScripts+=n;
        if(n>0)issues.Add(new{path,problem="Missing prefab script",name=t.name,count=n});
    }
}
var result=new{enabledScenes=enabled,dependencies=paths.Length,yamlFiles=files,references,uniqueGuids=checkedGuids.Count,loadedSceneObjects=objectCount,missingScripts,issues,legacyRemovalOverrides};
System.IO.File.WriteAllText("Temp/SubmissionReferences.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return Newtonsoft.Json.JsonConvert.SerializeObject(result);
