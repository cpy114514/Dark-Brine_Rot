using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mavis;

/// <summary>Neutral colors shared by the menus and instruments.</summary>
public static class GameUITheme
{
    public static readonly Color Foreground = Gray(.97f);
    public static readonly Color Secondary = Gray(.74f);
    public static readonly Color Muted = Gray(.56f);
    public static readonly Color Surface = Gray(.065f, .95f);
    public static readonly Color Track = Gray(.23f);
    public static readonly Color Backdrop = Gray(.025f, .98f);
    public static Color Gray(float value,float alpha=1) => new Color(value,value,value,alpha);
    public static Color Neutral(Color color) => Gray(color.grayscale,color.a);
    static readonly System.Collections.Generic.Dictionary<(string, bool), string> worldPrompts = new();
    static int promptRevision = -1;
    static GameLanguage promptLanguage;

    public static string InteractPrompt(string action, bool withSprint = false)
    {
        if (promptRevision != GameControls.Revision || promptLanguage != GameLocalization.Language)
        {
            worldPrompts.Clear();
            promptRevision = GameControls.Revision;
            promptLanguage = GameLocalization.Language;
        }
        if (worldPrompts.TryGetValue((action, withSprint), out var label)) return label;
        string key = GameControls.Prompt(GameControls.Command.Interact);
        if (withSprint) key = GameControls.Prompt(GameControls.Command.Sprint) + " + " + key;
        label = "<b>[" + key + "]</b>  " + GameLocalization.Text(action);
        worldPrompts.Add((action, withSprint), label);
        return label;
    }
    public static bool ModalOpen => SaveSlotMenu.IsOpen || PauseSettingsMenu.IsOpen || Mavis.SahurLoadoutUI.IsOpen ||
        Mavis.IslandMapUI.BlocksInput || Mavis.PlayerDeathRespawn.IsOpen;

    public static void Scale(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (!scaler) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
    }
    public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent,false);
        var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size;
        return rect;
    }
    public static Image Image(RectTransform rect, Color color)
    { var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
    public static Image Fill(RectTransform parent, string name, Color color)
    {
        var rect = Rect(parent,name,new Vector2(.5f,.5f),Vector2.zero);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = Image(rect,color);
        // A filled Image needs a sprite to respect fillAmount.
        image.sprite = WhiteSprite; image.type = UnityEngine.UI.Image.Type.Filled;
        image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; return image;
    }
    static Sprite whiteSprite;
    public static Sprite WhiteSprite
    {
        get
        {
            if (!whiteSprite)
            {
                var white = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(white,new Rect(0,0,white.width,white.height),new Vector2(.5f,.5f));
            }
            return whiteSprite;
        }
    }
    public static Text Label(Transform parent,string name,int size,Vector2 position,Vector2 dimensions,TextAnchor alignment)
    {
        var rect=Rect(parent,name,new Vector2(.5f,1),dimensions);
        rect.pivot=new Vector2(alignment==TextAnchor.MiddleLeft?0:alignment==TextAnchor.MiddleRight?1:.5f,.5f);
        rect.anchoredPosition=position;
        var text=rect.gameObject.AddComponent<Text>();text.font=GameLocalization.Font?GameLocalization.Font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(Pixel360Rendering.IsActive)text.material=PixelStyleUI.NativeTextMaterial;
        text.fontSize=size;text.color=Foreground;text.alignment=alignment;text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
    }
    public static void Rule(Transform parent,Vector2 position,Vector2 size,Color color,Vector2? anchor=null)
    {var rect=Rect(parent,"Hairline",anchor??new Vector2(.5f,.5f),size);rect.anchoredPosition=position;Image(rect,color);}

    public static void StyleButton(Button button,bool light=false)
    {
        if(!button) return;
        if(!button.targetGraphic) button.targetGraphic=button.GetComponent<Graphic>();
        // ColorTint multiplies Image.color, so black images cannot show hover feedback.
        // Use a white base and explicit neutral states instead.
        if(button.image) button.image.color=Color.white;
        var colors=button.colors;
        colors.normalColor=light ? Foreground : Gray(.115f);
        colors.highlightedColor=Gray(light ? .86f : .22f);
        colors.pressedColor=Gray(light ? .73f : .32f);
        colors.selectedColor=Gray(light ? .88f : .25f);
        colors.disabledColor=Gray(light ? .53f : .055f);
        colors.colorMultiplier=1; colors.fadeDuration=.08f;
        button.colors=colors;
        // Background and caption must be restyled together. Settings switches
        // originally light prefab buttons to dark ones when it binds controls.
        Color captionColor=light ? Color.black : button.interactable ? Foreground : Muted;
        foreach(var caption in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if(caption.GetComponentInParent<Button>(true)!=button) continue;
            caption.color=captionColor;
            caption.enableVertexGradient=false;
        }
        foreach(var caption in button.GetComponentsInChildren<Text>(true))
            if(caption.GetComponentInParent<Button>(true)==button) caption.color=captionColor;
    }
    public static void OutlineSymbol(Text text)
    {
        var outline=text.GetComponent<Outline>();
        if(!outline) outline=text.gameObject.AddComponent<Outline>();
        outline.effectColor=Color.black; outline.effectDistance=new Vector2(1,-1);
        outline.useGraphicAlpha=true;
    }
}
