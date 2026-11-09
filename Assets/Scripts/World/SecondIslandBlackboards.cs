using Mavis;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Shared materials for real, framed chalkboards in the second island.</summary>
public static class SecondIslandBlackboards
{
    public const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    static Material wood,slate;
    static Font interfaceFont;
    public static Material Wood => wood ? wood : wood=Make("Blackboard warm wood",new Color(.34f,.18f,.12f));
    public static Material Slate => slate ? slate : slate=Make("Blackboard dark green slate",new Color(.035f,.11f,.09f));
    public static TMP_FontAsset Font => GameLocalization.TMPFont;
    public static Font InterfaceFont => interfaceFont ? interfaceFont : interfaceFont=Resources.Load<Font>("SecondIslandBlackboard/LiberationSans");
    static Material Make(string name,Color color)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material=new Material(shader){name=name,hideFlags=HideFlags.DontSave};
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);else material.color=color;
        if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.12f);
        return material;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){wood=null;slate=null;interfaceFont=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initial(){for(int i=0;i<SceneManager.sceneCount;i++)ApplyAuthoredSigns(SceneManager.GetSceneAt(i));}
    static void Loaded(Scene scene,LoadSceneMode mode)=>ApplyAuthoredSigns(scene);
    static void ApplyAuthoredSigns(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        foreach(var root in scene.GetRootGameObjects())
            foreach(var text in root.GetComponentsInChildren<TextMesh>(true))
                if(!text.GetComponent<SecondIslandAuthoredChalk>())text.gameObject.AddComponent<SecondIslandAuthoredChalk>();
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<SecondIslandBlackboardUI>())return;
        var ui=new GameObject("Second Island Blackboard UI");SceneManager.MoveGameObjectToScene(ui,scene);ui.AddComponent<SecondIslandBlackboardUI>();
    }
    public static void Attach(GameObject label)
    {
        if(label && label.scene.path==ScenePath && !label.GetComponent<SecondIslandBlackboardLabel>())label.AddComponent<SecondIslandBlackboardLabel>();
    }
    public static GameObject Part(string name,Transform parent,Material material,bool collision)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);
        var renderer=o.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var collider=o.GetComponent<Collider>();if(collision)collider.isTrigger=true;else Object.Destroy(collider);
        return o;
    }
    public static TextMeshPro Chalk(Transform parent)
    {
        var obj=new GameObject("3D chalk lettering",typeof(RectTransform),typeof(TextMeshPro));obj.transform.SetParent(parent,false);
        var text=obj.GetComponent<TextMeshPro>();if(Font)text.font=Font;text.color=new Color(1f,.97f,.88f);
        text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;
        text.enableAutoSizing=true;text.fontSizeMin=.1f;text.fontSizeMax=80;text.raycastTarget=false;
        var renderer=obj.GetComponent<Renderer>();if(renderer){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        return text;
    }
}

