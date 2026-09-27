using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Placed on Sahur so additive scene loads do not require a hand-edited scene UI.</summary>
public sealed class SettingsMenuInstaller : MonoBehaviour
{
    public PauseSettingsMenu menuPrefab;

    void Start()
    {
        if (menuPrefab == null || FindFirstObjectByType<PauseSettingsMenu>() != null)
            return;

        PauseSettingsMenu menu = Instantiate(menuPrefab);
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventObject = new GameObject("Settings Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventObject.transform.SetParent(menu.transform, false);
        eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
}
