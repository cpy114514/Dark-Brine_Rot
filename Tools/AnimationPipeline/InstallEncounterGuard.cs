using System;
using UnityEditor;
public static class InstallEncounterGuard
{
    public static object Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        const string folder="Assets/Resources/Encounter/v002";
        AssetDatabase.CreateFolder("Assets/Resources/Encounter","v002");
        foreach(var name in new[]{"CH1_SahurGuard_v002","CH1_SahurBrace_v002"})
        {
            var error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,folder+"/"+name);
            if(error!="")throw new Exception(error);
        }
        return new{folder};
    }
}
