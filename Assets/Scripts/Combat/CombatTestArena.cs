using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Flat arena for trying bosses and Chicken Bros without the island story.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatTestArena : MonoBehaviour
    {
        public GameObject cappuccino;
        public GameObject nailong;
        public GameObject bombardino;
        public GameObject chicken;
        public GameObject duck;
        public ThirdPersonPlayerController player;
        readonly List<GameObject> spawned = new List<GameObject>(16);
        Text hint;

        void Start()
        {
            if (!player) player = FindFirstObjectByType<ThirdPersonPlayerController>();
            if (player)
            {
                player.seaLevel = -80f;
                if (!player.GetComponent<GameSaveExcluded>()) player.gameObject.AddComponent<GameSaveExcluded>();
                var inventory = player.GetComponent<EquipmentInventory>();
                var blades = Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades");
                var rifle = Resources.Load<EquipmentItem>(SahurWeaponLoadout.M4ItemPath);
                if (inventory && blades) inventory.Collect(blades);
                if (inventory && rifle) inventory.Collect(rifle);
                var health = player.GetComponent<PlayerHealth>();
                if (health) health.currentHealth = health.maxHealth;
                var stamina = player.GetComponent<PlayerStamina>();
                if (stamina) stamina.currentStamina = stamina.maxStamina;
            }
            BuildHint();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (PauseSettingsMenu.IsOpen || Keyboard.current == null) return;
            var keys = Keyboard.current;
            if (keys.f5Key.wasPressedThisFrame) SpawnOne(cappuccino, 16f);
            if (keys.f6Key.wasPressedThisFrame) SpawnOne(nailong, 10f);
            if (keys.f7Key.wasPressedThisFrame) SpawnOne(bombardino, 22f);
            if (keys.f8Key.wasPressedThisFrame) SpawnOne(chicken, 7f);
            if (keys.f9Key.wasPressedThisFrame) SpawnChickens(6);
            if (keys.f12Key.wasPressedThisFrame) SpawnOne(duck, 7f);
            if (keys.f10Key.wasPressedThisFrame) ClearSpawned();
            if (keys.f11Key.wasPressedThisFrame) Heal();
        }

        void SpawnOne(GameObject prefab, float distance)
        {
            if (!prefab || !player) return;
            ClearSpawned();
            Vector3 at = player.transform.position + player.transform.forward * distance;
            at.y = FloorHeight(at);
            Spawn(prefab, at);
        }

        void SpawnChickens(int count)
        {
            if (!chicken || !player) return;
            ClearSpawned();
            Vector3 origin = player.transform.position;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                Vector3 at = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 8f;
                at.y = FloorHeight(at);
                Spawn(chicken, at);
            }
        }

        void Spawn(GameObject prefab, Vector3 position)
        {
            Vector3 toPlayer = player ? Vector3.ProjectOnPlane(player.transform.position - position, Vector3.up) : Vector3.back;
            Quaternion facing = toPlayer.sqrMagnitude > .01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;
            var actor = Instantiate(prefab, position, facing);
            if (!actor.GetComponent<GameSaveExcluded>()) actor.AddComponent<GameSaveExcluded>();
            spawned.Add(actor);
        }

        void ClearSpawned()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i]) Destroy(spawned[i]);
            spawned.Clear();
        }

        void Heal()
        {
            if (!player) return;
            var health = player.GetComponent<PlayerHealth>();
            if (health) health.currentHealth = health.maxHealth;
            var stamina = player.GetComponent<PlayerStamina>();
            if (stamina) stamina.currentStamina = stamina.maxStamina;
        }

        float FloorHeight(Vector3 at)
        {
            if (Physics.Raycast(at + Vector3.up * 20f, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point.y + .05f;
            return 0f;
        }

        void BuildHint()
        {
            var canvas = new GameObject("Combat test hint", typeof(RectTransform), typeof(Canvas));
            var view = canvas.GetComponent<Canvas>();
            view.renderMode = RenderMode.ScreenSpaceOverlay;
            view.sortingOrder = 30;
            GameUITheme.Scale(view);
            var card = GameUITheme.Rect(canvas.transform, "Card", new Vector2(0f, 1f), new Vector2(460, 196));
            card.pivot = new Vector2(0f, 1f);
            card.anchoredPosition = new Vector2(18f, -18f);
            GameUITheme.Image(card, new Color(.04f, .05f, .05f, .82f));
            hint = GameUITheme.Label(card, "Hint", 18, new Vector2(16f, -90f), new Vector2(428, 176), TextAnchor.UpperLeft);
            hint.text = "Combat test\nF5 Cappuccino   F6 Nailong\nF7 Bombardino   F8 Chicken Bro\nF9 Six chickens   F12 Long-legged Duck\nF10 Clear   F11 Heal\n1 Stick   2 Twin blades   3 M4A1";
        }
    }
}
