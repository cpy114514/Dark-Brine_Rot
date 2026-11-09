#if UNITY_EDITOR
using System.Collections.Generic;
using Mavis;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Equipment fixture instantiated explicitly by the editor verification.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandTestStation : MonoBehaviour
{
    public enum TestItem { Skyboard, Scroll, Wristband, TwinBlades, Rifle, Glasses, Healing }
    sealed class Pedestal { public TestItem item;public Transform root,visual,label;public Text text; }
    readonly List<Pedestal> pedestals=new List<Pedestal>();
    readonly List<Material> materials=new List<Material>();
    static SecondIslandTestStation current;
    static bool skyboardUnlimited,scrollUnlimited,rifleUnlimited,glassesUnlimited,healingUnlimited;
    public static bool UnlimitedSkyboard => GameSaveManager.EditorTestSessionActive && skyboardUnlimited;
    public static bool UnlimitedScrolls => GameSaveManager.EditorTestSessionActive && scrollUnlimited;
    public static bool UnlimitedRifle => GameSaveManager.EditorTestSessionActive && rifleUnlimited;
    public static bool UnlimitedGlasses => GameSaveManager.EditorTestSessionActive && glassesUnlimited;
    public static bool UnlimitedHealing => GameSaveManager.EditorTestSessionActive && healingUnlimited;
    public static bool ReservesInteraction => current && current.Reserved();
    public static bool Protects(PlayerHealth target)
    {
        if(!current || !target || !current.player || target.gameObject!=current.player.gameObject)return false;
        var delta=target.transform.position-current.transform.position;
        if(GameSaveManager.EditorTestSessionActive)return delta.x*delta.x+delta.z*delta.z<22f*22f && Mathf.Abs(delta.y)<18f;
        // Before opting in, protect only the immediate pickup pads; normal
        // enemy tests near spawn retain their usual behaviour.
        return current.Nearest()>=0;
    }
    public int PedestalCount => pedestals.Count;
    public Transform PedestalAt(int index)=>index>=0 && index<pedestals.Count?pedestals[index].root:null;
    public string LastMessage {get;private set;}="";
    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    EquipmentInventory inventory;
    SkyboardReward skyboard;
    GravityWristbandReward wristband;
    Collider island;
    GameObject statusCard;
    Text status;
    int lastInteractionFrame=-1,lastNearest=-2;
    float nextLookup,nextLabel,noticeUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){current=null;skyboardUnlimited=scrollUnlimited=rifleUnlimited=glassesUnlimited=healingUnlimited=false;}
    void Awake()
    {
        current=this;player=FindFirstObjectByType<ThirdPersonPlayerController>();
        if(!player){enabled=false;return;}
        body=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerHealth>();inventory=player.GetComponent<EquipmentInventory>();
        skyboard=FindFirstObjectByType<SkyboardReward>();wristband=FindFirstObjectByType<GravityWristbandReward>();
        var experience=player.GetComponent<MarshmallowIslandExperience>();island=experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if(!body || !inventory || !skyboard || !wristband || !island){Debug.LogError("Editor test items could not find their second-island dependencies.");enabled=false;return;}
        var spawn=player.transform.position;
        transform.position=spawn;
        var items=new[]{Resources.Load<EquipmentItem>(SkyboardReward.BoardItemPath),Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath),wristband.item,
            Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades"),Resources.Load<EquipmentItem>(SahurWeaponLoadout.M4ItemPath),Resources.Load<EquipmentItem>(WeaknessGlasses.ItemPath)};
        for(int i=0;i<7;i++)
        {
            float x=(i-3)*4.4f;var at=Ground(spawn+new Vector3(x,0,10));
            var root=new GameObject("TEST - "+(TestItem)i).transform;root.SetParent(transform,false);root.position=at;
            MakeBase(root,i);
            GameObject model=i==0?skyboard.skateboardModel:i==1?skyboard.scrollModel:i==2?wristband.model:i<6 && items[i]?items[i].worldModel:null;
            Transform visual;
            if(model){var o=SkyboardReward.MakeVisual(model,root,1.75f,i==0?skyboard.skateboardMaterial:null);SkyboardReward.CenterVisual(o,new Vector3(0,1.25f,0));visual=o.transform;}
            else visual=MakeHealingVisual(root);
            SkyboardReward.Label(root,Name((TestItem)i));var label=root.Find("Pickup label");label.localPosition=new Vector3(0,2.6f,0);label.localScale=Vector3.one*.011f;
            var text=label.GetComponentInChildren<Text>();label.gameObject.SetActive(false);
            pedestals.Add(new Pedestal{item=(TestItem)i,root=root,visual=visual,label=label,text=text});
        }
        BuildInformationSign(spawn);BuildStatus();
    }
    Vector3 Ground(Vector3 at)
    {
        var b=island.bounds;if(island.Raycast(new Ray(new Vector3(at.x,b.max.y+10,at.z),Vector3.down),out var hit,b.size.y+20))return hit.point+.04f*Vector3.up;
        throw new System.InvalidOperationException("No island ground for editor test items near "+at);
    }
    Material Material(string name,Color color)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");var m=new Material(shader){name=name};
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);else m.color=color;materials.Add(m);return m;
    }
    void MakeBase(Transform root,int i)
    {
        var colors=new[]{new Color(.95f,.55f,.70f),new Color(.56f,.76f,1),new Color(.58f,.92f,.80f),new Color(.93f,.70f,.43f),new Color(.70f,.69f,.95f),new Color(.58f,.84f,.9f),new Color(.97f,.70f,.69f)};
        var baseObject=GameObject.CreatePrimitive(PrimitiveType.Cube);baseObject.name="Test item pedestal";baseObject.transform.SetParent(root,false);
        baseObject.transform.localPosition=new Vector3(0,.22f,0);baseObject.transform.localScale=new Vector3(2.5f,.44f,2.5f);baseObject.GetComponent<Renderer>().sharedMaterial=Material("Test pedestal "+i,colors[i]);
        var trigger=root.gameObject.AddComponent<BoxCollider>();trigger.center=Vector3.up*1.15f;trigger.size=new Vector3(2.4f,2.3f,2.4f);trigger.isTrigger=true;
    }
    Transform MakeHealingVisual(Transform root)
    {
        var visual=new GameObject("Healing supply model").transform;visual.SetParent(root,false);visual.localPosition=Vector3.up*1.2f;
        var white=Material("Test pack cream",new Color(.95f,.92f,.83f));var red=Material("Test pack cross",new Color(.85f,.20f,.25f));
        Shape(visual,"Pack",Vector3.zero,new Vector3(1.15f,.8f,.48f),white);
        Shape(visual,"Cross horizontal",new Vector3(0,0,-.255f),new Vector3(.68f,.17f,.06f),red);
        Shape(visual,"Cross vertical",new Vector3(0,0,-.26f),new Vector3(.17f,.57f,.06f),red);
        return visual;
    }
    static void Shape(Transform parent,string name,Vector3 pos,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());
    }
    void BuildInformationSign(Vector3 spawn)
    {
        var prefab=Resources.Load<GameObject>("SecondIslandBlackboard/Candy Blackboard");if(!prefab)return;
        var obj=Instantiate(prefab,Ground(spawn+new Vector3(-17,0,8)),Quaternion.identity,transform);obj.name="Editor test items - information blackboard";
        var text=obj.GetComponentInChildren<TextMeshPro>(true);if(text)text.text="EDITOR TEST ITEMS\nF: collect nearby item\nUnlimited use · separate test save\nSafe area · combat resumes outside";
    }
    void BuildStatus()
    {
        var ui=new GameObject("Editor test session HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));ui.transform.SetParent(transform,false);
        var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=73;GameUITheme.Scale(canvas);
        var panel=GameUITheme.Rect(ui.transform,"Test mode card",new Vector2(1,.5f),new Vector2(380,110));panel.pivot=new Vector2(1,.5f);panel.anchoredPosition=new Vector2(-24,0);
        GameUITheme.Image(panel,new Color(.025f,.09f,.075f,.94f));status=GameUITheme.Label(panel,"Test session",18,new Vector2(-175,-55),new Vector2(350,100),TextAnchor.MiddleLeft);
        statusCard=panel.gameObject;statusCard.SetActive(false);
    }
    static string Name(TestItem item)
    {
        switch(item){case TestItem.Skyboard:return "TEST SKYBOARD\nUnlimited flight · no cooldown";
            case TestItem.Scroll:return "TEST SPEED SCROLLS\nUnlimited · practice timer in Tools";
            case TestItem.Wristband:return "TEST GRAVITY WRISTBAND\nB: switch gravity";
            case TestItem.TwinBlades:return "TEST TWIN BLADES\n2: equip";
            case TestItem.Rifle:return "TEST M4A1\n3: equip · unlimited ammunition";
            case TestItem.Glasses:return "TEST WEAKNESS GLASSES\nEquip in menu · G: activate";
            default:return "TEST HEALING SUPPLIES\nInfinite packs · restore HP and SP";}
    }
    int Nearest()
    {
        if(!player || !body || !body.enabled)return -1;
        int best=-1;float distance=3f*3f;
        for(int i=0;i<pedestals.Count;i++)
        {
            var d=player.transform.position-pedestals[i].root.position;float flat=d.x*d.x+d.z*d.z;
            if(flat<distance && Mathf.Abs(body.bounds.min.y-pedestals[i].root.position.y)<4f){distance=flat;best=i;}
        }
        return best;
    }
    bool Reserved()=>lastInteractionFrame==Time.frameCount || Nearest()>=0;
    void Update()
    {
        if(!player && Time.unscaledTime>=nextLookup)
        {nextLookup=Time.unscaledTime+1;player=FindFirstObjectByType<ThirdPersonPlayerController>();if(player){body=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerHealth>();inventory=player.GetComponent<EquipmentInventory>();}}
        if(!player || !body)return;
        bool allowed=Time.timeScale>0 && body.enabled && player.isActiveAndEnabled && (!health || health.currentHealth>0) && !player.ExternalControlLock && !player.ExternalMovementLock && !GameUITheme.ModalOpen && !SecondIslandAwakening.BlocksInputFor(player);
        int near=allowed?Nearest():-1;
        var camera=Camera.main;
        if(Time.unscaledTime>=nextLabel || near!=lastNearest)
        {
            nextLabel=Time.unscaledTime+.25f;lastNearest=near;
            for(int i=0;i<pedestals.Count;i++)
            {
                var p=pedestals[i];var d=player.transform.position-p.root.position;bool show=allowed && d.x*d.x+d.z*d.z<10f*10f && Mathf.Abs(body.bounds.min.y-p.root.position.y)<8;
                if(p.label.gameObject.activeSelf!=show)p.label.gameObject.SetActive(show);
                if(show)p.text.text=(i==near?GameControls.Prompt(GameControls.Command.Interact)+"  ·  ":"")+Name(p.item);
            }
        }
        foreach(var p in pedestals){if(p.label.gameObject.activeSelf && camera)p.label.rotation=camera.transform.rotation;if(p.visual)p.visual.Rotate(0,25f*Time.deltaTime,0,Space.World);}
        bool showStatus=GameSaveManager.EditorTestSessionActive && !GameUITheme.ModalOpen && !player.ExternalControlLock;
        if(statusCard.activeSelf!=showStatus)statusCard.SetActive(showStatus);
        if(showStatus)status.text="EDITOR TEST MODE\nUnlimited test items · separate save"+(Time.unscaledTime<noticeUntil?"\n"+LastMessage:"");
        if(!allowed || near<0 || Cursor.lockState!=CursorLockMode.Locked || !GameControls.Down(GameControls.Command.Interact))return;
        lastInteractionFrame=Time.frameCount;Give(pedestals[near].item);
    }
    bool Give(TestItem type)
    {
        if(!GameSaveManager.BeginEditorTestSession()){Show("Wait for the game to finish loading.");return false;}
        EquipmentItem item=null;
        switch(type)
        {
            case TestItem.Skyboard: item=Resources.Load<EquipmentItem>(SkyboardReward.BoardItemPath);skyboardUnlimited=true;skyboard.ClearTestCooldown();break;
            case TestItem.Scroll:
                item=Resources.Load<EquipmentItem>(SkyboardReward.ScrollItemPath);scrollUnlimited=true;
                if(!player.GetComponent<SecondIslandTestTimer>())player.gameObject.AddComponent<SecondIslandTestTimer>();break;
            case TestItem.Wristband:item=wristband.item;break;
            case TestItem.TwinBlades:item=Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades");break;
            case TestItem.Rifle:item=Resources.Load<EquipmentItem>(SahurWeaponLoadout.M4ItemPath);rifleUnlimited=true;break;
            case TestItem.Glasses:item=Resources.Load<EquipmentItem>(WeaknessGlasses.ItemPath);glassesUnlimited=true;break;
            case TestItem.Healing:
                healingUnlimited=true;HealingPackSupply.RefillAfterRespawn();if(health)health.currentHealth=health.maxHealth;
                var stamina=player.GetComponent<PlayerStamina>();if(stamina)stamina.currentStamina=stamina.maxStamina;break;
        }
        if(item)inventory.RestoreStack(item,type==TestItem.Scroll?Mathf.Max(10,inventory.Count(item)):Mathf.Max(1,inventory.Count(item)));
        if(type==TestItem.Skyboard)skyboard.ClearTestCooldown();
        if(type!=TestItem.Healing && !item){Show("That test item could not be loaded.");return false;}
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,player.transform.position,true);Show(type+" ready");GameSaveManager.RequestAutosave();return true;
    }
    void Show(string message){LastMessage=message;noticeUntil=Time.unscaledTime+4;}
    void OnDestroy(){if(current==this)current=null;foreach(var m in materials)if(m)Destroy(m);}
}

/// <summary>A non-blocking mock target for trying the scroll selection flow repeatedly.</summary>
public sealed class SecondIslandTestTimer : MonoBehaviour, ICooldownTarget
{
    public int Uses {get;private set;}
    public float CooldownRemaining=>1200;
    public string CooldownDisplayName=>"TEST TIMER (PRACTICE)";
    public bool CooldownTargetOwned=>SecondIslandTestStation.UnlimitedScrolls;
    public bool CanReduceCooldown=>CooldownTargetOwned;
    public bool ReduceCooldown(float seconds){if(!CanReduceCooldown || seconds<=0)return false;Uses++;return true;}
}
#endif
