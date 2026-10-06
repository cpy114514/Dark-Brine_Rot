using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyShipImpact
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<object> CaptureNatural()
    {
        Check(Application.isPlaying,"Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        float oldScale=Time.timeScale;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!load.isDone)await Task.Delay(40);
            await Task.Delay(300);
            var seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=seq.GetComponent<Story1WreckBattle>();
            Time.timeScale=3;
            float deadline=Time.realtimeSinceStartup+22;
            while(seq.VoyageTime<27 && !seq.HasImpacted && Time.realtimeSinceStartup<deadline)await Task.Delay(90);
            Time.timeScale=1;
            float cruisingExposure=Exposure(seq.swimmer,seq.swimmer.GetComponent<TralaleroSwimAnimator>(),UnityEngine.Object.FindFirstObjectByType<OceanWorld>());
            Check(cruisingExposure<.28f,"Cruising shark exposes too much of its height: "+cruisingExposure);
            while(!seq.HasImpacted && Time.realtimeSinceStartup<deadline)await Task.Delay(20);
            Check(seq.HasImpacted,"Natural shark contact did not begin.");
            string dir=Path.GetFullPath(".codex/ship-tail-strike/frames");Directory.CreateDirectory(dir);
            var times=new List<float>();float start=Time.time;
            Vector3 fullScale=seq.swimmer.localScale;
            var sources=new Dictionary<int,(Vector3 at,Vector3 size)>();
            float launchDistance=0;bool heldScale=false;
            while(battle.CurrentPhase==Story1WreckBattle.Phase.Breaking)
            {
                float age=Time.time-start;
                times.Add(age);Shot(seq.storyCamera,Path.Combine(dir,(times.Count-1).ToString("D4")+".png"));
                foreach(var p in UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None))
                {
                    int id=p.GetInstanceID();
                    if(!sources.ContainsKey(id))
                    {
                        Check(Vector3.Distance(p.transform.position,p.ShipSourcePosition)<8,"Physics fragment did not start at its original ship position.");
                        sources[id]=(p.transform.position,p.transform.localScale);
                    }
                    else
                    {
                        if(age<3.2f)launchDistance=Mathf.Max(launchDistance,Vector3.Distance(sources[id].at,p.transform.position));
                        Check(Vector3.Distance(sources[id].size,p.transform.localScale)<.01f,"Ship geometry changed size during free fall.");
                        var body=p.GetComponent<Rigidbody>();Check(body!=null && body.useGravity && !body.isKinematic,"Fragment is not falling under physics.");
                        heldScale=true;
                    }
                }
                if(age<2.5f)Check(Vector3.Distance(seq.swimmer.localScale,fullScale)<.001f,"Shark shrank before finishing the tail strike.");
                Check(age<9,"Ship fracture stalled.");
                await Task.Delay(25);
            }
            Check(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Impact did not return controls.");
            Check(battle.ImpactTailGap<.05f,"Visible shark tail missed the hull.");
            Check(battle.ImpactNoseGap>10,"The nose struck the boat instead of the tail.");
            Check(battle.MaximumHullKickDegrees>10,"Hull did not kick under impact.");
            Check(launchDistance>8 && heldScale,"Directional debris launch is too weak.");
            Check(battle.ShipFragmentCount==202 && battle.PlankCount==44,"Real ship fragments or playable decks missing.");
            Check(seq.sahur.enabled && !seq.sahur.ExternalControlLock,"Player control stayed locked.");
            await Task.Delay(250);
            float fightingExposure=Exposure(seq.swimmer,seq.swimmer.GetComponent<TralaleroSwimAnimator>(),UnityEngine.Object.FindFirstObjectByType<OceanWorld>());
            Check(fightingExposure<.28f,"Normal combat exposes too much of the shark: "+fightingExposure);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(dir),"times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(times));
            var result=new{naturalApproach=true,tailGap=battle.ImpactTailGap,noseClearance=battle.ImpactNoseGap,hullKickDegrees=battle.MaximumHullKickDegrees,
                cruisingExposure,fightingExposure,launchDistance,fullShipScaleDuringLaunch=heldScale,fullSharkScaleThroughTailStrike=true,realFragments=202,playableBoards=44,
                returnedControls=true,rigidbodyGravity=true,fullSizeThroughout=true,duration=Time.time-start,frames=times.Count};
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(dir),"verification.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            Time.timeScale=oldScale;GameSaveManager.CancelPendingContinue();
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null && actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
    static float Exposure(Transform shark,TralaleroSwimAnimator animator,OceanWorld ocean)
    {
        Bounds bounds=animator.RootMeshBounds;float bottom=float.PositiveInfinity,top=float.NegativeInfinity;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            float height=shark.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))).y;
            bottom=Mathf.Min(bottom,height);top=Mathf.Max(top,height);
        }
        float water=ocean.SampleSurfaceHeight(shark.TransformPoint(bounds.center),Time.time);
        return Mathf.Clamp01((top-water)/(top-bottom));
    }
    static void Shot(Camera camera,string path)
    {
        var target=new RenderTexture(960,540,24);var frame=new Texture2D(960,540,TextureFormat.RGB24,false);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,960,540),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
}
