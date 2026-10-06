using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Refines the existing menu hierarchy; all buttons retain their authored actions.</summary>
public static class MinimalMenuLayout
{
    static RectTransform Place(Transform parent,string name,Vector2 position,Vector2 size,Vector2? anchor=null)
    {
        var child=parent.Find(name);if(!child)return null;
        var rect=child as RectTransform;if(!rect)return null;
        rect.anchorMin=rect.anchorMax=anchor??new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static void Caption(RectTransform rect,float size,Color color,bool left=true)
    {
        if(!rect)return;
        var text=rect.GetComponent<TMP_Text>();if(!text)return;
        text.fontSize=size;text.color=color;text.enableVertexGradient=false;
        text.alignment=left?TextAlignmentOptions.MidlineLeft:TextAlignmentOptions.Center;
        text.characterSpacing=size>=50?-2:1.5f;
        text.enableAutoSizing=false;
    }
    public static void MainMenu(MainMenuController menu)
    {
        var root=menu.transform;
        var canvas=menu.GetComponent<Canvas>();if(canvas)GameUITheme.Scale(canvas);
        var background=root.Find("Black Background")?.GetComponent<Image>();if(background)background.color=GameUITheme.Gray(.025f);
        Caption(Place(root,"Game Title",new Vector2(108,-130),new Vector2(1000,115)),82,Color.white);
        Caption(Place(root,"Menu Label",new Vector2(112,-276),new Vector2(500,32)),16,GameUITheme.Secondary);
        var rule=Place(root,"White Rule",new Vector2(112,-330),new Vector2(480,1));
        if(rule)rule.GetComponent<Image>().color=GameUITheme.Muted;
        var names=new[]{"New Game","Continue","Settings","Quit"};
        for(int i=0;i<names.Length;i++)
        {
            var rect=Place(root,names[i],new Vector2(112,-366-i*84),new Vector2(480,64));if(!rect)continue;
            var button=rect.GetComponent<Button>();GameUITheme.StyleButton(button,i==0);
            if(!rect.GetComponent<MinimalButtonMotion>())rect.gameObject.AddComponent<MinimalButtonMotion>();
            foreach(var text in rect.GetComponentsInChildren<TMP_Text>())
            {
                text.fontSize=22;text.characterSpacing=2;text.enableAutoSizing=false;
                text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
                text.rectTransform.offsetMin=new Vector2(24,0);text.rectTransform.offsetMax=new Vector2(-24,0);
            }
        }
        Caption(Place(root,"Save Status",new Vector2(112,-718),new Vector2(480,30)),13,GameUITheme.Secondary);
        Caption(Place(root,"Footer",new Vector2(112,-794),new Vector2(600,26)),13,GameUITheme.Secondary);
        GameUITheme.Rule(root,new Vector2(352,-764),new Vector2(480,1),GameUITheme.Muted,new Vector2(0,1));
        var marker=GameUITheme.Label(root,"Chapter marker",13,new Vector2(112,-78),new Vector2(620,24),TextAnchor.MiddleLeft);
        marker.rectTransform.anchorMin=marker.rectTransform.anchorMax=new Vector2(0,1);
        marker.text="01  /  FIRST ISLAND";marker.color=GameUITheme.Secondary;
        Mavis.LocalizedGameText.Bind(marker);
        // Authored decoration drawings already contain black-and-white silhouettes.
        // Keep their sprite swaps and interactive island rotation intact.
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {var shadow=text.GetComponent<Shadow>();if(shadow)shadow.enabled=false;}
        foreach(var image in root.GetComponentsInChildren<Image>(true))
            if(image.name.Contains("Decoration") && !image.GetComponent<MinimalArtworkStyle>())image.gameObject.AddComponent<MinimalArtworkStyle>();
    }

    public static void Settings(PauseSettingsMenu menu)
    {
        foreach(var image in menu.GetComponentsInChildren<Image>(true))
        {
            if(image.name.EndsWith("border"))image.color=GameUITheme.Gray(.22f);
            else if(image.name=="Divider")image.color=GameUITheme.Muted;
            else if(image.name=="Backdrop")image.color=GameUITheme.Backdrop;
        }
        var title=menu.homePanel.transform.Find("Paused title") as RectTransform;
        Caption(title,58,Color.white,false);
        if(title){title.anchoredPosition=new Vector2(0,192);title.sizeDelta=new Vector2(600,82);}
        GameUITheme.Rule(menu.homePanel.transform,new Vector2(0,126),new Vector2(440,1),GameUITheme.Muted);
        int i=0;
        foreach(var button in new[]{menu.resumeButton,menu.settingsButton,menu.quitButton})
        {
            if(!button)continue;
            var border=button.transform.parent as RectTransform;
            if(border){border.sizeDelta=new Vector2(442,62);border.anchoredPosition=new Vector2(0,65-i*84);}
            var rect=button.transform as RectTransform;
            if(rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(1,1);rect.offsetMax=new Vector2(-1,-1);}
            GameUITheme.StyleButton(button,i==0);
            if(!button.GetComponent<MinimalButtonMotion>())button.gameObject.AddComponent<MinimalButtonMotion>();
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=21;text.characterSpacing=1.5f;}
            i++;
        }
        foreach(var option in menu.options)
        {
            if(option.value){option.value.fontSize=18;option.value.color=GameUITheme.Foreground;}
            if(option.slider)
            {
                var fill=option.slider.fillRect?.GetComponent<Image>();if(fill)fill.color=GameUITheme.Foreground;
                var handle=option.slider.handleRect?.GetComponent<Image>();if(handle)handle.color=GameUITheme.Foreground;
                if(option.slider.handleRect)option.slider.handleRect.sizeDelta=new Vector2(3,16);
            }
        }
        foreach(var text in menu.GetComponentsInChildren<TMP_Text>(true))
        {
            text.enableVertexGradient=false;
            if(text.name=="Explanation"){text.fontSize=15;text.color=GameUITheme.Secondary;}
            else if(text.name=="Label")text.fontSize=20;
            var shadow=text.GetComponent<Shadow>();if(shadow)shadow.enabled=false;
        }
        if(menu.sectionTitle){menu.sectionTitle.fontSize=30;menu.sectionTitle.characterSpacing=2;}
        if(menu.footerHint){menu.footerHint.fontSize=14;menu.footerHint.color=GameUITheme.Secondary;}
    }
}
