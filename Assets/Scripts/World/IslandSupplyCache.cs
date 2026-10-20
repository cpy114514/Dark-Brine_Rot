using UnityEngine;
using UnityEngine.InputSystem;

namespace Mavis
{
    /// <summary>Visible, repeatable ammunition reward for reaching a side-route landmark.</summary>
    [DisallowMultipleComponent]
    public sealed class IslandSupplyCache : MonoBehaviour
    {
        public string englishName = "Supply camp", chineseName = "Supply camp";
        public int rounds = 45;
        public float cooldownSeconds = 45, interactionRadius = 3;
        float availableAt, searchAt;
        ThirdPersonPlayerController player;
        GUIStyle style;
        public string DisplayName => englishName;
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
            if (!health || health.currentHealth <= 0 || !loadout || !loadout.HasM4A1 || !rifle) return false;
            var body = actor.GetComponent<CharacterController>();
            Vector3 feet = actor.transform.position; if (body) feet.y = body.bounds.min.y;
            if (Mathf.Abs(feet.y - transform.position.y) > 3 ||
                Vector3.ProjectOnPlane(feet - transform.position, Vector3.up).sqrMagnitude > interactionRadius * interactionRadius) return false;
            Vector3 from = feet + Vector3.up, delta = transform.position + Vector3.up * .6f - from;
            foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(actor.transform)) return false;
            if (rifle.AddReserveAmmo(rounds) <= 0) return false;
            var stamina = actor.GetComponent<PlayerStamina>();
            if (stamina) stamina.currentStamina = Mathf.Min(stamina.maxStamina, stamina.currentStamina + 25);
            availableAt = Time.time + cooldownSeconds;
            GameWorldProgress.Record(this, -Mathf.CeilToInt(Remaining) - 1);
            GameplayAudio.Play(GameplayAudio.Cue.Pickup, transform.position, true); return true;
        }
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
            string hint = Remaining > 0 ? ("Restocking: ") + Mathf.CeilToInt(Remaining) + " s" :
                (player.GetComponent<SahurWeaponLoadout>()?.HasM4A1 ?? false) ?
                ("F — take up to 45 rounds and recover stamina") :
                ("Find the M4A1 first; this cache will remain available");
            GUI.Box(new Rect(Screen.width * .5f - 245, Screen.height - 160, 490, 72), DisplayName + "\n" + hint, style);
        }
    }
}
