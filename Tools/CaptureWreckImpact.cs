using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CaptureWreckImpact
{
    public static async Task<object> Capture()
    {
        var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
        while(!loading.isDone)await Task.Delay(30);await Task.Delay(400);
        var s=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var battle=s.GetComponent<Story1WreckBattle>();
        string dir=Path.GetFullPath(".codex/shark-finale-preview/impact-frames");Directory.CreateDirectory(dir);
        var times=new System.Collections.Generic.List<float>();
        s.BeginImpact();float start=Time.time;
        while(battle.CurrentPhase==Story1WreckBattle.Phase.Breaking)
        {
            times.Add(Time.time-start);Shot(s.storyCamera,Path.Combine(dir,(times.Count-1).ToString("D4")+".png"));await Task.Delay(33);
        }
        File.WriteAllText(Path.GetFullPath(".codex/shark-finale-preview/impact-times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(times));
        return new{frames=times.Count,battle.ShipFragmentCount,battle.PlankCount,battle.Effects.ImpactCount,battle.CurrentPhase};
    }
    static void Shot(Camera camera,string path)
    {
        var target=new RenderTexture(720,405,24);var frame=new Texture2D(720,405,TextureFormat.RGB24,false);
        var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,720,405),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);}
    }
}
