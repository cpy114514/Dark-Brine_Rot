using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Mavis
{
    /// <summary>Mouse-driven equipment browser and loadout management.</summary>
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
        SahurWeaponLoadout weapons;
        Text weaponHint;
        RectTransform itemContent;
        GameObject itemBrowser, skillChoices;
        Text categoryTitle, itemTitle, actionText, unequipText, selectionStatus;
        Button equipButton, unequipButton;
        readonly Image[] categoryFrames=new Image[5];
        readonly System.Collections.Generic.List<GameObject> itemRows=new System.Collections.Generic.List<GameObject>();
        int selectedWeapon=-1;
        EquipmentItem selectedItem;
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
            font = GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            movement = GetComponent<ThirdPersonPlayerController>();
            health = GetComponent<PlayerHealth>();
            inventory = GetComponent<EquipmentInventory>();
            weapons = GetComponent<SahurWeaponLoadout>();
            if (weapons != null) weapons.Changed += RefreshLanguage;
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
            if (IsOpen)
            {
                if (editingEquipment == (int)EquipmentSlot.Weapon)
                {
                    if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) weapons?.TryEquip(SahurWeaponLoadout.Weapon.Stick, true);
                    if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) weapons?.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades, true);
                    return;
                }
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
            if (weaponHint != null)
            {
                weaponHint.gameObject.SetActive(gameplay);
                weaponHint.text = weapons != null ? weapons.WeaponName + "\n[1] STICK   /   [2] " + (weapons.HasTwinBlades ? "TWIN BLADES" : "TWIN BLADES LOCKED") + "   /   [I] GEAR" : "";
            }
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
            if(itemBrowser)itemBrowser.SetActive(false);
            if(skillChoices)skillChoices.SetActive(true);
            if(categoryTitle)categoryTitle.text="技能槽位";
            if(itemTitle)itemTitle.text="技能 " + (index+1);
            if(actionText){actionText.text="暂无可配置技能";equipButton.interactable=false;}
            if(unequipButton)unequipButton.gameObject.SetActive(false);
            if(selectionStatus)selectionStatus.text="选择槽位查看说明";
            for(int i=0;i<5;i++)if(categoryFrames[i])SetSelected(categoryFrames[i].GetComponent<Button>(),false);
            for (int i = 0; i < 4; i++) skillFrames[i].color = i == index ? accent : GameUITheme.Muted;
            details.text = GameLocalization.Format("skill.details", index + 1);
        }

        public void SelectEquipment(EquipmentSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= 5 || details == null) return;
            editingEquipment = index;
            itemContent.anchoredPosition=Vector2.zero;
            if(itemBrowser)itemBrowser.SetActive(true);if(skillChoices)skillChoices.SetActive(false);
            if(categoryTitle)categoryTitle.text=equipmentNames[index]+"装备";
            for(int i=0;i<5;i++)SetSelected(categoryFrames[i].GetComponent<Button>(),i==index);
            foreach(var row in itemRows){row.SetActive(false);Destroy(row);}itemRows.Clear();
            int count=0;
            if(slot==EquipmentSlot.Weapon)
            {
                AddItemRow("萨胡尔木棍",weapons!=null&&!weapons.UsesTwinBlades?"已装备":"可装备",count++,()=>InspectWeapon(0));
                AddItemRow("卡布奇诺双刀",weapons!=null&&weapons.HasTwinBlades?(weapons.UsesTwinBlades?"已装备":"可装备"):"未解锁 · 击败卡布奇诺",count++,()=>InspectWeapon(1));
                InspectWeapon(selectedWeapon>=0?selectedWeapon:weapons!=null&&weapons.UsesTwinBlades?1:0);
            }
            else
            {
                EquipmentItem first=null;bool retainsSelection=false;
                if(inventory!=null)foreach(var pair in inventory.Items)
                    if(pair.Key && pair.Key.slot==slot)
                    {
                        var item=pair.Key;if(first==null)first=item;if(item==selectedItem)retainsSelection=true;
                        var glasses=GetComponent<WeaknessGlasses>();
                        AddItemRow(item.LocalizedName,item.id=="nailong_weakness_glasses"&&glasses&&glasses.Equipped?"已装备":"已收集 × "+pair.Value,count++,()=>InspectItem(item));
                    }
                if(slot==EquipmentSlot.Head && first==null)
                    AddItemRow("弱点眼镜","未解锁 · 击败奶龙",count++,()=>InspectItem(Resources.Load<EquipmentItem>(WeaknessGlasses.ItemPath)));
                InspectItem(retainsSelection?selectedItem:first);
                if(count==0)itemRows.Add(Label(itemContent,"这里还没有装备\n探索岛屿，收集 Boss 掉落",new Vector2(0,-80),new Vector2(290,110),18,GameUITheme.Secondary,new Vector2(.5f,1)).gameObject);
            }
            itemContent.sizeDelta=new Vector2(0,Mathf.Max(430,count*104));
        }

        void AddItemRow(string title,string status,int index,UnityEngine.Events.UnityAction click)
        {
            var label=Button(itemContent,title+"\n"+status,new Vector2(0,-54-index*104),new Vector2(304,90),click);
            var rect=(RectTransform)label.transform.parent;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
            label.fontSize=20;label.resizeTextMaxSize=20;itemRows.Add(rect.gameObject);
        }
        void InspectWeapon(int index)
        {
            selectedWeapon=index;selectedItem=null;
            bool owned=index==0 || weapons!=null&&weapons.HasTwinBlades;
            bool equipped=weapons!=null && (index==1)==weapons.UsesTwinBlades;
            itemTitle.text=index==0?"萨胡尔木棍":"卡布奇诺双刀";
            selectionStatus.text=equipped?"当前已装备":owned?"已解锁 · 可装备":"尚未解锁";
            details.text=index==0?"熟悉的三连击木棍。\n\n左键：三连击\n按住右键：蓄力攻击\n\n命中 Boss 的弱点部位造成 1.5 倍伤害。":"击败卡布奇诺并拾取双刀后解锁。\n\n左键：双刀连击\n按住右键：蓄力攻击\n\n点击下方按钮切换武器。";
            actionText.text=equipped?"已装备":owned?"装备这件武器":"击败卡布奇诺后解锁";
            equipButton.interactable=owned&&!equipped;unequipButton.gameObject.SetActive(false);
            HighlightSelection();
        }
        void InspectItem(EquipmentItem item)
        {
            selectedItem=item;selectedWeapon=-1;
            var glasses=GetComponent<WeaknessGlasses>();bool isGlasses=item&&item.id=="nailong_weakness_glasses";
            bool owned=item&&inventory!=null&&inventory.Count(item)>0;
            itemTitle.text=item?item.LocalizedName:"暂无装备";
            selectionStatus.text=isGlasses&&glasses&&glasses.Equipped?"当前已装备":owned?"已收集":item?"尚未解锁":"收集装备后会显示在这里";
            details.text=isGlasses?"奶龙掉落的弱点眼镜。\n\n装备后，关闭装备栏按 G 开启弱点显示。\n持续 30 秒，冷却 15 分钟。\n\n弱点以青绿色标记显示；命中该部位造成 1.5 倍伤害。":item?"已收集的岛屿掉落物。\n\n这件收藏道具目前没有可装备的能力。":"选择左侧分类，查看已收集的物品。";
            actionText.text=isGlasses?(owned?(glasses&&glasses.Equipped?"已装备":"装备弱点眼镜"):"击败奶龙后解锁"):"收藏道具";
            equipButton.interactable=isGlasses&&owned&&glasses&&!glasses.Equipped;
            unequipButton.gameObject.SetActive(isGlasses&&glasses&&glasses.Equipped);
            HighlightSelection();
        }
        void HighlightSelection()
        {
            foreach(var row in itemRows)
            {
                var image=row.GetComponent<Image>();var label=row.GetComponentInChildren<Text>();
                if(image&&label)SetSelected(row.GetComponent<Button>(),label.text.StartsWith(itemTitle.text));
            }
        }
        void SetSelected(Button button,bool selected)
        {
            if(!button)return;button.image.color=Color.white;var colors=button.colors;
            colors.normalColor=selected?new Color(.16f,.31f,.33f):GameUITheme.Gray(.08f);
            colors.selectedColor=colors.normalColor;colors.highlightedColor=selected?new Color(.22f,.4f,.42f):GameUITheme.Gray(.22f);button.colors=colors;
        }
        public void EquipSelection()
        {
            if(!IsOpen)return;
            if(selectedWeapon>=0)weapons?.TryEquip((SahurWeaponLoadout.Weapon)selectedWeapon,true);
            else if(selectedItem&&selectedItem.id=="nailong_weakness_glasses")GetComponent<WeaknessGlasses>()?.Equip();
            RefreshLanguage();
        }
        public void UnequipSelection(){if(!IsOpen)return;GetComponent<WeaknessGlasses>()?.Unequip();RefreshLanguage();}

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
            if(weapons!=null)equipmentLabels[(int)EquipmentSlot.Weapon].text=GameLocalization.Text("武器")+"\n"+(weapons.UsesTwinBlades?"卡布奇诺双刀":"萨胡尔木棍");
            var glasses=GetComponent<WeaknessGlasses>();
            if(glasses && glasses.Equipped)equipmentLabels[(int)EquipmentSlot.Head].text=GameLocalization.Text("头部")+"\n弱点眼镜 · 已装备";
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
                SelectEquipment(EquipmentSlot.Weapon);
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
            weaponHint = Label(root.transform, "", new Vector2(278, 214), new Vector2(500, 58), 16, GameUITheme.Secondary, new Vector2(0,0));
            weaponHint.alignment = TextAnchor.MiddleLeft;
            pickupNotice = Label(root.transform, "", new Vector2(0, 164), new Vector2(1100, 36), 22, accent, new Vector2(.5f, 0));
            panel = Box(root.transform, "Loadout overlay", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, .72f)).gameObject;
            var shade = (RectTransform)panel.transform;
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.sizeDelta = Vector2.zero;
            shade.GetComponent<Image>().raycastTarget = true;
            var window = Box(shade, "Loadout window", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1180, 760), GameUITheme.Surface);
            GameUITheme.Rule(window,new Vector2(0,278),new Vector2(1080,1),GameUITheme.Track);
            GameUITheme.Rule(window,new Vector2(-330,-16),new Vector2(1,518),GameUITheme.Track);
            GameUITheme.Rule(window,new Vector2(82,-16),new Vector2(1,518),GameUITheme.Track);
            var heading=Label(window, "SAHUR  /  装备栏", new Vector2(-270, 327), new Vector2(520, 48), 32, Color.white);heading.alignment=TextAnchor.MiddleLeft;
            Button(window, "关闭  ×", new Vector2(480, 327), new Vector2(130, 44), () => SetOpen(false));
            Label(window, "装备部位", new Vector2(-456, 235), new Vector2(220, 30), 16, GameUITheme.Secondary);
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                equipmentLabels[i] = Button(window, equipmentNames[i] + "     空", new Vector2(-456, 165 - i * 78), new Vector2(214, 62), () => SelectEquipment((EquipmentSlot)index));
                categoryFrames[i]=equipmentLabels[i].transform.parent.GetComponent<Image>();
            }
            Button(window,"技能槽位  ›",new Vector2(-456,-247),new Vector2(214,48),()=>SelectSkillForAssignment(EditingSkill));
            categoryTitle=Label(window,"武器装备",new Vector2(-118,235),new Vector2(330,30),18,GameUITheme.Secondary);
            itemBrowser=new GameObject("Equipment browser",typeof(RectTransform));itemBrowser.transform.SetParent(window,false);
            var viewport=Box(itemBrowser.transform,"Scrollable item list",new Vector2(.5f,.5f),new Vector2(-118,-32),new Vector2(330,470),new Color(.055f,.07f,.08f));
            viewport.GetComponent<Image>().raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var contentObject=new GameObject("Item cards",typeof(RectTransform));contentObject.transform.SetParent(viewport,false);itemContent=(RectTransform)contentObject.transform;
            itemContent.anchorMin=new Vector2(0,1);itemContent.anchorMax=new Vector2(1,1);itemContent.pivot=new Vector2(.5f,1);itemContent.sizeDelta=new Vector2(0,430);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=itemContent;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;
            skillChoices=new GameObject("Skill choices",typeof(RectTransform));skillChoices.transform.SetParent(window,false);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var frame = Box(skillChoices.transform, "Assignment slot " + (i + 1), new Vector2(.5f, .5f), new Vector2(-118,155-i*105), new Vector2(306,90), Color.gray);
                skillFrames[i] = frame.GetComponent<Image>();
                Button(frame, "技能 " + (i + 1) + "    /    未配置", Vector2.zero, new Vector2(302, 86), () => SelectSkillForAssignment(index));
            }
            itemTitle=Label(window,"",new Vector2(330,218),new Vector2(400,48),28,Color.white);itemTitle.alignment=TextAnchor.MiddleLeft;
            selectionStatus=Label(window,"",new Vector2(330,174),new Vector2(400,30),16,new Color(.45f,.85f,.8f));selectionStatus.alignment=TextAnchor.MiddleLeft;
            var info = Box(window, "Item description", new Vector2(.5f, .5f), new Vector2(330,-43), new Vector2(410,350), track);
            details = Label(info, "", Vector2.zero, new Vector2(370, 310), 21, Color.white);
            details.alignment = TextAnchor.UpperLeft;
            actionText=Button(window,"装备",new Vector2(330,-261),new Vector2(410,52),EquipSelection);equipButton=actionText.transform.parent.GetComponent<Button>();
            unequipText=Button(window,"卸下眼镜",new Vector2(330,-319),new Vector2(410,40),UnequipSelection);unequipButton=unequipText.transform.parent.GetComponent<Button>();
            Label(window, "鼠标选择分类与物品  ·  点击按钮装备  ·  I / Esc 关闭", new Vector2(-155, -342), new Vector2(760, 32), 15, GameUITheme.Secondary);
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
            if (weapons != null) weapons.Changed -= RefreshLanguage;
            if (active == this) { SetOpen(false); active = null; IsOpen = false; consumedFrame = -1; }
            if (root != null) Destroy(root);
        }
    }
}
