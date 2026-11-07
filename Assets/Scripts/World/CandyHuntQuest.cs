using System.Collections;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>A small, non-blocking fairy-led treasure hunt on the second island.</summary>
[DisallowMultipleComponent]
public sealed class CandyHuntQuest : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    const string ResourcePath = "SecondIslandCandyHunt/Candy Hunt";
    const int AllCandies = (1 << 10) - 1;
    public GameObject fairyPrefab;
    public Transform fairy;
    public Transform[] candies = new Transform[10];
    public bool Started { get; private set; }
    public int CollectedMask { get; private set; }
    public bool RewardClaimed { get; private set; }
    public int SelectedStyle { get; private set; }
    public static bool StyleSelectorOpen { get; private set; }
    public bool Completed => (CollectedMask & AllCandies) == AllCandies;
    public int CollectedCount { get { int n=0; for(int i=0;i<10;i++) if((CollectedMask & (1<<i))!=0)n++; return n; } }
    public int NextCandy { get { for(int i=0;i<10;i++)if((CollectedMask & (1<<i))==0)return i;return -1; } }
    public Transform NextTarget => Started && NextCandy>=0 && NextCandy<candies.Length ? candies[NextCandy] : Started && Completed && !RewardClaimed ? fairy : null;

    static readonly string[] Hints = {
        "Look west of me, in the pink valley.",
        "Search the northern meadow, beyond the valley.",
        "Look east of the moving marshmallow course.",
        "Search the far northwest slope beyond the marshmallow course.",
        "Look along the island's far western edge.",
        "Follow the western side south to the pink hillside.",
        "Search the southwest, near the island's southern edge.",
        "Look on the southern hills, west of the island's centre.",
        "Search the high ground in the southeast.",
        "The last candy is on the far eastern ridge."
    };
    static int lastInteractionFrame = -1;
    public static bool InteractionConsumedThisFrame => lastInteractionFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        lastInteractionFrame=-1;
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene()
    {
        for(int i=0;i<SceneManager.sceneCount;i++) EnsureFor(SceneManager.GetSceneAt(i));
    }
    static void SceneLoaded(Scene scene, LoadSceneMode mode) => EnsureFor(scene);
    static void EnsureFor(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath) return;
        foreach(var root in scene.GetRootGameObjects())
            if(root.GetComponentInChildren<CandyHuntQuest>(true)) return;
        var prefab=Resources.Load<GameObject>(ResourcePath);
        if(!prefab){Debug.LogError("Candy hunt prefab is missing from Resources.");return;}
        var quest=Instantiate(prefab);
        SceneManager.MoveGameObjectToScene(quest,scene);
    }

    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    Canvas hud;
    RectTransform card, portrait;
    Text title, clue, direction, talkPrompt, questLandmarkText;
    Camera portraitCamera;
    GameObject portraitRoot, talkPromptRoot, questLandmark;
    Transform portraitLeftWing, portraitRightWing;
    Quaternion portraitLeftRest, portraitRightRest;
    RenderTexture portraitTexture;
    float popupTime, popupUntil, completionUntil, nextLookup, nextHudRefresh;
    bool talking, ownsLocks;
    SecondIslandDialogue dialogue;
    GameObject conversationScreen;
    readonly List<Vector3> candyBases = new List<Vector3>();
    readonly List<Texture2D> stylePreviews = new List<Texture2D>();
    public bool PopupVisible => portrait && portrait.gameObject.activeInHierarchy;
    public string CurrentHint => NextCandy >= 0 ? Hints[NextCandy] : RewardClaimed ? "Your Skyboard styles are ready!" : "All ten found! Return to me for your reward.";

    public void RestoreProgress(bool started, int mask, bool claimed=false, int style=0)
    {
        Started=started || (mask & AllCandies)!=0;
        CollectedMask=mask & AllCandies;
        RewardClaimed=claimed && Completed;SelectedStyle=Mathf.Clamp(style,0,CandySkyboardStyles.Names.Length-1);
        ApplyCollected();
        if(Started && !Completed) ShowHint();
    }

    void Start()
    {
        foreach(var candy in candies) candyBases.Add(candy ? candy.localPosition : Vector3.zero);
        ApplyCollected();
        BuildHud();
        BuildTalkPrompt();
    }
    void ApplyCollected()
    {
        if(candies==null)return;
        for(int i=0;i<candies.Length && i<10;i++)
            if(candies[i])candies[i].gameObject.SetActive(Started && (CollectedMask & (1<<i))==0);
    }
    void BuildTalkPrompt()
    {
        if(!fairy)return;
        SkyboardReward.Label(fairy,"TALK TO FAIRY");
        var tr=fairy.Find("Pickup label"); if(!tr)return;
        talkPromptRoot=tr.gameObject;
        talkPromptRoot.name="Candy hunt talk prompt";
        tr.localPosition=new Vector3(0,1.85f,0);
        talkPrompt=tr.GetComponentInChildren<Text>();
        if(talkPrompt)talkPrompt.fontSize=28;
        talkPromptRoot.SetActive(false);
        SkyboardReward.Label(fairy,"!  OPTIONAL QUEST\nCANDY HUNT");
        var landmark=fairy.Find("Pickup label");
        if(landmark)
        {
            questLandmark=landmark.gameObject;questLandmark.name="Candy hunt landmark";
            landmark.localPosition=new Vector3(0,4.8f,0);
            float scale=Mathf.Max(.001f,Mathf.Abs(fairy.lossyScale.x));
            landmark.localScale=Vector3.one*(.028f/scale);
            questLandmarkText=landmark.GetComponentInChildren<Text>();
            if(questLandmarkText){questLandmarkText.fontSize=34;questLandmarkText.color=new Color(1f,.86f,.53f);}
            questLandmark.SetActive(false);
        }
    }
    void Update()
    {
        if(conversationScreen)conversationScreen.GetComponent<Canvas>().enabled=!GameUITheme.ModalOpen;
        if(!player && Time.unscaledTime>=nextLookup)
        {
            nextLookup=Time.unscaledTime+1;
            player=FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player){body=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerHealth>();}
        }
        if(!player || !body)return;
        if(talking && (!player.isActiveAndEnabled || !body.enabled || health && health.currentHealth<=0))
        { StopAllCoroutines(); EndConversation(); }
        var cam=Camera.main;
        for(int i=0;i<candies.Length && i<candyBases.Count;i++)
        {
            var candy=candies[i];if(!candy || !candy.gameObject.activeSelf)continue;
            candy.localPosition=candyBases[i]+Vector3.up*(.15f*Mathf.Sin(Time.time*1.8f+i));
            candy.localRotation=Quaternion.Euler(0,Time.time*35f+i*36f,0);
        }
        bool modal=GameUITheme.ModalOpen || SecondIslandAwakening.BlocksInputFor(player) || MarshmallowChallengeGuide.AnyConversation;
        bool alive=!health || health.currentHealth>0;
        bool allowed=Time.timeScale>0 && !modal && !talking && alive && player.enabled && body.enabled &&
            !player.ExternalControlLock && !player.ExternalMovementLock;
        float fairyDistance=fairy ? Vector3.ProjectOnPlane(player.transform.position-fairy.position,Vector3.up).magnitude : 999;
        bool near=allowed && fairy && fairyDistance<7f && Mathf.Abs(body.bounds.min.y-fairy.position.y)<5f;
        if(questLandmark)
        {
            bool show=(!Started || Completed && !RewardClaimed) && allowed && fairyDistance>=12f && fairyDistance<=90f;
            if(questLandmark.activeSelf!=show)questLandmark.SetActive(show);
            if(show && cam)questLandmark.transform.rotation=cam.transform.rotation;
            if(questLandmarkText)questLandmarkText.text=Completed && !RewardClaimed ? "!  RETURN TO FAIRY\nCLAIM YOUR REWARD" : "!  OPTIONAL QUEST\nCANDY HUNT";
        }
        if(talkPromptRoot)
        {
            talkPromptRoot.SetActive(near);
            if(near && cam)talkPromptRoot.transform.rotation=cam.transform.rotation;
            if(near && talkPrompt)talkPrompt.text=GameControls.Prompt(GameControls.Command.Interact)+(Completed ? "  ·  CHOOSE YOUR STYLE" : Started ? "  ·  ASK FAIRY" : "  ·  TALK TO FAIRY");
        }
        if(near && GameControls.Down(GameControls.Command.Interact) && Cursor.lockState==CursorLockMode.Locked)
        {
            lastInteractionFrame=Time.frameCount;
            if(!Started && body.isGrounded)StartCoroutine(Introduce());
            else if(Completed && body.isGrounded)StartCoroutine(ChooseStyle());
            else if(Started)ShowHint();
        }
        if(Started && !Completed && allowed)
        {
            for(int i=0;i<candies.Length && i<10;i++)
            {
                if(!candies[i] || (CollectedMask & (1<<i))!=0)continue;
                Vector3 d=candies[i].position-player.transform.position;
                if(new Vector2(d.x,d.z).sqrMagnitude>3.8f*3.8f || Mathf.Abs(candies[i].position.y-body.bounds.min.y)>4.5f)continue;
                CollectedMask |= 1<<i;
                candies[i].gameObject.SetActive(false);
                GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);
                ShowHint();
                // Save the one-time cosmetic unlock immediately; ordinary
                // pickups still use the shared throttled autosave.
                if(!Completed || !GameSaveManager.SaveCurrentGame()) GameSaveManager.RequestAutosave();
            }
        }
        UpdateHud(modal || talking,cam);
    }

    IEnumerator Introduce()
    {
        talking=ownsLocks=true;
        player.ExternalControlLock=true;player.ExternalMovementLock=true;
        conversationScreen=new GameObject("Candy hunt conversation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        conversationScreen.transform.SetParent(transform,false);
        var canvas=conversationScreen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;GameUITheme.Scale(canvas);
        dialogue=conversationScreen.AddComponent<SecondIslandDialogue>();
        var lines=new[]{
            new SecondIslandDialogue.Line(true,"Hello! I dropped ten colourful candies all across Marshmallow Island. Will you help me find them?"),
            new SecondIslandDialogue.Line(true,"Walk near a candy to collect it. I will fly in with another clue each time; you do not need to come back to me."),
            new SecondIslandDialogue.Line(true,"Find all ten, then return to me for colourful Skyboard styles. You can use them once you find the board!")
        };
        yield return dialogue.Play(conversationScreen.transform,lines);
        yield return null;
        Started=true;ApplyCollected();EndConversation();ShowHint();GameSaveManager.RequestAutosave();
    }
    void EndConversation()
    {
        if(conversationScreen)Destroy(conversationScreen);
        conversationScreen=null;dialogue=null;
        foreach(var texture in stylePreviews)if(texture)Destroy(texture);
        stylePreviews.Clear();
        if(ownsLocks && player){player.ExternalMovementLock=false;player.ExternalControlLock=false;}
        ownsLocks=talking=false;StyleSelectorOpen=false;
    }

    IEnumerator ChooseStyle()
    {
        talking=ownsLocks=true;StyleSelectorOpen=true;
        player.ExternalControlLock=true;player.ExternalMovementLock=true;
        conversationScreen=new GameObject("Fairy Skyboard styles",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        conversationScreen.transform.SetParent(transform,false);
        var canvas=conversationScreen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;GameUITheme.Scale(canvas);
        var panel=GameUITheme.Rect(conversationScreen.transform,"Choose a style",new Vector2(.5f,.5f),new Vector2(840,540));
        panel.anchoredPosition=Vector2.zero;GameUITheme.Image(panel,new Color(.05f,.035f,.09f,.97f));
        var heading=GameUITheme.Label(panel,"Heading",31,new Vector2(0,225),new Vector2(790,54),TextAnchor.MiddleCenter);
        heading.rectTransform.anchorMin=heading.rectTransform.anchorMax=new Vector2(.5f,.5f);
        heading.text=RewardClaimed ? "CHOOSE YOUR SKYBOARD STYLE" : "THANK YOU! CHOOSE YOUR REWARD";
        var subtitle=GameUITheme.Label(panel,"Explanation",20,new Vector2(0,180),new Vector2(770,44),TextAnchor.MiddleCenter);
        subtitle.rectTransform.anchorMin=subtitle.rectTransform.anchorMax=new Vector2(.5f,.5f);
        subtitle.text="Five Skyboard looks are yours. Return to change them anytime.";
        var labels=new Text[CandySkyboardStyles.Names.Length];
        for(int i=0;i<labels.Length;i++)
        {
            float y=125-i*61;
            var swatch=GameUITheme.Rect(panel,"Style swatch "+i,new Vector2(.5f,.5f),new Vector2(52,35));
            swatch.anchoredPosition=new Vector2(-280,y);
            var image=swatch.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            if(i<CandySkyboardStyles.Names.Length-1){var preview=CandySkyboardStyles.CreateTexture(i);stylePreviews.Add(preview);image.texture=preview;}
            else image.color=Color.white;
            labels[i]=GameUITheme.Label(panel,"Style "+i,26,new Vector2(-240,y),new Vector2(560,52),TextAnchor.MiddleLeft);
            labels[i].rectTransform.anchorMin=labels[i].rectTransform.anchorMax=new Vector2(.5f,.5f);
        }
        var hint=GameUITheme.Label(panel,"Controls",19,new Vector2(0,-220),new Vector2(760,42),TextAnchor.MiddleCenter);
        hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,.5f);
        hint.text="1–6: CHOOSE  ·  BACKSPACE: CLOSE    |    D-PAD + A: CHOOSE  ·  B: CLOSE";
        int selected=SelectedStyle;
        yield return null;
        while(true)
        {
            for(int i=0;i<labels.Length;i++)
            {
                labels[i].text=(i+1)+"   "+CandySkyboardStyles.Names[i]+(RewardClaimed && SelectedStyle==i ? "   ·   CURRENT" : "");
                labels[i].color=i==selected ? new Color(1f,.86f,.55f) : Color.white;
            }
            if(Time.timeScale>0 && !GameUITheme.ModalOpen)
            {
                var k=Keyboard.current;var pad=Gamepad.current;int choice=-1;
                if(k!=null)
                {
                    if(k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame)choice=0;
                    else if(k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame)choice=1;
                    else if(k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame)choice=2;
                    else if(k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame)choice=3;
                    else if(k.digit5Key.wasPressedThisFrame || k.numpad5Key.wasPressedThisFrame)choice=4;
                    else if(k.digit6Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame)choice=5;
                    else if(k.backspaceKey.wasPressedThisFrame){EndConversation();yield break;}
                }
                if(pad!=null)
                {
                    if(pad.dpad.up.wasPressedThisFrame)selected=(selected+labels.Length-1)%labels.Length;
                    if(pad.dpad.down.wasPressedThisFrame)selected=(selected+1)%labels.Length;
                    if(pad.buttonSouth.wasPressedThisFrame)choice=selected;
                    if(pad.buttonEast.wasPressedThisFrame){EndConversation();yield break;}
                }
                if(choice>=0)
                {
                    lastInteractionFrame=Time.frameCount;
                    RewardClaimed=true;SelectedStyle=choice;GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);
                    EndConversation();ShowHint();if(!GameSaveManager.SaveCurrentGame())GameSaveManager.RequestAutosave();yield break;
                }
            }
            yield return null;
        }
    }

    public void ShowHint()
    {
        popupTime=Time.unscaledTime;popupUntil=popupTime+9f;
        nextHudRefresh=0;
        if(Completed)completionUntil=popupTime+12f;
    }

    void BuildHud()
    {
        var obj=new GameObject("Candy hunt HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        obj.transform.SetParent(transform,false);hud=obj.GetComponent<Canvas>();hud.renderMode=RenderMode.ScreenSpaceOverlay;hud.sortingOrder=85;GameUITheme.Scale(hud);
        card=GameUITheme.Rect(obj.transform,"Fairy clue",new Vector2(1,1),new Vector2(600,152));
        card.pivot=new Vector2(1,1);card.anchoredPosition=new Vector2(-22,-24);
        GameUITheme.Image(card,new Color(.06f,.045f,.09f,.93f));
        title=GameUITheme.Label(card,"Title",22,new Vector2(-580,-26),new Vector2(410,34),TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin=title.rectTransform.anchorMax=new Vector2(1,1);
        clue=GameUITheme.Label(card,"Clue",22,new Vector2(-580,-75),new Vector2(420,70),TextAnchor.MiddleLeft);
        clue.rectTransform.anchorMin=clue.rectTransform.anchorMax=new Vector2(1,1);
        direction=GameUITheme.Label(card,"Direction",18,new Vector2(-580,-130),new Vector2(410,27),TextAnchor.MiddleLeft);
        direction.rectTransform.anchorMin=direction.rectTransform.anchorMax=new Vector2(1,1);direction.color=new Color(1f,.85f,.55f);
        portrait=GameUITheme.Rect(card,"Fairy portrait",new Vector2(1,1),new Vector2(160,160));
        portrait.pivot=new Vector2(1,1);portrait.anchoredPosition=new Vector2(-4,5);
        var image=portrait.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
        BuildPortrait(image);
        card.gameObject.SetActive(false);
    }

    void BuildPortrait(RawImage image)
    {
        if(!fairyPrefab)return;
        portraitTexture=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32){name="Candy fairy portrait"};portraitTexture.Create();image.texture=portraitTexture;
        portraitRoot=new GameObject("Candy fairy portrait studio");
        portraitRoot.transform.SetParent(transform,false);portraitRoot.transform.position=new Vector3(50000,1000,50000);
        var model=Instantiate(fairyPrefab,portraitRoot.transform);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(0,180,0);model.transform.localScale=Vector3.one*2f;
        // The portrait is not a world NPC and must not appear as a map marker.
        var hover=model.GetComponent<NealFairyHover>();
        if(hover)
        {
            portraitLeftWing=hover.leftWing;portraitRightWing=hover.rightWing;
            hover.enabled=false;
            if(portraitLeftWing)portraitLeftRest=portraitLeftWing.localRotation;
            if(portraitRightWing)portraitRightRest=portraitRightWing.localRotation;
            Destroy(hover);
        }
        foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        var cameraObj=new GameObject("Portrait camera",typeof(Camera));cameraObj.transform.SetParent(portraitRoot.transform,false);
        cameraObj.transform.localPosition=new Vector3(0,1.35f,-5);cameraObj.transform.localRotation=Quaternion.identity;
        portraitCamera=cameraObj.GetComponent<Camera>();portraitCamera.orthographic=true;portraitCamera.orthographicSize=1.55f;
        portraitCamera.clearFlags=CameraClearFlags.SolidColor;portraitCamera.backgroundColor=Color.clear;portraitCamera.cullingMask=1<<31;
        portraitCamera.nearClipPlane=.1f;portraitCamera.farClipPlane=12f;portraitCamera.targetTexture=portraitTexture;portraitCamera.allowHDR=false;portraitCamera.allowMSAA=false;
        var lightObj=new GameObject("Portrait light",typeof(Light));lightObj.transform.SetParent(portraitRoot.transform,false);
        // Directional lights are global in URP even when the portrait is far
        // away, and can brighten the island as the hint appears. A local point
        // light illuminates the off-screen studio without touching the world.
        lightObj.transform.localPosition=new Vector3(0,2.5f,-3f);
        var light=lightObj.GetComponent<Light>();light.type=LightType.Point;light.range=12f;light.intensity=8f;light.shadows=LightShadows.None;light.cullingMask=1<<31;
        portraitRoot.SetActive(false);
    }

    void UpdateHud(bool hidden, Camera view)
    {
        if(!card)return;
        bool show=Started && (!Completed || !RewardClaimed || Time.unscaledTime<completionUntil) && !hidden;
        card.gameObject.SetActive(show);
        if(!show){if(portraitRoot)portraitRoot.SetActive(false);return;}
        if(Time.unscaledTime>=nextHudRefresh)
        {
            nextHudRefresh=Time.unscaledTime+.2f;
            title.text="FAIRY'S CANDY HUNT   " + CollectedCount + " / 10";
            clue.text=CurrentHint;
            if(Completed && RewardClaimed)direction.text="Style: "+CandySkyboardStyles.Names[SelectedStyle]+"  ·  Visit fairy to change";
            else if(view && NextTarget)
            {
                Vector3 delta=Vector3.ProjectOnPlane(NextTarget.position-player.transform.position,Vector3.up);
                Vector3 f=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;
                Vector3 r=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
                float front=Vector3.Dot(f,delta),side=Vector3.Dot(r,delta);
                string arrow=Mathf.Abs(side)>Mathf.Abs(front)*.45f?(side>0?"→":"←"):(front>=0?"↑":"↓");
                direction.text=arrow+"  "+Mathf.RoundToInt(delta.magnitude)+" m  ·  "+GameControls.Prompt(GameControls.Command.Map)+": map";
            }
        }
        bool popup=Time.unscaledTime<popupUntil;
        portrait.gameObject.SetActive(popup);
        if(portraitRoot && portraitRoot.activeSelf!=popup)portraitRoot.SetActive(popup);
        if(popup)
        {
            float sweep=Mathf.Lerp(5f,40f,.5f-.5f*Mathf.Cos(Time.unscaledTime*1.45f*Mathf.PI*2));
            if(portraitLeftWing)portraitLeftWing.localRotation=portraitLeftRest*Quaternion.Euler(0,-sweep,0);
            if(portraitRightWing)portraitRightWing.localRotation=portraitRightRest*Quaternion.Euler(0,sweep,0);
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-popupTime)/.45f));
            float exit=Mathf.SmoothStep(0,1,Mathf.Clamp01((popupUntil-Time.unscaledTime)/.4f));
            portrait.anchoredPosition=new Vector2(-4+170*(1-Mathf.Min(enter,exit)),5+3*Mathf.Sin(Time.unscaledTime*3));
        }
    }
    void OnDisable(){StopAllCoroutines();EndConversation();}
    void OnDestroy(){if(portraitTexture){portraitTexture.Release();Destroy(portraitTexture);}}
}
