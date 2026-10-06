using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class PreviewBlenderSharkAnimation
{
    public static object Capture()
    {
        if(Application.isPlaying)throw new Exception("Requires Edit Mode.");
        string folder=Path.GetFullPath(".codex/shark-blender/preview");Directory.CreateDirectory(folder);
        var prefab=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations").rigPrefab;
        var rig=UnityEngine.Object.Instantiate(prefab);rig.transform.localScale=Vector3.one*100;
        var animator=rig.GetComponentInChildren<Animator>();animator.enabled=false;
        var model=animator.gameObject;
        foreach(var t in rig.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        var cameraGo=new GameObject("Shark preview camera");var camera=cameraGo.AddComponent<Camera>();
        camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.12f);
        camera.orthographic=true;camera.orthographicSize=9;camera.nearClipPlane=.1f;camera.farClipPlane=180;
        camera.transform.position=new Vector3(34,17,29);camera.transform.LookAt(new Vector3(0,5.4f,0));
        var lightGo=new GameObject("Shark preview light");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<31;
        light.transform.rotation=Quaternion.Euler(35,-35,0);
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/SharkAnimation/TralaleroAnimated.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        try
        {
            foreach(var clip in clips)for(int i=0;i<4;i++)
            {
                float time=clip.name=="Tail_Strike"||clip.name=="Ship_Smash" ? new[]{0,.25f,.65f,1.1f}[i] : clip.length*i/4;
                clip.SampleAnimation(model,time);Shot(camera,Path.Combine(folder,clip.name+"-"+i+".png"));
            }
            return new{frames=clips.Length*4,folder};
        }
        finally{UnityEngine.Object.DestroyImmediate(rig);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);}
    }
    static void Shot(Camera camera,string path)
    {
        var target=new RenderTexture(720,405,24);var frame=new Texture2D(720,405,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,720,405),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
}
