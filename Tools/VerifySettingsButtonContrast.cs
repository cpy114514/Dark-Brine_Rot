using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mavis;
public static class VerifySettingsButtonContrast
{
    static float Lum(Color c)
    {
        float Linear(float v)=>v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);
        return .2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);
    }
    public static async Task<object> Run()
    {
        if(!Application.isPlaying) throw new Exception("Requires Play mode.");
        var menu=UnityEngine.Object.FindObjectsByType<PauseSettingsMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Single(m=>m.gameObject.scene.name=="MainMenu");
        var language=GameLocalization.Language; int checkedStates=0; float minimum=100;
        try
        {
            menu.OpenSettingsFromMainMenu();
            foreach(var lang in new[]{GameLanguage.English,GameLanguage.SimplifiedChinese})
            {
                GameLocalization.SetLanguage(lang);
                for(int section=0;section<menu.sectionTabs.Length;section++)
                {
                    menu.sectionTabs[section].onClick.Invoke(); await Task.Delay(50);
                    foreach(var button in menu.GetComponentsInChildren<Button>(true))
                    foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))
                    foreach(var state in new[]{button.colors.normalColor,button.colors.highlightedColor,button.colors.pressedColor,button.colors.selectedColor,button.colors.disabledColor})
                    {
                        var background=button.image.color*state;
                        // State alpha dims the fill, not its opaque caption; use the conservative raw fill.
                        float a=Lum(text.color),b=Lum(background);
                        float contrast=(Math.Max(a,b)+.05f)/(Math.Min(a,b)+.05f);
                        minimum=Math.Min(minimum,contrast); checkedStates++;
                        if(contrast<4.5f) throw new Exception("Unreadable button: "+button.name+" / "+text.text+" contrast="+contrast);
                    }
                }
            }
            GameLocalization.SetLanguage(GameLanguage.English);menu.sectionTabs[0].onClick.Invoke();
            await Capture(menu.gameObject,"Tools/SettingsButtonContrastFixed.png");
            return new{checkedStates,minimumContrast=minimum,bothLanguages=true,allSections=true};
        }
        finally {GameLocalization.SetLanguage(language);menu.Resume();}
    }
    static async Task Capture(GameObject root,string path)
    {
        var clone=UnityEngine.Object.Instantiate(root); var camGO=new GameObject("Temporary settings contrast camera");
        var rt=new RenderTexture(1920,1080,24);var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);var prev=RenderTexture.active;
        try
        {
            var menu=clone.GetComponent<PauseSettingsMenu>();menu.enabled=false;
            menu.homePanel.SetActive(false);menu.settingsPanel.SetActive(true);menu.overlay.alpha=1;
            foreach(var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            var camera=camGO.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=540;
            camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=rt;
            var canvas=clone.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();await Task.Delay(80);camera.Render();RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.GetFullPath(path),pixels.EncodeToPNG());
        }
        finally{RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(clone);UnityEngine.Object.DestroyImmediate(camGO);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels);}
    }
}
