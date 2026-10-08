using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-30)]
    public sealed class SahurWeaponLoadout : MonoBehaviour
    {
        public enum Weapon { Stick, CapriTwinBlades }
        public Weapon CurrentWeapon { get; private set; }
        public bool UsesTwinBlades => CurrentWeapon == Weapon.CapriTwinBlades;
        public bool HasTwinBlades => inventory && twinBlades && inventory.Count(twinBlades) > 0;
        public string WeaponName => UsesTwinBlades ? "CAPRI TWIN BLADES" : "SAHUR STICK";
        public event System.Action Changed;
        EquipmentInventory inventory;
        EquipmentItem twinBlades;
        SahurAttack attack;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        Renderer[] stickRenderers;
        GameObject left, right;
        CapsuleCollider leftHitbox, rightHitbox;
        bool initialized;
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
            if (!attack || !inventory) return;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Sahur Stick") { stickRenderers = t.GetComponentsInChildren<Renderer>(true); break; }
            inventory.Collected += OnCollected;
            if (GetComponent<SahurTwinBladeMotion>() == null) gameObject.AddComponent<SahurTwinBladeMotion>();
            if (GetComponent<SahurTwinBladeReadyPose>() == null) gameObject.AddComponent<SahurTwinBladeReadyPose>();
            if (GetComponent<SahurTwinBladeLocomotion>() == null) gameObject.AddComponent<SahurTwinBladeLocomotion>();
            if (GetComponent<SahurTwinBladeHandGrip>() == null) gameObject.AddComponent<SahurTwinBladeHandGrip>();
            initialized = true;
        }
        void OnCollected(EquipmentItem item) { if (item == twinBlades) Changed?.Invoke(); }
        void Update()
        {
            if (!initialized) Initialize();
            if (!initialized || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput ||
                Cursor.lockState != CursorLockMode.Locked || Keyboard.current == null) return;
            var keys = Keyboard.current;
            if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) TryEquip(Weapon.Stick);
            else if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) TryEquip(Weapon.CapriTwinBlades);
        }

        public bool TryEquip(Weapon weapon, bool fromMenu = false)
        {
            Initialize();
            if (!initialized || !enabled || !gameObject.activeInHierarchy || (health && health.currentHealth <= 0) ||
                PauseSettingsMenu.IsOpen || (SahurLoadoutUI.BlocksInput && !(fromMenu && SahurLoadoutUI.IsOpen)) ||
                (movement && (!movement.enabled || movement.Swimming || movement.ExternalControlLock)) ||
                (GetComponent<SahurBoomerang>()?.IsBusy ?? false)) return false;
            if (weapon == Weapon.CapriTwinBlades && !HasTwinBlades) return false;
            if (weapon == CurrentWeapon) return true;
            if (weapon == Weapon.CapriTwinBlades && !CreateBlades()) return false;
            attack.SetTwinBladeMode(weapon == Weapon.CapriTwinBlades, rightHitbox, leftHitbox);
            CurrentWeapon = weapon;
            GetComponent<SahurTwinBladeLocomotion>()?.Refresh();
            if (stickRenderers != null) foreach (var r in stickRenderers) if (r) r.enabled = !UsesTwinBlades;
            if (left) left.SetActive(UsesTwinBlades);
            if (right) right.SetActive(UsesTwinBlades);
            Changed?.Invoke();
            return true;
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
            float bladeScale = 2.1f * playerScale / Mathf.Max(.01f, length * handScale);
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
            if (!UsesTwinBlades) return;
            bool visible = movement && movement.enabled && !movement.Swimming && !(GetComponent<SahurSwimmingWeapon>()?.Climbing ?? false) && (!health || health.currentHealth > 0);
            if (left) left.SetActive(visible);
            if (right) right.SetActive(visible);
            // Swimming may restore the original stick's grip; it stays hidden with blades selected.
            if (stickRenderers != null) foreach (var r in stickRenderers) if (r) r.enabled = false;
        }
        void OnDestroy()
        {
            if (inventory) inventory.Collected -= OnCollected;
            if (left) Destroy(left);
            if (right) Destroy(right);
        }
    }
}
