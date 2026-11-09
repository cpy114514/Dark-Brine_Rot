using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Offers the arrival wristband and tracks the island's enemy camps.</summary>
public sealed class GravityWristbandReward : MonoBehaviour
{
    public Transform enemyRoot;
    public EquipmentItem item;
    public GameObject model;
    public float pickupRadius=2.6f;
    public int Total { get; private set; }
    public int Defeated => defeated.Count;
    public int Remaining => Mathf.Max(0,Total-Defeated);
    public bool MainCampsComplete => MainCampsCleared == MainCampCount;
    public bool Collected => inventory && item && inventory.Count(item)>0;
    readonly HashSet<int> defeated=new HashSet<int>();
    readonly HashSet<string> defeatedKeys=new HashSet<string>();
    readonly Dictionary<int,string> enemyKeys=new Dictionary<int,string>();
    readonly Dictionary<int,int> enemyCamps=new Dictionary<int,int>();
    // Keep authored sibling indices stable because existing saves use them as enemy keys.
    static readonly int[] MainCampRoute = { 1, 0, 4 };
    static readonly string[] MainCampNames = { "Mint Gate", "Swirl Bend", "Wafer Ridge" };
    int[] campRemaining;
    int[] campPopulation;
    Transform[] campMapAnchors;
    Transform[] campSigns;
    Text[] campSignText;
    public int CampCount => campRemaining?.Length ?? 0;
    public int ClearedCamps { get { int count=0; for(int i=0;i<CampCount;i++)if(CampCleared(i))count++;return count; } }
    public int MainCampCount => MainCampRoute.Length;
    public int MainCampsCleared { get { int count=0; foreach(int camp in MainCampRoute) if(CampCleared(camp)) count++; return count; } }
    public int NextMainCamp { get { foreach(int camp in MainCampRoute) if(!CampCleared(camp)) return camp; return -1; } }
    public bool CampCleared(int index) => CampHasEnemies(index) && campRemaining[index]==0;
    bool CampHasEnemies(int index) => campPopulation!=null && index>=0 && index<campPopulation.Length && campPopulation[index]>0;
    public int CampEnemiesRemaining(int index) => CampHasEnemies(index) ? campRemaining[index] : 0;
    public bool IsMainCamp(int index) => System.Array.IndexOf(MainCampRoute,index)>=0;
    public string CampName(int index)
    {
        int step=System.Array.IndexOf(MainCampRoute,index);
        return step>=0 ? MainCampNames[step] : "Candy Camp "+(index+1);
    }
    public Transform CampMapAnchor(int index) => campMapAnchors!=null && index>=0 && index<campMapAnchors.Length ? campMapAnchors[index] : null;
    readonly Dictionary<MarshmallowEnemyHealth,UnityAction> listeners=new Dictionary<MarshmallowEnemyHealth,UnityAction>();
    EquipmentInventory inventory;
    ThirdPersonPlayerController player;
    CharacterController body;
    GravityWristbandAbility ability;
    GameObject presentation, display, caption;
    Camera viewer;
    float announcedUntil;
    Text message, remainingLabel;
    GameObject remainingPanel;
    readonly RectTransform[] enemyMarkers=new RectTransform[5];
    readonly Text[] enemyMarkerLabels=new Text[5];
    readonly Vector2[] markerAnchors=new Vector2[5];
    public int ActiveEnemyMarkers { get; private set; }
    public bool Visible => presentation && presentation.activeSelf;

