using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;

/// <summary>Capture actual sailing foam, with campaign saves excluded and restored.</summary>
public static class CaptureShipFoam
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static async Task<object> Capture(string label="after",bool motion=false)
    {
        if(!Application.isPlaying)throw new Exception("Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        try
        {
            GameSaveManager.CancelPendingContinue();
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");while(!load.isDone)await Task.Delay(30);await Task.Delay(350);
            var seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();seq.enabled=false;
            var ship=seq.ship;var cam=seq.storyCamera;var ocean=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            var follow=cam.GetComponent<ShipFollowCamera>();bool wasFollowing=follow.enabled;follow.enabled=false;
            if(seq.sahur.GetComponent<GameSaveExcluded>()==null)seq.sahur.gameObject.AddComponent<GameSaveExcluded>();
            string dir=Path.GetFullPath(".codex/ship-foam-20261007/"+label);Directory.CreateDirectory(dir);
            await Task.Delay(9500);
            var wake=ship.GetComponent<ShipWakeEffects>();
            Vector3 heading=Vector3.ProjectOnPlane(ship.transform.right,Vector3.up).normalized,side=Vector3.Cross(Vector3.up,heading);
            Vector3 savedAt=cam.transform.position;Quaternion savedRotation=cam.transform.rotation;float fov=cam.fieldOfView;
            void View(Vector3 offset){Vector3 center=ship.transform.position;center.y=ocean.oceanHeight+4;cam.transform.SetPositionAndRotation(center+offset,Quaternion.LookRotation(-offset));cam.fieldOfView=55;}
            View(-heading*120+side*70+Vector3.up*80);Shot(cam,Path.Combine(dir,"sailing.png"));
            View(side*110+Vector3.up*105);Shot(cam,Path.Combine(dir,"side.png"));
            View(-heading*40+Vector3.up*175);Shot(cam,Path.Combine(dir,"overhead.png"));
            var material=(Material)typeof(OceanWorld).GetField("generatedMaterial",Private).GetValue(ocean);
            var errors=ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray();
            if(errors.Length>0)throw new Exception(string.Join(";",errors));
            var times=new System.Collections.Generic.List<float>();
            if(motion)
            {
                float began=Time.time;while(Time.time-began<6)
                {
                    View(-heading*105+side*85+Vector3.up*70);times.Add(Time.time-began);Shot(cam,Path.Combine(dir,(times.Count-1).ToString("D4")+".png"));await Task.Delay(65);
                }
                File.WriteAllText(Path.Combine(dir,"times.json"),Newtonsoft.Json.JsonConvert.SerializeObject(times));
            }
            var oldTrail=(Vector4[])typeof(ShipWakeEffects).GetField("trail",Private).GetValue(wake);var sample=oldTrail[0];
            var data=material.GetVector("_WakeDirection");Time.timeScale=0;await Task.Delay(180);
            bool frozen=oldTrail[0]==sample&&material.GetVector("_WakeDirection")==data;Time.timeScale=1;
            ship.enabled=false;await Task.Delay(11000);
            bool expired=material.GetInt("_WakeCount")==0&&material.GetVector("_WakeDirection").z<.01f;
            ship.enabled=true;cam.transform.SetPositionAndRotation(savedAt,savedRotation);cam.fieldOfView=fov;follow.enabled=wasFollowing;
            return new{label,speed=ship.CurrentSpeed,pauseFreezesFoam=frozen,stoppedWakeExpires=expired,shaderErrors=errors.Length,motionFrames=times.Count,preview=dir};
        }
        finally
        {
            Time.timeScale=1;GameSaveManager.CancelPendingContinue();var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null&&actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
    static void Shot(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
    }
}
