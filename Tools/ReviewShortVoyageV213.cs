using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ReviewShortVoyageV213
{
    public static object Start(string label="before",int fps=10)
    {
        if(!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<ShortVoyageObserverV213>()!=null)throw new Exception("One live review required");
        var go=new GameObject("Opening voyage duration review");UnityEngine.Object.DontDestroyOnLoad(go);
        var observer=go.AddComponent<ShortVoyageObserverV213>();observer.label=label;observer.fps=fps;
        return new{started=true,label,fps};
    }
}
[DefaultExecutionOrder(32000)]
public class ShortVoyageObserverV213:MonoBehaviour
{
    public string label;public int fps;
    Story1SharkCollisionSequence sequence;Story1WreckBattle battle;
    string folder;bool ready,finished;int frame,images;double started;
    float attackAt=-1,patrolAt=-1;readonly List<object> rows=new List<object>();
    RenderTexture target;Texture2D pixels;
    IEnumerator Start()
    {
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/short-voyage-20261017/"+label;Directory.CreateDirectory(folder);
        Time.captureFramerate=fps;Mavis.GameSaveManager.CancelPendingContinue();
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");yield return null;
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();
        ready=true;started=Time.realtimeSinceStartupAsDouble;
        target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);
    }
    void LateUpdate()
    {
        if(!ready||finished||Time.deltaTime<=0)return;
        frame++;
        if(patrolAt<0&&!sequence.IsPatrolling)patrolAt=sequence.VoyageTime;
        if(attackAt<0&&sequence.HasImpacted)attackAt=sequence.VoyageTime;
        rows.Add(new{frame,time=sequence.VoyageTime,sequence.PatrolDuration,sequence.PatrolRadius,sequence.IsPatrolling,sequence.IsVoyageBreaching,sequence.CurrentSwimSpeed,phase=battle.CurrentPhase.ToString(),tailGap=battle.ImpactTailGap});
        if(!label.Contains("before")||frame%5==0)Capture();
        if(frame%fps==0)File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows.Last()));
        if(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting || Time.realtimeSinceStartupAsDouble-started>240 || sequence.VoyageTime>180)
        {
            finished=true;
            File.WriteAllText(folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{attackAt,patrolAt,sequence.PatrolDuration,sequence.PatrolRadius,sequence.VoyageTime,images,fps,captureFps=label.Contains("before")?fps/5f:fps,phase=battle.CurrentPhase.ToString(),battle.ImpactTailGap,rows},Newtonsoft.Json.Formatting.Indented));
            Time.captureFramerate=0;Destroy(gameObject);
        }
    }
    void Capture()
    {
        var camera=sequence.storyCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
        var overlays=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();
        try
        {
            foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;}
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);
            File.WriteAllBytes(folder+"/game-"+images.ToString("D4")+".png",pixels.EncodeToPNG());images++;
        }
        finally
        {
            camera.targetTexture=previous;RenderTexture.active=active;
            for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];}
        }
    }
    void OnDestroy(){Time.captureFramerate=0;if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
