using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Prevents marshmallow aggression until Sahur first moves after the opening.</summary>
[DefaultExecutionOrder(600), DisallowMultipleComponent]
public sealed class SecondIslandStartProtection : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    static SecondIslandStartProtection active;
    ThirdPersonPlayerController player;
    CharacterController body;
    SkyboardReward skyboard;
    Vector3 previous;
    public bool WaitingForMovement { get; private set; } = true;
    public static bool Protects(PlayerHealth target) => active && active.WaitingForMovement &&
        target && active.gameObject == target.gameObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        active = null;
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) Install(SceneManager.GetSceneAt(i));
    }
    static void SceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);
    static void Install(Scene scene)
    {
        if (!scene.isLoaded || scene.path != ScenePath) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<ThirdPersonPlayerController>(true))
                if (candidate && candidate.gameObject.scene == scene)
                {
                    if (!candidate.GetComponent<SecondIslandStartProtection>()) candidate.gameObject.AddComponent<SecondIslandStartProtection>();
                    return;
                }
    }
    void Awake()
    {
        active = this; player = GetComponent<ThirdPersonPlayerController>(); body = GetComponent<CharacterController>();
        previous = transform.position;
    }
    void LateUpdate()
    {
        if (!WaitingForMovement || !player || !body) return;
        Vector3 position = transform.position;
        Vector3 delta = position - previous;
        previous = position;
        if (!skyboard && player.ExternalMovementLock) skyboard = FindFirstObjectByType<SkyboardReward>();
        if (Time.deltaTime <= 0 || PauseSettingsMenu.IsOpen || GameUITheme.ModalOpen ||
            !player.enabled || !body.enabled || player.ExternalControlLock || player.ExternalMovementLock && (!skyboard || !skyboard.Flying) ||
            Cursor.lockState != CursorLockMode.Locked) return;
        bool direction = GameInputSettings.Pressed(GameInputSettings.Action.Forward) || GameInputSettings.Pressed(GameInputSettings.Action.Back) ||
            GameInputSettings.Pressed(GameInputSettings.Action.Left) || GameInputSettings.Pressed(GameInputSettings.Action.Right);
        bool flight = skyboard && skyboard.Flying;
        // Space also advances the dialogue. Holding that same press while the
        // character settles must not count as a deliberate jump.
        bool vertical = GameInputSettings.Pressed(GameInputSettings.Action.Jump) && (player.VerticalSpeed > 1f || flight) ||
            GameInputSettings.Pressed(GameInputSettings.Action.Dodge) && (player.IsDodging || flight);
        if (direction && Vector3.ProjectOnPlane(delta, Vector3.up).sqrMagnitude > .0001f || vertical && Mathf.Abs(delta.y) > .01f)
            WaitingForMovement = false;
    }
    void OnDestroy() { if (active == this) active = null; }
}
