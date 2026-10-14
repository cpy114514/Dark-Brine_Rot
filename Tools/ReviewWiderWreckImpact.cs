using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviewWiderWreckImpact
{
    public static object Start()
    {
        if(!Application.isPlaying)throw new Exception("Original Editor Play Mode required");
        if(UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(x=>x.GetType().Name=="WiderWreckObserver"))throw new Exception("Duplicate observer");
        var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        if(sequence==null || sequence.HasImpacted)throw new Exception("Need sailing before fracture");
        var go=new GameObject("Diagnostic wreck power comparison");UnityEngine.Object.DontDestroyOnLoad(go);
        var observer=go.AddComponent<WiderWreckObserver>();observer.Attach(sequence);Time.captureFramerate=30;
        return new{started=true,originalEditorOnly=true,powers=new[]{1f,1.35f},diagnosticSeed=20261013,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class WiderWreckObserver:MonoBehaviour
{
    const string Folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/wider-wreck-impact-20261013/";
    Story1SharkCollisionSequence sequence;Story1WreckBattle battle;
    Camera topCamera;RenderTexture target;Texture2D pixels;
    readonly List<object> runs=new List<object>(),samples=new List<object>();
    readonly Dictionary<Story1WreckFloatBody,Vector3> launchPositions=new Dictionary<Story1WreckFloatBody,Vector3>();
    readonly HashSet<int> captures=new HashSet<int>();
    int run,frames,initialCount;float sinceRelease=-1,phaseAge,lastFilmCapture=-1;string phase;
    bool loading,finished,landed;AsyncOperation load;float watchdog;double nextSearch;
    public void Attach(Story1SharkCollisionSequence owner)
    {
        sequence=owner;battle=owner.GetComponent<Story1WreckBattle>();
        battle.shipBreakPower=run==0 ? 1 : 1.35f;battle.DiagnosticScatterSeed=20261013;
        Directory.CreateDirectory(Folder+(run==0 ? "baseline/" : "stronger/"));
        samples.Clear();launchPositions.Clear();captures.Clear();frames=0;sinceRelease=-1;phaseAge=0;phase=null;lastFilmCapture=-1;landed=false;
        // Diagnostic timing only: skip the long sailing patrol, retaining the
        // native tail strike, original ship geometry and physical release.
        owner.BeginImpact();
    }
    void LateUpdate()
    {
        if(finished)return;
        try
        {
            watchdog+=Time.deltaTime;if(watchdog>260){Finish("Diagnostic timeout");return;}
            if(loading)
            {
                if(load!=null&&!load.isDone)return;
                if(Time.realtimeSinceStartupAsDouble<nextSearch)return;
                nextSearch=Time.realtimeSinceStartupAsDouble+.2;
                var next=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
                if(next==null)return;Attach(next);loading=false;return;
            }
            if(sequence==null || battle==null){Finish("Lost encounter");return;}
            if(target==null)
            {
                target=new RenderTexture(960,540,24);target.Create();pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
                var camObject=new GameObject("Diagnostic top view");camObject.transform.SetParent(transform,false);topCamera=camObject.AddComponent<Camera>();topCamera.enabled=false;
            }
            string current=battle.CurrentPhase.ToString();if(current!=phase){phase=current;phaseAge=0;}phaseAge+=Time.deltaTime;
            var bodies=UnityEngine.Object.FindObjectsByType<Story1WreckFloatBody>(FindObjectsSortMode.None).Where(x=>x.enabled&&x.Body!=null&&x.gameObject.activeInHierarchy).ToArray();
            if(bodies.Length>0 && sinceRelease<0)
            {
                sinceRelease=0;initialCount=bodies.Length;
                foreach(var body in bodies)launchPositions[body]=body.Body.position;
                Sample(bodies,"release");
            }
            if(sinceRelease>=0)
            {
                sinceRelease+=Time.deltaTime;
                if(sinceRelease<6.5f && sinceRelease-lastFilmCapture>=1f/15-.001f){lastFilmCapture=sinceRelease;Capture(bodies,"game-"+(frames++).ToString("D4"),false);}
                foreach(int seconds in new[]{1,3,6,12})
                    if(sinceRelease>=seconds&&captures.Add(seconds)){Sample(bodies,seconds+" seconds");Capture(bodies,"layout-"+seconds,true);}
            }
            if(current=="Fighting" && phaseAge>.2f && !landed)
            {
                landed=true;var rider=sequence.sahur.GetComponent<Story1WreckRider>();
                samples.Add(new{label="control handback",support=rider!=null&&rider.Support()!=null,playerEnabled=sequence.sahur.enabled,locked=sequence.sahur.ExternalControlLock,battle.ImpactTailGap});
            }
            if(sinceRelease>12.1f)
            {
                string mode=run==0 ? "baseline" : "stronger";
                var result=new{mode,power=battle.shipBreakPower,initialCount,battle.ShipFragmentCount,battle.BrokenBoardCount,battle.ImpactTailGap,frames,landed,samples=samples.ToArray()};
                runs.Add(result);File.WriteAllText(Folder+mode+"/review.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
                if(run++==0){loading=true;Mavis.GameSaveManager.CancelPendingContinue();load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity",LoadSceneMode.Single);}
                else Finish(null);
            }
        }
        catch(Exception error){Finish(error.ToString());}
    }
    void Sample(Story1WreckFloatBody[] bodies,string label)
    {
        Vector3 mean=Vector3.zero;foreach(var b in bodies)mean+=b.Body.position;mean/=Mathf.Max(1,bodies.Length);
        var radii=bodies.Select(b=>Vector3.ProjectOnPlane(b.Body.position-mean,Vector3.up).magnitude).OrderBy(x=>x).ToArray();
        var boards=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).Where(x=>!x.IsBroken).ToArray();
        float nearest=0;int pairs=0;
        foreach(var a in boards)
        {
            float best=float.PositiveInfinity;foreach(var b in boards)if(a!=b){float d=Vector3.ProjectOnPlane(a.transform.position-b.transform.position,Vector3.up).magnitude;best=Mathf.Min(best,d);if(d<10)pairs++;}
            if(float.IsFinite(best))nearest+=best;
        }
        var distances=bodies.Where(b=>launchPositions.ContainsKey(b)).Select(b=>Vector3.ProjectOnPlane(b.Body.position-launchPositions[b],Vector3.up).magnitude).OrderBy(x=>x).ToArray();
        if(bodies.Any(b=>!b.Body.useGravity||b.Body.isKinematic||!float.IsFinite(b.Body.position.x)))throw new Exception("Fragment gravity/finite pose invalid");
        var speeds=bodies.Select(b=>Vector3.ProjectOnPlane(b.Body.linearVelocity,Vector3.up).magnitude).OrderBy(x=>x).ToArray();
        samples.Add(new{label,age=sinceRelease,count=bodies.Length,boardCount=boards.Length,usableBoards=boards.Count(b=>b.IsBoardable&&Mathf.Abs(b.transform.up.y)>.82f),radius90=radii.Length>0?radii[(int)((radii.Length-1)*.9f)]:0,meanNearestBoard=nearest/Mathf.Max(1,boards.Length),boardPairsUnder10m=pairs/2,medianHorizontalTravel=distances.Length>0?distances[distances.Length/2]:0,medianHorizontalSpeed=speeds.Length>0?speeds[speeds.Length/2]:0,gravityFree=true});
    }
    void Capture(Story1WreckFloatBody[] bodies,string file,bool overhead)
    {
        var camera=overhead?topCamera:sequence.storyCamera;
        if(overhead)
        {
            camera.CopyFrom(sequence.storyCamera);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=100;
            Vector3 center=Vector3.zero;foreach(var b in bodies)center+=b.Body.position;center/=Mathf.Max(1,bodies.Length);center.y=0;
            camera.transform.SetPositionAndRotation(center+Vector3.up*210,Quaternion.LookRotation(Vector3.down,Vector3.forward));
        }
        var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);pixels.Apply();File.WriteAllBytes(Folder+(run==0?"baseline/":"stronger/")+file+".png",pixels.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;}
    }
    void Finish(string error)
    {
        if(finished)return;finished=true;
        File.WriteAllText(Folder+"comparison.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{error,diagnosticSeed=20261013,offline=true,performanceEvidence=false,naturalRelease=true,woodPlaced=false,runs},Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=0;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;
    }
    void OnDestroy(){Time.captureFramerate=0;if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
