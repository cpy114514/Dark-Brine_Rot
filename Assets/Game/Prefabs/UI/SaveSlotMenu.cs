using System;
using Mavis;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Shared English save selection for the title screen and paused game.</summary>
[DefaultExecutionOrder(-250)]
public sealed class SaveSlotMenu : MonoBehaviour
{
    public enum Mode { Load, Save, NewGame }
    static SaveSlotMenu instance;
    static int closedFrame = -1;
    public static bool IsOpen => instance;
    public static bool BlocksInput => IsOpen || closedFrame == Time.frameCount;
    Mode mode;
    Func<int, bool> action;
    float previousTime;
    int confirmation;
    Text hint;
    GameObject previousSelection;
    readonly Button[] buttons = new Button[GameSaveManager.SlotCount];
    readonly Text[] captions = new Text[GameSaveManager.SlotCount];
    bool closed;

    public static void Show(Mode mode, Func<int, bool> action, string error = null)
    {
        if (instance) return;
        var root = new GameObject("Save Slots", typeof(Canvas), typeof(GraphicRaycaster), typeof(SaveSlotMenu));
        var menu = root.GetComponent<SaveSlotMenu>();
        instance = menu;
        menu.mode = mode;
        menu.action = action;
        menu.previousTime = Time.timeScale;
        menu.previousSelection = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        GameUITheme.Scale(canvas);
        var background = GameUITheme.Fill((RectTransform)root.transform, "Backdrop", GameUITheme.Backdrop);
        background.raycastTarget = true;
        var panel = GameUITheme.Rect(root.transform, "Slot panel", new Vector2(.5f, .5f), new Vector2(900, 690));
        GameUITheme.Image(panel, GameUITheme.Surface);
        var title = GameUITheme.Label(panel, "Title", 36, new Vector2(0, -60), new Vector2(800, 56), TextAnchor.MiddleCenter);
        title.text = mode == Mode.Load ? "LOAD GAME" : mode == Mode.Save ? "SAVE GAME" : "NEW GAME";
        menu.hint = GameUITheme.Label(panel, "Status", 20, new Vector2(0, -119), new Vector2(810, 58), TextAnchor.MiddleCenter);
        menu.hint.text = error ?? "Choose a save slot";
        for (int i = 0; i < GameSaveManager.SlotCount; i++)
        {
            int slot = i + 1;
            menu.buttons[i] = MakeButton(panel, "Slot " + slot, -211 - i * 112, 92, out menu.captions[i]);
            menu.buttons[i].onClick.AddListener(() => menu.Choose(slot));
        }
        var cancel = MakeButton(panel, "Back", -588, 58, out var back);
        back.text = "BACK";
        cancel.onClick.AddListener(menu.Close);
        menu.Refresh();
        var navigation = new System.Collections.Generic.List<Button>();
        foreach (var button in menu.buttons) if (button.interactable) navigation.Add(button);
        navigation.Add(cancel);
        for (int i = 0; i < navigation.Count; i++)
            navigation[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = navigation[(i + navigation.Count - 1) % navigation.Count],
                selectOnDown = navigation[(i + 1) % navigation.Count] };
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(cancel.gameObject);
    }

    static Button MakeButton(Transform parent, string name, float y, float height, out Text caption)
    {
        var rect = GameUITheme.Rect(parent, name, new Vector2(.5f, 1), new Vector2(800, height));
        rect.anchoredPosition = new Vector2(0, y);
        var image = GameUITheme.Image(rect, Color.white);
        image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        GameUITheme.StyleButton(button);
        // Apply the palette immediately, including on the first paused frame.
        var colors = button.colors;
        colors.fadeDuration = 0;
        button.colors = colors;
        caption = GameUITheme.Label(rect, "Caption", 22, new Vector2(0, -height * .5f), new Vector2(760, height - 10), TextAnchor.MiddleCenter);
        return button;
    }

    void Refresh()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var slot = GameSaveManager.GetSlot(i + 1);
            buttons[i].interactable = mode != Mode.Load || slot.readable;
            captions[i].text = confirmation == slot.number ? "CONFIRM OVERWRITE SLOT " + slot.number :
                "SLOT " + slot.number + (slot.number == GameSaveManager.ActiveSlot ? "  /  ACTIVE" : "") + "\n" + slot.description;
        }
    }

    void Choose(int slot)
    {
        if (mode != Mode.Load && GameSaveManager.GetSlot(slot).exists && confirmation != slot)
        {
            confirmation = slot;
            hint.text = "Existing progress in this slot will be replaced. Select it again to confirm.";
            Refresh();
            return;
        }
        var callback = action;
        var selectedMode = mode;
        Close();
        if (!callback(slot)) Show(selectedMode, callback, "Could not complete this action. Please try again.");
    }

    void Update()
    {
        if (GameControls.Down(GameControls.Command.Cancel)) Close();
    }

    public void Close()
    {
        if (closed) return;
        closed = true;
        closedFrame = Time.frameCount;
        GameControls.ConsumeMenuInput();
        Time.timeScale = previousTime;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(previousSelection && previousSelection.activeInHierarchy ? previousSelection : null);
        if (instance == this) instance = null;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (instance != this) return;
        instance = null;
        Time.timeScale = previousTime;
    }
}
