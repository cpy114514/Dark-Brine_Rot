using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class AuditSharkFilmV241
{
    public static object Start(string label)
    {
        var go = new GameObject("Temporary independent shark film geometry review");
        go.AddComponent<SharkFilmAuditV241>().folder = ".codex/shark-direction-20261023/" + label;
        return new { started = true };
    }
}
[DefaultExecutionOrder(31520)] public sealed class SharkFilmAuditV241 : MonoBehaviour
{
    public string folder;
    Story1WreckBattle battle;
    Mesh mesh;
    object tree;
    int lastContacts, samples;
    readonly List<object> rows = new List<object>();
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    object Get(string name) => typeof(Story1WreckBattle).GetField(name, Flags).GetValue(battle);
    object Call(string name, params object[] args) => typeof(Story1WreckBattle).GetMethod(name, Flags).Invoke(battle, args);
    void LateUpdate()
    {
        if (!battle) battle = UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        if (!battle || !battle.RedesignActive) return;
        bool film = battle.RedesignStage == Story1WreckBattle.OpeningStage.Pursuit || battle.RedesignStage == Story1WreckBattle.OpeningStage.Duel;
        bool contact = battle.RedesignContacts != lastContacts; lastContacts = battle.RedesignContacts;
        if (!film || !contact && ++samples % (Time.captureDeltaTime>.08f?5:15) != 0) return;
        try
        {
            var skin = (SkinnedMeshRenderer)Call("RedContactSkin");
            if (!mesh) mesh = new Mesh(); skin.BakeMesh(mesh, true);
            var points = mesh.vertices;
            for (int i = 0; i < points.Length; i++) points[i] = skin.transform.TransformPoint(points[i]);
            var type = typeof(Story1WreckBattle).GetNestedType("RedRiggingTree", BindingFlags.NonPublic);
            if (tree == null) tree = Activator.CreateInstance(type, new object[] { points, mesh.triangles });
            else type.GetMethod("Refit").Invoke(tree, new object[] { points });
            Call("RedTutorialBody"); float bodyGap = .3f;
            foreach (var capsule in (IList)Get("redWaterCapsules"))
            {
                var c = capsule.GetType();
                float gap = (float)type.GetMethod("CapsuleGapUpTo").Invoke(tree, new object[] {
                    c.GetField("a").GetValue(capsule), c.GetField("b").GetValue(capsule), c.GetField("radius").GetValue(capsule), .3f });
                bodyGap = Mathf.Min(bodyGap, gap);
            }
            var wood = new List<string>();
            foreach (var shape in UnityEngine.Object.FindObjectsByType<Story1WreckMeshCollision>(FindObjectsSortMode.None))
            {
                if (!shape.Active || !shape.WorldBounds.Intersects(skin.bounds)) continue;
                var filter = shape.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh) continue;
                var vertices = filter.sharedMesh.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i] = shape.transform.TransformPoint(vertices[i]);
                object[] args = { vertices, filter.sharedMesh.triangles, Vector3.zero };
                if ((bool)type.GetMethod("Touch", new[] { typeof(Vector3[]), typeof(int[]), typeof(Vector3).MakeByRefType() }).Invoke(tree, args)) wood.Add(shape.name);
            }
            rows.Add(new { frame = Time.frameCount, stage = battle.RedesignStage.ToString(), time = (float)Get("redesignTime"),
                shot = battle.RedesignShot, contact, contacts = battle.RedesignContacts, bodyGap, wood,
                battle.RedesignSolidUnresolved, battle.RedesignSolidFractures, battle.RedesignSolidSupportLift, battle.RedesignSolidHeroWoodDepth,
                tailHit = (bool)Get("redTailHit"), overhead = (bool)Get("redOverheadHit"),
                scale = new[] { skin.transform.lossyScale.x, skin.transform.lossyScale.y, skin.transform.lossyScale.z } });
            File.WriteAllText(folder + "/independent-film-geometry.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception e) { File.WriteAllText(folder + "/film-audit-error.txt", e.ToString()); enabled = false; }
    }
    void OnDestroy() { if (mesh) Destroy(mesh); }
}
