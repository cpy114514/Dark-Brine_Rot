using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
public static class AuditNearPassV216
{
    public static object Start(string take,bool stopAfterPass=true)
    {
        if(!Application.isPlaying)throw new Exception("Live recording required");
        var obj=new GameObject("Opening near-pass diagnostic camera");var audit=obj.AddComponent<OpeningNearPassV216>();audit.take=take;audit.stopAfterPass=stopAfterPass;
        return new{started=true,take};
    }
}
[DefaultExecutionOrder(32000)]
public sealed class OpeningNearPassV216:MonoBehaviour
{
    public string take;
    public bool stopAfterPass=true;
    Story1SharkCollisionSequence seq;Story1WreckBattle battle;Camera camera;RenderTexture target;Texture2D pixels;
    string folder;float next;int count;readonly List<object> rows=new List<object>();
    void Start()
    {
        folder="D:/indiegameDev/unity/Dark Brine_Rot/.codex/encounter-redesign-20261017/"+take+"/near-pass-audit";Directory.CreateDirectory(folder);
        seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=seq.GetComponent<Story1WreckBattle>();
        camera=new GameObject("Read-only geometry review camera").AddComponent<Camera>();camera.CopyFrom(seq.storyCamera);camera.enabled=false;
        target=new RenderTexture(640,360,24);target.Create();pixels=new Texture2D(640,360,TextureFormat.RGB24,false);camera.targetTexture=target;
    }
    void LateUpdate()
    {
        if(stopAfterPass&&battle!=null&&count>0&&battle.RedesignStage==Story1WreckBattle.OpeningStage.Duel&&battle.RedesignShot==35){UnityEditor.EditorApplication.isPlaying=false;return;}
        if(battle==null||battle.RedesignStage!=Story1WreckBattle.OpeningStage.Duel||battle.RedesignShot!=34||battle.RedesignClock<next)return;
        next=battle.RedesignClock+.16f;
        var animator=seq.sahur.CharacterAnimator;Vector3 focus=animator.GetBoneTransform(HumanBodyBones.Chest).position;
        Vector3 offset=Vector3.up*42-seq.sahur.transform.forward*8;
        camera.orthographic=true;camera.orthographicSize=22;
        camera.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset,Vector3.forward));
        var previous=RenderTexture.active;
        try{
            camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(folder+"/top-"+count.ToString("D3")+".png",pixels.EncodeToPNG());
            offset=Vector3.Cross(Vector3.up,seq.sahur.transform.forward).normalized*40+Vector3.up*5;
            camera.orthographicSize=18;camera.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset,Vector3.up));
            camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0,false);File.WriteAllBytes(folder+"/side-"+count.ToString("D3")+".png",pixels.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;}
        rows.Add(new{index=count,clock=battle.RedesignClock,shot=battle.RedesignShot,hero=seq.sahur.transform.position.ToString("F3"),camera=camera.transform.position.ToString("F3")});count++;
        File.WriteAllText(folder+"/index.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
    }
    void OnDestroy(){if(camera!=null)Destroy(camera.gameObject);if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
