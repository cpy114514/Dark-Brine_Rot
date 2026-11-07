using System;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>A replayable, ordered Skyboard time trial above the candy-house valley.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(450)]
public sealed class SkyboardRingRally : MonoBehaviour
{
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    const float RingRadius=6.5f;
    const float RelocateAfter=15f, MoveSeconds=.8f, MaxShift=100f;
    static readonly Vector2[] Route={
        new Vector2(20,1965),new Vector2(-15,2000),new Vector2(-50,2035),new Vector2(-95,2055),new Vector2(-140,2035),
        new Vector2(-165,1995),new Vector2(-140,1955),new Vector2(-100,1935),new Vector2(-60,1925),new Vector2(-20,1935)
    };
    static readonly Vector2[] ClearanceOffsets={new Vector2(7,0),new Vector2(-7,0),new Vector2(0,7),new Vector2(0,-7)};
    sealed class Ring
    {
        public Transform root;
        public Renderer renderer;
        public GameObject label;
        public Text labelText;
        public Vector3 anchor;
        public Quaternion initialRotation;
    }
    readonly List<Ring> rings=new List<Ring>();
    readonly List<Material> materials=new List<Material>();
    readonly System.Random random=new System.Random();
    Mesh ringMesh;
    Material idleMaterial,nextMaterial,passedMaterial,boostMaterial;
    ThirdPersonPlayerController player;
    CharacterController body;
    SkyboardReward board;
    EquipmentInventory inventory;
    EquipmentItem scroll;
    Collider island;
    GameObject sign, signLabel, hud, hudCard;
    Text status, detail;
    Vector3 previous, lastCleared, moveFrom, moveTo;
    bool hasPrevious, wasFlying, boostActive, relocating;
    float nextLookup, startedAt, boostUntil, baseSpeed, messageUntil, lastTime, targetDeadline, moveStarted, nextHudRefresh;
    string resultText="";
    public bool Running {get;private set;}
    public bool RewardClaimed {get;private set;}
    public float BestTime {get;private set;}
    public int NextRing {get;private set;}
    public int TotalRings => rings.Count;
    public Transform StartMarker => sign ? sign.transform : null;
    public Transform RingTransform(int index)=>index>=0 && index<rings.Count ? rings[index].root : null;
    public float Elapsed => Running ? Mathf.Max(0,Time.time-startedAt) : lastTime;
    public bool Relocating => relocating;
    public float TimeUntilMove => targetDeadline>0 ? Mathf.Max(0,targetDeadline-Time.time) : RelocateAfter;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene(){for(int i=0;i<SceneManager.sceneCount;i++)Ensure(SceneManager.GetSceneAt(i));}
    static void OnSceneLoaded(Scene scene,LoadSceneMode mode)=>Ensure(scene);
    static void Ensure(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<SkyboardRingRally>(true))return;
        var obj=new GameObject("Skyboard Ring Rally");SceneManager.MoveGameObjectToScene(obj,scene);obj.AddComponent<SkyboardRingRally>();
    }
    void Awake()
    {
        var experience=FindFirstObjectByType<MarshmallowIslandExperience>();
        island=experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if(!island){Debug.LogError("Skyboard rally could not find the island surface.");enabled=false;return;}
        idleMaterial=MakeMaterial("Rally icing blue",new Color(.28f,.72f,.88f));
        nextMaterial=MakeMaterial("Rally next ring gold",new Color(1f,.77f,.17f),true);
        passedMaterial=MakeMaterial("Rally cleared ring pink",new Color(.95f,.42f,.65f));
        boostMaterial=MakeMaterial("Rally boost ring violet",new Color(.71f,.48f,1f),true);
        var pearlMaterial=MakeMaterial("Rally sugar pearls",new Color(1f,.87f,.56f),true);
        ringMesh=BuildRingMesh();
        var centers=new Vector3[Route.Length];
        for(int i=0;i<Route.Length;i++)centers[i]=Ground(Route[i])+Vector3.up*12f;
        for(int i=0;i<centers.Length;i++)
        {
            Vector3 dir=i==0 ? centers[1]-centers[0] : centers[i]-centers[i-1];dir.y=0;
            var root=new GameObject("Ring "+(i+1).ToString("00"),typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider),typeof(BoxCollider));
            root.transform.SetParent(transform,false);root.transform.SetPositionAndRotation(centers[i],Quaternion.LookRotation(dir.normalized));
            root.GetComponent<MeshFilter>().sharedMesh=ringMesh;
            var renderer=root.GetComponent<MeshRenderer>();renderer.sharedMaterial=idleMaterial;
            root.GetComponent<MeshCollider>().sharedMesh=ringMesh;
            var trigger=root.GetComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(RingRadius*2,RingRadius*2,1.5f);
            for(int pearl=0;pearl<4;pearl++)
            {
                float a=pearl*Mathf.PI*.5f;
                Solid("Ring sugar pearl",PrimitiveType.Sphere,root.transform,new Vector3(Mathf.Cos(a)*RingRadius,Mathf.Sin(a)*RingRadius,0),Vector3.one*.52f,pearlMaterial);
            }
            SkyboardReward.Label(root.transform,"");var label=root.transform.Find("Pickup label");label.name="Ring number";
            label.localPosition=new Vector3(0,RingRadius+1.8f,0);label.localScale=Vector3.one*.021f;
            var text=label.GetComponentInChildren<Text>();text.fontSize=30;text.color=new Color(1f,.95f,.70f);label.gameObject.SetActive(false);
            rings.Add(new Ring{root=root.transform,renderer=renderer,label=label.gameObject,labelText=text,anchor=centers[i],initialRotation=root.transform.rotation});
        }
        BuildSign();BuildHud();RefreshRings();
    }
    Vector3 Ground(Vector2 xy)
    {
        var b=island.bounds;
        if(island.Raycast(new Ray(new Vector3(xy.x,b.max.y+10,xy.y),Vector3.down),out var hit,b.size.y+20))return hit.point;
        throw new InvalidOperationException("Skyboard rally route is missing ground at "+xy);
    }
    bool TryGround(Vector2 xy,out RaycastHit hit)
    {
        var b=island.bounds;
        return island.Raycast(new Ray(new Vector3(xy.x,b.max.y+10,xy.y),Vector3.down),out hit,b.size.y+20);
    }
    Vector3 ChoosePosition(int index,Vector3 current)
    {
        Vector3 anchor=rings[index].anchor;
        for(int attempt=0;attempt<90;attempt++)
        {
            double angle=random.NextDouble()*Math.PI*2;
            float radius=12f+(float)random.NextDouble()*20f;
            var xy=new Vector2(anchor.x+Mathf.Cos((float)angle)*radius,anchor.z+Mathf.Sin((float)angle)*radius);
            if(!TryGround(xy,out var hit) || hit.normal.y<.75f)continue;
            Vector3 point=hit.point+Vector3.up*12f;
            if(board && point.y+RingRadius>board.CeilingHeight+2.6f)continue;
            if(Vector3.Distance(point,current)>MaxShift || Vector3.Distance(point,current)<8f || Vector3.Distance(point,anchor)>MaxShift)continue;
            if(index>0 && Vector3.Distance(point,lastCleared)>95f)continue;
            bool clear=true;
            foreach(var offset in ClearanceOffsets)
                if(!TryGround(xy+offset,out var nearby) || nearby.point.y>point.y-RingRadius-.5f){clear=false;break;}
            if(clear)return point;
        }
        // A valid original location is a safe fallback if unusual terrain or
        // scene changes leave no suitable nearby random candidate.
        if(Vector3.Distance(anchor,current)>8f && Vector3.Distance(anchor,current)<=MaxShift &&
            (index==0 || Vector3.Distance(anchor,lastCleared)<=95f))return anchor;
        return current;
    }
    void FaceTarget(int index,Vector3 point)
    {
        Vector3 origin=index>0 ? lastCleared : (player ? player.transform.position : rings[0].anchor-rings[0].root.forward*10f);
        Vector3 direction=point-origin;direction.y=0;
        if(direction.sqrMagnitude>.01f)rings[index].root.rotation=Quaternion.LookRotation(direction);
    }
    void ShowNextRing(int index)
    {
        var ring=rings[index];
        ring.root.position=ChoosePosition(index,ring.anchor);
        FaceTarget(index,ring.root.position);
        relocating=false;targetDeadline=Time.time+RelocateAfter;
        RefreshRings();
    }
    void BeginRelocation(int index)
    {
        var ring=rings[index];
        moveFrom=ring.root.position;moveTo=ChoosePosition(index,moveFrom);
        // If terrain changed and no candidate is safe, retry later rather than
        // moving through the island or farther than the stated maximum.
        if(Vector3.Distance(moveFrom,moveTo)<1f){targetDeadline=Time.time+RelocateAfter;return;}
        FaceTarget(index,moveTo);
        relocating=true;moveStarted=Time.time;targetDeadline=0;
    }
    void ResetStartRing()
    {
        relocating=false;targetDeadline=0;hasPrevious=false;
        if(rings.Count>0){rings[0].root.position=rings[0].anchor;rings[0].root.rotation=rings[0].initialRotation;}
    }
    Material MakeMaterial(string name,Color color,bool glow=false)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m=new Material(shader){name=name};
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);else m.color=color;
        if(glow && m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.25f);}
        materials.Add(m);return m;
    }
    static Mesh BuildRingMesh()
    {
        const int segments=32,sides=8;const float tube=.32f;
        var vertices=new Vector3[(segments+1)*(sides+1)];var triangles=new int[segments*sides*6];
        for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++)
        {
            float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;
            float radius=RingRadius+Mathf.Cos(b)*tube;
            vertices[i*(sides+1)+j]=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,Mathf.Sin(b)*tube);
        }
        int k=0;
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
        {
            int a=i*(sides+1)+j,b=a+sides+1;
            triangles[k++]=a;triangles[k++]=b;triangles[k++]=a+1;
            triangles[k++]=a+1;triangles[k++]=b;triangles[k++]=b+1;
        }
        var mesh=new Mesh{name="Skyboard rally torus"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static GameObject Solid(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 size,Material material)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=pos;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
    }
    void BuildSign()
    {
        sign=new GameObject("Skyboard rally start sign");sign.transform.SetParent(transform,false);sign.transform.position=Ground(new Vector2(31,1957));
        var wood=MakeMaterial("Rally sign frame",new Color(.58f,.29f,.36f));
        var backing=MakeMaterial("Rally sign icing",new Color(.19f,.10f,.24f));
        var accent=MakeMaterial("Rally sign candy",new Color(1f,.60f,.73f));
        Solid("Left sign post",PrimitiveType.Cylinder,sign.transform,new Vector3(-3,2,0),new Vector3(.35f,2,.35f),wood);
        Solid("Right sign post",PrimitiveType.Cylinder,sign.transform,new Vector3(3,2,0),new Vector3(.35f,2,.35f),wood);
        Solid("Sign board",PrimitiveType.Cube,sign.transform,new Vector3(0,4.1f,0),new Vector3(7.8f,3,.4f),backing);
        Solid("Sign topping",PrimitiveType.Cube,sign.transform,new Vector3(0,5.7f,0),new Vector3(8,.25f,.5f),accent);
        SkyboardReward.Label(sign.transform,"SKYBOARD RING RALLY\nOne ring appears at a time\nEach moves after 15 seconds\nPurple rings give a speed boost\nFirst finish: +1 speed scroll!");
        var label=sign.transform.Find("Pickup label");label.name="Rally sign text";label.localPosition=new Vector3(0,4.15f,-.28f);label.localScale=Vector3.one*.013f;
        ((RectTransform)label).sizeDelta=new Vector2(550,180);
        var text=label.GetComponentInChildren<Text>();text.fontSize=25;text.color=new Color(1f,.95f,.86f);signLabel=label.gameObject;
    }
    void BuildHud()
    {
        hud=new GameObject("Skyboard rally HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));hud.transform.SetParent(transform,false);
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;GameUITheme.Scale(canvas);
        var card=GameUITheme.Rect(hud.transform,"Rally card",new Vector2(.5f,1),new Vector2(560,112));card.pivot=new Vector2(.5f,1);card.anchoredPosition=new Vector2(0,-25);
        GameUITheme.Image(card,new Color(.045f,.035f,.075f,.91f));
        status=GameUITheme.Label(card,"Status",24,new Vector2(-260,-26),new Vector2(520,34),TextAnchor.MiddleLeft);
        detail=GameUITheme.Label(card,"Detail",19,new Vector2(-260,-72),new Vector2(520,64),TextAnchor.MiddleLeft);
        hudCard=card.gameObject;hudCard.SetActive(false);
    }
    public void RestoreProgress(bool claimed,float best)
    {
        RewardClaimed=claimed;BestTime=float.IsNaN(best)||float.IsInfinity(best)||best<=0?0:Mathf.Min(best,3600);
    }
    void Update()
    {
        if(!player && Time.unscaledTime>=nextLookup)
        {
            nextLookup=Time.unscaledTime+1;player=FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player){body=player.GetComponent<CharacterController>();inventory=player.GetComponent<EquipmentInventory>();}
            board=FindFirstObjectByType<SkyboardReward>();scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
        }
        if(!player || !body || !board || rings.Count!=Route.Length)return;
        var cam=Camera.main;
        for(int i=0;i<rings.Count;i++)if(rings[i].label.activeSelf && cam)rings[i].label.transform.rotation=cam.transform.rotation;
        bool flying=board.Flying && body.enabled && player.isActiveAndEnabled;
        if(wasFlying && !flying && Running)Fail("Flight ended. Start again at ring 1.");
        wasFlying=flying;
        if(boostActive && (!flying || !Running || Time.time>=boostUntil))StopBoost();
        if(!flying || Time.timeScale<=0 || GameUITheme.ModalOpen)
        {
            if(!flying && !Running){relocating=false;targetDeadline=0;}
            hasPrevious=false;UpdateHud(cam);return;
        }
        var current=body.bounds.center;
        if(Running && Keyboard.current!=null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Fail("RALLY RESET  ·  Fly through START to retry.");
            previous=current;hasPrevious=true;UpdateHud(cam);return;
        }
        int wanted=Running ? NextRing : 0;
        bool timing=Running || sign && Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-sign.transform.position,Vector3.up),Vector3.zero)<120f;
        if(relocating)
        {
            float phase=Mathf.Clamp01((Time.time-moveStarted)/MoveSeconds);
            rings[wanted].root.position=Vector3.Lerp(moveFrom,moveTo,Mathf.SmoothStep(0,1,phase));
            previous=current;hasPrevious=true;
            if(phase>=1){relocating=false;targetDeadline=timing?Time.time+RelocateAfter:0;}
            UpdateHud(cam);return;
        }
        if(!timing)targetDeadline=0;
        else if(targetDeadline<=0)targetDeadline=Time.time+RelocateAfter;
        else if(Time.time>=targetDeadline)
        {
            BeginRelocation(wanted);previous=current;hasPrevious=true;UpdateHud(cam);return;
        }
        if(!hasPrevious){previous=current;hasPrevious=true;UpdateHud(cam);return;}
        if(wanted<rings.Count && Crosses(rings[wanted].root,previous,current))PassRing(wanted);
        previous=current;
        UpdateHud(cam);
    }
    static bool Crosses(Transform ring,Vector3 from,Vector3 to)
    {
        Vector3 segment=to-from;
        // Ignore scene loads and teleports rather than awarding a crossed ring.
        if(segment.sqrMagnitude<.000001f || segment.sqrMagnitude>16f*16f)return false;
        Vector3 normal=ring.forward;
        float a=Vector3.Dot(from-ring.position,normal),b=Vector3.Dot(to-ring.position,normal);
        if(a>=0 || b<0 || b-a<.00001f)return false;
        Vector3 point=Vector3.Lerp(from,to,-a/(b-a));
        Vector3 local=ring.InverseTransformPoint(point);
        return local.x*local.x+local.y*local.y<=(RingRadius-.8f)*(RingRadius-.8f);
    }
    void PassRing(int index)
    {
        if(index==0){Running=true;startedAt=Time.time;lastTime=0;resultText="";}
        lastCleared=rings[index].root.position;
        NextRing=index+1;
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);
        if(index==2 || index==5 || index==8)StartBoost();
        if(NextRing>=rings.Count){Finish();return;}
        ShowNextRing(NextRing);
    }
    void StartBoost()
    {
        if(!boostActive){baseSpeed=board.horizontalSpeed;boostActive=true;}
        board.horizontalSpeed=baseSpeed*1.2f;boostUntil=Time.time+1.5f;
    }
    void StopBoost(){if(boostActive && board)board.horizontalSpeed=baseSpeed;boostActive=false;}
    void Finish()
    {
        lastTime=Time.time-startedAt;Running=false;StopBoost();
        bool personal=BestTime<=0 || lastTime<BestTime;if(personal)BestTime=lastTime;
        bool reward=false;
        if(!RewardClaimed && inventory && scroll)
        {
            inventory.RestoreStack(scroll,inventory.Count(scroll)+1);RewardClaimed=true;reward=true;
        }
        string medal=lastTime<32f?"GOLD":lastTime<42f?"SILVER":"BRONZE";
        resultText=medal+" FINISH  "+lastTime.ToString("F1")+"s"+(personal?"  ·  NEW BEST":"")+(reward?"  ·  +1 SPEED SCROLL":"");
        messageUntil=Time.unscaledTime+8f;NextRing=0;ResetStartRing();RefreshRings();
        if(!GameSaveManager.SaveCurrentGame())GameSaveManager.RequestAutosave();
    }
    void Fail(string reason)
    {
        lastTime=Time.time-startedAt;Running=false;NextRing=0;StopBoost();resultText=reason;messageUntil=Time.unscaledTime+7f;ResetStartRing();RefreshRings();
    }
    void RefreshRings()
    {
        nextHudRefresh=0;
        for(int i=0;i<rings.Count;i++)
        {
            bool next=i==(Running?NextRing:0);
            rings[i].root.gameObject.SetActive(next);
            rings[i].renderer.sharedMaterial=(i==2 || i==5 || i==8)?boostMaterial:nextMaterial;
            rings[i].label.SetActive(next);
            rings[i].labelText.text=i==0 && !Running?"START  ·  1 / "+rings.Count:(i+1)+" / "+rings.Count+((i==2 || i==5 || i==8)?"  ·  BOOST":"");
        }
    }
    void UpdateHud(Camera cam)
    {
        if(!hudCard)return;
        bool modal=GameUITheme.ModalOpen || SecondIslandAwakening.BlocksInputFor(player) || player.ExternalControlLock;
        bool close=sign && Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-sign.transform.position,Vector3.up),Vector3.zero)<28f && Mathf.Abs(body.bounds.min.y-sign.transform.position.y)<25f;
        bool result=Time.unscaledTime<messageUntil;
        bool approaching=board.Flying && sign && Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-sign.transform.position,Vector3.up),Vector3.zero)<120f;
        bool show=!modal && (Running || result || close || approaching);
        if(hudCard.activeSelf!=show)hudCard.SetActive(show);if(!show)return;
        if(Time.unscaledTime<nextHudRefresh)return;
        nextHudRefresh=Time.unscaledTime+.1f;
        if(Running)
        {
            status.text="SKYBOARD RALLY  ·  "+NextRing+" / "+rings.Count+"  ·  "+Elapsed.ToString("F1")+"s";
            Vector3 delta=rings[NextRing].root.position-body.bounds.center;
            Vector3 forward=cam?Vector3.ProjectOnPlane(cam.transform.forward,Vector3.up).normalized:player.transform.forward;
            Vector3 right=cam?Vector3.ProjectOnPlane(cam.transform.right,Vector3.up).normalized:player.transform.right;
            float f=Vector3.Dot(forward,delta),s=Vector3.Dot(right,delta);
            string arrow=Mathf.Abs(s)>Mathf.Abs(f)*.45f?(s>0?"→":"←"):(f>=0?"↑":"↓");
            string movement=relocating?"MOVING":"MOVES IN "+Mathf.CeilToInt(TimeUntilMove)+"s";
            detail.text=arrow+"  NEXT RING  ·  "+Mathf.RoundToInt(delta.magnitude)+" m  ·  "+movement+(boostActive?"  ·  BOOST!":"")+"\nR: reset rally";
        }
        else if(result){status.text=resultText;detail.text="Best: "+(BestTime>0?BestTime.ToString("F1")+"s":"--")+"  ·  Fly through ring 1 to retry.";}
        else
        {
            status.text="SKYBOARD RING RALLY";
            detail.text=board.Owned?(approaching?(relocating?"START RING MOVING":"START RING MOVES IN "+Mathf.CeilToInt(TimeUntilMove)+"s")+"  ·  Only one ring appears at a time.\n":"Only one ring appears at a time; it moves after 15s.\n")+"Gold <32s  ·  Silver <42s  ·  Best: "+(BestTime>0?BestTime.ToString("F1")+"s":"--"):
                "Find the Skyboard at the end of the marshmallow course.\nThen fly through the gold start ring!";
        }
    }
    void OnDisable(){StopBoost();}
    void OnDestroy()
    {
        StopBoost();foreach(var m in materials)if(m)Destroy(m);
        if(ringMesh)Destroy(ringMesh);
    }
}