    void Start()
    {
        player=FindFirstObjectByType<ThirdPersonPlayerController>();
        if(!player || !enemyRoot || !item || !model){enabled=false;return;}
        body=player.GetComponent<CharacterController>();inventory=player.GetComponent<EquipmentInventory>();
        if(!inventory){enabled=false;return;}
        ability=player.GetComponent<GravityWristbandAbility>();if(!ability)ability=player.gameObject.AddComponent<GravityWristbandAbility>();
        ability.Initialize(item);
        var enemies=enemyRoot.GetComponentsInChildren<MarshmallowEnemyHealth>(true);Total=enemies.Length;
        campRemaining=new int[enemyRoot.childCount];
        campPopulation=new int[campRemaining.Length];
        var campCenters=new Vector3[campRemaining.Length];
        foreach(var enemy in enemies)
        {
            var captured=enemy;int id=enemy.GetInstanceID();
            enemyKeys[id]=StableKey(enemy.transform);
            var camp=enemy.transform;while(camp.parent && camp.parent!=enemyRoot)camp=camp.parent;
            int campIndex=camp.parent==enemyRoot ? camp.GetSiblingIndex() : -1;
            enemyCamps[id]=campIndex;if(campIndex>=0 && campIndex<campRemaining.Length){campRemaining[campIndex]++;campPopulation[campIndex]++;campCenters[campIndex]+=enemy.transform.position;}
            if(campIndex==MainCampRoute[0] && !enemy.IsDead)
            {
                // The first pack leaves enough room to learn combat in low gravity.
                enemy.maxHealth*=.60f;
                enemy.currentHealth=Mathf.Min(enemy.currentHealth,enemy.maxHealth);
                if(enemy.TryGetComponent<ChickenBroAI>(out var brain))
                {
                    brain.strikeDamage*=.65f;
                    brain.lungeDamage*=.65f;
                    brain.attackCooldown*=1.25f;
                }
            }
            else if(campIndex==MainCampRoute[1] && enemy.TryGetComponent<ChickenBroAI>(out var runner))
            {
                runner.runSpeed*=1.12f;
                runner.strikeDamage*=.90f;
            }
            UnityAction onDeath=()=>RecordDefeat(id, true);
            listeners.Add(captured,onDeath);enemy.OnDeath.AddListener(onDeath);
            if(enemy.IsDead)RecordDefeat(id, false);
        }
        campMapAnchors=new Transform[campRemaining.Length];
        campSigns=new Transform[campRemaining.Length];campSignText=new Text[campRemaining.Length];
        Color[] campColors={new Color(1f,.73f,.79f),new Color(.7f,.9f,1f),new Color(.73f,1f,.83f),new Color(1f,.9f,.62f),new Color(.88f,.76f,1f),new Color(1f,.78f,.63f),new Color(.79f,.98f,.66f)};
        for(int i=0;i<campMapAnchors.Length;i++)
        {
            var anchor=new GameObject("Camp map anchor").transform;anchor.SetParent(enemyRoot.GetChild(i),false);
            if(campPopulation[i]>0)anchor.position=campCenters[i]/campPopulation[i];
            campMapAnchors[i]=anchor;
            SkyboardReward.Label(anchor,CampName(i).ToUpperInvariant()+(CampCleared(i)?"\nCLEARED":""));
            var sign=anchor.Find("Pickup label");campSigns[i]=sign;
            if(sign)
            {
                sign.name="Candy camp landmark";sign.localPosition=Vector3.up*5f;sign.localScale=Vector3.one*.018f;
                var text=sign.GetComponentInChildren<Text>();campSignText[i]=text;
                if(text){text.fontSize=32;text.color=campColors[i%campColors.Length];}
                sign.gameObject.SetActive(false);
            }
        }
        SecondIslandArrivalTrail.Build(this, player);
        BuildPresentation();
        presentation.SetActive(!Collected);
    }
    string StableKey(Transform item)
    {
        string key="";
        while(item && item!=enemyRoot){key="/"+item.GetSiblingIndex()+key;item=item.parent;}
        return key;
    }
    void RecordDefeat(int id,bool notify)
    {
        if(!defeated.Add(id))return;
        if(enemyKeys.TryGetValue(id,out string key))defeatedKeys.Add(key);
        if(enemyCamps.TryGetValue(id,out int camp) && camp>=0 && camp<campRemaining.Length)
        {
            campRemaining[camp]=Mathf.Max(0,campRemaining[camp]-1);
            if(campRemaining[camp]==0 && campSignText!=null && camp<campSignText.Length && campSignText[camp])
                campSignText[camp].text=CampName(camp).ToUpperInvariant()+"\nCLEARED";
            if(notify && campRemaining[camp]==0)
            {
                string reward=RestoreAfterCamp();
                if(message)
                {
                    message.text=IsMainCamp(camp)
                        ? CampName(camp).ToUpperInvariant()+" CLEARED  ·  "+MainCampsCleared+" / "+MainCampCount+reward
                        : "ISLAND SWEEP  ·  "+ClearedCamps+" / "+CampCount+" CAMPS"+reward;
                    message.gameObject.SetActive(true);announcedUntil=Time.unscaledTime+5f;
                }
            }
        }
        if(notify)GameSaveManager.RequestAutosave();
    }
    string RestoreAfterCamp()
    {
        if(!player)return "";
        var health=player.GetComponent<PlayerHealth>();
        if(!health || health.currentHealth<=0)return "";
        var stamina=player.GetComponent<PlayerStamina>();
        float oldHealth=health.currentHealth,oldStamina=stamina?stamina.currentStamina:0f;
        health.currentHealth=Mathf.Min(health.maxHealth,health.currentHealth+health.maxHealth*.1f);
        if(stamina)stamina.currentStamina=Mathf.Min(stamina.maxStamina,stamina.currentStamina+15f);
        int restoredHealth=Mathf.RoundToInt(health.currentHealth-oldHealth);
        int restoredStamina=stamina?Mathf.RoundToInt(stamina.currentStamina-oldStamina):0;
        if(restoredHealth==0 && restoredStamina==0)return "";
        return (restoredHealth>0 ? "  ·  +"+restoredHealth+" HP" : "")+
            (restoredStamina>0 ? "  ·  +"+restoredStamina+" SP" : "");
    }
    public List<string> CaptureDefeated() => new List<string>(defeatedKeys);
    public void RestoreDefeated(List<string> keys)
    {
        if(keys==null)return;
        var restored=new HashSet<string>(keys);
        foreach(var pair in listeners)
        {
            var enemy=pair.Key;if(!enemy)continue;int id=enemy.GetInstanceID();
            if(!enemyKeys.TryGetValue(id,out var key) || !restored.Contains(key))continue;
            RecordDefeat(id,false);enemy.currentHealth=0;enemy.gameObject.SetActive(false);
        }
    }
    void BuildPresentation()
    {
        presentation=new GameObject("Gravity wristband pickup");presentation.transform.SetParent(transform,false);
        var pickupCollider=presentation.AddComponent<SphereCollider>();
        pickupCollider.isTrigger=true;pickupCollider.center=new Vector3(0,1.7f,0);pickupCollider.radius=1.25f;
        display=Instantiate(model,presentation.transform);display.name="Gravity wristband display";
        display.transform.localPosition=new Vector3(0,1.7f,0);display.transform.localScale=Vector3.one*1.8f;
        foreach(var c in display.GetComponentsInChildren<Collider>())c.enabled=false;
        var ringObject=new GameObject("Reward marker",typeof(LineRenderer));ringObject.transform.SetParent(presentation.transform,false);
        var ring=ringObject.GetComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=40;ring.widthMultiplier=.08f;
        ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
        var shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader){var material=new Material(shader);material.SetColor("_BaseColor",new Color(.35f,.9f,1));ring.material=material;}
        for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*1.5f,.08f,Mathf.Sin(a)*1.5f));}
        caption=new GameObject("Gravity wristband pickup label",typeof(RectTransform),typeof(Canvas));caption.transform.SetParent(presentation.transform,false);
        caption.transform.localPosition=new Vector3(0,3.5f,0);caption.transform.localScale=Vector3.one*.012f;
        var rect=(RectTransform)caption.transform;rect.sizeDelta=new Vector2(460,120);caption.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var text=new GameObject("Label",typeof(RectTransform),typeof(Text));text.transform.SetParent(caption.transform,false);
        var tr=(RectTransform)text.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
        var label=text.GetComponent<Text>();label.font=GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize=28;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.75f,.95f,1);label.raycastTarget=false;
        label.text=GameLocalization.Text("Gravity Wristband\nWalk up to collect");
        var outline=text.AddComponent<Outline>();outline.effectColor=Color.black;outline.effectDistance=new Vector2(2,-2);
        SecondIslandBlackboards.Attach(caption);
        var hud=new GameObject("Gravity reward notification",typeof(RectTransform),typeof(Canvas));hud.transform.SetParent(transform,false);
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=65;GameUITheme.Scale(canvas);
        message=GameUITheme.Label(hud.transform,"Reward notification",28,new Vector2(0,-115),new Vector2(1050,60),TextAnchor.MiddleCenter);
        message.gameObject.SetActive(false);
        var counter=GameUITheme.Rect(hud.transform,"Marshmallows remaining",new Vector2(0,1),new Vector2(380,48));
        counter.pivot=new Vector2(0,1);counter.anchoredPosition=new Vector2(28,-105);
        var counterBg=counter.gameObject.AddComponent<Image>();counterBg.color=new Color(.05f,.04f,.07f,.76f);counterBg.raycastTarget=false;
        remainingPanel=counter.gameObject;
        var countObject=new GameObject("Count",typeof(RectTransform),typeof(Text));countObject.transform.SetParent(counter,false);
        var countRect=(RectTransform)countObject.transform;countRect.anchorMin=Vector2.zero;countRect.anchorMax=Vector2.one;
        countRect.offsetMin=new Vector2(12,0);countRect.offsetMax=new Vector2(-12,0);
        remainingLabel=countObject.GetComponent<Text>();remainingLabel.font=GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        remainingLabel.fontSize=22;remainingLabel.color=Color.white;remainingLabel.alignment=TextAnchor.MiddleLeft;remainingLabel.raycastTarget=false;
        for(int i=0;i<enemyMarkers.Length;i++)
        {
            var marker=GameUITheme.Rect(hud.transform,"Remaining marshmallow marker "+(i+1),new Vector2(.5f,.5f),new Vector2(130,70));
            marker.pivot=new Vector2(.5f,.5f);enemyMarkers[i]=marker;
            var markerText=marker.gameObject.AddComponent<Text>();enemyMarkerLabels[i]=markerText;
            markerText.font=remainingLabel.font;markerText.fontSize=23;markerText.alignment=TextAnchor.MiddleCenter;
            markerText.color=new Color(1f,.85f,.1f,1f);markerText.raycastTarget=false;
            var markerOutline=marker.gameObject.AddComponent<Outline>();markerOutline.effectColor=Color.black;markerOutline.effectDistance=new Vector2(2,-2);
            marker.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if(!presentation || !player || !inventory)return;
        if(!viewer)viewer=Camera.main;
        if(campSigns!=null && viewer)
            for(int i=0;i<campSigns.Length;i++)
            {
                var sign=campSigns[i];if(!sign)continue;
                float distance=Vector3.ProjectOnPlane(sign.position-player.transform.position,Vector3.up).sqrMagnitude;
                // Once the player is inside a camp, keep its label out of combat view.
                bool nearby=distance>18f*18f && distance<75f*75f && !GameUITheme.ModalOpen && !SecondIslandAwakening.BlocksInputFor(player);
                if(sign.gameObject.activeSelf!=nearby)sign.gameObject.SetActive(nearby);
                if(nearby)sign.rotation=viewer.transform.rotation;
            }
        UpdateEnemyHud();
        if(message && message.gameObject.activeSelf && (Time.unscaledTime>=announcedUntil || MarshmallowHollowEntrance.Contains(player)))message.gameObject.SetActive(false);
        if(presentation.activeSelf==Collected)presentation.SetActive(!Collected);
        if(Collected)return;
        if(display){display.transform.localRotation=Quaternion.Euler(0,Time.time*35f,0);display.transform.localPosition=new Vector3(0,1.7f+Mathf.Sin(Time.time*1.8f)*.18f,0);}
        if(!viewer)viewer=Camera.main;if(viewer && caption)caption.transform.rotation=viewer.transform.rotation;
        if(PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || !player.enabled || player.ExternalControlLock || player.ExternalMovementLock)return;
        var health=player.GetComponent<PlayerHealth>();if(health && health.currentHealth<=0)return;
        Vector3 feet=player.transform.position;if(body)feet.y=body.bounds.min.y;
        Vector3 delta=feet-transform.position;
        if(Mathf.Abs(delta.y)<=3 && new Vector2(delta.x,delta.z).sqrMagnitude<=pickupRadius*pickupRadius && inventory.Collect(item))
        {presentation.SetActive(false);ability.Initialize(item);GameSaveManager.RequestAutosave();}
    }
    void UpdateEnemyHud()
    {
        int mainCamp=NextMainCamp;
        int left=mainCamp>=0 ? CampEnemiesRemaining(mainCamp) : Remaining;
        bool nearCamp=false;
        for(int i=0;i<CampCount && !nearCamp;i++)
        {
            if(mainCamp>=0 && i!=mainCamp || CampEnemiesRemaining(i)==0)continue;
            var anchor=CampMapAnchor(i);
            nearCamp=anchor && Vector3.ProjectOnPlane(anchor.position-player.transform.position,Vector3.up).sqrMagnitude<65f*65f;
        }
        bool show=left>0 && (nearCamp || mainCamp<0 && left<=5) && !GameUITheme.ModalOpen && !MarshmallowHollowEntrance.Contains(player) && player.enabled &&
            !player.ExternalControlLock && !player.ExternalMovementLock && !SecondIslandAwakening.BlocksInputFor(player);
        if(remainingPanel)remainingPanel.SetActive(show);
        if(remainingLabel && show)remainingLabel.text=(mainCamp>=0 ? CampName(mainCamp) : "Island sweep")+"  ·  "+left+" left";
        if(!viewer)viewer=Camera.main;
        int count=0;
        if(show && left<=5 && viewer)
        {
            foreach(var enemy in listeners.Keys)
            {
                if(!enemy || enemy.IsDead || !enemy.gameObject.activeInHierarchy)continue;
                if(mainCamp>=0 && (!enemyCamps.TryGetValue(enemy.GetInstanceID(),out int camp) || camp!=mainCamp))continue;
                var point=enemy.transform.position+Vector3.up*(enemy.healthBarHeight+1.0f);
                Vector3 viewport=viewer.WorldToViewportPoint(point);
                // Mirror targets behind the camera to the correct screen edge.
                if(viewport.z<0){viewport.x=1-viewport.x;viewport.y=1-viewport.y;}
                var marker=enemyMarkers[count];
                var anchor=new Vector2(Mathf.Clamp(viewport.x,.055f,.945f),Mathf.Clamp(viewport.y,.10f,.92f));
                // Multiple offscreen enemies often clamp to the same edge.
                // Stagger their labels so every remaining target stays legible.
                if(anchor.x<.23f && anchor.y>.82f)anchor.y=.79f;
                for(int attempt=0;attempt<12;attempt++)
                {
                    bool overlap=false;
                    for(int j=0;j<count;j++)
                        if(Mathf.Abs(anchor.x-markerAnchors[j].x)<.075f && Mathf.Abs(anchor.y-markerAnchors[j].y)<.075f)
                        {overlap=true;break;}
                    if(!overlap)break;
                    if(anchor.y>.19f)anchor.y-=.085f;
                    else {anchor.x=Mathf.Min(.945f,anchor.x+.08f);anchor.y=.79f;}
                }
                markerAnchors[count]=anchor;
                marker.anchorMin=marker.anchorMax=anchor;marker.anchoredPosition=Vector2.zero;
                float distance=Vector3.Distance(player.transform.position,enemy.transform.position);
                enemyMarkerLabels[count].text="◆\n"+Mathf.RoundToInt(distance)+" m";
                marker.gameObject.SetActive(true);count++;
                if(count==enemyMarkers.Length)break;
            }
        }
        for(int i=count;i<enemyMarkers.Length;i++)enemyMarkers[i].gameObject.SetActive(false);
        ActiveEnemyMarkers=count;
    }
    void OnDestroy()
    {
        foreach(var pair in listeners)if(pair.Key)pair.Key.OnDeath.RemoveListener(pair.Value);
        if(presentation){var ring=presentation.GetComponentInChildren<LineRenderer>(true);if(ring && ring.material)Destroy(ring.material);}
    }
}
