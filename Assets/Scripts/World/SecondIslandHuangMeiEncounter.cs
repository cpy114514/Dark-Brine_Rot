using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Places Huang Mei on the west ridge unless a designer has placed the prefab already.</summary>
public static class SecondIslandHuangMeiEncounter
{
    public const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    public const string PrefabResource="SecondIslandHuangMei/Huang Mei Boss";
    static readonly Vector2 Ridge=new Vector2(-185f,1850f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initial(){for(int i=0;i<SceneManager.sceneCount;i++)Install(SceneManager.GetSceneAt(i));}
    static void OnLoaded(Scene scene,LoadSceneMode mode)=>Install(scene);

    static void Install(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        MarshmallowIslandExperience experience=null;
        foreach(var root in scene.GetRootGameObjects())
        {
            if(root.GetComponentInChildren<HuangMeiBoss>(true))return;
            if(!experience)experience=root.GetComponentInChildren<MarshmallowIslandExperience>(true);
        }
        var prefab=Resources.Load<GameObject>(PrefabResource);
        var surface=experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if(!prefab || !surface)return;
        var bounds=surface.bounds;
        var ray=new Ray(new Vector3(Ridge.x,bounds.max.y+2f,Ridge.y),Vector3.down);
        if(!surface.Raycast(ray,out var ground,bounds.size.y+4f))return;
        var boss=Object.Instantiate(prefab,ground.point+Vector3.up*.05f,Quaternion.Euler(0,148,0));
        boss.name="Huang Mei - West Ridge";
        SceneManager.MoveGameObjectToScene(boss,scene);
    }
}
