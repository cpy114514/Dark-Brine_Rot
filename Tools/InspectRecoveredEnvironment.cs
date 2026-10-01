using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class InspectRecoveredEnvironment
{
    public static object Inspect()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        return new { islands = transforms.SelectMany(t => t.GetComponents<ProceduralIsland>()).Select(i => new { i.name, id = GlobalObjectId.GetGlobalObjectIdSlow(i).ToString() }).ToArray(),
            foam = transforms.Where(t => t.name == "Procedural Shore Foam").Select(t => new {
                path = t.parent != null ? t.parent.name + "/" + t.name : t.name, flags = t.gameObject.hideFlags.ToString(),
                mesh = t.GetComponent<MeshFilter>()?.sharedMesh?.name, shader = t.GetComponent<MeshRenderer>()?.sharedMaterial?.shader.name,
                id = GlobalObjectId.GetGlobalObjectIdSlow(t).ToString() }).ToArray(),
            missingMeshes = transforms.SelectMany(t => t.GetComponents<MeshFilter>()).Where(m => m.sharedMesh == null)
                .Select(m => new { m.name, parent = m.transform.parent?.name, id = GlobalObjectId.GetGlobalObjectIdSlow(m).ToString() }).ToArray() };
    }
}
