using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using Mavis;
public static class TestRecoilPauseV002
{
    static async Task Frames(int count){int end=Time.frameCount+count;var deadline=DateTime.UtcNow.AddSeconds(30);while(Time.frameCount<end){if(DateTime.UtcNow>deadline)throw new Exception("Native frame timeout");await Task.Delay(5);}}
    public static async Task<object> Run()
    {
        float capture=Time.captureDeltaTime,scale=Time.timeScale;
        const string folder=".codex/dual-blade-polish-20261019/pause";Directory.CreateDirectory(folder);
        try
        {
            CombatFeelPreview.Spawn();Time.captureDeltaTime=1f/60;
            var target=CombatFeelPreview.Target;var reaction=target.GetComponent<NailongHitReaction>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var recorder=target.AddComponent<RecoilPauseRecordV002>();recorder.folder=folder;recorder.reaction=reaction;recorder.bone=(Transform)typeof(NailongHitReaction).GetField("spine",flags).GetValue(reaction);
            await Frames(8);
            CombatHitFeedback.Apply(CombatFeelPreview.Player,target.GetComponent<NailongHealth>(),1,target.transform.position+Vector3.up*2,CombatHitKind.ChargedHeavy,Vector3.forward);
            int deadline=Time.frameCount+120;
            while(recorder.angle<8){if(Time.frameCount>deadline)throw new Exception("No visible recoil before pause");await Frames(1);}
            Quaternion before=recorder.pose;float clock=(float)typeof(NailongHitReaction).GetField("elapsed",flags).GetValue(reaction);
            Time.timeScale=0;await Frames(8);float error=Quaternion.Angle(before,recorder.pose),clockError=Mathf.Abs(clock-(float)typeof(NailongHitReaction).GetField("elapsed",flags).GetValue(reaction));
            if(error>.05f||clockError>.00001f)throw new Exception("Pause lost recoil pose/clock: "+error+"/"+clockError);
            Time.timeScale=1;await Frames(45);if(reaction.IsReacting)throw new Exception("Recoil failed to recover after resume");
            reaction.enabled=false;await Frames(3);
            var result=new{passed=true,pausePoseErrorDegrees=error,pauseClockError=clockError,frames=recorder.frames,nativeConsecutiveCamera=true};
            File.WriteAllText(folder+"/tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
        }
        finally{Time.timeScale=1;CombatFeelPreview.Clear();Time.captureDeltaTime=capture;Time.timeScale=scale;}
    }
}
[DefaultExecutionOrder(32500)]
public sealed class RecoilPauseRecordV002:MonoBehaviour
{
    public string folder;public NailongHitReaction reaction;public Transform bone;public Quaternion pose;public float angle;public int frames;
    RenderTexture target;Texture2D pixels;
    void LateUpdate()
    {
        if(!bone||string.IsNullOrEmpty(folder))return;
        pose=bone.localRotation;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var basis=(Quaternion)typeof(NailongHitReaction).GetField("spineBase",flags).GetValue(reaction);angle=Quaternion.Angle(pose,basis);
        var camera=CombatFeelPreview.Camera;camera.transform.position=transform.position+new Vector3(8,5,-9);camera.transform.LookAt(transform.position+Vector3.up*2);
        if(!target){target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);}
        var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(folder+"/frame-"+(frames++).ToString("D4")+".png",pixels.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;}
    }
    void OnDestroy(){if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);}
}
