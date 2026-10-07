using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mavis
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-90)]
    public sealed class EnemyLockOn : MonoBehaviour
    {
        public float acquireRange = 30f;
        public float releaseRange = 40f;
        public float obstructionGrace = 0.8f;
        public Transform Target { get; private set; }
        public bool IsLocked => Target != null;
        public Vector3 AimPoint => targetHealth is Story1SharkHealth shark ? shark.LockPoint : targetRenderer != null ? targetRenderer.bounds.center
            : targetCollider != null ? targetCollider.bounds.center : Target != null ? Target.position + Vector3.up : transform.position;
        Component targetHealth;
        Renderer targetRenderer;
        Collider targetCollider;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        float blockedTime;
        GameObject hud;
        RectTransform marker;
        Font font;
        RaycastHit[] sightHits=new RaycastHit[32];

        void Awake() { movement = GetComponent<ThirdPersonPlayerController>(); health = GetComponent<PlayerHealth>(); }
        void Update()
        {
            if ((health != null && health.currentHealth <= 0f) || (movement != null && movement.Swimming)) { Clear(); return; }
            if (SahurLoadoutUI.BlocksInput || PauseSettingsMenu.IsOpen) { if (hud != null) hud.SetActive(false); return; }
            Validate(Time.deltaTime);
            if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
                Toggle(Camera.main);
            RefreshMarker();
        }
        static bool Alive(Component hp)
        {
            if (hp == null || !hp.gameObject.activeInHierarchy) return false;
            if (hp is NailongHealth boss) return boss.enabled && !boss.IsDead;
            if (hp is Health enemy) return enemy.enabled && !enemy.IsDead;
            return false;
        }
        Vector3 Point(Component hp)
        {
            // Keep the partly submerged story shark's marker on its exposed dorsal area.
            if(hp is Story1SharkHealth shark)return shark.LockPoint;
            var skin = hp.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin != null) return skin.bounds.center;
            var collider = hp.GetComponentInChildren<Collider>();
            return collider != null ? collider.bounds.center : hp.transform.position + Vector3.up;
        }
        bool Visible(Component hp, Vector3 point, Camera camera)
        {
            Vector3 origin = camera != null ? camera.transform.position : transform.position + Vector3.up * 1.5f;
            Vector3 ray = point - origin;
            int count;
            while(true)
            {
                count=Physics.RaycastNonAlloc(origin,ray.normalized,sightHits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
                if(count<sightHits.Length)break;
                // Retry crowded sight lines so a full buffer never hides an obstruction.
                System.Array.Resize(ref sightHits,sightHits.Length*2);
            }
            for(int i=0;i<count;i++)
            {
                var hit=sightHits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(hp.transform)) continue;
                return false;
            }
            return true;
        }
        public bool Toggle(Camera camera)
        {
            if (IsLocked) { Clear(); return true; }
            if (camera == null || SahurLoadoutUI.BlocksInput || PauseSettingsMenu.IsOpen || (health != null && health.currentHealth <= 0f)) return false;
            Component best = null;
            float bestScore = float.PositiveInfinity;
            void Consider(Component candidate)
            {
                if (!Alive(candidate) || candidate.transform.IsChildOf(transform)) return;
                if (candidate is Health && !candidate.CompareTag("Enemy") && !candidate.transform.root.CompareTag("Enemy")) return;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance > acquireRange) return;
                Vector3 point = Point(candidate);
                Vector3 screen = camera.WorldToViewportPoint(point);
                if (screen.z <= 0f || screen.x < 0f || screen.x > 1f || screen.y < 0f || screen.y > 1f || !Visible(candidate, point, camera)) return;
                float score = new Vector2(screen.x - .5f, screen.y - .5f).sqrMagnitude * 4f + distance / acquireRange * .15f;
                if (score < bestScore) { best = candidate; bestScore = score; }
            }
            foreach (var boss in FindObjectsByType<NailongHealth>(FindObjectsSortMode.None)) Consider(boss);
            foreach (var enemy in FindObjectsByType<Health>(FindObjectsSortMode.None)) Consider(enemy);
            if (best == null) return false;
            targetHealth = best; Target = best.transform;
            targetRenderer = best.GetComponentInChildren<SkinnedMeshRenderer>();
            targetCollider = best.GetComponentInChildren<Collider>();
            blockedTime = 0f;
            if (hud == null) BuildMarker();
            RefreshMarker();
            return true;
        }
        public void Validate(float deltaTime)
        {
            if (!IsLocked) { Clear(); return; }
            if (!Alive(targetHealth) || Vector3.Distance(transform.position, Target.position) > releaseRange) { Clear(); return; }
            blockedTime = Visible(targetHealth, AimPoint, Camera.main) ? 0f : blockedTime + Mathf.Max(0f, deltaTime);
            if (blockedTime >= obstructionGrace) Clear();
        }
        public Quaternion FacingRotation()
        {
            Vector3 direction = Target != null ? Target.position - transform.position : transform.forward;
            direction.y = 0f;
            return direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : transform.rotation;
        }
        public void Clear()
        {
            Target = null; targetHealth = null; targetRenderer = null; targetCollider = null; blockedTime = 0f;
            if (hud != null) hud.SetActive(false);
        }
        void BuildMarker()
        {
            hud = new GameObject("Enemy lock-on marker", typeof(RectTransform), typeof(Canvas));
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            hud.GetComponent<Canvas>().sortingOrder = 100;
            var obj = new GameObject("Target brackets", typeof(RectTransform)); obj.transform.SetParent(hud.transform, false);
            marker = (RectTransform)obj.transform; marker.sizeDelta = Vector2.one * 36f;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int axis = 0; axis < 2; axis++)
                    {
                        var line = new GameObject("Bracket", typeof(RectTransform), typeof(Image)); line.transform.SetParent(marker, false);
                        var rect = (RectTransform)line.transform;
                        rect.sizeDelta = axis == 0 ? new Vector2(12, 3) : new Vector2(3, 12);
                        rect.anchoredPosition = new Vector2(x * (axis == 0 ? 12 : 18), y * (axis == 0 ? 18 : 12));
                        var image = line.GetComponent<Image>(); image.color = GameUITheme.Foreground; image.raycastTarget = false;
                    }
            var label = new GameObject("Lock hint", typeof(RectTransform), typeof(Text)); label.transform.SetParent(marker, false);
            var text = label.GetComponent<Text>();
            font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 16);
            text.font = font; text.fontSize = 16; text.alignment = TextAnchor.MiddleCenter;
            text.text = "锁定 · 中键解除"; text.color = GameUITheme.Foreground; text.raycastTarget = false;
            GameUITheme.OutlineSymbol(text);
            LocalizedGameText.Bind(text);
            text.rectTransform.sizeDelta = new Vector2(200, 30); text.rectTransform.anchoredPosition = new Vector2(0, -40);
        }
        public void RefreshMarker()
        {
            if (hud == null) return;
            var camera = Camera.main;
            bool show = IsLocked && camera != null && !SahurLoadoutUI.BlocksInput && !PauseSettingsMenu.IsOpen;
            if (show)
            {
                Vector3 screen = camera.WorldToScreenPoint(AimPoint);
                show = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hud.transform, screen, null, out var local);
                marker.anchoredPosition = local;
            }
            hud.SetActive(show);
        }
        void OnDisable() => Clear();
        void OnDestroy() { if (hud != null) Destroy(hud); if (font != null) Destroy(font); }
    }
}
