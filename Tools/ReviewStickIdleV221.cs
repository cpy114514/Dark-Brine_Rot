using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public static class ReviewStickIdleV221
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Play mode required");
        string path=".codex/encounter-redesign-20261017/"+label+"/stick-closeup";
        if(Directory.Exists(path))throw new Exception("Preserve previous recording");Directory.CreateDirectory(path);
        var observer=new GameObject("Stick closeup review v221").AddComponent<StickIdleReviewV221>();observer.folder=path;
        return new{started=true,auxiliaryCameraOnly=true};
    }
}
[DefaultExecutionOrder(31900)]
public sealed class StickIdleReviewV221:MonoBehaviour
{
    public string folder;
    Story1SharkCollisionSequence sequence;Story1WreckBattle battle;Camera auxiliary;RenderTexture target;Texture2D pixels;int frame;
    readonly List<object> rows=new List<object>();
    void Start()
    {
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();battle=sequence.GetComponent<Story1WreckBattle>();
        auxiliary=new GameObject("Auxiliary stick review camera").AddComponent<Camera>();auxiliary.CopyFrom(sequence.storyCamera);auxiliary.enabled=false;
        auxiliary.gameObject.AddComponent<UniversalAdditionalCameraData>();auxiliary.fieldOfView=42;
        target=new RenderTexture(640,480,24);target.Create();auxiliary.targetTexture=target;pixels=new Texture2D(640,480,TextureFormat.RGB24,false);
    }
    void LateUpdate()
    {
        if(battle==null)return;
        if(battle.RedesignActive&&battle.RedesignClock>2)
        {File.WriteAllText(folder+"/frames.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));enabled=false;return;}
        if(Time.frameCount%3!=0)return;
        var actor=sequence.sahur;var a=actor.CharacterAnimator;var chest=a.GetBoneTransform(HumanBodyBones.Chest).position;
        var look=chest-Vector3.up*.5f;
        auxiliary.transform.position=look+actor.transform.forward*9+actor.transform.right*6+Vector3.up*2.5f;
        auxiliary.transform.rotation=Quaternion.LookRotation(look-auxiliary.transform.position);
        var previous=RenderTexture.active;
        try{auxiliary.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,480),0,0,false);File.WriteAllBytes(folder+"/idle-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());}
        finally{RenderTexture.active=previous;}
        var weapon=actor.GetComponent<Mavis.SahurAttack>().stickHitbox as CapsuleCollider;
        Vector3 axis=weapon.direction==0?Vector3.right:weapon.direction==1?Vector3.up:Vector3.forward;
        var near=weapon.transform.TransformPoint(weapon.center-axis*(weapon.height*.5f-weapon.radius));
        var far=weapon.transform.TransformPoint(weapon.center+axis*(weapon.height*.5f-weapon.radius));
        rows.Add(new{frame,clock=battle.RedesignClock,active=battle.RedesignActive,idle=actor.GetComponent<Story1SahurPassenger>().StickIdleTime,near=new[]{near.x,near.y,near.z},far=new[]{far.x,far.y,far.z}});frame++;
    }
    void OnDestroy(){if(auxiliary!=null)Destroy(auxiliary.gameObject);if(target!=null){target.Release();Destroy(target);}if(pixels!=null)Destroy(pixels);}
}
