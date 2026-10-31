using Mavis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>A collected bracelet switches the island's movement between its two gravity modes.</summary>
[DisallowMultipleComponent]
public sealed class GravityWristbandAbility : MonoBehaviour
{
    EquipmentItem item;
    EquipmentInventory inventory;
    MarshmallowTraversal traversal;
    ThirdPersonPlayerController movement;
    PlayerHealth health;
    GameObject worn, hud;
    Text status;
    public bool Owned => item && inventory && inventory.Count(item)>0;
    public bool NormalGravity => traversal && traversal.NormalGravity;

    public void Initialize(EquipmentItem value)
    {
        item=value;inventory=GetComponent<EquipmentInventory>();traversal=GetComponent<MarshmallowTraversal>();
        movement=GetComponent<ThirdPersonPlayerController>();health=GetComponent<PlayerHealth>();
        if(inventory){inventory.Collected-=OnCollected;inventory.Collected+=OnCollected;}
        Refresh();
    }
    void OnCollected(EquipmentItem collected){if(collected==item)Refresh();}
    void Update()
    {
        if(!Owned || !traversal || !traversal.Active)return;
        if(!worn)Attach();
        if(hud)hud.SetActive(!GameUITheme.ModalOpen && movement && movement.enabled && !(health && health.currentHealth<=0));
        if(!movement || !movement.enabled || movement.ExternalControlLock || movement.ExternalMovementLock ||
            health && health.currentHealth<=0 || GameUITheme.ModalOpen || Time.timeScale<=0 ||
            Cursor.lockState!=CursorLockMode.Locked || Keyboard.current==null)return;
        if(Keyboard.current.bKey.wasPressedThisFrame)SetNormalGravity(!NormalGravity);
    }
    public bool SetNormalGravity(bool value)
    {
        if(!Owned || !traversal || !traversal.Active)return false;
        traversal.SetNormalGravity(value);RefreshStatus();return true;
    }
    void Refresh()
    {
        if(!Owned)return;
        Attach();
        if(!hud)
        {
            hud=new GameObject("Gravity wristband status",typeof(RectTransform),typeof(Canvas));
            hud.transform.SetParent(transform,false);var canvas=hud.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=60;GameUITheme.Scale(canvas);
            var rect=GameUITheme.Rect(hud.transform,"Gravity status",new Vector2(1,0),new Vector2(340,42));
            rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-28,78);
            var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.04f,.04f,.06f,.78f);bg.raycastTarget=false;
            var text=new GameObject("Mode",typeof(RectTransform),typeof(Text));text.transform.SetParent(rect,false);
            var tr=(RectTransform)text.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            status=text.GetComponent<Text>();status.font=GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status.fontSize=20;status.alignment=TextAnchor.MiddleCenter;status.color=Color.white;status.raycastTarget=false;
            GameLocalization.Changed+=RefreshStatus;
        }
        RefreshStatus();
    }
    void RefreshStatus(){if(status)status.text=GameLocalization.Text(NormalGravity ? "B  ·  Gravity: normal" : "B  ·  Gravity: low");}
    void Attach()
    {
        if(worn || !item || !item.worldModel || !movement || !movement.CharacterAnimator || !movement.CharacterAnimator.isHuman)return;
        var anim=movement.CharacterAnimator;
        var arm=anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);var hand=anim.GetBoneTransform(HumanBodyBones.LeftHand);
        if(!arm || !hand)return;
        var axis=hand.position-arm.position;if(axis.sqrMagnitude<.0001f)return;
        worn=Instantiate(item.worldModel);worn.name="Gravity wristband - worn";
        // The imported bracelet lies in its local XY plane (its normal is Z).
        // Align that normal with the forearm and keep the cuff above the hand.
        worn.transform.SetParent(arm,true);worn.transform.position=Vector3.Lerp(hand.position,arm.position,.40f);
        worn.transform.rotation=Quaternion.FromToRotation(Vector3.forward,axis.normalized);
        float parentScale=Mathf.Max(.001f,Mathf.Abs(arm.lossyScale.x));worn.transform.localScale=Vector3.one*(.30f/parentScale);
        foreach(var collider in worn.GetComponentsInChildren<Collider>())collider.enabled=false;
    }
    void OnDestroy(){if(inventory)inventory.Collected-=OnCollected;GameLocalization.Changed-=RefreshStatus;}
}
