using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public static class AutoQaSandFootprintsRegression
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Key = "AUTO_QA.Batch003.Sand";
    static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static void Check(bool ok, string text, List<string> checks) { if (!ok) throw new Exception(text); checks.Add(text); }
    static object Write(string name, object data) { Directory.CreateDirectory("AUTO_QA/Evidence"); File.WriteAllText("AUTO_QA/Evidence/" + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented)); return data; }
    static void Born(Array entries, int index, float born) { var value = entries.GetValue(index); value.GetType().GetField("born").SetValue(value, born); entries.SetValue(value, index); }
    static Transform Root(Array entries, int index) => (Transform)entries.GetValue(index).GetType().GetField("transform").GetValue(entries.GetValue(index));

    public static object Prepare(string label)
    {
        if (!Application.isPlaying || SceneManager.sceneCount != 1 || !string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new Exception("Requires isolated unsaved Play scene");
        if (GameObject.Find("AUTO_QA Sand Footprints")) throw new Exception("Existing fixture must be cleaned first");
        var go = new GameObject("AUTO_QA Sand Footprints");
        var component = go.AddComponent<SandFootprints>();
        component.lifetime = 120;
        var entries = (Array)Get(component, "stamps");
        if (entries == null || entries.Length != 28) throw new Exception("Fresh footprint pool did not initialize");
        for (int i = 0; i < 2; i++) { Born(entries, i, Time.time); Root(entries, i).gameObject.SetActive(true); }
        Set(component, "cursor", 7);
        int[] ids = Enumerable.Range(0, entries.Length).Select(i => Root(entries, i).gameObject.GetInstanceID()).ToArray();
        SessionState.SetInt(Key + ".Owner", component.GetInstanceID());
        SessionState.SetInt(Key + ".Mesh", ((Mesh)Get(component, "oval")).GetInstanceID());
        SessionState.SetInt(Key + ".Material", ((Material)Get(component, "material")).GetInstanceID());
        SessionState.SetString(Key + ".Ids", string.Join(",", ids));
        SessionState.SetString(Key + ".Label", label);
        return Write("sand-reload-" + label + "-before", new { initialized = true, frame = Time.frameCount, ids, cursor = 7, activePrints = 2 });
    }

    public static object AfterReload(bool expectHealthy)
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode");
        var component = EditorUtility.EntityIdToObject(SessionState.GetInt(Key + ".Owner", 0)) as SandFootprints;
        if (!component) throw new Exception("Fixture component did not survive");
        var entries = (Array)Get(component, "stamps");
        string label = SessionState.GetString(Key + ".Label", "unknown");
        if (entries == null)
        {
            component.enabled = false;
            return Write("sand-reload-" + label + "-after", new { passed = false, reproduced = !expectHealthy, cause = "Native children survived; nonserializable Stamp[] cache became null on actual domain reload", children = component.transform.childCount, enabled = component.enabled });
        }
        var checks = new List<string>();
        Check(expectHealthy, "Cache preservation expected after repair", checks);
        Check(entries.Length == 28, "All 28 stamp entries preserved across real domain reload", checks);
        var expected = SessionState.GetString(Key + ".Ids", "").Split(',').Select(int.Parse).ToArray();
        Check(Enumerable.Range(0, entries.Length).Select(i => Root(entries, i).gameObject.GetInstanceID()).SequenceEqual(expected), "All native stamp identities preserved", checks);
        Check(((Mesh)Get(component, "oval")).GetInstanceID() == SessionState.GetInt(Key + ".Mesh", 0), "Native shared stamp mesh reused", checks);
        Check(((Material)Get(component, "material")).GetInstanceID() == SessionState.GetInt(Key + ".Material", 0), "Native shared stamp material reused", checks);
        Check((int)Get(component, "cursor") == 7, "Next pool slot preserved", checks);
        Check(Root(entries, 0).gameObject.activeSelf && Root(entries, 1).gameObject.activeSelf, "Unexpired prints survive reload", checks);
        Check(Mathf.Abs(component.lifetime - 120) < .001f, "Footprint lifetime preserved", checks);
        Born(entries, 0, Time.time - component.lifetime * .85f);
        Born(entries, 1, Time.time - component.lifetime - 1);
        typeof(SandFootprints).GetMethod("Fade", Private).Invoke(component, null);
        Check(Root(entries, 0).gameObject.activeSelf, "Fading stamp remains active", checks);
        Check(!Root(entries, 1).gameObject.activeSelf, "Expired stamp deactivates", checks);
        var block = new MaterialPropertyBlock();
        Root(entries, 0).GetComponent<MeshRenderer>().GetPropertyBlock(block);
        float expectedAlpha = .62f * (1f - (.85f - .72f) / .28f);
        Check(Mathf.Abs(block.GetColor("_Color").a - expectedAlpha) < .0001f, "Original fade curve still controls renderer alpha", checks);
        for (int i = 0; i < 5; i++) { component.enabled = false; component.enabled = true; }
        Check(component.GetComponentsInChildren<MeshRenderer>(true).Length == 28, "Repeated re-enable keeps pool bounded at 28", checks);
        Check(Enumerable.Range(0, entries.Length).Select(i => Root(entries, i).gameObject.GetInstanceID()).SequenceEqual(expected), "Repeated re-enable preserves pool identities", checks);
        return Write("sand-reload-" + label + "-after", new { passed = true, frame = Time.frameCount, checks });
    }

    public static object Cleanup()
    {
        var go = GameObject.Find("AUTO_QA Sand Footprints");
        if (go) UnityEngine.Object.DestroyImmediate(go);
        SessionState.EraseInt(Key + ".Owner");
        SessionState.EraseInt(Key + ".Mesh");
        SessionState.EraseInt(Key + ".Material");
        SessionState.EraseString(Key + ".Ids");
        SessionState.EraseString(Key + ".Label");
        return new { cleaned = true };
    }
}
