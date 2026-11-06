using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Places the reusable cottage on the second island if the scene has no instance.</summary>
[DisallowMultipleComponent]
public sealed class CandyHouseMarker : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/Second Island/Second Island.unity";
    const string PrefabPath = "SecondIslandCandyHouse/Candy Cottage";
    static readonly Vector3 DefaultPosition = new Vector3(92f, 2516.22f, 1945f);
    public int CollectedHealingPacks { get; private set; }
    public bool HealingPackCollected(int index) => index >= 0 && index < 3 && (CollectedHealingPacks & (1 << index)) != 0;
    public void MarkHealingPackCollected(int index)
    {
        if (index >= 0 && index < 3) CollectedHealingPacks |= 1 << index;
    }
    public void RestoreHealingPacks(int mask)
    {
        CollectedHealingPacks = mask & 7;
        foreach (var pickup in GetComponentsInChildren<CandyHouseHealingPickup>(true))
            pickup.gameObject.SetActive(!HealingPackCollected(pickup.index));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitialScene()
    {
        for(int i=0;i<SceneManager.sceneCount;i++) EnsureHouse(SceneManager.GetSceneAt(i));
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureHouse(scene);
    static void EnsureHouse(Scene scene)
    {
        if(!scene.isLoaded || scene.path!=ScenePath)return;
        foreach(var root in scene.GetRootGameObjects())
            if(root.GetComponentInChildren<CandyHouseMarker>(true))return;
        var prefab=Resources.Load<GameObject>(PrefabPath);
        if(!prefab){Debug.LogError("Candy cottage prefab is missing from Resources.");return;}
        var instance=Instantiate(prefab,DefaultPosition,Quaternion.identity);
        instance.name="Candy Cottage";
        SceneManager.MoveGameObjectToScene(instance,scene);
    }
}
