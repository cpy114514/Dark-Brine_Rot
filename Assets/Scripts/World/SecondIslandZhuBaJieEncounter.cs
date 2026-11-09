using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Spawns the combat prefab at the authored placement or the ridge fallback.</summary>
public static class SecondIslandZhuBaJieEncounter
{
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    const string ResourcePath="SecondIslandZhuBaJie/ZhuBaJie Boss";
    // Use the position selected for the original visual model if a scene has
    // not yet saved that instance; existing placed models take precedence.
    static readonly Vector3 Position=new Vector3(-105f,2469.80f,1755f);
    static readonly Quaternion Rotation=Quaternion.Euler(0,185.12f,0);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initial(){for(int i=0;i<SceneManager.sceneCount;i++)Install(SceneManager.GetSceneAt(i));}
    static void OnLoaded(Scene scene,LoadSceneMode mode)=>Install(scene);
    static void Install(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        var roots=scene.GetRootGameObjects();
        foreach(var root in roots)if(root.GetComponentInChildren<ZhuBaJieBoss>(true))return;
        ZhuBaJieVisualOnly placed=null;
        foreach(var root in roots)
        {
            foreach(var candidate in root.GetComponentsInChildren<ZhuBaJieVisualOnly>(true))
                if(candidate.isActiveAndEnabled && !candidate.GetComponentInParent<ZhuBaJieBoss>()){placed=candidate;break;}
            if(placed)break;
        }
        var prefab=Resources.Load<GameObject>(ResourcePath);
        if(!prefab){Debug.LogError("Zhu Bajie encounter prefab was not found in Resources.");return;}
        // The visual-only model faces left in local space; the combat prefab's
        // child has a 90-degree correction so preserve the visible direction.
        var position=placed?placed.transform.position:Position;
        var rotation=placed?placed.transform.rotation*Quaternion.Euler(0,-90,0):Rotation;
        var scale=placed?placed.transform.lossyScale:Vector3.one;
        if(placed)placed.gameObject.SetActive(false);
        var boss=Object.Instantiate(prefab,position,rotation);
        boss.transform.localScale=scale;
        SceneManager.MoveGameObjectToScene(boss,scene);
    }
}
