using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyCinematicLetterbox
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stopped Editor required");
        const string folder=".codex/cinematic-letterbox-20261013/";
        var scene=EditorSceneManager.NewPreviewScene();
        RenderTexture target=null;Texture2D pixels=null;var old=RenderTexture.active;
        var rows=new List<object>();
        try
        {
            var go=new GameObject("Diagnostic cinematic owners");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
            var first=go.AddComponent<Story1OceanAwakening>();var second=go.AddComponent<Story1WreckBattle>();
            CinematicLetterbox.Set(first,true);CinematicLetterbox.Set(first,true);
            const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
            var box=scene.GetRootGameObjects().Single(x=>x.name=="Cinematic letterbox").GetComponent<CinematicLetterbox>();
            var owners=(List<MonoBehaviour>)typeof(CinematicLetterbox).GetField("owners",Private).GetValue(box);
            if(owners.Count!=1)throw new Exception("Duplicate request");
            var canvas=box.GetComponent<Canvas>();
            if(canvas.sortingOrder!=450)throw new Exception("Letterbox must remain below the existing order-500 pause/death UI");
            void Check(int width,int height,float amount)
            {
                typeof(CinematicLetterbox).GetField("amount",Private).SetValue(box,amount);
                typeof(CinematicLetterbox).GetMethod("Apply",Private).Invoke(box,new object[]{amount,width,height});
                var bottom=(RectTransform)box.transform.Find("Bottom");var top=(RectTransform)box.transform.Find("Top");
                float visible=top.anchorMin.y-bottom.anchorMax.y;
                float expected=1-(1-Mathf.Min(1,(float)width/height/CinematicLetterbox.Aspect))*Mathf.SmoothStep(0,1,amount);
                if(Mathf.Abs(visible-expected)>.00001f)throw new Exception("Incorrect crop");
                if(box.GetComponentsInChildren<Image>().Any(x=>x.raycastTarget))throw new Exception("Bars intercept input");
                rows.Add(new{width,height,amount,visibleHeight=visible,barPixels=bottom.anchorMax.y*height,canvas.enabled});
            }
            Check(1920,1080,1);Check(2560,1440,1);Check(1920,1200,1);Check(3440,1440,1);Check(5120,1440,1);Check(1920,1080,.5f);
            CinematicLetterbox.Set(second,true);CinematicLetterbox.Set(first,false);
            if(owners.Count!=1 || CinematicLetterbox.VisibleHeight(16f/9)>.75f)throw new Exception("Owner handoff loses framing");
            CinematicLetterbox.Set(second,false);Check(1920,1080,0);
            if(canvas.enabled || CinematicLetterbox.VisibleHeight(16f/9)!=1)throw new Exception("Fullscreen not restored");
            CinematicLetterbox.Set(first,true);first.enabled=false;
            typeof(CinematicLetterbox).GetMethod("LateUpdate",Private).Invoke(box,null);
            if(owners.Count!=0)throw new Exception("Disabled owner retained");
            first.enabled=true;CinematicLetterbox.Set(first,true);Check(1920,1080,1);
            // Render the actual bar Images in an isolated UI scene, without entering Play Mode.
            canvas.renderMode=RenderMode.WorldSpace;
            var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(1920,1080);rect.position=Vector3.zero;
            var camGo=new GameObject("Letterbox preview camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo,scene);
            var cam=camGo.AddComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.orthographic=true;cam.orthographicSize=540;
            cam.transform.position=new Vector3(0,0,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.2f,.25f);cam.farClipPlane=100;
            target=new RenderTexture(1920,1080,24);target.Create();cam.targetTexture=target;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;
            pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
            File.WriteAllBytes(folder+"layout-preview.png",pixels.EncodeToPNG());
            var result=new{passed=true,aspect=CinematicLetterbox.Aspect,ownerHandoff=true,disabledOwnerCleanup=true,fullscreenRestored=true,inputPassThrough=true,rows};
            File.WriteAllText(folder+"layout-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            RenderTexture.active=old;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
