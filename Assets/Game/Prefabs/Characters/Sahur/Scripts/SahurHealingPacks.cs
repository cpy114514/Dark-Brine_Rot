using System;
using UnityEngine;

namespace Mavis
{
    /// <summary>One shared, persistent supply for a campaign, across every Sahur instance.</summary>
    public static class HealingPackSupply
    {
        public const int Total = 2;
        const string ActiveKey = "DarkBrine.HealingPacks.Campaign";
        const string CountPrefix = "DarkBrine.HealingPacks.Remaining.";
        static bool loaded;
        static string campaign;
        static int remaining;
        public static string Campaign { get { Load(); return campaign; } }
        public static int Remaining { get { Load(); return remaining; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { loaded = false; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeginGameSession()
        {
            // Direct scene starts (including a fresh Editor Play session) are new
            // games too. Continue can subsequently restore its saved campaign.
            StartNewGame();
        }
        static void Load()
        {
            if (loaded) return;
            campaign = PlayerPrefs.GetString(ActiveKey, "legacy");
            remaining = Mathf.Clamp(PlayerPrefs.GetInt(CountPrefix + campaign, Total), 0, Total);
            loaded = true;
        }
        static void Persist()
        {
            PlayerPrefs.SetString(ActiveKey, campaign);
            PlayerPrefs.SetInt(CountPrefix + campaign, remaining);
            PlayerPrefs.Save();
        }
        public static void StartNewGame()
        {
            campaign = Guid.NewGuid().ToString("N"); remaining = Total; loaded = true; Persist();
        }
        public static void RestoreCampaign(string id, int savedRemaining)
        {
            campaign = string.IsNullOrEmpty(id) ? "legacy" : id;
            // Continuing an older save must not refund already consumed packs.
            remaining = Mathf.Min(Mathf.Clamp(savedRemaining, 0, Total),
                Mathf.Clamp(PlayerPrefs.GetInt(CountPrefix + campaign, Total), 0, Total));
            loaded = true; Persist();
        }
        internal static bool Consume()
        {
            Load(); if (remaining == 0) return false;
            remaining--; Persist(); return true;
        }
    }

    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class SahurHealingPacks : MonoBehaviour
    {
        public const float HealFraction = .5f;
        PlayerHealth health;
        ThirdPersonPlayerController movement;
        string feedbackKey;
        string feedbackText;
        GameLanguage feedbackLanguage;
        float feedbackUntil, restored;
        public int Remaining => HealingPackSupply.Remaining;
        public string Feedback
        {
            get
            {
                if (Time.unscaledTime >= feedbackUntil) return "";
                if (feedbackText == null || feedbackLanguage != GameLocalization.Language)
                {
                    feedbackLanguage = GameLocalization.Language;
                    feedbackText = GameLocalization.Format(feedbackKey, Mathf.RoundToInt(restored));
                }
                return feedbackText;
            }
        }
        public bool CanAct => isActiveAndEnabled && health != null && health.isActiveAndEnabled && health.currentHealth > 0 &&
            movement != null && movement.isActiveAndEnabled && !movement.ExternalControlLock && Time.timeScale > 0 &&
            !PauseSettingsMenu.IsOpen && !SahurLoadoutUI.BlocksInput;

        void Awake() { health = GetComponent<PlayerHealth>(); movement = GetComponent<ThirdPersonPlayerController>(); }
        void Update()
        {
            if (Cursor.lockState == CursorLockMode.Locked && GameInputSettings.PressedThisFrame(GameInputSettings.Action.Heal)) TryUse();
        }
        public bool TryUse()
        {
            if (!CanAct) return false;
            if (health.currentHealth >= health.maxHealth) { Notify("healing.full"); return false; }
            if (!HealingPackSupply.Consume()) { Notify("healing.empty"); return false; }
            restored = Mathf.Min(health.maxHealth * HealFraction, health.maxHealth - health.currentHealth);
            health.currentHealth += restored;
            Notify("healing.used");
            return true;
        }
        void Notify(string key) { feedbackKey = key; feedbackText = null; feedbackUntil = Time.unscaledTime + 2.5f; }
    }
}
