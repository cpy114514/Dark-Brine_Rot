using UnityEngine;
using UnityEngine.InputSystem;
namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class WeaknessGlasses : MonoBehaviour
    {
        public const string ItemPath="Equipment/Nailong/WeaknessGlasses";
        public const float Duration=30f, Cooldown=900f;
        public bool Equipped {get;private set;}
        float activeUntil,readyAt; EquipmentInventory inventory; EquipmentItem item; GameObject worn; GUIStyle hudStyle;
        public float ActiveRemaining=>Mathf.Max(0,activeUntil-Time.time);
        public float CooldownRemaining=>Mathf.Max(0,readyAt-Time.time);
        public bool IsActive=>Equipped && ActiveRemaining>0 && GetComponent<PlayerHealth>()?.currentHealth>0;
        static readonly System.Collections.Generic.HashSet<WeaknessGlasses> wearers=new System.Collections.Generic.HashSet<WeaknessGlasses>();
        public static bool AnyActive {get{foreach(var wearer in wearers)if(wearer && wearer.isActiveAndEnabled && wearer.IsActive)return true;return false;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetState()=>wearers.Clear();
        void OnEnable()=>wearers.Add(this);
        public bool Owned {get{Initialize();return item && inventory && inventory.Count(item)>0;}}
        void Initialize(){if(!inventory)inventory=GetComponent<EquipmentInventory>();if(!item)item=Resources.Load<EquipmentItem>(ItemPath);}
        public bool Equip()
        {
            if(!Owned)return false;Equipped=true;if(worn)worn.SetActive(true);
            var animator=GetComponent<SahurAttack>()?.animator;
            if(!worn && item.worldModel && animator && animator.isHuman)
            {
                var head=animator.GetBoneTransform(HumanBodyBones.Head);
                if(head)
                {
                    worn=Instantiate(item.worldModel,head.position+transform.forward*.32f,transform.rotation);
                    worn.name="Equipped weakness glasses";worn.transform.SetParent(head,true);
                }
            }
            return true;
        }
        public void Unequip(){Equipped=false;if(worn)worn.SetActive(false);}
        public void Restore(bool owned,bool equipped,float active,float cooldown)
        {
            Initialize();if(owned && !Owned && inventory && item)inventory.Collect(item);
            Equipped=owned && equipped;if(Equipped)Equip();activeUntil=Time.time+Mathf.Clamp(active,0,Duration);readyAt=Time.time+Mathf.Clamp(cooldown,0,Cooldown);
        }
        public bool Activate()
        {
            if(!Equipped || !Owned || CooldownRemaining>0 || GetComponent<PlayerHealth>()?.currentHealth<=0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput)return false;
            activeUntil=Time.time+Duration;readyAt=Time.time+Cooldown;return true;
        }
        void Update()
        {
            if(Keyboard.current!=null && Keyboard.current.gKey.wasPressedThisFrame && Cursor.lockState==CursorLockMode.Locked)Activate();
        }
        void OnDisable()=>wearers.Remove(this);
        void OnGUI()
        {
            if(!Equipped || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput)return;
            string status=IsActive?"弱点显示 "+Mathf.CeilToInt(ActiveRemaining)+" 秒":CooldownRemaining>0?"弱点眼镜 冷却 "+System.TimeSpan.FromSeconds(CooldownRemaining).ToString(@"mm\:ss"):"弱点眼镜 [G] 开启 30 秒";
            if(hudStyle==null)hudStyle=new GUIStyle(GUI.skin.box){font=GameLocalization.Font?GameLocalization.Font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),fontSize=16};
            GUI.Box(new Rect(Screen.width*.5f-145,Screen.height-110,290,30),status,hudStyle);
        }
    }
}
