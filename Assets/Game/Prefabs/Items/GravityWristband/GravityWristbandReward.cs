using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Appears only after every scene-authored marshmallow has actually died.</summary>
public sealed class GravityWristbandReward : MonoBehaviour
{
    public Transform enemyRoot;
    public EquipmentItem item;
    public GameObject model;
    public float pickupRadius=2.6f;
    public int Total { get; private set; }
    public int Defeated => defeated.Count;
    public int Remaining => Mathf.Max(0,Total-Defeated);
    public bool Unlocked => Total>0 && Defeated==Total;
    public bool Collected => inventory && item && inventory.Count(item)>0;
    readonly HashSet<int> defeated=new HashSet<int>();
    readonly Dictionary<MarshmallowEnemyHealth,UnityAction> listeners=new Dictionary<MarshmallowEnemyHealth,UnityAction>();
    EquipmentInventory inventory;
    ThirdPersonPlayerController player;
    CharacterController body;
    GravityWristbandAbility ability;
    GameObject presentation, display, caption;
    Camera viewer;
    float announcedUntil;
    Text message, guidance, remainingLabel;
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
        foreach(var enemy in enemies)
        {
            var captured=enemy;int id=enemy.GetInstanceID();
            UnityAction onDeath=()=>{if(defeated.Add(id) && Unlocked)Reveal();};
            listeners.Add(captured,onDeath);enemy.OnDeath.AddListener(onDeath);
            if(enemy.IsDead)defeated.Add(id);
        }
        BuildPresentation();
        if(Unlocked && !Collected)Reveal();else presentation.SetActive(false);
    }
    void BuildPresentation()
    {
        presentation=new GameObject("Unlocked gravity wristband");presentation.transform.SetParent(transform,false);
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
        var hud=new GameObject("Gravity reward notification",typeof(RectTransform),typeof(Canvas));hud.transform.SetParent(transform,false);
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=65;GameUITheme.Scale(canvas);
        message=GameUITheme.Label(hud.transform,"Reward notification",28,new Vector2(0,-115),new Vector2(1050,60),TextAnchor.MiddleCenter);
        message.text=GameLocalization.Text("Gravity Wristband unlocked! Find it beyond the marshmallows.");message.gameObject.SetActive(false);
        guidance=GameUITheme.Label(hud.transform,"Reward direction and distance",22,new Vector2(0,-165),new Vector2(560,42),TextAnchor.MiddleCenter);
        guidance.color=new Color(.75f,.95f,1);guidance.gameObject.SetActive(false);
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
    void Reveal()
    {
        if(Collected || !presentation)return;
        presentation.SetActive(true);announcedUntil=Time.unscaledTime+8f;
        if(message)message.gameObject.SetActive(true);
    }
    void Update()
    {
        if(!presentation || !player || !inventory)return;
        UpdateEnemyHud();
        if(message && message.gameObject.activeSelf && Time.unscaledTime>=announcedUntil)message.gameObject.SetActive(false);
        if(!Unlocked || Collected){if(presentation.activeSelf)presentation.SetActive(false);if(guidance)guidance.gameObject.SetActive(false);return;}
        if(!presentation.activeSelf)Reveal();
        if(display){display.transform.localRotation=Quaternion.Euler(0,Time.time*35f,0);display.transform.localPosition=new Vector3(0,1.7f+Mathf.Sin(Time.time*1.8f)*.18f,0);}
        if(!viewer)viewer=Camera.main;if(viewer && caption)caption.transform.rotation=viewer.transform.rotation;
        if(guidance)
        {
            guidance.gameObject.SetActive(!GameUITheme.ModalOpen);
            Vector3 toward=Vector3.ProjectOnPlane(transform.position-player.transform.position,Vector3.up);
            Vector3 forward=viewer ? Vector3.ProjectOnPlane(viewer.transform.forward,Vector3.up).normalized : player.transform.forward;
            Vector3 right=viewer ? Vector3.ProjectOnPlane(viewer.transform.right,Vector3.up).normalized : player.transform.right;
            float ahead=Vector3.Dot(forward,toward),side=Vector3.Dot(right,toward);
            string arrow=Mathf.Abs(side)>Mathf.Abs(ahead)*.45f ? (side>0 ? "→" : "←") : ahead>=0 ? "↑" : "↓";
            guidance.text=arrow+"  "+GameLocalization.Text("Gravity Wristband")+"  ·  "+Mathf.RoundToInt(toward.magnitude)+" m";
        }
        if(PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || !player.enabled || player.ExternalControlLock || player.ExternalMovementLock)return;
        var health=player.GetComponent<PlayerHealth>();if(health && health.currentHealth<=0)return;
        Vector3 feet=player.transform.position;if(body)feet.y=body.bounds.min.y;
        Vector3 delta=feet-transform.position;
        if(Mathf.Abs(delta.y)<=3 && new Vector2(delta.x,delta.z).sqrMagnitude<=pickupRadius*pickupRadius && inventory.Collect(item))
        {presentation.SetActive(false);ability.Initialize(item);}
    }
    void UpdateEnemyHud()
    {
        bool show=Remaining>0 && !GameUITheme.ModalOpen && player.enabled &&
            !player.ExternalControlLock && !player.ExternalMovementLock && !SecondIslandAwakening.BlocksInputFor(player);
        if(remainingPanel)remainingPanel.SetActive(show);
        if(remainingLabel && show)remainingLabel.text=GameLocalization.Format("Marshmallows remaining: {0}",Remaining);
        if(!viewer)viewer=Camera.main;
        int count=0;
        if(show && Remaining<=5 && viewer)
        {
            foreach(var enemy in listeners.Keys)
            {
                if(!enemy || enemy.IsDead || !enemy.gameObject.activeInHierarchy)continue;
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
