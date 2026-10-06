using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    [DisallowMultipleComponent, RequireComponent(typeof(CappuccinoAI))]
    public sealed class CappuccinoUltimate : MonoBehaviour
    {
        public enum Phase { Ready, Empowered, Immobilized, Spent, Charging }
        [Header("One ultimate per encounter")]
        public bool ultimateEnabled = true;
        [Range(0f, 1f)] public float triggerHealthRatio = 0.5f;
        [Min(0.1f)] public float empoweredSeconds = 5f;
        [Min(0.1f)] public float immobilizedSeconds = 3f;
        [Min(1f)] public float healthMultiplier = 2f;
        [Min(1f)] public float damageMultiplier = 2f;
        public bool showWarnings = true;
        [Min(.3f)] public float chargeSeconds = .95f;
        public Phase CurrentPhase { get; private set; }
        public bool MovementLocked => CurrentPhase == Phase.Immobilized || CurrentPhase == Phase.Charging;
        public bool AttackLocked => CurrentPhase == Phase.Charging || CurrentPhase == Phase.Immobilized;
        public bool IsEmpowered => CurrentPhase == Phase.Empowered;
        public float RemainingSeconds => Mathf.Max(0f, phaseEndsAt - lastTime);

        Health health;
        CappuccinoAI ai;
        float originalMaxHealth, originalDamage, phaseEndsAt, lastTime;
        bool attributesBoosted;
        GameObject hud;
        Image accent;
        Text title, description, timer;
        int lastTenths = -1;
        Phase displayedPhase = Phase.Ready;

        void Awake()
        {
            health = GetComponent<Health>();
            ai = GetComponent<CappuccinoAI>();
            health.OnDeath ??= new UnityEngine.Events.UnityEvent();
            health.OnDeath.AddListener(OnDeath);
        }

        internal bool TryActivate(float now)
        {
            if (!ultimateEnabled || !isActiveAndEnabled || CurrentPhase != Phase.Ready || health == null ||
                health.IsDead || health.Ratio > triggerHealthRatio) return false;
            originalMaxHealth = health.maxHealth;
            originalDamage = ai.damage;
            CurrentPhase = Phase.Charging;
            phaseEndsAt = now + chargeSeconds;
            lastTime = now;
            Present();
            return true;
        }

        void BeginEmpowerment()
        {
            health.maxHealth = originalMaxHealth * healthMultiplier;
            health.currentHealth = Mathf.Min(health.maxHealth, health.currentHealth * healthMultiplier);
            ai.damage = originalDamage * damageMultiplier;
            attributesBoosted = true;
            CurrentPhase = Phase.Empowered;
            phaseEndsAt += empoweredSeconds;
            health.OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            Present();
        }

        internal void Simulate(float now)
        {
            lastTime = now;
            if (health == null || health.IsDead) return;
            if (CurrentPhase == Phase.Charging && now >= phaseEndsAt) BeginEmpowerment();
            if (CurrentPhase == Phase.Empowered && now >= phaseEndsAt)
            {
                RestoreAttributes(false);
                health.currentHealth = health.maxHealth;
                health.OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
                CurrentPhase = Phase.Immobilized;
                phaseEndsAt += immobilizedSeconds;
            }
            if (CurrentPhase == Phase.Immobilized && now >= phaseEndsAt)
                CurrentPhase = Phase.Spent;
            Present();
        }

        void RestoreAttributes(bool preserveRatio)
        {
            if (!attributesBoosted) return;
            float ratio = health.Ratio;
            health.maxHealth = originalMaxHealth;
            health.currentHealth = preserveRatio ? health.maxHealth * ratio : Mathf.Min(health.currentHealth, health.maxHealth);
            ai.damage = originalDamage;
            attributesBoosted = false;
        }

        void Present()
        {
            bool visible = showWarnings && (IsEmpowered || MovementLocked);
            if (!visible) { if (hud != null) hud.SetActive(false); return; }
            if (hud == null)
            {
                if (!Application.isPlaying) return;
                BuildHUD();
            }
            hud.SetActive(true);
            if (displayedPhase != CurrentPhase)
            {
                displayedPhase = CurrentPhase;
                lastTenths = -1;
                if (CurrentPhase == Phase.Charging)
                {
                    title.text = "CAFFEINE SURGE";
                    description.text = "Charging ultimate. Move away from his blades.";
                    accent.color = title.color = GameUITheme.Foreground;
                }
                else if (IsEmpowered)
                {
                    title.text = "CAFFEINE SURGE";
                    description.text = "Health x" + Format(healthMultiplier) + "  |  Damage x" + Format(damageMultiplier) +
                        "  /  Keep your distance for " + Format(empoweredSeconds) + " seconds.";
                    accent.color = title.color = GameUITheme.Foreground;
                }
                else
                {
                    title.text = "FULL HEALTH RESTORED";
                    description.text = "Cannot move or attack for " + Format(immobilizedSeconds) +
                        " seconds. Counterattack now!";
                    accent.color = title.color = GameUITheme.Foreground;
                }
            }
            int tenths = Mathf.CeilToInt(RemainingSeconds * 10f);
            if (tenths == lastTenths) return;
            lastTenths = tenths;
            timer.text = (CurrentPhase == Phase.Charging ? "CHARGING  " : IsEmpowered ? "EMPOWERED  " : "IMMOBILIZED  ") +
                (tenths / 10f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s";
        }

        static string Format(float value) => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        void BuildHUD()
        {
            hud = new GameObject("Capri Ultimate Warning", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hud.transform.SetParent(transform, false);
            var canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("Warning Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(hud.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0,-250);
            rect.sizeDelta = new Vector2(760, 126);
            panel.GetComponent<Image>().color = GameUITheme.Surface;
            panel.GetComponent<Image>().raycastTarget = false;
            var line = new GameObject("Phase Accent", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(panel.transform, false);
            var lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f); lineRect.anchorMax = new Vector2(0f, 1f);
            lineRect.pivot = new Vector2(0f, 0.5f); lineRect.sizeDelta = new Vector2(2, 0);
            accent = line.GetComponent<Image>(); accent.raycastTarget = false;
            title = Label(panel.transform, "Title", 24, 36, 32);
            description = Label(panel.transform, "Advice", 18, 0, 40);
            timer = Label(panel.transform, "Countdown", 15, -42, 24);
            timer.color = GameUITheme.Secondary;
        }

        static Text Label(Transform parent, string name, int size, float y, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.sizeDelta = new Vector2(708, height); rect.anchoredPosition = new Vector2(0, y);
            var text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            // These combat warnings intentionally remain English in every language setting.
            return text;
        }

        void OnDeath()
        {
            RestoreAttributes(false);
            CurrentPhase = Phase.Spent;
            if (hud != null) hud.SetActive(false);
        }

        void LateUpdate()
        {
            if(hud)hud.SetActive(showWarnings && (IsEmpowered || MovementLocked) && !GameUITheme.ModalOpen);
        }

        internal void ResetEncounter()
        {
            RestoreAttributes(false);
            CurrentPhase = Phase.Ready;
            if (hud != null) hud.SetActive(false);
        }

        void OnDisable()
        {
            if (attributesBoosted)
            {
                RestoreAttributes(true);
                health.OnHealthChanged?.Invoke(health.currentHealth, health.maxHealth);
            }
            if (CurrentPhase != Phase.Ready) CurrentPhase = Phase.Spent;
            if (hud != null) hud.SetActive(false);
        }

        void OnDestroy()
        {
            if (health != null) health.OnDeath?.RemoveListener(OnDeath);
            // The warning is a child of Capri, so destroying the enemy also removes its canvas.
        }
    }
}
