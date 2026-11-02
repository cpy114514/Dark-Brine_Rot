using System;
using System.IO;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEngine;

public static class VerifyBrineVfxReload037
{
    public static object State(string label)
    {
        var worlds=UnityEngine.Object.FindObjectsByType<BrineVfxWorld>(FindObjectsSortMode.None);
        var owners=UnityEngine.Object.FindObjectsByType<CombatImpactVisuals>(FindObjectsSortMode.None);
        var counts=owners.Select(o=>new{owner=o.name,nodes=o.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.name.StartsWith("Saltwind ")||r.name=="Weapon-bound converging brine")}).ToArray();
        var result=new{passed=worlds.Length==1&&counts.Length>0&&counts.All(c=>c.nodes==3),worlds=worlds.Length,counts,
            meshCount=Resources.FindObjectsOfTypeAll<Mesh>().Count(m=>m.name.Contains("cambered loft")||m.name.Contains("Projected terrain compression")||m.name.Contains("solid stratified")||m.name.Contains("pressure crescents")||m.name.Contains("helical blade")||m.name.Contains("ray bounds"))};
        Directory.CreateDirectory(".codex/vfx037/reload");File.WriteAllText(".codex/vfx037/reload/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    public static object Reload()
    {
        if(!Application.isPlaying)throw new Exception("Sandbox Play required");
        State("before");EditorApplication.isPaused=false;EditorUtility.RequestScriptReload();return new{queued=true};
    }
}
