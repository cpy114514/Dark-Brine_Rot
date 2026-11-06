using System;
using System.Collections;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>A gentle, replayable colour-and-sound memory game on the second island.</summary>
[DisallowMultipleComponent]
public sealed class CandyMelodyGarden : MonoBehaviour
{
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    static readonly Vector2 GardenSite=new Vector2(160,2080);
    static readonly Vector2[] Sites={new Vector2(152,2074),new Vector2(168,2074),new Vector2(152,2086),new Vector2(168,2086)};
    static readonly string[] Names={"PINK","BLUE","LEMON","MINT"};
    static readonly Color[] Colors={new Color(1f,.42f,.65f),new Color(.32f,.70f,1f),new Color(1f,.83f,.22f),new Color(.34f,.91f,.67f)};
    static readonly float[] Frequencies={261.63f,329.63f,392f,523.25f};
    public enum Stage { Idle, Showing, Listening, Complete }
    public Stage CurrentStage {get;private set;}
    public bool RewardClaimed {get;private set;}
    public int Round {get;private set;}
    public int InputCount {get;private set;}
    public int Mistakes {get;private set;}
    public int SequenceLength=>3+Round;
    public Transform MapAnchor=>transform;
    public Transform NoteTransform(int index)=>index>=0 && index<notes.Count?notes[index].root:null;
    public int SequenceAt(int index)=>index>=0 && index<SequenceLength?sequence[index]:-1;
    public Transform StartSign=>sign ? sign.transform : null;
    public bool ReservesInteraction
    {
        get
        {
            if(lastInteractionFrame==Time.frameCount)return true;
            if(!player || !body || !body.enabled)return false;
            if(sign && Near(sign.transform,4f))return true;
            return false;
        }
    }
    sealed class Note
    {
        public Transform root, candy;
        public Renderer renderer;
        public Material normal, bright;
        public GameObject prompt;
        public Text promptText;
    }
    sealed class Friend { public Transform root,visual,arm; public float phase; }
    readonly List<Note> notes=new List<Note>();
    readonly List<Friend> friends=new List<Friend>();
    readonly List<Material> materials=new List<Material>();
    readonly AudioClip[] tones=new AudioClip[4];
    readonly int[] sequence=new int[5];
    readonly System.Random random=new System.Random();
    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    EquipmentInventory inventory;
    EquipmentItem scroll;
    SkyboardReward board;
    Collider island;
    AudioSource audioSource;
    GameObject sign,signLabel,signPrompt,hud,hudCard;
    Text signPromptText,title,detail;
    Coroutine playback;
    int lit=-1,lastInteractionFrame=-1;
    readonly bool[] insideNote = new bool[4];
    float nextLookup,feedbackUntil,lightUntil;
    string feedback="",showingText="";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene(){for(int i=0;i<SceneManager.sceneCount;i++)Ensure(SceneManager.GetSceneAt(i));}
    static void OnSceneLoaded(Scene scene,LoadSceneMode mode)=>Ensure(scene);
    static void Ensure(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<CandyMelodyGarden>(true))return;
        var obj=new GameObject("Candy Melody Garden");SceneManager.MoveGameObjectToScene(obj,scene);obj.AddComponent<CandyMelodyGarden>();
    }
    void Awake()
    {
        var experience=FindFirstObjectByType<MarshmallowIslandExperience>();
        island=experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if(!island){Debug.LogError("Candy Melody Garden could not find the island ground.");enabled=false;return;}
        transform.position=Ground(GardenSite);
        var cream=Mat("Melody icing",new Color(1f,.88f,.94f));
        var wood=Mat("Melody candy stems",new Color(.62f,.34f,.40f));
        var gold=Mat("Melody gold",new Color(1f,.83f,.38f),true);
        for(int i=0;i<Sites.Length;i++)BuildNote(i,cream,wood,gold);
        BuildSign(cream,wood,gold);
        MakeFriend("Strawberry",new Vector2(146,2072),.7f);
        MakeFriend("Mint",new Vector2(175,2091),2.3f);
        BuildHud();BuildTones();
    }
    Vector3 Ground(Vector2 point)
    {
        var b=island.bounds;
        if(island.Raycast(new Ray(new Vector3(point.x,b.max.y+10,point.y),Vector3.down),out var hit,b.size.y+20))return hit.point;
        throw new InvalidOperationException("Candy Melody Garden has no ground at "+point);
    }
    Material Mat(string name,Color color,bool glow=false)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material=new Material(shader){name=name};
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);else material.color=color;
        if(glow && material.HasProperty("_EmissionColor")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.65f);}
        materials.Add(material);return material;
    }
    static GameObject Shape(string name,PrimitiveType shape,Transform parent,Vector3 pos,Vector3 scale,Material material)
    {
        var obj=GameObject.CreatePrimitive(shape);obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=pos;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
    }
    GameObject Label(Transform parent,string value,float height,float scale=.014f)
    {
        SkyboardReward.Label(parent,value);var t=parent.Find("Pickup label");t.name="Melody label";
        t.localPosition=Vector3.up*height;t.localScale=Vector3.one*scale;
        var text=t.GetComponentInChildren<Text>();if(text)text.fontSize=27;
        return t.gameObject;
    }
    void BuildNote(int index,Material cream,Material wood,Material gold)
    {
        var root=new GameObject(Names[index]+" singing candy");root.transform.SetParent(transform,false);root.transform.position=Ground(Sites[index]);
        var normal=Mat(Names[index]+" candy",Colors[index]);var bright=Mat(Names[index]+" glowing candy",Colors[index],true);
        Shape("Icing base",PrimitiveType.Cylinder,root.transform,new Vector3(0,.14f,0),new Vector3(3,.14f,3),cream);
        Shape("Candy stem",PrimitiveType.Cylinder,root.transform,new Vector3(0,.9f,0),new Vector3(.34f,.76f,.34f),wood);
        var candy=Shape("Singing candy",PrimitiveType.Sphere,root.transform,new Vector3(0,2.15f,0),new Vector3(1.9f,1.9f,1.9f),normal);
        Shape("Candy cap",PrimitiveType.Sphere,root.transform,new Vector3(0,3.08f,0),Vector3.one*.28f,gold);
        var prompt=Label(root.transform,(index+1)+"  "+Names[index],3.9f,.02f);var promptText=prompt.GetComponentInChildren<Text>();
        notes.Add(new Note{root=root.transform,candy=candy.transform,renderer=candy.GetComponent<Renderer>(),normal=normal,bright=bright,prompt=prompt,promptText=promptText});
    }
    void BuildSign(Material cream,Material wood,Material gold)
    {
        sign=new GameObject("Melody Garden sign");sign.transform.SetParent(transform,false);sign.transform.position=Ground(new Vector2(160,2067));
        var dark=Mat("Melody sign background",new Color(.15f,.075f,.20f));
        Shape("Sign post left",PrimitiveType.Cylinder,sign.transform,new Vector3(-2.8f,1.8f,0),new Vector3(.32f,1.8f,.32f),wood);
        Shape("Sign post right",PrimitiveType.Cylinder,sign.transform,new Vector3(2.8f,1.8f,0),new Vector3(.32f,1.8f,.32f),wood);
        Shape("Sign board",PrimitiveType.Cube,sign.transform,new Vector3(0,3.7f,0),new Vector3(7.2f,2.6f,.45f),dark);
        Shape("Icing sign top",PrimitiveType.Cube,sign.transform,new Vector3(0,5.1f,0),new Vector3(7.4f,.2f,.5f),cream);
        Shape("Star on sign",PrimitiveType.Sphere,sign.transform,new Vector3(0,5.4f,0),Vector3.one*.45f,gold);
        signLabel=Label(sign.transform,"CANDY MELODY GARDEN\nWatch the candy lights and listen\nRepeat the colours in order\n3 rounds  ·  First win: 1 speed scroll",3.7f,.012f);
        ((RectTransform)signLabel.transform).sizeDelta=new Vector2(550,165);
        signPrompt=Label(sign.transform,"",6.4f,.017f);signPromptText=signPrompt.GetComponentInChildren<Text>();signPrompt.SetActive(false);
    }
    void MakeFriend(string color,Vector2 site,float phase)
    {
        var prefab=Resources.Load<GameObject>("SecondIslandResidents/"+color+" Friendly Marshmallow");if(!prefab)return;
        var root=Instantiate(prefab,Ground(site),Quaternion.identity,transform);root.name=color+" melody audience";
        var collider=root.AddComponent<CapsuleCollider>();collider.center=Vector3.up;collider.radius=.52f;collider.height=2f;
        var visual=root.transform.Find("Bouncy visual");friends.Add(new Friend{root=root.transform,visual=visual,arm=visual?visual.Find("Right arm pivot"):null,phase=phase});
    }
    void BuildHud()
    {
        hud=new GameObject("Candy Melody HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));hud.transform.SetParent(transform,false);
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=82;GameUITheme.Scale(canvas);
        var card=GameUITheme.Rect(hud.transform,"Melody card",new Vector2(.5f,1),new Vector2(600,128));card.pivot=new Vector2(.5f,1);card.anchoredPosition=new Vector2(0,-24);
        GameUITheme.Image(card,new Color(.09f,.045f,.13f,.94f));
        title=GameUITheme.Label(card,"Title",24,new Vector2(-280,-26),new Vector2(560,34),TextAnchor.MiddleLeft);
        detail=GameUITheme.Label(card,"Instructions",20,new Vector2(-280,-79),new Vector2(560,70),TextAnchor.MiddleLeft);
        hudCard=card.gameObject;hudCard.SetActive(false);
    }
    void BuildTones()
    {
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;audioSource.volume=.36f;
        const int rate=22050;int count=Mathf.RoundToInt(.38f*rate);
        for(int n=0;n<tones.Length;n++)
        {
            var data=new float[count];
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate,env=Mathf.Min(1,t*80)*Mathf.Exp(-t*8);
                float phase=2*Mathf.PI*Frequencies[n]*t;
                data[i]=(.72f*Mathf.Sin(phase)+.2f*Mathf.Sin(phase*2)+.08f*Mathf.Sin(phase*3))*env*.55f;
            }
            tones[n]=AudioClip.Create("Candy melody "+Names[n],count,1,rate,false);tones[n].SetData(data,0);
        }
    }
    public void RestoreProgress(bool claimed){RewardClaimed=claimed;}
    bool Near(Transform target,float radius)
    {
        if(!target || !player || !body)return false;
        Vector3 delta=player.transform.position-target.position;float y=Mathf.Abs(body.bounds.min.y-target.position.y);delta.y=0;
        return delta.sqrMagnitude<radius*radius && y<5f;
    }
    void Update()
    {
        if(!player && Time.unscaledTime>=nextLookup)
        {
            nextLookup=Time.unscaledTime+1;player=FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player){body=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerHealth>();inventory=player.GetComponent<EquipmentInventory>();}
            board=FindFirstObjectByType<SkyboardReward>();scroll=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);
        }
        if(!player || !body || notes.Count!=4)return;
        bool allowed=Time.timeScale>0 && body.enabled && player.isActiveAndEnabled && (!health || health.currentHealth>0) &&
            !player.ExternalControlLock && !player.ExternalMovementLock && !GameUITheme.ModalOpen && !SecondIslandAwakening.BlocksInputFor(player) && !(board && board.Flying);
        float distance=Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up),Vector3.zero);
        if((CurrentStage==Stage.Showing || CurrentStage==Stage.Listening) && distance>42f){StopPlayback();CurrentStage=Stage.Idle;SetLit(-1);feedback="";}
        if(CurrentStage!=Stage.Showing && lit>=0 && Time.time>=lightUntil)SetLit(-1);
        var cam=Camera.main;
        if(signLabel && cam)
        {
            signLabel.transform.rotation=cam.transform.rotation;
            Vector3 toward=cam.transform.position-sign.transform.position;toward.y=0;
            signLabel.transform.position=sign.transform.position+Vector3.up*3.7f+(toward.sqrMagnitude>.001f?toward.normalized*.38f:Vector3.zero);
        }
        bool bySign=allowed && Near(sign.transform,4f);
        if(signPrompt.activeSelf!=bySign)signPrompt.SetActive(bySign);
        if(bySign && cam)signPrompt.transform.rotation=cam.transform.rotation;
        if(bySign)signPromptText.text=CurrentStage==Stage.Showing?"LISTEN TO THE MELODY":
            GameControls.Prompt(GameControls.Command.Interact)+(CurrentStage==Stage.Listening?"  ·  REPLAY MELODY":"  ·  START MELODY");
        int entered=-1;float closest=3.7f;
        for(int i=0;i<notes.Count;i++)
        {
            var n=notes[i];bool inside=Near(n.root,3.7f);
            float d=Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position-n.root.position,Vector3.up),Vector3.zero);
            if(allowed && CurrentStage==Stage.Listening && inside && !insideNote[i] && d<closest){closest=d;entered=i;}
            insideNote[i]=inside;
            if(n.prompt && cam)n.prompt.transform.rotation=cam.transform.rotation;
            if(n.promptText)n.promptText.text=(i+1)+"  "+Names[i];
        }
        if(entered>=0)SelectNote(entered);
        foreach(var friend in friends)
        {
            if(!friend.visual)continue;
            bool nearby=distance<28f;float t=Time.time*3f+friend.phase;
            friend.visual.localPosition=Vector3.up*(nearby?Mathf.Max(0,Mathf.Sin(t))*.10f:.015f*Mathf.Sin(t));
            if(friend.arm)friend.arm.localRotation=Quaternion.Euler(0,0,nearby?75+Mathf.Sin(t*1.6f)*18:12);
        }
        UpdateHud(distance<35f && allowed);
        if(!allowed || Cursor.lockState!=CursorLockMode.Locked || !GameControls.Down(GameControls.Command.Interact))return;
        if(bySign)lastInteractionFrame=Time.frameCount;
        if(bySign && CurrentStage!=Stage.Showing)
        {
            if(CurrentStage==Stage.Listening){InputCount=0;BeginPlayback();}
            else StartGame();
        }
    }
    void StartGame()
    {
        for(int i=0;i<sequence.Length;i++)
        {
            int value=random.Next(4);
            if(i>0 && value==sequence[i-1])value=(value+1+random.Next(3))%4;
            sequence[i]=value;
        }
        Round=0;InputCount=0;Mistakes=0;feedback="";BeginPlayback();
    }
    void BeginPlayback()
    {
        StopPlayback();CurrentStage=Stage.Showing;InputCount=0;playback=StartCoroutine(PlaySequence());
    }
    IEnumerator WaitActive(float seconds)
    {
        float elapsed=0;
        while(elapsed<seconds)
        {
            if(Time.timeScale>0 && !GameUITheme.ModalOpen && player && !player.ExternalControlLock)elapsed+=Time.deltaTime;
            yield return null;
        }
    }
    IEnumerator PlaySequence()
    {
        yield return WaitActive(.65f);
        for(int i=0;i<SequenceLength;i++)
        {
            int index=sequence[i];showingText=(i+1)+" / "+SequenceLength+"  ·  "+Names[index];SetLit(index);PlayTone(index);
            yield return WaitActive(.65f);SetLit(-1);yield return WaitActive(.3f);
        }
        showingText="";CurrentStage=Stage.Listening;playback=null;
    }
    void SelectNote(int index)
    {
        SetLit(index);lightUntil=Time.time+.45f;PlayTone(index);
        if(sequence[InputCount]!=index)
        {
            Mistakes++;feedback="Not quite! Watch the same sequence again.";feedbackUntil=Time.unscaledTime+2.5f;BeginPlayback();return;
        }
        InputCount++;
        if(InputCount<SequenceLength)return;
        if(Round<2){Round++;feedback="Nice! One more note joins the melody.";feedbackUntil=Time.unscaledTime+2.5f;BeginPlayback();return;}
        CurrentStage=Stage.Complete;SetLit(-1);
        bool awarded=false;
        if(!RewardClaimed && inventory && scroll){inventory.RestoreStack(scroll,inventory.Count(scroll)+1);RewardClaimed=true;awarded=true;}
        feedback=awarded?"Wonderful! You received 1 speed scroll.":"Wonderful! The candies love your melody.";
        feedbackUntil=Time.unscaledTime+8f;
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);
        if(awarded && !GameSaveManager.SaveCurrentGame())GameSaveManager.RequestAutosave();
    }
    void SetLit(int index)
    {
        lit=index;
        for(int i=0;i<notes.Count;i++)
        {
            var n=notes[i];n.renderer.sharedMaterial=i==index?n.bright:n.normal;
            n.candy.localScale=Vector3.one*(i==index?2.25f:1.9f);
        }
    }
    void PlayTone(int index){if(GameAudioPolicy.SoundEnabled && audioSource && tones[index])audioSource.PlayOneShot(tones[index]);}
    void StopPlayback(){if(playback!=null){StopCoroutine(playback);playback=null;}showingText="";}
    void UpdateHud(bool near)
    {
        if(!hudCard)return;
        if(hudCard.activeSelf!=near)hudCard.SetActive(near);if(!near)return;
        title.text="CANDY MELODY GARDEN"+(CurrentStage==Stage.Showing || CurrentStage==Stage.Listening?"  ·  ROUND "+(Round+1)+" / 3":"");
        if(!string.IsNullOrEmpty(feedback) && Time.unscaledTime<feedbackUntil && CurrentStage!=Stage.Listening)detail.text=feedback+(CurrentStage==Stage.Showing && !string.IsNullOrEmpty(showingText)?"\nWATCH: "+showingText:"");
        else if(CurrentStage==Stage.Showing)detail.text="WATCH AND LISTEN\n"+(string.IsNullOrEmpty(showingText)?"Get ready...":showingText);
        else if(CurrentStage==Stage.Listening)detail.text="REPEAT THE ORDER  ·  "+InputCount+" / "+SequenceLength+"\nWalk into each candy to play it. Step away before touching it again. Use the sign to replay.";
        else if(CurrentStage==Stage.Complete && Time.unscaledTime<feedbackUntil)detail.text=feedback+"\nPress "+GameControls.Prompt(GameControls.Command.Interact)+" at the sign to play again.";
        else detail.text="Watch the lights and repeat the melody.\nThree rounds. The sign lets you replay any pattern.";
    }
    void OnDisable(){StopPlayback();SetLit(-1);if(audioSource)audioSource.Stop();}
    void OnDestroy(){foreach(var clip in tones)if(clip)Destroy(clip);foreach(var material in materials)if(material)Destroy(material);}
}
