using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mavis
{
    /// <summary>Loads the world scenes required by the lightweight Main scene.</summary>
    public sealed class AdditiveSceneBootstrap : MonoBehaviour
    {
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
            foreach (string sceneName in requiredScenes)
            {
                if (string.IsNullOrWhiteSpace(sceneName) || IsLoaded(sceneName))
                    continue;

                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.LogError($"[Scenes] Required scene '{sceneName}' is not in Build Settings.", this);
                    continue;
                }

                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load != null)
                    yield return load;
            }

            Scene activeScene = SceneManager.GetSceneByName(activeSceneAfterLoad);
            if (activeScene.IsValid() && activeScene.isLoaded)
                SceneManager.SetActiveScene(activeScene);
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
