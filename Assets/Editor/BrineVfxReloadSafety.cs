using UnityEditor;
using UnityEngine;
using Mavis;

// Release only owned runtime meshes before Unity serializes the Play-mode world.
// The normal combat callbacks rebuild them after a script reload.
[InitializeOnLoad]
static class BrineVfxReloadSafety
{
    static BrineVfxReloadSafety()=>AssemblyReloadEvents.beforeAssemblyReload+=Release;
    static void Release()
    {
        if(!EditorApplication.isPlaying)return;
        foreach(var v in Resources.FindObjectsOfTypeAll<CombatImpactVisuals>())if(Application.IsPlaying(v.gameObject))v.ReleaseBeforeReload();
        foreach(var v in Resources.FindObjectsOfTypeAll<BossWeaponTrails>())if(Application.IsPlaying(v.gameObject))v.ReleaseBeforeReload();
        foreach(var v in Resources.FindObjectsOfTypeAll<BrineVfxWorld>())if(Application.IsPlaying(v.gameObject))v.ReleaseBeforeReload();
    }
}
