using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviewShipbreakCamera20261014
{
    public static object Start(int fps=30,bool opposite=false,bool capture=true,string label="polish")
    {
        if(!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<ShipbreakCameraReview>()!=null)throw new Exception("Single original Play Mode review required");
        var go=new GameObject("Native shipbreak camera continuity review");UnityEngine.Object.DontDestroyOnLoad(go);
        var review=go.AddComponent<ShipbreakCameraReview>();review.fps=fps;review.opposite=opposite;review.capture=capture;review.label=label;
        return new{started=true,fps,opposite,capture,offline=true,performanceEvidence=false};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class ShipbreakCameraReview:MonoBehaviour
{
    public int fps;public bool opposite,capture;public string label;
    string folder;
    Story1SharkCollisionSequence sequence;Story1WreckBattle battle;Camera viewCamera;
    RenderTexture target;Texture2D pixels;
    readonly List<object> rows=new List<object>();
    Vector3 last;Quaternion lastRotation;bool sampled,ready,begun,finished;
    float warm,afterFight,beginPositionDelta,beginRotationDelta,maxBreakAngle,maxBreakStep,handoffAngle,handoffStep;
    int frames;Story1WreckBattle.Phase previousPhase;bool paused,pauseDone;double pauseAt;Vector3 pausePosition;Quaternion pauseRotation;float maximumPausedMotion;int pauseFrames;
    IEnumerator Start()
    {
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/shipbreak-camera-20261014/"+(opposite?"opposite-":"default-")+fps+"-"+label;
        Directory.CreateDirectory(folder);Time.captureFramerate=fps;
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
        yield return null;
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();viewCamera=sequence.storyCamera;
        var follow=viewCamera.GetComponent<ShipFollowCamera>();follow.mouseFreeLook=false;
        if(opposite)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(ShipFollowCamera).GetMethod("ResetView",flags).Invoke(follow,null);
            var yaw=typeof(ShipFollowCamera).GetField("orbitYaw",flags);yaw.SetValue(follow,-Mathf.Atan2(140,65)*Mathf.Rad2Deg);
            typeof(ShipFollowCamera).GetMethod("PlaceCameraImmediately",flags).Invoke(follow,null);
        }
        if(capture){target=new RenderTexture(960,540,24);target.Create();pixels=new Texture2D(960,540,TextureFormat.RGB24,false);}
        ready=true;
    }
    void LateUpdate()
    {
        if(!ready || finished)return;
        try
        {
            if(opposite && begun && !pauseDone && !paused && warm>=2)
            {paused=true;pauseAt=Time.realtimeSinceStartupAsDouble;pausePosition=viewCamera.transform.position;pauseRotation=viewCamera.transform.rotation;Time.timeScale=0;}
            if(paused)
            {
                pauseFrames++;maximumPausedMotion=Mathf.Max(maximumPausedMotion,Mathf.Max(Vector3.Distance(pausePosition,viewCamera.transform.position),Quaternion.Angle(pauseRotation,viewCamera.transform.rotation)));
                if(Time.realtimeSinceStartupAsDouble-pauseAt>.35){Time.timeScale=1;paused=false;pauseDone=true;sampled=false;}
                return;
            }
            warm+=Time.deltaTime;
            Vector3 at=viewCamera.transform.position;Quaternion rotation=viewCamera.transform.rotation;
            float step=sampled?Vector3.Distance(last,at):0,angle=sampled?Quaternion.Angle(lastRotation,rotation):0;
            if(!begun && warm>=1.5f)
            {
                sequence.BeginImpact();begun=true;
                beginPositionDelta=Vector3.Distance(at,viewCamera.transform.position);beginRotationDelta=Quaternion.Angle(rotation,viewCamera.transform.rotation);
            }
            var phase=battle.CurrentPhase;
            if(phase==Story1WreckBattle.Phase.Breaking){maxBreakAngle=Mathf.Max(maxBreakAngle,angle);maxBreakStep=Mathf.Max(maxBreakStep,step);}
            if(phase==Story1WreckBattle.Phase.Fighting)
            {
                if(previousPhase==Story1WreckBattle.Phase.Breaking){handoffAngle=angle;handoffStep=step;}
                afterFight+=Time.deltaTime;
            }
            rows.Add(new{frame=frames,time=warm,phase=phase.ToString(),step,angle,angularSpeed=angle/Mathf.Max(.0001f,Time.deltaTime),
                position=new[]{at.x,at.y,at.z},rotation=new[]{rotation.x,rotation.y,rotation.z,rotation.w},battle.ImpactTailGap,battle.InitialFragmentCount});
            if(capture)
            {
                var oldTarget=viewCamera.targetTexture;var active=RenderTexture.active;
                try{viewCamera.targetTexture=target;viewCamera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/game-"+frames.ToString("D4")+".png",pixels.EncodeToPNG());}
                finally{viewCamera.targetTexture=oldTarget;RenderTexture.active=active;}
            }
            frames++;last=at;lastRotation=rotation;sampled=true;previousPhase=phase;
            if(afterFight>=(opposite?4f:1.8f))Finish();
            if(warm>15)throw new Exception("Shipbreak did not hand off to normal combat");
        }
        catch(Exception e){File.WriteAllText(folder+"/error.txt",e.ToString());Finish();}
    }
    void Finish()
    {
        if(finished)return;finished=true;
        File.WriteAllText(folder+"/review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{native=true,offline=true,performanceEvidence=false,
            fps,opposite,frames,pauseFrames,maximumPausedMotion,beginPositionDelta,beginRotationDelta,maxBreakAngle,maxBreakStep,handoffAngle,handoffStep,
            handoffStillActive=sequence!=null && (bool)typeof(ThirdPersonPlayerController).GetField("continuingCinematicCamera",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sequence.sahur),impactTailGap=battle!=null?battle.ImpactTailGap:999,passedNoInitialCut=beginPositionDelta<.001f && beginRotationDelta<.01f,rows},Newtonsoft.Json.Formatting.Indented));
        Destroy(gameObject);
    }
    void OnDestroy(){if(paused)Time.timeScale=1;Time.captureFramerate=0;if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
