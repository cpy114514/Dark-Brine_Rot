using Mavis;
using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Two monochrome bars, identified by HP/SP rather than color.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class PlayerVitalsHUD : MonoBehaviour
    {
        [Min(180f)] public float panelWidth = 280f;
        [Min(28f)] public float panelHeight = 32f;
        public Vector2 screenMargin = new Vector2(28f, 26f);

        PlayerHealth health;
        PlayerStamina stamina;
        CanvasGroup canvasGroup;
        Image healthFill;
        Image staminaFill;
        Sprite solidSprite;
        float shownHealth;
        float shownStamina;

        void Awake()
        {
            ConfigureCanvasScale();
            BuildHud();
            FindPlayerVitals();
            RefreshBars(true);
        }

        void Update()
        {
            FindPlayerVitals();
            if (canvasGroup != null)
                canvasGroup.alpha = health != null && health.isActiveAndEnabled && health.currentHealth > 0f &&
                    health.GetComponent<ThirdPersonPlayerController>() is ThirdPersonPlayerController movement && movement.isActiveAndEnabled &&
                    !global::PauseSettingsMenu.IsOpen && !SahurLoadoutUI.IsOpen && !IslandMapUI.BlocksInput && !PlayerDeathRespawn.IsOpen ? 1f : 0f;
            RefreshBars(false);
        }

        void ConfigureCanvasScale()
        {
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        void BuildHud()
        {
            solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);

            var hud = new GameObject("Sahur Vitals HUD", typeof(RectTransform));
            hud.transform.SetParent(transform, false);
            var hudRect = hud.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.zero;
            hudRect.pivot = Vector2.zero;
            hudRect.anchoredPosition = screenMargin;
            hudRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            canvasGroup = hud.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var healthBack = CreateImage(hud.transform, "Health Track", Color.black);
            SetTopRow(healthBack.rectTransform, 30f, 0f, 10f);
            healthFill = CreateImage(healthBack.transform, "Health Fill", new Color32(245, 245, 245, 255));
            SetFillRect(healthFill.rectTransform);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillAmount = 1f;

            var staminaBack = CreateImage(hud.transform, "Stamina Track", Color.black);
            SetTopRow(staminaBack.rectTransform, 30f, -16f, 10f);
            staminaFill = CreateImage(staminaBack.transform, "Stamina Fill", GameUITheme.Secondary);
            SetFillRect(staminaFill.rectTransform);
            staminaFill.type = Image.Type.Filled;
            staminaFill.fillMethod = Image.FillMethod.Horizontal;
            staminaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            staminaFill.fillAmount = 1f;

            BarLabel(hud.transform,"HP",0);
            BarLabel(hud.transform,"SP",-16);
        }

        void BarLabel(Transform parent,string caption,float y)
        {
            var obj=new GameObject(caption,typeof(RectTransform),typeof(Text));
            obj.transform.SetParent(parent,false);
            var text=obj.GetComponent<Text>();
            text.font=GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=12; text.text=caption; text.color=GameUITheme.Foreground;
            text.verticalOverflow=VerticalWrapMode.Overflow;
            text.alignment=TextAnchor.MiddleLeft; text.raycastTarget=false;
            var rect=text.rectTransform; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(0,y+2); rect.sizeDelta=new Vector2(26,14);
            GameUITheme.OutlineSymbol(text);
        }

        void FindPlayerVitals()
        {
            if (health == null) health = FindFirstObjectByType<PlayerHealth>();
            if (stamina == null) stamina = FindFirstObjectByType<PlayerStamina>();
        }

        void RefreshBars(bool immediate)
        {
            float targetHealth = health != null && health.maxHealth > 0f
                ? Mathf.Clamp01(health.currentHealth / health.maxHealth) : 0f;
            float targetStamina = stamina != null ? stamina.Normalized : 0f;
            float step = immediate ? 1f : Time.unscaledDeltaTime * 3.5f;
            shownHealth = Mathf.MoveTowards(shownHealth, targetHealth, step);
            shownStamina = Mathf.MoveTowards(shownStamina, targetStamina, step * 1.5f);

            if (healthFill != null) healthFill.fillAmount = shownHealth;
            if (staminaFill != null) staminaFill.fillAmount = shownStamina;
        }

        Image CreateImage(Transform parent, string objectName, Color color)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = solidSprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void SetTopRow(RectTransform rect, float left, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, top);
            rect.sizeDelta = new Vector2(-left, height);
        }

        static void SetFillRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            if (solidSprite != null) Destroy(solidSprite);
        }
    }
}
