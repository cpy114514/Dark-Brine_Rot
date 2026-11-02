using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using Mavis;

public static class VerifyGrassContinuity038
{
    const string Root=".codex/grass038";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static WorldPerformanceManager Manager=>Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(w=>w.gameObject.scene.isLoaded);
    static IDictionary Registry=>(IDictionary)typeof(WorldPerformanceManager).GetField("grassLods",Flags).GetValue(Manager);
    static void Write(string name,object value)=>File.WriteAllText(Root+"/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    public static async Task<object> Ready()
    {
        var deadline=DateTime.UtcNow.AddSeconds(70);
        while(Registry.Count<11298||typeof(WorldPerformanceManager).GetField("cacheRoutine",Flags).GetValue(Manager)!=null){if(DateTime.UtcNow>deadline)throw new Exception("Island cache timeout");await Task.Yield();}
        return new{groups=Registry.Count,ready=true};
    }
    public static object LegacyComparison()
    {
        float timeScale=Time.timeScale;Time.timeScale=0;var root=new GameObject("Grass038 legacy probe");root.transform.position=new Vector3(-4000,2000,-4000);
        root.layer=31;var renderer=root.AddComponent<MeshRenderer>();var filter=root.AddComponent<MeshFilter>();
        var camera=new GameObject("Grass038 legacy camera",typeof(Camera)).GetComponent<Camera>();camera.cullingMask=1<<31;camera.enabled=false;camera.orthographic=true;camera.farClipPlane=20000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.GetUniversalAdditionalCameraData().renderShadows=false;
        var rows=new List<object>();var created=new List<Material>();
        try
        {
            var items=((IEnumerable)typeof(WorldPerformanceManager).GetField("foliage",Flags).GetValue(Manager)).Cast<object>().ToArray();
            foreach(var entry in Resources.Load<FoliageMeshLibrary>("FoliagePerformance/Library").entries.Where(e=>e.grass))
            {
                var item=items.FirstOrDefault(i=>(Mesh)i.GetType().GetField("original").GetValue(i)==entry.source);if(item==null)continue;
                var source=(Material[])item.GetType().GetField("materials").GetValue(item);var mats=source.Select(m=>new Material(m)).ToArray();created.AddRange(mats);
                foreach(var m in mats){if(m.HasProperty("_WindBend"))m.SetFloat("_WindBend",0);if(m.HasProperty("_PlayerPushStrength"))m.SetFloat("_PlayerPushStrength",0);if(m.HasProperty("_ClipBelowSea"))m.SetFloat("_ClipBelowSea",0);}
                renderer.sharedMaterials=mats;var b=entry.source.bounds;float size=Mathf.Max(b.size.x,b.size.y,b.size.z);
                camera.transform.position=root.transform.position+b.center+new Vector3(0,size*.12f,-size*1.8f);camera.transform.LookAt(root.transform.position+b.center);camera.orthographicSize=Mathf.Max(b.extents.y,b.extents.x/(16f/9))*1.25f;
                filter.sharedMesh=entry.source;var a=Render(camera);filter.sharedMesh=entry.near;var c=Render(camera);var ap=a.GetPixels32();var cp=c.GetPixels32();
                int av=ap.Count(p=>p.r>2||p.g>2||p.b>2),cv=cp.Count(p=>p.r>2||p.g>2||p.b>2);
                rows.Add(new{name=entry.source.name,sourceTriangles=entry.source.triangles.Length/3,nearTriangles=entry.near.triangles.Length/3,sourcePixels=av,nearPixels=cv});
                Save(a,"legacy-"+entry.source.name+"-original");Save(c,"legacy-"+entry.source.name+"-near");
            }
            Write("legacy-comparison",rows);return rows;
        }
        finally{Time.timeScale=timeScale;UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(root);foreach(var m in created)UnityEngine.Object.DestroyImmediate(m);}
    }
    static Texture2D Render(Camera camera,int w=640,int h=360)
    {
        var target=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();return image;}
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
    }
    static void Save(Texture2D image,string name){File.WriteAllBytes(Root+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
    public static object Audit()
    {
        var rows=new List<object>();int wrong=0,flagged=0,renderers=0;
        foreach(LODGroup group in Registry.Keys)
        {
            var levels=group.GetLODs();if(levels.Length!=1||levels[0].screenRelativeTransitionHeight!=0||group.fadeMode!=LODFadeMode.None)wrong++;
            foreach(var r in levels[0].renderers){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);renderers++;if(b.GetFloat("_ClipBelowSea")>.5f)flagged++;}
        }
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MavisFoliageWind.shader");
        var messages=ShaderUtil.GetShaderMessages(shader).Select(m=>new{m.message,severity=m.severity.ToString()}).ToArray();
        int checkedLegacy=0,wrongLegacy=0;
        var meshMap=(Dictionary<Mesh,Mesh>)typeof(WorldPerformanceManager).GetField("renderMeshes",Flags).GetValue(Manager);
        foreach(var item in (IEnumerable)typeof(WorldPerformanceManager).GetField("foliage",Flags).GetValue(Manager))
        {
            var t=item.GetType();if(!(bool)t.GetField("grass").GetValue(item)||(bool)t.GetField("forced").GetValue(item)||(bool)t.GetField("duplicate").GetValue(item))continue;
            var detail=(FoliageMeshLibrary.Entry)t.GetField("detail").GetValue(item);if(detail==null||!detail.far)continue;
            var bounds=(Bounds)t.GetField("bounds").GetValue(item);if(bounds.SqrDistance(Camera.main.transform.position)>170*170)continue;
            var expected=detail.far;if((bool)t.GetField("compactWind").GetValue(item)&&meshMap.TryGetValue(expected,out var compact))expected=compact;
            checkedLegacy++;if((Mesh)t.GetField("currentMesh").GetValue(item)!=expected)wrongLegacy++;
        }
        var result=new{passed=Registry.Count==11298&&wrong==0&&flagged==renderers&&wrongLegacy==0&&messages.Length==0,registered=Registry.Count,wrong,renderers,flagged,checkedLegacy,wrongLegacy,fade=Shader.GetGlobalVector("_MavisGrassFadeRange").ToString(),shaderMessages=messages};Write("cold-audit",result);return result;
    }
    public static object Policy()
    {
        var configure=typeof(WorldPerformanceManager).GetMethod("ConfigureGrassLod",Flags);
        var restore=typeof(WorldPerformanceManager).GetMethod("RestoreGrassLod",BindingFlags.Static|BindingFlags.NonPublic);
        var root=new GameObject("Grass038 ownership tests");var rows=new List<object>();
        Action<bool,string> check=(ok,name)=>rows.Add(new{ok,name});
        try
        {
            for(int variant=0;variant<4;variant++)
            {
                var go=new GameObject("variant"+variant,typeof(LODGroup));go.transform.SetParent(root.transform);go.transform.position=new Vector3(-4000,2200,-4000);var group=go.GetComponent<LODGroup>();var levels=new List<LOD>();
                for(int level=0;level<3;level++)
                {
                    var child=new GameObject("tier"+level,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(go.transform,false);
                    child.GetComponent<MeshFilter>().sharedMesh=variant==2?Resources.GetBuiltinResource<Mesh>("Cube.fbx"):AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass"+new[]{"Near","Far","Low"}[level]+".asset");
                    var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/NormalIslandGrass20261025/VerifiedGrass_0.mat");
                    if(variant==3){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.hideFlags=HideFlags.DontSave;}
                    child.GetComponent<MeshRenderer>().sharedMaterial=material;levels.Add(new LOD(new[]{.6f,.22f,.09f}[level],new[]{child.GetComponent<MeshRenderer>()}){fadeTransitionWidth=.07f+level*.01f});
                }
                var authored=levels.ToArray();group.SetLODs(authored);group.fadeMode=variant==1?LODFadeMode.CrossFade:LODFadeMode.None;group.animateCrossFading=true;
                for(int cycle=0;cycle<2;cycle++)
                {
                    configure.Invoke(Manager,new object[]{group.GetComponentsInChildren<MeshRenderer>()[0]});var actual=group.GetLODs();
                    bool owned=variant==0||variant==3;
                    check(Registry.Contains(group)==owned,"ownership variant"+variant+" cycle"+cycle);
                    if(owned)
                    {
                        check(actual.Length==1&&actual[0].renderers[0].GetComponent<MeshFilter>().sharedMesh.name=="VerifiedGrassNear","complete Near membership variant"+variant+" cycle"+cycle);
                        check(variant==0?actual[0].screenRelativeTransitionHeight==0&&group.fadeMode==LODFadeMode.None:Mathf.Abs(actual[0].screenRelativeTransitionHeight-.045f)<.000001f&&group.fadeMode==LODFadeMode.CrossFade,"shader-specific exit variant"+variant+" cycle"+cycle);
                        check(group.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)==1,"alternate layouts suppressed variant"+variant+" cycle"+cycle);
                        configure.Invoke(Manager,new object[]{actual[0].renderers[0]});check(Registry.Contains(group)&&group.GetLODs()[0].screenRelativeTransitionHeight==actual[0].screenRelativeTransitionHeight,"repeated configure stable variant"+variant+" cycle"+cycle);
                        restore.Invoke(null,new object[]{group,Registry[group]});Registry.Remove(group);
                        check(group.GetLODs().Length==3&&group.fadeMode==LODFadeMode.None&&group.animateCrossFading&&group.GetComponentsInChildren<MeshRenderer>().All(r=>r.enabled),"restore flags/membership variant"+variant+" cycle"+cycle);
                        check(group.GetLODs().Select((l,i)=>l.screenRelativeTransitionHeight==authored[i].screenRelativeTransitionHeight&&l.fadeTransitionWidth==authored[i].fadeTransitionWidth).All(v=>v),"restore thresholds/widths variant"+variant+" cycle"+cycle);
                    }
                    else check(actual.Length==3&&group.fadeMode==(variant==1?LODFadeMode.CrossFade:LODFadeMode.None),"unsupported/authored groups unchanged variant"+variant+" cycle"+cycle);
                }
                if(variant==3)foreach(var r in group.GetComponentsInChildren<MeshRenderer>())UnityEngine.Object.DestroyImmediate(r.sharedMaterial);
            }
            var result=new{passed=rows.All(r=>(bool)r.GetType().GetProperty("ok").GetValue(r)),checks=rows.Count,rows};Write("policy",result);return new{result.passed,result.checks};
        }
        finally{foreach(var g in root.GetComponentsInChildren<LODGroup>())if(Registry.Contains(g)){restore.Invoke(null,new object[]{g,Registry[g]});Registry.Remove(g);}UnityEngine.Object.DestroyImmediate(root);}
    }
    public static object Cost()
    {
        var source=(IEnumerable)typeof(WorldPerformanceManager).GetField("foliage",Flags).GetValue(Manager);
        var groups=new Dictionary<Mesh,List<object>>();
        foreach(var item in source){var type=item.GetType();if(!(bool)type.GetField("grass").GetValue(item))continue;var mesh=(Mesh)type.GetField("original").GetValue(item);if(!groups.ContainsKey(mesh))groups[mesh]=new List<object>();groups[mesh].Add(item);}
        var rows=groups.Select(pair=>{var i=pair.Value[0];var type=i.GetType();var detail=(FoliageMeshLibrary.Entry)type.GetField("detail").GetValue(i);var renderer=(MeshRenderer)type.GetField("renderer").GetValue(i);return new{mesh=pair.Key.name,count=pair.Value.Count,triangles=pair.Key.triangles.Length/3,detailFar=detail?.far?.name,farTriangles=detail?.far?.triangles.Length/3,batchable=type.GetField("batchable").GetValue(i),shader=renderer.sharedMaterial.shader.name};}).OrderByDescending(g=>(long)g.triangles*g.count).ToArray();Write("mesh-cost",rows);return rows;
    }
    static Vector3 Ground(Vector3 p)
    {
        var hits=Physics.RaycastAll(p+Vector3.up*400,Vector3.down,800,~0,QueryTriggerInteraction.Ignore);
        var terrain=hits.Where(h=>h.collider.GetComponentInParent<ProceduralIsland>()).OrderBy(h=>h.distance).FirstOrDefault();
        if(terrain.collider)p.y=terrain.point.y+6.5f;return p;
    }
    public static async Task<object> Scout()
    {
        var camera=Camera.main;var p=camera.transform.position;var q=camera.transform.rotation;
        var positions=new[]{new Vector3(288,29,346),new Vector3(250,35,500),new Vector3(340,50,450)};int index=0;
        try{foreach(var pos in positions)foreach(float yaw in new[]{0f,90f,180f,270f}){camera.transform.SetPositionAndRotation(Ground(pos),Quaternion.Euler(12,yaw,0));await Task.Yield();Save(Render(camera),"scout-"+index++);}}
        finally{camera.transform.SetPositionAndRotation(p,q);}return new{views=index};
    }
    public static async Task<object> LegacyVisibility()
    {
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float timeScale=Time.timeScale;Time.timeScale=0;
        var submission=Manager.GetComponent<GrassRenderSubmission>();bool wasEnabled=submission.enabled;submission.enabled=false;
        var rows=new List<object>();
        try
        {
            int view=0;
            foreach(var pose in new[]{new{p=Ground(new Vector3(288,29,346)),yaw=335f},new{p=Ground(new Vector3(250,35,500)),yaw=90f}})
            {
                camera.transform.SetPositionAndRotation(pose.p,Quaternion.Euler(12,pose.yaw,0));float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<2)await Task.Yield();
                int f=Time.frameCount;while(Time.frameCount<f+2)await Task.Yield();
                var c=Render(camera,960,540);
                typeof(WorldPerformanceManager).GetMethod("SubmitVisibleGrass",Flags).Invoke(Manager,null);
                var a=Render(camera,960,540);
                var ap=a.GetPixels32();var cp=c.GetPixels32();int diff=ap.Where((p,i)=>Math.Abs(p.r-cp[i].r)>2||Math.Abs(p.g-cp[i].g)>2||Math.Abs(p.b-cp[i].b)>2).Count();
                rows.Add(new{view,changedPixels=diff,scope="All manual grass batches; explicit submission positive control"});Save(a,"legacy-visible-"+view);Save(c,"legacy-hidden-"+view++);
            }
            Write("legacy-visibility",rows);return rows;
        }
        finally{submission.enabled=wasEnabled;Time.timeScale=timeScale;camera.transform.SetPositionAndRotation(position,rotation);}
    }
    public static async Task<object> Measure(string name)
    {
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        var timings=new FrameTiming[1];var rows=new List<object>();
        var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();bool clockRuns=clock.clockRuns;float hour=clock.startHour;
        var elapsedField=typeof(DayNightCycle).GetField("elapsedGameHours",Flags);float elapsed=(float)elapsedField.GetValue(clock);
        clock.clockRuns=false;clock.startHour=12;elapsedField.SetValue(clock,0f);typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",Flags).Invoke(clock,null);
        try
        {
            var poses=new[]{new{p=Ground(new Vector3(288,29,346)),yaw=335f},new{p=Ground(new Vector3(250,35,500)),yaw=90f}};
            for(int view=0;view<poses.Length;view++)
            {
                camera.transform.SetPositionAndRotation(poses[view].p,Quaternion.Euler(12,poses[view].yaw,0));
                float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<2)await Task.Yield();
                for(int sample=0;sample<90;sample++)
                {
                    int frame=Time.frameCount;while(Time.frameCount==frame)await Task.Yield();FrameTimingManager.CaptureFrameTimings();
                    uint count=FrameTimingManager.GetLatestTimings(1,timings);var t=timings[0];rows.Add(new{view,frame=Time.frameCount,triangles=UnityStats.triangles,drawCalls=UnityStats.drawCalls,cpuMs=count>0?t.cpuFrameTime:0,gpuMs=count>0?t.gpuFrameTime:0});
                }
            }
            var result=new{name,editorOnly=true,samples=rows.Count,rows};Write(name+"-timings",result);return new{name,samples=rows.Count};
        }
        finally{camera.transform.SetPositionAndRotation(position,rotation);clock.clockRuns=clockRuns;clock.startHour=hour;elapsedField.SetValue(clock,elapsed);typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",Flags).Invoke(clock,null);}
    }
    public static async Task<object> FixedPreview()
    {
        var items=((IEnumerable)typeof(WorldPerformanceManager).GetField("foliage",Flags).GetValue(Manager)).Cast<object>().ToArray();
        var selected=items.Where(i=>(bool)i.GetType().GetField("grass").GetValue(i)&&(FoliageMeshLibrary.Entry)i.GetType().GetField("detail").GetValue(i)!=null).Select(i=>new{item=i,original=(Mesh)i.GetType().GetField("original").GetValue(i),detail=(FoliageMeshLibrary.Entry)i.GetType().GetField("detail").GetValue(i)}).Where(s=>s.detail.far).ToArray();
        var settings=Registry.Keys.Cast<LODGroup>().Select(g=>new{g,levels=g.GetLODs(),mode=g.fadeMode}).ToArray();
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float scale=Time.timeScale;
        var submission=Manager.GetComponent<GrassRenderSubmission>();bool auto=submission.enabled;
        try
        {
            foreach(var s in settings){s.g.SetLODs(new[]{new LOD(0,s.levels[0].renderers)});s.g.fadeMode=LODFadeMode.None;}
            foreach(string variant in new[]{"fixed-balanced","fixed-highest"})
            {
                foreach(var s in selected){s.item.GetType().GetField("detail").SetValue(s.item,null);s.item.GetType().GetField("original").SetValue(s.item,variant=="fixed-balanced"?s.detail.far:s.original);}
                submission.enabled=true;Time.timeScale=1;await Measure(variant);
                Time.timeScale=0;submission.enabled=false;int view=0;
                foreach(var pose in new[]{new{p=Ground(new Vector3(288,29,346)),yaw=335f},new{p=Ground(new Vector3(250,35,500)),yaw=90f}})
                {
                    camera.transform.SetPositionAndRotation(pose.p,Quaternion.Euler(12,pose.yaw,0));float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<2)await Task.Yield();
                    typeof(WorldPerformanceManager).GetMethod("SubmitVisibleGrass",Flags).Invoke(Manager,null);Save(Render(camera,960,540),variant+"-"+view++);
                }
            }
            return new{compared=2,changedLegacyEntries=selected.Length};
        }
        finally
        {
            foreach(var s in selected){s.item.GetType().GetField("original").SetValue(s.item,s.original);s.item.GetType().GetField("detail").SetValue(s.item,s.detail);}
            foreach(var s in settings){s.g.SetLODs(s.levels);s.g.fadeMode=s.mode;}
            submission.enabled=auto;Time.timeScale=scale;camera.transform.SetPositionAndRotation(position,rotation);
        }
    }
    public static async Task<object> Coverage()
    {
        if(!Application.isPlaying||Registry.Count<10000)throw new Exception("Loaded island Play required");
        var source=((LODGroup)Registry.Keys.Cast<object>().First()).GetLODs()[0].renderers[0];
        var origin=new Vector3(-4000,2000,-4000);
        var stage=new GameObject("Grass038 isolated coverage fixture");stage.transform.position=origin;
        var meshGo=new GameObject("Original complete grass",typeof(MeshFilter),typeof(MeshRenderer));meshGo.transform.SetParent(stage.transform,false);
        meshGo.layer=31;
        meshGo.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
        // Retain the original renderer scale so the authored full blade layout is tested.
        meshGo.transform.localScale=source.transform.lossyScale;
        var materials=source.sharedMaterials.Select(m=>new Material(m)).ToArray();
        foreach(var material in materials){material.SetFloat("_WindBend",0);material.SetFloat("_LocalWindScale",0);material.SetFloat("_PlayerPushStrength",0);material.SetFloat("_ClipBelowSea",1);}
        meshGo.GetComponent<MeshRenderer>().sharedMaterials=materials;
        var camera=new GameObject("Grass038 ortho test",typeof(Camera)).GetComponent<Camera>();camera.cullingMask=1<<31;camera.enabled=false;camera.orthographic=true;camera.orthographicSize=4;camera.nearClipPlane=.1f;camera.farClipPlane=1000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        camera.GetUniversalAdditionalCameraData().renderShadows=false;
        float beach=Shader.GetGlobalFloat("_MavisBeachCeiling");Shader.SetGlobalFloat("_MavisBeachCeiling",-100000);
        float timeScale=Time.timeScale;Time.timeScale=0;
        var rows=new List<object>();Color32[] reference=null;int last=int.MaxValue;bool nearEqual=true,monotonic=true;int lastVisible=0;
        try
        {
            foreach(float distance in new float[]{20,34.9f,35.1f,59,60,70,79,81,100,120,130,139,140,141,150,160,170,176,185,200})
            {
                camera.transform.SetPositionAndRotation(origin+new Vector3(0,1.7f,-distance),Quaternion.identity);
                await Task.Yield();var image=Render(camera);var pixels=image.GetPixels32();
                int visible=pixels.Count(p=>p.r>2||p.g>2||p.b>2);if(reference==null)reference=pixels;
                int changed=pixels.Where((p,i)=>p.r!=reference[i].r||p.g!=reference[i].g||p.b!=reference[i].b).Count();
                int maskChanged=pixels.Where((p,i)=>(p.r>2||p.g>2||p.b>2)!=(reference[i].r>2||reference[i].g>2||reference[i].b>2)).Count();
                if(distance<=120&&maskChanged!=0)nearEqual=false;if(visible>last)monotonic=false;last=visible;lastVisible=visible;
                rows.Add(new{distance,visiblePixels=visible,changedFrom20m=changed,maskChangedFrom20m=maskChanged});
                if(distance==20||distance==70||distance==150||distance==170||distance==185)Save(image,"coverage-"+distance);else UnityEngine.Object.DestroyImmediate(image);
            }
            var result=new{passed=nearEqual&&monotonic&&lastVisible==0&&reference.Any(p=>p.r>2||p.g>2||p.b>2),nearEqual,monotonic,fullyGoneAt200=lastVisible==0,rows};Write("coverage",result);return result;
        }
        finally{Time.timeScale=timeScale;Shader.SetGlobalFloat("_MavisBeachCeiling",beach);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(stage);foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);}
    }
    public static async Task<object> Walk(string name="after",bool before=false)
    {
        if(!Application.isPlaying||Registry.Count<10000)throw new Exception("Loaded island Play required");
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        var fade=Shader.GetGlobalVector("_MavisGrassFadeRange");var settings=Registry.Keys.Cast<LODGroup>().Select(g=>new{g,levels=g.GetLODs(),mode=g.fadeMode}).ToArray();
        float timeScale=Time.timeScale;int capture=Time.captureFramerate;
        var submission=Manager.GetComponent<GrassRenderSubmission>();bool auto=submission.enabled;submission.enabled=false;
        var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();bool clockRuns=clock.clockRuns;float hour=clock.startHour;
        var elapsedField=typeof(DayNightCycle).GetField("elapsedGameHours",Flags);float elapsed=(float)elapsedField.GetValue(clock);
        clock.clockRuns=false;clock.startHour=12;elapsedField.SetValue(clock,0f);typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",Flags).Invoke(clock,null);
        Directory.CreateDirectory(Root+"/"+name);
        try
        {
            if(before){Shader.SetGlobalVector("_MavisGrassFadeRange",Vector4.zero);foreach(var s in settings){var l=s.levels[0];l.screenRelativeTransitionHeight=.0463295f;l.fadeTransitionWidth=.0161934f;s.g.SetLODs(new[]{l});s.g.fadeMode=LODFadeMode.CrossFade;}}
            Time.captureFramerate=30;Time.timeScale=1;
            bool coast=name.Contains("coast");var start=coast?new Vector3(250,35,500):new Vector3(288,29,346);var direction=coast?Vector3.right:new Vector3(-.422618f,0,.906308f);
            var report=new List<object>();
            for(int frame=0;frame<240;frame++)
            {
                float t=frame<120?frame/119f:(239-frame)/119f;var p=start+direction*(t*30);
                if(Physics.Raycast(p+Vector3.up*200,Vector3.down,out var hit,500,~0,QueryTriggerInteraction.Ignore))p.y=hit.point.y+6.5f;
                camera.transform.SetPositionAndRotation(p,Quaternion.Euler(8,coast?90:-25,0));await Task.Yield();
                typeof(WorldPerformanceManager).GetMethod("SubmitVisibleGrass",Flags).Invoke(Manager,null);
                Save(Render(camera,960,540),name+"/"+frame.ToString("D4"));
                if(frame%30==0)report.Add(new{frame,position=p.ToString(),triangles=UnityEditor.UnityStats.triangles,drawCalls=UnityEditor.UnityStats.drawCalls});
            }
            var result=new{frames=240,width=960,height=540,fixedStep=30,rangeMetres=30,before,report};Write(name+"-walk",result);return result;
        }
        finally{submission.enabled=auto;camera.transform.SetPositionAndRotation(position,rotation);Shader.SetGlobalVector("_MavisGrassFadeRange",fade);foreach(var s in settings)if(s.g){s.g.SetLODs(s.levels);s.g.fadeMode=s.mode;}Time.captureFramerate=capture;Time.timeScale=timeScale;clock.clockRuns=clockRuns;clock.startHour=hour;elapsedField.SetValue(clock,elapsed);typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",Flags).Invoke(clock,null);}
    }
}
