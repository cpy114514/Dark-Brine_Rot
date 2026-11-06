using Mavis;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One permanent, walk-up healing-pack pickup in the candy cottage.</summary>
[DisallowMultipleComponent]
public sealed class CandyHouseHealingPickup : MonoBehaviour
{
    [Range(0,2)] public int index;
    public Transform visual;
    public float pickupRadius = 2.1f;
    CandyHouseMarker cottage;
    CandyHouseHealingPickup[] nearbyPickups;
    ThirdPersonPlayerController player;
    CharacterController body;
    Text caption;
    Transform label;
    Vector3 restPosition;
    float nextSearch, nextCheck, nextCaptionRefresh;
    readonly RaycastHit[] hits = new RaycastHit[24];

    void Start()
    {
        cottage=GetComponentInParent<CandyHouseMarker>();
        if(cottage && cottage.HealingPackCollected(index)){gameObject.SetActive(false);return;}
        if(cottage)nearbyPickups=cottage.GetComponentsInChildren<CandyHouseHealingPickup>(true);
        if(visual)restPosition=visual.localPosition;
        SkyboardReward.Label(transform,"HEALING PACK +1");
        label=transform.Find("Pickup label");
        if(label)
        {
            label.localPosition=new Vector3(0,2.4f,0);
            label.localScale=Vector3.one*.011f;
            caption=label.GetComponentInChildren<Text>();
            if(caption)caption.fontSize=26;
            label.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if(!player && Time.unscaledTime>=nextSearch)
        {
            nextSearch=Time.unscaledTime+1;
            player=FindFirstObjectByType<ThirdPersonPlayerController>();
            if(player)body=player.GetComponent<CharacterController>();
        }
        if(visual)visual.localPosition=restPosition+Vector3.up*(.10f*Mathf.Sin(Time.time*1.8f+index));
        if(!player || !body)return;
        Vector3 difference=body.bounds.center-transform.position;
        float distanceSquared=difference.x*difference.x+difference.z*difference.z;
        bool nearby=distanceSquared<25f && Mathf.Abs(body.bounds.min.y-transform.position.y)<4f &&
            !GameUITheme.ModalOpen && !SecondIslandAwakening.BlocksInputFor(player);
        if(nearby && nearbyPickups!=null)
            foreach(var other in nearbyPickups)
            {
                if(!other || other==this || !other.gameObject.activeInHierarchy)continue;
                Vector3 d=body.bounds.center-other.transform.position;
                if(d.x*d.x+d.z*d.z<distanceSquared){nearby=false;break;}
            }
        if(label)
        {
            if(label.gameObject.activeSelf!=nearby)label.gameObject.SetActive(nearby);
            if(nearby && Camera.main)label.rotation=Camera.main.transform.rotation;
            if(nearby && caption && Time.unscaledTime>=nextCaptionRefresh)
            {
                nextCaptionRefresh=Time.unscaledTime+.2f;
                caption.text=HealingPackSupply.Remaining>=HealingPackSupply.Total ?
                    "HEALING PACKS FULL\n"+GameControls.Prompt(GameInputSettings.Action.Heal)+" TO USE ONE" :
                    "HEALING PACK +1\nWALK UP TO PICK UP";
            }
        }
        if(Time.time>=nextCheck){nextCheck=Time.time+.12f;TryCollect(player);}
    }
    public bool TryCollect(ThirdPersonPlayerController actor)
    {
        if(!isActiveAndEnabled || !actor || !actor.isActiveAndEnabled || !cottage || cottage.HealingPackCollected(index) ||
            Time.timeScale<=0 || GameUITheme.ModalOpen || actor.ExternalControlLock || actor.ExternalMovementLock || actor.Swimming)return false;
        var hp=actor.GetComponent<PlayerHealth>();var capsule=actor.GetComponent<CharacterController>();
        if(!hp || hp.currentHealth<=0 || !capsule || !capsule.enabled || HealingPackSupply.Remaining>=HealingPackSupply.Total)return false;
        Vector3 feet=capsule.bounds.center;feet.y=capsule.bounds.min.y;
        var delta=feet-transform.position;
        if(Mathf.Abs(delta.y)>2.5f || new Vector2(delta.x,delta.z).sqrMagnitude>pickupRadius*pickupRadius)return false;
        Vector3 from=feet+Vector3.up*.9f, to=transform.position+Vector3.up*.85f;
        Vector3 ray=to-from;
        if(ray.sqrMagnitude>.001f)
        {
            int count=Physics.RaycastNonAlloc(from,ray.normalized,hits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return false;
            for(int i=0;i<count;i++)
                if(hits[i].collider && !hits[i].transform.IsChildOf(actor.transform) && !hits[i].transform.IsChildOf(transform))return false;
        }
        if(HealingPackSupply.Add(1)!=1)return false;
        cottage.MarkHealingPackCollected(index);
        actor.GetComponent<SahurHealingPacks>()?.NotifyPickup(1);
        GameplayAudio.Play(GameplayAudio.Cue.Pickup,transform.position,true);
        gameObject.SetActive(false);
        if(!GameSaveManager.SaveCurrentGame())GameSaveManager.RequestAutosave();
        return true;
    }
    void OnValidate(){index=Mathf.Clamp(index,0,2);pickupRadius=Mathf.Max(.1f,pickupRadius);}
}
