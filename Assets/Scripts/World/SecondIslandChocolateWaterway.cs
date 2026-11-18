using System;
using UnityEngine;

/// <summary>Baked waterline shared by shore authoring, player contact and spatial audio.</summary>
public sealed class SecondIslandChocolateWaterway : ScriptableObject
{
    [Serializable]
    public struct Section
    {
        public Vector3 left, right;
        public float distance, speed, eddy;
        public Vector3 Center => (left + right) * .5f;
        public float HalfWidth => Vector3.Distance(left, right) * .5f;
    }

    [Serializable]
    public struct Crossing
    {
        public string label;
        public Vector3 from, to;
    }

    public Vector3 lakeCenter, outletDirection;
    public Vector3[] lakeOutline;
    public Section[] sections;
    public Crossing[] crossings;
    public Vector3[] previousChannel;
    public Vector3 FallLip => sections[sections.Length - 1].Center;

    public bool TrySample(Vector3 point, out Vector3 surface, out Vector3 current)
    {
        surface = point;
        current = Vector3.zero;
        Vector2 p = XZ(point);
        if (Mathf.Abs(point.y - lakeCenter.y) < 4f && InsideLake(p))
        {
            surface.y = lakeCenter.y;
            float pull = 1f - Mathf.SmoothStep(0f, 1f, Vector2.Distance(p, XZ(sections[0].Center)) / 32f);
            current = outletDirection * Mathf.Lerp(.015f, .24f, pull);
            return true;
        }

        for (int i = 1; i < sections.Length; i++)
        {
            Section a = sections[i - 1], b = sections[i];
            if (Mathf.Abs(point.y - a.left.y) > 5f && Mathf.Abs(point.y - b.left.y) > 5f) continue;
            if (p.x < Mathf.Min(Mathf.Min(a.left.x, a.right.x), Mathf.Min(b.left.x, b.right.x)) ||
                p.x > Mathf.Max(Mathf.Max(a.left.x, a.right.x), Mathf.Max(b.left.x, b.right.x)) ||
                p.y < Mathf.Min(Mathf.Min(a.left.z, a.right.z), Mathf.Min(b.left.z, b.right.z)) ||
                p.y > Mathf.Max(Mathf.Max(a.left.z, a.right.z), Mathf.Max(b.left.z, b.right.z))) continue;

            if (!Triangle(p, a.left, a.right, b.left, out float height) &&
                !Triangle(p, a.right, b.right, b.left, out height)) continue;
            Vector2 axis = XZ(b.Center - a.Center);
            float t = Fraction(p, XZ(a.Center), axis);
            Vector2 across = Vector2.Lerp(XZ(a.right - a.left), XZ(b.right - b.left), t);
            float lateral = Vector2.Dot(p - Vector2.Lerp(XZ(a.Center), XZ(b.Center), t), across) * 2f /
                Mathf.Max(.001f, across.sqrMagnitude);
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, 1f, Mathf.Abs(lateral)));
            float eddy = Mathf.Lerp(a.eddy, b.eddy, t);
            float bankSpeed = lateral * eddy > 0f ? -.14f * Mathf.Abs(eddy) : .15f;
            float speed = Mathf.Lerp(a.speed, b.speed, t) * Mathf.Lerp(1f, bankSpeed, edge);
            surface.y = height;
            current = new Vector3(axis.x, 0f, axis.y).normalized * speed;
            return true;
        }
        return false;
    }

    public Vector3 NearestSurface(Vector3 point)
    {
        if (TrySample(point, out var surface, out _)) return surface;
        Vector2 p = XZ(point);
        Vector3 closest = lakeCenter;
        float best = float.PositiveInfinity;
        for (int i = 0; i < lakeOutline.Length; i++)
            ConsiderEdge(point, p, lakeOutline[i], lakeOutline[(i + 1) % lakeOutline.Length], ref closest, ref best);
        for (int i = 1; i < sections.Length; i++)
        {
            ConsiderEdge(point, p, sections[i - 1].left, sections[i].left, ref closest, ref best);
            ConsiderEdge(point, p, sections[i - 1].right, sections[i].right, ref closest, ref best);
        }
        return closest;
    }

    public bool IsInChangedGround(Vector3 point)
    {
        Vector2 p = XZ(point);
        if ((p - XZ(lakeCenter)).sqrMagnitude < 130f * 130f) return true;
        foreach (Vector3 old in previousChannel)
            if ((p - XZ(old)).sqrMagnitude < 46f * 46f) return true;
        foreach (Section section in sections)
            if ((p - XZ(section.Center)).sqrMagnitude < 45f * 45f) return true;
        return false;
    }

    bool InsideLake(Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = lakeOutline.Length - 1; i < lakeOutline.Length; j = i++)
        {
            Vector2 a = XZ(lakeOutline[i]), b = XZ(lakeOutline[j]);
            if ((a.y > p.y) != (b.y > p.y) && p.x < a.x + (b.x - a.x) * (p.y - a.y) / (b.y - a.y))
                inside = !inside;
        }
        return inside;
    }

    static void ConsiderEdge(Vector3 at, Vector2 p, Vector3 a, Vector3 b, ref Vector3 nearest, ref float best)
    {
        Vector3 candidate = Vector3.Lerp(a, b, Fraction(p, XZ(a), XZ(b - a)));
        float distance = (at - candidate).sqrMagnitude;
        if (distance >= best) return;
        best = distance;
        nearest = candidate;
    }

    static bool Triangle(Vector2 p, Vector3 a, Vector3 b, Vector3 c, out float height)
    {
        Vector2 x = XZ(b - a), y = XZ(c - a), q = p - XZ(a);
        float determinant = x.x * y.y - y.x * x.y;
        height = a.y;
        if (Mathf.Abs(determinant) < .00001f) return false;
        float u = (q.x * y.y - y.x * q.y) / determinant;
        float v = (x.x * q.y - q.x * x.y) / determinant;
        height += (b.y - a.y) * u + (c.y - a.y) * v;
        return u >= -.0001f && v >= -.0001f && u + v <= 1.0001f;
    }

    static float Fraction(Vector2 p, Vector2 start, Vector2 delta) =>
        Mathf.Clamp01(Vector2.Dot(p - start, delta) / Mathf.Max(.001f, delta.sqrMagnitude));

    static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
}
