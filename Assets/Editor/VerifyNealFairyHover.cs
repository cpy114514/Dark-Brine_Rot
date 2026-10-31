using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyNealFairyHover
{
    const string Key="VerifyNealFairyHover";
    const string Folder="Temp/NealFairyHoverReview";
    public static string Status="idle";
    public static object Summary;
    static VerifyNealFairyHover()=>EditorApplication.playModeStateChanged+=OnMode;
    public static void Arm(){SessionState.SetBool(Key,true);Status="armed";}
    public static void Start(){_ = Run();}
    static void OnMode(PlayModeStateChange mode)
    {
        if(mode!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);_ = Run();
    }
    static void Capture(Camera camera,NealFairyHover flight,string name,bool side)
    {
        var focus=flight.visual.position+Vector3.up*Mathf.Abs(flight.transform.lossyScale.y)*.55f;
        Vector3 dir=side?flight.transform.right:flight.transform.forward;
        camera.transform.position=focus+dir*8+Vector3.up*.45f;camera.transform.LookAt(focus);
        var rt=RenderTexture.GetTemporary(1200,1000,24);var prev=RenderTexture.active;
        var texture=new Texture2D(1200,1000,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,1200,1000),0,0);texture.Apply();
            File.WriteAllBytes(Folder+"/"+name+".png",texture.EncodeToPNG());
        }
        finally{camera.targetTexture=null;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(texture);}
    }
    static async Task Run()
    {
        Directory.CreateDirectory(Folder);GameObject cameraObject=null;
        try
        {
            Status="playing";Summary=null;
            var fairy=UnityEngine.Object.FindObjectsByType<NealFairyHover>(FindObjectsSortMode.None).FirstOrDefault(f=>f.gameObject.scene.name=="Second Island");
            if(!fairy || !fairy.visual || !fairy.leftWing || !fairy.rightWing)throw new Exception("Fairy rig is not present in the island");
            cameraObject=new GameObject("Temporary fairy flight review camera");var camera=cameraObject.AddComponent<Camera>();
            camera.CopyFrom(Camera.main);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=2.3f;
            camera.nearClipPlane=.05f;camera.farClipPlane=100;
            var data=new List<object>();float start=Time.time;float minAngle=999,maxAngle=0,minY=999,maxY=-999,maxAsymmetry=0;
            int beats=0;float lastPhase=0;bool openShot=false,foldShot=false;float oldScale=Time.timeScale;bool paused=false;
            try
            {
                while(Time.time-start<3.5f)
                {
                    if(!Application.isPlaying)throw new Exception("Play stopped");
                    float phase=fairy.WingPhase;
                    if(phase<lastPhase-.5f)beats++;lastPhase=phase;
                    float left=Mathf.Abs(Mathf.DeltaAngle(0,fairy.leftWing.localEulerAngles.y));
                    float right=Mathf.Abs(Mathf.DeltaAngle(0,fairy.rightWing.localEulerAngles.y));
                    minAngle=Mathf.Min(minAngle,left,right);maxAngle=Mathf.Max(maxAngle,left,right);
                    minY=Mathf.Min(minY,fairy.HoverOffset);maxY=Mathf.Max(maxY,fairy.HoverOffset);
                    maxAsymmetry=Mathf.Max(maxAsymmetry,Mathf.Abs(left-right));
                    data.Add(new{time=Time.time-start,phase,left,right,hover=fairy.HoverOffset});
                    if(!openShot && phase<.045f && Time.time-start>.2f)
                    {openShot=true;Capture(camera,fairy,"open-front",false);Capture(camera,fairy,"open-side",true);}
                    if(!foldShot && phase>.46f && phase<.54f)
                    {foldShot=true;Capture(camera,fairy,"fold-front",false);Capture(camera,fairy,"fold-side",true);}
                    if(!paused && Time.time-start>1.5f)
                    {
                        float frozen=fairy.WingPhase,y=fairy.HoverOffset;Time.timeScale=0;
                        await Task.Delay(300);
                        if(Mathf.Abs(fairy.WingPhase-frozen)>.0001f || Mathf.Abs(fairy.HoverOffset-y)>.0001f)
                            throw new Exception("Flight continued while paused");
                        Time.timeScale=oldScale;paused=true;
                    }
                    await Task.Delay(12);
                }
                if(beats<3 || maxAngle-minAngle<30 || maxY-minY<.095f || maxAsymmetry>1f || !paused || !openShot || !foldShot)
                    throw new Exception($"Flight failed: beats={beats}, angles={minAngle}..{maxAngle}, hover={minY}..{maxY}, asymmetry={maxAsymmetry}, shots={openShot}/{foldShot}");
                var initialRoot=fairy.transform.position;var originalRest=fairy.visual.localPosition-Vector3.up*fairy.HoverOffset;
                fairy.enabled=false;
                bool restored=Vector3.Distance(fairy.visual.localPosition,originalRest)<.001f &&
                    Quaternion.Angle(fairy.leftWing.localRotation,Quaternion.identity)<.1f && Quaternion.Angle(fairy.rightWing.localRotation,Quaternion.identity)<.1f &&
                    Vector3.Distance(initialRoot,fairy.transform.position)<.001f;
                fairy.enabled=true;
                if(!restored)throw new Exception("Disabling flight did not restore the authored pose");
                Summary=new{beats,minAngle,maxAngle,minHover=minY,maxHover=maxY,maxAsymmetry,paused,restored,openShot,foldShot,rootUnmoved=restored};
                File.WriteAllText(Folder+"/samples.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
                Status="completed";
            }
            finally{Time.timeScale=oldScale;}
        }
        catch(Exception e){Status="failed: "+e;}
        finally
        {
            if(cameraObject)UnityEngine.Object.Destroy(cameraObject);
            File.WriteAllText(Folder+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{Status,Summary},Newtonsoft.Json.Formatting.Indented));
        }
    }
}
