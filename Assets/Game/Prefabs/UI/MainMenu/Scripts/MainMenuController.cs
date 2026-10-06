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
        if (!Application.CanStreamedLevelBeLoaded(StoryScenePath))
        {
            Debug.LogError("[Main Menu] Story1 is missing from Build Settings.", this);
            return;
        }
        HealingPackSupply.StartNewGame();
        GameSaveManager.CancelPendingContinue();
        StartGame(StoryScenePath);
    }

    public void ContinueGame()
    {
        if (!GameSaveManager.QueueContinue())
        {
            RefreshSaveStatus();
            return;
        }

        StartGame(GameScenePath);
    }

    void StartGame(string scenePath)
    {
        if (!Application.CanStreamedLevelBeLoaded(scenePath))
        {
            GameSaveManager.CancelPendingContinue();
            Debug.LogError($"[Main Menu] {scenePath} is missing from Build Settings.", this);
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
    }

    void RefreshSaveStatus()
    {
        bool hasSave = GameSaveManager.HasSave;
        if (continueButton != null)
            continueButton.interactable = hasSave;
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
