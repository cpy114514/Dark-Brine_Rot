using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Mavis;

/// <summary>The first conversation after Sahur wakes on the second island.</summary>
public sealed class SecondIslandDialogue : MonoBehaviour
{
    public readonly struct Line
    {
        public readonly bool fairy;
        public readonly string english;
        public Line(bool fairy, string english)
        { this.fairy=fairy; this.english=english; }
    }
    public static readonly Line[] Lines = {
        new Line(false, "Ugh... my head is spinning... Where am I?"),
        new Line(true,  "You're finally awake. I found you unconscious while patrolling the island."),
        new Line(false, "I was unconscious? What is this place? And who are you?"),
        new Line(true,  "This is Marshmallow Island. You can call me Fairy."),
        new Line(false, "Why am I here? Do you know how I can get back?"),
        new Line(true,  "I'm afraid you'll have to find that answer yourself. I don't know where you mean by 'back'."),
        new Line(false, "What do you mean? Is there no way for you to leave this island?"),
        new Line(true,  "No, there isn't. But I believe you can find a way. Don't give up!")
    };
    public bool Active { get; private set; }
    public int CurrentLine { get; private set; } = -1;
    public string CurrentSpeaker => CurrentLine<0 ? "" : Lines[CurrentLine].fairy ? "Fairy" : "Sahur";
    GameObject panel;
    Text speaker, english, hint;

    public IEnumerator Play(Transform canvas)
    {
        Build(canvas);
        Active=true;
        for (int index=0; index<Lines.Length; index++)
        {
            CurrentLine=index;
            var line=Lines[index];
            speaker.text=line.fairy ? "FAIRY" : "SAHUR";
            speaker.color=line.fairy ? new Color(.66f,.94f,.9f) : new Color(1f,.82f,.68f);
            english.text=line.english;
            hint.text=index==Lines.Length-1 ? "SPACE / CLICK  FINISH" : "SPACE / CLICK  CONTINUE";
            // Require a fresh press after the line is visible; an input used to
            // leave an earlier screen must never skip the first sentence.
            float ready=0;
            while (true)
            {
                if (Time.timeScale>0 && !PauseSettingsMenu.IsOpen)
                {
                    ready+=Time.deltaTime;
                    if (ready>=.15f && AdvancePressed()) break;
                }
                yield return null;
            }
            yield return null;
        }
        Active=false; CurrentLine=-1;
        if(panel)Destroy(panel);
    }
    static bool AdvancePressed()
    {
        var k=Keyboard.current; var mouse=Mouse.current; var gamepad=Gamepad.current;
        return k!=null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) ||
            mouse!=null && mouse.leftButton.wasPressedThisFrame || gamepad!=null && gamepad.buttonSouth.wasPressedThisFrame;
    }
    static Text Label(Transform parent,string name,int size,TextAnchor alignment,Vector2 min,Vector2 max,Vector2 padMin,Vector2 padMax)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=padMin;rect.offsetMax=padMax;
        var text=go.GetComponent<Text>();text.font=GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize=size;text.alignment=alignment;text.color=Color.white;text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
        return text;
    }
    void Build(Transform parent)
    {
        panel=new GameObject("Fairy conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));panel.transform.SetParent(parent,false);
        var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.12f,0);rect.anchorMax=new Vector2(.88f,0);
        rect.offsetMin=new Vector2(0,42);rect.offsetMax=new Vector2(0,258);
        var bg=panel.GetComponent<Image>();bg.color=new Color(.035f,.025f,.055f,.92f);bg.raycastTarget=false;
        var rule=new GameObject("Accent",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));rule.transform.SetParent(panel.transform,false);
        var rr=rule.GetComponent<RectTransform>();rr.anchorMin=new Vector2(0,1);rr.anchorMax=Vector2.one;rr.offsetMin=new Vector2(0,-3);rr.offsetMax=Vector2.zero;
        var line=rule.GetComponent<Image>();line.color=new Color(.75f,.59f,.82f,.95f);line.raycastTarget=false;
        speaker=Label(panel.transform,"Speaker",26,TextAnchor.MiddleLeft,new Vector2(0,1),Vector2.one,new Vector2(36,-56),new Vector2(-36,-16));
        english=Label(panel.transform,"English dialogue",30,TextAnchor.MiddleLeft,new Vector2(0,1),Vector2.one,new Vector2(36,-155),new Vector2(-36,-55));
        hint=Label(panel.transform,"Continue",16,TextAnchor.MiddleRight,Vector2.zero,new Vector2(1,0),new Vector2(36,9),new Vector2(-36,39));
        hint.color=new Color(.72f,.68f,.77f);
    }
    void OnDestroy() { Active=false; if(panel)Destroy(panel); }
}
