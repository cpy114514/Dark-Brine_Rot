using System;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>A relaxed, replayable spring-pad game on Marshmallow Island.</summary>
[DisallowMultipleComponent]
public sealed class CandyBounceGarden : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    static readonly Vector2[] Sites = { new Vector2(106,2070), new Vector2(118,2070), new Vector2(130,2070) };
    static readonly Color[] PadColors = { new Color(1,.48f,.66f), new Color(.39f,.76f,1), new Color(.56f,.92f,.68f) };
    const float PadTop = .52f;
    public bool Playing { get; private set; }
    public bool Advanced { get; private set; }
    public bool RewardClaimed { get; private set; }
    public int Collected { get; private set; }
    public Transform MapAnchor => transform;
    public Transform Sign => sign;
    public Transform Pad(int i) => i>=0 && i<pads.Count ? pads[i] : null;
    public Transform Star(int i) => i>=0 && i<stars.Count ? stars[i] : null;
    public bool ReservesInteraction
    {
        get
        {
            if(lastInteractionFrame==Time.frameCount)return true;
            if(!sign || !player || !body || !body.enabled)return false;
            return Vector3.ProjectOnPlane(player.transform.position-sign.position,Vector3.up).sqrMagnitude<16 && Mathf.Abs(body.bounds.min.y-sign.position.y)<4;
        }
    }
    readonly List<Transform> pads = new List<Transform>(), stars = new List<Transform>();
    readonly List<Material> materials = new List<Material>();
    readonly List<Mesh> rampMeshes = new List<Mesh>();
    readonly bool[] collected = new bool[3];
    Collider island;
    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    EquipmentInventory inventory;
    EquipmentItem scroll;
    SkyboardReward board;
    CandyMelodyGarden melody;
    Transform sign, signLabel, signPrompt, friendVisual, friendArm;
    Text promptText, hudText;
    GameObject hud;
    Mesh starMesh;
    float nextLookup, nextBounce, nextHud, feedbackUntil;
    int lastInteractionFrame=-1;
    string feedback = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register() { SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene() { for (int i=0;i<SceneManager.sceneCount;i++) Ensure(SceneManager.GetSceneAt(i)); }
    static void OnSceneLoaded(Scene s, LoadSceneMode mode) => Ensure(s);
    static void Ensure(Scene scene)
    {
        if (!scene.isLoaded || scene.path != ScenePath) return;
        foreach (var root in scene.GetRootGameObjects()) if (root.GetComponentInChildren<CandyBounceGarden>(true)) return;
        var obj = new GameObject("Candy Bounce Garden"); SceneManager.MoveGameObjectToScene(obj,scene); obj.AddComponent<CandyBounceGarden>();
    }
    void Awake()
    {
        var experience=FindFirstObjectByType<MarshmallowIslandExperience>();
        island=experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if (!island) { Debug.LogError("Candy Bounce Garden could not find island ground."); enabled=false; return; }
        transform.position=Ground(new Vector2(118,2070));
        var cream=Mat("Bounce vanilla icing",new Color(1,.91f,.90f));
        var gold=Mat("Bounce star gold",new Color(1,.83f,.23f),true);
        var wood=Mat("Bounce sign wood",new Color(.42f,.22f,.28f));
        starMesh=CreateStarMesh();
        for (int i=0;i<Sites.Length;i++) BuildPad(i,cream,gold);
        BuildSign(cream,wood,gold);
        var prefab=Resources.Load<GameObject>("SecondIslandResidents/Strawberry Friendly Marshmallow");
        if(prefab)
        {
            var friend=Instantiate(prefab,Ground(new Vector2(124,2059)),Quaternion.identity,transform);
            friend.name="Bounce garden cheering friend";
            var hit=friend.AddComponent<CapsuleCollider>();hit.center=Vector3.up;hit.radius=.52f;hit.height=2;
            friendVisual=friend.transform.Find("Bouncy visual");friendArm=friendVisual ? friendVisual.Find("Right arm pivot") : null;
        }
        BuildHud();
    }
    Vector3 Ground(Vector2 p)
    {
        var b=island.bounds;
        if(island.Raycast(new Ray(new Vector3(p.x,b.max.y+10,p.y),Vector3.down),out var h,b.size.y+20)) return h.point;
        throw new InvalidOperationException("Bounce Garden has no ground at "+p);
    }
    Material Mat(string name,Color color,bool glow=false)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")){name=name};
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);else m.color=color;
        if(glow && m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.35f);}
        materials.Add(m);return m;
    }
    static GameObject Shape(string name,PrimitiveType kind,Transform parent,Vector3 pos,Vector3 scale,Material material)
    {
        var o=GameObject.CreatePrimitive(kind);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;
        // Unity's primitive cylinder has a capsule collider: scaling it into a
        // flat pad makes the collider balloon vertically. Match the actual mesh.
        if(kind==PrimitiveType.Cylinder)
        {
            Destroy(o.GetComponent<Collider>());
            var mesh=o.AddComponent<MeshCollider>();mesh.sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;mesh.convex=true;
        }
        return o;
    }
    void BuildPad(int i,Material cream,Material gold)
    {
        var p=Sites[i];var ground=Ground(p);
        // Keep the top of each spring above the uneven ground, including its edges.
        foreach(var d in new[]{new Vector2(2.8f,0),new Vector2(-2.8f,0),new Vector2(0,2.8f),new Vector2(0,-2.8f)})
            ground.y=Mathf.Max(ground.y,Ground(p+d).y);
        var root=new GameObject("Candy spring "+(i+1)).transform;root.SetParent(transform,false);root.position=ground;
        BuildRamp(root,p,cream);
        Shape("Vanilla spring base",PrimitiveType.Cylinder,root,new Vector3(0,.16f,0),new Vector3(6.5f,.16f,6.5f),cream);
        Shape("Soft candy top",PrimitiveType.Cylinder,root,new Vector3(0,.40f,0),new Vector3(6f,.12f,6f),Mat("Spring "+i,PadColors[i]));
        for(int n=0;n<8;n++)
        {
            float a=n*Mathf.PI/4;var sprinkle=Shape("Sugar sprinkle",PrimitiveType.Sphere,root,new Vector3(Mathf.Cos(a)*2.35f,.55f,Mathf.Sin(a)*2.35f),new Vector3(.22f,.11f,.38f),n%2==0?gold:cream);
            sprinkle.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);sprinkle.GetComponent<Collider>().isTrigger=true;
        }
        pads.Add(root);
        var star=new GameObject("Bounce star "+(i+1));star.transform.SetParent(root,false);star.transform.localPosition=Vector3.up*6.8f;
        star.AddComponent<MeshFilter>().sharedMesh=starMesh;star.AddComponent<MeshRenderer>().sharedMaterial=gold;
        var trigger=star.AddComponent<SphereCollider>();trigger.radius=1.2f;trigger.isTrigger=true;
        star.SetActive(false);stars.Add(star.transform);
    }
    void BuildRamp(Transform root,Vector2 site,Material material)
    {
        // Blend each spring into the hills so it can be reached even with normal gravity.
        const int segments=24;var vertices=new Vector3[segments*2];var triangles=new int[segments*6];
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI*2/segments;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            vertices[i*2]=new Vector3(d.x*2.95f,PadTop+.015f,d.y*2.95f);
            float y=Ground(site+d*5f).y-root.position.y+.045f;
            vertices[i*2+1]=new Vector3(d.x*5f,y,d.y*5f);
            int j=(i+1)%segments,k=i*6;
            triangles[k]=i*2;triangles[k+1]=j*2;triangles[k+2]=i*2+1;
            triangles[k+3]=j*2;triangles[k+4]=j*2+1;triangles[k+5]=i*2+1;
        }
        var mesh=new Mesh{name="Candy spring ramp"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();rampMeshes.Add(mesh);
        var ramp=new GameObject("Icing approach ramp",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));ramp.transform.SetParent(root,false);
        ramp.GetComponent<MeshFilter>().sharedMesh=mesh;ramp.GetComponent<MeshRenderer>().sharedMaterial=material;ramp.GetComponent<MeshCollider>().sharedMesh=mesh;
    }
    static Mesh CreateStarMesh()
    {
        var verts=new Vector3[22];verts[0]=new Vector3(0,0,-.16f);verts[11]=new Vector3(0,0,.16f);
        for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5;float r=i%2==0?1.1f:.48f;var v=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);verts[1+i]=v+Vector3.back*.16f;verts[12+i]=v+Vector3.forward*.16f;}
        var tri=new List<int>();for(int i=0;i<10;i++){int j=(i+1)%10;tri.AddRange(new[]{0,1+j,1+i,11,12+i,12+j,1+i,1+j,12+j,1+i,12+j,12+i});}
        var mesh=new Mesh{name="Candy Bounce Star"};mesh.vertices=verts;mesh.triangles=tri.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    Transform Label(Transform parent,string text,float height,float scale)
    {
        SkyboardReward.Label(parent,text);var t=parent.Find("Pickup label");t.name="Bounce label";t.localPosition=Vector3.up*height;t.localScale=Vector3.one*scale;return t;
    }
    void BuildSign(Material cream,Material wood,Material gold)
    {
        sign=new GameObject("Bounce Garden sign").transform;sign.SetParent(transform,false);sign.position=Ground(new Vector2(116,2058));
        Shape("Left sign post",PrimitiveType.Cylinder,sign,new Vector3(-2.6f,1.7f,0),new Vector3(.3f,1.7f,.3f),wood);
        Shape("Right sign post",PrimitiveType.Cylinder,sign,new Vector3(2.6f,1.7f,0),new Vector3(.3f,1.7f,.3f),wood);
        Shape("Sign board",PrimitiveType.Cube,sign,new Vector3(0,3.6f,0),new Vector3(6.8f,2.7f,.4f),wood);
        Shape("Icing",PrimitiveType.Cube,sign,new Vector3(0,5,0),new Vector3(7,.2f,.5f),cream);
        Shape("Golden topper",PrimitiveType.Sphere,sign,new Vector3(0,5.3f,0),Vector3.one*.48f,gold);
        signLabel=Label(sign,"CANDY BOUNCE GARDEN\nStep on the sweets to bounce!\nCatch 3 stars · no time limit\nFirst win: scroll + moving stars",3.6f,.012f);
        signLabel.localPosition=new Vector3(0,3.6f,-.27f);
        ((RectTransform)signLabel).sizeDelta=new Vector2(550,180);
        signPrompt=Label(sign,"",6.2f,.017f);promptText=signPrompt.GetComponentInChildren<Text>();signPrompt.gameObject.SetActive(false);
    }
    void BuildHud()
    {
        var root=new GameObject("Bounce Garden HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform,false);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=82;GameUITheme.Scale(canvas);
        var card=GameUITheme.Rect(root.transform,"Bounce card",new Vector2(.5f,1),new Vector2(570,112));card.pivot=new Vector2(.5f,1);card.anchoredPosition=new Vector2(0,-24);
        GameUITheme.Image(card,new Color(.12f,.055f,.13f,.94f));
        hudText=GameUITheme.Label(card,"Bounce status",21,new Vector2(-265,-57),new Vector2(530,104),TextAnchor.MiddleLeft);
        hud=card.gameObject;hud.SetActive(false);
    }
    public void RestoreProgress(bool claimed){RewardClaimed=claimed;}
    void BeginGame(bool advanced)
    {
        Playing=true;Advanced=advanced && RewardClaimed;Collected=0;feedback="";nextHud=0;
        for(int i=0;i<stars.Count;i++){collected[i]=false;stars[i].localPosition=Vector3.up*6.8f;stars[i].gameObject.SetActive(true);}
    }
    void Update()
    {
        if(!player && Time.unscaledTime>=nextLookup)
        {
            nextLookup=Time.unscaledTime+1;player=FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player){body=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerHealth>();inventory=player.GetComponent<EquipmentInventory>();}
            board=FindFirstObjectByType<SkyboardReward>();scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
            melody=FindFirstObjectByType<CandyMelodyGarden>();
        }
        if(!player || !body)return;
        bool allowed=Time.timeScale>0 && body.enabled && player.isActiveAndEnabled && (!health || health.currentHealth>0) && !player.ExternalControlLock && !player.ExternalMovementLock && !GameUITheme.ModalOpen && !SecondIslandAwakening.BlocksInputFor(player) && !(board && board.Flying);
        float distance=Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up).magnitude;
        var cam=Camera.main;
        bool bySign=allowed && Vector3.ProjectOnPlane(player.transform.position-sign.position,Vector3.up).sqrMagnitude<16 && Mathf.Abs(body.bounds.min.y-sign.position.y)<4;
        if(signPrompt.gameObject.activeSelf!=bySign)signPrompt.gameObject.SetActive(bySign);
        if(bySign){if(cam)signPrompt.rotation=cam.transform.rotation;promptText.text=GameControls.Prompt(GameControls.Command.Interact)+(Playing?"  ·  RESTART":"  ·  PLAY")+(RewardClaimed?"\n"+GameControls.Prompt(GameControls.Command.Sprint)+" + "+GameControls.Prompt(GameControls.Command.Interact)+"  ·  MOVING STARS":"");}
        if(bySign && Cursor.lockState==CursorLockMode.Locked && GameControls.Down(GameControls.Command.Interact)){lastInteractionFrame=Time.frameCount;BeginGame(GameControls.Held(GameControls.Command.Sprint));}
        if(friendVisual){float t=Time.time*3;friendVisual.localPosition=Vector3.up*(distance<32?Mathf.Max(0,Mathf.Sin(t))*.13f:.015f*Mathf.Sin(t));if(friendArm)friendArm.localRotation=Quaternion.Euler(0,0,distance<32?75+Mathf.Sin(t*1.4f)*20:12);}
        for(int i=0;i<stars.Count;i++)if(stars[i].gameObject.activeSelf)
        {
            if(Advanced)stars[i].localPosition=new Vector3(1.2f*Mathf.Sin(Time.time*.7f+i*2),6.8f,1.2f*Mathf.Cos(Time.time*.7f+i*2));
            stars[i].Rotate(0,45*Time.deltaTime,0,Space.World);
        }
        if(allowed && distance<32)
        {
            if(Time.time>=nextBounce)
                for(int i=0;i<pads.Count;i++)
                {
                    var d=player.transform.position-pads[i].position;float feet=body.bounds.min.y-(pads[i].position.y+PadTop);
                    if(new Vector2(d.x,d.z).sqrMagnitude<2.8f*2.8f && feet>-.35f && feet<.95f && player.TryCandySpring(8.5f))
                    {nextBounce=Time.time+.8f;GameplayAudio.Play(GameplayAudio.Cue.Pickup,pads[i].position,true);break;}
                }
            if(Playing)for(int i=0;i<stars.Count;i++)
                // A little forgiving reach keeps a high bounce fun with both gravity modes.
                if(!collected[i] && body.bounds.SqrDistance(stars[i].position)<2.6f*2.6f)
                {
                    collected[i]=true;stars[i].gameObject.SetActive(false);Collected++;nextHud=0;
                    GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);
                    if(Collected==stars.Count)Complete();
                }
        }
        float melodyDistance=melody?Vector3.ProjectOnPlane(player.transform.position-melody.transform.position,Vector3.up).magnitude:float.PositiveInfinity;
        bool melodyPlaying=melody && (melody.CurrentStage==CandyMelodyGarden.Stage.Showing || melody.CurrentStage==CandyMelodyGarden.Stage.Listening);
        bool show=allowed && distance<30 && (Playing || (!melodyPlaying && distance<melodyDistance));
        if(hud.activeSelf!=show)hud.SetActive(show);
        if(show && Time.unscaledTime>=nextHud)
        {
            nextHud=Time.unscaledTime+.2f;
            hudText.text="CANDY BOUNCE GARDEN"+(Advanced && Playing?"  ·  MOVING STARS":"")+"\n"+(Time.unscaledTime<feedbackUntil?feedback:Playing?"Stars: "+Collected+" / 3  ·  Land on a candy spring to bounce!":RewardClaimed?"Play again, or hold Sprint at the sign for moving stars.":"Use the sign to play. Catch 3 stars; take your time.");
        }
    }
    void Complete()
    {
        Playing=false;bool awarded=false;
        if(!RewardClaimed && inventory && scroll){inventory.RestoreStack(scroll,inventory.Count(scroll)+1);RewardClaimed=true;awarded=true;}
        feedback=awarded?"Wonderful! You earned 1 speed scroll.":RewardClaimed?"Wonderful bouncing! Play again anytime.":"Stars caught! Visit again to claim your reward.";
        feedbackUntil=Time.unscaledTime+8;nextHud=0;
        if(awarded && !GameSaveManager.SaveCurrentGame())GameSaveManager.RequestAutosave();
    }
    void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);foreach(var mesh in rampMeshes)if(mesh)Destroy(mesh);if(starMesh)Destroy(starMesh);}
}
