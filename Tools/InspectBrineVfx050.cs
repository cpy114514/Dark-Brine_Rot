using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class InspectBrineVfx050
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static byte[] Render(Camera camera)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24);
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();return pixels.EncodeToPNG();}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels);}
    }
    public static object Occlusion()
    {
        if(!Application.isPlaying||!EditorApplication.isPaused||!GameObject.Find("Disposable native VFX review stage"))throw new Exception("Paused owned review required");
        var camera=GameObject.Find("VFX review camera").GetComponent<Camera>();var world=BrineVfxWorld.Get();world.ClearAll();
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary solid VFX occluder";
        wall.transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*3,camera.transform.rotation);wall.transform.localScale=new Vector3(30,30,.5f);
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.3f,.33f,.35f));wall.GetComponent<Renderer>().sharedMaterial=material;
        var before=new Texture2D(2,2);var after=new Texture2D(2,2);
        try
        {
            Physics.SyncTransforms();var baseline=Render(camera);before.LoadImage(baseline);
            var point=GameObject.Find("Solid practice pillar").transform.position+Vector3.down*2;
            typeof(BrineVfxWorld).GetField("frame",Flags).SetValue(world,-1);world.Emit(405,point,Vector3.up,4,1,false,false,true);
            var c=((IEnumerable)typeof(BrineVfxWorld).GetField("contacts",Flags).GetValue(world)).Cast<object>().First(x=>(bool)x.GetType().GetField("active").GetValue(x));
            c.GetType().GetField("age").SetValue(c,.3f);typeof(BrineVfxWorld).GetMethod("Apply",Flags).Invoke(world,new[]{c});
            foreach(var light in world.GetComponentsInChildren<Light>())light.enabled=false;
            var effect=Render(camera);after.LoadImage(effect);var a=before.GetPixels32();var b=after.GetPixels32();long sum=0;int maximum=0;
            for(int i=0;i<a.Length;i++)foreach(int d in new[]{Math.Abs(a[i].r-b[i].r),Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)}){sum+=d;maximum=Math.Max(maximum,d);}
            var result=new{passed=maximum<=1,maximumChannelDifference=maximum,meanChannelDifference=sum/(double)(a.Length*3),note="Opaque full-frame wall in front of both volume and six-way mesh; bounce lights disabled for comparison."};
            Directory.CreateDirectory(".codex/vfx050/occlusion");File.WriteAllBytes(".codex/vfx050/occlusion/control.png",baseline);File.WriteAllBytes(".codex/vfx050/occlusion/effect-behind-wall.png",effect);
            File.WriteAllText(".codex/vfx050/occlusion/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{world.ClearAll();UnityEngine.Object.DestroyImmediate(wall);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);}
    }
    public static object Angles(string label)
    {
        if(!Application.isPlaying||!EditorApplication.isPaused||!GameObject.Find("Disposable native VFX review stage"))throw new Exception("Paused owned review required");
        string folder=".codex/vfx050/"+label;if(Directory.Exists(folder))throw new Exception("Preserve prior inspection");Directory.CreateDirectory(folder);
        var camera=GameObject.Find("VFX review camera").GetComponent<Camera>();var main=Camera.main;var world=BrineVfxWorld.Get();
        Vector3 point=GameObject.Find("Solid practice pillar").transform.position+new Vector3(4,-3,1);
        var savedPosition=main.transform.position;var savedRotation=main.transform.rotation;var cp=camera.transform.position;var cr=camera.transform.rotation;
        var rows=new List<object>();var shaders=new List<Shader>();var materials=new List<Material>();
        try
        {
            foreach(float angle in new[]{0,90,180,270})
            {
                world.ClearAll();typeof(BrineVfxWorld).GetField("frame",Flags).SetValue(world,-1);
                Vector3 position=point+Quaternion.Euler(0,angle,0)*new Vector3(8,5,-10);
                camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(point+Vector3.up*2-position));
                main.transform.SetPositionAndRotation(position,camera.transform.rotation);
                world.Emit(404,point,Vector3.up,3,1,true,false,true);
                var contacts=(IEnumerable)typeof(BrineVfxWorld).GetField("contacts",Flags).GetValue(world);
                var c=contacts.Cast<object>().First(x=>(bool)x.GetType().GetField("active").GetValue(x));var type=c.GetType();
                type.GetField("age").SetValue(c,.38f);typeof(BrineVfxWorld).GetMethod("Apply",Flags).Invoke(world,new[]{c});
                File.WriteAllBytes(folder+"/angle-"+angle+".png",Render(camera));
            }
            // Component-isolated comparison at an identical native camera/time.
            // This is a shader inspection, not a separate gameplay recording.
            world.ClearAll();typeof(BrineVfxWorld).GetField("frame",Flags).SetValue(world,-1);
            camera.transform.SetPositionAndRotation(cp,cr);main.transform.SetPositionAndRotation(savedPosition,savedRotation);
            world.Emit(404,point,Vector3.up,3,1,true,false,true);
            var all=((IEnumerable)typeof(BrineVfxWorld).GetField("contacts",Flags).GetValue(world)).Cast<object>();
            var chosen=all.First(x=>(bool)x.GetType().GetField("active").GetValue(x));var t=chosen.GetType();t.GetField("age").SetValue(chosen,.32f);typeof(BrineVfxWorld).GetMethod("Apply",Flags).Invoke(world,new[]{chosen});
            var renderer=(MeshRenderer)t.GetField("renderer").GetValue(chosen);var smoke=(MeshRenderer)t.GetField("smokeRenderer").GetValue(chosen);
            File.WriteAllBytes(folder+"/hybrid.png",Render(camera));
            smoke.enabled=false;File.WriteAllBytes(folder+"/volume-only.png",Render(camera));
            var source=File.ReadAllText(".codex/vfx050/rollback/Assets/Scripts/Effects/BrineThermalVolume.shader").Replace("DarkBrine/Brine Thermal Volume","DarkBrine/Inspection baseline thermal");
            var oldShader=ShaderUtil.CreateShaderAsset(source,false);shaders.Add(oldShader);var mat=new Material(oldShader);materials.Add(mat);
            var original=renderer.sharedMaterial;mat.CopyPropertiesFromMaterial(original);renderer.sharedMaterial=mat;
            File.WriteAllBytes(folder+"/baseline-volume.png",Render(camera));renderer.sharedMaterial=original;
            var sourceMaps=new[]{world.Style.smokeDetail.GetTexture("_Positive"),world.Style.smokeDetail.GetTexture("_Negative")};
            var result=new{angles=4,sourceTextureBytes=sourceMaps.Sum(m=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m)),sourceMaps=sourceMaps.Select(m=>new{m.name,m.width,m.height}),baselineShaderError=ShaderUtil.ShaderHasError(oldShader)};
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{world.ClearAll();camera.transform.SetPositionAndRotation(cp,cr);main.transform.SetPositionAndRotation(savedPosition,savedRotation);foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);foreach(var s in shaders)UnityEngine.Object.DestroyImmediate(s);}
    }
}