/// <summary>Applies a readable matching typeface and chalkboard cards to island HUDs.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandBlackboardUI : MonoBehaviour
{
    static readonly System.Collections.Generic.HashSet<string> Panels=new System.Collections.Generic.HashSet<string>{
        "Vitals surface","Objective card","Supply reminder","Marshmallows remaining","Encounter","Course card","Fairy clue","Bounce card","Melody card","Grove information","Discovery tip","Friends story card","Rally card","Fairy conversation","Flight controls card","Test mode card"
    };
    float nextScan;
    void Update()
    {
        if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.75f;
        var font=SecondIslandBlackboards.InterfaceFont;
        foreach(var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(text.gameObject.scene!=gameObject.scene)continue;
            var canvas=text.GetComponentInParent<Canvas>(true);if(!canvas || canvas.renderMode==RenderMode.WorldSpace)continue;
            if(font && text.font!=font)text.font=font;
            if(text.name=="Label" && text.text.StartsWith("Current weapon",System.StringComparison.Ordinal) && text.transform.parent && text.transform.parent.name=="Sahur Equipment and Skills UI")AddWeaponBackdrop(text);
        }
        foreach(var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(text.gameObject.scene==gameObject.scene && SecondIslandBlackboards.Font && text.font!=SecondIslandBlackboards.Font)text.font=SecondIslandBlackboards.Font;
        foreach(var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(image.gameObject.scene!=gameObject.scene || !Panels.Contains(image.name))continue;
            var canvas=image.GetComponentInParent<Canvas>(true);if(!canvas || canvas.renderMode==RenderMode.WorldSpace)continue;
            var c=image.color;image.color=new Color(.025f,.09f,.075f,c.a);
            var outline=image.GetComponent<Outline>();if(!outline)outline=image.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(.42f,.24f,.14f,.95f);outline.effectDistance=new Vector2(3,-3);
        }
    }
    static void AddWeaponBackdrop(Text text)
    {
        var parent=text.transform.parent;if(parent.Find("Weapon chalkboard"))return;
        var obj=new GameObject("Weapon chalkboard",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Outline));obj.transform.SetParent(parent,false);
        var src=text.rectTransform;var rect=(RectTransform)obj.transform;rect.anchorMin=src.anchorMin;rect.anchorMax=src.anchorMax;rect.pivot=src.pivot;
        rect.anchoredPosition=src.anchoredPosition;rect.sizeDelta=src.sizeDelta+new Vector2(20,14);rect.localRotation=src.localRotation;rect.localScale=src.localScale;
        obj.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
        var image=obj.GetComponent<Image>();image.color=new Color(.025f,.09f,.075f,.9f);image.raycastTarget=false;
        var outline=obj.GetComponent<Outline>();outline.effectColor=new Color(.42f,.24f,.14f,.95f);outline.effectDistance=new Vector2(3,-3);
        obj.AddComponent<SecondIslandWeaponBackdrop>().source=text;
    }
}

/// <summary>Keep the weapon caption's backing aligned and visible with the caption.</summary>
public sealed class SecondIslandWeaponBackdrop : MonoBehaviour
{
    public Text source;
    void LateUpdate()
    {
        var image=GetComponent<Image>();bool show=source && source.isActiveAndEnabled;
        if(image.enabled!=show)image.enabled=show;
        var outline=GetComponent<Outline>();if(outline && outline.enabled!=show)outline.enabled=show;
        if(!show)return;
        var s=source.rectTransform;var r=(RectTransform)transform;
        if(r.anchorMin!=s.anchorMin || r.anchorMax!=s.anchorMax || r.anchoredPosition!=s.anchoredPosition || r.sizeDelta!=s.sizeDelta+new Vector2(20,14))
        {r.anchorMin=s.anchorMin;r.anchorMax=s.anchorMax;r.pivot=s.pivot;r.anchoredPosition=s.anchoredPosition;r.sizeDelta=s.sizeDelta+new Vector2(20,14);}
    }
}

