using System.Collections;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>A small optional story: help two friendly islanders find each other.</summary>
[DisallowMultipleComponent]
public sealed class MarshmallowFriendsQuest : MonoBehaviour
{
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    static readonly Vector2 HomeSite=new Vector2(92,2010), LostSite=new Vector2(128,2020);
    public int Stage { get; private set; }
    public bool Talking { get; private set; }
    public Transform Home => home;
    public Transform Lost => lost;
    public Transform MapAnchor => Stage==1 || Stage==2 && player && lost && Flat(player.transform.position,lost.position)>28f ? lost : home;
    public bool IsNearHome => home && lost && Flat(home.position,lost.position)<10f;
    public bool ReservesInteraction => lastInteractionFrame==Time.frameCount || Talking || player && body && body.enabled &&
        (Near(home,4.5f) || (Stage==1 && Near(lost,4.5f)));
    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    EquipmentInventory inventory;
    EquipmentItem scroll;
    Collider island;
    Transform home,lost,homeVisual,lostVisual,homeArm,lostArm,prompt;
    Text promptText,cardText;
    Transform[] butterflies=new Transform[3];
    Material butterflyPink,butterflyBlue,butterflyBody;
    GameObject card;
    Canvas dialogueCanvas;
    SecondIslandDialogue activeDialogue;
    float nextLookup,nextGround,nextHud,noticeUntil;
    float lostGround;
    string notice="";
    int lastInteractionFrame=-1;
    bool ownsLock;
    bool followGroundValid=true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initial(){for(int i=0;i<SceneManager.sceneCount;i++)Ensure(SceneManager.GetSceneAt(i));}
    static void OnSceneLoaded(Scene scene,LoadSceneMode mode)=>Ensure(scene);
    static void Ensure(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<MarshmallowFriendsQuest>(true))return;
        var go=new GameObject("Marshmallow Friends Story");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<MarshmallowFriendsQuest>();
    }
    void Awake()
    {
        var experience=FindFirstObjectByType<MarshmallowIslandExperience>();island=experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if(!island){Debug.LogError("Marshmallow friends could not find the island ground.");enabled=false;return;}
        transform.position=Ground(HomeSite);
        home=MakeFriend("Mint","Lily - waiting friend",HomeSite,out homeVisual,out homeArm);
        lost=MakeFriend("Sky","Pip - lost friend",LostSite,out lostVisual,out lostArm);
        if(!home || !lost){enabled=false;return;}
        lostGround=lost.position.y;
        BuildButterflies();
        SkyboardReward.Label(transform,"");prompt=transform.Find("Pickup label");prompt.name="Friend story prompt";promptText=prompt.GetComponentInChildren<Text>();prompt.gameObject.SetActive(false);
        var ui=new GameObject("Friends story UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));ui.transform.SetParent(transform,false);
        dialogueCanvas=ui.GetComponent<Canvas>();dialogueCanvas.renderMode=RenderMode.ScreenSpaceOverlay;dialogueCanvas.sortingOrder=88;GameUITheme.Scale(dialogueCanvas);
        var panel=GameUITheme.Rect(ui.transform,"Friends story card",new Vector2(1,1),new Vector2(480,120));panel.pivot=Vector2.one;panel.anchoredPosition=new Vector2(-24,-195);
        GameUITheme.Image(panel,new Color(.09f,.045f,.12f,.93f));cardText=GameUITheme.Label(panel,"Story hint",20,new Vector2(-460,-58),new Vector2(440,105),TextAnchor.MiddleLeft);card=panel.gameObject;card.SetActive(false);
    }
    Vector3 Ground(Vector2 p)
    {
        if(TryGround(p,out var ground))return ground;
        throw new System.InvalidOperationException("Friend story has no island ground at "+p);
    }
    bool TryGround(Vector2 p,out Vector3 ground)
    {
        var b=island.bounds;if(island.Raycast(new Ray(new Vector3(p.x,b.max.y+10,p.y),Vector3.down),out var hit,b.size.y+20)){ground=hit.point+Vector3.up*.04f;return true;}
        ground=default;return false;
    }
    Transform MakeFriend(string color,string label,Vector2 site,out Transform visual,out Transform arm)
    {
        visual=arm=null;var prefab=Resources.Load<GameObject>("SecondIslandResidents/"+color+" Friendly Marshmallow");if(!prefab){Debug.LogError("Missing friendly resident: "+color);return null;}
        var obj=Instantiate(prefab,Ground(site),Quaternion.identity,transform);obj.name=label;
        var col=obj.AddComponent<CapsuleCollider>();col.center=Vector3.up;col.height=2;col.radius=.52f;
        visual=obj.transform.Find("Bouncy visual");if(visual)arm=visual.Find("Right arm pivot");return obj.transform;
    }
    void BuildButterflies()
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        butterflyPink=new Material(shader){color=new Color(1,.58f,.76f)};butterflyBlue=new Material(shader){color=new Color(.54f,.82f,1)};butterflyBody=new Material(shader){color=new Color(.33f,.23f,.35f)};
        for(int i=0;i<butterflies.Length;i++)
        {
            var root=new GameObject("Friendly garden butterfly "+(i+1)).transform;root.SetParent(home,false);butterflies[i]=root;
            for(int j=-1;j<=1;j++)
            {
                var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);part.name=j==0?"Butterfly body":"Butterfly wing";part.transform.SetParent(root,false);
                part.transform.localPosition=new Vector3(j*.18f,0,0);part.transform.localScale=j==0?new Vector3(.12f,.12f,.3f):new Vector3(.28f,.12f,.25f);
                part.GetComponent<Renderer>().sharedMaterial=j==0?butterflyBody:i%2==0?butterflyPink:butterflyBlue;part.GetComponent<Collider>().isTrigger=true;
            }
        }
    }
    static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
    bool Near(Transform t,float radius)=>t && player && body && Flat(t.position,player.transform.position)<radius && Mathf.Abs(body.bounds.min.y-t.position.y)<5;
    public void RestoreProgress(int stage)
    {
        Stage=Mathf.Clamp(stage,0,3);
        if(Stage==3 && lost && home){var p=home.position+new Vector3(2.5f,0,0);lost.position=Ground(new Vector2(p.x,p.z));lostGround=lost.position.y;}
        else if(lost){lost.position=Ground(LostSite);lostGround=lost.position.y;}
        nextHud=0;
    }
    void Update()
    {
        if(!player && Time.unscaledTime>=nextLookup)
        {
            nextLookup=Time.unscaledTime+1;player=FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player){body=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerHealth>();inventory=player.GetComponent<EquipmentInventory>();}
            scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
        }
        if(!player || !body || !home || !lost)return;
        if(Talking && (!player.isActiveAndEnabled || !body.enabled || health && health.currentHealth<=0)){StopAllCoroutines();EndTalk();}
        bool allowed=Time.timeScale>0 && body.enabled && player.isActiveAndEnabled && (!health || health.currentHealth>0) && !player.ExternalControlLock && !player.ExternalMovementLock && !GameUITheme.ModalOpen && !SecondIslandAwakening.BlocksInputFor(player) && !MarshmallowChallengeGuide.AnyConversation;
        float homeDistance=Flat(player.transform.position,home.position),lostDistance=Flat(player.transform.position,lost.position);
        bool walking=Stage==2 && allowed && lostDistance>3.5f && lostDistance<28f && !IsNearHome;
        if(walking)
        {
            var dir=Vector3.ProjectOnPlane(player.transform.position-lost.position,Vector3.up).normalized;
            var next=lost.position+dir*(Mathf.Min(lostDistance-3f,5.4f*Time.deltaTime));
            if(Time.time>=nextGround)
            {
                nextGround=Time.time+.12f;
                followGroundValid=TryGround(new Vector2(next.x,next.z),out var ground);
                if(followGroundValid)lostGround=ground.y;
            }
            if(!followGroundValid)next=lost.position;
            next.y=lostGround;lost.position=next;if(dir.sqrMagnitude>.001f)lost.rotation=Quaternion.Slerp(lost.rotation,Quaternion.LookRotation(dir),Mathf.Min(1,Time.deltaTime*5));
        }
        Animate(homeVisual,homeArm,homeDistance,0,false);Animate(lostVisual,lostArm,lostDistance,1.8f,walking);
        if(homeDistance<55f)for(int i=0;i<butterflies.Length;i++)if(butterflies[i])
        {
            float t=Time.time*.65f+i*2.1f;butterflies[i].localPosition=new Vector3(Mathf.Cos(t)*(2.7f+i*.55f),2.5f+Mathf.Sin(t*1.7f)*.35f,Mathf.Sin(t)*(2.7f+i*.55f));
            butterflies[i].localRotation=Quaternion.Euler(0,-t*Mathf.Rad2Deg,0);
            for(int wing=0;wing<3;wing+=2)butterflies[i].GetChild(wing).localRotation=Quaternion.Euler(0,0,(wing==0?1:-1)*Mathf.Sin(t*9)*45);
        }
        bool nearHome=allowed && Near(home,4.5f),nearLost=allowed && Stage==1 && Near(lost,4.5f);
        var who=nearHome?home:nearLost?lost:null;var cam=Camera.main;
        if(prompt.gameObject.activeSelf!=(who!=null))prompt.gameObject.SetActive(who!=null);
        if(who){prompt.position=who.position+Vector3.up*3.6f;if(cam)prompt.rotation=cam.transform.rotation;promptText.text=GameControls.Prompt(GameControls.Command.Interact)+"  ·  TALK TO "+(who==home?"LILY":"PIP");}
        bool show=allowed && (Stage==1 || Stage==2 || Time.unscaledTime<noticeUntil) && Mathf.Min(homeDistance,lostDistance)<75f;
        if(card.activeSelf!=show)card.SetActive(show);
        if(show && Time.unscaledTime>=nextHud)
        {
            nextHud=Time.unscaledTime+.25f;var target=MapAnchor;
            string hint=Stage==1?"Find Pip, the blue marshmallow.":IsNearHome?"Talk to Lily to reunite the friends.":lostDistance>28?"Pip is waiting for you. Go back to help.":"Lead Pip back to Lily. Pip follows nearby.";
            cardText.text="A FRIEND IN THE CLOUDS\n"+(Time.unscaledTime<noticeUntil?notice:hint)+"\n"+Mathf.RoundToInt(Flat(player.transform.position,target.position))+" m  ·  See M for map";
        }
        if(!allowed || Cursor.lockState!=CursorLockMode.Locked || !GameControls.Down(GameControls.Command.Interact))return;
        if(nearHome || nearLost){lastInteractionFrame=Time.frameCount;StartCoroutine(Talk(nearHome));}
    }
    static void Animate(Transform visual,Transform arm,float distance,float phase,bool walking)
    {
        float t=Time.time*3+phase;if(visual)visual.localPosition=Vector3.up*(distance<12?Mathf.Max(0,Mathf.Sin(t*(walking?2:1)))*.1f:.015f*Mathf.Sin(t));
        if(arm)arm.localRotation=Quaternion.Euler(0,0,distance<12?70+20*Mathf.Sin(t*1.5f):12);
    }
    IEnumerator Talk(bool atHome)
    {
        if(Talking)yield break;
        if(atHome && Stage==2 && !IsNearHome){notice="Pip is still on the way. Stay close to help.";noticeUntil=Time.unscaledTime+5;nextHud=0;yield break;}
        Talking=true;ownsLock=true;player.ExternalControlLock=true;player.ExternalMovementLock=true;
        SecondIslandDialogue.Line[] lines;
        if(atHome && Stage==0)lines=new[]{new SecondIslandDialogue.Line(true,"My friend Pip went to look at the clouds, and hasn't come back. Could you help me find them?"),new SecondIslandDialogue.Line(false,"Of course. I'll look for a blue marshmallow and bring Pip home.")};
        else if(!atHome && Stage==1)lines=new[]{new SecondIslandDialogue.Line(true,"I was watching the clouds and lost my way. Is Lily looking for me?"),new SecondIslandDialogue.Line(false,"Yes! Stay close and I'll lead you back to Lily.")};
        else if(atHome && Stage==2)lines=new[]{new SecondIslandDialogue.Line(true,"Pip! I was so worried. Thank you for bringing my friend home!"),new SecondIslandDialogue.Line(false,"You're welcome. It's good to see you together again.")};
        else lines=new[]{new SecondIslandDialogue.Line(true,Stage==3?"We're together again! Thank you, Sahur.":"Pip is a blue marshmallow. Please help me find them.")};
        activeDialogue=gameObject.AddComponent<SecondIslandDialogue>();yield return activeDialogue.Play(dialogueCanvas.transform,lines,atHome?"LILY":"PIP");
        if(atHome && Stage==0)Stage=1;else if(!atHome && Stage==1)Stage=2;else if(atHome && Stage==2)
        {
            if(inventory && scroll){inventory.RestoreStack(scroll,inventory.Count(scroll)+1);Stage=3;notice="Friends reunited! You received 1 speed scroll.";noticeUntil=Time.unscaledTime+8;}
        }
        EndTalk();GameSaveManager.RequestAutosave();
    }
    void EndTalk(){Talking=false;if(activeDialogue)Destroy(activeDialogue);activeDialogue=null;if(ownsLock && player){player.ExternalControlLock=false;player.ExternalMovementLock=false;}ownsLock=false;}
    void OnDisable(){if(Talking){StopAllCoroutines();EndTalk();}}
    void OnDestroy(){if(butterflyPink)Destroy(butterflyPink);if(butterflyBlue)Destroy(butterflyBlue);if(butterflyBody)Destroy(butterflyBody);}
}
