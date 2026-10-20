using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-30)]
    public sealed class SahurWeaponLoadout : MonoBehaviour
    {
        public enum Weapon { Stick, CapriTwinBlades, M4A1 }
        public Weapon CurrentWeapon { get; private set; }
        public const string M4ItemPath = "Equipment/M4A1/M4A1";
        public Transform HeldRifle => rifle ? rifle.transform : null;
        SahurRifleAnimation rifleAnimation;
        public bool UsesRifle => CurrentWeapon == Weapon.M4A1;
        public bool HasM4A1 => inventory && rifleItem && inventory.Count(rifleItem)>0;
        public bool UsesTwinBlades => CurrentWeapon == Weapon.CapriTwinBlades;
        public bool HasTwinBlades => inventory && twinBlades && inventory.Count(twinBlades) > 0;
        public static string DisplayName(Weapon weapon) => GameLocalization.Text(weapon==Weapon.M4A1?"M4A1 assault rifle":weapon==Weapon.CapriTwinBlades?"Capri twin blades":"Sahur stick");
        public string WeaponName => DisplayName(CurrentWeapon);
        public string ShortName => GameLocalization.Text(UsesRifle?"M4A1":UsesTwinBlades?"Twin blades":"Stick");
        public bool Owns(Weapon weapon) => weapon==Weapon.Stick || weapon==Weapon.CapriTwinBlades&&HasTwinBlades || weapon==Weapon.M4A1&&HasM4A1;
        public event System.Action Changed;
        EquipmentInventory inventory;
        EquipmentItem twinBlades, rifleItem;
        SahurAttack attack;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        Renderer[] stickRenderers;
        GameObject left, right, rifle;
        CapsuleCollider leftHitbox, rightHitbox;
        bool initialized;
        Weapon? restoredWeapon;
        [Min(.1f)] public float twinBladeSizeMultiplier = 1.2f;
        public static Vector3 GripShaft(float side) => new Vector3(side * .18f, .41f, -.89f).normalized;

        void Start() => Initialize();
        public void Initialize()
        {
            if (initialized) return;
            attack = GetComponent<SahurAttack>();
            movement = GetComponent<ThirdPersonPlayerController>();
            health = GetComponent<PlayerHealth>();
            inventory = GetComponent<EquipmentInventory>();
            twinBlades = Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades");
            rifleItem = Resources.Load<EquipmentItem>(M4ItemPath);
            if (!attack || !inventory) return;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Sahur Stick") { stickRenderers = t.GetComponentsInChildren<Renderer>(true); break; }
            inventory.Collected += OnCollected;
            if (GetComponent<SahurTwinBladeMotion>() == null) gameObject.AddComponent<SahurTwinBladeMotion>();
            if (GetComponent<SahurTwinBladeReadyPose>() == null) gameObject.AddComponent<SahurTwinBladeReadyPose>();
            if (GetComponent<SahurTwinBladeLocomotion>() == null) gameObject.AddComponent<SahurTwinBladeLocomotion>();
            if (GetComponent<SahurTwinBladeHandGrip>() == null) gameObject.AddComponent<SahurTwinBladeHandGrip>();
            rifleAnimation = GetComponent<SahurRifleAnimation>();
            if (!rifleAnimation) rifleAnimation = gameObject.AddComponent<SahurRifleAnimation>();
            if (!GetComponent<SahurRifleCombat>()) gameObject.AddComponent<SahurRifleCombat>();
            if (!GetComponent<SahurSupplyGuidance>()) gameObject.AddComponent<SahurSupplyGuidance>();
            initialized = true;
        }
        void OnCollected(EquipmentItem item) { if (item == twinBlades || item == rifleItem) Changed?.Invoke(); }
        void Update()
        {
            if (!initialized) Initialize();
            if (restoredWeapon.HasValue) TryEquip(restoredWeapon.Value);
            if (!initialized || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput ||
                Cursor.lockState != CursorLockMode.Locked || Keyboard.current == null) return;
            var keys = Keyboard.current;
            if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) TryEquip(Weapon.Stick);
            else if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) TryEquip(Weapon.CapriTwinBlades);
            else if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) TryEquip(Weapon.M4A1);
        }

        public bool TryEquip(Weapon weapon, bool fromMenu = false)
        {
            Initialize();
            if (!initialized || !enabled || !gameObject.activeInHierarchy || (health && health.currentHealth <= 0) ||
                PauseSettingsMenu.IsOpen || (SahurLoadoutUI.BlocksInput && !(fromMenu && SahurLoadoutUI.IsOpen)) ||
                (movement && (!movement.enabled || movement.Swimming || movement.ExternalControlLock)) ||
                (GetComponent<SahurBoomerang>()?.IsBusy ?? false)) return false;
            if (!Owns(weapon)) return false;
            if (weapon == CurrentWeapon) { restoredWeapon = null; return true; }
            if (weapon == Weapon.CapriTwinBlades && !CreateBlades()) return false;
            if (weapon == Weapon.M4A1 && !CreateRifle()) return false;
            attack.SetTwinBladeMode(weapon == Weapon.CapriTwinBlades, rightHitbox, leftHitbox);
            CurrentWeapon = weapon;
            // A held rifle must not inherit the stick's melee swings.
            var arrival=GetComponent<FirstIslandArrival>();
            attack.enabled=!UsesRifle && (UsesTwinBlades || !arrival || arrival.HasStick);
            restoredWeapon = null;
            GetComponent<SahurTwinBladeLocomotion>()?.Refresh();
            if (stickRenderers != null) foreach (var r in stickRenderers) if (r) r.enabled = CurrentWeapon==Weapon.Stick;
            if (left) left.SetActive(UsesTwinBlades);
            if (right) right.SetActive(UsesTwinBlades);
            if (rifle) rifle.SetActive(UsesRifle);
            Changed?.Invoke();
            return true;
        }

        public void Restore(bool ownsTwinBlades, Weapon selected, bool ownsM4A1=false)
        {
            Initialize();
            if (ownsTwinBlades && inventory && twinBlades && !HasTwinBlades) inventory.Collect(twinBlades);
            if(ownsM4A1 && inventory && rifleItem && !HasM4A1)inventory.Collect(rifleItem);
            restoredWeapon = Owns(selected) ? selected : Weapon.Stick;
            // A saved pose may still be swimming or finishing its arrival sequence.
            // Retry on Update once normal weapon switching is available.
            TryEquip(restoredWeapon.Value);
        }

        bool CreateRifle()
        {
            if(rifle)return true;
            var animator=attack.animator;
            if(!rifleItem || !rifleItem.worldModel || !animator || !animator.isHuman)return false;
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);if(!hand)return false;
            rifle=Instantiate(rifleItem.worldModel,hand,false);rifle.name="Equipped M4A1";
            float handScale=Mathf.Max(.001f,Mathf.Abs(hand.lossyScale.y));
            float playerScale=Mathf.Max(.01f,Mathf.Abs(transform.lossyScale.y)/3f);
            float scale=2.2f*playerScale/(.84f*handScale);
            var rotation=Quaternion.FromToRotation(Vector3.left,GripShaft(1));
            rifle.transform.localRotation=rotation;rifle.transform.localScale=Vector3.one*scale;
            // The centred prefab's pistol grip lies right and below its receiver.
            rifle.transform.localPosition=new Vector3(-.015f,.075f,-.01f)-rotation*(new Vector3(.17f,-.085f,0)*scale);
            foreach(var c in rifle.GetComponentsInChildren<Collider>())c.enabled=false;
            rifle.SetActive(false);return true;
        }

        bool CreateBlades()
        {
            if (left && right) return true;
            var animator = attack.animator;
            if (!twinBlades || !twinBlades.worldModel || !animator || !animator.isHuman) return false;
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (!leftHand || !rightHand) return false;
            var model = twinBlades.worldModel;
            left = MakeBlade(model.transform.Find("Left blade"), leftHand, out leftHitbox);
            right = MakeBlade(model.transform.Find("Right blade"), rightHand, out rightHitbox);
            return left && right;
        }
        GameObject MakeBlade(Transform template, Transform hand, out CapsuleCollider hitbox)
        {
            hitbox = null;
            if (!template) return null;
            var obj = Instantiate(template.gameObject, hand, false);
            obj.name = template.name;
            var mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            float length = Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z));
            float playerScale = Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.y) / 3f);
            float handScale = Mathf.Max(.001f, Mathf.Abs(hand.lossyScale.y));
            float bladeScale = 2.1f * twinBladeSizeMultiplier * playerScale / Mathf.Max(.01f, length * handScale);
            obj.transform.localScale = Vector3.one * bladeScale;
            int axis = mesh.bounds.size.x > mesh.bounds.size.y && mesh.bounds.size.x > mesh.bounds.size.z ? 0 : mesh.bounds.size.y > mesh.bounds.size.z ? 1 : 2;
            float sign = Mathf.Sign(mesh.bounds.center[axis]);
            if (sign == 0) sign = 1;
            Vector3 tipAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
            // Put the handle, rather than the imported mesh pivot, into the palm.
            Vector3 handle = Vector3.zero, tip = Vector3.zero; int handleVertices = 0, tipVertices = 0;
            float end = sign > 0 ? mesh.bounds.min[axis] : mesh.bounds.max[axis];
            float tipEnd = sign > 0 ? mesh.bounds.max[axis] : mesh.bounds.min[axis];
            foreach (var point in mesh.vertices)
            {
                if (Mathf.Abs(point[axis] - end) < length * .18f) { handle += point; handleVertices++; }
                if (Mathf.Abs(point[axis] - tipEnd) < length * .12f) { tip += point; tipVertices++; }
            }
            if (handleVertices > 0) handle /= handleVertices;
            if (tipVertices > 0) tip /= tipVertices;
            Vector3 shaft = tipVertices > 0 ? (tip - handle).normalized : tipAxis * sign;
            float side = template.name.StartsWith("Left") ? -1f : 1f;
            var gripRotation = Quaternion.FromToRotation(shaft, GripShaft(side));
            obj.transform.localRotation = gripRotation;
            var palm = new Vector3(-side * .04f, .125f, -.005f);
            obj.transform.localPosition = palm - gripRotation * (handle * bladeScale);
            hitbox = obj.AddComponent<CapsuleCollider>();
            hitbox.isTrigger = true;
            hitbox.direction = axis;
            hitbox.center = mesh.bounds.center;
            hitbox.height = length;
            hitbox.radius = length * .055f;
            hitbox.enabled = false;
            obj.SetActive(false);
            return obj;
        }
        void LateUpdate()
        {
            if (!UsesTwinBlades && !UsesRifle) return;
            bool visible = movement && movement.enabled && !movement.Swimming && !(GetComponent<SahurSwimmingWeapon>()?.Climbing ?? false) && (!health || health.currentHealth > 0);
            if (left) left.SetActive(visible && UsesTwinBlades);
            if (right) right.SetActive(visible && UsesTwinBlades);
            if(rifle)rifle.SetActive(UsesRifle && (visible || (rifleAnimation && rifleAnimation.DeathPoseActive)));
            if(UsesRifle && attack.enabled)attack.enabled=false;
            // Swimming may restore the original stick's grip; it stays hidden with blades selected.
            if (stickRenderers != null) foreach (var r in stickRenderers) if (r) r.enabled = false;
        }
        void OnDestroy()
        {
            if (inventory) inventory.Collected -= OnCollected;
            if (left) Destroy(left);
            if (right) Destroy(right);
            if(rifle)Destroy(rifle);
        }
    }
}
