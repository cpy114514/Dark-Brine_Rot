using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Adds a named pickup to an already positioned model without duplicating its mesh.</summary>
    [DisallowMultipleComponent]
    public sealed class PlacedEquipmentPickup : MonoBehaviour
    {
        public EquipmentItem item;
        public float pickupRadius = 2.5f;
        public bool IsCollected { get; private set; }
        public Vector3 GuidancePosition => anchor + Vector3.up * 1.5f;
        EquipmentInventory player;
        Camera viewer;
        RectTransform caption;
        CanvasGroup captionGroup;
        Text label;
        LineRenderer ring;
        Material markerMaterial;
        Vector3 anchor;
        float age, searchAt;
        readonly RaycastHit[] hits = new RaycastHit[32];

        void Start()
        {
            if (!item) { enabled=false; return; }
            var renderers=GetComponentsInChildren<Renderer>();
            Bounds bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            anchor=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            // The author keeps control of the model placement. Only the marker follows the floor.
            float nearest=float.PositiveInfinity;
            int count=Physics.RaycastNonAlloc(bounds.center+Vector3.up*5,Vector3.down,hits,bounds.size.y+10,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(!hits[i].transform.IsChildOf(transform) && !CombatBodyCollision.IsActor(hits[i].collider) && hits[i].normal.y>.6f && hits[i].distance<nearest)
                {nearest=hits[i].distance;anchor.y=hits[i].point.y;}
            var marker=new GameObject("M4A1 loot marker",typeof(LineRenderer));marker.transform.SetParent(transform,false);
            ring=marker.GetComponent<LineRenderer>();ring.useWorldSpace=true;ring.loop=true;ring.positionCount=40;ring.widthMultiplier=.055f;
            ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
            markerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));markerMaterial.SetColor("_BaseColor",new Color(1,.75f,.23f));ring.sharedMaterial=markerMaterial;
            float radius=Mathf.Max(.65f,Mathf.Max(bounds.extents.x,bounds.extents.z)*.45f);
            for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;ring.SetPosition(i,anchor+new Vector3(Mathf.Cos(a)*radius,.055f,Mathf.Sin(a)*radius));}
            var go=new GameObject("Loot name - M4A1",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));go.transform.SetParent(transform,false);
            caption=(RectTransform)go.transform;caption.position=new Vector3(bounds.center.x,Mathf.Max(bounds.max.y+.65f,anchor.y+5.5f),bounds.center.z);
            caption.sizeDelta=new Vector2(420,110);var scale=transform.lossyScale;caption.localScale=new Vector3(.012f/Mathf.Abs(scale.x),.012f/Mathf.Abs(scale.y),.012f/Mathf.Abs(scale.z));
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;captionGroup=go.GetComponent<CanvasGroup>();captionGroup.blocksRaycasts=false;captionGroup.interactable=false;
            var text=new GameObject("Name",typeof(RectTransform),typeof(Text));text.transform.SetParent(go.transform,false);
            var rect=(RectTransform)text.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            label=text.GetComponent<Text>();label.font=GameLocalization.Font?GameLocalization.Font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=28;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(1,.83f,.39f);label.raycastTarget=false;
            var outline=text.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.95f);outline.effectDistance=new Vector2(1.5f,-1.5f);
            GameLocalization.Changed+=RefreshLabel;RefreshLabel();
        }

        void RefreshLabel(){if(label)label.text="▼ "+item.LocalizedName+"\n"+GameLocalization.Text("Loot · Walk up to collect");}
        void Update()
        {
            if(IsCollected || !item)return;
            if(Time.unscaledTime>=searchAt){searchAt=Time.unscaledTime+.5f;if(!player)player=FindFirstObjectByType<EquipmentInventory>();if(!viewer)viewer=Camera.main;}
            if(player && player.Count(item)>0){gameObject.SetActive(false);Destroy(gameObject);return;}
            if(caption && viewer)
            {
                float distance=(viewer.transform.position-caption.position).sqrMagnitude;bool visible=distance<65*65;
                captionGroup.alpha=visible?1:0;ring.enabled=visible;
                if(visible)caption.rotation=viewer.transform.rotation;
            }
            if(PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput)return;
            age+=Time.deltaTime;if(age>=.75f && player)TryCollect(player);
        }
        public bool TryCollect(EquipmentInventory inventory)
        {
            if(IsCollected || !item || !inventory || !inventory.gameObject.activeInHierarchy || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput)return false;
            var health=inventory.GetComponent<PlayerHealth>();var movement=inventory.GetComponent<ThirdPersonPlayerController>();
            if(health && health.currentHealth<=0 || movement && (!movement.enabled || movement.ExternalControlLock))return false;
            var body=inventory.GetComponent<CharacterController>();Vector3 feet=inventory.transform.position;if(body)feet.y=body.bounds.min.y;
            Vector3 gap=feet-anchor;if(Mathf.Abs(gap.y)>3 || new Vector2(gap.x,gap.z).sqrMagnitude>pickupRadius*pickupRadius)return false;
            Vector3 from=feet+Vector3.up*.8f,to=anchor+Vector3.up*.2f,ray=to-from;
            int count=Physics.RaycastNonAlloc(from,ray.normalized,hits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(inventory.transform) && !hits[i].transform.IsChildOf(transform))return false;
            IsCollected=true;GameWorldProgress.Record(this, 0);inventory.Collect(item);gameObject.SetActive(false);Destroy(gameObject);return true;
        }
        void OnDestroy(){GameLocalization.Changed-=RefreshLabel;if(markerMaterial)Destroy(markerMaterial);}
    }
}
