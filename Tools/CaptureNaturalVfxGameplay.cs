using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;
public static class CaptureNaturalVfxGameplay
{
    public static async Task<object> Capture()
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!loading.isDone)await Task.Delay(30);await Task.Delay(300);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
            var battle=sequence.GetComponent<Story1WreckBattle>();sequence.BeginImpact();
            for(int i=0;i<250 && battle.ShipFragmentCount==0;i++)await Task.Delay(20);
            if(battle.ShipFragmentCount!=202)throw new Exception("Missing actual ship fragments.");
            await Task.Delay(100);
            string folder=Path.GetFullPath(".codex/vfx/visuals");Directory.CreateDirectory(folder);
            Shot(sequence.storyCamera,Path.Combine(folder,"ship-impact.png"));
            await Task.Delay(350);
            Shot(sequence.storyCamera,Path.Combine(folder,"ship-impact-spray.png"));
            await Task.Delay(450);
            Shot(sequence.storyCamera,Path.Combine(folder,"ship-impact-scatter.png"));
            return new{battle.ShipFragmentCount,battle.Effects.ImpactCount};
        }
        finally
        {
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/Main Menu/MainMenu.unity");while(!loading.isDone)await Task.Delay(30);
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
    static void Shot(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);var frame=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var old=camera.targetTexture;var active=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            frame.ReadPixels(new Rect(0,0,1280,720),0,0);frame.Apply();File.WriteAllBytes(path,frame.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=old;RenderTexture.active=active;target.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }
    }
}
