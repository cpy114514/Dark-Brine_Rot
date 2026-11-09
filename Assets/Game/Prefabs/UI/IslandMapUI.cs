using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Mavis
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-150)]
    public sealed class IslandMapUI : MonoBehaviour
    {
        static IslandMapUI active;
        static int consumedFrame = -1;
        public static bool IsOpen => active != null && active.PanelVisible;
        public static bool BlocksInput => IsOpen || consumedFrame == Time.frameCount;
        public bool PanelVisible => root != null && root.activeSelf;
        public IslandMapGraphic MapGraphic { get; private set; }
        [SerializeField, HideInInspector] GameObject root;
        [SerializeField, HideInInspector] Font font;
        [SerializeField, HideInInspector] bool ownsFont;
        float previousTimeScale;
        CursorLockMode previousCursor;
        bool previousVisible;
        Text playerArrow, coordinates;
        Camera suspendedCamera;
        UniversalAdditionalCameraData cameraData;
        int worldMask;
        bool worldShadows,worldPost;
        CameraOverrideOption worldDepth,worldColor;
        Vector3 shownCoordinates=new Vector3(float.NaN,float.NaN,float.NaN);
        GameLanguage coordinateLanguage;
        readonly List<Text> markers = new List<Text>();
        readonly List<Transform> targets = new List<Transform>();
        GravityWristbandReward wristband;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { active = null; consumedFrame = -1; }
        void OnEnable()
        {
            // Rebuild lazily after reload; keep ownership of the previous native canvas.
            if (root == null || MapGraphic != null) return;
            root.SetActive(false); Destroy(root); root = null;
            if (ownsFont && font != null) Destroy(font);
            font = null; ownsFont = false;
            markers.Clear(); targets.Clear();
        }
        void Update()
        {
            if (PlayerDeathRespawn.IsOpen) { if (PanelVisible) SetOpen(false); return; }
            if (GameControls.Down(GameControls.Command.Map)) SetOpen(!PanelVisible);
            else if (PanelVisible && GameControls.Down(GameControls.Command.Cancel)) SetOpen(false);
            if (PanelVisible) RefreshMarkers();
        }
        public bool SetOpen(bool open)
        {
            GameControls.ConsumeMenuInput();
            if (open == PanelVisible) return false;
            if (open && MarshmallowHollowEntrance.Contains(GetComponent<ThirdPersonPlayerController>())) return false;
            if (open && consumedFrame == Time.frameCount) return false;
            if (open && (CandyHuntQuest.StyleSelectorOpen || PlayerDeathRespawn.IsOpen || PauseSettingsMenu.IsOpen || SahurLoadoutUI.IsOpen || SecondIslandAwakening.BlocksInputFor(GetComponent<ThirdPersonPlayerController>()) || (active != null && active != this))) return false;
            if (open)
            {
                if (root == null) Build();
                active = this;
                previousTimeScale = Time.timeScale;
                previousCursor = Cursor.lockState;
                previousVisible = Cursor.visible;
                root.SetActive(true);
                // Ensure RectTransform layout is ready before computing marker positions.
                Canvas.ForceUpdateCanvases();
                MapGraphic.Refresh(transform.position);
                RebuildMarkers();
                RefreshMarkers();
                SuspendWorldRendering();
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (EventSystem.current == null) new GameObject("Map EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
            else
            {
                root.SetActive(false);
                RestoreWorldRendering();
                if (active == this)
                {
                    active = null;
                    Time.timeScale = previousTimeScale;
                    Cursor.lockState = previousCursor;
                    Cursor.visible = previousVisible;
                }
            }
            consumedFrame = Time.frameCount;
            return true;
        }
        void RebuildMarkers()
        {
            foreach (var marker in markers) if (marker != null) { marker.gameObject.SetActive(false); Destroy(marker.gameObject); }
            markers.Clear(); targets.Clear();
            wristband = null;
            if (gameObject.scene.path == "Assets/Scenes/Second Island/Second Island.unity")
            {
                foreach (var fairy in FindObjectsByType<NealFairyHover>(FindObjectsSortMode.None))
                    if (fairy.gameObject.scene == gameObject.scene)
                    {
                        var quest=fairy.GetComponentInParent<CandyHuntQuest>();
                        if(quest && quest.fairy==fairy.transform && !quest.Started)
                            AddMarker(fairy.transform,"!",new Color(1f,.86f,.53f),30);
                        else AddMarker(fairy.transform, "✦", new Color(1f, .88f, .98f));
                    }
                var cottage=FindFirstObjectByType<CandyHouseMarker>();
                if(cottage && cottage.gameObject.scene==gameObject.scene)
                    AddMarker(cottage.transform,"H",new Color(1f,.67f,.78f),26);
                var rally=FindFirstObjectByType<SkyboardRingRally>();
                if(rally && rally.StartMarker)AddMarker(rally.StartMarker,"S",new Color(1f,.85f,.35f),26);
                var melody=FindFirstObjectByType<CandyMelodyGarden>();
                if(melody && melody.MapAnchor)AddMarker(melody.MapAnchor,"♪",new Color(.69f,1f,.82f),30);
                var bounce=FindFirstObjectByType<CandyBounceGarden>();
                if(bounce && bounce.MapAnchor)AddMarker(bounce.MapAnchor,"★",new Color(1f,.78f,.32f),28);
                var friends=FindFirstObjectByType<MarshmallowFriendsQuest>();
                if(friends && friends.MapAnchor)AddMarker(friends.MapAnchor,"F",new Color(.65f,.93f,1f),26);
                var zhuBaJie=FindFirstObjectByType<ZhuBaJieBoss>();
                if(zhuBaJie && zhuBaJie.Health && !zhuBaJie.Health.IsDead)AddMarker(zhuBaJie.transform,"Z",new Color(1f,.42f,.32f),28);
                var huangMei=FindFirstObjectByType<HuangMeiBoss>();
                if(huangMei && huangMei.Health && !huangMei.Health.IsDead)AddMarker(huangMei.transform,"Y",new Color(1f,.73f,.29f),28);
                var reward=FindFirstObjectByType<GravityWristbandReward>();
                wristband=reward;
                if(reward && reward.enemyRoot)
                    for(int i=0;i<reward.CampCount;i++)if(reward.CampMapAnchor(i))AddMarker(reward.CampMapAnchor(i),reward.IsMainCamp(i)?"◆":"●",new Color(1f,.7f,.25f),18);
                var objective=GetComponent<SecondIslandObjectives>();
                if(objective && objective.CurrentTarget && !objective.CurrentTarget.GetComponent<SecondIslandRoninCamp>())
                    AddMarker(objective.CurrentTarget,"◆",new Color(.45f,1f,.92f));
                var hunt=FindFirstObjectByType<CandyHuntQuest>();
                if(hunt && hunt.NextTarget)AddMarker(hunt.NextTarget,"●",new Color(1f,.42f,.7f),24);
            }
            foreach (var point in FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None))
                if (!point.GetComponent<SecondIslandRoninCamp>()) AddMarker(point.transform, "◆", GameUITheme.Foreground);
            foreach (var enemy in FindObjectsByType<NailongHealth>(FindObjectsSortMode.None))
                if (!enemy.IsDead) AddMarker(enemy.transform, "N", GameUITheme.Foreground);
            foreach (var enemy in FindObjectsByType<CappuccinoAI>(FindObjectsSortMode.None))
                if (enemy.GetComponent<Health>() && !enemy.GetComponent<Health>().IsDead) AddMarker(enemy.transform, "C", GameUITheme.Foreground);
            foreach (var enemy in FindObjectsByType<BombardinoBoss>(FindObjectsSortMode.None))
                if (!enemy.IsDead) AddMarker(enemy.transform, "B", GameUITheme.Foreground);
            var flock=ChickenBroEncounter.Active;
            if(flock && flock.RequiredOnIsland && flock.TryGetGuidanceTarget(out _))AddMarker(flock.transform,"F",new Color(1f,.7f,.25f));
            foreach (var gun in FindObjectsByType<PlacedEquipmentPickup>(FindObjectsSortMode.None))
                if (gun.isActiveAndEnabled && !gun.IsCollected) AddMarker(gun.transform, "+", new Color(1, .83f, .2f));
            foreach (var cache in FindObjectsByType<IslandSupplyCache>(FindObjectsSortMode.None))
                AddMarker(cache.transform, cache.supportsMelee ? "R" : "▣", new Color(1, .83f, .2f));
            playerArrow.transform.SetAsLastSibling();
        }
        void AddMarker(Transform target, string symbol, Color color, int size=28)
        {
            var label = Label(MapGraphic.transform, symbol, size, Vector2.zero, new Vector2(42, 42), color);
            GameUITheme.OutlineSymbol(label);
            targets.Add(target); markers.Add(label);
        }
        void RefreshMarkers()
        {
            playerArrow.rectTransform.anchoredPosition = MapGraphic.WorldToMap(transform.position);
            playerArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -transform.eulerAngles.y);
            var friends=gameObject.scene.path=="Assets/Scenes/Second Island/Second Island.unity" ? FindFirstObjectByType<MarshmallowFriendsQuest>() : null;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if(friends && target && (target==friends.Home || target==friends.Lost))target=targets[i]=friends.MapAnchor;
                bool visible = target != null && target.gameObject.activeInHierarchy;
                if (visible && target.TryGetComponent<NailongHealth>(out var health)) visible = !health.IsDead;
                if (visible && target.TryGetComponent<Health>(out var generic)) visible = !generic.IsDead;
                if (visible && target.TryGetComponent<IslandSupplyCache>(out var cache))
                    markers[i].color = cache.IsAvailable ? new Color(1, .83f, .2f) : GameUITheme.Muted;
                if (visible && wristband && target.name=="Camp map anchor" && target.parent && target.parent.parent==wristband.enemyRoot)
                {
                    int camp=target.parent.GetSiblingIndex();
                    markers[i].color = wristband.CampCleared(camp) ? GameUITheme.Muted :
                        camp==wristband.NextMainCamp ? new Color(.56f,.96f,.80f) : new Color(1f,.7f,.25f);
                }
                if(visible)
                {
                    bool completed=target.TryGetComponent<CandyBounceGarden>(out var b) && b.RewardClaimed ||
                        target.GetComponentInParent<SkyboardRingRally>() is SkyboardRingRally rally && rally.RewardClaimed ||
                        target.GetComponentInParent<CandyHuntQuest>() is CandyHuntQuest hunt && hunt.fairy==target && hunt.RewardClaimed || friends && target==friends.MapAnchor && friends.Stage==3;
                    if(completed){var text=markers[i];text.text="✓";text.color=new Color(.55f,1f,.72f);}
                }
                markers[i].gameObject.SetActive(visible);
                if (visible)
                {
                    var melody = target.GetComponentInParent<CandyMelodyGarden>();
                    bool melodyEntrance = melody && target == melody.MapAnchor;
                    if (melodyEntrance)
                    {
                        var text = markers[i].GetComponent<Text>();
                        text.text = melody.RewardClaimed ? "♪✓" : "♪";
                        text.fontSize = melody.RewardClaimed ? 23 : 30;
                        text.color = new Color(.69f,1f,.82f);
                    }
                    Vector2 position = MapGraphic.WorldToMap(target.position);
                    // Keep nearby landmarks visible beside the player arrow. The icon
                    // returns to its actual projected position once they separate.
                    if (target.GetComponent<NealFairyHover>() != null || target.GetComponent<CandyHouseMarker>() != null || melodyEntrance || target.GetComponent<CandyBounceGarden>() != null || target.GetComponent<ZhuBaJieBoss>() != null || target.GetComponent<HuangMeiBoss>() != null || friends && (target==friends.Home || target==friends.Lost) || target.name=="Skyboard rally start sign")
                    {
                        Vector2 separation = position - playerArrow.rectTransform.anchoredPosition;
                        if (separation.sqrMagnitude < 28f * 28f)
                            position = playerArrow.rectTransform.anchoredPosition +
                                (separation.sqrMagnitude > .01f ? separation.normalized : Vector2.up) * 28f;
                    }
                    markers[i].rectTransform.anchoredPosition = position;
                }
            }
            var rounded=new Vector3(Mathf.Round(transform.position.x),Mathf.Round(transform.position.z),Mathf.Round(MapGraphic.WorldSpan));
            if(rounded!=shownCoordinates || coordinateLanguage!=GameLocalization.Language)
            {
                shownCoordinates=rounded;coordinateLanguage=GameLocalization.Language;
                coordinates.text=GameLocalization.Format("map.coordinates",rounded.x.ToString("F0"),rounded.y.ToString("F0"),rounded.z.ToString("F0"));
            }
        }
        void SuspendWorldRendering()
        {
            suspendedCamera=Camera.main;if(!suspendedCamera)return;
            worldMask=suspendedCamera.cullingMask;suspendedCamera.cullingMask=0;
            cameraData=suspendedCamera.GetComponent<UniversalAdditionalCameraData>();
            if(!cameraData)return;
            worldShadows=cameraData.renderShadows;worldPost=cameraData.renderPostProcessing;
            worldDepth=cameraData.requiresDepthOption;worldColor=cameraData.requiresColorOption;
            cameraData.renderShadows=false;cameraData.renderPostProcessing=false;
            cameraData.requiresDepthOption=CameraOverrideOption.Off;cameraData.requiresColorOption=CameraOverrideOption.Off;
        }
        void RestoreWorldRendering()
        {
            if(suspendedCamera)suspendedCamera.cullingMask=worldMask;
            if(cameraData)
            {
                cameraData.renderShadows=worldShadows;cameraData.renderPostProcessing=worldPost;
                cameraData.requiresDepthOption=worldDepth;cameraData.requiresColorOption=worldColor;
            }
            suspendedCamera=null;cameraData=null;
        }
        void Build()
        {
            if(gameObject.scene.path==SecondIslandBlackboards.ScenePath)font=SecondIslandBlackboards.InterfaceFont;
            if(!font)foreach (string name in Font.GetOSInstalledFontNames())
                if (name == "Microsoft YaHei") { font = Font.CreateDynamicFontFromOSFont(name, 24); ownsFont = true; break; }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = new GameObject("Island Map Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 400;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var background = Box(root.transform, "Map backdrop", Vector2.zero, Vector2.zero, GameUITheme.Gray(0,.98f));
            background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.offsetMin = background.offsetMax = Vector2.zero;
            Label(root.transform, "ISLAND MAP", 40, new Vector2(-165, 470), new Vector2(800, 70), Color.white);
            GameUITheme.Rule(root.transform,new Vector2(-165,429),new Vector2(814,1),GameUITheme.Muted);
            Box(root.transform, "Map border", new Vector2(-165, 0), new Vector2(814, 814), GameUITheme.Muted);
            var mapArea = Box(root.transform, "Ocean", new Vector2(-165, 0), new Vector2(810, 810), GameUITheme.Gray(.12f));
            mapArea.gameObject.AddComponent<RectMask2D>();
            for (int i = -3; i <= 3; i++)
            {
                Box(mapArea, "Longitude", new Vector2(i * 100, 0), new Vector2(1, 810), GameUITheme.Gray(.2f));
                Box(mapArea, "Latitude", new Vector2(0, i * 100), new Vector2(810, 1), GameUITheme.Gray(.2f));
            }
            var terrain = new GameObject("Island terrain", typeof(RectTransform), typeof(IslandMapGraphic));
            terrain.transform.SetParent(mapArea, false);
            var rect = (RectTransform)terrain.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            MapGraphic = terrain.GetComponent<IslandMapGraphic>(); MapGraphic.raycastTarget = false;
            playerArrow = Label(terrain.transform, "▲", 32, Vector2.zero, new Vector2(44, 44), GameUITheme.Foreground);
            GameUITheme.OutlineSymbol(playerArrow);
            Label(mapArea, "NORTH  N", 23, new Vector2(0, 380), new Vector2(110, 45), Color.white);
            Label(root.transform, "LEGEND", 21, new Vector2(480, 335), new Vector2(330, 60), Color.white);
            GameUITheme.Rule(root.transform,new Vector2(480,292),new Vector2(290,1),GameUITheme.Muted);
            Label(root.transform, "▲  Player / Facing", 24, new Vector2(480, 245), new Vector2(330, 60), GameUITheme.Foreground);
            if (gameObject.scene.path == "Assets/Scenes/Second Island/Second Island.unity")
            {
                Label(root.transform, "✦  Fairy", 24, new Vector2(480, 165), new Vector2(330, 40), new Color(1f, .88f, .98f));
                Label(root.transform, "!  Candy hunt", 22, new Vector2(480, 125), new Vector2(330, 40), new Color(1f,.86f,.53f));
                Label(root.transform, "H  Candy house", 22, new Vector2(480, 85), new Vector2(330, 40), new Color(1f,.67f,.78f));
                Label(root.transform, "◆ Trail camp   ● Island sweep", 20, new Vector2(480, 45), new Vector2(350, 40), new Color(1f,.7f,.25f));
                Label(root.transform, "◆  Current objective", 22, new Vector2(480, 5), new Vector2(330, 40), new Color(.45f,1f,.92f));
                Label(root.transform, "●  Candy clue", 22, new Vector2(480, -35), new Vector2(330, 40), new Color(1f,.42f,.7f));
                Label(root.transform, "S  Skyboard rally", 22, new Vector2(480, -75), new Vector2(330, 40), new Color(1f,.85f,.35f));
                Label(root.transform, "♪  Candy Melody Garden", 22, new Vector2(480, -115), new Vector2(330, 40), new Color(.69f,1f,.82f));
                Label(root.transform, "★  Bounce garden", 22, new Vector2(480, -155), new Vector2(330, 40), new Color(1f,.78f,.32f));
                Label(root.transform, "F  Friends story   ·   ✓  Completed", 18, new Vector2(480, -190), new Vector2(360, 32), new Color(.65f,.93f,1f));
                Label(root.transform, "Z  Zhu Bajie     Y  Huang Mei", 19, new Vector2(480, -220), new Vector2(330, 28), new Color(1f,.55f,.30f));
                Label(root.transform, "R  Ridge rest camp", 19, new Vector2(480, -244), new Vector2(330, 28), new Color(1f,.83f,.2f));
            }
            else
            {
            Label(root.transform, "◆  Island checkpoint", 24, new Vector2(480, 165), new Vector2(330, 60), GameUITheme.Foreground);
            Label(root.transform, "C  Capri", 22, new Vector2(480, 105), new Vector2(330, 48), GameUITheme.Foreground);
            Label(root.transform, "N  Nailong", 22, new Vector2(480, 55), new Vector2(330, 48), GameUITheme.Foreground);
            Label(root.transform, "B  Bombardino", 22, new Vector2(480, 5), new Vector2(330, 48), GameUITheme.Foreground);
            Label(root.transform, "+  M4A1", 22, new Vector2(480, -45), new Vector2(330, 48), new Color(1, .83f, .2f));
            Label(root.transform, "R  Shore camp · HP / SP / ammo", 20, new Vector2(480, -95), new Vector2(330, 48), new Color(1, .83f, .2f));
            Label(root.transform, "▣ Ammo supply points", 22, new Vector2(480, -145), new Vector2(330, 48), new Color(1, .83f, .2f));
            }
            coordinates = Label(root.transform, "", 18, new Vector2(480, -270), new Vector2(360, 50), GameUITheme.Secondary);
            Label(root.transform, "M / Esc to close · Game paused while viewing", 17, new Vector2(-165, -465), new Vector2(1100, 55), GameUITheme.Secondary);
            var close = Box(root.transform, "Close map", new Vector2(480, -325), new Vector2(290, 64), GameUITheme.Track);
            var closeButton=close.gameObject.AddComponent<Button>();
            GameUITheme.StyleButton(closeButton);
            closeButton.onClick.AddListener(() => SetOpen(false));
            Label(close, "BACK TO GAME", 25, Vector2.zero, new Vector2(280, 60), Color.white);
            root.SetActive(false);
        }
        RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
            obj.GetComponent<Image>().color = color; return rect;
        }
        Text Label(Transform parent, string value, int size, Vector2 position, Vector2 bounds, Color color)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; rect.sizeDelta = bounds; rect.anchoredPosition = position;
            var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = size; text.text = value;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            LocalizedGameText.Bind(text); return text;
        }
        void OnDisable() { if (PanelVisible) SetOpen(false); RestoreWorldRendering(); }
        void OnDestroy()
        {
            if (PanelVisible) SetOpen(false);
            RestoreWorldRendering();
            if (root != null) Destroy(root);
            if (ownsFont && font != null) Destroy(font);
        }
    }
}
