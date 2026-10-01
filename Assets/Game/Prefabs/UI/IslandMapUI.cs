using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        GameObject root;
        Font font;
        bool ownsFont;
        float previousTimeScale;
        CursorLockMode previousCursor;
        bool previousVisible;
        Text playerArrow, coordinates;
        readonly List<RectTransform> markers = new List<RectTransform>();
        readonly List<Transform> targets = new List<Transform>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { active = null; consumedFrame = -1; }
        void Update()
        {
            if (PlayerDeathRespawn.IsOpen) { if (PanelVisible) SetOpen(false); return; }
            if (Keyboard.current != null)
            {
                if (Keyboard.current.mKey.wasPressedThisFrame) SetOpen(!PanelVisible);
                else if (PanelVisible && Keyboard.current.escapeKey.wasPressedThisFrame) SetOpen(false);
            }
            if (PanelVisible) RefreshMarkers();
        }
        public bool SetOpen(bool open)
        {
            if (open == PanelVisible) return false;
            if (open && consumedFrame == Time.frameCount) return false;
            if (open && (PlayerDeathRespawn.IsOpen || PauseSettingsMenu.IsOpen || SahurLoadoutUI.IsOpen || (active != null && active != this))) return false;
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
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (EventSystem.current == null) new GameObject("Map EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
            else
            {
                root.SetActive(false);
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
            foreach (var point in FindObjectsByType<IslandRespawnPoint>(FindObjectsSortMode.None))
                AddMarker(point.transform, "◆", GameUITheme.Foreground);
            foreach (var enemy in FindObjectsByType<NailongHealth>(FindObjectsSortMode.None))
                if (!enemy.IsDead) AddMarker(enemy.transform, "●", GameUITheme.Foreground);
            playerArrow.transform.SetAsLastSibling();
        }
        void AddMarker(Transform target, string symbol, Color color)
        {
            var label = Label(MapGraphic.transform, symbol, 28, Vector2.zero, new Vector2(42, 42), color);
            GameUITheme.OutlineSymbol(label);
            targets.Add(target); markers.Add(label.rectTransform);
        }
        void RefreshMarkers()
        {
            playerArrow.rectTransform.anchoredPosition = MapGraphic.WorldToMap(transform.position);
            playerArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -transform.eulerAngles.y);
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                bool visible = target != null && target.gameObject.activeInHierarchy;
                if (visible && target.TryGetComponent<NailongHealth>(out var health)) visible = !health.IsDead;
                markers[i].gameObject.SetActive(visible);
                if (visible) markers[i].anchoredPosition = MapGraphic.WorldToMap(target.position);
            }
            coordinates.text = GameLocalization.Format("map.coordinates", transform.position.x.ToString("F0"), transform.position.z.ToString("F0"), MapGraphic.WorldSpan.ToString("F0"));
        }
        void Build()
        {
            foreach (string name in Font.GetOSInstalledFontNames())
                if (name == "Microsoft YaHei") { font = Font.CreateDynamicFontFromOSFont(name, 24); ownsFont = true; break; }
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = new GameObject("Island Map Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 400;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var background = Box(root.transform, "Map backdrop", Vector2.zero, Vector2.zero, GameUITheme.Gray(0,.98f));
            background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.offsetMin = background.offsetMax = Vector2.zero;
            Label(root.transform, "岛屿地图", 40, new Vector2(-165, 470), new Vector2(800, 70), Color.white);
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
            Label(mapArea, "北  N", 23, new Vector2(0, 380), new Vector2(110, 45), Color.white);
            Label(root.transform, "地图图例", 30, new Vector2(480, 335), new Vector2(330, 60), Color.white);
            Label(root.transform, "▲  玩家 / 朝向", 24, new Vector2(480, 245), new Vector2(330, 60), GameUITheme.Foreground);
            Label(root.transform, "◆  岛上复活点", 24, new Vector2(480, 165), new Vector2(330, 60), GameUITheme.Foreground);
            Label(root.transform, "●  Nailong", 24, new Vector2(480, 85), new Vector2(330, 60), GameUITheme.Foreground);
            coordinates = Label(root.transform, "", 22, new Vector2(480, -125), new Vector2(360, 190), GameUITheme.Secondary);
            Label(root.transform, "M / Esc 关闭地图 · 查看时游戏暂停", 23, new Vector2(-165, -465), new Vector2(1100, 55), GameUITheme.Secondary);
            var close = Box(root.transform, "Close map", new Vector2(480, -325), new Vector2(290, 64), GameUITheme.Track);
            var closeButton=close.gameObject.AddComponent<Button>();
            GameUITheme.StyleButton(closeButton);
            closeButton.onClick.AddListener(() => SetOpen(false));
            Label(close, "返回游戏", 25, Vector2.zero, new Vector2(280, 60), Color.white);
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
        void OnDisable() { if (PanelVisible) SetOpen(false); }
        void OnDestroy()
        {
            if (PanelVisible) SetOpen(false);
            if (root != null) Destroy(root);
            if (ownsFont && font != null) Destroy(font);
        }
    }
}
