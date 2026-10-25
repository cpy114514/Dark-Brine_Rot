using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Mavis;

public static class CaptureOceanV244
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        if(UnityEngine.Object.FindAnyObjectByType<OceanMovieV244>())throw new Exception("Already recording");
        string folder=".codex/ocean-buoyancy-20261023/"+label;
        if(Directory.Exists(folder))throw new Exception("Retain prior recording");Directory.CreateDirectory(folder);
        var go=new GameObject("Temporary ocean review v244");UnityEngine.Object.DontDestroyOnLoad(go);
        var movie=go.AddComponent<OceanMovieV244>();movie.folder=folder;
        EditorApplication.isPaused=false;return new{folder};
    }
}
public sealed class OceanMovieV244:MonoBehaviour
{
    public string folder;Camera view;RenderTexture rt;Texture2D pixels;
    bool ready;int frame,rate,sync;float delta;double began;
    async void Start()
    {
        rate=Application.targetFrameRate;sync=QualitySettings.vSyncCount;delta=Time.captureDeltaTime;
        try
        {
            foreach(var player in UnityEngine.Object.FindObjectsByType<ThirdPersonPlayerController>(FindObjectsSortMode.None))
                if(!player.GetComponent<GameSaveExcluded>())player.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())if(root!=gameObject)root.SetActive(false);
            foreach(string name in new[]{"Environment","Lighting"})
            {
                var scene=SceneManager.GetSceneByPath("Assets/Scenes/First Island/"+name+".unity");
                if(!scene.isLoaded){var load=SceneManager.LoadSceneAsync("Assets/Scenes/First Island/"+name+".unity",LoadSceneMode.Additive);while(!load.isDone)await Task.Delay(30);}
            }
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){camera.enabled=false;camera.tag="Untagged";}
            var go=new GameObject("Ocean review camera v244");go.transform.SetParent(transform);view=go.AddComponent<Camera>();
            view.tag="MainCamera";view.nearClipPlane=.1f;view.farClipPlane=3020;view.fieldOfView=60;view.aspect=16f/9;
            var data=go.AddComponent<UniversalAdditionalCameraData>();data.requiresDepthTexture=true;data.requiresColorTexture=true;data.renderPostProcessing=true;
            rt=new RenderTexture(960,540,24,RenderTextureFormat.Default);rt.Create();view.targetTexture=rt;
            pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            Time.timeScale=1;Time.captureDeltaTime=1f/24;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            Pose(0);await Task.Delay(2000);
            RenderPipelineManager.endCameraRendering+=Rendered;ready=true;began=Time.realtimeSinceStartupAsDouble;
        }
        catch(Exception e){File.WriteAllText(folder+"/error.txt",e.ToString());Finish();}
    }
    void Pose(int shot)
    {
        Vector3 p=shot==0?new Vector3(480.576f,6,466.249f):shot==1?new Vector3(492.338f,14,444.463f):new Vector3(900,5,900);
        Quaternion q=shot==0?Quaternion.Euler(10.574f,285,0):shot==1?Quaternion.Euler(15.407f,248.807f,0):Quaternion.LookRotation(new Vector3(1,-.08f,1));
        view.transform.SetPositionAndRotation(p,q);
    }
    void LateUpdate(){if(ready)Pose(Mathf.Min(2,frame/(24*7)));}
    void Rendered(ScriptableRenderContext context,Camera camera)
    {
        if(!ready||camera!=view)return;
        var active=RenderTexture.active;RenderTexture.active=rt;
        pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply(false);RenderTexture.active=active;
        File.WriteAllBytes(folder+"/frame-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());
        frame++;
        if(frame%24==0)File.WriteAllText(folder+"/progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,time=Time.time,wall=Time.realtimeSinceStartupAsDouble-began}));
        if(frame>=24*21)Finish();
    }
    void Finish()
    {
        ready=false;RenderPipelineManager.endCameraRendering-=Rendered;
        Time.captureDeltaTime=delta;Application.targetFrameRate=rate;QualitySettings.vSyncCount=sync;
        if(view){view.targetTexture=null;view.enabled=true;}
        if(rt){rt.Release();DestroyImmediate(rt);}if(pixels)DestroyImmediate(pixels);
        File.WriteAllText(folder+"/complete.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frame,fps=24,duration=frame/24f,time=Time.time,scope="Existing Editor native URP camera. Playback is silent; temporary fixtures only."}));
    }
    void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;if(ready)Finish();}
}
