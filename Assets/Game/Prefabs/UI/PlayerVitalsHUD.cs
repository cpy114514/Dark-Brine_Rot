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
        ThirdPersonPlayerController movement;
        float nextPlayerSearch;
        PlayerStamina stamina;
        SahurHealingPacks healing;
        Text healingLabel, healingFeedback, hpValue;
        int shownHp=-1, shownMaxHp=-1;
        int shownPacks = -1;
        GameLanguage shownLanguage;
        UnityEngine.InputSystem.Key shownHealKey;
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
                    movement != null && movement.isActiveAndEnabled &&
                    !global::PauseSettingsMenu.IsOpen && !SahurLoadoutUI.IsOpen && !IslandMapUI.BlocksInput && !PlayerDeathRespawn.IsOpen ? 1f : 0f;
            RefreshBars(false);
            RefreshHealing();
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
            panelWidth=Mathf.Max(320,panelWidth);
            hudRect.sizeDelta = new Vector2(panelWidth+36,112);
            CreateImage(hud.transform,"Vitals surface",GameUITheme.Surface).rectTransform.sizeDelta=hudRect.sizeDelta;
            var surface=hud.transform.Find("Vitals surface").GetComponent<RectTransform>();
            surface.anchorMin=surface.anchorMax=surface.pivot=Vector2.zero;surface.anchoredPosition=Vector2.zero;
            GameUITheme.Rule(hud.transform,new Vector2((panelWidth+36)*.5f,112),new Vector2(panelWidth+36,1),GameUITheme.Muted,Vector2.zero);
            var name=HealingText(hud.transform,"Player name",new Vector2(18,-12),new Vector2(180,24));
            name.text="SAHUR";name.fontSize=13;name.color=GameUITheme.Secondary;
            hpValue=HealingText(hud.transform,"Health value",new Vector2(panelWidth-142,-12),new Vector2(160,24));
            hpValue.alignment=TextAnchor.MiddleRight;hpValue.fontSize=15;

            canvasGroup = hud.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var healthBack = CreateImage(hud.transform, "Health Track", GameUITheme.Track);
            SetTopRow(healthBack.rectTransform, 18f, -44f, 8f);
            healthFill = CreateImage(healthBack.transform, "Health Fill", new Color32(245, 245, 245, 255));
            SetFillRect(healthFill.rectTransform);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillAmount = 1f;

            var staminaBack = CreateImage(hud.transform, "Stamina Track", GameUITheme.Track);
            SetTopRow(staminaBack.rectTransform, 46f, -62f, 3f);
            staminaBack.rectTransform.sizeDelta=new Vector2(-64,3);
            var sp=HealingText(hud.transform,"Stamina label",new Vector2(18,-52),new Vector2(28,20));
            sp.text="SP";sp.fontSize=10;sp.color=GameUITheme.Secondary;
            staminaFill = CreateImage(staminaBack.transform, "Stamina Fill", GameUITheme.Secondary);
            SetFillRect(staminaFill.rectTransform);
            staminaFill.type = Image.Type.Filled;
            staminaFill.fillMethod = Image.FillMethod.Horizontal;
            staminaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            staminaFill.fillAmount = 1f;


            var pack = CreateImage(hud.transform, "Healing pack icon", GameUITheme.Track);
            var packRect = pack.rectTransform;
            packRect.anchorMin = packRect.anchorMax = packRect.pivot = new Vector2(0,1);
            packRect.anchoredPosition = new Vector2(18,-82); packRect.sizeDelta = new Vector2(18,18);
            foreach (bool horizontal in new[]{true,false})
            {
                var cross=CreateImage(pack.transform,"Pack cross",GameUITheme.Foreground);
                cross.rectTransform.anchorMin=cross.rectTransform.anchorMax=new Vector2(.5f,.5f);
                cross.rectTransform.anchoredPosition=Vector2.zero;
                cross.rectTransform.sizeDelta=horizontal?new Vector2(10,2):new Vector2(2,10);
            }
            healingLabel = HealingText(hud.transform,"Healing supply",new Vector2(48,-78),new Vector2(panelWidth-48,26));
            healingFeedback = HealingText(hud.transform,"Healing feedback",new Vector2(0,32),new Vector2(470,26));
        }

        Text HealingText(Transform parent,string objectName,Vector2 position,Vector2 size)
        {
            var obj=new GameObject(objectName,typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
            var text=obj.GetComponent<Text>(); text.font=GameLocalization.Font?GameLocalization.Font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=17;text.color=GameUITheme.Foreground;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;
            text.verticalOverflow=VerticalWrapMode.Overflow;
            text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(0,1);
            text.rectTransform.anchoredPosition=position;text.rectTransform.sizeDelta=size;
            return text;
        }

        void RefreshHealing()
        {
            if(healingLabel==null)return;
            int remaining=HealingPackSupply.Remaining;
            var key=GameInputSettings.Get(GameInputSettings.Action.Heal);
            if(remaining!=shownPacks||GameLocalization.Language!=shownLanguage||key!=shownHealKey)
            {
                shownPacks=remaining;shownLanguage=GameLocalization.Language;shownHealKey=key;
                healingLabel.text=GameLocalization.Format("healing.count",key.ToString().ToUpperInvariant(),remaining);
                healingLabel.color=remaining>0?GameUITheme.Foreground:GameUITheme.Secondary;
            }
            string message=healing!=null?healing.Feedback:"";
            if(healingFeedback.text!=message)healingFeedback.text=message;
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
            if (health != null) return;
            if (Time.unscaledTime < nextPlayerSearch) return;
            nextPlayerSearch = Time.unscaledTime + .5f;
            health = FindFirstObjectByType<PlayerHealth>();
            if (health == null) return;
            movement = health.GetComponent<ThirdPersonPlayerController>();
            stamina = health.GetComponent<PlayerStamina>();
            healing = health.GetComponent<SahurHealingPacks>();
        }

        void RefreshBars(bool immediate)
        {
            float targetHealth = health != null && health.maxHealth > 0f
                ? Mathf.Clamp01(health.currentHealth / health.maxHealth) : 0f;
            float targetStamina = stamina != null ? stamina.Normalized : 0f;
            float step = immediate ? 1f : Time.unscaledDeltaTime * 3.5f;
            shownHealth = Mathf.MoveTowards(shownHealth, targetHealth, step);
            shownStamina = Mathf.MoveTowards(shownStamina, targetStamina, step * 1.5f);

            int hp=health?Mathf.CeilToInt(Mathf.Max(0,health.currentHealth)):0;
            int maximum=health?Mathf.CeilToInt(health.maxHealth):0;
            if(hpValue && (hp!=shownHp || maximum!=shownMaxHp))
            {shownHp=hp;shownMaxHp=maximum;hpValue.text=hp+" / "+maximum;}
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
            rect.sizeDelta = new Vector2(-left*2, height);
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
