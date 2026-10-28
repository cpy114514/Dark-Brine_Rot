using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AutoQaIslandValidationRegression
{
    const string SetupKey = "AUTO_QA.Batch002.SceneSetup";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message, List<string> checks) { if (!condition) throw new Exception(message); checks.Add(message); }
    static object Write(string name, object value) { Directory.CreateDirectory("AUTO_QA/Evidence"); File.WriteAllText("AUTO_QA/Evidence/" + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented)); return value; }

    public static object PrepareIsolatedEditor()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before changing workspace");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new Exception("Preserve unsaved user scene work");
        if (!string.IsNullOrEmpty(SessionState.GetString(SetupKey, ""))) throw new Exception("Existing QA scene setup must be restored first");
        SessionState.SetString(SetupKey, Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        return new { isolated = true, priorSetupSaved = true };
    }

    public static object RestoreWorkspace()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before restoring workspace");
        string json = SessionState.GetString(SetupKey, "");
        if (string.IsNullOrEmpty(json)) throw new Exception("No recorded scene setup");
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(json));
        SessionState.EraseString(SetupKey);
        return new { restored = true, scenes = SceneManager.sceneCount };
    }

    public static object Run(bool expectDeferred)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        var go = new GameObject("AUTO_QA Island Validation");
        go.SetActive(false);
        var warnings = new List<string>();
        var checks = new List<string>();
        Application.LogCallback receive = (message, stack, level) => { if (message.Contains("SendMessage cannot be called")) warnings.Add(message); };
        try
        {
            var island = go.AddComponent<ProceduralIsland>();
            island.radialSegments = 8; island.rings = 4; island.shorelineRadius = 10;
            go.SetActive(true);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Check(mesh.vertexCount == 33, "Fixture starts with expected 8x4 terrain geometry", checks);
            var foam = go.transform.Find("Procedural Shore Foam");
            Check(foam != null, "Shore foam shader and generated child present", checks);
            UnityEngine.Object.DestroyImmediate(foam.gameObject);
            Application.logMessageReceived += receive;
            var serialized = new SerializedObject(island);
            serialized.FindProperty("radialSegments").intValue = 12;
            serialized.FindProperty("shorelineRadius").floatValue = 14;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            int immediateVertices = mesh.vertexCount;
            if (expectDeferred)
            {
                Check(immediateVertices == 33, "Native OnValidate leaves terrain unchanged until normal update", checks);
                Check(go.transform.Find("Procedural Shore Foam") == null, "Native OnValidate creates no child objects", checks);
            }
            else Check(immediateVertices == 49, "Reproduced synchronous terrain rebuild inside native OnValidate", checks);
            typeof(ProceduralIsland).GetMethod("LateUpdate", Flags).Invoke(island, null);
            Check(mesh.vertexCount == 49, "Updated 12x4 terrain geometry appears by LateUpdate", checks);
            Check(go.GetComponent<MeshCollider>().sharedMesh == mesh, "Physics and render mesh remain synchronized", checks);
            Check(go.GetComponentsInChildren<MeshRenderer>().Count(r => r.name == "Procedural Shore Foam") == 1, "Exactly one regenerated foam child", checks);
            var vertices = mesh.vertices;
            var foamMesh = go.transform.Find("Procedural Shore Foam").GetComponent<MeshFilter>().sharedMesh;
            var foamVertices = foamMesh.vertices;
            for (int i = 0; i < 5; i++)
            {
                var same = new SerializedObject(island);
                same.FindProperty("textureTiling").floatValue = island.textureTiling + .001f;
                same.ApplyModifiedPropertiesWithoutUndo();
            }
            typeof(ProceduralIsland).GetMethod("LateUpdate", Flags).Invoke(island, null);
            Check(mesh.vertices.SequenceEqual(vertices), "Material tiling updates preserve terrain vertex positions", checks);
            Check(foamMesh.vertices.SequenceEqual(foamVertices), "Material tiling updates preserve shore vertex positions", checks);
            Check(go.GetComponentsInChildren<MeshRenderer>().Count(r => r.name == "Procedural Shore Foam") == 1, "Repeated validations do not duplicate foam children", checks);
            if (expectDeferred) Check(warnings.Count == 0, "No OnValidate SendMessage lifecycle warnings", checks);
            return Write(expectDeferred ? "island-validation-after" : "island-validation-before", new { passed = true, expectDeferred, immediateVertices, warnings, checks });
        }
        finally
        {
            Application.logMessageReceived -= receive;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
