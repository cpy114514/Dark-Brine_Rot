using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Mavis;
using UnityEngine;
using UnityEditor;

public static class AutoQaLifecycleRegression
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] PoolNames = Enumerable.Range(0, CombatImpactVisuals.PulseCapacity).Select(i => "Pooled pressure ring " + i).Concat(new[] { "Weapon pressure ribbon", "Offhand pressure ribbon" }).ToArray();
    static object Field(object o, string name) => o.GetType().GetField(name, Flags).GetValue(o);
    static void Check(bool ok, string text, List<string> checks) { if (!ok) throw new Exception(text); checks.Add(text); }
    static object Write(string name, object result) { Directory.CreateDirectory("AUTO_QA/Evidence"); File.WriteAllText("AUTO_QA/Evidence/" + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented)); return result; }

    public static object MissingAi()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode");
        var go = new GameObject("AUTO_QA Missing AI Fixture");
        go.SetActive(false);
        go.transform.position = new Vector3(10000, 10000, 10000);
        try
        {
            go.AddComponent<NailongHealth>();
            var bar = go.AddComponent<NailongHealthBar>();
            go.SetActive(true);
            string error = null;
            try { bar.Refresh(.1f); } catch (Exception ex) { error = ex.ToString(); }
            var result = new { passed = error == null, error, fraction = bar.DisplayedFraction, expected = 1f, hasAi = go.GetComponent<NailongAI>() != null };
            return Write("missing-ai-" + (error == null ? "after" : "before"), result);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    public static object BeforeReload()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode");
        var visuals = UnityEngine.Object.FindFirstObjectByType<CombatImpactVisuals>();
        if (!visuals) throw new Exception("No production combat visual pool");
        var ids = PoolNames.Select(n => visuals.transform.Find(n).gameObject.GetInstanceID()).ToArray();
        SessionState.SetString("AUTO_QA.PoolIds", string.Join(",", ids));
        SessionState.SetInt("AUTO_QA.PoolOwner", visuals.GetInstanceID());
        SessionState.SetInt("AUTO_QA.ReloadFrame", Time.frameCount);
        return Write("reload-before", new { frame = Time.frameCount, ids, owner = visuals.name });
    }

    public static object AfterReload()
    {
        var checks = new List<string>();
        Check(Application.isPlaying, "Still in Play Mode", checks);
        var visuals = EditorUtility.EntityIdToObject(SessionState.GetInt("AUTO_QA.PoolOwner", 0)) as CombatImpactVisuals;
        Check(visuals != null, "Production component survives reload", checks);
        var expectedIds = SessionState.GetString("AUTO_QA.PoolIds", "").Split(',').Select(int.Parse).ToArray();
        var actualIds = PoolNames.Select(n => visuals.transform.Find(n).gameObject.GetInstanceID()).ToArray();
        Check(expectedIds.SequenceEqual(actualIds), "All ten native pool objects reused across reload", checks);
        Check(((Array)Field(visuals, "pulses")).Cast<object>().Count(x => x != null) == 8, "Eight managed pulse entries restored", checks);
        Check(((Array)Field(visuals, "ribbons")).Cast<object>().Count(x => x != null) == 2, "Both managed ribbon entries restored", checks);
        int count = visuals.GetComponentsInChildren<MeshRenderer>(true).Length;
        for (int hand = 0; hand < 2; hand++)
        {
            visuals.Sample(visuals.transform.position, visuals.transform.position + Vector3.up, true, .2f, hand);
            visuals.Sample(visuals.transform.position, visuals.transform.position + Vector3.up + Vector3.right * .1f, true, .2f, hand);
        }
        Check(((Array)Field(visuals, "ribbons")).Cast<object>().All(r => (int)r.GetType().GetField("count").GetValue(r) == 2), "Both weapon ribbons sample and build mesh", checks);
        visuals.Landing(visuals.transform.position);
        Check(visuals.ActivePulseCount > 0, "Landing pressure emits after reload", checks);
        for (int i = 0; i < 5; i++) { visuals.enabled = false; visuals.enabled = true; }
        Check(visuals.ActivePulseCount == 0, "Disable clears active pressure", checks);
        Check(visuals.GetComponentsInChildren<MeshRenderer>(true).Length == count, "Repeated re-enable does not duplicate native pool renderers", checks);
        Check(PoolNames.Select(n => visuals.transform.Find(n).gameObject.GetInstanceID()).SequenceEqual(expectedIds), "Re-enable preserves pool identities", checks);
        var bars = UnityEngine.Object.FindObjectsByType<NailongHealthBar>(FindObjectsSortMode.None);
        Check(bars.Length > 0, "Production Nailong health bar present", checks);
        foreach (var bar in bars)
        {
            bar.Refresh(.1f);
            var health = bar.GetComponent<NailongHealth>();
            Check(Mathf.Abs(bar.DisplayedFraction - health.HealthFraction) < .001f, "Nailong HUD refresh matches real HP after reload", checks);
            Check(Field(bar, "encounter") != null, "Encounter panel restored after reload", checks);
            Check(bar.GetComponentsInChildren<Canvas>(true).Count(c => c.name == "Nailong Encounter HUD") == 1, "Exactly one encounter panel after reload", checks);
            var panel = Field(bar, "encounter");
            for (int i = 0; i < 5; i++) { bar.enabled = false; bar.enabled = true; }
            Check(ReferenceEquals(panel, Field(bar, "encounter")), "Ordinary re-enable reuses managed HUD panel", checks);
        }
        return Write("reload-after", new { passed = true, frame = Time.frameCount, checks });
    }
}
