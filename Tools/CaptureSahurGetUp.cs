using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CaptureSahurGetUp
{
    public static async Task<object> Capture()
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode.");
        GameSaveManager.CancelPendingContinue();
        var load=SceneManager.LoadSceneAsync("Assets/Scenes/First Island/Main.unity",LoadSceneMode.Single);
        while(!load.isDone)await Task.Delay(40);
        FirstIslandArrival arrival=null;
        for(int k=0;k<300&&arrival==null;k++){arrival=UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();await Task.Delay(30);}
        if(arrival==null)throw new Exception("Arrival did not load.");
        var target=new RenderTexture(800,450,24);var frame=new Texture2D(800,450,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;var times=new List<float>();string dir=Path.GetFullPath(".codex/sahur-get-up-preview");Directory.CreateDirectory(dir);
        float start=Time.realtimeSinceStartup;bool lyingCaptured=false;float nextDriftShot=0;
        try{
            void Shot(){
                var camera=Camera.main;var previousTarget=camera.targetTexture;
                try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,800,450),0,0);frame.Apply();
                    File.WriteAllBytes(Path.Combine(dir,times.Count.ToString("D3")+".png"),frame.EncodeToPNG());times.Add(Time.time);
                }finally{camera.targetTexture=previousTarget;RenderTexture.active=previous;}
            }
            while(!arrival.IsAwake&&Time.realtimeSinceStartup-start<35){
                await Task.Delay(20);
                if(!arrival.HasLanded && Vector3.ProjectOnPlane(arrival.player.transform.position-arrival.shore.position,Vector3.up).magnitude<22 && Time.time>=nextDriftShot){Shot();nextDriftShot=Time.time+.15f;}
                else if(arrival.HasLanded&&!arrival.IsGettingUp&&!lyingCaptured){Shot();lyingCaptured=true;}
                else if(arrival.IsGettingUp)Shot();
            }
            await Task.Delay(100);Shot();
            File.WriteAllText(Path.Combine(dir,"times.json"),"["+string.Join(",",times.ConvertAll(t=>t.ToString(System.Globalization.CultureInfo.InvariantCulture)))+"]");
            return new{frames=times.Count,directory=dir,faceUp=lyingCaptured,visibleGetUp=arrival.IsAwake};
        }finally{
            if(arrival.player.GetComponent<GameSaveExcluded>()==null)arrival.player.gameObject.AddComponent<GameSaveExcluded>();
            RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }
    }
}
