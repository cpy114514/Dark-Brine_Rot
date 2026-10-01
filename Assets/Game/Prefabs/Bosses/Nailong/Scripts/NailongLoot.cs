using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class NailongLoot : MonoBehaviour
    {
        public EquipmentItem[] drops;
        public bool HasDropped { get; private set; }

        void Awake()
        {
            if (drops == null || drops.Length == 0) drops = Resources.LoadAll<EquipmentItem>("Equipment/Nailong");
        }

        public int Drop()
        {
            var health = GetComponent<NailongHealth>();
            if (HasDropped || health == null || !health.IsDead) return 0;
            HasDropped = true;
            int count = 0;
            for (int i = 0; i < drops.Length; i++)
            {
                var item = drops[i];
                if (item == null || item.worldModel == null) continue;
                float angle = i * Mathf.PI * 2f / drops.Length;
                Vector3 position = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.3f;
                float ground = position.y;
                float nearest = float.MaxValue;
                foreach (var hit in Physics.RaycastAll(position + Vector3.up * 6f, Vector3.down, 18f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<PlayerHealth>() != null ||
                        hit.collider.GetComponentInParent<NailongHealth>() != null || hit.collider.GetComponentInParent<EquipmentPickup>() != null) continue;
                    if (hit.distance < nearest) { nearest = hit.distance; ground = hit.point.y; }
                }
                position.y = ground + .5f;
                var obj = new GameObject("Loot - " + item.displayName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
                obj.transform.position = position;
                obj.AddComponent<EquipmentPickup>().Initialize(item);
                count++;
            }
            return count;
        }
    }
}
