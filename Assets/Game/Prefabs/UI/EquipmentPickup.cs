using UnityEngine;

namespace Mavis
{
    /// <summary>Non-physical hovering loot, collected once by a living player within reach.</summary>
    public sealed class EquipmentPickup : MonoBehaviour
    {
        public EquipmentItem Item { get; private set; }
        public bool IsCollected { get; private set; }
        GameObject visual;
        Vector3 anchor;
        EquipmentInventory player;
        Material beaconMaterial;
        float age;

        public void Initialize(EquipmentItem item)
        {
            Item = item;
            anchor = transform.position;
            visual = Instantiate(item.worldModel, transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var behaviour in visual.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
            // A simple gold marker remains legible on grass and sand without extra lights.
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Loot marker";
            beacon.transform.SetParent(transform, false);
            beacon.transform.localPosition = new Vector3(0, -.42f, 0);
            beacon.transform.localScale = new Vector3(.48f, .015f, .48f);
            var colliderToRemove = beacon.GetComponent<Collider>();
            colliderToRemove.enabled = false; Destroy(colliderToRemove);
            beaconMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            beaconMaterial.color = new Color(1f, .73f, .2f);
            beacon.GetComponent<Renderer>().sharedMaterial = beaconMaterial;
        }

        void Update()
        {
            if (IsCollected || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput) return;
            age += Time.deltaTime;
            if (visual != null)
            {
                visual.transform.localPosition = Vector3.up * (Mathf.Sin(age * 2.4f) * .07f);
                visual.transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);
            }
            if (player == null) player = FindFirstObjectByType<EquipmentInventory>();
            // Allow the player time to see the three drops before nearby ones are collected.
            if (age >= .75f && player != null) TryCollect(player);
        }

        public bool TryCollect(EquipmentInventory inventory)
        {
            if (IsCollected || Item == null || inventory == null || !inventory.gameObject.activeInHierarchy ||
                PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput) return false;
            var health = inventory.GetComponent<PlayerHealth>();
            if (health != null && health.currentHealth <= 0f) return false;
            Vector3 feet = inventory.transform.position;
            var controller = inventory.GetComponent<CharacterController>();
            if (controller != null) feet.y = controller.bounds.min.y;
            Vector3 offset = feet - anchor;
            if (Mathf.Abs(offset.y) > 2f || new Vector2(offset.x, offset.z).sqrMagnitude > 1.1f * 1.1f) return false;
            // Do not collect through solid walls. Ignore the owner and trigger volumes.
            Vector3 from = feet + Vector3.up * .7f;
            Vector3 step = anchor - from;
            foreach (var hit in Physics.RaycastAll(from, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(inventory.transform) && !hit.collider.transform.IsChildOf(transform)) return false;
            IsCollected = true;
            inventory.Collect(Item);
            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }

        void OnDestroy() { if (beaconMaterial != null) Destroy(beaconMaterial); }
    }
}
