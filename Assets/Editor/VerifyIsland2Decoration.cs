using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mavis;

// Isolated runtime visual/traversal verification. Never writes the player's campaign.
[InitializeOnLoad]
public static class VerifyIsland2Decoration
{
    const string Folder=".codex/island2-decoration/playtest/";
    const string Flag="Island2Decoration.Verify";
    static Keyboard keyboard,oldKeyboard;static bool walk;static Camera review;static readonly List<string> errors=new List<string>();
    static VerifyIsland2Decoration(){EditorApplication.playModeStateChanged+=Changed;}
    static void Write(string n,object o){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+n,Newtonsoft.Json.JsonConvert.SerializeObject(o,Newtonsoft.Json.Formatting.Indented));}
    public static object Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit mode scene required");
        var dressing=GameObject.Find("Island 2 - Environment Decoration");
        var bad=dressing.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Where(m=>!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader"||ShaderUtil.ShaderHasError(m.shader)).ToArray();
        if(bad.Length>0)throw new Exception("Resolve decoration materials before Play: "+string.Join(",",bad.Select(m=>m?m.name:"null")));
        Write("saves-before.json",Directory.GetFiles(Application.persistentDataPath,"dark-brine-rot-save*.json*").ToDictionary(p=>p,p=>Convert.ToBase64String(File.ReadAllBytes(p))));
        SessionState.SetBool(Flag,true);EditorApplication.EnterPlaymode();return "Isolated decoration playtest queued";
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Flag,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,Path.GetFullPath(Folder+"isolated-save.json"));GameSaveManager.BeginEditorTestSession();EditorApplication.delayCall+=Run;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);var before=Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Folder+"saves-before.json"));Write("preservation.json",new{formalSavesUnchanged=before.All(p=>File.Exists(p.Key)&&Convert.ToBase64String(File.ReadAllBytes(p.Key))==p.Value),stopped=true,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty});}
    }
    static async Task Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end){if(!EditorApplication.isPlaying)throw new Exception("Play interrupted");await Task.Delay(20);}}
    static void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(message);}
    static void Inject(){if(keyboard!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();Cursor.lockState=CursorLockMode.Locked;InputState.Change(keyboard,new KeyboardState(walk?new[]{Key.W}:Array.Empty<Key>()));}}
    static Vector3 Floor(Collider g,Vector2 p){if(!g.Raycast(new Ray(new Vector3(p.x,g.bounds.max.y+10,p.y),Vector3.down),out var h,g.bounds.size.y+20))throw new Exception("No ground");return h.point;}
    static void Shot(string name)
    {
        var rt=new RenderTexture(1600,1000,24);var prev=RenderTexture.active;var t=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try{RenderPipeline.SubmitRenderRequest(review,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1600,1000),0,0);t.Apply();File.WriteAllBytes(Folder+name+".png",t.EncodeToPNG());}
        finally{RenderTexture.active=prev;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(t);}
    }
    static async Task<object> Sample(GameObject decoration,bool on)
    {
        decoration.SetActive(on);await Wait(.35f);int frame=Time.frameCount;var batch=new List<int>();var tri=new List<int>();var times=new List<float>();
        while(times.Count<35){await Task.Delay(5);if(Time.frameCount==frame)continue;frame=Time.frameCount;times.Add(Time.unscaledDeltaTime*1000);batch.Add(UnityEditor.UnityStats.batches);tri.Add(UnityEditor.UnityStats.triangles);}
        times.Sort();return new{decoration=on,frames=times.Count,medianEditorFrameMs=times[times.Count/2],averageBatches=batch.Average(),averageTriangles=tri.Average()};
    }
    static async void Run()
    {
        var traversal=new List<object>();var perf=new List<object>();bool passed=false;string failure=null;errors.Clear();Application.logMessageReceived+=Log;
        try{
            var op=SceneManager.LoadSceneAsync("Assets/Scenes/Second Island/Second Island.unity");while(!op.isDone)await Wait(.05f);await Wait(1.5f);
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.Destroy(opening.gameObject);await Wait(.2f);
            var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();var ground=player.GetComponent<MarshmallowIslandExperience>().surface.GetComponent<Collider>();
            var root=GameObject.Find("Island 2 - Environment Decoration");if(!root)throw new Exception("Decoration not loaded in Play");
            oldKeyboard=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Inject;Time.timeScale=1;
            // Real player's W input crosses three different shallow sections of the river.
            foreach(var crossing in new[]{(a:new Vector2(14,1926),b:new Vector2(28,1913)),(a:new Vector2(-36,1861),b:new Vector2(-16,1852)),(a:new Vector2(-66,1812),b:new Vector2(-55,1796))}){
                var heading=(Floor(ground,crossing.b)-Floor(ground,crossing.a));heading.y=0;
                player.ExternalControlLock=false;player.ExternalMovementLock=false;player.RestoreSavedPose(Floor(ground,crossing.a)+Vector3.up*(-player.LowestFootWorldOffset+.06f),Quaternion.LookRotation(heading));
                await Wait(.25f);var start=player.transform.position;walk=true;await Wait(2.5f);walk=false;await Wait(.12f);var end=player.transform.position;float progress=Vector3.Dot(end-start,heading.normalized);
                bool okay=progress>9&&float.IsFinite(end.y)&&Mathf.Abs(end.y-Floor(ground,new Vector2(end.x,end.z)).y)<8;
                traversal.Add(new{start=start.ToString(),end=end.ToString(),metresForward=progress,passed=okay});if(!okay)throw new Exception("Ford traversal blocked: "+progress);
            }
            InputSystem.onBeforeUpdate-=Inject;InputSystem.RemoveDevice(keyboard);keyboard=null;if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
            player.ExternalMovementLock=true;var original=Camera.main;original.enabled=false;review=new GameObject("Decoration verification camera").AddComponent<Camera>();review.CopyFrom(original);review.enabled=true;review.fieldOfView=58;review.farClipPlane=2400;review.nearClipPlane=.3f;
            var manager=Mavis.VisualStyles.VisualStyleManager.Instance;manager.Select("realistic",false);
            var views=new[]{(n:"river",p:new Vector2(-39,1881),h:5f,t:new Vector2(-24,1857)),(n:"spring",p:new Vector2(23,1978),h:7f,t:new Vector2(61,1980)),(n:"wreck",p:new Vector2(-227,1945),h:5f,t:new Vector2(-246,1970)),(n:"arrival",p:new Vector2(-142,1630),h:8f,t:new Vector2(-70,1715)),(n:"cottage",p:new Vector2(135,1900),h:18f,t:new Vector2(70,1940)),(n:"cloud-coast",p:new Vector2(990,1600),h:8f,t:new Vector2(1015,1610))};
            foreach(var view in views){review.transform.position=Floor(ground,view.p)+Vector3.up*view.h;review.transform.LookAt(Floor(ground,view.t)+Vector3.up*2);await Wait(.15f);Shot(view.n);
                if(view.n=="river"||view.n=="arrival"||view.n=="cloud-coast"){var off=await Sample(root,false);var on=await Sample(root,true);perf.Add(new{view=view.n,without=off,with=on});}
                if(view.n=="river"){Shot("river-flow-a");for(int i=0;i<16;i++){Shot("flow-frame-"+i.ToString("00"));await Wait(.14f);}Shot("river-flow-b");manager.Select("van-gogh",false);await Wait(.15f);Shot("river-alternate-style");manager.Select("realistic",false);}}
            root.SetActive(true);manager.RestorePreference();passed=errors.Count==0;if(!passed)failure="Runtime errors captured";
        }catch(Exception e){failure=e.ToString();}
        finally{walk=false;InputSystem.onBeforeUpdate-=Inject;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(review)UnityEngine.Object.Destroy(review.gameObject);Application.logMessageReceived-=Log;Write("result.json",new{passed,failure,traversal,performance=perf,errors});EditorApplication.ExitPlaymode();}
    }
}
