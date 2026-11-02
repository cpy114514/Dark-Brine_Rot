using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Mavis;

public static class VerifyBrineVfxReload050
{
    public static object State(string label)
    {
        var worlds=UnityEngine.Object.FindObjectsByType<BrineVfxWorld>(FindObjectsSortMode.None);
        var owners=UnityEngine.Object.FindObjectsByType<CombatImpactVisuals>(FindObjectsSortMode.None);
        var result=new{passed=worlds.Length==1&&owners.Length>0,worlds=worlds.Length,
            worldMeshes=worlds.SelectMany(w=>w.GetComponentsInChildren<MeshFilter>(true)).Select(m=>m.sharedMesh).Where(m=>m).Distinct().Count(),
            fragments=worlds.Select(w=>w.GetComponentsInChildren<MeshFilter>(true).Count(m=>m.sharedMesh&&m.sharedMesh.name.StartsWith("Bounded material fragments"))),
            smokeBounds=Resources.FindObjectsOfTypeAll<Mesh>().Count(m=>m.name=="Cambered secondary simulated smoke"),
            weaponNodes=owners.Select(o=>new{o.name,nodes=o.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.name.StartsWith("Saltwind ")||r.name=="Weapon-bound converging brine")})};
        Directory.CreateDirectory(".codex/vfx050/reload");File.WriteAllText(".codex/vfx050/reload/"+label+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Reload()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        State("before");
        var stage=GameObject.Find("Disposable native VFX review stage");if(stage){var recorder=stage.GetComponent("ReviewBrineLate050");if(recorder)UnityEngine.Object.DestroyImmediate(recorder);}
        EditorApplication.isPaused=false;EditorUtility.RequestScriptReload();return new{queued=true};
    }
}
