using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class CappuccinoWeaponLoot : MonoBehaviour
    {
        public bool HasDropped { get; private set; }
        readonly RaycastHit[] groundHits = new RaycastHit[32];
        public EquipmentPickup Drop()
        {
            var health = GetComponent<Health>();
            if (HasDropped || !health || !health.IsDead) return null;
            var item = Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades");
            if (!item || !item.worldModel) return null;
            HasDropped = true;
            var body = GetComponent<CharacterController>();
            float radius = body ? body.radius * Mathf.Abs(transform.lossyScale.x) : 2f;
            var point = transform.position + transform.forward * (radius + 1.8f);
            int count = Physics.RaycastNonAlloc(point + Vector3.up * 8f, Vector3.down, groundHits, 24f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<IDamageable>() != null || hit.distance >= nearest) continue;
                nearest = hit.distance; point.y = hit.point.y;
            }
            point.y += .5f;
            var obj = new GameObject("Loot - Capri Twin Blades");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
            obj.transform.position = point;
            var pickup = obj.AddComponent<EquipmentPickup>();pickup.Initialize(item);
            return pickup;
        }
    }
}
