using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Stores the current first-island player state between game sessions.</summary>
    public sealed class GameSaveManager : MonoBehaviour
    {
        const int SaveVersion = 1;
        const string SaveFileName = "dark-brine-rot-save.json";
        const string GameplaySceneName = "Gameplay";

        static GameSaveManager instance;
        SaveData pendingSave;
        bool restorePending;
        bool restoreRoutineRunning;

        [Serializable]
        sealed class SaveData
        {
            public int version;
            public Vector3 playerPosition;
            public Quaternion playerRotation;
            public bool hasHealth;
            public float health;
            public bool hasStamina;
            public float stamina;
            public long savedAtUtcTicks;
            public bool hasArrivalState;
            public bool hasLanded;
            public bool hasStick;
            public bool hasHealingPacks;
            public int healingPacksRemaining;
            public string healingCampaign;
            public bool ownsWeaknessGlasses, weaknessGlassesEquipped;
            public float weaknessActiveRemaining, weaknessCooldownRemaining;
            public bool hasWeaponState, ownsTwinBlades;
            public int selectedWeapon;
        }

        static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool HasSave => TryReadSave(out _);
        public static bool IsContinuePending => instance != null && instance.restorePending;
        public static bool HasSavableGame => CanSavePlayer(FindFirstObjectByType<ThirdPersonPlayerController>());

        static bool CanSavePlayer(ThirdPersonPlayerController player)
        {
            if (player == null || player.gameObject.scene.name != GameplaySceneName ||
                player.GetComponent<GameSaveExcluded>() != null) return false;
            var arrival = player.GetComponent<FirstIslandArrival>();
            return arrival == null || arrival.IsAwake;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateManager()
        {
            EnsureManager();
        }

        static GameSaveManager EnsureManager()
        {
            if (instance != null)
                return instance;

            GameSaveManager existing = FindFirstObjectByType<GameSaveManager>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            var managerObject = new GameObject("Game Save Manager");
            instance = managerObject.AddComponent<GameSaveManager>();
            DontDestroyOnLoad(managerObject);
            return instance;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        public static bool QueueContinue()
        {
            if (!TryReadSave(out SaveData data))
                return false;

            GameSaveManager manager = EnsureManager();
            manager.pendingSave = data;
            manager.restorePending = true;
            HealingPackSupply.RestoreCampaign(data.hasHealingPacks ? data.healingCampaign : "legacy",
                data.hasHealingPacks ? data.healingPacksRemaining : HealingPackSupply.Total);
            return true;
        }

        public static void CancelPendingContinue()
        {
            if (instance == null)
                return;

            instance.pendingSave = null;
            instance.restorePending = false;
        }

        public static bool SaveCurrentGame()
        {
            ThirdPersonPlayerController player = FindFirstObjectByType<ThirdPersonPlayerController>();
            if (!CanSavePlayer(player)) return false;
            var arrival = player.GetComponent<FirstIslandArrival>();

            try
            {
                var data = new SaveData
                {
                    version = SaveVersion,
                    playerPosition = player.transform.position,
                    playerRotation = player.transform.rotation,
                    savedAtUtcTicks = DateTime.UtcNow.Ticks,
                    hasHealingPacks = true,
                    healingPacksRemaining = HealingPackSupply.Remaining,
                    healingCampaign = HealingPackSupply.Campaign
                };
                if (arrival != null)
                {
                    data.hasArrivalState = true;
                    data.hasLanded = arrival.HasLanded;
                    data.hasStick = arrival.HasStick;
                }

                PlayerHealth health = player.GetComponent<PlayerHealth>();
                var glasses=player.GetComponent<WeaknessGlasses>();
                if(glasses!=null)
                {
                    data.ownsWeaknessGlasses=glasses.Owned;data.weaknessGlassesEquipped=glasses.Equipped;
                    data.weaknessActiveRemaining=glasses.ActiveRemaining;data.weaknessCooldownRemaining=glasses.CooldownRemaining;
                }
                if (health != null)
                {
                    data.hasHealth = true;
                    data.health = health.currentHealth;
                }

                PlayerStamina stamina = player.GetComponent<PlayerStamina>();
                if (stamina != null)
                {
                    data.hasStamina = true;
                    data.stamina = stamina.currentStamina;
                }

                var weapons = player.GetComponent<SahurWeaponLoadout>();
                if (weapons != null)
                {
                    weapons.Initialize();
                    data.hasWeaponState = true;
                    data.ownsTwinBlades = weapons.HasTwinBlades;
                    data.selectedWeapon = (int)weapons.CurrentWeapon;
                }

                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                // Commit only a completely written file, preserving the previous save
                // if writing is interrupted or the replacement cannot be completed.
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Replace(temporary, SavePath, null);
                else File.Move(temporary, SavePath);
                PlayerPrefs.Save();
                Debug.Log("[Save] Game progress saved.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Could not save game progress: {exception.Message}");
                return false;
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveCurrentGame();
        }

        void OnApplicationQuit()
        {
            SaveCurrentGame();
            PlayerPrefs.Save();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (restorePending && !restoreRoutineRunning && scene.name == GameplaySceneName)
                StartCoroutine(RestoreAfterSceneStarts());
        }

        IEnumerator RestoreAfterSceneStarts()
        {
            restoreRoutineRunning = true;

            // Let the additive world and the player's Start methods finish first.
            yield return null;
            yield return null;

            ThirdPersonPlayerController player = null;
            for (int attempt = 0; attempt < 30 && player == null; attempt++)
            {
                player = FindFirstObjectByType<ThirdPersonPlayerController>();
                if (player == null)
                    yield return null;
            }

            if (restorePending && player != null && pendingSave != null)
            {
                SaveData data = pendingSave;
                player.RestoreSavedPose(data.playerPosition, data.playerRotation);
                player.GetComponent<WeaknessGlasses>()?.Restore(data.ownsWeaknessGlasses,data.weaknessGlassesEquipped,data.weaknessActiveRemaining,data.weaknessCooldownRemaining);
                var arrival = player.GetComponent<FirstIslandArrival>();
                if (arrival != null)
                    arrival.RestoreProgress(!data.hasArrivalState || data.hasLanded, !data.hasArrivalState || data.hasStick);

                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (data.hasHealth && health != null)
                    health.currentHealth = Mathf.Clamp(data.health, 0f, health.maxHealth);

                PlayerStamina stamina = player.GetComponent<PlayerStamina>();
                if (data.hasStamina && stamina != null)
                    stamina.currentStamina = Mathf.Clamp(data.stamina, 0f, stamina.maxStamina);

                if (data.hasWeaponState)
                    player.GetComponent<SahurWeaponLoadout>()?.Restore(data.ownsTwinBlades, (SahurWeaponLoadout.Weapon)data.selectedWeapon);

                Debug.Log("[Save] Saved game progress restored.");
            }
            else if (restorePending)
            {
                Debug.LogError("[Save] Continue was selected, but the player could not be found in the Gameplay scene.");
            }

            pendingSave = null;
            restorePending = false;
            restoreRoutineRunning = false;
        }

        static bool TryReadSave(out SaveData data)
        {
            data = null;
            if (!File.Exists(SavePath))
                return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data == null || data.version != SaveVersion ||
                    !IsFinite(data.playerPosition.x) || !IsFinite(data.playerPosition.y) ||
                    !IsFinite(data.playerPosition.z) ||
                    !IsFinite(data.playerRotation.x) || !IsFinite(data.playerRotation.y) ||
                    !IsFinite(data.playerRotation.z) || !IsFinite(data.playerRotation.w) ||
                    (data.hasHealth && !IsFinite(data.health)) ||
                    (data.hasStamina && !IsFinite(data.stamina)) ||
                    !IsFinite(data.weaknessActiveRemaining) || !IsFinite(data.weaknessCooldownRemaining) ||
                    (data.hasWeaponState && (data.selectedWeapon < 0 || data.selectedWeapon > 1)))
                {
                    data = null;
                    return false;
                }

                float rotationMagnitude = Mathf.Sqrt(
                    data.playerRotation.x * data.playerRotation.x +
                    data.playerRotation.y * data.playerRotation.y +
                    data.playerRotation.z * data.playerRotation.z +
                    data.playerRotation.w * data.playerRotation.w);
                if (rotationMagnitude < 0.001f)
                {
                    data = null;
                    return false;
                }

                data.playerRotation = data.playerRotation.normalized;
                return true;
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
