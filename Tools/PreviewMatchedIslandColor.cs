using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class PreviewMatchedIslandColor
{
    public static async Task<object> Runtime()
    {
        if(!Application.isPlaying) throw new Exception("Requires Play mode.");
        var source=UnityEngine.Object.FindObjectsByType<MenuIslandDecoration>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .First(d=>d.gameObject.scene.name=="MainMenu");
        var root=new GameObject("Temporary local colour runtime test",typeof(RectTransform),typeof(Canvas));
        var clone=UnityEngine.Object.Instantiate(source.gameObject,root.transform); clone.SetActive(true);
        var scale=Time.timeScale;
        try
        {
            var island=clone.GetComponent<MenuIslandDecoration>(); var hover=clone.GetComponent<SahurDecorationHover>();
            var image=clone.GetComponent<Image>(); var normal=image.material;
            if(normal.GetFloat("_LocalCorrection")!=0 || hover.hoverMaterial.GetFloat("_LocalCorrection")!=0)
                throw new Exception("Local correction unexpectedly enabled.");
            var click=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            float angle=clone.transform.localEulerAngles.z; Time.timeScale=0;
            hover.OnPointerEnter(click); island.OnPointerClick(click); await Task.Delay(1000);
            if(island.IsRotating || Mathf.Abs(Mathf.Abs(Mathf.DeltaAngle(angle,clone.transform.localEulerAngles.z))-180)>.1f)
                throw new Exception("Rotation failed after correction.");
            if(image.material!=hover.hoverMaterial) throw new Exception("Rotation lost colored local correction.");
            hover.OnPointerExit(click); if(image.material!=normal) throw new Exception("Black-and-white local correction not restored.");
            return new {bothVariantsUncalibrated=true,rotation180=true,pausedRotation=true,hoverRestore=true};
        }
        finally {Time.timeScale=scale; UnityEngine.Object.DestroyImmediate(root);}
    }
    public static async Task<object> Run()
    {
        var menu=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Single(c=>c.name=="Main Menu" && c.gameObject.scene.name=="MainMenu");
        var clone=UnityEngine.Object.Instantiate(menu.gameObject);
        var cameraObject=new GameObject("Temporary matched island preview");
        var target=new RenderTexture(1920,1080,24); var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            foreach(var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            camera.orthographic=true; camera.orthographicSize=540; camera.nearClipPlane=.01f; camera.farClipPlane=100;
            camera.targetTexture=target; camera.transform.position=new Vector3(0,0,-10);
            var canvas=clone.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            var island=clone.GetComponentInChildren<MenuIslandDecoration>(true); var hover=island.GetComponent<SahurDecorationHover>();
            var image=island.GetComponent<Image>(); var rect=(RectTransform)island.transform;
            var size=rect.sizeDelta; var position=rect.anchoredPosition; var angle=rect.localEulerAngles.z;
            hover.OnPointerExit(null); var normalMaterial=image.material;
            if(!normalMaterial.HasProperty("_WhiteBalance")) throw new Exception("Normal material not saved.");
            if(normalMaterial.GetFloat("_LocalCorrection")!=0 || hover.hoverMaterial.GetFloat("_LocalCorrection")!=0)
                throw new Exception("Local correction unexpectedly enabled.");
            await Capture("Tools/IslandColorMatchedNormal.png");
            foreach(var decoration in clone.GetComponentsInChildren<SahurDecorationHover>(true)) decoration.OnPointerEnter(null);
            if(image.material!=hover.hoverMaterial) throw new Exception("Colored material missing.");
            await Capture("Tools/IslandColorMatchedColored.png");
            hover.OnPointerExit(null);
            if(image.material!=normalMaterial || image.sprite!=hover.normalSprite) throw new Exception("Exit failed to restore normal material.");
            if(rect.sizeDelta!=size || rect.anchoredPosition!=position || Mathf.Abs(Mathf.DeltaAngle(rect.localEulerAngles.z,angle))>.01f) throw new Exception("Colour correction changes layout.");
            // Fit the entire drawing for inspecting all regions; only the temporary clone moves.
            foreach(var child in clone.transform.Cast<Transform>().ToArray())
                if(child!=island.transform) child.gameObject.SetActive(false);
            island.transform.SetParent(clone.transform,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;
            rect.sizeDelta=new Vector2(1100,1100*hover.normalSprite.rect.height/hover.normalSprite.rect.width);
            await Capture("Tools/IslandLocalNormalFull.png");
            hover.OnPointerEnter(null); await Capture("Tools/IslandLocalColoredFull.png");
            return new {hover=true,normalRestored=true,layoutPreserved=true,contrast=image.material.GetFloat("_Contrast"),coloredContrast=hover.hoverMaterial.GetFloat("_Contrast")};
            async Task Capture(string path)
            {
                Canvas.ForceUpdateCanvases(); await Task.Delay(100); camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply(); File.WriteAllBytes(Path.GetFullPath(path),pixels.EncodeToPNG());
            }
        }
        finally { RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(clone); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels); }
    }
}