/// <summary>Replaces a world-space floating caption with a dimensional sign and 3D text mesh.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandBlackboardLabel : MonoBehaviour
{
    Text source;
    RectTransform sourceRect;
    TextMeshPro chalk;
    Transform frame,surface;
    BoxCollider hitArea;
    string shown;
    int oldFontSize=-1;
    Vector2 oldSize;
    public TextMeshPro ChalkText => chalk;
    void Awake()
    {
        source=GetComponentInChildren<Text>(true);sourceRect=transform as RectTransform;
        if(!source || !sourceRect || !SecondIslandBlackboards.Font){enabled=false;return;}
        hitArea=gameObject.AddComponent<BoxCollider>();hitArea.isTrigger=true;
        frame=SecondIslandBlackboards.Part("3D blackboard frame",transform,SecondIslandBlackboards.Wood,false).transform;
        surface=SecondIslandBlackboards.Part("3D blackboard slate",transform,SecondIslandBlackboards.Slate,false).transform;
        chalk=SecondIslandBlackboards.Chalk(transform);
        source.enabled=false;
        Refresh();
    }
    void OnEnable(){if(source)Refresh();}
    void LateUpdate()
    {
        if(!source || !sourceRect || !chalk)return;
        if(shown!=source.text || oldFontSize!=source.fontSize || oldSize!=sourceRect.sizeDelta)Refresh();
    }
    void Refresh()
    {
        if(!source || !chalk)return;
        shown=source.text ?? "";oldFontSize=source.fontSize;oldSize=sourceRect.sizeDelta;
        int lines=1,longest=0,count=0;foreach(char c in shown){if(c=='\n'){longest=Mathf.Max(longest,count);count=0;lines++;}else count++;}longest=Mathf.Max(longest,count);
        float width=Mathf.Clamp(longest*oldFontSize*.63f+75,180,Mathf.Max(180,oldSize.x));
        float height=Mathf.Clamp(lines*oldFontSize*1.35f+42,75,Mathf.Max(75,oldSize.y));
        frame.localPosition=new Vector3(0,0,4);frame.localScale=new Vector3(width,height,10);
        hitArea.center=new Vector3(0,0,4);hitArea.size=new Vector3(width,height,10);
        surface.localPosition=new Vector3(0,0,-1.2f);surface.localScale=new Vector3(width-14,height-14,1);
        chalk.transform.localPosition=new Vector3(0,0,-3);chalk.rectTransform.sizeDelta=new Vector2(width-30,height-24);
        // World-space TextMeshPro units are one tenth of legacy UI points.
        // Let auto-sizing use the full board instead of drawing tiny letters.
        chalk.fontSizeMin=Mathf.Max(4,oldFontSize*4f);
        chalk.fontSizeMax=Mathf.Max(12,oldFontSize*12f);
        chalk.fontSize=chalk.fontSizeMax;chalk.text=shown;
    }
}

/// <summary>Gives authored scene lettering a matching 3D font and a board where needed.</summary>
[DisallowMultipleComponent]
public sealed class SecondIslandAuthoredChalk : MonoBehaviour
{
    void Awake()
    {
        var source=GetComponent<TextMesh>();var renderer=GetComponent<Renderer>();
        if(!source || !renderer || !SecondIslandBlackboards.Font)return;
        var bounds=renderer.bounds;var scale=transform.lossyScale;
        float width=Mathf.Max(.4f,bounds.size.x/Mathf.Max(.001f,Mathf.Abs(scale.x)));
        float height=Mathf.Max(.3f,bounds.size.y/Mathf.Max(.001f,Mathf.Abs(scale.y)));
        if(!GetComponent<Collider>())
        {
            var hit=gameObject.AddComponent<BoxCollider>();hit.isTrigger=true;
            hit.size=new Vector3(width*1.3f,height*1.8f,.16f/Mathf.Max(.001f,Mathf.Abs(scale.z)));
        }
        var chalk=SecondIslandBlackboards.Chalk(transform);chalk.rectTransform.sizeDelta=new Vector2(width*1.08f,height*1.15f);
        chalk.fontSize=4;chalk.fontSizeMax=30;chalk.text=source.text;
        chalk.transform.localPosition=new Vector3(0,0,-.012f/Mathf.Max(.001f,Mathf.Abs(scale.z)));
        if(transform.parent && transform.parent.name=="Sign board")
        {
            var board=transform.parent.GetComponent<Renderer>();if(board)board.sharedMaterial=SecondIslandBlackboards.Slate;
        }
        else if(!transform.parent || transform.parent.name!="Marshmallow Challenge Blackboard")
        {
            var frame=SecondIslandBlackboards.Part("3D blackboard frame",transform,SecondIslandBlackboards.Wood,true).transform;
            var slate=SecondIslandBlackboards.Part("3D blackboard slate",transform,SecondIslandBlackboards.Slate,false).transform;
            float depth=.16f/Mathf.Max(.001f,Mathf.Abs(scale.z));
            frame.localPosition=new Vector3(0,0,depth*.55f);frame.localScale=new Vector3(width*1.3f,height*1.8f,depth);
            slate.localPosition=new Vector3(0,0,depth*.02f);slate.localScale=new Vector3(width*1.19f,height*1.55f,depth*.08f);
        }
        renderer.enabled=false;
    }
}
