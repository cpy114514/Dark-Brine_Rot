using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;

public static class CompareNailongVfxV228
{
    static async Task Frames(int count=4)
    {
        int from=Time.frameCount;while(Time.frameCount<from+count)await Task.Delay(5);
    }
    static async Task Capture(Camera camera,string path)
    {
        await Frames();var previous=RenderTexture.active;
        var texture=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGB24,false);
        try {RenderTexture.active=camera.targetTexture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0,false);File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally {RenderTexture.active=previous;UnityEngine.Object.Destroy(texture);}
    }
    public static async Task<object> Run()
    {
        if(!Application.isPlaying||!EditorApplication.isPaused)throw new Exception("Use the paused render-qa native stage.");
        string folder=".codex/nailong-attacks-20261020/vfx-final";
        if(Directory.Exists(folder))throw new Exception("Preserve previous comparison.");Directory.CreateDirectory(folder);
        var camera=GameObject.Find("Native attack review camera").GetComponent<Camera>();
        var data=camera.GetComponent<UniversalAdditionalCameraData>();
        var water=UnityEngine.Object.FindObjectsByType<NailongSpitProjectile>(FindObjectsSortMode.None).First(p=>!p.HasImpacted);
        var renderers=water.GetComponentsInChildren<MeshRenderer>();var enabled=renderers.Select(r=>r.enabled).ToArray();
        var materials=renderers.Select(r=>r.sharedMaterial).Distinct().ToArray();
        var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;float oldFov=camera.fieldOfView;
        Time.timeScale=0;EditorApplication.isPaused=false;
        try
        {
            camera.transform.SetPositionAndRotation(water.transform.position+new Vector3(1.4f,.6f,-1.4f),
                Quaternion.LookRotation(new Vector3(-1.4f,-.6f,1.4f)));camera.fieldOfView=38;
            await Capture(camera,folder+"/water.png");
            foreach(var renderer in renderers)renderer.enabled=false;
            await Capture(camera,folder+"/water-reference.png");
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=enabled[i];
            data.requiresColorTexture=false;data.requiresDepthTexture=false;
            foreach(var material in materials)material.SetFloat("_UseRefraction",0);
            await Capture(camera,folder+"/water-fallback.png");
            foreach(var renderer in renderers)renderer.enabled=false;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.fieldOfView=oldFov;
            data.requiresColorTexture=true;data.requiresDepthTexture=true;
            var source=UnityEngine.Object.FindFirstObjectByType<NailongAttack>();
            var go=new GameObject("Temporary isolated visible pressure wave");go.transform.position=new Vector3(0,500,0);
            var wave=go.AddComponent<NailongGroundSlam>();wave.Initialize(source,10,1,1,2.4f,.65f,true);wave.Tick(1.1f);
            var rings=go.GetComponentsInChildren<MeshRenderer>();
            await Capture(camera,folder+"/pressure.png");
            foreach(var renderer in rings)renderer.enabled=false;
            await Capture(camera,folder+"/pressure-reference.png");
            foreach(var renderer in rings)
            {
                renderer.enabled=true;renderer.sharedMaterial.SetFloat("_UseRefraction",0);renderer.sharedMaterial.SetFloat("_UseDepth",0);
            }
            data.requiresColorTexture=false;data.requiresDepthTexture=false;
            await Capture(camera,folder+"/pressure-fallback.png");
            var diagnostics=new[]{"DarkBrine/Sonar Ring","DarkBrine/Nailong Water Droplet"}.Select(n=>new{
                shader=n,messages=ShaderUtil.GetShaderMessages(Shader.Find(n)).Select(m=>new{m.message,m.severity,m.line}).ToArray()}).ToArray();
            File.WriteAllText(folder+"/shaders.json",Newtonsoft.Json.JsonConvert.SerializeObject(diagnostics,Newtonsoft.Json.Formatting.Indented));
            return new{folder,normalCameraFrames=true,refractionAndNoOpaqueTextureFallback=true,diagnostics};
        }
        finally {Time.timeScale=1;EditorApplication.isPaused=false;EditorApplication.isPlaying=false;}
    }
}
