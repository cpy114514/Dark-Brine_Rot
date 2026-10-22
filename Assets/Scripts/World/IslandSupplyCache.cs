using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    /// <summary>Repeatable ammunition supply; the shore camp also restores HP and SP.</summary>
    [DisallowMultipleComponent]
    public sealed class IslandSupplyCache : MonoBehaviour
    {
        public string englishName = "Ammo supply point", chineseName = "弹药补给点";
        public int rounds = 45;
        [Tooltip("The first shore camp also helps players without a rifle.")]
        public bool supportsMelee;
        public const float RecoveryFraction = .35f;
        public float cooldownSeconds = 45, interactionRadius = 3;
        float availableAt, searchAt;
        float feedbackUntil;
        string feedback;
        public string Feedback => Time.unscaledTime < feedbackUntil ? feedback : "";
        ThirdPersonPlayerController player;
        GUIStyle style;
        public string DisplayName => GameLocalization.Text(supportsMelee ? "Recovery & ammo supply point" : "Ammo supply point");
        public float Remaining => Mathf.Max(0, availableAt - Time.time);
        public bool IsAvailable => isActiveAndEnabled && Remaining <= 0;
        public void RestoreRemaining(float seconds) => availableAt = Time.time + Mathf.Clamp(seconds, 0, cooldownSeconds);
        public bool TryRefill(ThirdPersonPlayerController actor)
        {
            if (!IsAvailable || !actor || !actor.enabled || actor.ExternalControlLock || actor.Swimming ||
                Time.timeScale <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || PlayerDeathRespawn.IsOpen) return false;
            var health = actor.GetComponent<PlayerHealth>();
            var loadout = actor.GetComponent<SahurWeaponLoadout>();
            var rifle = actor.GetComponent<SahurRifleCombat>();
            if (!health || health.currentHealth <= 0 || (!supportsMelee && (!loadout || !loadout.HasM4A1 || !rifle))) return false;
            var body = actor.GetComponent<CharacterController>();
            Vector3 feet = actor.transform.position; if (body) feet.y = body.bounds.min.y;
            if (Mathf.Abs(feet.y - transform.position.y) > 3 ||
                Vector3.ProjectOnPlane(feet - transform.position, Vector3.up).sqrMagnitude > interactionRadius * interactionRadius) return false;
            Vector3 from = feet + Vector3.up, delta = transform.position + Vector3.up * .6f - from;
            foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(actor.transform)) return false;
            var stamina = actor.GetComponent<PlayerStamina>();
            float hpBefore = health.currentHealth, spBefore = stamina ? stamina.currentStamina : 0;
            int ammoBefore = rifle ? rifle.ReserveRounds : 0;
            bool supplied = loadout && loadout.HasM4A1 && rifle && rifle.AddReserveAmmo(rounds) > 0;
            if (supportsMelee)
            {
                float before = health.currentHealth;
                health.currentHealth = Mathf.Min(health.maxHealth, before + health.maxHealth * RecoveryFraction);
                supplied |= health.currentHealth > before;
                if (stamina)
                {
                    float previous = stamina.currentStamina;
                    stamina.currentStamina = Mathf.Min(stamina.maxStamina, previous + 25);
                    supplied |= stamina.currentStamina > previous;
                }
            }
            if (!supplied) { ShowFeedback(GameLocalization.Text("supply.full")); return false; }
            if (!supportsMelee && stamina) stamina.currentStamina = Mathf.Min(stamina.maxStamina, stamina.currentStamina + 25);
            ShowFeedback(GameLocalization.Format("supply.received", Mathf.RoundToInt(health.currentHealth - hpBefore),
                Mathf.RoundToInt((stamina ? stamina.currentStamina : 0) - spBefore), (rifle ? rifle.ReserveRounds : 0) - ammoBefore));
            availableAt = Time.time + cooldownSeconds;
            GameWorldProgress.Record(this, -Mathf.CeilToInt(Remaining) - 1);
            GameplayAudio.Play(GameplayAudio.Cue.Pickup, transform.position, true); return true;
        }
        void ShowFeedback(string value) { feedback = value; feedbackUntil = Time.unscaledTime + 3f; }
        void Update()
        {
            if (!player && Time.unscaledTime >= searchAt)
            { searchAt = Time.unscaledTime + 1; player = FindFirstObjectByType<ThirdPersonPlayerController>(); }
            if (player && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
                TryRefill(player);
        }
        void OnGUI()
        {
            if (!player || !player.enabled || player.ExternalControlLock || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput ||
                IslandMapUI.IsOpen || PlayerDeathRespawn.IsOpen) return;
            var body = player.GetComponent<CharacterController>();
            Vector3 feet = player.transform.position; if (body) feet.y = body.bounds.min.y;
            if (Vector3.Distance(feet, transform.position) > 5) return;
            if (style == null) style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 19, font = GameLocalization.Font };
            string hint = Remaining > 0 ? ("Restocking: ") + Mathf.CeilToInt(Remaining) + " s" : supportsMelee ?
                "F — restore 35% HP / 25 SP; M4A1 ammo if owned\n45s restock · Does not refill healing packs" :
                (player.GetComponent<SahurWeaponLoadout>()?.HasM4A1 ?? false) ?
                ("F — take up to " + rounds + " rounds and recover stamina (no healing)") :
                ("Find the M4A1 first; this cache will remain available");
            if (!string.IsNullOrEmpty(Feedback))
                hint = Feedback + (Remaining > 0 ? "\nRestocking: " + Mathf.CeilToInt(Remaining) + " s" : "");
            GUI.Box(new Rect(Screen.width * .5f - 300, Screen.height - 180, 600, supportsMelee || !string.IsNullOrEmpty(Feedback) ? 100 : 72), DisplayName + "\n" + hint, style);
        }
    }
}
