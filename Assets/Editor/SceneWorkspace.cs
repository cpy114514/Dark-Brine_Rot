using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Mavis.EditorTools
{
    public static class SceneWorkspace
    {
        const string Main = "Assets/Scenes/First Island/Main.unity";
        const string Environment = "Assets/Scenes/First Island/Environment.unity";
        const string Gameplay = "Assets/Scenes/First Island/Gameplay.unity";
        const string Lighting = "Assets/Scenes/First Island/Lighting.unity";
        const string Enemies = "Assets/Scenes/First Island/Enemies.unity";

        [MenuItem("Tools/Scenes/Open Full Workspace")]
        public static void OpenFullWorkspace()
        {
            OpenWorkspace(Gameplay, Environment, Gameplay, Lighting, Enemies);
        }

        [MenuItem("Tools/Scenes/Open Environment Workspace")]
        public static void OpenEnvironmentWorkspace()
        {
            OpenWorkspace(Environment, Environment, Lighting);
        }

        [MenuItem("Tools/Scenes/Open Gameplay Workspace")]
        public static void OpenGameplayWorkspace()
        {
            OpenWorkspace(Gameplay, Environment, Gameplay, Lighting);
        }

        static void OpenWorkspace(string activeScenePath, params string[] additiveScenePaths)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(Main, OpenSceneMode.Single);
            foreach (string path in additiveScenePaths)
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            Scene activeScene = SceneManager.GetSceneByPath(activeScenePath);
            if (activeScene.IsValid())
                SceneManager.SetActiveScene(activeScene);
        }
    }
}
