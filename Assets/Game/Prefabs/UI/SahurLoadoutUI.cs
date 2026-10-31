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

        readonly string[] equipmentNames = { "Head", "Body", "Legs", "Feet", "Weapon" };
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
        [SerializeField, HideInInspector] GameObject root;
        GameObject panel;
        CanvasGroup hud;
        Text loadoutHint;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        float hintRemaining = 8f;
        Text details;
        Font font;
        EquipmentJournalInk specimen;
        static readonly Color Ink = new Color(.22f,.17f,.12f);
        static readonly Color Pencil = new Color(.43f,.36f,.27f);
        static readonly Color Rust = new Color(.52f,.24f,.12f);
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
            RefreshLanguage();
            panel.SetActive(false);
            GameLocalization.Changed += RefreshLanguage;
            RefreshVisibility();
        }

        void OnEnable()
        {
            // A script reload retains native canvases but resets managed UI caches.
            if (active != null) return;
            if (root != null) { root.SetActive(false); Destroy(root); }
            Awake();
        }

        void Update()
        {
            if (active != this) return;
            RefreshVisibility();
            if (PauseSettingsMenu.IsOpen || IslandMapUI.BlocksInput || PlayerDeathRespawn.IsOpen ||
                movement == null || !movement.enabled || SecondIslandAwakening.BlocksInputFor(movement) || Keyboard.current == null) return;
            var keys = Keyboard.current;
            if (keys.iKey.wasPressedThisFrame) SetOpen(!IsOpen);
            else if (IsOpen && keys.escapeKey.wasPressedThisFrame) { SetOpen(false); consumedFrame = Time.frameCount; }
            if (IsOpen)
            {
                if (editingEquipment == (int)EquipmentSlot.Weapon)
                {
                    if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) weapons?.TryEquip(SahurWeaponLoadout.Weapon.Stick, true);
                    if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) weapons?.TryEquip(SahurWeaponLoadout.Weapon.CapriTwinBlades, true);
                    if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) weapons?.TryEquip(SahurWeaponLoadout.Weapon.M4A1, true);
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
                !SecondIslandAwakening.BlocksInputFor(movement) &&
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
                weaponHint.gameObject.SetActive(gameplay && !BossHudPanel.HasActiveEncounter);

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
            if(categoryTitle)categoryTitle.text="Skill slots";
            if(specimen)specimen.SetDrawing(EquipmentJournalInk.Drawing.Skill);
            if(itemTitle)itemTitle.text="Skill " + (index+1);
            if(actionText){actionText.text="No skills available";equipButton.interactable=false;}
            if(unequipButton)unequipButton.gameObject.SetActive(false);
            if(selectionStatus)selectionStatus.text="Select a slot for details";
            for(int i=0;i<5;i++)if(categoryFrames[i])SetSelected(categoryFrames[i].GetComponent<Button>(),false);
            for (int i = 0; i < 4; i++) SetSelected(skillFrames[i].GetComponent<Button>(), i == index);
            details.text = GameLocalization.Format("skill.details", index + 1);
        }

        public void SelectEquipment(EquipmentSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= 5 || details == null) return;
            editingEquipment = index;
            itemContent.anchoredPosition=Vector2.zero;
            if(itemBrowser)itemBrowser.SetActive(true);if(skillChoices)skillChoices.SetActive(false);
            if(categoryTitle)categoryTitle.text=equipmentNames[index]+"Gear";
            for(int i=0;i<5;i++)SetSelected(categoryFrames[i].GetComponent<Button>(),i==index);
            foreach(var row in itemRows){row.SetActive(false);Destroy(row);}itemRows.Clear();
            int count=0;
            if(slot==EquipmentSlot.Weapon)
            {
                AddItemRow("Sahur stick",weapons!=null&&!weapons.HasStick?"Weapon lost":weapons!=null&&weapons.CurrentWeapon==SahurWeaponLoadout.Weapon.Stick?"Equipped":"Ready to equip",count++,()=>InspectWeapon(0));
                AddItemRow("Capri twin blades",weapons!=null&&weapons.HasTwinBlades?(weapons.UsesTwinBlades?"Equipped":"Ready to equip"):"Locked · Defeat Capri",count++,()=>InspectWeapon(1));
                AddItemRow("M4A1 assault rifle",weapons!=null&&!weapons.RifleUnlocked?FirstIslandBossProgression.RifleUnlockHint:weapons!=null&&weapons.HasM4A1?(weapons.UsesRifle?"Equipped":"Ready to equip"):"Not collected · Find it on the island",count++,()=>InspectWeapon(2));
                InspectWeapon(selectedWeapon>=0?selectedWeapon:weapons!=null?(int)weapons.CurrentWeapon:0);
            }
            else
            {
                EquipmentItem first=null;bool retainsSelection=false;
                if(inventory!=null)foreach(var pair in inventory.Items)
                    if(pair.Key && pair.Key.slot==slot)
                    {
                        var item=pair.Key;if(first==null)first=item;if(item==selectedItem)retainsSelection=true;
                        var glasses=GetComponent<WeaknessGlasses>();
                        bool isGlasses=item.id=="nailong_weakness_glasses";
                        AddItemRow(item.LocalizedName,isGlasses?(glasses&&glasses.Equipped?"Equipped":"Collected x "+pair.Value):"Collected x "+pair.Value,count++,()=>InspectItem(item));
                    }
                if(slot==EquipmentSlot.Head && first==null)
                    AddItemRow(WeaknessGlasses.DisplayName,"Locked - Defeat Nailong",count++,()=>InspectItem(Resources.Load<EquipmentItem>(WeaknessGlasses.ItemPath)));
                InspectItem(retainsSelection?selectedItem:first);
                if(count==0)itemRows.Add(Label(itemContent,"No gear collected yet\nExplore the island and collect boss drops",new Vector2(0,-80),new Vector2(290,110),18,Pencil,new Vector2(.5f,1)).gameObject);
            }
            itemContent.sizeDelta=new Vector2(0,Mathf.Max(430,count*104));
        }

        void AddItemRow(string title,string status,int index,UnityEngine.Events.UnityAction click)
        {
            var label=Button(itemContent,GameLocalization.Text(title)+"\n"+GameLocalization.Text(status),new Vector2(0,-54-index*104),new Vector2(304,90),click);
            var rect=(RectTransform)label.transform.parent;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
            label.fontSize=19;label.resizeTextMaxSize=19;
            rect.localRotation=Quaternion.Euler(0,0,index%2==0?-.5f:.4f);
            itemRows.Add(rect.gameObject);
        }
        void InspectWeapon(int index)
        {
            selectedWeapon=index;selectedItem=null;
            if(specimen)specimen.SetDrawing(index==2?EquipmentJournalInk.Drawing.Rifle:index==0?EquipmentJournalInk.Drawing.Stick:EquipmentJournalInk.Drawing.Blades);
            bool owned=weapons!=null&&weapons.Owns((SahurWeaponLoadout.Weapon)index);
            bool rifleLocked=index==2&&weapons!=null&&!weapons.RifleUnlocked;
            bool lostStick=index==0 && weapons!=null && !weapons.HasStick;
            bool equipped=owned && weapons!=null && index==(int)weapons.CurrentWeapon;
            itemTitle.text=SahurWeaponLoadout.DisplayName((SahurWeaponLoadout.Weapon)index);
            selectionStatus.text=equipped?"Currently equipped":owned?"Unlocked · Ready to equip":"Locked";
            details.text=index==0?"Wooden club\n\nLMB: three-hit combo\nHold RMB: charged strike (also while moving)\nHold LMB + RMB: two-handed spin\nJump + LMB: overhead chop\nE: throw and recall\n\nWeak points take 1.5x damage.":"Capri twin blades\n\nLMB: twin-blade combo\nHold RMB: X charge while moving; release to rush and cut outward\nPartial charge: lower damage, longer rush (up to 20m)\nFull charge (2.6s): maximum damage, 16m rush\nHold LMB + RMB: spin attack\n"+GameInputSettings.Get(GameInputSettings.Action.Guard).ToString()+": hold to block all frontal attacks\n\nInvulnerable during rush travel. Walls stop the rush.\nGuarding and spinning consume stamina.";
            if(index==2)details.text="M4A1: ranged and anti-air support.\n\nLMB: automatic fire\nHold RMB: aim and zoom while standing or walking\nSprint: carry and fire; aiming is disabled\n30-round magazine - R: reload\nGround damage falls with distance.\nAttack the aircraft's engines during its counter window.\nSupply camps: up to 45 rounds, 45s restock.";
            actionText.text=equipped?"Equipped":owned?"Equip this weapon":index==2?"Collect on the island to unlock":"Defeat Capri to unlock";
            equipButton.interactable=owned&&!equipped;unequipButton.gameObject.SetActive(false);
            if(rifleLocked)
            {
                selectionStatus.text="Locked";
                details.text=FirstIslandBossProgression.RifleUnlockHint+".\n\n"+details.text;
                actionText.text="Defeat "+GameLocalization.Text("enemy.name")+" first";
                equipButton.interactable=false;
            }
            HighlightSelection();
        }
        void InspectItem(EquipmentItem item)
        {
            selectedItem=item;selectedWeapon=-1;
            var glasses=GetComponent<WeaknessGlasses>();bool isGlasses=item&&item.id=="nailong_weakness_glasses";
            var worn=GetComponent<SahurWearableEquipment>();bool wearable=SahurWearableEquipment.CanWear(item);
            bool wearing=worn&&worn.IsEquipped(item);
            if(specimen)specimen.SetDrawing(isGlasses?EquipmentJournalInk.Drawing.Glasses:SlotDrawing(Mathf.Max(0,editingEquipment)));
            bool owned=item&&inventory!=null&&inventory.Count(item)>0;
            itemTitle.text=item?item.LocalizedName:"No gear";
            selectionStatus.text=isGlasses?(glasses&&glasses.Equipped?"Equipped":owned?"Collected":"Locked"):owned?"Collected":item?"Locked":"Collected gear will appear here";
            details.text=isGlasses?"Dropped by Nailong.\n\nEquip, close Gear, then press G to reveal boss weak points.\nDuration: 30 seconds\nCooldown: 15 minutes\n\nTeal markers show weak points. Hit them to deal 1.5x damage.":item?"An island collectible.\n\nThis collectible has no equippable ability yet.":"Select a category on the left to inspect collected items.";
            actionText.text=isGlasses?(owned?(glasses&&glasses.Equipped?"Equipped":"Equip Weakness Glasses"):"Defeat Nailong to unlock"):"Collectible";
            if(isGlasses)unequipText.text="Unequip Glasses";
            equipButton.interactable=isGlasses&&owned&&glasses&&!glasses.Equipped;
            unequipButton.gameObject.SetActive(isGlasses&&glasses&&glasses.Equipped);
            if(wearable)
            {
                selectionStatus.text=wearing?"Equipped":owned?"Collected":"Locked";
                details.text="Dropped by Nailong.\n\nWear this item on Sahur. It follows his movements and remains equipped when you load your save.\n\nAppearance only; your combat stats stay the same.";
                actionText.text=wearing?"Equipped":owned?"Equip on Sahur":"Defeat Nailong to collect";
                equipButton.interactable=owned&&!wearing;
                unequipText.text="Unequip";unequipButton.gameObject.SetActive(wearing);
            }
            if(item && item.id=="second_island_gravity_wristband")
            {
                var gravity=GetComponent<GravityWristbandAbility>();
                selectionStatus.text=owned ? (gravity && gravity.NormalGravity ? "Normal gravity active" : "Low gravity active") : "Locked";
                details.text="Reward for defeating the marshmallows on the second island.\n\nAutomatically worn when collected. Close Gear and press B to switch between normal and low gravity.";
                actionText.text=owned ? "Press B during play" : "Defeat the marshmallows";
                equipButton.interactable=false;unequipButton.gameObject.SetActive(false);
            }
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
            if(!button)return;
            button.image.color=Color.white;
            var colors=button.colors;
            colors.normalColor=selected?new Color(.77f,.56f,.31f,.32f):new Color(.86f,.77f,.59f,.13f);
            colors.selectedColor=colors.normalColor;
            colors.highlightedColor=new Color(.76f,.55f,.30f,.4f);
            colors.pressedColor=new Color(.65f,.43f,.22f,.48f);
            colors.disabledColor=new Color(.66f,.57f,.43f,.16f);
            colors.fadeDuration=.1f; button.colors=colors;
            var line=button.transform.Find("Pen outline")?.GetComponent<EquipmentJournalInk>();
            if(line)line.color=selected?Rust:new Color(Ink.r,Ink.g,Ink.b,.4f);
            foreach(var caption in button.GetComponentsInChildren<Text>())caption.color=selected?Rust:Ink;
        }
        public void EquipSelection()
        {
            if(!IsOpen)return;
            if(selectedWeapon>=0)weapons?.TryEquip((SahurWeaponLoadout.Weapon)selectedWeapon,true);
            else if(selectedItem&&selectedItem.id=="nailong_weakness_glasses")GetComponent<WeaknessGlasses>()?.Equip();
            else if(selectedItem)GetComponent<SahurWearableEquipment>()?.Equip(selectedItem);
            RefreshLanguage();
        }
        public void UnequipSelection(){if(!IsOpen)return;if(selectedItem&&selectedItem.id!="nailong_weakness_glasses")GetComponent<SahurWearableEquipment>()?.Unequip(selectedItem.slot);else GetComponent<WeaknessGlasses>()?.Unequip();RefreshLanguage();}

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
                pickupNotice.text = GameLocalization.Format("gear.pickup", string.Join(GameLocalization.Language == GameLanguage.English ? ", " : ", ", names));
            }
            for (int i = 0; i < 5; i++)
            {
                int count = 0;
                if (inventory != null) foreach (var pair in inventory.Items) if ((int)pair.Key.slot == i) count += pair.Value;
                equipmentLabels[i].text = count > 0 ? GameLocalization.Format("gear.count", GameLocalization.Text(equipmentNames[i]), count) :
                    GameLocalization.Format("gear.empty", GameLocalization.Text(equipmentNames[i]));
            }
            if(weapons!=null)equipmentLabels[(int)EquipmentSlot.Weapon].text=GameLocalization.Text("Weapon")+"\n"+weapons.ShortName;
            var glasses=GetComponent<WeaknessGlasses>();
            if(glasses && glasses.Equipped)equipmentLabels[(int)EquipmentSlot.Head].text=GameLocalization.Text("Head")+"\n"+GameLocalization.Text("Equipped");
            if(weaponHint && weapons!=null)
            {
                string locked=GameLocalization.Text("Locked"),missing=GameLocalization.Text("Not collected");
                weaponHint.text=(weapons.WeaponMissing?GameLocalization.Text("Weapon lost"):GameLocalization.Text("Current weapon")+"  "+weapons.WeaponName)+"\n[1] "+SahurWeaponLoadout.DisplayName(SahurWeaponLoadout.Weapon.Stick)+(weapons.HasStick?"":" · "+GameLocalization.Text("Weapon lost"))+
                    "\n[2] "+SahurWeaponLoadout.DisplayName(SahurWeaponLoadout.Weapon.CapriTwinBlades)+(weapons.HasTwinBlades?"":" · "+locked)+
                    "\n[3] "+SahurWeaponLoadout.DisplayName(SahurWeaponLoadout.Weapon.M4A1)+(!weapons.RifleUnlocked?" · "+locked:weapons.HasM4A1?"":" · "+missing);
            }
            if (editingEquipment >= 0) SelectEquipment((EquipmentSlot)editingEquipment);
            else SelectSkillForAssignment(EditingSkill);
        }

        public void SetOpen(bool open)
        {
            if (active != this || panel == null || open == IsOpen || (open && (PauseSettingsMenu.IsOpen || PlayerDeathRespawn.IsOpen || IslandMapUI.BlocksInput || SecondIslandAwakening.BlocksInputFor(movement)))) return;
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
                RefreshLanguage();
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
            // Keep arrival captions and boss HUDs beneath the modal paper overlay.
            canvas.sortingOrder = 910;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var hotbar = Box(root.transform, "Skill hotbar", new Vector2(.5f, 0), new Vector2(0, 84), new Vector2(440, 106), new Color(0, 0, 0, .7f));
            hud = hotbar.gameObject.AddComponent<CanvasGroup>();
            hud.blocksRaycasts = false;
            Label(hotbar, "Skills  /  1–4 to select", new Vector2(0, 32), new Vector2(380, 26), 16, Color.white);
            for (int i = 0; i < 4; i++)
            {
                var frame = Box(hotbar, "Skill " + (i + 1), new Vector2(.5f, .5f), new Vector2((i - 1.5f) * 94, -7), new Vector2(80, 56), Color.gray);
                hotbarFrames[i] = frame.GetComponent<Image>();
                Box(frame, "Inner", new Vector2(.5f, .5f), Vector2.zero, new Vector2(76, 52), track);
                Label(frame, (i + 1) + "   Empty", Vector2.zero, new Vector2(76, 50), 18, Color.white);
            }
            loadoutHint = Label(root.transform, "I  Gear / Skills", new Vector2(-120, 30), new Vector2(220, 30), 18, Color.white, new Vector2(1, 0));
            weaponHint = Label(root.transform, "", new Vector2(278, 226), new Vector2(500, 128), 18, GameUITheme.Foreground, new Vector2(0,0));
            weaponHint.alignment = TextAnchor.MiddleLeft;
            pickupNotice = Label(root.transform, "", new Vector2(0, 164), new Vector2(1100, 36), 22, accent, new Vector2(.5f, 0));
            panel = Box(root.transform, "Loadout overlay", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, .72f)).gameObject;
            var shade = (RectTransform)panel.transform;
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.sizeDelta = Vector2.zero;
            shade.GetComponent<Image>().raycastTarget = true;
            var window = Box(shade, "Equipment sketchbook", new Vector2(.5f,.5f),Vector2.zero,new Vector2(1380,920),Color.white);
            window.GetComponent<Image>().sprite=Resources.Load<Sprite>("EquipmentJournal/Sketchbook");
            var heading=Label(window,"SAHUR / FIELD NOTES",new Vector2(-352,355),new Vector2(510,55),34,Ink);
            heading.alignment=TextAnchor.MiddleLeft;
            heading.rectTransform.localRotation=Quaternion.Euler(0,0,-1);
            Pen(window,EquipmentJournalInk.Drawing.Rule,new Vector2(-327,318),new Vector2(570,7),Ink);
            var note=Label(window,"Equipment & island finds",new Vector2(-355,285),new Vector2(510,28),16,Pencil);
            note.alignment=TextAnchor.MiddleLeft;
            Button(window,"Close ×",new Vector2(458,-391),new Vector2(150,42),()=>SetOpen(false));
            Label(window,"Gear slots",new Vector2(-516,229),new Vector2(210,30),17,Pencil);
            for(int i=0;i<5;i++)
            {
                int index=i;
                equipmentLabels[i]=Button(window,equipmentNames[i]+"     Empty",new Vector2(-516,164-i*76),new Vector2(214,64),()=>SelectEquipment((EquipmentSlot)index));
                var label=equipmentLabels[i];
                label.rectTransform.anchoredPosition=new Vector2(23,0);label.rectTransform.sizeDelta=new Vector2(157,58);
                label.fontSize=18;label.resizeTextMaxSize=18;
                categoryFrames[i]=label.transform.parent.GetComponent<Image>();
                Pen(label.transform.parent,SlotDrawing(i),new Vector2(-81,0),new Vector2(34,40),Ink);
            }
            Button(window,"Skill slots >",new Vector2(-216,-295),new Vector2(304,46),()=>SelectSkillForAssignment(EditingSkill));
            categoryTitle=Label(window,"Weapons",new Vector2(-216,229),new Vector2(320,30),19,Ink);
            Pen(window,EquipmentJournalInk.Drawing.Rule,new Vector2(-216,205),new Vector2(290,4),Pencil);
            itemBrowser=new GameObject("Equipment browser",typeof(RectTransform));itemBrowser.transform.SetParent(window,false);
            var viewport=Box(itemBrowser.transform,"Scrollable item list",new Vector2(.5f,.5f),new Vector2(-216,-37),new Vector2(330,470),Color.clear);
            viewport.GetComponent<Image>().raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var contentObject=new GameObject("Item cards",typeof(RectTransform));contentObject.transform.SetParent(viewport,false);itemContent=(RectTransform)contentObject.transform;
            itemContent.anchorMin=new Vector2(0,1);itemContent.anchorMax=new Vector2(1,1);itemContent.pivot=new Vector2(.5f,1);itemContent.sizeDelta=new Vector2(0,430);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=itemContent;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;
            skillChoices=new GameObject("Skill choices",typeof(RectTransform));skillChoices.transform.SetParent(window,false);
            for(int i=0;i<4;i++)
            {
                int index=i;
                var label=Button(skillChoices.transform,"Skill "+(i+1)+" / Unassigned",new Vector2(-216,145-i*105),new Vector2(304,88),()=>SelectSkillForAssignment(index));
                skillFrames[i]=label.transform.parent.GetComponent<Image>();
            }
            itemTitle=Label(window,"",new Vector2(333,288),new Vector2(470,50),29,Ink);
            selectionStatus=Label(window,"",new Vector2(333,246),new Vector2(470,28),16,Rust);
            specimen=Pen(window,EquipmentJournalInk.Drawing.Stick,new Vector2(333,133),new Vector2(210,175),Ink);
            Pen(window,EquipmentJournalInk.Drawing.Rule,new Vector2(333,26),new Vector2(450,6),Pencil);
            details=Label(window,"",new Vector2(333,-124),new Vector2(450,267),20,Ink);
            details.alignment=TextAnchor.UpperLeft;
            actionText=Button(window,"Gear",new Vector2(333,-290),new Vector2(450,50),EquipSelection);equipButton=actionText.transform.parent.GetComponent<Button>();
            unequipText=Button(window,"Unequip Glasses",new Vector2(333,-345),new Vector2(450,40),UnequipSelection);unequipButton=unequipText.transform.parent.GetComponent<Button>();
            Label(window,"I / Esc to close",new Vector2(-126,-387),new Vector2(220,30),15,Pencil);
        }

        static EquipmentJournalInk.Drawing SlotDrawing(int slot)
        {
            switch(slot)
            {
                case 0:return EquipmentJournalInk.Drawing.Head;
                case 1:return EquipmentJournalInk.Drawing.Coat;
                case 2:return EquipmentJournalInk.Drawing.Trousers;
                case 3:return EquipmentJournalInk.Drawing.Boots;
                default:return EquipmentJournalInk.Drawing.Stick;
            }
        }

        EquipmentJournalInk Pen(Transform parent,EquipmentJournalInk.Drawing drawing,Vector2 position,Vector2 size,Color color)
        {
            var obj=new GameObject(drawing==EquipmentJournalInk.Drawing.Border?"Pen outline":"Ink sketch",typeof(RectTransform),typeof(CanvasRenderer),typeof(EquipmentJournalInk));
            obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchoredPosition=position;rect.sizeDelta=size;
            var pen=obj.GetComponent<EquipmentJournalInk>();pen.raycastTarget=false;pen.color=color;pen.SetDrawing(drawing);
            return pen;
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
            Pen(rect,EquipmentJournalInk.Drawing.Border,Vector2.zero,size-new Vector2(3,3),new Color(Ink.r,Ink.g,Ink.b,.4f));
            SetSelected(button,false);
            var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            button.onClick.AddListener(action);
            return Label(rect, title, Vector2.zero, size - new Vector2(20, 6), 20, Ink);
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
