using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    [DisallowMultipleComponent]
    public sealed class RifleAmmoPickup : MonoBehaviour
    {
        [Min(1), Tooltip("Rounds supplied by this pickup. Excess stays in the box when the reserve is full.")]
        public int amount = 30;
        [Min(.1f)] public float pickupRadius = 2.5f;
        public Text caption;
        public bool IsCollected { get; private set; }
        SahurRifleCombat player;
        Camera viewer;
        float searchAt, collectAt;
        readonly RaycastHit[] hits = new RaycastHit[32];

        void Start() { GameLocalization.Changed += RefreshCaption; RefreshCaption(); }
        void RefreshCaption()
        {
            if (caption)
            {
                caption.font = GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                caption.text = GameLocalization.Format("rifle.ammoPickup", amount);
            }
        }
        void Update()
        {
            if (IsCollected) return;
            if (Time.unscaledTime >= searchAt)
            {
                searchAt = Time.unscaledTime + .5f;
                if (!player) player = FindFirstObjectByType<SahurRifleCombat>();
                if (!viewer) viewer = Camera.main;
            }
            if (caption && viewer)
            {
                bool visible = (viewer.transform.position - transform.position).sqrMagnitude < 65 * 65;
                if (caption.transform.parent.gameObject.activeSelf != visible) caption.transform.parent.gameObject.SetActive(visible);
                if (visible) caption.transform.parent.rotation = viewer.transform.rotation;
            }
            if (Time.time >= collectAt)
            {
                collectAt = Time.time + .15f;
                if (player) TryCollect(player);
            }
        }
        public bool TryCollect(SahurRifleCombat rifle)
        {
            if (IsCollected || amount <= 0 || !isActiveAndEnabled || !rifle || !rifle.isActiveAndEnabled ||
                Time.timeScale <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput) return false;
            var health = rifle.GetComponent<PlayerHealth>();
            var movement = rifle.GetComponent<ThirdPersonPlayerController>();
            if (!health || health.currentHealth <= 0 || !movement || !movement.enabled || movement.ExternalControlLock) return false;
            Vector3 feet = rifle.transform.position;
            var body = rifle.GetComponent<CharacterController>();
            if (body) feet.y = body.bounds.min.y;
            Vector3 gap = feet - transform.position;
            if (Mathf.Abs(gap.y) > 3 || new Vector2(gap.x, gap.z).sqrMagnitude > pickupRadius * pickupRadius) return false;
            Vector3 from = feet + Vector3.up * .8f, step = transform.position + Vector3.up * .3f - from;
            if (step.sqrMagnitude > .001f)
            {
                int count = Physics.RaycastNonAlloc(from, step.normalized, hits, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
                if (count == hits.Length) return false;
                for (int i = 0; i < count; i++)
                    if (!hits[i].transform.IsChildOf(rifle.transform) && !hits[i].transform.IsChildOf(transform)) return false;
            }
            int added = rifle.AddReserveAmmo(amount);
            if (added == 0) return false;
            amount -= added;
            if (amount > 0) RefreshCaption();
            else { IsCollected = true; gameObject.SetActive(false); Destroy(gameObject); }
            return true;
        }
        void OnValidate() { amount = Mathf.Max(1, amount); pickupRadius = Mathf.Max(.1f, pickupRadius); }
        void OnDestroy() { GameLocalization.Changed -= RefreshCaption; }
        void OnDrawGizmosSelected() { Gizmos.color = new Color(1, .75f, .2f, .6f); Gizmos.DrawWireSphere(transform.position, pickupRadius); }
    }
}
