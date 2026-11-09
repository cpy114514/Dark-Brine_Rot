using System.Collections.Generic;
using UnityEngine;

public sealed partial class SkyboardRingRally
{
    static readonly Vector3 CircuitOrigin = new Vector3(8000, 3100, 8000);
    static readonly Vector3[] Course =
    {
        new Vector3(0, 9, 34), new Vector3(-9, 12, 72), new Vector3(-39, 17, 109),
        new Vector3(-83, 22, 124), new Vector3(-127, 25, 108), new Vector3(-154, 24, 73),
        new Vector3(-153, 18, 31), new Vector3(-126, 13, -6), new Vector3(-84, 10, -23),
        new Vector3(-37, 9, -16)
    };
    static readonly Vector3 Grid = new Vector3(0, 9, 0);
    const float DeckDrop = 8.6f;
    readonly List<Mesh> sceneryMeshes = new List<Mesh>();
    GameObject circuit;

    void BuildCircuit()
    {
        circuit = new GameObject("Midnight Sugar Circuit");
        circuit.transform.SetParent(transform, false);
        circuit.transform.position = CircuitOrigin;
        var navy = CircuitMaterial("Midnight road", new Color(.055f, .10f, .22f));
        var edge = CircuitMaterial("Cyan rail lights", new Color(.14f, .92f, 1f));
        var pink = CircuitMaterial("Pink rail lights", new Color(1f, .25f, .63f));
        var lane = CircuitMaterial("Runway dashes", new Color(.28f, .43f, .69f));
        var gold = CircuitMaterial("Finish lights", new Color(1f, .80f, .26f));
        var white = CircuitMaterial("Timing stripe white", new Color(.92f, .96f, 1f));
        var black = CircuitMaterial("Timing stripe navy", new Color(.05f, .04f, .15f));
        var supports = CircuitMaterial("Circuit supports", new Color(.10f, .14f, .29f));
        var track = SampleRoad();
        Ribbon("Solid racing deck", track, -8.2f, 8.2f, 0, navy, true);
        Ribbon("Inside light strip", track, -8.05f, -7.6f, .025f, edge, false);
        Ribbon("Outside light strip", track, 7.6f, 8.05f, .025f, pink, false);

        for (int i = 3; i < track.Count - 3; i += 4)
        {
            Vector3 tangent = track[i + 1] - track[i - 1];
            var dash = CircuitPart("Center line", PrimitiveType.Cube, track[i] + Vector3.up * .025f,
                new Vector3(.26f, .04f, 2.6f), lane);
            dash.transform.localRotation = Quaternion.LookRotation(tangent);
        }
        for (int i = 7; i < track.Count - 5; i += 7)
        {
            Vector3 tangent = track[i + 1] - track[i - 1]; tangent.y = 0;
            Vector3 right = Vector3.Cross(Vector3.up, tangent.normalized);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 foot = track[i] + right * (8.05f * side);
                CircuitPart("Runway beacon post", PrimitiveType.Cube, foot + Vector3.up * .65f,
                    new Vector3(.34f, 1.3f, .34f), supports, true);
                CircuitPart("Runway beacon", PrimitiveType.Sphere, foot + Vector3.up * 1.3f,
                    Vector3.one * .60f, side < 0 ? edge : pink);
            }
            CircuitPart("Floating course support", PrimitiveType.Cylinder, track[i] - Vector3.up * 5,
                new Vector3(2.5f, 4.8f, 2.5f), supports, true);
        }

