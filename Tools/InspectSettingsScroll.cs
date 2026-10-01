using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
public static class InspectSettingsScroll
{
    public static object Run()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab");
        return new { playing=Application.isPlaying, menus=UnityEngine.Object.FindObjectsByType<PauseSettingsMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(m=>m.name).ToArray(),
            scrolls=prefab.GetComponentsInChildren<ScrollRect>(true).Select(s=>new { s.name,s.enabled,s.vertical,s.scrollSensitivity,view=s.viewport.rect.ToString(),content=s.content.rect.ToString(),graphics=s.viewport.GetComponentsInChildren<Graphic>(true).Where(g=>g.raycastTarget).Select(g=>g.name).ToArray(), viewportGraphic=s.viewport.GetComponent<Graphic>() ? s.viewport.GetComponent<Graphic>().GetType().Name : "none" }).ToArray(),
            modules=UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(m=>new { m.name,m.enabled, scroll=m.scrollWheel?.action?.bindings.Select(b=>b.effectivePath).ToArray() }).ToArray(),quality=QualitySettings.names,level=QualitySettings.GetQualityLevel(),lod=QualitySettings.lodBias };
    }
}
