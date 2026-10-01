using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Mavis;
public static class InspectLocalizedSettings
{
    public static async Task<object> Run()
    {
        var previous=GameLocalization.Language;
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/SettingsMenu/PauseSettingsMenu.prefab"));
        var menu=root.GetComponent<PauseSettingsMenu>();
        try
        {
            menu.Open(); typeof(PauseSettingsMenu).GetMethod("ShowSettings",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(menu,null);
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            await Task.Delay(1000); Canvas.ForceUpdateCanvases();
            var image=ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.GetFullPath("Tools/ChineseSettings.png"),image.EncodeToPNG()); UnityEngine.Object.Destroy(image);
            return new {alpha=menu.overlay.alpha,images=root.GetComponentsInChildren<Image>().Select(i=>new {i.name,alpha=i.color.a,cull=i.canvasRenderer.cull,shader=i.material.shader.name}).Take(8).ToArray()};
        }
        finally { menu.Resume(); UnityEngine.Object.Destroy(root); GameLocalization.SetLanguage(previous); }
    }
}
