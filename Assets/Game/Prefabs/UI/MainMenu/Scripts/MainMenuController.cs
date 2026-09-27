using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Minimal title-screen navigation for the first-island build.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    const string GameScenePath = "Assets/Scenes/First Island/Main.unity";

    public Button continueButton;
    public PauseSettingsMenu settingsMenu;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var inputModule = FindFirstObjectByType<InputSystemUIInputModule>();
        if (inputModule != null)
            inputModule.AssignDefaultActions();
        // There is no save/checkpoint system yet. Do not present a fake load action.
        if (continueButton != null)
            continueButton.interactable = false;
    }

    void Update()
    {
        if (!PauseSettingsMenu.IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void NewGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(GameScenePath))
        {
            Debug.LogError("[Main Menu] First Island/Main is missing from Build Settings.", this);
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameScenePath, LoadSceneMode.Single);
    }

    public void Settings()
    {
        if (settingsMenu != null)
            settingsMenu.OpenSettingsFromMainMenu();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
