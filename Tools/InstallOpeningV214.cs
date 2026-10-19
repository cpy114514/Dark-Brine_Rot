using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class InstallOpeningV214
{
    public static object Stage(string revision="v214")
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        if(!System.Text.RegularExpressions.Regex.IsMatch(revision,@"^v[0-9]+$"))throw new ArgumentException("Version required");
        var helper=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityAnimationImport")).FirstOrDefault(t=>t!=null);
        if(helper==null)throw new Exception("Compile/load UnityAnimationImport with run_script first");
        var method=helper.GetMethod("Stage");var result=new List<object>();
        foreach(var file in Directory.GetFiles("ArtSource/Encounter/"+revision,"*.fbx"))
        {
            string name=Path.GetFileNameWithoutExtension(file),stage="Assets/AnimationStaging/"+name;
            string final="Assets/Resources/Encounter/"+revision+"/"+name;
            if(AssetDatabase.IsValidFolder(final))continue;
            if(!AssetDatabase.IsValidFolder(stage))
                result.Add(method.Invoke(null,new object[]{Path.GetFullPath(file),Path.GetFullPath(Path.ChangeExtension(file,"animation.json")),stage,name.Contains("Sahur")?"Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx":""}));
            if(!AssetDatabase.IsValidFolder("Assets/Resources/Encounter/"+revision))AssetDatabase.CreateFolder("Assets/Resources/Encounter",revision);
            string error=AssetDatabase.MoveAsset(stage,final);if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        return new {staged=result.Count,results=result};
    }
    public static object Inspect(string revision="v214")
    {
        return AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Resources/Encounter/"+revision})
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>())
            .Where(c=>!c.name.StartsWith("__preview__")).Select(c=>new{c.name,c.length,c.frameRate,c.humanMotion}).ToArray();
    }
}
