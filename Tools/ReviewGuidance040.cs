using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ReviewGuidance040
{
    const string Root=".codex/guidance040";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static GameObject stage,player;static Camera camera;static MainQuestGuidance guide;static FirstIslandObjectives objectives;
    static readonly List<object> checks=new List<object>();
    static readonly List<string> errors=new List<string>();
    static void Log(string s,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(s+"\n"+trace);}
    static void Check(bool value,string label){checks.Add(new{ok=value,label});if(!value)throw new Exception(label);}
    static WorldPerformanceManager Manager=>Resources.FindObjectsOfTypeAll<WorldPerformanceManager>().Single(m=>m.gameObject.scene.isLoaded);
    static void Bind()
    {
        stage=GameObject.Find("Owned guidance040 stage");if(!stage)throw new Exception("Setup first");
        player=stage.GetComponentInChildren<ThirdPersonPlayerController>().gameObject;guide=player.GetComponent<MainQuestGuidance>();objectives=player.GetComponent<FirstIslandObjectives>();
        camera=GameObject.Find("Guidance040 camera").GetComponent<Camera>();
    }
    static async Task Frames(int n){int end=Time.frameCount+n;var deadline=DateTime.UtcNow.AddSeconds(45);while(Time.frameCount<end){if(DateTime.UtcNow>deadline)throw new Exception("Native frames stalled");await Task.Delay(5);}}
    static async Task Wait(Func<bool> condition,int seconds=45){var deadline=DateTime.UtcNow.AddSeconds(seconds);while(!condition()){if(DateTime.UtcNow>deadline)throw new Exception("Native condition timed out");await Frames(1);}}
    static Vector3 Ground(Vector3 p)
    {
        var hits=Physics.RaycastAll(p+Vector3.up*200,Vector3.down,500,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        var hit=hits.First(h=>h.collider.GetComponentInParent<ProceduralIsland>());return hit.point;
    }
    static void Write(string file,object value)=>File.WriteAllText(Root+"/"+file+".json",Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    static Texture2D Render(Camera c,int width=1280,int height=720,bool batches=true)
    {
        var target=RenderTexture.GetTemporary(width,height,24);var previous=c.targetTexture;var active=RenderTexture.active;
        try
        {
            if(batches)typeof(WorldPerformanceManager).GetMethod("SubmitVisibleGrass",Flags).Invoke(Manager,null);
            c.targetTexture=target;c.Render();RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();return image;
        }
        finally{c.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
    }
    static void Save(Texture2D image,string name){File.WriteAllBytes(Root+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
    public static async Task<object> Setup()
    {
        if(!Application.isPlaying||GameSaveManager.HasSavableGame)throw new Exception("Environment-only disposable Play required");
        await Wait(()=>typeof(WorldPerformanceManager).GetField("cacheRoutine",Flags).GetValue(Manager)==null,100);
        var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.clockRuns=false;clock.startHour=10;
        typeof(DayNightCycle).GetField("elapsedGameHours",Flags).SetValue(clock,0f);typeof(DayNightCycle).GetMethod("ApplyTimeOfDay",Flags).Invoke(clock,null);
        foreach(var a in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(a.GetType().Name.EndsWith("AI")||a is BombardinoBoss)a.enabled=false;
        stage=new GameObject("Owned guidance040 stage");stage.AddComponent<GameSaveExcluded>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");player=UnityEngine.Object.Instantiate(prefab,stage.transform);player.AddComponent<GameSaveExcluded>();
        var cc=player.GetComponent<CharacterController>();var point=Ground(new Vector3(288,29,346));float sole=(cc.center.y-cc.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        player.GetComponent<ThirdPersonPlayerController>().RestoreSavedPose(point+Vector3.up*(.03f-sole),Quaternion.identity);
        objectives=player.GetComponent<FirstIslandObjectives>()??player.AddComponent<FirstIslandObjectives>();
        await Frames(2);objectives.Restore(new FirstIslandObjectives.Progress{stage=4,combatLesson=4});
        typeof(FirstIslandObjectives).GetMethod("DiscoverBosses",Flags).Invoke(objectives,null);
        await Frames(2);guide=player.GetComponent<MainQuestGuidance>();
        Check(objectives.TryGetGuidanceTarget(out var goal,out _),"real objective source supplies the unlocked main boss");
        Vector3 direction=Vector3.ProjectOnPlane(goal-player.transform.position,Vector3.up).normalized;
        player.transform.rotation=Quaternion.LookRotation(direction);camera=new GameObject("Guidance040 camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=60;camera.farClipPlane=2000;
        camera.transform.position=player.transform.position-direction*10+Vector3.Cross(Vector3.up,direction)*5+Vector3.up*5;
        camera.transform.LookAt(player.transform.position+direction*9+Vector3.up*1.5f);camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
        await Wait(()=>guide.RoutePointCount>2&&guide.Visible,60);
        var result=new{ready=true,guide.TargetId,guide.RoutePointCount,guide.SolvedRoutes,player=player.transform.position.ToString(),goal=guide.TargetPosition.ToString()};Write("setup",result);return result;
    }
    public static async Task<object> Capture()
    {
        Bind();
        if(!guide||!guide.Visible)throw new Exception("Setup first");
        Directory.CreateDirectory(Root+"/frames");float capture=Time.captureDeltaTime;float scale=Time.timeScale;
        var submission=Manager.GetComponent<GrassRenderSubmission>();bool original=submission.enabled;submission.enabled=false;
        try
        {
            Time.captureDeltaTime=1f/30;Time.timeScale=1;
            for(int f=0;f<180;f++)
            {
                await Frames(1);Save(Render(camera,960,540),"frames/"+f.ToString("D4"));
                if(f==60)Save(Render(camera),"guidance-and-grass");
            }
            return new{frames=180,fixedStep=30,guide.RoutePointCount,guide.TargetId};
        }
        finally{Time.captureDeltaTime=capture;Time.timeScale=scale;submission.enabled=original;}
    }
    public static object ShadowImages()
    {
        Bind();
        if(!camera)throw new Exception("Setup first");
        var range=Shader.GetGlobalVector("_MavisGrassShadowRange");var submission=Manager.GetComponent<GrassRenderSubmission>();bool enabled=submission.enabled;submission.enabled=false;
        float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            var a=Render(camera);Shader.SetGlobalVector("_MavisGrassShadowRange",new Vector4(0,.001f,1000,1));var b=Render(camera);
            var ap=a.GetPixels32();var bp=b.GetPixels32();int changed=0,darker=0;
            for(int i=0;i<ap.Length;i++){int delta=(bp[i].r+bp[i].g+bp[i].b)-(ap[i].r+ap[i].g+ap[i].b);if(Mathf.Abs(delta)>6)changed++;if(delta>6)darker++;}
            Save(a,"grass-shadows-on");Save(b,"grass-shadows-control");Check(changed>100&&darker>100,"native grass shadows visibly darken the receiving scene");
            var result=new{passed=true,changedPixels=changed,darkerPixels=darker};Write("shadow-images",result);return result;
        }
        finally{Shader.SetGlobalVector("_MavisGrassShadowRange",range);Time.timeScale=scale;submission.enabled=enabled;}
    }
    public static object ThreadImages()
    {
        Bind();var r=player.GetComponentsInChildren<MeshRenderer>().First(m=>m.name=="Main quest guiding thread");
        var submission=Manager.GetComponent<GrassRenderSubmission>();bool enabled=submission.enabled;submission.enabled=false;float scale=Time.timeScale;Time.timeScale=0;
        try
        {
            var a=Render(camera);r.enabled=false;var b=Render(camera);var ap=a.GetPixels32();var bp=b.GetPixels32();int changed=0;
            for(int i=0;i<ap.Length;i++)if(Mathf.Abs(ap[i].r-bp[i].r)+Mathf.Abs(ap[i].g-bp[i].g)+Mathf.Abs(ap[i].b-bp[i].b)>12)changed++;
            Save(a,"guidance-on");Save(b,"guidance-control");Check(changed>100,"guidance is visibly distinct from the real grass backdrop");
            var result=new{passed=true,changedPixels=changed};Write("guidance-image-proof",result);return result;
        }
        finally{r.enabled=true;submission.enabled=enabled;Time.timeScale=scale;}
    }
    public static async Task<object> Verify()
    {
        Bind();
        if(!guide)throw new Exception("Setup first");checks.Clear();errors.Clear();Application.logMessageReceived+=Log;
        var state=objectives.Capture();Vector3 position=player.transform.position;Quaternion rotation=player.transform.rotation;
        try
        {
            Check(guide.Visible&&guide.RoutePointCount>2,"main quest guiding thread visible");
            var mesh=player.GetComponentsInChildren<MeshFilter>().First(m=>m.sharedMesh&&m.sharedMesh.name=="Main quest gold-green guiding thread").sharedMesh;
            Check(mesh.vertexCount<=1152&&mesh.triangles.Length/3<=2268,"guidance geometry is bounded");
            var vertices=mesh.vertices;Check(vertices.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)),"finite world-space guiding geometry");
            Check(mesh.colors.Any(c=>c.r>.9f)&&mesh.colors.Any(c=>c.g>c.r*2),"distinct gold and green filaments");
            var oldId=guide.TargetId;Check(objectives.TryGetGuidanceTarget(out _,out var sourceId)&&oldId==sourceId,"visual uses the authoritative objective target");
            var arrivalField=typeof(FirstIslandObjectives).GetField("arrival",Flags);var originalArrival=arrivalField.GetValue(objectives);
            var probe=new GameObject("Inactive arrival guidance probe");probe.SetActive(false);probe.transform.SetParent(stage.transform);
            try
            {
                var arrival=probe.AddComponent<FirstIslandArrival>();arrival.player=player.GetComponent<ThirdPersonPlayerController>();
                var stick=new GameObject("Stick objective probe");stick.transform.SetParent(probe.transform);stick.transform.position=Ground(player.transform.position+player.transform.forward*24);arrival.lostStickPoint=stick.transform;
                typeof(FirstIslandArrival).GetField("<IsAwake>k__BackingField",Flags).SetValue(arrival,true);
                typeof(FirstIslandArrival).GetField("<HasStick>k__BackingField",Flags).SetValue(arrival,false);arrivalField.SetValue(objectives,arrival);
                await Wait(()=>guide.TargetId=="arrival-stick"&&guide.Visible);Check(objectives.TryGetGuidanceTarget(out _,out var stickId)&&stickId=="arrival-stick","recover-stick main objective uses the same guide");
                typeof(FirstIslandArrival).GetField("<IsAwake>k__BackingField",Flags).SetValue(arrival,false);await Frames(20);Check(!guide.Visible,"unconscious arrival hides guidance");
                arrivalField.SetValue(objectives,originalArrival);await Wait(()=>guide.TargetId==oldId&&guide.Visible);Check(true,"guide returns to main boss after arrival objective");
            }
            finally{arrivalField.SetValue(objectives,originalArrival);UnityEngine.Object.DestroyImmediate(probe);}
            var route=(Vector3[])typeof(MainQuestGuidance).GetField("route",Flags).GetValue(guide);int count=guide.RoutePointCount;
            Check(Enumerable.Range(1,count-1).All(i=>Vector3.Distance(route[i-1],route[i])<3),"route has no discontinuous spans");
            bool terrain=route.Take(count).All(p=>Vector3.Distance(p,Ground(p))<.35f);Check(terrain,"route follows actual island ground");
            Vector3 oldEnd=route[count-1];int at=Mathf.Min(10,count-2);Vector3 direction=route[at+1]-route[at-1];direction.y=0;
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="Disposable guidance obstacle";obstacle.transform.SetParent(stage.transform);obstacle.transform.SetPositionAndRotation(route[at]+Vector3.up*4,Quaternion.LookRotation(direction));obstacle.transform.localScale=new Vector3(8,8,1.5f);Physics.SyncTransforms();
            try
            {
                guide.enabled=false;guide.enabled=true;await Wait(()=>guide.Visible&&guide.RoutePointCount>2);
                count=guide.RoutePointCount;
                Write("obstacle-route",new{count,endDelta=Vector3.Distance(route[count-1],oldEnd),maxSpan=Enumerable.Range(1,count-1).Max(i=>Vector3.Distance(route[i-1],route[i]))});
                Check(route.Take(count).All(p=>Vector3.Distance(obstacle.GetComponent<Collider>().ClosestPoint(p+Vector3.up),p+Vector3.up)>.3f),"route avoids a solid obstacle");
                Check(Vector3.Distance(route[count-1],oldEnd)<4&&Enumerable.Range(1,count-1).All(i=>Vector3.Distance(route[i-1],route[i])<3),"obstacle detour reaches the forward goal without vertical jumps");
            }
            finally{UnityEngine.Object.DestroyImmediate(obstacle);Physics.SyncTransforms();guide.enabled=false;guide.enabled=true;await Wait(()=>guide.Visible);}
            Time.timeScale=0;await Frames(20);Check(!guide.Visible,"paused gameplay hides guidance");Time.timeScale=1;await Wait(()=>guide.Visible);
            var complete=new FirstIslandObjectives.Progress{stage=7,combatLesson=4};
            foreach(var kind in new[]{FirstIslandObjectives.BossKind.Capri,FirstIslandObjectives.BossKind.Nailong,FirstIslandObjectives.BossKind.Bombardino})complete.bosses.Add(new FirstIslandObjectives.BossProgress{bossId="qa-complete-"+kind,kind=kind,defeated=true});
            objectives.Restore(complete);await Frames(20);Check(!guide.Visible&&!objectives.TryGetGuidanceTarget(out _,out _),"completed main objectives hide guidance");
            objectives.Restore(state);typeof(FirstIslandObjectives).GetMethod("DiscoverBosses",Flags).Invoke(objectives,null);await Wait(()=>guide.Visible);
            guide.enabled=false;Check(!guide.Visible&&guide.RoutePointCount==0,"disable clears active route and render state");guide.enabled=true;await Wait(()=>guide.Visible);
            Check(player.GetComponents<MainQuestGuidance>().Length==1,"single guide per objective owner");
            Check(new[]{Shader.Find("Mavis/FoliageWind"),Shader.Find("DarkBrine/Quest Guiding Thread")}.All(s=>s&&!ShaderUtil.ShaderHasError(s)),"grass and guidance shaders compile");
            var native=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh&&r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("VerifiedGrassNear")&&r.bounds.SqrDistance(camera.transform.position)<24*24).ToArray();
            Check(native.Length>0&&native.All(r=>r.shadowCastingMode==ShadowCastingMode.TwoSided&&r.receiveShadows),"nearby native grass casts and receives shadows");
            Check(errors.Count==0,"no native runtime or shader errors");
            var result=new{passed=true,checks,errors};Write("acceptance",result);return new{passed=true,checks=checks.Count};
        }
        catch(Exception e){Write("acceptance",new{passed=false,error=e.ToString(),checks,errors});throw;}
        finally{Time.timeScale=1;objectives.Restore(state);typeof(FirstIslandObjectives).GetMethod("DiscoverBosses",Flags).Invoke(objectives,null);player.GetComponent<ThirdPersonPlayerController>().RestoreSavedPose(position,rotation);Application.logMessageReceived-=Log;}
    }
    public static async Task<object> Transitions()
    {
        Bind();checks.Clear();errors.Clear();Application.logMessageReceived+=Log;
        var saved=objectives.Capture();var field=typeof(GameWorldProgress).GetField("changes",BindingFlags.Static|BindingFlags.NonPublic);
        var progress=(IDictionary)field.GetValue(null);var backup=new Dictionary<string,int>();foreach(DictionaryEntry e in progress)backup[(string)e.Key]=(int)e.Value;
        const string scenePath="Assets/Scenes/First Island/Enemies.unity";bool loadedHere=!SceneManager.GetSceneByPath(scenePath).isLoaded;
        try
        {
            if(loadedHere){var operation=SceneManager.LoadSceneAsync(scenePath,LoadSceneMode.Additive);while(!operation.isDone)await Frames(1);}
            foreach(var a in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(a.GetType().Name.EndsWith("AI")||a is BombardinoBoss)a.enabled=false;
            typeof(FirstIslandObjectives).GetMethod("DiscoverBosses",Flags).Invoke(objectives,null);
            var flows=((IEnumerable)typeof(FirstIslandObjectives).GetField("flows",Flags).GetValue(objectives)).Cast<object>().ToArray();
            for(int kind=0;kind<3;kind++)
            {
                int wanted=kind;var targets=flows.Where(f=>(int)((FirstIslandObjectives.BossProgress)f.GetType().GetField("progress",Flags).GetValue(f)).kind==wanted).ToArray();
                Check(targets.Length>0,"real loaded objective kind "+kind);
                var ids=targets.Select(f=>((FirstIslandObjectives.BossProgress)f.GetType().GetField("progress",Flags).GetValue(f)).bossId).ToArray();
                await Wait(()=>objectives.TryGetGuidanceTarget(out _,out var id)&&ids.Contains(id));
                await Wait(()=>ids.Contains(guide.TargetId)&&guide.Visible);
                Check(true,"guide selects next unlocked boss kind "+kind);
                foreach(var f in targets)GameWorldProgress.Record((WorldSaveIdentity)f.GetType().GetField("identity",Flags).GetValue(f),0);
                typeof(FirstIslandObjectives).GetMethod("RefreshBosses",Flags).Invoke(objectives,null);await Frames(2);
            }
            await Frames(20);Check(!objectives.TryGetGuidanceTarget(out _,out _)&&!guide.Visible,"all main bosses completed retires guiding thread");
            Check(errors.Count==0,"no native errors during real objective transitions");Write("objective-transitions",new{passed=true,checks,errors});return new{passed=true,checks=checks.Count};
        }
        catch(Exception e){Write("objective-transitions",new{passed=false,error=e.ToString(),checks,errors});throw;}
        finally
        {
            progress.Clear();foreach(var e in backup)progress.Add(e.Key,e.Value);objectives.Restore(saved);
            if(loadedHere){var operation=SceneManager.UnloadSceneAsync(scenePath);while(operation!=null&&!operation.isDone)await Frames(1);}
            typeof(FirstIslandObjectives).GetMethod("DiscoverBosses",Flags).Invoke(objectives,null);Application.logMessageReceived-=Log;
        }
    }
}
