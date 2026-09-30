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
        }

        static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool HasSave => TryReadSave(out _);

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
            if (player == null)
                return false;

            try
            {
                var data = new SaveData
                {
                    version = SaveVersion,
                    playerPosition = player.transform.position,
                    playerRotation = player.transform.rotation,
                    savedAtUtcTicks = DateTime.UtcNow.Ticks
                };

                PlayerHealth health = player.GetComponent<PlayerHealth>();
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

                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
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

                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (data.hasHealth && health != null)
                    health.currentHealth = Mathf.Clamp(data.health, 0f, health.maxHealth);

                PlayerStamina stamina = player.GetComponent<PlayerStamina>();
                if (data.hasStamina && stamina != null)
                    stamina.currentStamina = Mathf.Clamp(data.stamina, 0f, stamina.maxStamina);

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
                    (data.hasStamina && !IsFinite(data.stamina)))
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
