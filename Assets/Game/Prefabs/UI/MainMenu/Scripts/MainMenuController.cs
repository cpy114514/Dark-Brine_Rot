using Mavis;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Starts the sailing prologue or resumes saved first-island progress.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    const string StoryScenePath = "Assets/Scenes/First story/Story1.unity";
    const string GameScenePath = "Assets/Scenes/First Island/Main.unity";

    public Button continueButton;
    public PauseSettingsMenu settingsMenu;

    TMP_Text saveStatus;

    void Awake()
    {
        LocalizedGameText.BindTree(transform);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var inputModule = FindFirstObjectByType<InputSystemUIInputModule>();
        if (inputModule != null)
            inputModule.AssignDefaultActions();
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        Transform saveStatusTransform = transform.Find("Save Status");
        if (saveStatusTransform != null)
            saveStatus = saveStatusTransform.GetComponent<TMP_Text>();
        RefreshSaveStatus();
        MinimalMenuLayout.MainMenu(this);
        if (continueButton) continueButton.GetComponentInChildren<TMP_Text>().text = "LOAD GAME";
        var quit = transform.Find("Quit");
        if (quit) quit.GetComponentInChildren<TMP_Text>().text = "QUIT TO DESKTOP";
        Mavis.VisualStyles.VisualStyleUIController.InstallMainMenu(this);
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
        if(SceneLoadingScreen.IsLoading)return;
        if (!Application.CanStreamedLevelBeLoaded(StoryScenePath))
        {
            Debug.LogError("[Main Menu] Story1 is missing from Build Settings.", this);
            return;
        }
        SaveSlotMenu.Show(SaveSlotMenu.Mode.NewGame, slot =>
        {
            if (!GameSaveManager.PrepareNewGame(slot)) return false;
            StartGame(StoryScenePath);
            return true;
        });
    }

    public void ContinueGame()
    {
        if(SceneLoadingScreen.IsLoading)return;
        SaveSlotMenu.Show(SaveSlotMenu.Mode.Load, slot =>
        {
            if (!GameSaveManager.QueueContinue(slot)) { RefreshSaveStatus(); return false; }
            StartGame(GameSaveManager.ContinueScenePath);
            return true;
        });
    }

    void StartGame(string scenePath)
    {
        if(SceneLoadingScreen.IsLoading)return;
        if (!Application.CanStreamedLevelBeLoaded(scenePath))
        {
            GameSaveManager.CancelPendingContinue();
            Debug.LogError($"[Main Menu] {scenePath} is missing from Build Settings.", this);
            return;
        }
        Time.timeScale = 1f;
        SceneLoadingScreen.Load(scenePath);
    }

    void RefreshSaveStatus()
    {
        bool hasSave = GameSaveManager.HasSave;
        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
            GameUITheme.StyleButton(continueButton);
        }
        if (saveStatus != null)
            saveStatus.text = hasSave ? "SAVE DATA FOUND" : "NO SAVE DATA";
    }

    public void Settings()
    {
        if (settingsMenu != null)
            settingsMenu.OpenSettingsFromMainMenu();
    }

    public void Quit()
    {
        GameSaveManager.SaveCurrentGame();
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
