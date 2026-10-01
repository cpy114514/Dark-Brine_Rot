using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Shared neutral palette. UI only: never desaturates world materials or authored illustrations.</summary>
public static class GameUITheme
{
    public static readonly Color Foreground = Color.white;
    public static readonly Color Secondary = Gray(.72f);
    public static readonly Color Muted = Gray(.42f);
    public static readonly Color Surface = Gray(.06f,.99f);
    public static readonly Color Track = Gray(.14f,.98f);
    public static readonly Color Backdrop = Gray(0,.92f);
    public static Color Gray(float value,float alpha=1) => new Color(value,value,value,alpha);
    public static Color Neutral(Color color) => Gray(color.grayscale,color.a);

    public static void StyleButton(Button button,bool light=false)
    {
        if(!button) return;
        if(!button.targetGraphic) button.targetGraphic=button.GetComponent<Graphic>();
        // ColorTint multiplies Image.color, so black images cannot show hover feedback.
        // Use a white base and explicit neutral states instead.
        if(button.image) button.image.color=Color.white;
        var colors=button.colors;
        colors.normalColor=Gray(light ? 1f : .08f);
        colors.highlightedColor=Gray(light ? .86f : .22f);
        colors.pressedColor=Gray(light ? .72f : .32f);
        colors.selectedColor=Gray(light ? .92f : .18f);
        colors.disabledColor=Gray(light ? .65f : .10f,.5f);
        colors.colorMultiplier=1; colors.fadeDuration=.08f;
        button.colors=colors;
        // Background and caption must be restyled together. Settings switches
        // originally light prefab buttons to dark ones when it binds controls.
        Color captionColor=light ? Color.black : Foreground;
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
