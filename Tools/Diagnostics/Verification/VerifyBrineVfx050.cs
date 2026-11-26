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

public static class VerifyBrineVfx050
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static object Run()
    {
        if(!Application.isPlaying||!EditorApplication.isPaused||!GameObject.Find("Disposable native VFX review stage"))throw new Exception("Paused owned review required");
        var world=BrineVfxWorld.Get();var camera=Camera.main;var data=camera.GetUniversalAdditionalCameraData();
        var depth=data.requiresDepthOption;var color=data.requiresColorOption;bool heat=world.Style.heatRefraction;
        float scale=Time.timeScale;var results=new List<object>();
        Action<bool,string> check=(ok,name)=>results.Add(new{ok,name});
        Action reset=()=>{world.ClearAll();typeof(BrineVfxWorld).GetField("frame",Flags).SetValue(world,-1);};
        Vector3 point=GameObject.Find("Solid practice pillar").transform.position+Vector3.back*1.2f;
        try
        {
            var nativeErrors=new List<string>();Application.LogCallback logger=(message,trace,type)=>{if(type==LogType.Assert||type==LogType.Error||type==LogType.Exception)nativeErrors.Add(message);};
            Application.logMessageReceived+=logger;
            try
            {
                foreach(var mesh in new[]{BrineVfxGeometry.Veils(),BrineVfxGeometry.Veils(false,2),BrineVfxGeometry.Veils(true)})UnityEngine.Object.DestroyImmediate(mesh);
                check(nativeErrors.Count==0,"contact tips and charge meshes construct without invalid native bounds");
            }
            finally{Application.logMessageReceived-=logger;}
            reset();data.requiresDepthOption=CameraOverrideOption.Off;data.requiresColorOption=CameraOverrideOption.Off;
            for(int i=0;i<4;i++)check(world.Emit(301,point,Vector3.back,1,1,false,false,false,1,CombatMaterial.Metal),"metal contact accepted "+i);
            check(world.ActiveFragmentCount==4,"metal contacts emit solid ballistic geometry");
            check(!data.requiresDepthTexture&&!data.requiresColorTexture,"metal-only contacts require no scene copies");
            check(!world.Emit(301,point,Vector3.back,1,1,false),"fifth same-frame contact rejected");
            typeof(BrineVfxWorld).GetField("frame",Flags).SetValue(world,-1);
            for(int i=0;i<4;i++)world.Emit(302,point,Vector3.back,1,1,false,false,false,1,CombatMaterial.Wood);
            check(world.ActiveFragmentCount==6,"ballistic group ceiling is six");
            check(data.requiresDepthTexture&&!data.requiresColorTexture,"wood dust requests only scene depth");
            world.ClearOwner(301);check(world.ActiveFragmentCount==2,"owner cleanup releases only its fragments");
            reset();check(!data.requiresDepthTexture&&!data.requiresColorTexture,"clear restores explicit camera options");
            world.Style.heatRefraction=false;world.Emit(303,point,Vector3.up,2,1,true,false,true);
            check(!data.requiresColorTexture,"refraction toggle suppresses opaque scene copy");
            reset();world.Style.heatRefraction=true;world.Emit(303,point,Vector3.up,2,1,true,false,true);
            check(data.requiresDepthTexture&&data.requiresColorTexture,"single PC quality tier supports volume occlusion and refraction");
            reset();BossCombatVfx.Burst(point,Vector3.up,Color.white,12,2);
            check(world.ActiveCount==1&&data.requiresDepthTexture,"native boss dust cue enters volume pool");
            var contacts=((IEnumerable)typeof(BrineVfxWorld).GetField("contacts",Flags).GetValue(world)).Cast<object>();
            var active=contacts.First(c=>(bool)c.GetType().GetField("active").GetValue(c));
            var brush=(MeshRenderer)active.GetType().GetField("renderer").GetValue(active);
            check(!brush.enabled,"boss dust does not invent a melee contact flash");
            reset();Time.timeScale=0;check(!world.Emit(1,point,Vector3.up,1,1,true),"paused game rejects emissions");Time.timeScale=scale;
            // Real collision integration in an isolated invisible test region.
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary fragment collision contract";floor.transform.position=point+Vector3.down*3;floor.transform.localScale=new Vector3(30,.2f,30);
            var local=new BrineContactFragments(world.transform,world.Style.stone,99);
            try
            {
                Physics.SyncTransforms();local.Begin(500,point,Vector3.down,1,1,CombatMaterial.Wood,false);
                for(int i=0;i<45;i++)local.Tick(1f/60);
                check(local.CollisionCount>0,"wood fragments collide with actual scene geometry");
                for(int i=0;i<30;i++)local.Tick(1f/60);
                check(!local.Active,"fragment geometry retires after bounded lifetime");
            }
            finally{local.Dispose(true);UnityEngine.Object.DestroyImmediate(floor);}
            check(world.GetComponentsInChildren<ParticleSystem>().Length==0,"shared contact pool contains no particle emitters");
            check(new[]{world.Style.surface,world.Style.stone,world.Style.blast,world.Style.smokeDetail}.All(m=>m&&!ShaderUtil.ShaderHasError(m.shader)),"all four modified shaders compile");
            check(world.Style.smokeDetail.GetTexture("_Positive")&&world.Style.smokeDetail.GetTexture("_Negative"),"actual six-way source maps are included through the resource style");
            var result=new{passed=results.All(r=>(bool)r.GetType().GetProperty("ok").GetValue(r)),checks=results.Count,results};
            File.WriteAllText(".codex/vfx050/acceptance.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{Time.timeScale=scale;world.Style.heatRefraction=heat;world.ClearAll();data.requiresDepthOption=depth;data.requiresColorOption=color;}
    }
}
