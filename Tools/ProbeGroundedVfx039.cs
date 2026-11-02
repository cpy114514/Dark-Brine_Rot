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

public static class ProbeGroundedVfx039
{
    const string Root=".codex/vfx039";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Texture2D Render(Camera camera)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();return image;}
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
    }
    static void Save(Texture2D image,string path){File.WriteAllBytes(Root+"/"+path+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
    public static object Verify()
    {
        if(!Application.isPlaying||!EditorApplication.isPaused||!GameObject.Find("Disposable native VFX review stage"))throw new Exception("Paused owned review only");
        var world=BrineVfxWorld.Get();var camera=Camera.main;var data=camera.GetUniversalAdditionalCameraData();var previous=data.requiresDepthOption;float scale=Time.timeScale;
        var rows=new List<object>();Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});
        Func<object[]> contacts=()=>((IEnumerable)typeof(BrineVfxWorld).GetField("contacts",Flags).GetValue(world)).Cast<object>().ToArray();
        Func<object,bool> active=c=>(bool)c.GetType().GetField("active",Flags).GetValue(c);
        Func<object,MeshRenderer> dust=c=>(MeshRenderer)c.GetType().GetField("dustRenderer",Flags).GetValue(c);
        var point=GameObject.Find("Solid practice pillar").transform.position+Vector3.back;
        try
        {
            world.ClearAll();data.requiresDepthOption=CameraOverrideOption.Off;typeof(BrineVfxWorld).GetField("frame",Flags).SetValue(world,-1);
            for(int i=0;i<4;i++)check(world.Emit(771,point,Vector3.back,1,1,true,false,false,1,(CombatMaterial)(i%3)),"accepted contact "+i);
            check(world.ActiveCount==4,"four accepted surfaces");
            check(contacts().Count(c=>active(c)&&dust(c).enabled)==3,"at most three simultaneous contact dust volumes");
            check(!world.Emit(771,point,Vector3.back,1,1,true),"fifth same-frame contact rejected");
            check(data.requiresDepthTexture,"ordinary contact dust leases depth for solid occlusion");
            foreach(var c in contacts().Where(c=>active(c)&&dust(c).enabled))
            {
                var block=new MaterialPropertyBlock();dust(c).GetPropertyBlock(block);check(block.GetFloat("_Dust")==1&&block.GetFloat("_Steps")<=20,"non-emissive bounded dust samples");
            }
            world.ClearOwner(771);check(world.ActiveCount==0&&contacts().All(c=>!dust(c).enabled),"owner cleanup retires all dust renderers");
            world.ClearAll();check(data.requiresDepthOption==CameraOverrideOption.Off,"explicit cleanup restores depth override");
            Time.timeScale=0;check(!world.Emit(771,point,Vector3.back,1,1,true),"paused game rejects new visual work");Time.timeScale=scale;
            check(world.GetComponentsInChildren<ParticleSystem>().Length==0,"contact feedback uses three-dimensional pooled geometry");
            check(new[]{world.Style.surface,world.Style.stone,world.Style.blast}.All(m=>!ShaderUtil.ShaderHasError(m.shader)),"all native shaders compile");
            var result=new{passed=rows.All(r=>(bool)r.GetType().GetProperty("ok").GetValue(r)),checks=rows.Count,rows};File.WriteAllText(Root+"/acceptance.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{Time.timeScale=scale;world.ClearAll();data.requiresDepthOption=previous;}
    }
    public static object Contact()
    {
        if(!Application.isPlaying||!EditorApplication.isPaused||!GameObject.Find("Disposable native VFX review stage"))throw new Exception("Paused owned review only");
        var camera=GameObject.Find("VFX review camera").GetComponent<Camera>();var target=GameObject.Find("Solid practice pillar").GetComponent<Collider>();var world=BrineVfxWorld.Get();world.ClearAll();
        var ray=new Ray(camera.transform.position,(target.bounds.center-camera.transform.position).normalized);if(!target.Raycast(ray,out var hit,100))throw new Exception("Target contact ray");
        var rows=new List<object>();
        try
        {
            Save(Render(camera),"contact-control");
            typeof(BrineVfxWorld).GetField("frame",Flags).SetValue(world,-1);
            if(!world.Emit(998,hit.point+hit.normal*.04f,hit.normal,1.5f,1.5f,true,false,false,1,CombatMaterial.Metal))throw new Exception("Probe emission rejected");
            var contacts=(IEnumerable)typeof(BrineVfxWorld).GetField("contacts",Flags).GetValue(world);var contact=contacts.Cast<object>().First(c=>(bool)c.GetType().GetField("active",Flags).GetValue(c));var type=contact.GetType();
            foreach(float age in new[]{.016f,.04f,.08f,.13f})
            {
                type.GetField("age",Flags).SetValue(contact,age);typeof(BrineVfxWorld).GetMethod("Apply",Flags).Invoke(world,new[]{contact});
                var renderer=(MeshRenderer)type.GetField("renderer",Flags).GetValue(contact);var dust=(MeshRenderer)type.GetField("dustRenderer",Flags).GetValue(contact);dust.enabled=false;
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);rows.Add(new{age,renderer.enabled,fade=block.GetFloat("_Fade"),mode=block.GetFloat("_Mode"),shaderAge=block.GetFloat("_Age"),mesh=renderer.GetComponent<MeshFilter>().sharedMesh.name});
                Save(Render(camera),"contact-surface-"+age.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            }
            File.WriteAllText(Root+"/contact-probe.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
        }
        finally{world.ClearAll();}
    }
}
