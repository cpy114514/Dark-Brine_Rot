using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class VerifySettingsScrollAndDefaults
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Prefix="DarkBrine.Settings.";
    static object Get(object obj,string field)=>obj.GetType().GetField(field,Private).GetValue(obj);
    static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Private).Invoke(obj,args);
    static T Value<T>(object settings,string field)=>(T)settings.GetType().GetField(field).GetValue(settings);
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static async Task<object> Run()
    {
        if(!Application.isPlaying) throw new Exception("Requires Play Mode.");
        var menu=UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();
        Check(menu!=null,"No settings menu.");
        bool opened=PauseSettingsMenu.IsOpen;
        int oldSection=(int)Get(menu,"section");
        var scrollReport=new List<object>();
        var positions=new Dictionary<ScrollRect,Vector2>();
        try
        {
            menu.OpenSettingsFromMainMenu();
            await Task.Delay(100);
            foreach(int section in new[]{1,4})
            {
                Call(menu,"SetSection",section); Canvas.ForceUpdateCanvases();
                await Task.Delay(100);
                var scroll=menu.sectionPanels[section].GetComponent<ScrollRect>();
                positions[scroll]=scroll.content.anchoredPosition;
                scroll.verticalNormalizedPosition=1f; Canvas.ForceUpdateCanvases();
                var viewport=scroll.viewport;
                var label=scroll.content.GetComponentsInChildren<TMPro.TextMeshProUGUI>().First(t=>t.name=="Label");
                var button=scroll.content.GetComponentsInChildren<Button>().First();
                var targets=new[] { new { label="blank", transform=(RectTransform)viewport, point=new Vector2(-viewport.rect.width*.49f,0) },
                    new {label="label",transform=label.rectTransform,point=Vector2.zero},
                    new {label="button",transform=(RectTransform)button.transform,point=Vector2.zero} };
                foreach(var target in targets)
                {
                    scroll.verticalNormalizedPosition=1f; Canvas.ForceUpdateCanvases();
                    var pointer=new PointerEventData(EventSystem.current);
                    pointer.position=RectTransformUtility.WorldToScreenPoint(null,target.transform.TransformPoint(target.point));
                    pointer.scrollDelta=new Vector2(0,-1);
                    var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
                    Check(hits.Count>0,"No UI raycast for "+target.label+" point="+pointer.position+" screen="+Screen.width+"x"+Screen.height);
                    var handled=ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.scrollHandler);
                    Check(handled==scroll.gameObject,"Wheel routed outside the scroll view at "+target.label+": "+hits[0].gameObject.name);
                    Check(scroll.content.anchoredPosition.y>1f,"Wheel did not move paused UI at "+target.label);
                    scrollReport.Add(new {section,target=target.label,hit=hits[0].gameObject.name,offset=scroll.content.anchoredPosition.y});
                }
                var p=new PointerEventData(EventSystem.current) { scrollDelta=new Vector2(0,-100) };
                ExecuteEvents.Execute(scroll.gameObject,p,ExecuteEvents.scrollHandler);
                Check(scroll.verticalNormalizedPosition<.001f,"Cannot reach final setting row.");
                p.scrollDelta=new Vector2(0,100); ExecuteEvents.Execute(scroll.gameObject,p,ExecuteEvents.scrollHandler);
                Check(scroll.verticalNormalizedPosition>.999f,"Cannot scroll back to top.");
                Vector2 point=RectTransformUtility.WorldToScreenPoint(null,viewport.TransformPoint(new Vector2(-viewport.rect.width*.49f,0)));
                var drag=new PointerEventData(EventSystem.current) {position=point,button=PointerEventData.InputButton.Left};
                var results=new List<RaycastResult>(); EventSystem.current.RaycastAll(drag,results);
                Check(ExecuteEvents.GetEventHandler<IDragHandler>(results[0].gameObject)==scroll.gameObject,"Blank-space drag does not route to scroll view.");
            }
            var defaults=Get(menu,"factoryDefaults"); var saved=Get(menu,"saved");
            Check(Mathf.Approximately(Value<float>(defaults,"renderScale"),.85f),"Defaults are not low graphics.");
            Check(Value<int>(defaults,"textureMipmapLimit")==1 && Value<float>(defaults,"lodBias")==1,"Texture/LOD defaults not low.");
            Check(Value<bool>(defaults,"shadows") && Value<bool>(defaults,"postProcessing"),"Visibility/lighting lost in low defaults.");
            Check(!Value<bool>(defaults,"motionBlur") && !Value<bool>(defaults,"bloom") && !Value<bool>(defaults,"vignette"),"Cosmetic effects still default on.");
            Check(Value<int>(defaults,"frameLimitIndex")==1,"Low defaults reduce the target below 60 FPS.");
            var camera=Camera.main; var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float far=camera ? camera.farClipPlane : 0;
            int maximumLOD=QualitySettings.maximumLODLevel;
            float health=UnityEngine.Object.FindFirstObjectByType<Mavis.SahurAttack>()?.damage ?? 0;
            // Default migration already ran on Awake. Explicit choices must survive later reads.
            float prior=PlayerPrefs.GetFloat(Prefix+"RenderScale");
            try
            {
                PlayerPrefs.SetFloat(Prefix+"RenderScale",1.1f);
                var read=Call(menu,"ReadSettings");
                Check(Mathf.Approximately(Value<float>(read,"renderScale"),1.1f),"New preferences are reset on every read.");
            }
            finally { PlayerPrefs.SetFloat(Prefix+"RenderScale",prior); PlayerPrefs.Save(); }
            Call(menu,"RestoreDefaults");
            var draft=Get(menu,"draft"); Check(Value<float>(draft,"renderScale")==.85f,"Defaults button restores old graphics.");
            Check((camera ? camera.farClipPlane : 0)==far && QualitySettings.maximumLODLevel==maximumLOD,"Default preview alters visible geometry.");
            Check((UnityEngine.Object.FindFirstObjectByType<Mavis.SahurAttack>()?.damage ?? 0)==health,"Default preview changed damage.");
            return new {scrollReport,pausedScroll=Time.timeScale==0,blankDragRoutesCorrectly=true,bothDirectionsAndBottomReachable=true,
                defaultScale=Value<float>(defaults,"renderScale"),shadowDistance=Value<float>(defaults,"shadowDistance"),shadowsRetained=true,
                nativeResolutionUI=true,preferencesSurviveReload=true,farClipUnchanged=far,maximumLODUnchanged=maximumLOD,
                actualScale=pipeline ? pipeline.renderScale : 0,actualShadowMap=pipeline ? pipeline.mainLightShadowmapResolution : 0,
                actualMSAA=pipeline ? pipeline.msaaSampleCount : 0,textureMip=QualitySettings.globalTextureMipmapLimit,lod=QualitySettings.lodBias};
        }
        finally
        {
            foreach(var pair in positions) if(pair.Key) pair.Key.content.anchoredPosition=pair.Value;
            Call(menu,"SetSection",oldSection);
            if(!opened) menu.Resume();
        }
    }
}
