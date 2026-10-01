using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public static class InspectMenuStoneDecoration
{
    public static object Run()
    {
        var menu=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Single(c=>c.name=="Main Menu" && c.gameObject.scene.name=="MainMenu");
        var clone=UnityEngine.Object.Instantiate(menu.gameObject);
        var cameraObject=new GameObject("Temporary menu capture camera");
        var target=new RenderTexture(1920,1080,24);
        var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            foreach(var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            var camera=cameraObject.AddComponent<Camera>(); camera.enabled=false; camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            camera.orthographic=true; camera.orthographicSize=540;
            camera.nearClipPlane=.01f; camera.farClipPlane=100; camera.targetTexture=target;
            camera.transform.position=new Vector3(0,0,-10);
            var canvas=clone.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera; canvas.planeDistance=1;
            var stone=clone.GetComponentsInChildren<SahurDecorationHover>().Single(d=>d.name=="Stone Decoration");
            for(int i=0;i<2;i++)
            {
                if(i==0) stone.OnPointerExit(null); else stone.OnPointerEnter(null);
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                File.WriteAllBytes(Path.GetFullPath(i==0 ? "Tools/MenuStoneDecoration.png":"Tools/MenuStoneDecorationColored.png"),pixels.EncodeToPNG());
            }
            return new { normal="Tools/MenuStoneDecoration.png",hover="Tools/MenuStoneDecorationColored.png" };
        }
        finally
        {
            RenderTexture.active=previous; UnityEngine.Object.DestroyImmediate(clone);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
