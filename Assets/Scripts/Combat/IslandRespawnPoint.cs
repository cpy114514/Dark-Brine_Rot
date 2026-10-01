using UnityEngine;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class IslandRespawnPoint : MonoBehaviour
    {
        public string pointName = "小岛复活点";
        public bool isDefault = true;
        public float activationRadius = 3f;
        Material markerMaterial;
        void Start()
        {
            // Runtime-only presentation keeps generated meshes/materials out of scene serialization.
            var ringObject = new GameObject("Respawn beacon ring");
            ringObject.transform.SetParent(transform, false);
            var ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 64;
            ring.widthMultiplier = 0.055f;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            markerMaterial = new Material(shader);
            markerMaterial.color = new Color(0.2f, 0.95f, 0.8f);
            ring.sharedMaterial = markerMaterial;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2f / 64;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.3f, 0.12f, Mathf.Sin(a) * 1.3f));
            }
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Respawn beacon";
            beacon.transform.SetParent(transform, false);
            beacon.transform.localPosition = Vector3.up * 1.5f;
            beacon.transform.localScale = Vector3.one * 0.25f;
            Destroy(beacon.GetComponent<Collider>());
            beacon.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        }
        void Update()
        {
            var player = FindFirstObjectByType<PlayerDeathRespawn>();
            if (player == null || player.IsDead) return;
            if ((player.transform.position - transform.position).sqrMagnitude < activationRadius * activationRadius)
                player.SetCheckpoint(this);
        }
        public Vector3 GroundPosition => transform.position;
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.95f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        }
        void OnDestroy() { if (markerMaterial != null) Destroy(markerMaterial); }
    }
}
