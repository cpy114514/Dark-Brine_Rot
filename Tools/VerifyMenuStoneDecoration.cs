using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class VerifyMenuStoneDecoration
{
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    public static object Run()
    {
        Canvas.ForceUpdateCanvases();
        var stone=UnityEngine.Object.FindObjectsByType<SahurDecorationHover>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Single(d=>d.name=="Stone Decoration" && d.gameObject.scene.name=="MainMenu");
        var image=stone.GetComponent<Image>();
        var rect=(RectTransform)stone.transform;
        var data=new PointerEventData(EventSystem.current);
        stone.OnPointerExit(data);
        Check(image.sprite==stone.normalSprite,"Default stone must be uncolored.");
        stone.OnPointerEnter(data);
        Check(image.sprite==(stone.alignHoverInMaterial ? stone.normalSprite : stone.hoverSprite) && image.material==stone.hoverMaterial,"Colored hover and contrast missing.");
        stone.OnPointerExit(data);
        Check(image.sprite==stone.normalSprite && image.material!=stone.hoverMaterial,"Pointer exit did not restore original stone.");
        Func<float,float,Vector2> point=(u,v)=>RectTransformUtility.WorldToScreenPoint(null,
            rect.TransformPoint(new Vector3(rect.rect.xMin+u*rect.rect.width,rect.rect.yMin+v*rect.rect.height,0)));
        Check(stone.IsRaycastLocationValid(point(.5f,.5f),null),"Solid stone does not respond to the mouse.");
        Check(!stone.IsRaycastLocationValid(point(.01f,.01f),null),"Transparent stone background blocks the mouse.");
        Check(!stone.IsRaycastLocationValid(point(.5f,.98f),null),"Aspect-ratio padding blocks the mouse.");
        Check(image.preserveAspect,"Artwork aspect ratio is not preserved.");
        bool buttonRaycastsTested=Application.isPlaying;
        if(buttonRaycastsTested)
        {
            var menu=stone.transform.parent;
            var raycaster=menu.GetComponent<GraphicRaycaster>();
            foreach(var name in new[]{"New Game","Settings","Quit"})
            {
                var button=menu.Find(name).GetComponent<Button>();
                var buttonRect=(RectTransform)button.transform;
                data.position=RectTransformUtility.WorldToScreenPoint(null,buttonRect.TransformPoint(buttonRect.rect.center));
                var hits=new List<RaycastResult>(); raycaster.Raycast(data,hits);
                Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Decoration covers button: "+name);
            }
        }
        return new { normal=true,hover=true,restored=true,transparentPasses=true,paddingPasses=true,
            originalAspect=true,buttonRaycastsTested,buttonsClear=true };
    }
}
