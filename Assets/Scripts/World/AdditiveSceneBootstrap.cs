using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Loads the world scenes required by the lightweight Main scene.</summary>
    public sealed class AdditiveSceneBootstrap : MonoBehaviour
    {
        static int activeLoads;
        static float progress;
        bool loading;
        public static bool IsLoading => activeLoads > 0;
        public static float Progress => IsLoading?progress:1f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLoading() {activeLoads=0;progress=0;}
        void OnEnable() { if(Application.isPlaying && !loading) {if(activeLoads==0)progress=0;loading=true;activeLoads++;SceneLoadingScreen.ShowWorldLoading();} }
        void OnDisable() => FinishLoading();
        void FinishLoading() { if(!loading)return;loading=false;activeLoads=Mathf.Max(0,activeLoads-1); }
        [SerializeField]
        string[] requiredScenes =
        {
            "Environment",
            "Gameplay",
            "Lighting",
            "Enemies"
        };

        [SerializeField]
        string activeSceneAfterLoad = "Lighting";

        IEnumerator Start()
        {
            int completed=0,total=Mathf.Max(1,requiredScenes.Length);
            foreach (string sceneName in requiredScenes)
            {
                if (string.IsNullOrWhiteSpace(sceneName) || IsLoaded(sceneName))
                {progress=(float)++completed/total;continue;}

                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.LogError($"[Scenes] Required scene '{sceneName}' is not in Build Settings.", this);
                    progress=(float)++completed/total;
                    continue;
                }

                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load != null)
                    while(!load.isDone)
                    {
                        progress=(completed+Mathf.Clamp01(load.progress/.9f)*.95f)/total;
                        yield return null;
                    }
                progress=(float)++completed/total;
            }

            Scene activeScene = SceneManager.GetSceneByName(activeSceneAfterLoad);
            if (activeScene.IsValid() && activeScene.isLoaded)
                SceneManager.SetActiveScene(activeScene);
            FinishLoading();
        }

        static bool IsLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == sceneName)
                    return true;
            }

            return false;
        }
    }
}
