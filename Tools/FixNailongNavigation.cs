using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class FixNailongNavigation
{
    public static object Fix()
    {
        if (Application.isPlaying) throw new Exception("Fix outside Play Mode.");
        const string path = "Assets/Game/Prefabs/Bosses/Nailong/Nailong.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // Enabling before a NavMesh exists emits a native engine warning before AI.Awake.
            var agent = root.GetComponent<NavMeshAgent>();
            if (agent == null) throw new Exception("Missing Nailong agent.");
            agent.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { path, initialAgentEnabled = false, enablesOnValidNavMesh = true };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
