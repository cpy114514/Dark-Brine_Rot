using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Mavis
{
    /// <summary>Collected equipment display and placeholder skill slots. No stats or skill execution.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-120)]
    public sealed class SahurLoadoutUI : MonoBehaviour
    {
        public enum EquipmentSlot { Head, Body, Legs, Feet, Weapon }
        public static bool IsOpen { get; private set; }
        public static bool BlocksInput => IsOpen || IslandMapUI.BlocksInput || PlayerDeathRespawn.BlocksInput || consumedFrame == Time.frameCount;
        static int consumedFrame = -1;
        static SahurLoadoutUI active;
        public int SelectedSkill { get; private set; }
        public int EditingSkill { get; private set; }
        public bool PanelVisible => panel != null && panel.activeSelf;
        public int EquipmentSlotCount => 5;
        public int SkillSlotCount => 4;
        [Tooltip("Keep disabled while there are no usable skills; the four assignment slots remain in the I menu.")]
        public bool showEmptySkillHotbar;

        readonly string[] equipmentNames = { "头部", "身体", "腿部", "脚部", "武器" };
        readonly Image[] hotbarFrames = new Image[4];
        readonly Image[] skillFrames = new Image[4];
        readonly Text[] equipmentLabels = new Text[5];
        EquipmentInventory inventory;
        Text pickupNotice;
        float noticeUntil;
        int editingEquipment = -1;
        GameObject root, panel;
        CanvasGroup hud;
        Text loadoutHint;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        float hintRemaining = 8f;
        Text details;
        Font font;
        bool ownsFont;
        readonly System.Collections.Generic.List<EquipmentItem> recentPickups = new System.Collections.Generic.List<EquipmentItem>();
        float previousTimeScale;
        CursorLockMode previousCursor;
        bool previousCursorVisible;
        readonly Color track = GameUITheme.Track;
        readonly Color accent = GameUITheme.Foreground;

        void Awake()
        {
            if (active != null && active != this) { enabled = false; return; }
            active = this;
            foreach (string name in Font.GetOSInstalledFontNames())
                if (name == "Microsoft YaHei") { font = Font.CreateDynamicFontFromOSFont(name, 24); ownsFont = true; break; }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            movement = GetComponent<ThirdPersonPlayerController>();
            health = GetComponent<PlayerHealth>();
            inventory = GetComponent<EquipmentInventory>();
            if (inventory != null) inventory.Collected += OnCollected;
            SelectSkill(0);
            SelectSkillForAssignment(0);
            panel.SetActive(false);
            GameLocalization.Changed += RefreshLanguage;
            RefreshVisibility();
        }

        void Update()
        {
            if (active != this) return;
            RefreshVisibility();
            if (PauseSettingsMenu.IsOpen || IslandMapUI.BlocksInput || PlayerDeathRespawn.IsOpen ||
                movement == null || !movement.enabled || Keyboard.current == null) return;
            var keys = Keyboard.current;
            if (keys.iKey.wasPressedThisFrame) SetOpen(!IsOpen);
            else if (IsOpen && keys.escapeKey.wasPressedThisFrame) { SetOpen(false); consumedFrame = Time.frameCount; }
            if (IsOpen || Cursor.lockState == CursorLockMode.Locked)
            {
                if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) SelectSkill(0);
                if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) SelectSkill(1);
                if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) SelectSkill(2);
                if (keys.digit4Key.wasPressedThisFrame || keys.numpad4Key.wasPressedThisFrame) SelectSkill(3);
            }
        }

        void RefreshVisibility()
        {
            bool gameplay = enabled && movement != null && movement.enabled && gameObject.activeInHierarchy &&
                (health == null || health.currentHealth > 0f) && !PauseSettingsMenu.IsOpen &&
                !IslandMapUI.BlocksInput && !PlayerDeathRespawn.IsOpen && !IsOpen;
            if (hud != null) hud.gameObject.SetActive(gameplay && showEmptySkillHotbar);
            if (loadoutHint != null)
            {
                loadoutHint.gameObject.SetActive(gameplay && hintRemaining > 0f);
                if (gameplay) hintRemaining = Mathf.Max(0f, hintRemaining - Time.unscaledDeltaTime);
            }
            if (pickupNotice != null) pickupNotice.gameObject.SetActive(gameplay && Time.unscaledTime < noticeUntil);
        }

        public void SelectSkill(int index)
        {
            if (index < 0 || index >= 4 || root == null) return;
            SelectedSkill = index;
            for (int i = 0; i < 4; i++) hotbarFrames[i].color = i == index ? accent : GameUITheme.Muted;
            if (PanelVisible) SelectSkillForAssignment(index);
        }

        public void SelectSkillForAssignment(int index)
        {
            if (index < 0 || index >= 4 || details == null) return;
            EditingSkill = index;
            editingEquipment = -1;
            for (int i = 0; i < 4; i++) skillFrames[i].color = i == index ? accent : GameUITheme.Muted;
            details.text = GameLocalization.Format("skill.details", index + 1);
        }

        public void SelectEquipment(EquipmentSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= 5 || details == null) return;
            editingEquipment = index;
            string contents = "";
            if (inventory != null)
                foreach (var pair in inventory.Items)
                    if (pair.Key.slot == slot) contents += "\n" + pair.Key.LocalizedName + "  × " + pair.Value;
            details.text = GameLocalization.Format("gear.details", GameLocalization.Text(equipmentNames[index]),
                contents.Length == 0 ? GameLocalization.Text("gear.none") : GameLocalization.Text("gear.collected") + contents);
        }

        void OnCollected(EquipmentItem item)
        {
            if (pickupNotice != null)
            {
                if (Time.unscaledTime >= noticeUntil) recentPickups.Clear();
                recentPickups.Add(item);
                noticeUntil = Time.unscaledTime + 3f;
            }
            RefreshLanguage();
        }

        void RefreshLanguage()
        {
            if (pickupNotice != null)
            {
                var names = recentPickups.ConvertAll(item => item ? item.LocalizedName : "");
                pickupNotice.text = GameLocalization.Format("gear.pickup", string.Join(GameLocalization.Language == GameLanguage.English ? ", " : "、", names));
            }
            for (int i = 0; i < 5; i++)
            {
                int count = 0;
                if (inventory != null) foreach (var pair in inventory.Items) if ((int)pair.Key.slot == i) count += pair.Value;
                equipmentLabels[i].text = count > 0 ? GameLocalization.Format("gear.count", GameLocalization.Text(equipmentNames[i]), count) :
                    GameLocalization.Format("gear.empty", GameLocalization.Text(equipmentNames[i]));
            }
            if (editingEquipment >= 0) SelectEquipment((EquipmentSlot)editingEquipment);
            else SelectSkillForAssignment(EditingSkill);
        }

        public void SetOpen(bool open)
        {
            if (active != this || panel == null || open == IsOpen || (open && (PauseSettingsMenu.IsOpen || PlayerDeathRespawn.IsOpen || IslandMapUI.BlocksInput))) return;
            IsOpen = open;
            panel.SetActive(open);
            consumedFrame = Time.frameCount;
            if (open)
            {
                hintRemaining = 0f;
                EnsureEventSystem();
                previousTimeScale = Time.timeScale;
                previousCursor = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SelectSkillForAssignment(SelectedSkill);
            }
            else
            {
                if (!PauseSettingsMenu.IsOpen)
                {
                    Time.timeScale = previousTimeScale;
                    Cursor.lockState = previousCursor;
                    Cursor.visible = previousCursorVisible;
                }
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }
            RefreshVisibility();
        }

        void Build()
        {
            root = new GameObject("Sahur Equipment and Skills UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            // Screen-space UI must not inherit Sahur's imported model scale.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var hotbar = Box(root.transform, "Skill hotbar", new Vector2(.5f, 0), new Vector2(0, 84), new Vector2(440, 106), new Color(0, 0, 0, .7f));
            hud = hotbar.gameObject.AddComponent<CanvasGroup>();
            hud.blocksRaycasts = false;
            Label(hotbar, "技能  /  1–4 选择", new Vector2(0, 32), new Vector2(380, 26), 16, Color.white);
            for (int i = 0; i < 4; i++)
            {
                var frame = Box(hotbar, "Skill " + (i + 1), new Vector2(.5f, .5f), new Vector2((i - 1.5f) * 94, -7), new Vector2(80, 56), Color.gray);
                hotbarFrames[i] = frame.GetComponent<Image>();
                Box(frame, "Inner", new Vector2(.5f, .5f), Vector2.zero, new Vector2(76, 52), track);
                Label(frame, (i + 1) + "   空", Vector2.zero, new Vector2(76, 50), 18, Color.white);
            }
            loadoutHint = Label(root.transform, "I  装备 / 技能", new Vector2(-120, 30), new Vector2(220, 30), 18, Color.white, new Vector2(1, 0));
            pickupNotice = Label(root.transform, "", new Vector2(0, 164), new Vector2(1100, 36), 22, accent, new Vector2(.5f, 0));
            panel = Box(root.transform, "Loadout overlay", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, .72f)).gameObject;
            var shade = (RectTransform)panel.transform;
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.sizeDelta = Vector2.zero;
            shade.GetComponent<Image>().raycastTarget = true;
            var window = Box(shade, "Loadout window", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1080, 700), GameUITheme.Surface);
            GameUITheme.Rule(window,new Vector2(0,350),new Vector2(1080,1),GameUITheme.Muted);
            GameUITheme.Rule(window,new Vector2(0,242),new Vector2(968,1),GameUITheme.Track);
            GameUITheme.Rule(window,new Vector2(-196,-8),new Vector2(1,424),GameUITheme.Track);
            Label(window, "SAHUR  /  装备与技能", new Vector2(-245, 285), new Vector2(470, 48), 30, Color.white);
            Button(window, "关闭  [I]", new Vector2(408, 285), new Vector2(150, 42), () => SetOpen(false));
            Label(window, "装备", new Vector2(-345, 210), new Vector2(250, 35), 16, GameUITheme.Secondary);
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                equipmentLabels[i] = Button(window, equipmentNames[i] + "     空", new Vector2(-345, 140 - i * 80), new Vector2(270, 62), () => SelectEquipment((EquipmentSlot)index));
            }
            Label(window, "技能配置  /  四个槽位", new Vector2(105, 210), new Vector2(570, 35), 16, GameUITheme.Secondary);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var frame = Box(window, "Assignment slot " + (i + 1), new Vector2(.5f, .5f), new Vector2(-105 + i * 145, 125), new Vector2(126, 92), Color.gray);
                skillFrames[i] = frame.GetComponent<Image>();
                Button(frame, (i + 1) + "\n未配置", Vector2.zero, new Vector2(122, 88), () => SelectSkillForAssignment(index));
            }
            var info = Box(window, "Available skills and detail", new Vector2(.5f, .5f), new Vector2(110, -80), new Vector2(590, 265), track);
            details = Label(info, "", Vector2.zero, new Vector2(540, 225), 21, Color.white);
            details.alignment = TextAnchor.UpperLeft;
            Label(window, "I / Esc 关闭", new Vector2(0, -304), new Vector2(940, 36), 15, GameUITheme.Secondary);
        }

        RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }

        Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, Vector2? anchor = null)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = fontSize;
            text.text = value; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(12, fontSize - 4); text.resizeTextMaxSize = fontSize;
            LocalizedGameText.Bind(text);
            return text;
        }

        Text Button(Transform parent, string title, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = Box(parent, title, new Vector2(.5f, .5f), position, size, track);
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            GameUITheme.StyleButton(button);
            var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            button.onClick.AddListener(action);
            return Label(rect, title, Vector2.zero, size - new Vector2(8, 4), 22, Color.white);
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var obj = new GameObject("Loadout EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            obj.transform.SetParent(root.transform, false);
        }

        void OnDisable() { if (active == this && IsOpen) SetOpen(false); RefreshVisibility(); }
        void OnDestroy()
        {
            GameLocalization.Changed -= RefreshLanguage;
            if (inventory != null) inventory.Collected -= OnCollected;
            if (active == this) { SetOpen(false); active = null; IsOpen = false; consumedFrame = -1; }
            if (root != null) Destroy(root);
            if (ownsFont && font != null) Destroy(font);
        }
    }
}
