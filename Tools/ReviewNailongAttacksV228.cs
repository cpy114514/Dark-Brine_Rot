using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mavis;

public static class ReviewNailongAttacksV228
{
    public static object Start(string label="review")
    {
        if(!Application.isPlaying)throw new Exception("Use the existing Editor Play Mode.");
        string folder=".codex/nailong-attacks-20261020/"+label;
        if(Directory.Exists(folder))throw new Exception("Preserve old recordings.");Directory.CreateDirectory(folder);
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())root.SetActive(false);
        var observer=new GameObject("Temporary native Nailong attack review").AddComponent<NailongReviewV228>();observer.folder=folder;
        EditorApplication.isPaused=false;return new{started=true,folder};
    }
}

[DefaultExecutionOrder(31900)]
public sealed class NailongReviewV228:MonoBehaviour
{
    public string folder;
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    NailongAI ai;NailongCombatEffects effects;NailongAttackMotion motion;PlayerHealth victim;
    Camera cameraView;RenderTexture target;Texture2D pixels,checker;Material floorMaterial;
    readonly List<object> rows=new List<object>();
    float began,oldScale;int frame,oldCapture,phase=-1;
    bool oldFog;AmbientMode oldAmbientMode;Color oldAmbient;
    Type patternType;

    void Start()
    {
        oldCapture=Time.captureFramerate;oldScale=Time.timeScale;Time.captureFramerate=30;Time.timeScale=1;
        oldFog=RenderSettings.fog;oldAmbientMode=RenderSettings.ambientMode;oldAmbient=RenderSettings.ambientLight;
        RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.28f,.31f,.35f);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary checker ground";floor.transform.SetParent(transform);
        floor.transform.position=new Vector3(0,499.5f,0);floor.transform.localScale=new Vector3(80,1,80);
        checker=new Texture2D(64,64,TextureFormat.RGB24,false);checker.wrapMode=TextureWrapMode.Repeat;
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)checker.SetPixel(x,y,((x/16+y/16)%2)==0?new Color(.32f,.36f,.29f):new Color(.57f,.58f,.49f));
        checker.Apply();floorMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMaterial.mainTexture=checker;floorMaterial.mainTextureScale=new Vector2(36,36);
        floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
        var sun=new GameObject("Temporary attack review sunlight").AddComponent<Light>();sun.transform.SetParent(transform);
        sun.type=LightType.Directional;sun.intensity=1.1f;sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;
        var player=new GameObject("Excluded review target");player.transform.SetParent(transform);player.AddComponent<GameSaveExcluded>();
        player.transform.position=new Vector3(0,500,12);player.tag="Player";
        var body=player.AddComponent<CharacterController>();body.height=2;body.center=Vector3.up;body.radius=.3f;
        victim=player.AddComponent<PlayerHealth>();victim.maxHealth=victim.currentHealth=100;
        var mannequin=GameObject.CreatePrimitive(PrimitiveType.Capsule);mannequin.transform.SetParent(player.transform,false);mannequin.transform.localPosition=Vector3.up;
        var bodyCollider=mannequin.GetComponent<Collider>();bodyCollider.enabled=false;Destroy(bodyCollider);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab");
        var boss=UnityEngine.Object.Instantiate(prefab,new Vector3(0,500,0),Quaternion.identity);boss.transform.SetParent(transform,true);
        ai=boss.GetComponent<NailongAI>();ai.enabled=false;var agent=boss.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
        effects=boss.GetComponent<NailongCombatEffects>();motion=boss.GetComponent<NailongAttackMotion>();
        typeof(NailongAI).GetField("target",Private).SetValue(ai,player.transform);
        patternType=typeof(NailongAI).GetNestedType("AttackPattern",BindingFlags.NonPublic);
        cameraView=new GameObject("Native attack review camera").AddComponent<Camera>();cameraView.transform.SetParent(transform);cameraView.tag="MainCamera";
        cameraView.fieldOfView=50;cameraView.nearClipPlane=.1f;cameraView.farClipPlane=200;
        var data=cameraView.gameObject.AddComponent<UniversalAdditionalCameraData>();data.requiresColorTexture=!folder.Contains("fallback");data.requiresDepthTexture=!folder.Contains("fallback");
        cameraView.transform.SetPositionAndRotation(new Vector3(18,507,9),Quaternion.LookRotation(new Vector3(0,502,5)-new Vector3(18,507,9)));
        target=new RenderTexture(960,540,24);target.Create();cameraView.targetTexture=target;
        pixels=new Texture2D(960,540,TextureFormat.RGB24,false);began=Time.time;
    }
    void Update()
    {
        if(!ai)return;float clock=Time.time-began;
        int next=clock<4?0:clock<8?1:2;
        if(next!=phase)
        {
            phase=next;victim.currentHealth=100;
            if(phase>0)victim.transform.position=new Vector3(0,500,8);
            Physics.SyncTransforms();
            typeof(NailongAI).GetField("state",Private).SetValue(ai,NailongAI.State.Idle);
            typeof(NailongAI).GetMethod("BeginAttack",Private).Invoke(ai,new[]{Enum.Parse(patternType,phase==0?"ScoldingSpit":phase==1?"Roar":"GroundSlam")});
        }
        typeof(NailongAI).GetMethod("TickAttack",Private).Invoke(ai,null);
    }
    void LateUpdate()
    {
        if(!ai)return;float clock=Time.time-began;
        var projectiles=UnityEngine.Object.FindObjectsByType<NailongSpitProjectile>(FindObjectsSortMode.None);
        var a=ai.GetComponentInChildren<Animator>();
        rows.Add(new{clock,phase,attack=ai.AttackName,state=ai.CurrentState.ToString(),victim.currentHealth,
            mouth=new[]{motion.MouthPosition.x,motion.MouthPosition.y,motion.MouthPosition.z},
            leftFoot=a.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,rightFoot=a.GetBoneTransform(HumanBodyBones.RightFoot).position.y,
            projectiles=projectiles.Select(p=>new{p.HasImpacted,pos=new[]{p.transform.position.x,p.transform.position.y,p.transform.position.z},p.FlightAge,vy=p.Velocity.y}).ToArray(),
            waves=UnityEngine.Object.FindObjectsByType<NailongGroundSlam>(FindObjectsSortMode.None).Select(w=>new{w.PulsesReleased,w.DamageHits,w.JumpDodges}).ToArray()});
        if(frame>3&&frame%2==0)
        {
            var previous=RenderTexture.active;
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,540),0,0,false);File.WriteAllBytes(folder+"/frame-"+(frame/2).ToString("D4")+".png",pixels.EncodeToPNG());}
            finally{RenderTexture.active=previous;}
        }
        frame++;
        if(folder.Contains("qa")&&clock>=1.25f)
        {
            enabled=false;EditorApplication.isPaused=true;File.WriteAllText(folder+"/ready.txt",clock.ToString("F3"));return;
        }
        if(clock<15)return;
        File.WriteAllText(folder+"/motion.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
        Time.captureFramerate=oldCapture;Time.timeScale=oldScale;enabled=false;EditorApplication.isPlaying=false;
    }
    void OnDestroy()
    {
        Time.captureFramerate=oldCapture;Time.timeScale=oldScale;
        RenderSettings.fog=oldFog;RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientLight=oldAmbient;
        if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);if(checker)Destroy(checker);if(floorMaterial)Destroy(floorMaterial);
    }
}
