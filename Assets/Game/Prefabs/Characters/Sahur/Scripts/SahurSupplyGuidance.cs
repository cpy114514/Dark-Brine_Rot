using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Non-interactive pickup guidance; points at existing, available world loot.</summary>
    [DisallowMultipleComponent]
    public sealed class SahurSupplyGuidance : MonoBehaviour
    {
        public enum GuidanceKind { None, Gun, Ammo }
        public GuidanceKind CurrentGuidance { get; private set; }
        public Component Target { get; private set; }
        public bool IsVisible => group && group.alpha > 0;
        const int LowAmmoThreshold = 50;
        static readonly Color Yellow = new Color(1f, .83f, .12f);
        SahurWeaponLoadout loadout;
        SahurRifleCombat rifle;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        EquipmentItem gunItem;
        BombardinoBoss[] bosses = new BombardinoBoss[0];
        PlacedEquipmentPickup[] placedGuns = new PlacedEquipmentPickup[0];
        EquipmentPickup[] droppedGuns = new EquipmentPickup[0];
        RifleAmmoPickup[] ammoPickups = new RifleAmmoPickup[0];
        IslandSupplyCache[] supplyCaches = new IslandSupplyCache[0];
        float scanAt, selectAt, textAt;
        int shownTotal = -1;
        Component shownTarget;
        GuidanceKind shownKind;
        GameLanguage shownLanguage;
        GameObject root;
        RectTransform canvasRect, marker, direction;
        CanvasGroup group;
        Text message, destination;
        Camera viewer;

        void Awake()
        {
            loadout = GetComponent<SahurWeaponLoadout>();
            rifle = GetComponent<SahurRifleCombat>();
            movement = GetComponent<ThirdPersonPlayerController>();
            health = GetComponent<PlayerHealth>();
            gunItem = Resources.Load<EquipmentItem>(SahurWeaponLoadout.M4ItemPath);
            BuildUI();
        }

        void BuildUI()
        {
            root = new GameObject("Pickup guidance", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 155;
            GameUITheme.Scale(canvas);
            canvasRect = root.GetComponent<RectTransform>();
            group = root.GetComponent<CanvasGroup>();
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;

            var panel = GameUITheme.Rect(root.transform, "Supply reminder", new Vector2(.5f, 1), new Vector2(850, 72));
            panel.anchoredPosition = new Vector2(0, -190);
            GameUITheme.Image(panel, GameUITheme.Surface);
            GameUITheme.Rule(panel, new Vector2(0, 35), new Vector2(850, 2), Yellow);
            message = GameUITheme.Label(panel, "Reminder", 22, new Vector2(0, -36), new Vector2(810, 62), TextAnchor.MiddleCenter);
            message.color = Yellow;

            marker = GameUITheme.Rect(root.transform, "Yellow pickup square", new Vector2(.5f, .5f), new Vector2(32, 32));
            // Four UI edges form a square without allocating world geometry, lights or colliders.
            Edge(new Vector2(0, 15), new Vector2(32, 3));
            Edge(new Vector2(0, -15), new Vector2(32, 3));
            Edge(new Vector2(-15, 0), new Vector2(3, 32));
            Edge(new Vector2(15, 0), new Vector2(3, 32));
            GameUITheme.Image(GameUITheme.Rect(marker, "Centre", new Vector2(.5f, .5f), new Vector2(5, 5)), Yellow);
            destination = GameUITheme.Label(marker, "Pickup distance", 20, new Vector2(0, -48), new Vector2(300, 30), TextAnchor.MiddleCenter);
            destination.color = Yellow; GameUITheme.OutlineSymbol(destination);
            var arrow = GameUITheme.Label(marker, "Off-screen direction", 25, new Vector2(0, 33), new Vector2(36, 36), TextAnchor.MiddleCenter);
            arrow.text = "▲"; arrow.color = Yellow; GameUITheme.OutlineSymbol(arrow);
            direction = arrow.rectTransform;
            direction.anchorMin = direction.anchorMax = new Vector2(.5f, .5f);
        }

        void Edge(Vector2 position, Vector2 size)
        {
            var edge = GameUITheme.Rect(marker, "Square edge", new Vector2(.5f, .5f), size);
            edge.anchoredPosition = position;
            var image = GameUITheme.Image(edge, Yellow);
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black; outline.effectDistance = new Vector2(1, -1);
        }

        void ScanSources()
        {
            scanAt = Time.unscaledTime + 1;
            bosses = FindObjectsByType<BombardinoBoss>(FindObjectsSortMode.None);
            placedGuns = FindObjectsByType<PlacedEquipmentPickup>(FindObjectsSortMode.None);
            droppedGuns = FindObjectsByType<EquipmentPickup>(FindObjectsSortMode.None);
            ammoPickups = FindObjectsByType<RifleAmmoPickup>(FindObjectsSortMode.None);
            supplyCaches = FindObjectsByType<IslandSupplyCache>(FindObjectsSortMode.None);
            viewer = Camera.main;
        }

        bool Available(Component candidate, GuidanceKind kind)
        {
            if (!candidate || !candidate.gameObject.activeInHierarchy) return false;
            if (kind == GuidanceKind.Ammo)
                return candidate is RifleAmmoPickup ammo && ammo.isActiveAndEnabled && !ammo.IsCollected && ammo.amount > 0 ||
                    candidate is IslandSupplyCache cache && cache.IsAvailable;
            if (kind != GuidanceKind.Gun) return false;
            if (candidate is PlacedEquipmentPickup placed)
                return placed.isActiveAndEnabled && !placed.IsCollected && placed.item && placed.item == gunItem;
            return candidate is EquipmentPickup dropped && dropped.isActiveAndEnabled && !dropped.IsCollected && dropped.Item && dropped.Item == gunItem;
        }

        Vector3 Position(Component candidate)
        {
            if (candidate is PlacedEquipmentPickup placed) return placed.GuidancePosition;
            return candidate.transform.position + Vector3.up * 1.2f;
        }

        void SelectTarget(GuidanceKind kind)
        {
            selectAt = Time.unscaledTime + .25f;
            Component best = null; float distance = float.PositiveInfinity;
            void Consider(Component candidate)
            {
                if (!Available(candidate, kind)) return;
                var delta = Position(candidate) - transform.position;
                float squared = delta.x * delta.x + delta.z * delta.z;
                if (squared < distance) { best = candidate; distance = squared; }
            }
            if (kind == GuidanceKind.Ammo)
            { foreach (var pickup in ammoPickups) Consider(pickup); foreach (var cache in supplyCaches) Consider(cache); }
            else { foreach (var pickup in placedGuns) Consider(pickup); foreach (var pickup in droppedGuns) Consider(pickup); }
            Target = best;
        }

        void LateUpdate() => Refresh();
        void Refresh()
        {
            if (!loadout || !rifle || !movement || !health || health.currentHealth <= 0 ||
                !movement.enabled || movement.ExternalControlLock || movement.Swimming ||
                Time.timeScale <= 0 || GameUITheme.ModalOpen || SahurLoadoutUI.BlocksInput)
            { Hide(); return; }
            if (Time.unscaledTime >= scanAt) ScanSources();
            GuidanceKind kind = GuidanceKind.None;
            int total = rifle.Rounds + rifle.ReserveRounds;
            if (loadout.HasM4A1)
            {
                // Include loaded rounds: reloading cannot create a false low-ammo reminder.
                if (total < LowAmmoThreshold) kind = GuidanceKind.Ammo;
            }
            else
                foreach (var boss in bosses)
                    if (boss && boss.isActiveAndEnabled && boss.FightActive && !boss.IsDead)
                    { kind = GuidanceKind.Gun; break; }
            if (kind == GuidanceKind.None) { Hide(); return; }
            if (kind != CurrentGuidance || Time.unscaledTime >= selectAt || !Available(Target, kind)) SelectTarget(kind);
            CurrentGuidance = kind;
            if (!viewer) viewer = Camera.main;
            if (!viewer) { Hide(); return; }
            group.alpha = 1;
            // Project the square every frame; format captions at 5 Hz or when state changes.
            if (Time.unscaledTime >= textAt || total != shownTotal || Target != shownTarget ||
                kind != shownKind || GameLocalization.Language != shownLanguage)
            {
                textAt = Time.unscaledTime + .2f; shownTotal = total; shownTarget = Target;
                shownKind = kind; shownLanguage = GameLocalization.Language;
                string reminder = kind == GuidanceKind.Gun ? GameLocalization.Text("supply.pickupGun") :
                    GameLocalization.Format(Target ? "supply.lowAmmo" : "supply.noAmmo", total);
                SetText(message, reminder);
                if (Target)
                {
                    int metres = Mathf.CeilToInt(Vector3.Distance(Position(Target), transform.position));
                    SetText(destination, kind == GuidanceKind.Gun ? GameLocalization.Format("supply.gunDistance", metres) :
                        Target is RifleAmmoPickup ammo ? GameLocalization.Format("supply.ammoDistance", ammo.amount, metres) :
                        ((IslandSupplyCache)Target).DisplayName + " · " + metres + " m");
                }
            }
            marker.gameObject.SetActive(Target);
            if (!Target) return;
            PlaceMarker(Position(Target));
        }

        void PlaceMarker(Vector3 point)
        {
            Vector3 projected = viewer.WorldToScreenPoint(point);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, projected, null, out Vector2 local);
            Vector2 limits = canvasRect.rect.size * .5f - new Vector2(175, 140);
            limits.x = Mathf.Max(30, limits.x); limits.y = Mathf.Max(30, limits.y);
            // Leave room above for the boss health bar and the supply reminder.
            float upper = Mathf.Max(30, canvasRect.rect.height * .5f - 285);
            bool onScreen = projected.z > 0 && Mathf.Abs(local.x) <= limits.x && local.y >= -limits.y && local.y <= upper;
            direction.gameObject.SetActive(!onScreen);
            if (!onScreen)
            {
                if (projected.z < 0) local = -local;
                if (local.sqrMagnitude < .001f) local = Vector2.down;
                float verticalLimit = local.y > 0 ? upper : limits.y;
                local *= Mathf.Min(limits.x / Mathf.Max(.001f, Mathf.Abs(local.x)), verticalLimit / Mathf.Max(.001f, Mathf.Abs(local.y)));
                direction.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg - 90);
            }
            marker.anchoredPosition = local;
        }

        static void SetText(Text label, string text) { if (label.text != text) label.text = text; }
        void Hide() { if (group) group.alpha = 0; CurrentGuidance = GuidanceKind.None; Target = null; }
        void OnDisable() => Hide();
        void OnDestroy() { if (root) Destroy(root); }
    }
}
