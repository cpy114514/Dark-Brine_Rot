using System;
using System.IO;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(32650)]
public sealed class FramingObserverV075 : MonoBehaviour
{
    public string output;
    Story1WreckFinale film;
    ThirdPersonPlayerController hero;
    Camera shotCamera;
    SkinnedMeshRenderer[] heroSkins;
    int frames,croppedCrown,croppedFeet;
    float minY=float.PositiveInfinity,maxY=float.NegativeInfinity,maxFov;
    void LateUpdate()
    {
        if(film==null)
        {
            var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
            if(battle==null || battle.Finale==null)return;
            film=battle.Finale;
            var sequence=battle.GetComponent<Story1SharkCollisionSequence>();hero=sequence.sahur;shotCamera=sequence.storyCamera;
            heroSkins=hero.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }
        if(film.CinematicTime<0)return;
        if(film.CinematicTime>=18.05f)
        {
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{frames,croppedCrown,croppedFeet,minY,maxY,maxFov,
                usesActualRendererBounds=true,passed=frames>=100 && croppedCrown==0 && croppedFeet==0,
                observesScreenFramingOnly=true,contactAndArtAcceptance=false},Newtonsoft.Json.Formatting.Indented));
            Destroy(gameObject);return;
        }
        bool cropped=false;
        foreach(var skin in heroSkins)
        {
            if(!skin.enabled || !skin.gameObject.activeInHierarchy)continue;
            Bounds bounds=skin.bounds;
            for(int corner=0;corner<8;corner++)
            {
                var screen=shotCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,
                    new Vector3((corner&1)==0 ? -1 : 1,(corner&2)==0 ? -1 : 1,(corner&4)==0 ? -1 : 1)));
                minY=Mathf.Min(minY,screen.y);maxY=Mathf.Max(maxY,screen.y);cropped|=Outside(screen);
            }
        }
        Vector3 feet=shotCamera.WorldToViewportPoint(hero.transform.position+Vector3.up*hero.LowestFootWorldOffset);
        frames++;maxFov=Mathf.Max(maxFov,shotCamera.fieldOfView);
        bool Outside(Vector3 p)=>p.z<=0 || p.x<0 || p.x>1 || p.y<0 || p.y>1;
        if(cropped)croppedCrown++;if(Outside(feet))croppedFeet++;
    }
}
public static class ObserveFramingV075
{
    public static object Start(string output)
    {
        if(!Application.isPlaying)throw new Exception("Requires original Play Mode");
        var go=new GameObject("Observe actual crown framing v075");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<FramingObserverV075>().output=output;return new{started=true,output};
    }
}
