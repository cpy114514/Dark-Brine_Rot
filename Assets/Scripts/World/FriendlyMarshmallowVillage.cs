using System.Collections;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>A peaceful neighbourhood and a conversation-unlocked pocket of the second island.</summary>
[DisallowMultipleComponent]
public sealed class FriendlyMarshmallowVillage : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    static readonly Vector3 GroveCenter = new Vector3(40f, 3000f, 1980f);
    static readonly Vector2 GuideHome = new Vector2(70f, 1940f);
    static readonly Vector2 GateSite = new Vector2(40f, 1980f);
    static readonly Vector2 LightSite = new Vector2(20f, 2025f);
    const float SmallFactor = .38f;
    public bool Unlocked { get; private set; }
    public bool Rested { get; private set; }
    public bool DoorInspected { get; private set; }
    public bool SmallLightOwned { get; private set; }
    public bool IsSmall => isSmall;
    public bool Leading { get; private set; }
    public bool Talking { get; private set; }
    public Transform Guide => residents.Count > 0 ? residents[0].root : null;
    public Transform Entrance => entrance ? entrance.transform : null;
    public Transform Grove => grove ? grove.transform : null;
    public Transform SmallLight => smallLight ? smallLight.transform : null;
    public bool InGrove { get; private set; }
    // F is also used by the Skyboard. Reserve it before either Update runs,
    // and for the rest of the frame after an interaction teleports the player.
    public bool ReservesInteraction
    {
        get
        {
            if(lastInteractionFrame==Time.frameCount)return true;
            if(!player || !body || !body.enabled)return false;
            if(InGrove || Talking)return true;
            if(Guide && FlatDistance(player.transform.position,Guide.position)<5f && Mathf.Abs(body.bounds.min.y-Guide.position.y)<4f)return true;
            if(Unlocked && entrance && FlatDistance(player.transform.position,entrance.transform.position)<(!DoorInspected?8f:5f) && Mathf.Abs(body.bounds.min.y-entrance.transform.position.y)<5f)return true;
            return smallLight && smallLight.activeSelf && FlatDistance(player.transform.position,smallLight.transform.position)<4.5f && Mathf.Abs(body.bounds.min.y-smallLight.transform.position.y)<5f;
        }
    }

    sealed class Resident
    {
        public Transform root, visual, leftArm, rightArm, leftLeg, rightLeg;
        public Vector3 origin;
        public float phase;
    }
    readonly List<Resident> residents = new List<Resident>();
    readonly List<Material> materials = new List<Material>();
    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    Collider island;
    GameObject entrance, grove, entranceLabel, exitLabel, residentLabel, guideLandmark, fountainLabel, screen, smallLight, lightLabel, lightLandmark, questCard, shrinkEffect, shrinkActor;
    Text residentText, entranceText, exitText, fountainText, lightText, questText;
    Canvas conversationCanvas;
    Material shrinkMaterial;
    GameObject portraitStudio;
    RenderTexture portraitTexture;
    Transform portraitArm, portraitVisual;
    bool ownsLocks, isSmall, sizing, originalSettingsCaptured, leadingToLight;
    bool guideHiddenForShrink;
    Vector3 originalScale;
    float originalMove, originalJump, originalRoll, originalAcceleration, originalDeceleration, originalCameraDistance, originalCameraHeight;
    float originalSoftJump, originalWalkBound, originalSprintBound;
    float nextSearch, portalReady, messageUntil;
    int lastInteractionFrame=-1;
    Text message;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene()
    { for(int i=0;i<SceneManager.sceneCount;i++) Ensure(SceneManager.GetSceneAt(i)); }
    static void SceneLoaded(Scene scene, LoadSceneMode mode) => Ensure(scene);
    static void Ensure(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath) return;
        foreach(var root in scene.GetRootGameObjects())
            if(root.GetComponentInChildren<FriendlyMarshmallowVillage>(true)) return;
        var obj=new GameObject("Friendly Marshmallow Village");
        SceneManager.MoveGameObjectToScene(obj,scene);
        obj.AddComponent<FriendlyMarshmallowVillage>();
    }

    void Awake()
    {
        var experience=FindFirstObjectByType<MarshmallowIslandExperience>();
        island=experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if(!island) { Debug.LogError("Friendly marshmallows could not find the island ground."); enabled=false; return; }
        MakeResident("Strawberry", Ground(GuideHome), 0f, transform);
        MakeResident("Mint", Ground(new Vector2(50f,1920f)), 1.5f, transform);
        MakeResident("Sky", Ground(new Vector2(115f,1960f)), 3f, transform);
        entrance=MakeGate("Hidden grove entrance", Ground(GateSite), transform);
        entrance.transform.localScale=Vector3.one*.58f;
        entranceLabel=MakeLabel(entrance.transform, out entranceText, 6.2f);
        entranceLabel.transform.localScale=Vector3.one*(.012f/.58f);
        residentLabel=MakeLabel(Guide, out residentText, 3.0f);
        guideLandmark=MakeLabel(Guide,out var landmarkText,4.5f);
        landmarkText.text="FRIENDLY\nMARSHMALLOW";
        landmarkText.color=new Color(1f,.9f,.65f);
        MakeSmallLight();
        MakeGrove();
        BuildMessage();
        ApplyProgress();
    }
    Vector3 Ground(Vector2 point)
    {
        var b=island.bounds;
        return island.Raycast(new Ray(new Vector3(point.x,b.max.y+10f,point.y),Vector3.down),out var hit,b.size.y+20f)
            ? hit.point + Vector3.up*.05f : new Vector3(point.x,2518f,point.y);
    }
    void MakeResident(string color, Vector3 position, float phase, Transform parent)
    {
        var prefab=Resources.Load<GameObject>("SecondIslandResidents/"+color+" Friendly Marshmallow");
        if(!prefab) { Debug.LogError("Missing friendly marshmallow prefab: "+color); return; }
        var obj=Instantiate(prefab,position,Quaternion.identity,parent);
        obj.name=color+" friendly resident";
        var residentCollider=obj.AddComponent<CapsuleCollider>();residentCollider.center=new Vector3(0,1,0);residentCollider.height=2f;residentCollider.radius=.52f;
        var visual=obj.transform.Find("Bouncy visual");
        residents.Add(new Resident { root=obj.transform, visual=visual, origin=position, phase=phase,
            leftArm=visual.Find("Left arm pivot"),rightArm=visual.Find("Right arm pivot"),
            leftLeg=visual.Find("Left leg pivot"),rightLeg=visual.Find("Right leg pivot") });
    }
    Material Mat(string name, Color color, bool glow=false)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m=new Material(shader){name=name};
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color); else m.color=color;
        if(glow && m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.45f);}
        materials.Add(m);return m;
    }
    GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid=true)
    {
        var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=position;obj.transform.localScale=scale;
        obj.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)obj.GetComponent<Collider>().isTrigger=true;
        return obj;
    }
    GameObject MakeGate(string name, Vector3 position, Transform parent)
    {
        var gate=new GameObject(name);gate.transform.SetParent(parent,false);gate.transform.position=position;
        var frame=Mat(name+" sugar",new Color(1f,.72f,.83f));
        var glow=Mat(name+" light",new Color(.53f,.92f,.93f),true);
        Shape("Left candy pillar",PrimitiveType.Cylinder,gate.transform,new Vector3(-1.8f,2.1f,0),new Vector3(.55f,2.1f,.55f),frame);
        Shape("Right candy pillar",PrimitiveType.Cylinder,gate.transform,new Vector3(1.8f,2.1f,0),new Vector3(.55f,2.1f,.55f),frame);
        Shape("Sugar arch",PrimitiveType.Cube,gate.transform,new Vector3(0,4.2f,0),new Vector3(4.1f,.48f,.55f),frame);
        Shape("Shimmering doorway",PrimitiveType.Cube,gate.transform,new Vector3(0,2.05f,0),new Vector3(3.05f,3.8f,.13f),glow,false);
        var gold=Mat(name+" sugar pearls",new Color(1f,.87f,.52f),true);
        for(int i=0;i<7;i++)
            Shape("Arch sugar pearl",PrimitiveType.Sphere,gate.transform,new Vector3(-1.65f+i*.55f,4.48f,-.02f),Vector3.one*.24f,gold);
        foreach(int side in new[]{-1,1})
            Shape("Gate finial",PrimitiveType.Sphere,gate.transform,new Vector3(side*1.8f,4.55f,0),Vector3.one*.5f,gold);
        return gate;
    }
    GameObject MakeLabel(Transform parent,out Text text,float height)
    {
        SkyboardReward.Label(parent,"");var t=parent.Find("Pickup label");
        t.name="Resident interaction label";t.localPosition=Vector3.up*height;text=t.GetComponentInChildren<Text>();
        if(text)text.fontSize=27;t.gameObject.SetActive(false);return t.gameObject;
    }
    // A handcrafted Small Light based on the Doraemon gadget: green grip,
    // blue head, yellow studs, red/yellow controls and a glowing lens.
    void MakeSmallLight()
    {
        smallLight=new GameObject("Doraemon Small Light pickup");smallLight.transform.SetParent(transform,false);smallLight.transform.position=Ground(LightSite);
        var mint=Mat("Small Light green",new Color(.38f,.86f,.25f));
        var blue=Mat("Small Light blue",new Color(.08f,.37f,.84f));
        var yellow=Mat("Small Light yellow",new Color(1f,.82f,.12f));
        var red=Mat("Small Light red",new Color(.96f,.15f,.13f));
        var pearl=Mat("Small Light lens",new Color(.85f,1f,1f),true);
        var pedestal=Mat("Small Light pedestal",new Color(.97f,.70f,.78f));
        Shape("Pickup pedestal",PrimitiveType.Cylinder,smallLight.transform,new Vector3(0,.32f,0),new Vector3(2,.32f,2),pedestal);
        Shape("Green handle",PrimitiveType.Cylinder,smallLight.transform,new Vector3(0,1.55f,0),new Vector3(.68f,.82f,.68f),mint);
        Shape("Yellow base",PrimitiveType.Cylinder,smallLight.transform,new Vector3(0,.78f,0),new Vector3(.76f,.10f,.76f),yellow);
        Shape("Blue lamp head",PrimitiveType.Cylinder,smallLight.transform,new Vector3(0,2.48f,0),new Vector3(1.35f,.20f,1.35f),blue);
        Shape("Glass lens",PrimitiveType.Sphere,smallLight.transform,new Vector3(0,2.70f,0),new Vector3(1.1f,.50f,1.1f),pearl);
        Shape("Red switch",PrimitiveType.Sphere,smallLight.transform,new Vector3(-.15f,1.75f,-.34f),Vector3.one*.20f,red);
        Shape("Yellow switch",PrimitiveType.Sphere,smallLight.transform,new Vector3(.15f,1.75f,-.34f),Vector3.one*.20f,yellow);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4f;
            Shape("Yellow head stud",PrimitiveType.Sphere,smallLight.transform,new Vector3(Mathf.Cos(a)*.60f,2.49f,Mathf.Sin(a)*.60f),Vector3.one*.17f,yellow);
        }
        lightLabel=MakeLabel(smallLight.transform,out lightText,3.8f);
        lightLandmark=MakeLabel(smallLight.transform,out var marker,6f);
        marker.text="!  SMALL LIGHT";marker.color=new Color(1f,.9f,.45f);
    }
    void MakeGrove()
    {
        grove=new GameObject("Secret Starlight Grove");grove.transform.SetParent(transform,false);grove.transform.position=GroveCenter;
        var ground=Mat("Grove rose clay",new Color(.66f,.49f,.50f));
        var edge=Mat("Grove cream stone",new Color(.84f,.72f,.59f));
        Shape("Solid sugar island",PrimitiveType.Cube,grove.transform,new Vector3(0,-.6f,0),new Vector3(36,1.2f,34),ground,true);
        // Low rails make the boundary visible. A recovery check catches low-gravity jumps beyond it.
        foreach(int side in new[]{-1,1})
        {
            Shape("Icing rail",PrimitiveType.Cube,grove.transform,new Vector3(side*17.4f,.65f,0),new Vector3(.55f,1.3f,34),edge,true);
            Shape("Icing rail",PrimitiveType.Cube,grove.transform,new Vector3(0,.65f,side*16.4f),new Vector3(35,1.3f,.55f),edge,true);
        }
        var swirl = SecondIslandProp.Load("Kenney/lollypop");
        var cupcake = SecondIslandProp.Load("Kenney/cupcake");
        var wafer = SecondIslandProp.Load("Kenney/waffle");
        var bench = SecondIslandProp.Load("Quaternius/Bench");
        var well = SecondIslandProp.Load("Quaternius/Well");
        for(int i=0;i<7;i++)
        {
            float a=(i+.35f)*Mathf.PI*2/7f;
            var site = new Vector3(Mathf.Cos(a)*13.5f,0,Mathf.Sin(a)*12f);
            PlaceGroveProp(i%3==1 ? cupcake : swirl, site, new Vector3(4.5f,4.6f,4.5f), i * 31f);
        }
        for(int i=0;i<7;i++)
        {
            var site = new Vector3(Mathf.Sin(i*.9f)*.6f,0,-8+i*1.6f);
            PlaceGroveProp(wafer, site, new Vector3(1.4f,.12f,1.4f), (i%3-1)*12f, true);
        }
        foreach(int side in new[]{-1,1})
            PlaceGroveProp(bench, new Vector3(side*8,0,2), new Vector3(3.4f,1.65f,3.4f), -side*90);
        if (well)
            PlaceGroveProp(well, new Vector3(0,0,4), new Vector3(3.8f,3.25f,3.8f), 0);
        else
            Shape("Resting fountain basin",PrimitiveType.Cylinder,grove.transform,new Vector3(0,.45f,4),new Vector3(3.8f,.45f,3.8f),edge);
        var fountain=new GameObject("Resting fountain");fountain.transform.SetParent(grove.transform,false);fountain.transform.localPosition=new Vector3(0,1,4);
        fountainLabel=MakeLabel(fountain.transform,out fountainText,2.65f);
        var exit=MakeGate("Return to Marshmallow Island",GroveCenter+new Vector3(0,.05f,-12),grove.transform);
        exitLabel=MakeLabel(exit.transform,out exitText,4.8f);
        MakeResident("Mint",GroveCenter+new Vector3(5,.05f,7),4.7f,grove.transform);
    }

    void PlaceGroveProp(GameObject asset, Vector3 site, Vector3 size, float yaw, bool stretch = false)
    {
        if (!asset) return;
        var root = new GameObject(asset.name + " fixture").transform;
        root.SetParent(grove.transform, false);
        root.localPosition = site;
        var model = SecondIslandProp.Mount(asset, root, Vector3.zero, size, Quaternion.Euler(0,yaw,0), stretch);
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        var lift = Vector3.up * (root.position.y - bounds.min.y);
        model.position += lift;
        var collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = root.InverseTransformPoint(bounds.center + lift);
        collider.size = bounds.size;
    }
    void BuildMessage()
    {
        var obj=new GameObject("Grove discovery message",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));obj.transform.SetParent(transform,false);
        var canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=90;GameUITheme.Scale(canvas);
        message=GameUITheme.Label(obj.transform,"Message",25,new Vector2(0,250),new Vector2(820,60),TextAnchor.MiddleCenter);
        message.gameObject.SetActive(false);
        var card=GameUITheme.Rect(obj.transform,"Grove information",new Vector2(1,1),new Vector2(510,150));
        card.pivot=new Vector2(1,1);card.anchoredPosition=new Vector2(-25,-25);
        GameUITheme.Image(card,new Color(.10f,.045f,.12f,.94f));
        questText=GameUITheme.Label(card,"Grove guide",21,Vector2.zero,new Vector2(470,130),TextAnchor.MiddleLeft);
        questText.rectTransform.anchorMin=questText.rectTransform.anchorMax=new Vector2(.5f,.5f);
        questText.rectTransform.pivot=new Vector2(.5f,.5f);
        questCard=card.gameObject;questCard.SetActive(false);
    }
    void Notify(string value) { if(!message)return;message.text=value;messageUntil=Time.unscaledTime+5f;message.gameObject.SetActive(true); }
    public void RestoreProgress(bool unlocked,bool rested,bool inspected=false,bool owned=false)
    { Unlocked=unlocked;Rested=rested && unlocked;SmallLightOwned=owned;DoorInspected=inspected||owned;ApplyProgress(); }
    void ApplyProgress()
    { if(entrance)entrance.SetActive(Unlocked);if(grove)grove.SetActive(Unlocked);if(smallLight)smallLight.SetActive(Unlocked && DoorInspected && !SmallLightOwned); }

    void CaptureSettings()
    {
        if(originalSettingsCaptured || !player)return;
        originalSettingsCaptured=true;originalScale=player.transform.localScale;
        originalMove=player.moveSpeed;originalJump=player.jumpHeight;originalRoll=player.rollSpeed;
        originalAcceleration=player.acceleration;originalDeceleration=player.deceleration;
        originalCameraDistance=player.cameraDistance;originalCameraHeight=player.cameraHeight;
        var soft=player.GetComponent<MarshmallowTraversal>();
        if(soft){originalSoftJump=soft.jumpHeight;originalWalkBound=soft.walkingBoundHeight;originalSprintBound=soft.sprintingBoundHeight;}
    }
    void SetSmall(bool small,bool preserveFeet=true)
    {
        if(!player || !body || isSmall==small)return;
        CaptureSettings();
        // Bounds can lag behind a long teleport by a physics step. Calculate
        // the capsule's sole from the current pose before changing its scale.
        float oldFeet=player.transform.position.y+(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        bool wasEnabled=body.enabled;if(wasEnabled)body.enabled=false;
        player.transform.localScale=originalScale*(small?SmallFactor:1f);
        if(wasEnabled)body.enabled=true;
        if(preserveFeet && wasEnabled)
        {
            float newFeet=player.transform.position.y+(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
            player.transform.position+=Vector3.up*(oldFeet-newFeet);
            Physics.SyncTransforms();
        }
        float speed=small?.58f:1f;
        player.moveSpeed=originalMove*speed;player.rollSpeed=originalRoll*speed;
        player.acceleration=originalAcceleration*speed;player.deceleration=originalDeceleration*speed;
        player.jumpHeight=originalJump*(small?.42f:1f);
        player.cameraDistance=small?Mathf.Max(2.5f,originalCameraDistance*.55f):originalCameraDistance;
        player.cameraHeight=originalCameraHeight*(small?SmallFactor:1f);
        var soft=player.GetComponent<MarshmallowTraversal>();
        if(soft){soft.jumpHeight=originalSoftJump*(small?.42f:1f);soft.walkingBoundHeight=originalWalkBound*(small?.42f:1f);soft.sprintingBoundHeight=originalSprintBound*(small?.42f:1f);}
        isSmall=small;
    }

    void Update()
    {
        if(conversationCanvas)conversationCanvas.enabled=!GameUITheme.ModalOpen;
        if(portraitArm)portraitArm.localRotation=Quaternion.Euler(0,0,-95f+Mathf.Sin(Time.unscaledTime*7f)*25f);
        if(portraitVisual)portraitVisual.localPosition=Vector3.up*(Mathf.Max(0,Mathf.Sin(Time.unscaledTime*4f))*.08f);
        if(message && message.gameObject.activeSelf && Time.unscaledTime>=messageUntil)message.gameObject.SetActive(false);
        if(!player && Time.unscaledTime>=nextSearch)
        {
            nextSearch=Time.unscaledTime+1;player=FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player){body=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerHealth>();CaptureSettings();}
        }
        if(!player || !body || residents.Count<4)return;
        if(Talking && (!player.isActiveAndEnabled || !body.enabled || health && health.currentHealth<=0))
        {StopAllCoroutines();EndConversation();}
        bool allowed=Time.timeScale>0 && body.enabled && player.isActiveAndEnabled && (!health || health.currentHealth>0) &&
            !player.ExternalControlLock && !player.ExternalMovementLock && !GameUITheme.ModalOpen &&
            !SecondIslandAwakening.BlocksInputFor(player) && !MarshmallowChallengeGuide.AnyConversation;
        if(Unlocked && player.transform.position.y>GroveCenter.y-15 && Vector3.Distance(player.transform.position,GroveCenter)<80) InGrove=true;
        // A respawn or another scene system may deliberately move Sahur back to
        // the island. Do not mistake that large relocation for a grove fall.
        if(InGrove && (player.transform.position.y<GroveCenter.y-100 || FlatDistance(player.transform.position,GroveCenter)>100)) InGrove=false;
        // Continue can restore Sahur inside the grove before this component's
        // first update. His saved position already corresponds to the small body.
        if(InGrove && !isSmall && !sizing)SetSmall(true,false);
        else if(!InGrove && isSmall && !sizing)SetSmall(false);
        if(InGrove && allowed && (player.transform.position.y<GroveCenter.y-5 || Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-GroveCenter,Vector3.up),Vector3.zero)>32))
        { Travel(true);Notify("The grove's magic brought you back safely."); }
        AnimateResidents(allowed);
        var cam=Camera.main;
        float guideDistance=FlatDistance(player.transform.position,Guide.position);
        bool nearGuide=allowed && !InGrove && guideDistance<5f && Mathf.Abs(body.bounds.min.y-Guide.position.y)<4f;
        Label(guideLandmark,allowed && !InGrove && guideDistance>=8f && guideDistance<55f,cam);
        bool nearEntrance=allowed && Unlocked && !InGrove && FlatDistance(player.transform.position,entrance.transform.position)<5f && Mathf.Abs(body.bounds.min.y-entrance.transform.position.y)<5f;
        bool approachesNewDoor=allowed && Unlocked && !InGrove && !DoorInspected && !SmallLightOwned && body.isGrounded &&
            FlatDistance(player.transform.position,entrance.transform.position)<8f && Mathf.Abs(body.bounds.min.y-entrance.transform.position.y)<5f;
        bool nearExit=allowed && InGrove && FlatDistance(player.transform.position,exitLabel.transform.parent.position)<5f;
        Label(entranceLabel,nearEntrance,cam);Label(exitLabel,nearExit,cam);
        if(nearEntrance)entranceText.text=SmallLightOwned ? GameControls.Prompt(GameControls.Command.Interact)+"  ·  USE SMALL LIGHT & ENTER" : "FIND THE SMALL LIGHT FIRST";
        if(nearExit)exitText.text=GameControls.Prompt(GameControls.Command.Interact)+"  ·  RETURN TO THE ISLAND";
        bool nearFountain=allowed && InGrove && FlatDistance(player.transform.position,GroveCenter+new Vector3(0,0,4))<5f;
        Label(fountainLabel,nearFountain,cam);
        if(nearFountain)fountainText.text=Rested ? "A PEACEFUL PLACE TO REST" : GameControls.Prompt(GameControls.Command.Interact)+"  ·  REST BY THE WELL";
        float lampDistance=smallLight ? FlatDistance(player.transform.position,smallLight.transform.position) : 999f;
        bool nearLight=allowed && smallLight && smallLight.activeSelf && lampDistance<4.5f && Mathf.Abs(body.bounds.min.y-smallLight.transform.position.y)<5f;
        Label(residentLabel,nearGuide && !nearEntrance && !nearLight,cam);if(nearGuide)residentText.text=GameControls.Prompt(GameControls.Command.Interact)+"  ·  TALK TO MARSHMALLOW";
        Label(lightLabel,nearLight,cam);if(nearLight)lightText.text=GameControls.Prompt(GameControls.Command.Interact)+"  ·  PICK UP SMALL LIGHT";
        Label(lightLandmark,allowed && smallLight && smallLight.activeSelf && lampDistance>=6f && lampDistance<90f,cam);
        bool showCard=allowed && Unlocked && (InGrove || DoorInspected && !SmallLightOwned || nearEntrance);
        if(questCard && questCard.activeSelf!=showCard)questCard.SetActive(showCard);
        if(showCard)
        {
            if(InGrove)questText.text="STARLIGHT GROVE\nA peaceful tiny garden above the clouds.\nRest by the well to restore 40% HP and full SP once.\nUse the return door to grow back.";
            else if(!DoorInspected)questText.text="STARLIGHT GROVE\nA hidden garden for tiny visitors.\nApproach the little door to learn how to enter.";
            else if(!SmallLightOwned)questText.text="FIND THE SMALL LIGHT\nThe door is too small for Sahur.\nLook northwest of the door for the light.\nDistance: "+Mathf.RoundToInt(lampDistance)+" m";
            else questText.text="STARLIGHT GROVE\nUse the Small Light at the tiny door.\nInside is a peaceful garden and a healing well.";
        }
        if(approachesNewDoor && Time.time>=portalReady)
        { StartCoroutine(InspectDoor());return; }
        if(!allowed || Cursor.lockState!=CursorLockMode.Locked || !GameControls.Down(GameControls.Command.Interact) || Time.time<portalReady)return;
        if(nearGuide || nearEntrance || nearExit || nearFountain && !Rested || nearLight)lastInteractionFrame=Time.frameCount;
        if(nearEntrance && !SmallLightOwned && !DoorInspected)StartCoroutine(InspectDoor());
        else if(nearEntrance && !SmallLightOwned)Notify("Find the Small Light northwest of the door.");
        else if(nearEntrance && body.isGrounded)StartCoroutine(ShrinkAndEnter());
        else if(nearExit)Travel(false);
        else if(nearFountain && !Rested)Rest();
        else if(nearLight)CollectLight();
        else if(nearGuide && body.isGrounded && !CandyHuntQuest.InteractionConsumedThisFrame)StartCoroutine(Conversation());
    }
    static float FlatDistance(Vector3 a,Vector3 b) { a.y=b.y=0;return Vector3.Distance(a,b); }
    static void Label(GameObject obj,bool show,Camera cam)
    { if(!obj)return;if(obj.activeSelf!=show)obj.SetActive(show);if(show && cam)obj.transform.rotation=cam.transform.rotation; }
    void AnimateResidents(bool allowed)
    {
        for(int i=0;i<residents.Count;i++)
        {
            var r=residents[i];if(!r.root.gameObject.activeInHierarchy)continue;
            float distance=FlatDistance(player.transform.position,r.root.position);
            bool nearby=allowed && distance<10f && Mathf.Abs(player.transform.position.y-r.root.position.y)<8f;
            var guideTarget=leadingToLight && smallLight ? smallLight.transform.position : entrance.transform.position;
            bool walking=i==0 && Leading && allowed && !InGrove && distance<16f && FlatDistance(r.root.position,guideTarget)>3f;
            if(walking)
            {
                var dest=guideTarget;var next=Vector3.MoveTowards(r.root.position,dest,2.5f*Time.deltaTime);
                r.root.position=Ground(new Vector2(next.x,next.z));
                var direction=dest-r.root.position;direction.y=0;if(direction.sqrMagnitude>.01f)r.root.rotation=Quaternion.Slerp(r.root.rotation,Quaternion.LookRotation(direction),Time.deltaTime*5);
            }
            else if(nearby)
            {
                var direction=player.transform.position-r.root.position;direction.y=0;
                if(direction.sqrMagnitude>.01f)r.root.rotation=Quaternion.Slerp(r.root.rotation,Quaternion.LookRotation(direction),Time.deltaTime*3);
            }
            float t=Time.time*5f+r.phase;
            r.visual.localPosition=Vector3.up*(nearby ? Mathf.Max(0,Mathf.Sin(t))*.18f : .02f*Mathf.Sin(t*.4f));
            r.visual.localRotation=Quaternion.Euler(0,0,nearby?Mathf.Sin(t)*4:Mathf.Sin(t*.4f)*2);
            if(r.leftArm)r.leftArm.localRotation=Quaternion.Euler(0,0,nearby?-100+Mathf.Sin(t*1.5f)*24:-12);
            if(r.rightArm)r.rightArm.localRotation=Quaternion.Euler(walking?Mathf.Sin(t)*30:0,0,12);
            if(r.leftLeg)r.leftLeg.localRotation=Quaternion.Euler(walking?-Mathf.Sin(t)*30:0,0,-4);
            if(r.rightLeg)r.rightLeg.localRotation=Quaternion.Euler(walking?Mathf.Sin(t)*30:0,0,4);
        }
        var destination=leadingToLight && smallLight ? smallLight.transform.position : entrance.transform.position;
        if(Leading && FlatDistance(Guide.position,destination)<3f)Leading=false;
    }
    IEnumerator Conversation()
    {
        Talking=ownsLocks=true;player.ExternalControlLock=true;player.ExternalMovementLock=true;
        screen=new GameObject("Friendly marshmallow conversation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));screen.transform.SetParent(transform,false);
        conversationCanvas=screen.GetComponent<Canvas>();conversationCanvas.renderMode=RenderMode.ScreenSpaceOverlay;conversationCanvas.sortingOrder=1000;GameUITheme.Scale(conversationCanvas);
        var dialogue=screen.AddComponent<SecondIslandDialogue>();
        var lines=Unlocked ? new[]{new SecondIslandDialogue.Line(true,DoorInspected && !SmallLightOwned ?
                "The Small Light is northwest of the tiny door. Follow me to its pink stand!" :
                "Hello again! Follow me to the glowing doorway whenever you want to visit Starlight Grove.")}
            : new[]{new SecondIslandDialogue.Line(true,"Hello, traveller! We live peacefully near the candy house."),
                new SecondIslandDialogue.Line(false,"It's nice to meet a friendly marshmallow!"),
                new SecondIslandDialogue.Line(true,"Starlight Grove is a secret garden for tiny visitors. I'll show you its little doorway. Stay close!"),
                new SecondIslandDialogue.Line(true,"Rest by the well to restore your health and stamina once. A return door brings visitors safely home.")};
        yield return dialogue.Play(screen.transform,lines,"MARSHMALLOW");yield return null;
        bool first=!Unlocked;Unlocked=true;ApplyProgress();leadingToLight=DoorInspected && !SmallLightOwned;Leading=true;EndConversation();
        if(first)Notify("STARLIGHT GROVE UNLOCKED  ·  Follow your new friend!");
        GameSaveManager.RequestAutosave();
    }
    IEnumerator InspectDoor()
    {
        Talking=ownsLocks=true;player.ExternalControlLock=true;player.ExternalMovementLock=true;
        screen=new GameObject("Tiny door conversation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));screen.transform.SetParent(transform,false);
        conversationCanvas=screen.GetComponent<Canvas>();conversationCanvas.renderMode=RenderMode.ScreenSpaceOverlay;conversationCanvas.sortingOrder=1000;GameUITheme.Scale(conversationCanvas);
        BuildMarshmallowPortrait(screen.transform);
        var dialogue=screen.AddComponent<SecondIslandDialogue>();
        var lines=new[]{new SecondIslandDialogue.Line(true,"You're too big to fit through this little door, Sahur!"),
            new SecondIslandDialogue.Line(false,"Oh! How can I get inside?"),
            new SecondIslandDialogue.Line(true,"Find the Small Light on its pink stand northwest of the door. Follow me, and watch for its glowing marker!"),
            new SecondIslandDialogue.Line(true,"Bring it back to shrink and enter. The return door will make you big again.")};
        yield return dialogue.Play(screen.transform,lines,"MARSHMALLOW");yield return null;
        DoorInspected=true;ApplyProgress();leadingToLight=Leading=true;EndConversation();Notify("FIND THE SMALL LIGHT  ·  Follow the marshmallow northwest");GameSaveManager.RequestAutosave();
    }
    void BuildMarshmallowPortrait(Transform parent)
    {
        var prefab=Resources.Load<GameObject>("SecondIslandResidents/Strawberry Friendly Marshmallow");
        if(!prefab)return;
        var frame=GameUITheme.Rect(parent,"Marshmallow portrait frame",new Vector2(1,1),new Vector2(205,205));
        frame.pivot=Vector2.one;frame.anchoredPosition=new Vector2(-28,-25);
        GameUITheme.Image(frame,new Color(.12f,.06f,.14f,.88f));
        var imageRect=GameUITheme.Rect(frame,"Marshmallow portrait",new Vector2(.5f,.5f),new Vector2(190,190));
        imageRect.anchoredPosition=Vector2.zero;
        var image=imageRect.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
        portraitTexture=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32){name="Friendly marshmallow dialogue portrait"};
        portraitTexture.Create();image.texture=portraitTexture;
        portraitStudio=new GameObject("Friendly marshmallow portrait studio");portraitStudio.transform.SetParent(transform,false);
        portraitStudio.transform.position=new Vector3(50100,1000,50000);
        var model=Instantiate(prefab,portraitStudio.transform);
        foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
        portraitVisual=model.transform.Find("Bouncy visual");
        if(portraitVisual)portraitArm=portraitVisual.Find("Left arm pivot");
        var cameraObject=new GameObject("Marshmallow portrait camera",typeof(Camera));cameraObject.transform.SetParent(portraitStudio.transform,false);
        cameraObject.transform.localPosition=new Vector3(0,1.05f,4);cameraObject.transform.localRotation=Quaternion.Euler(0,180,0);
        var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.45f;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;
        camera.nearClipPlane=.1f;camera.farClipPlane=12f;camera.targetTexture=portraitTexture;camera.allowHDR=false;camera.allowMSAA=false;
        var lightObject=new GameObject("Marshmallow portrait light",typeof(Light));lightObject.transform.SetParent(portraitStudio.transform,false);
        // Keep the conversation portrait light local to its distant studio.
        lightObject.transform.localPosition=new Vector3(0,2.5f,3f);
        var light=lightObject.GetComponent<Light>();light.type=LightType.Point;light.range=12f;light.intensity=3f;light.shadows=LightShadows.None;light.cullingMask=1<<31;
    }
    void CollectLight()
    {
        SmallLightOwned=DoorInspected=true;ApplyProgress();
        leadingToLight=false;Leading=true;
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);
        Notify("SMALL LIGHT FOUND  ·  Return to the tiny door");GameSaveManager.RequestAutosave();
    }
    IEnumerator ShrinkAndEnter()
    {
        sizing=Talking=ownsLocks=true;player.ExternalControlLock=true;player.ExternalMovementLock=true;
        CaptureSettings();
        Notify("MARSHMALLOW USES THE SMALL LIGHT...");
        // Stage a temporary copy of the guide alongside Sahur so the helper is
        // visible even if the player returned to the door ahead of the guide.
        var prefab=Resources.Load<GameObject>("SecondIslandResidents/Strawberry Friendly Marshmallow");
        Transform hand=null,lens=null,arm=null,gunTransform=null;
        if(prefab)
        {
            var cam=Camera.main;
            Vector3 right=cam ? Vector3.ProjectOnPlane(cam.transform.right,Vector3.up).normalized : player.transform.right;
            Vector3 forward=cam ? Vector3.ProjectOnPlane(cam.transform.forward,Vector3.up).normalized : player.transform.forward;
            // Put the guide's right hand on the camera-facing side of the shot.
            var at=player.transform.position-right*2.8f+forward*1.1f;
            shrinkActor=Instantiate(prefab,Ground(new Vector2(at.x,at.z)),Quaternion.identity,transform);
            shrinkActor.name="Small Light marshmallow performer";
            if(Guide && Guide.gameObject.activeSelf){guideHiddenForShrink=true;Guide.gameObject.SetActive(false);}
            var look=Vector3.ProjectOnPlane(player.transform.position-shrinkActor.transform.position,Vector3.up);
            if(look.sqrMagnitude>.01f)shrinkActor.transform.rotation=Quaternion.LookRotation(look);
            foreach(var collider in shrinkActor.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            arm=shrinkActor.transform.Find("Bouncy visual/Right arm pivot");
            hand=arm ? arm.Find("Right arm hand") : null;
            if(arm && smallLight)
            {
                var gun=new GameObject("Handheld Small Light");gun.transform.SetParent(arm,false);
                gunTransform=gun.transform;
                gun.transform.localPosition=new Vector3(.36f,-.20f,-.43f);
                gun.transform.localRotation=Quaternion.Euler(90,0,0);gun.transform.localScale=Vector3.one*.28f;
                foreach(Transform part in smallLight.transform)
                {
                    if(part.name=="Pickup pedestal" || part.name.Contains("label") || part.name.Contains("landmark"))continue;
                    var copy=Instantiate(part.gameObject,gun.transform);copy.SetActive(true);
                    foreach(var collider in copy.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                    if(part.name=="Glass lens")lens=copy.transform;
                }
            }
        }
        shrinkEffect=new GameObject("Small Light magic");shrinkEffect.transform.SetParent(player.transform,false);
        if(!shrinkMaterial)shrinkMaterial=Mat("Small Light magic glow",new Color(.77f,1f,.70f),true);
        for(int i=0;i<9;i++)
        {
            float a=i*Mathf.PI*2/9f;
            Shape("Magic sparkle",PrimitiveType.Sphere,shrinkEffect.transform,new Vector3(Mathf.Cos(a)*.55f,.12f+(i%3)*.48f,Mathf.Sin(a)*.55f),Vector3.one*.13f,shrinkMaterial,false);
        }
        var beamObject=new GameObject("Small Light beam");beamObject.transform.SetParent(shrinkEffect.transform,false);
        var beam=beamObject.AddComponent<LineRenderer>();beam.useWorldSpace=true;beam.positionCount=2;beam.sharedMaterial=shrinkMaterial;
        beam.startColor=new Color(.85f,1f,.6f,.8f);beam.endColor=new Color(.55f,1f,.8f,.2f);beam.enabled=false;
        float elapsed=0,feet=body.bounds.min.y;
        while(elapsed<3.5f)
        {
            elapsed+=Time.deltaTime;
            if(shrinkEffect)shrinkEffect.transform.localRotation=Quaternion.Euler(0,elapsed*220f,0);
            float raise=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.8f));
            if(arm)arm.localRotation=Quaternion.Euler(-15*raise,-45*raise,40*raise);
            if(gunTransform && hand)
            {
                var aim=(body.bounds.center-hand.position).normalized;
                if(aim.sqrMagnitude>.01f)gunTransform.rotation=Quaternion.FromToRotation(Vector3.up,aim);
                // The green grip, not the lamp's origin, sits in the hand.
                gunTransform.position=hand.position-gunTransform.TransformVector(new Vector3(0,1.55f,0));
            }
            if(shrinkActor)shrinkActor.transform.localPosition+=Vector3.up*(Mathf.Sin(elapsed*7f)*.0015f);
            if(elapsed>=.85f)
            {
                if(!beam.enabled){beam.enabled=true;GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);}
                beam.SetPosition(0,lens ? lens.position : hand ? hand.position : player.transform.position+Vector3.up*2f);
                beam.SetPosition(1,body.bounds.center);beam.startWidth=.13f;beam.endWidth=.45f+.1f*Mathf.Sin(elapsed*16f);
                float amount=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-1.15f)/1.45f));
                player.transform.localScale=originalScale*Mathf.Lerp(1f,SmallFactor,amount);
                player.transform.position+=Vector3.up*(feet-body.bounds.min.y);
            }
            yield return null;
        }
        // Apply the movement and camera settings through the normal path.
        player.transform.localScale=originalScale;player.transform.position+=Vector3.up*(feet-body.bounds.min.y);
        SetSmall(true);
        Travel(true);sizing=false;EndConversation();
    }
    void EndConversation()
    {
        if(screen)Destroy(screen);screen=null;conversationCanvas=null;
        if(portraitStudio)
        {
            var portraitCamera=portraitStudio.GetComponentInChildren<Camera>(true);
            if(portraitCamera){portraitCamera.targetTexture=null;portraitCamera.enabled=false;}
            portraitStudio.SetActive(false);Destroy(portraitStudio);
        }
        portraitStudio=null;portraitArm=portraitVisual=null;
        if(portraitTexture){portraitTexture.Release();Destroy(portraitTexture);portraitTexture=null;}
        if(shrinkEffect)Destroy(shrinkEffect);shrinkEffect=null;
        if(shrinkActor)Destroy(shrinkActor);shrinkActor=null;
        if(guideHiddenForShrink && Guide)Guide.gameObject.SetActive(true);guideHiddenForShrink=false;
        if(ownsLocks && player){player.ExternalControlLock=false;player.ExternalMovementLock=false;}
        ownsLocks=Talking=false;
        if(sizing){sizing=false;if(!InGrove){if(isSmall)SetSmall(false);else if(player && originalSettingsCaptured)player.transform.localScale=originalScale;}}
    }
    void Travel(bool toGrove)
    {
        SetSmall(toGrove,false);
        var target=toGrove ? GroveCenter+new Vector3(0,.12f,-4f) : Ground(GateSite)+new Vector3(0,.12f,-5f);
        if(!toGrove)target=Ground(new Vector2(target.x,target.z))+Vector3.up*.12f;
        float sole=(body.center.y-body.height*.5f)*Mathf.Abs(player.transform.lossyScale.y);
        target.y-=sole;
        FindFirstObjectByType<SkyboardReward>()?.GetComponent<MarshmallowHopRecovery>()?.ClearTracking();
        player.RestoreSavedPose(target,Quaternion.identity);InGrove=toGrove;portalReady=Time.time+1.5f;
        Notify(toGrove ? "STARLIGHT GROVE  ·  A peaceful secret above the clouds" : "BACK TO MARSHMALLOW ISLAND");
        GameSaveManager.RequestAutosave();
    }
    void Rest()
    {
        var stamina=player.GetComponent<PlayerStamina>();
        if((!health || health.currentHealth>=health.maxHealth) && (!stamina || stamina.currentStamina>=stamina.maxStamina))
        {Notify("You are already fully rested. Come back when you need to recover.");return;}
        if(health)health.currentHealth=Mathf.Min(health.maxHealth,health.currentHealth+health.maxHealth*.4f);
        if(stamina)stamina.currentStamina=stamina.maxStamina;
        Rested=true;GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);
        Notify("The well restored health and stamina.");GameSaveManager.RequestAutosave();
    }
    void OnDisable() { StopAllCoroutines();EndConversation();if(isSmall)SetSmall(false); }
    void OnDestroy() { foreach(var m in materials)if(m)Destroy(m); }
}
