using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CleanupHopPreviewV086
{
    public static object CloseInterruptedPreview()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var scenes=Resources.FindObjectsOfTypeAll<Camera>()
            .Where(c=>c.name=="Common action review camera" && c.gameObject.scene.IsValid() && EditorSceneManager.IsPreviewScene(c.gameObject.scene))
            .Select(c=>c.gameObject.scene).Distinct().Where(s=>s.GetRootGameObjects().Any(g=>g.name=="SahurPlayer(Clone)")
                && s.GetRootGameObjects().Any(g=>g.name=="Actual sampled skin")).ToArray();
        foreach(var scene in scenes)EditorSceneManager.ClosePreviewScene(scene);
        return new{closedInterruptedOwnedPreviewScenes=scenes.Length,activeSceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
    }
}
