using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Mavis
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-200)]
    public sealed class PlayerDeathRespawn : MonoBehaviour
    {
        public static bool IsOpen => active != null && active.IsDead;
        public static bool BlocksInput => IsOpen || consumedFrame == Time.frameCount;
        static PlayerDeathRespawn active;
        static int consumedFrame = -1;
        public bool IsDead { get; private set; }
        public IslandRespawnPoint Checkpoint { get; private set; }
        public float protectionSeconds = 2f;
        PlayerHealth health;
        ThirdPersonPlayerController movement;
        GameObject overlay;
        Font font;
        bool ownsFont;
        float previousTimeScale;
        Vector3 initialPosition;
        Quaternion initialRotation;
        float animatorSpeed;
        readonly List<Behaviour> disabled = new List<Behaviour>();
        public bool PanelVisible => overlay != null && overlay.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { active = null; consumedFrame = -1; }
        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            movement = GetComponent<ThirdPersonPlayerController>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            health.OnDeath.AddListener(Die);
        }
        void Start() { FindCheckpoint(); }
        void FindCheckpoint()
        {
            if (Checkpoint != null) return;
            float distance = float.PositiveInfinity;
            foreach (var point in FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None))
            {
                if (Checkpoint != null && Checkpoint.isDefault && !point.isDefault) continue;
                float d = (point.transform.position - transform.position).sqrMagnitude;
                if (Checkpoint == null || (point.isDefault && !Checkpoint.isDefault) || d < distance)
                { Checkpoint = point; distance = d; }
            }
        }
        public void SetCheckpoint(IslandRespawnPoint point) { if (!IsDead && point != null) Checkpoint = point; }
        void Update()
        {
            if (!IsDead && health.currentHealth <= 0f) Die();
            if (IsDead && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) Respawn();
        }
        void Disable(Behaviour component)
        {
            if (component != null && component.enabled) { disabled.Add(component); component.enabled = false; }
        }
        public void Die()
        {
            if (IsDead || health.currentHealth > 0f) return;
            if (health.NarrativeDefeatHandler != null) { health.NarrativeDefeatHandler(); return; }
            GetComponent<IslandMapUI>()?.SetOpen(false);
            GetComponent<EnemyLockOn>()?.Clear();
            GetComponent<SahurLoadoutUI>()?.SetOpen(false);
            foreach (var menu in FindObjectsByType<PauseSettingsMenu>(FindObjectsSortMode.None))
                if (PauseSettingsMenu.IsOpen) menu.Resume();
            previousTimeScale = Time.timeScale;
            IsDead = true;
            active = this;
            GetComponent<SahurAttack>()?.SuspendForSwimming();
            Disable(GetComponent<SahurAttack>());
            Disable(GetComponent<SahurBoomerang>()); // OnDisable returns the weapon to its hand.
            Disable(GetComponent<CombatKnockback>());
            Disable(movement);
            GetComponent<PlayerStamina>()?.StopSprinting();
            if (movement != null && movement.CharacterAnimator != null)
            {
                var animator = movement.CharacterAnimator;
                animatorSpeed = animator.speed;
                animator.speed = 0f;
            }
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (overlay == null) BuildUI();
            overlay.SetActive(true);
        }
        public bool Respawn()
        {
            if (!IsDead) return false;
            FindCheckpoint();
            var capsule = GetComponent<CharacterController>();
            Vector3 position = initialPosition;
            Quaternion rotation = initialRotation;
            if (Checkpoint != null)
            {
                float sole = capsule != null ? (capsule.center.y - capsule.height * 0.5f) * Mathf.Abs(transform.lossyScale.y) : 0f;
                position = Checkpoint.GroundPosition + Vector3.up * (0.08f - sole);
                rotation = Checkpoint.transform.rotation;
            }
            if (movement != null) movement.RestoreSavedPose(position, rotation);
            else transform.SetPositionAndRotation(position, rotation);
            health.currentHealth = health.maxHealth;
            health.GrantProtection(protectionSeconds);
            var stamina = GetComponent<PlayerStamina>();
            if (stamina != null) { stamina.StopSprinting(); stamina.currentStamina = stamina.maxStamina; }
            if (movement != null && movement.CharacterAnimator != null)
            {
                var animator = movement.CharacterAnimator;
                animator.speed = animatorSpeed;
                animator.Rebind();
                animator.Play("Locomotion", 0, 0f);
                animator.Update(0f);
            }
            foreach (var component in disabled) if (component != null) component.enabled = true;
            disabled.Clear();
            IsDead = false;
            overlay.SetActive(false);
            consumedFrame = Time.frameCount;
            if (active == this) active = null;
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return true;
        }
        void BuildUI()
        {
            foreach (var name in Font.GetOSInstalledFontNames())
                if (name == "Microsoft YaHei") { font = Font.CreateDynamicFontFromOSFont(name, 30); ownsFont = true; break; }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            overlay = new GameObject("Sahur Death Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
            var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(overlay.transform, false);
            var rect = (RectTransform)backdrop.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = GameUITheme.Backdrop;
            Label("你倒下了", 64, 125, GameUITheme.Foreground);
            Label("在岛上的复活点重新出发", 26, 32, Color.white);
            Label("生命与体力恢复 · 已拾取装备保留", 22, -25, GameUITheme.Secondary);
            var button = new GameObject("Respawn button", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(overlay.transform, false);
            var buttonRect = (RectTransform)button.transform; buttonRect.sizeDelta = new Vector2(380, 78); buttonRect.anchoredPosition = new Vector2(0, -130);
            GameUITheme.StyleButton(button.GetComponent<Button>());
            button.GetComponent<Button>().onClick.AddListener(() => Respawn());
            var text = Label("复活  /  Enter", 28, 0, Color.white);
            text.transform.SetParent(button.transform, false);
            if (EventSystem.current == null)
                new GameObject("Death UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
        Text Label(string value, int size, float y, Color color)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(overlay.transform, false);
            var rect = (RectTransform)obj.transform; rect.sizeDelta = new Vector2(1000, 90); rect.anchoredPosition = new Vector2(0, y);
            var label = obj.GetComponent<Text>(); label.font = font; label.fontSize = size;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = value; label.alignment = TextAnchor.MiddleCenter; label.color = color; label.raycastTarget = false;
            LocalizedGameText.Bind(label);
            return label;
        }
        void OnDestroy()
        {
            if (health != null) health.OnDeath.RemoveListener(Die);
            if (active == this) { active = null; if (IsDead) Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f; }
            if (overlay != null) Destroy(overlay);
            if (ownsFont && font != null) Destroy(font);
        }
    }
}