        BuildGantry(Grid + Vector3.forward * 15f, Vector3.forward, "START", edge, supports, white, black);
        Vector3 finishDirection = Course[9] - Course[8]; finishDirection.y = 0;
        BuildGantry(Course[9] + finishDirection.normalized * 2f, finishDirection.normalized, "FINISH", gold, supports, white, black);
        BuildBackdrop();
        circuit.SetActive(false);
    }

    static List<Vector3> SampleRoad()
    {
        var knots = new List<Vector3> { Grid - Vector3.forward * 17, Grid };
        knots.AddRange(Course);
        knots.Add(Course[9] + (Course[9] - Course[8]).normalized * 20);
        var path = new List<Vector3>();
        for (int i = 0; i < knots.Count - 1; i++)
        {
            Vector3 a = knots[Mathf.Max(0, i - 1)], b = knots[i], c = knots[i + 1], d = knots[Mathf.Min(i + 2, knots.Count - 1)];
            for (int j = 0; j < 10; j++)
            {
                float t = j / 10f;
                Vector3 point = .5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * (t * t) +
                    (-a + 3 * b - 3 * c + d) * (t * t * t));
                path.Add(point - Vector3.up * DeckDrop);
            }
        }
        path.Add(knots[knots.Count - 1] - Vector3.up * DeckDrop);
        return path;
    }

    void Ribbon(string name, List<Vector3> path, float left, float right, float lift, Material material, bool solid)
    {
        var vertices = new Vector3[path.Count * 2];
        var triangles = new int[(path.Count - 1) * 6];
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 tangent = path[Mathf.Min(path.Count - 1, i + 1)] - path[Mathf.Max(0, i - 1)];
            Vector3 across = Vector3.Cross(Vector3.up, tangent).normalized;
            vertices[i * 2] = path[i] + across * left + Vector3.up * lift;
            vertices[i * 2 + 1] = path[i] + across * right + Vector3.up * lift;
            if (i == path.Count - 1) continue;
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        Mesh mesh = CircuitMesh(name, vertices, triangles, material);
        if (solid) circuit.transform.Find(name).gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    void BuildGantry(Vector3 point, Vector3 direction, string title, Material lights, Material metal, Material white, Material black)
    {
        var frame = new GameObject(title + " gantry");
        frame.transform.SetParent(circuit.transform, false);
        frame.transform.localPosition = point;
        frame.transform.localRotation = Quaternion.LookRotation(direction);
        for (int side = -1; side <= 1; side += 2)
        {
            AddGantryPart("Gantry column", new Vector3(10f * side, .75f, 0), new Vector3(1.4f, 18.5f, 1.4f), metal);
            AddGantryPart("Column light", new Vector3(10f * side, .75f, -.74f), new Vector3(.42f, 18f, .10f), lights, false);
        }
        AddGantryPart("Gantry beam", new Vector3(0, 10f, 0), new Vector3(21.4f, 2.4f, 1.4f), metal);
        AddGantryPart("Gantry trim", new Vector3(0, 11.25f, -.74f), new Vector3(21.4f, .25f, .12f), lights, false);
        for (int i = 0; i < 8; i++)
            for (int row = 0; row < 2; row++)
                AddGantryPart("Checkered timing tile", new Vector3((i - 3.5f) * 2, -DeckDrop + .10f, row - .5f),
                    new Vector3(2f, .10f, 1f), (i + row) % 2 == 0 ? white : black, false);

        SkyboardReward.Label(frame.transform, title);
        var label = frame.transform.Find("Pickup label");
        label.localPosition = new Vector3(0, 10f, -.78f);
        label.localScale = Vector3.one * .034f;
        var text = label.GetComponentInChildren<UnityEngine.UI.Text>();
        text.fontSize = 34;

        void AddGantryPart(string name, Vector3 at, Vector3 size, Material material, bool solid = true)
        {
            var part = CircuitPart(name, PrimitiveType.Cube, at, size, material, solid);
            part.transform.SetParent(frame.transform, false);
        }
    }

    void BuildBackdrop()
    {
        const int starCount = 850;
        const float radius = 345f;
        var stars = CircuitMaterial("Starlight", new Color(.80f, .84f, .90f));
        var rng = new System.Random(8024);
        var center = new Vector3(-70, 12, 53);
        var vertices = new Vector3[starCount * 4];
        var triangles = new int[starCount * 6];
        for (int i = 0; i < starCount; i++)
        {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2;
            float elevation = (float)rng.NextDouble() * 2 - 1;
            float horizontal = Mathf.Sqrt(1 - elevation * elevation);
            Vector3 direction = new Vector3(Mathf.Cos(angle) * horizontal, elevation, Mathf.Sin(angle) * horizontal);
            Vector3 star = center + direction * radius;
            float size = Mathf.Lerp(.23f, .79f, Mathf.Pow((float)rng.NextDouble(), 2));
            Vector3 right = new Vector3(Mathf.Sin(angle), 0, -Mathf.Cos(angle));
            Vector3 up = Vector3.Cross(right, direction);
            int v = i * 4, t = i * 6;
            vertices[v] = star - up * size; vertices[v + 1] = star + right * size;
            vertices[v + 2] = star + up * size; vertices[v + 3] = star - right * size;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        CircuitMesh("Star field", vertices, triangles, stars);
    }

    Material CircuitMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0);
        materials.Add(material);
        return material;
    }

    GameObject CircuitPart(string name, PrimitiveType type, Vector3 local, Vector3 size, Material material, bool solid = false)
    {
        var obj = Solid(name, type, circuit.transform, local, size, material);
        if (!solid) obj.GetComponent<Collider>().enabled = false;
        return obj;
    }

    Mesh CircuitMesh(string name, Vector3[] vertices, int[] triangles, Material material)
    {
        var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); sceneryMeshes.Add(mesh);
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(circuit.transform, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return mesh;
    }
}
